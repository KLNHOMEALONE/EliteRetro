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
    private StardustRenderer _stardustRenderer = null!;
    private HudRenderer _hudRenderer = null!;
    private ScannerRenderer _scannerRenderer = null!;
    private BitmapFont _font = null!;
    private GraphicsDevice? _graphicsDevice;
    private IGameContext _gameInstance = null!;
    private IBubbleManager _bubbleManager = null!;
    private FlightControlService _flightControlService = null!;
    private OrientationMatrix _universeOrientation = OrientationMatrix.Identity;
    private Matrix _view;
    private Matrix _projection;
    private bool _paused;
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
    private string _lastEventMessage = ""; // HUD message for spawn/despawn events
    private int _eventMessageTimer; // frames remaining to display event message
    private int _damageFlashTimer; // frames remaining for red damage flash
    private byte _lastPlayerHull; // track hull for damage detection
    private byte _lastPlayerEnergy; // track energy/shields for damage detection
    private Texture2D _whitePixel = null!; // 1x1 white texture for damage flash overlay
    private bool _ramMode; // when true, spawned entities aim directly at player
    private GameTime _lastGameTime = null!;
    private string _lastSaveMessage = ""; // HUD message for save confirmation
    private int _saveMessageTimer; // frames remaining to display save message
    private bool _isFiring; // true when player is firing lasers
    private int _laserCooldown; // frames until next shot allowed
    private int _laserFlashTimer; // frames remaining to show laser beam
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
    private const float HudHeightFraction = 0.375f; // 288/768: matches current layout but scales by percentage
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
        _flightControlService = new FlightControlService();
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
        _stardustRenderer = new StardustRenderer(_graphicsDevice);
        _hudRenderer = new HudRenderer(_graphicsDevice);
        _scannerRenderer = new ScannerRenderer(_graphicsDevice);
        _stardustRenderer.Initialize(42); // Fixed seed for consistent starfield

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
        _lastControl = _flightControlService.Update(gameTime, input);

        // Handle laser fire
        if (_laserCooldown > 0) _laserCooldown--;
        if (_laserFlashTimer > 0) _laserFlashTimer--;

        _isFiring = _lastControl.FireLaser;
        if (_isFiring && _laserCooldown <= 0)
        {
            _gameInstance?.Audio.PlayLaser();
            FireLaserAtTarget();
            _laserCooldown = 15;
            _laserFlashTimer = 6;
        }

        if (!_lastControl.IsPaused)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _lastDt = dt;

            if (_planetHit)
            {
                _lastMoveStep = 0f;
                if (input.IsKeyPressed(Keys.Escape))
                    _gameInstance.ChangeScene(new MainMenuScene(_gameInstance));
                return;
            }

            float rollDelta = MathHelper.Clamp(_lastControl.RollAngle * dt * 60f, -0.1f, 0.1f);
            float pitchDelta = MathHelper.Clamp(_lastControl.PitchAngle * dt * 60f, -0.1f, 0.1f);
            _bubbleManager.ApplyUniverseRotation(-rollDelta, -pitchDelta);
            _cumulativeRoll += rollDelta;

            float moveStep = _playerSpeed * dt * 60f;
            _lastMoveStep = moveStep;
            foreach (var entity in _bubbleManager.GetAllActive())
            {
                if (entity.SlotIndex == GameConstants.PlayerSlot) continue;
                entity.Position.Z -= moveStep;
                if (entity.Speed != 0) entity.MoveForward();
            }

            _stardustRenderer.Update(_playerSpeed, -rollDelta, -pitchDelta);
            _bubbleManager.TidyAllActive();
            _gameInstance.Explosions.Update(gameTime, _bubbleManager, _gameInstance.Audio);

            EnforceOverflyDistance(_bubbleManager.Planet, GameConstants.PlanetRadius);
            var sunOrStation = _bubbleManager.SunOrStation;
            if (sunOrStation?.Blueprint?.Name == "Sun")
                EnforceOverflyDistance(sunOrStation, GameConstants.PlanetRadius * 2 * GameConstants.SunFatalDistanceMultiplier);

            CollisionSystem.CheckPlayerCollisions(_bubbleManager);
            var player = _bubbleManager.PlayerShip;
            var planet = _bubbleManager.Planet;
            if (player != null && planet != null)
            {
                var col = CollisionSystem.CheckPlanetCollision(player, planet);
                if (col.Type == CollisionSystem.PlanetCollisionType.Crash)
                {
                    _lastEventMessage = "PLANET HIT";
                    _eventMessageTimer = int.MaxValue;
                    _playerSpeed = 0f;
                    _planetHit = true;
                    return;
                }
                else if (col.Type == CollisionSystem.PlanetCollisionType.Glancing)
                {
                    _lastEventMessage = "ALTITUDE CRITICAL - SCRAPE!";
                    _eventMessageTimer = 60;
                    player.TakeDamage(15);
                    _damageFlashTimer = 20;
                    planet.Position -= col.PushBack;
                }
            }

            _bubbleManager.CleanupExpired();

            if (_planetRotationCounter++ % 32 == 0)
                _planetRotation = (_planetRotation + 1) % 64;

            _spawnCounter++;
            if (_spawnCounter % 180 == 0 && _rng.NextDouble() < 0.3)
                SpawnSystem.SpawnRandomEncounter(_bubbleManager, _ramMode);

            _viewMode = _lastControl.ViewIndex;

            float zoomSpeed = 2f * dt;
            if (input.IsKeyDown(Keys.OemPlus) || input.IsKeyDown(Keys.Add)) _cameraDistance -= zoomSpeed;
            if (input.IsKeyDown(Keys.OemMinus) || input.IsKeyDown(Keys.Subtract)) _cameraDistance += zoomSpeed;
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

            _bubbleManager.CheckSunProximity();
        }

        if (input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.P) && !_lastControl.IsPaused)
            _paused = !_paused;

        if (input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.F5))
            SaveGame();

        if (input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Escape))
            _gameInstance.ChangeScene(new MainMenuScene(_gameInstance));

        if (input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.R))
            _ramMode = !_ramMode;

        if (input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.I))
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
        _stardustRenderer.Draw(spriteBatch, screenCenter, 500f, _view, _gameInstance.DrawWhite);

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

        var planetEntity = _bubbleManager.Planet;
        var sunEntity = (_bubbleManager.SunOrStation?.Blueprint?.Name == "Sun") ? _bubbleManager.SunOrStation : null;
        CelestialDisc? planetDisc = planetEntity != null ? ComputeCelestialDisc(planetEntity.Position, GameConstants.PlanetRadius) : null;
        CelestialDisc? sunDisc = sunEntity != null ? ComputeCelestialDisc(sunEntity.Position, GameConstants.PlanetRadius * 6) : null;

        if (planetDisc.HasValue || sunDisc.HasValue)
        {
            if (planetDisc.HasValue && sunDisc.HasValue && sunDisc.Value.ViewZ < planetDisc.Value.ViewZ && Vector2.Distance(planetDisc.Value.ScreenCenter, sunDisc.Value.ScreenCenter) < planetDisc.Value.ScreenRadius - 0.5f)
                sunDisc = null;

            if (sunDisc.HasValue && planetDisc.HasValue)
            {
                if (sunDisc.Value.ViewZ < planetDisc.Value.ViewZ) { DrawCelestialSun(spriteBatch, sunDisc.Value); DrawCelestialPlanet(spriteBatch, planetDisc.Value); }
                else { DrawCelestialPlanet(spriteBatch, planetDisc.Value); DrawCelestialSun(spriteBatch, sunDisc.Value); }
            }
            else { if (sunDisc.HasValue) DrawCelestialSun(spriteBatch, sunDisc.Value); if (planetDisc.HasValue) DrawCelestialPlanet(spriteBatch, planetDisc.Value); }
        }

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

        if (_laserFlashTimer > 0)
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
            _lastEventMessage,
            _eventMessageTimer,
            _showHiddenEdges);

        _hudRenderer.Draw(spriteBatch, hudState, _font, hudRect, screenRect);

        int leftW = (int)MathF.Round(hudRect.Width * 0.25f);
        _scannerRenderer.Draw(spriteBatch, _bubbleManager, GameConstants.PlayerSlot, _universeOrientation, new Rectangle(hudRect.X + leftW, hudRect.Y, hudRect.Width - leftW * 2, hudRect.Height));

        if (_eventMessageTimer > 0 && !string.IsNullOrEmpty(_lastEventMessage))
        {
            bool isMilestone = _lastEventMessage == "RIGHT ON COMMANDER!";
            var msgSize = _font.MeasureString(_lastEventMessage);
            _font.DrawString(spriteBatch, $">> {_lastEventMessage}", new Vector2(isMilestone ? (screenRect.Width - msgSize.X) / 2 : EventMsgX, isMilestone ? MilestoneMsgY : EventMsgY), isMilestone ? Color.Gold : Color.Yellow, isMilestone ? 2.0f : 1.2f);
            _eventMessageTimer--;
        }
        if (_saveMessageTimer > 0 && !string.IsNullOrEmpty(_lastSaveMessage))
        {
            _font.DrawString(spriteBatch, _lastSaveMessage, new Vector2(400, 380), Color.Green, 1.4f);
            _saveMessageTimer--;
        }
        if (_paused) _font.DrawString(spriteBatch, "PAUSED", new Vector2(400, 350), Color.Red, 2f);
    }

    private void DrawViewOverlay(SpriteBatch spriteBatch, Rectangle viewContentRect)
    {
        float x = viewContentRect.X + 10, y = viewContentRect.Y + 10;
        _font.DrawString(spriteBatch, _viewMode switch { 0 => "FRONT", 1 => "REAR", 2 => "LEFT", 3 => "RIGHT", _ => "FRONT" }, new Vector2(x, y), new Color(255, 180, 50), 1.5f);
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

    private readonly record struct CelestialDisc(Vector3 WorldPosElite, Vector2 ScreenCenter, float ScreenRadius, float ViewZ);
    private CelestialDisc? ComputeCelestialDisc(Vector3 worldPosElite, float radiusElite) { Vector3 worldMg = ToMonoGameWorld(worldPosElite), viewPos = Vector3.Transform(worldMg, _view); if (viewPos.Z >= -0.001f) return null; Vector2 screenPos = ProjectToScreenElite(worldPosElite); float dist = worldMg.Length(); if (dist < 0.001f) return null; int h = _graphicsDevice!.Viewport.Height; int viewH = Math.Max(1, h - (int)MathF.Round(h * HudHeightFraction)); float screenRadius = ((radiusElite * RenderScale) / dist) * (1.0f / 0.767f) * (viewH / 2f); if (screenRadius <= 0 || screenRadius > 4000) return null; return new CelestialDisc(worldPosElite, screenPos, screenRadius, viewPos.Z); }
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

    private void DrawCelestialSun(SpriteBatch spriteBatch, CelestialDisc disc) => _circleRenderer.DrawFilledCircle(spriteBatch, disc.ScreenCenter, disc.ScreenRadius, Color.White, _gameInstance.DrawWhite);
    private void DrawCelestialPlanet(SpriteBatch spriteBatch, CelestialDisc disc) => _circleRenderer.DrawCircle(spriteBatch, disc.ScreenCenter, disc.ScreenRadius, Color.White, 48, _gameInstance.DrawWhite);

    private void FireLaserAtTarget()
    {
        var player = _gameInstance.PlayerManager.Ship;
        if (player == null) return;
        Vector3 forward = _viewMode switch { 0 => new Vector3(0, 0, 1), 1 => new Vector3(0, 0, -1), 2 => new Vector3(-1, 0, 0), 3 => new Vector3(1, 0, 0), _ => new Vector3(0, 0, 1) };
        ShipInstance? bestTarget = null; float bestDot = -1f;
        foreach (var entity in _bubbleManager.GetAllActive()) { if (entity.SlotIndex == GameConstants.PlayerSlot || !entity.IsActive || entity.Blueprint.Name == "Planet" || entity.Blueprint.Name == "Sun") continue; float distSq = entity.Position.LengthSquared(); if (distSq > 600 * 600) continue; float dist = (float)Math.Sqrt(distSq), dot = (dist < 5.0f) ? 1.0f : Vector3.Dot(forward, entity.Position / dist); if (dot >= 0.96f && dot > bestDot) { bestDot = dot; bestTarget = entity; } }
        if (bestTarget != null) { _gameInstance?.Audio.PlayLaserHit(); int laserDamage = 90; bool destroyed = false; if (bestTarget.Energy > 0) { int shieldDmg = Math.Min(laserDamage, (int)bestTarget.Energy); bestTarget.Energy = (byte)(bestTarget.Energy - shieldDmg); int hullDmg = laserDamage - shieldDmg; if (hullDmg > 0) destroyed = bestTarget.TakeDamage(hullDmg); } else destroyed = bestTarget.TakeDamage(laserDamage); _lastEventMessage = "HIT!"; _eventMessageTimer = 10; if (destroyed) { bool milestone = _gameInstance.PlayerManager.Commander.AddKill(); if (milestone) { _lastEventMessage = "RIGHT ON COMMANDER!"; _eventMessageTimer = 180; } else if ((bestTarget.Blueprint.Personality & NewbFlags.Cop) != 0) { _gameInstance.PlayerManager.Commander.LegalStatus = Math.Max(_gameInstance.PlayerManager.Commander.LegalStatus, (byte)64); _lastEventMessage = "FUGITIVE! Killed a cop!"; _eventMessageTimer = 180; } CollisionSystem.SpawnCargoDrops(bestTarget, _bubbleManager); bestTarget.IsActive = false; if (string.IsNullOrEmpty(_lastEventMessage) || _eventMessageTimer <= 0) { _lastEventMessage = $"{bestTarget.Blueprint.Name} destroyed!"; _eventMessageTimer = 120; } } }
    }

    private void SaveGame() { if (SaveGameManager.SaveDefault(_gameInstance)) { _lastSaveMessage = "GAME SAVED"; _saveMessageTimer = 120; } else { _lastSaveMessage = "SAVE FAILED"; _saveMessageTimer = 180; } }
    private void OnEntityEvent(object? sender, EntityEventArgs e) { _lastEventMessage = e.Reason switch { "lifetime expired" => $"{e.EntityName} disappeared", "out of bounds" => $"{e.EntityName} left sector", _ => $"{e.EntityName} detected" }; _eventMessageTimer = 120; }
    private void OnCollision(object? sender, CollisionEventArgs e) { _lastEventMessage = $"COLLISION with {e.OtherShipName}!"; _eventMessageTimer = 120; }

    public override void UnloadContent()
    {
        if (_bubbleManager != null) { _bubbleManager.EntityEvent -= OnEntityEvent; _bubbleManager.CollisionEvent -= OnCollision; }
        _whitePixel?.Dispose();
    }

    private static void EnforceOverflyDistance(ShipInstance? body, float bodyRadius) { if (body == null) return; Vector3 pos = body.Position; float dist = pos.Length(); if (dist < 1f) return; float noseDot = pos.Z / dist, angleFromNose = MathF.Acos(MathHelper.Clamp(noseDot, -1f, 1f)), sinAngle = MathF.Sin(angleFromNose); float minDist = bodyRadius + bodyRadius * 0.4f * sinAngle; if (dist < minDist) body.Position = (pos / dist) * minDist; }
}
