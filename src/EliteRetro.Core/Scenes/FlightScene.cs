using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EliteRetro.Core.Entities;
using EliteRetro.Core.Rendering;
using EliteRetro.Core.Managers;
using EliteRetro.Core.Systems;
using EliteRetro.Core.HUD;
using EliteRetro.Core.Utilities;

namespace EliteRetro.Core.Scenes;

/// <summary>
/// Main gameplay scene: first-person cockpit view inside the local bubble.
/// Player flies through space, interacts with planets, stations, and ships.
/// </summary>
public class FlightScene : GameScene
{
    private const float RenderScale = 0.001f; // Elite internal units -> MonoGame world units
    private const int EventMsgX = 300;
    private const int EventMsgY = 350;
    private const int MilestoneMsgY = 200;
    private WireframeRenderer _wireframeRenderer = null!;
    private CircleRenderer _circleRenderer = null!;
    private HudRenderer _hudRenderer = null!;
    private ScannerRenderer _scannerRenderer = null!;
    private BitmapFont _font = null!;
    private GraphicsDevice? _graphicsDevice;
    private IGameContext _gameInstance = null!;
    private IBubbleManager _bubbleManager = null!;
    private OrientationMatrix _universeOrientation = OrientationMatrix.Identity;
    private Matrix _view;
    private Matrix _projection;
    private bool _initialized;
    private bool _showHiddenEdges = true;
    private int _planetRotation;
    private int _planetRotationCounter;
    private int _viewMode; // 0=front, 1=rear, 2=left, 3=right
    private Vector3 _cameraLookDir = -Vector3.UnitZ; // current camera look direction in world space
    private float _cameraDistance = 80f;
    private float _playerSpeed;
    private float _cumulativeRoll; // accumulated roll angle in radians, for planet/ring counter-rotation
    private int _spawnCounter; // frame counter for random ship spawning
    private readonly Random _rng = new Random();
    private int _damageFlashTimer; // frames remaining for red damage flash
    private byte _lastPlayerHull; // track hull for damage detection
    private byte _lastPlayerEnergy; // track energy/shields for damage detection
    private Texture2D _whitePixel = null!; // 1x1 white texture for damage flash overlay
    private bool _ramMode; // when true, spawned entities aim directly at player
    private GameTime _lastGameTime = null!;
    private bool _isFiring; // true when player is firing lasers
    private FlightControlState _lastControl; // store last input state for HUD
    private readonly GalaxySeed _systemSeed;
    private bool _planetHit;
    private float _lastMoveStep;
    private float _lastDt;
    private float _prevPlanetDist;
    private float _prevPlanetZ;
    private float _planetDistDelta;
    private float _planetZDelta;
    private int _lastBackBufferW;
    private int _lastBackBufferH;
    private int _lastViewH;
    private const float HudHeightFraction = 0.34f; // ~1/3 of screen for HUD panel
    private readonly RasterizerState _scissorRasterizer = new RasterizerState { ScissorTestEnable = true };

    public FlightScene(IGameContext? game = null, GalaxySeed? systemSeed = null)
    {
        if (game != null)
        {
            _gameInstance = game;
            // Temporary cast until we move to full DI for BubbleManager
            if (game is GameInstance gi)
                _bubbleManager = gi.BubbleManager;
        }
        if (_bubbleManager != null)
        {
            _bubbleManager.EntityEvent += OnEntityEvent;
            _bubbleManager.CollisionEvent += OnCollision;
        }

        _systemSeed = systemSeed ?? GalaxySeed.Galaxy0System0;
    }

    public override void LoadContent(ContentManager content, BitmapFont font, GraphicsDevice graphicsDevice)
    {
        _font = font;
        _graphicsDevice = graphicsDevice;
        _wireframeRenderer = new WireframeRenderer(_graphicsDevice);
        _circleRenderer = new CircleRenderer(_graphicsDevice);
        _hudRenderer = new HudRenderer(_graphicsDevice);
        _scannerRenderer = new ScannerRenderer(_graphicsDevice);
        _gameInstance.Stardust.Initialize(42); // Fixed seed for consistent starfield

        // Initialize audio
        _gameInstance?.Audio.Initialize();

        // Create 1x1 white texture for damage flash overlay
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });

        // Apply persisted "draw invisible" setting as initial hidden-edge mode
        _showHiddenEdges = _gameInstance?.DrawInvisible ?? false;

        // Elite feels wrong if you start at absolute zero speed
        if (_playerSpeed <= 0f)
            _playerSpeed = GameConstants.SpeedMax;
    }

    private void EnsureProjectionMatchesViewport()
    {
        if (_graphicsDevice == null) return;
        int w = _graphicsDevice.Viewport.Width;
        int h = _graphicsDevice.Viewport.Height;
        int hudH = (int)MathF.Round(h * HudHeightFraction);
        int viewH = Math.Max(1, h - hudH);

        if (w == _lastBackBufferW && h == _lastBackBufferH && viewH == _lastViewH)
            return;

        _lastBackBufferW = w;
        _lastBackBufferH = h;
        _lastViewH = viewH;

        _projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(75f),
            w / (float)viewH,
            0.1f, 1000f);
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;

        if (_gameInstance != null)
        {
            InitializeBubble();
            // Initialize damage tracking
            _lastPlayerHull = _gameInstance.PlayerManager.Ship.Hull;
            _lastPlayerEnergy = _gameInstance.PlayerManager.Ship.Energy;
            _initialized = true;
        }
    }

    private void InitializeBubble()
    {
        _bubbleManager.Clear();

        int fistBit0 = _gameInstance.PlayerManager.Commander.LegalStatus & 1;
        var (planetPos, sunPos) = ComputeSolarSpawn(_systemSeed, fistBit0);

        // Slot 0: Planet
        var planetBlueprint = new ShipBlueprint
        {
            Name = "Planet",
            Model = PlanetModel.Create(GameConstants.PlanetRadius),
            MaxSpeed = 0, MaxEnergy = 255, HullStrength = 255, ShieldStrength = 255
        };
        _bubbleManager.SetSlot(GameConstants.PlanetSlot, new ShipInstance(planetBlueprint) { Position = planetPos });

        // Slot 1: Sun
        var sunBlueprint = new ShipBlueprint
        {
            Name = "Sun",
            Model = SunModel.Create(GameConstants.PlanetRadius * 6),
            MaxSpeed = 0, MaxEnergy = 255, HullStrength = 255, ShieldStrength = 255
        };
        _bubbleManager.SetSlot(GameConstants.SunStationSlot, new ShipInstance(sunBlueprint) { Position = sunPos });
    }

    private static (Vector3 planetPos, Vector3 sunPos) ComputeSolarSpawn(GalaxySeed seed, int fistBit0)
    {
        int planetZHi = ((seed.W0Hi & 0x07) + 6 + (fistBit0 & 1)) >> 1;
        int planetXYHi = (fistBit0 & 1) == 0 ? -2 : 3;
        Vector3 planetPos = new Vector3(planetXYHi << 15, planetXYHi << 15, +(planetZHi << 15));

        int sunZHi = (seed.W1Hi & 0x03) + 4;
        int sunOff = seed.W1Lo & 0x03;
        if (sunOff > 2) sunOff = 2;
        Vector3 sunPos = new Vector3(sunOff << 15, sunOff << 15, -(sunZHi << 15));

        return (planetPos, sunPos);
    }

    public override void Update(GameTime gameTime)
    {
        _lastGameTime = gameTime;
        var input = _gameInstance.Input;
        _lastControl = _gameInstance.FlightControl.Update(gameTime, input);

        _isFiring = _lastControl.FireLaser;
        if (_isFiring && _gameInstance.Combat.LaserCooldown <= 0)
        {
            _gameInstance.Audio.PlayLaser();
            _gameInstance.Combat.FireLaser(_gameInstance, _bubbleManager, _viewMode);
        }

        if (!_lastControl.IsPaused)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _lastDt = dt;

            if (_planetHit)
            {
                _lastMoveStep = 0f;
                if (_lastControl.ExitRequested)
                    _gameInstance.ChangeScene(new MainMenuScene(_gameInstance));
                return;
            }

            float rollDelta = MathHelper.Clamp(_lastControl.RollAngle * dt * 60f, -0.1f, 0.1f);
            float pitchDelta = MathHelper.Clamp(_lastControl.PitchAngle * dt * 60f, -0.1f, 0.1f);
            
            float moveStep = _playerSpeed * dt * 60f;
            _lastMoveStep = moveStep;

            // Delegate core universe simulation to service
            _gameInstance.Simulation.Update(_bubbleManager, _playerSpeed, rollDelta, pitchDelta, moveStep);
            
            _cumulativeRoll += rollDelta;
            _gameInstance.Stardust.Update(_playerSpeed, -rollDelta, -pitchDelta, gameTime);
            _bubbleManager.TidyAllActive();
            _gameInstance.Explosions.Update(gameTime, _bubbleManager, _gameInstance.Audio);

            _gameInstance.Simulation.EnforceOverflyDistance(_bubbleManager.Planet, GameConstants.PlanetRadius);
            var sunOrStation = _bubbleManager.SunOrStation;
            if (sunOrStation?.Blueprint?.Name == "Sun")
                _gameInstance.Simulation.EnforceOverflyDistance(sunOrStation, GameConstants.PlanetRadius * 2 * GameConstants.SunFatalDistanceMultiplier);

            CollisionSystem.CheckPlayerCollisions(_bubbleManager);
            
            _gameInstance.Simulation.CheckPlanetCollision(_gameInstance, _bubbleManager, ref _playerSpeed, ref _planetHit, ref _damageFlashTimer);
            if (_planetHit) return;

            _bubbleManager.CleanupExpired();

            if (_planetRotationCounter++ % 32 == 0)
                _planetRotation = (_planetRotation + 1) % 64;

            _spawnCounter++;
            if (_spawnCounter % 180 == 0 && _rng.NextDouble() < 0.3)
                SpawnSystem.SpawnRandomEncounter(_bubbleManager, _ramMode);

            _viewMode = _lastControl.ViewIndex;

            float zoomSpeed = 2f * dt;
            if (_lastControl.ZoomIn) _cameraDistance -= zoomSpeed;
            if (_lastControl.ZoomOut) _cameraDistance += zoomSpeed;
            _cameraDistance = MathHelper.Clamp(_cameraDistance, 2f, 20f);

            if (_lastControl.SpeedDelta != 0)
                _playerSpeed = MathHelper.Clamp(_playerSpeed + _lastControl.SpeedDelta * dt, 0f, GameConstants.SpeedMax);

            if (_bubbleManager.PlayerShip != null)
            {
                _bubbleManager.PlayerShip.Speed = _playerSpeed;
                byte currentHull = _bubbleManager.PlayerShip.Hull;
                byte currentEnergy = _bubbleManager.PlayerShip.Energy;
                if (currentHull < _lastPlayerHull || currentEnergy < _lastPlayerEnergy)
                    _damageFlashTimer = 15;
                _lastPlayerHull = currentHull;
                _lastPlayerEnergy = currentEnergy;
            }
            if (_damageFlashTimer > 0) _damageFlashTimer--;

            _gameInstance.PlayerManager.CheckSunProximity(_bubbleManager);
        }

        if (_lastControl.SaveRequested)
            SaveGame();

        if (_lastControl.ExitRequested)
            _gameInstance.ChangeScene(new MainMenuScene(_gameInstance));

        if (_lastControl.RamModeToggled)
            _ramMode = !_ramMode;

        if (_lastControl.EdgeToggleRequested)
        {
            _showHiddenEdges = !_showHiddenEdges;
            _gameInstance.DrawInvisible = _showHiddenEdges;
            Systems.OptionsManager.Save(_gameInstance.DrawWhite, _gameInstance.DrawInvisible);
        }

        _cameraLookDir = -Vector3.UnitZ;
        Vector3 forwardBasis = Vector3.UnitZ, sideBasis = Vector3.UnitX, upBasis = Vector3.UnitY;
        switch (_viewMode)
        {
            case 0: _cameraLookDir = -Vector3.UnitZ; forwardBasis = Vector3.UnitZ; sideBasis = Vector3.UnitX; break;
            case 1: _cameraLookDir = Vector3.UnitZ; forwardBasis = -Vector3.UnitZ; sideBasis = -Vector3.UnitX; break;
            case 2: _cameraLookDir = -Vector3.UnitX; forwardBasis = Vector3.UnitX; sideBasis = Vector3.UnitZ; break;
            case 3: _cameraLookDir = Vector3.UnitX; forwardBasis = -Vector3.UnitX; sideBasis = -Vector3.UnitZ; break;
        }
        _view = new Matrix(sideBasis.X, upBasis.X, forwardBasis.X, 0, sideBasis.Y, upBasis.Y, forwardBasis.Y, 0, sideBasis.Z, upBasis.Z, forwardBasis.Z, 0, 0, 0, 0, 1);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        EnsureInitialized();
        if (_graphicsDevice == null || !_initialized) return;
        _graphicsDevice.Clear(_damageFlashTimer > 0 ? Color.DarkRed : Color.Black);
        EnsureProjectionMatchesViewport();

        int screenW = _graphicsDevice.Viewport.Width, screenH = _graphicsDevice.Viewport.Height;
        int hudH = (int)MathF.Round(screenH * HudHeightFraction), viewH = Math.Max(1, screenH - hudH);
        var screenRect = new Rectangle(0, 0, screenW, screenH);
        var viewRect = new Rectangle(0, 0, screenW, viewH);
        var hudRect = new Rectangle(0, viewH, screenW, hudH);
        var screenCenter = new Vector2(viewRect.X + viewRect.Width / 2f, viewRect.Y + viewRect.Height / 2f);

        int outerMargin = GetOuterMarginPixels(screenW, screenH);
        var viewContentRect = InsetRect(viewRect, outerMargin * 2);
        var hudContentRect = InsetRect(hudRect, outerMargin);

        BeginScissored(spriteBatch, viewContentRect);
        _gameInstance.Stardust.Draw(spriteBatch, screenCenter, 500f, _view, _gameInstance.DrawWhite);

        // Draw explosions via service
        _gameInstance.Explosions.Draw(spriteBatch, ProjectToScreenElite, pos => ToMonoGameWorld(pos).Length(), IsInFrontOfCamera, _gameInstance.DrawWhite);

        foreach (var entity in _bubbleManager.GetAllActive())
        {
            if (entity.SlotIndex == GameConstants.PlayerSlot) continue;
            if (entity.Blueprint.Name == "Planet" || entity.Blueprint.Name == "Sun" || !IsInFrontOfCamera(entity.Position)) continue;

            Matrix entityOrientation = new Matrix(entity.Orientation.Sidev.X, entity.Orientation.Sidev.Y, entity.Orientation.Sidev.Z, 0, entity.Orientation.Roofv.X, entity.Orientation.Roofv.Y, entity.Orientation.Roofv.Z, 0, entity.Orientation.Nosev.X, entity.Orientation.Nosev.Y, entity.Orientation.Nosev.Z, 0, 0, 0, 0, 1);
            Vector3 entityPosMG = ToMonoGameWorld(entity.Position);
            Matrix world = Matrix.CreateScale(RenderScale) * entityOrientation * Matrix.CreateTranslation(entityPosMG);
            _wireframeRenderer.Draw(entity.Blueprint.Model, world, _view, _projection, spriteBatch, drawHiddenEdges: _showHiddenEdges, drawWhite: _gameInstance.DrawWhite);
        }

        _gameInstance.Celestial.Draw(spriteBatch, _bubbleManager, _view, _projection, _cameraLookDir, _graphicsDevice!, HudHeightFraction, _gameInstance.DrawWhite);

        if (_damageFlashTimer > 0)
        {
            byte alpha = (byte)((_damageFlashTimer / 15f) * 128);
            spriteBatch.Draw(_whitePixel, viewContentRect, new Color(255, 0, 0, (int)alpha));
        }

        int cx = (int)screenCenter.X, cy = (int)screenCenter.Y;
        const int inner = 16, outer = 48;
        var crossColor = Color.White;
        spriteBatch.Draw(_whitePixel, new Rectangle(cx - 1, cy - 1, 2, 2), crossColor);
        spriteBatch.Draw(_whitePixel, new Rectangle(cx - 1, cy - outer, 2, outer - inner), crossColor);
        spriteBatch.Draw(_whitePixel, new Rectangle(cx - 1, cy + inner, 2, outer - inner), crossColor);
        spriteBatch.Draw(_whitePixel, new Rectangle(cx - outer, cy - 1, outer - inner, 2), crossColor);
        spriteBatch.Draw(_whitePixel, new Rectangle(cx + inner, cy - 1, outer - inner, 2), crossColor);

        if (_gameInstance.Combat.LaserFlashTimer > 0)
        {
            DrawLine(spriteBatch, new Vector2(viewContentRect.Left, viewContentRect.Bottom), new Vector2(cx, cy), Color.Yellow, 2);
            DrawLine(spriteBatch, new Vector2(viewContentRect.Right, viewContentRect.Bottom), new Vector2(cx, cy), Color.Yellow, 2);
        }

        DrawViewOverlay(spriteBatch, viewContentRect);
        spriteBatch.End();

        BeginScissored(spriteBatch, hudContentRect);
        DrawHUD(spriteBatch, hudContentRect, screenRect);
        spriteBatch.End();

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        DrawFrame(spriteBatch, viewContentRect, Color.White, 1);
        DrawFrame(spriteBatch, hudContentRect, Color.White, 1);
        spriteBatch.End();
    }

    private void DrawHUD(SpriteBatch spriteBatch, Rectangle hudRect, Rectangle screenRect)
    {
        var hudState = _gameInstance.Hud.CalculateState(
            _gameInstance,
            _playerSpeed,
            _lastControl.PitchAngle / GameConstants.PitchMax,
            _lastControl.RollAngle / GameConstants.RollMax,
            _cumulativeRoll,
            _viewMode,
            _gameInstance.Messages.GeneralMessage ?? "",
            _gameInstance.Messages.GeneralTimer,
            _showHiddenEdges);

        _hudRenderer.Draw(spriteBatch, hudState, _font, hudRect, screenRect);

        int leftW = (int)MathF.Round(hudRect.Width * 0.25f);
        _scannerRenderer.Draw(spriteBatch, _bubbleManager, GameConstants.PlayerSlot, _universeOrientation, new Rectangle(hudRect.X + leftW, hudRect.Y, hudRect.Width - leftW * 2, hudRect.Height));

        var msg = _gameInstance.Messages.GeneralMessage;
        if (_gameInstance.Messages.GeneralTimer > 0 && !string.IsNullOrEmpty(msg))
        {
            bool isMilestone = msg == "RIGHT ON COMMANDER!";
            var msgSize = _font.MeasureString(msg);
            _font.DrawString(spriteBatch, $">> {msg}", new Vector2(isMilestone ? (screenRect.Width - msgSize.X) / 2 : EventMsgX, isMilestone ? MilestoneMsgY : EventMsgY), isMilestone ? Color.Gold : Color.Yellow, isMilestone ? 2.0f : 1.2f);
        }
        
        var saveMsg = _gameInstance.Messages.StatusMessage;
        if (_gameInstance.Messages.StatusTimer > 0 && !string.IsNullOrEmpty(saveMsg))
        {
            _font.DrawString(spriteBatch, saveMsg, new Vector2(400, 380), Color.Green, 1.4f);
        }
        
        if (_lastControl.IsPaused) _font.DrawString(spriteBatch, "PAUSED", new Vector2(400, 350), Color.Red, 2f);
    }

    private void DrawViewOverlay(SpriteBatch spriteBatch, Rectangle viewContentRect)
    {
        var viewLabel = _viewMode switch { 0 => "FRONT", 1 => "REAR", 2 => "LEFT", 3 => "RIGHT", _ => "FRONT" };
        var labelSz = _font.MeasureString(viewLabel);
        float labelX = viewContentRect.X + (viewContentRect.Width - labelSz.X) / 2;
        float y = viewContentRect.Y + 10;
        _font.DrawString(spriteBatch, viewLabel, new Vector2(labelX, y), Color.Green, 1.5f);
        float x = viewContentRect.X + 10;
        var commander = _gameInstance.PlayerManager.Commander;
        string legalText = commander.LegalStatus switch { 0 => "CLEAN", < 50 => "OFFENDER", _ => "FUGITIVE" };
        var legalSz = _font.MeasureString(legalText);
        _font.DrawString(spriteBatch, legalText, new Vector2(viewContentRect.Right - legalSz.X - 10, y), commander.LegalStatus >= 50 ? Color.OrangeRed : Color.Lime, 1.0f);
        string rankText = commander.RankName;
        if (!string.IsNullOrEmpty(rankText)) { var rankSz = _font.MeasureString(rankText); _font.DrawString(spriteBatch, rankText, new Vector2(viewContentRect.Right - rankSz.X - 10, y + 22), Color.Gold, 1.0f); }

        float planetDist = _bubbleManager.Planet?.Position.Length() ?? 0, sunDist = _bubbleManager.SunOrStation?.Position.Length() ?? 0;
        if (_bubbleManager.Planet != null) { _planetDistDelta = planetDist - _prevPlanetDist; _planetZDelta = _bubbleManager.Planet.Position.Z - _prevPlanetZ; _prevPlanetDist = planetDist; _prevPlanetZ = _bubbleManager.Planet.Position.Z; }
        _font.DrawString(spriteBatch, $"PLANET DIST: {planetDist:F4} (Δ { _planetDistDelta:+0.0000;-0.0000;0.0000})", new Vector2(x, y + 50), Color.Cyan, 1.2f);
        _font.DrawString(spriteBatch, $"SUN DIST: {sunDist:F4}", new Vector2(x, y + 72), Color.Orange, 1.2f);
        _font.DrawString(spriteBatch, $"SPEED: {_playerSpeed:F4}", new Vector2(x, y + 130), Color.Gray, 0.9f);
        if (_planetHit) _font.DrawString(spriteBatch, "PLANET HIT: TRUE", new Vector2(x, y + 148), Color.Red, 0.9f);
        _font.DrawString(spriteBatch, _showHiddenEdges ? "HIDDEN: ON" : "HIDDEN: OFF", new Vector2(x, y + 190), Color.White, 0.8f);
        _font.DrawString(spriteBatch, _ramMode ? "RAM MODE: ON (press R to toggle)" : "RAM MODE: OFF (press R to toggle)", new Vector2(x, y + 212), _ramMode ? Color.Red : Color.DarkGray, 0.8f);

        int debugY = (int)(y + 235);
        foreach (var entity in _bubbleManager.GetActiveShips()) { float dist = entity.Position.Length(); if (dist < 500) { _font.DrawString(spriteBatch, $"{entity.Blueprint.Name}: {dist:F0}", new Vector2(x, debugY), dist < 50 ? Color.Red : dist < 200 ? Color.Orange : Color.Yellow, 0.8f); debugY += 22; if (debugY > viewContentRect.Bottom - 24) break; } }
        _font.DrawString(spriteBatch, "ARROWS: PITCH/ROLL  W/S: SPEED  V: VIEW  SPACE: FIRE  P: PAUSE  +/-: ZOOM  I: EDGES  F5: SAVE  ESC: MENU", new Vector2(viewContentRect.X + 10, viewContentRect.Bottom - 24), Color.Gray, 0.8f);
    }

    private static int GetOuterMarginPixels(int w, int h) => Math.Max(6, (int)MathF.Round(MathF.Min(w, h) * 0.024f));
    private static Rectangle InsetRect(Rectangle r, int inset) => new Rectangle(r.X + inset, r.Y + inset, Math.Max(1, r.Width - inset * 2), Math.Max(1, r.Height - inset * 2));
    private void BeginScissored(SpriteBatch spriteBatch, Rectangle scissorRect) { _graphicsDevice!.ScissorRectangle = scissorRect; spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, _scissorRasterizer); }
    private void DrawFrame(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness) { thickness = Math.Max(1, thickness); spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color); spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color); spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color); spriteBatch.Draw(_whitePixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color); }

    private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, int thickness) { Vector2 edge = end - start; float angle = (float)Math.Atan2(edge.Y, edge.X); spriteBatch.Draw(_whitePixel, start, null, color, angle, new Vector2(0, 0.5f), new Vector2(edge.Length(), thickness), SpriteEffects.None, 0); }
    private bool IsInFrontOfCamera(Vector3 worldPos) { Vector3 mg = ToMonoGameWorld(worldPos); if (mg.LengthSquared() < 0.001f) return true; return Vector3.Dot(Vector3.Normalize(mg), _cameraLookDir) > 0; }
    private static Vector3 ToMonoGameWorld(Vector3 eliteWorldPos) => new Vector3(eliteWorldPos.X, eliteWorldPos.Y, -eliteWorldPos.Z) * RenderScale;

    private Vector2 ProjectToScreenElite(Vector3 eliteWorldPos)
    {
        Vector3 worldPos = ToMonoGameWorld(eliteWorldPos);
        Vector3 viewPos = Vector3.Transform(worldPos, _view);
        Vector4 projected = Vector4.Transform(new Vector4(viewPos, 1f), _projection);
        int w = _graphicsDevice?.Viewport.Width ?? 1024;
        int h = _graphicsDevice?.Viewport.Height ?? 768;
        int viewH = Math.Max(1, h - (int)MathF.Round(h * HudHeightFraction));
        if (MathF.Abs(projected.W) < 0.001f) return new Vector2(w / 2f, viewH / 2f);
        return new Vector2((projected.X / projected.W + 1f) * 0.5f * w, (1 - projected.Y / projected.W) * 0.5f * viewH);
    }

    private void SaveGame() { 
        if (SaveGameManager.SaveDefault(_gameInstance)) { 
            _gameInstance.Messages.Post("GAME SAVED", MessageType.Status, 120);
        } else { 
            _gameInstance.Messages.Post("SAVE FAILED", MessageType.Status, 180);
        } 
    }

    private void OnEntityEvent(object? sender, EntityEventArgs e) { 
        string msg = e.Reason switch { 
            "lifetime expired" => $"{e.EntityName} disappeared", 
            "out of bounds" => $"{e.EntityName} left sector", 
            _ => $"{e.EntityName} detected" 
        }; 
        _gameInstance.Messages.Post(msg, MessageType.General, 120);
    }

    private void OnCollision(object? sender, CollisionEventArgs e) { 
        _gameInstance.Messages.Post($"COLLISION with {e.OtherShipName}!", MessageType.General, 120);
    }

    public override void UnloadContent()
    {
        if (_bubbleManager != null) { _bubbleManager.EntityEvent -= OnEntityEvent; _bubbleManager.CollisionEvent -= OnCollision; }
        _whitePixel?.Dispose();
    }

    private static void EnforceOverflyDistance(ShipInstance? body, float bodyRadius) { if (body == null) return; Vector3 pos = body.Position; float dist = pos.Length(); if (dist < 1f) return; float noseDot = pos.Z / dist, angleFromNose = MathF.Acos(MathHelper.Clamp(noseDot, -1f, 1f)), sinAngle = MathF.Sin(angleFromNose); float minDist = bodyRadius + bodyRadius * 0.4f * sinAngle; if (dist < minDist) body.Position = (pos / dist) * minDist; }
}
