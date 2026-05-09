using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EliteRetro.Core.Entities;
using EliteRetro.Core.Rendering;
using EliteRetro.Core.Managers;
using EliteRetro.Core.Systems;
using EliteRetro.Core.HUD;

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
    private ExplosionRenderer _explosionRenderer = null!;
    private readonly List<ExplosionRenderer.ExplosionCloud> _explosions = new();
    private HudRenderer _hudRenderer = null!;
    private ScannerRenderer _scannerRenderer = null!;
    private BitmapFont _font = null!;
    private GraphicsDevice? _graphicsDevice;
    private GameInstance _gameInstance = null!;
    private LocalBubbleManager _bubbleManager = null!;
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
    private KeyboardState _prevKb;
    private float _cameraDistance = 80f;
    private float _playerSpeed;
    private float _cumulativeRoll; // accumulated roll angle in radians, for planet/ring counter-rotation
    private int _spawnCounter; // frame counter for random ship spawning
    private int _tidyCounter; // frame counter for TIDY orthonormalization
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
    private bool _targetPracticeMode; // when true, spawn stationary target ship ahead
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

    public FlightScene(Game? game = null, GalaxySeed? systemSeed = null)
    {
        if (game is GameInstance gi)
        {
            _gameInstance = gi;
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
        _explosionRenderer = new ExplosionRenderer(_graphicsDevice);
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

        // Elite feels wrong if you start at absolute zero speed: you can rotate forever and
        // distances won't change (looks like "moving in a circle"). Start with a small cruise.
        if (_playerSpeed <= 0f)
            _playerSpeed = 12f;
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
            if (_bubbleManager.PlayerShip != null)
            {
                _lastPlayerHull = _bubbleManager.PlayerShip.Hull;
                _lastPlayerEnergy = _bubbleManager.PlayerShip.Energy;
            }
            _initialized = true;
        }
    }

    /// <summary>
    /// Initialize the local bubble with planet, sun, and player at origin.
    /// </summary>
    private void InitializeBubble()
    {
        _bubbleManager.Clear();

        // BBC SOLAR-style derived placement: uses system seed + FIST bit0.
        int fistBit0 = _bubbleManager.Commander?.LegalStatus is byte ls ? (ls & 1) : 0;
        var (planetPos, sunPos) = ComputeSolarSpawn(_systemSeed, fistBit0);

        // Slot 0: Planet
        var planetModel = PlanetModel.Create(GameConstants.PlanetRadius);
        var planetBlueprint = new ShipBlueprint
        {
            Name = "Planet",
            Model = planetModel,
            MaxSpeed = 0,
            MaxEnergy = 255,
            HullStrength = 255,
            ShieldStrength = 255
        };
        var planet = new ShipInstance(planetBlueprint)
        {
            Position = planetPos,
            Speed = 0
        };
        _bubbleManager.SetSlot(GameConstants.PlanetSlot, planet);

        // Slot 1: Sun (behind player)
        var sunModel = SunModel.Create(GameConstants.PlanetRadius * 6);
        var sunBlueprint = new ShipBlueprint
        {
            Name = "Sun",
            Model = sunModel,
            MaxSpeed = 0,
            MaxEnergy = 255,
            HullStrength = 255,
            ShieldStrength = 255
        };
        var sun = new ShipInstance(sunBlueprint)
        {
            Position = sunPos,
            Speed = 0
        };
        _bubbleManager.SetSlot(GameConstants.SunStationSlot, sun);
    }

    /// <summary>
    /// Compute BBC Elite SOLAR spawn positions for planet (ahead) and sun (behind).
    /// Positions are returned in Elite-world local-bubble coordinates (player at origin).
    ///
    /// Coordinate convention in this project:
    /// - Positive Z: ahead of player (front view)
    /// - Negative Z: behind player
    /// </summary>
    private static (Vector3 planetPos, Vector3 sunPos) ComputeSolarSpawn(GalaxySeed seed, int fistBit0)
    {
        // Planet distance hi-byte (planetZHi): ((s0_hi & 0b111) + 6 + fistBit0) >> 1
        // BBC example for Lave: z = (5 0 0) (ahead).
        int planetZHi = ((seed.W0Hi & 0x07) + 6 + (fistBit0 & 1)) >> 1;

        // Planet x/y offsets are small and depend on fistBit0 in the BBC examples.
        // Example from docs: fistBit0=0 => x=y=-(2 0 0); fistBit0=1 => x=y=(3 0 0)
        int planetXYHi = (fistBit0 & 1) == 0 ? -2 : 3;

        Vector3 planetPos = new Vector3(
            planetXYHi << 16,
            planetXYHi << 16,
            +(planetZHi << 16));

        // Sun is always behind. In BBC: z_sign = (s1_hi & 0b111) | 0b10000001 -> negative.
        // That yields a magnitude in [1..7] (as -(1..7) 0 0).
        int sunZHi = (seed.W1Hi & 0x07) + 1;

        // Sun x/y offset is small (0..2-ish). Derive from low bits of s1_lo.
        int sunOff = seed.W1Lo & 0x03;
        if (sunOff > 2) sunOff = 2;

        Vector3 sunPos = new Vector3(
            sunOff << 16,
            sunOff << 16,
            -(sunZHi << 16));

        return (planetPos, sunPos);
    }

    public override void Update(GameTime gameTime)
    {
        _lastGameTime = gameTime;
        var kb = Keyboard.GetState();
        _lastControl = _flightControlService.Update(gameTime);

        // Handle laser fire
        if (_laserCooldown > 0) _laserCooldown--;
        if (_laserFlashTimer > 0) _laserFlashTimer--;

        _isFiring = _lastControl.FireLaser;
        if (_isFiring && _laserCooldown <= 0)
        {
            _gameInstance?.Audio.PlayLaser();
            FireLaserAtTarget();
            _laserCooldown = 15; // 4 shots per second
            _laserFlashTimer = 6; // beam visible for 6 frames (~100ms)
        }

        if (!_lastControl.IsPaused)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _lastDt = dt;

            // If we've hit the planet, freeze motion/rotation but still allow Escape.
            if (_planetHit)
            {
                _lastMoveStep = 0f;
                if (kb.IsKeyDown(Keys.Escape) && _prevKb.IsKeyUp(Keys.Escape))
                {
                    if (_gameInstance != null)
                    {
                        _gameInstance.ChangeScene(new MainMenuScene(_gameInstance));
                        _prevKb = kb;
                        return;
                    }
                }
                _prevKb = kb;
                return;
            }

            // 1. ROTATE UNIVERSE (Minsky algorythm)
            // Roll and Pitch are applied to ALL entities in the universe.
            // ShipInstance.ApplyUniverseRotation uses authentic Elite MVS4 logic.
            // Signs are set for aircraft-style control (UP = Dive, planet goes UP):
            // Positive Roll (Right) -> rotate universe LEFT (negative rollDelta).
            // Positive Pitch (Up/Climb) -> rotate universe DOWN (negative pitchDelta).
            float rollDelta = Math.Clamp(_lastControl.RollAngle * dt * 60f, -0.1f, 0.1f);
            float pitchDelta = Math.Clamp(_lastControl.PitchAngle * dt * 60f, -0.1f, 0.1f);
            _bubbleManager.ApplyUniverseRotation(-rollDelta, -pitchDelta);

            // Track cumulative roll for planet/ring counter-rotation
            _cumulativeRoll += rollDelta;

            // 2. MOVE UNIVERSE (Forward move = objects move toward player)
            // In this coordinate system, forward motion makes objects move closer along -Z.
            float moveStep = _playerSpeed * dt * 60f;
            _lastMoveStep = moveStep;
            foreach (var entity in _bubbleManager.GetAllActive())
            {
                // Skip player - player does not move
                if (entity.SlotIndex == GameConstants.PlayerSlot) continue;

                if (_playerSpeed != 0)
                {
                    entity.Position.Z -= moveStep;
                }
                // Entity's own relative movement
                if (entity.Speed != 0)
                    entity.MoveForward();
            }

            // Update stardust - rotates and moves with the same logic
            _stardustRenderer.Update(_playerSpeed, -rollDelta, -pitchDelta);

            // Periodic TIDY orthonormalization to correct Minsky drift
            // Tidy ALL entities EVERY frame for absolute stability of orientations
            _bubbleManager.TidyAllActive();
            CheckExplosions();

            // Check player collision against nearby entities (every frame, O(n) not O(n²))
            CollisionSystem.CheckPlayerCollisions(_bubbleManager);

            // Planet crash check for player (prevents "flying through" the planet)
            var player = _bubbleManager.PlayerShip;
            var planet = _bubbleManager.Planet;
            if (player != null && planet != null && CollisionSystem.CheckPlanetCrash(player, planet))
            {
                _lastEventMessage = "PLANET HIT";
                _eventMessageTimer = int.MaxValue; // constant message
                _playerSpeed = 0f;
                _planetHit = true;
                _lastMoveStep = 0f;
                _prevKb = kb;
                return;
            }

            // Cleanup expired entities (lifetime or out of bounds)
            _bubbleManager.CleanupExpired();

            // Slow planet rotation
            if (_planetRotationCounter++ % 32 == 0)
                _planetRotation = (_planetRotation + 1) % 64;

            // Random ship/asteroid spawning (about 1 per 3 seconds at 60fps)
            // Skip spawning in target practice mode
            if (!_targetPracticeMode)
            {
                _spawnCounter++;
                if (_spawnCounter % 180 == 0 && _rng.NextDouble() < 0.3)
                    SpawnRandomEntity();
            }

            // View switching from control service
            _viewMode = _lastControl.ViewIndex;

            // Zoom with +/-
            float speed = 2f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (kb.IsKeyDown(Keys.OemPlus) || kb.IsKeyDown(Keys.Add)) _cameraDistance -= speed;
            if (kb.IsKeyDown(Keys.OemMinus) || kb.IsKeyDown(Keys.Subtract)) _cameraDistance += speed;
            _cameraDistance = MathHelper.Clamp(_cameraDistance, 2f, 20f);

            // Speed control via W/S keys (SpeedDelta is in units/sec)
            if (_lastControl.SpeedDelta != 0)
                _playerSpeed = Math.Clamp(_playerSpeed + _lastControl.SpeedDelta * (float)gameTime.ElapsedGameTime.TotalSeconds, 0f, GameConstants.SpeedMax);

            // Check for player damage (shield or hull decrease)
            if (_bubbleManager.PlayerShip != null)
            {
                byte currentHull = _bubbleManager.PlayerShip.Hull;
                byte currentEnergy = _bubbleManager.PlayerShip.Energy;
                if (currentHull < _lastPlayerHull || currentEnergy < _lastPlayerEnergy)
                {
                    _damageFlashTimer = 15; // 15 frames = 250ms red flash
                }
                _lastPlayerHull = currentHull;
                _lastPlayerEnergy = currentEnergy;
            }
            if (_damageFlashTimer > 0)
                _damageFlashTimer--;

            // Check sun proximity effects
            var sunEffect = _bubbleManager.CheckSunProximity();
            // TODO: Apply heat damage, fuel scooping, etc. based on sunEffect

            // Toggle local target practice practice mode with L
            if (kb.IsKeyDown(Keys.L) && _prevKb.IsKeyUp(Keys.L))
            {
                _targetPracticeMode = !_targetPracticeMode;
                _bubbleManager.TargetPracticeMode = _targetPracticeMode;
                if (_targetPracticeMode)
                    SpawnTargetPracticeShip();
                else
                    ClearTargetPracticeShip();
            }
        }

        // Toggle local pause on P (when not already paused by flight control)
        if (kb.IsKeyDown(Keys.P) && _prevKb.IsKeyUp(Keys.P) && !_lastControl.IsPaused)
            _paused = !_paused;

        // Save game with F5
        if (kb.IsKeyDown(Keys.F5) && _prevKb.IsKeyUp(Keys.F5))
        {
            SaveGame();
        }

        // Return to menu with Escape
        if (kb.IsKeyDown(Keys.Escape) && _prevKb.IsKeyUp(Keys.Escape))
        {
            if (_gameInstance != null)
            {
                _gameInstance.ChangeScene(new MainMenuScene(_gameInstance));
                return;
            }
        }

        // Toggle ram mode with R
        if (kb.IsKeyDown(Keys.R) && _prevKb.IsKeyUp(Keys.R))
            _ramMode = !_ramMode;

        // Toggle hidden edges with I
        if (kb.IsKeyDown(Keys.I) && _prevKb.IsKeyUp(Keys.I))
        {
            _showHiddenEdges = !_showHiddenEdges;
            if (_gameInstance != null)
            {
                _gameInstance.DrawInvisible = _showHiddenEdges;
                Systems.OptionsManager.Save(_gameInstance.DrawWhite, _gameInstance.DrawInvisible);
            }
        }

        // FIXED VIEW DIRECTIONS for Rotating Universe model.
        // Camera is fixed at origin looking at these axes.
        _cameraLookDir = -Vector3.UnitZ; // default front
        Vector3 forwardBasis = Vector3.UnitZ; // camera Z-basis (backwards in RH)
        Vector3 sideBasis = Vector3.UnitX;
        Vector3 upBasis = Vector3.UnitY;

        switch (_viewMode)
        {
            case 0: // Front: look at -Z
                _cameraLookDir = -Vector3.UnitZ;
                forwardBasis = Vector3.UnitZ;
                sideBasis = Vector3.UnitX;
                break;
            case 1: // Rear: look at +Z
                _cameraLookDir = Vector3.UnitZ;
                forwardBasis = -Vector3.UnitZ;
                sideBasis = -Vector3.UnitX;
                break;
            case 2: // Left: look at -X
                _cameraLookDir = -Vector3.UnitX;
                forwardBasis = Vector3.UnitX;
                sideBasis = Vector3.UnitZ;
                break;
            case 3: // Right: look at +X
                _cameraLookDir = Vector3.UnitX;
                forwardBasis = -Vector3.UnitX;
                sideBasis = -Vector3.UnitZ;
                break;
        }

        // Fixed View matrix basis vectors in COLUMNS for MonoGame v * M convention.
        // Column 0 = Sidev (X_cam), Column 1 = Roofv (Y_cam), Column 2 = forwardBasis (Z_cam)
        _view = new Matrix(
            sideBasis.X, upBasis.X, forwardBasis.X, 0,
            sideBasis.Y, upBasis.Y, forwardBasis.Y, 0,
            sideBasis.Z, upBasis.Z, forwardBasis.Z, 0,
            0, 0, 0, 1);

        _prevKb = kb;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        EnsureInitialized();

        if (_graphicsDevice != null)
            _graphicsDevice.Clear(Color.Black);

        if (!_initialized)
            return;

        EnsureProjectionMatchesViewport();

        int screenW = _graphicsDevice?.Viewport.Width ?? 1024;
        int screenH = _graphicsDevice?.Viewport.Height ?? 768;
        int hudH = (int)MathF.Round(screenH * HudHeightFraction);
        int viewH = Math.Max(1, screenH - hudH);

        var screenRect = new Rectangle(0, 0, screenW, screenH);
        var viewRect = new Rectangle(0, 0, screenW, viewH);
        var hudRect = new Rectangle(0, viewH, screenW, hudH);
        var screenCenter = new Vector2(viewRect.X + viewRect.Width / 2f, viewRect.Y + viewRect.Height / 2f);

        int outerMargin = GetOuterMarginPixels(screenW, screenH);
        var viewContentRect = InsetRect(viewRect, outerMargin);
        var hudContentRect = InsetRect(hudRect, outerMargin);

        // --- Main 3D view pass (clipped to view content rect) ---
        BeginScissored(spriteBatch, viewContentRect);
        _graphicsDevice!.ScissorRectangle = viewContentRect;

        // Draw stardust (starfield)
        _stardustRenderer.Draw(spriteBatch, screenCenter, 500f, _view, _gameInstance?.DrawWhite ?? false);

        // Draw explosions (world-anchored, projected each frame)
        foreach (var cloud in _explosions)
        {
            if (!IsInFrontOfCamera(cloud.WorldPosElite))
                continue;

            cloud.Center = ProjectToScreenElite(cloud.WorldPosElite);
            cloud.Distance = Math.Max(ToMonoGameWorld(cloud.WorldPosElite).Length(), 0.5f);
            _explosionRenderer.UpdateAndDraw(spriteBatch, cloud, _lastGameTime, _gameInstance?.DrawWhite ?? false);
        }

        // Render bubble entities (skip planet and sun - rendered separately)
        foreach (var entity in _bubbleManager.GetAllActive())
        {
            // Skip player ship - do not render self in cockpit view
            if (entity.SlotIndex == GameConstants.PlayerSlot) continue;

            if (entity.Blueprint?.Model != null &&
                entity.Blueprint.Name != "Planet" &&
                entity.Blueprint.Name != "Sun" &&
                IsInFrontOfCamera(entity.Position))
            {
                // In XNA (V * M convention), a World Matrix (Model-to-World) should have basis vectors in ROWS.
                Matrix entityOrientation = new Matrix(
                    entity.Orientation.Sidev.X, entity.Orientation.Sidev.Y, entity.Orientation.Sidev.Z, 0,
                    entity.Orientation.Roofv.X, entity.Orientation.Roofv.Y, entity.Orientation.Roofv.Z, 0,
                    entity.Orientation.Nosev.X, entity.Orientation.Nosev.Y, entity.Orientation.Nosev.Z, 0,
                    0, 0, 0, 1);

                // Map Elite (X, Y, Z_depth) to MonoGame (X, Y, -Z_depth)
                Vector3 entityPosMG = new Vector3(entity.Position.X, entity.Position.Y, -entity.Position.Z) * RenderScale;

                Matrix entityWorld =
                    Matrix.CreateScale(RenderScale) *
                    entityOrientation *
                    Matrix.CreateTranslation(entityPosMG);
                _wireframeRenderer.Draw(entity.Blueprint.Model, entityWorld, _view, _projection, spriteBatch, drawHiddenEdges: _showHiddenEdges, drawWhite: _gameInstance?.DrawWhite ?? false);
            }
        }

        // Draw planet/sun using simple 2D discs, but projected from true 3D positions
        // (so they still obey 3D rules: culling + distance scaling + screen shift).
        // Fix: use view-space depth and explicit eclipse check so the sun cannot appear
        // "inside" the planet disc when it is physically behind it.
        var planetEntity = _bubbleManager.Planet;
        var sunEntity = (_bubbleManager.SunOrStation?.Blueprint?.Name == "Sun") ? _bubbleManager.SunOrStation : null;

        CelestialDisc? planetDisc = planetEntity != null ? ComputeCelestialDisc(planetEntity.Position, GameConstants.PlanetRadius) : null;
        CelestialDisc? sunDisc = sunEntity != null ? ComputeCelestialDisc(sunEntity.Position, GameConstants.PlanetRadius * 6) : null;

        if (planetDisc.HasValue || sunDisc.HasValue)
        {
            // Eclipse: if sun is farther than planet and projects inside the planet disc, it's occluded.
            // Note: in view space (MonoGame RH), objects in front have Z < 0.
            // More negative Z => farther away. Less negative Z => closer.
            if (planetDisc.HasValue && sunDisc.HasValue)
            {
                var p = planetDisc.Value;
                var s = sunDisc.Value;
                if (s.ViewZ < p.ViewZ) // sun farther than planet
                {
                    float d = Vector2.Distance(p.ScreenCenter, s.ScreenCenter);
                    if (d < p.ScreenRadius - 0.5f)
                        sunDisc = null;
                }
            }

            // Painter's algorithm using view-space Z (more negative = farther; less negative = closer)
            if (sunDisc.HasValue && planetDisc.HasValue)
            {
                var p = planetDisc.Value;
                var s = sunDisc.Value;
                if (s.ViewZ < p.ViewZ)
                {
                    // sun farther than planet: draw sun first, then planet on top
                    DrawCelestialSun(spriteBatch, s.WorldPosElite, GameConstants.PlanetRadius * 6, Color.White);
                    DrawCelestialPlanet(spriteBatch, p.WorldPosElite, GameConstants.PlanetRadius, Color.White);
                }
                else
                {
                    // planet farther than sun: draw planet first, then sun on top
                    DrawCelestialPlanet(spriteBatch, p.WorldPosElite, GameConstants.PlanetRadius, Color.White);
                    DrawCelestialSun(spriteBatch, s.WorldPosElite, GameConstants.PlanetRadius * 6, Color.White);
                }
            }
            else
            {
                if (sunDisc.HasValue)
                    DrawCelestialSun(spriteBatch, sunDisc.Value.WorldPosElite, GameConstants.PlanetRadius * 6, Color.White);
                if (planetDisc.HasValue)
                    DrawCelestialPlanet(spriteBatch, planetDisc.Value.WorldPosElite, GameConstants.PlanetRadius, Color.White);
            }
        }

        // Damage flash overlay (only inside view frame)
        if (_damageFlashTimer > 0)
        {
            float intensity = _damageFlashTimer / 15f;
            byte alpha = (byte)(intensity * 128);
            Color flashColor = new Color(255, 0, 0) { A = alpha };
            spriteBatch.Draw(_whitePixel, viewContentRect, flashColor);
        }

        // Crosshair at center of space view (BBC Elite targeting reticle)
        int cx = (int)screenCenter.X;
        int cy = (int)screenCenter.Y;
        const int inner = 16;
        const int outer = 48;
        var crossColor = Color.White;

        // Central tiny dot
        spriteBatch.Draw(_whitePixel, new Rectangle(cx - 1, cy - 1, 2, 2), crossColor);

        // Orthogonal segments
        spriteBatch.Draw(_whitePixel, new Rectangle(cx - 1, cy - outer, 2, outer - inner), crossColor); // Top
        spriteBatch.Draw(_whitePixel, new Rectangle(cx - 1, cy + inner, 2, outer - inner), crossColor); // Bottom
        spriteBatch.Draw(_whitePixel, new Rectangle(cx - outer, cy - 1, outer - inner, 2), crossColor); // Left
        spriteBatch.Draw(_whitePixel, new Rectangle(cx + inner, cy - 1, outer - inner, 2), crossColor); // Right

        // Draw lasers when firing
        if (_laserFlashTimer > 0)
        {
            DrawLine(spriteBatch, new Vector2(viewRect.Left, viewRect.Bottom), new Vector2(cx, cy), Color.Yellow, 2);
            DrawLine(spriteBatch, new Vector2(viewRect.Right, viewRect.Bottom), new Vector2(cx, cy), Color.Yellow, 2);
        }

        // Overlay messages (kept inside view frame)
        DrawViewOverlay(spriteBatch, viewContentRect);

        spriteBatch.End();

        // --- HUD pass (clipped to HUD content rect) ---
        BeginScissored(spriteBatch, hudContentRect);
        _graphicsDevice!.ScissorRectangle = hudContentRect;

        DrawHUD(spriteBatch, viewRect, hudContentRect, screenRect);

        spriteBatch.End();

        // --- Frames pass (unclipped) ---
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        // Target practice mode indicator and health status (kept in the view area, above the frame)
        if (_targetPracticeMode)
        {
            var sz = _font.MeasureString("TARGET PRACTICE");
            _font.DrawString(spriteBatch, "TARGET PRACTICE",
                new Vector2(screenRect.Width / 2f - sz.X / 2, 10), new Color(255, 200, 50), 1.0f);

            var targetShip = _bubbleManager.GetAllActive().FirstOrDefault(e => e.IsTargetPractice);
            if (targetShip != null)
            {
                string status = $"S: {targetShip.Energy} H: {targetShip.Hull}";
                var statSz = _font.MeasureString(status);
                _font.DrawString(spriteBatch, status,
                    new Vector2(screenRect.Width / 2f - statSz.X / 2, 40), Color.White, 0.8f);
            }
        }

        // ZX-style white frames around view + HUD (true bounds for content)
        DrawFrame(spriteBatch, viewContentRect, Color.White, 1);
        DrawFrame(spriteBatch, hudContentRect, Color.White, 1);

        spriteBatch.End();
    }

    /// <summary>
    /// Spawn a random ship or asteroid moving in the local bubble.
    /// Some fly toward the player, others toward the planet.
    /// </summary>
    private void SpawnRandomEntity()
    {
        // Pick a random model (ships and asteroids, not stations)
        var models = new (string Name, Func<float, ShipModel> Create)[]
        {
            ("Sidewinder", size => SidewinderModel.Create(size)),
            ("Viper", size => ViperModel.Create(size)),
            ("Cobra Mk3", size => CobraMk3Model.Create(size)),
            ("Python", size => PythonModel.Create(size)),
            ("Asteroid", size => AsteroidModel.Create(size)),
            ("Boulder", size => BoulderModel.Create(size)),
            ("Rock Hermit", size => RockHermitModel.Create(size)),
        };

        var chosen = models[_rng.Next(models.Length)];
        float size = 16f + (float)_rng.NextDouble() * 16f; // 16 to 32 (very large, easily visible)
        var model = chosen.Create(size);

        // Spawn closer: 100-300 units away (visible size)
        float distance = 100 + (float)_rng.NextDouble() * 200;
        float lateralX = (float)_rng.NextDouble() * 150 - 75; // -75 to +75
        float lateralY = (float)_rng.NextDouble() * 150 - 75; // -75 to +75

        var blueprint = new ShipBlueprint
        {
            Name = chosen.Name,
            Model = model,
            MaxSpeed = model.IsRock ? 0f : 255f,
            MaxEnergy = model.IsRock ? (byte)0 : (byte)255,
            HullStrength = model.IsRock ? (byte)1 : (byte)255,
            ShieldStrength = model.IsRock ? (byte)0 : (byte)255,
            IsRock = model.IsRock
        };

        // 50% chance: fly toward player (spawn ahead, move toward origin)
        // 50% chance: fly toward planet (spawn near player, move away toward planet)
        // In ram mode (R toggle): always fly toward player at high speed
        bool towardPlayer = _ramMode || _rng.NextDouble() < 0.5;
        float speed = _ramMode ? (5f + (float)_rng.NextDouble() * 5f) : (1f + (float)_rng.NextDouble() * 2f); // ram: 5-10, normal: 1-3

        Vector3 position;
        float entitySpeed;
        OrientationMatrix orientation = OrientationMatrix.Identity;

        if (towardPlayer)
        {
            // Spawn ahead (positive Z), fly toward player (negative Z direction)
            position = new Vector3(lateralX, lateralY, distance);
            entitySpeed = -speed;

            // In ram mode, aim directly at player position
            if (_ramMode)
            {
                Vector3 toPlayer = -position;
                if (toPlayer.LengthSquared() > 0.01f)
                {
                    toPlayer = Vector3.Normalize(toPlayer);
                    var newRoofv = Vector3.Normalize(Vector3.Cross(toPlayer, Vector3.UnitY));
                    orientation = new OrientationMatrix
                    {
                        Nosev = toPlayer,
                        Roofv = newRoofv,
                        Sidev = Vector3.Normalize(Vector3.Cross(newRoofv, toPlayer))
                    };
                }
            }
            else
            {
                // Non-ram: nose points toward +Z (toward player)
                orientation = new OrientationMatrix
                {
                    Nosev = new Vector3(0, 0, 1),
                    Roofv = new Vector3(0, 1, 0),
                    Sidev = new Vector3(1, 0, 0)
                };
            }
        }
        else
        {
            // Spawn between player and planet, fly away from player toward planet (positive Z)
            position = new Vector3(lateralX, lateralY, distance * 0.3f);
            entitySpeed = speed;
            // Nose points toward +Z (toward planet)
            orientation = new OrientationMatrix
            {
                Nosev = new Vector3(0, 0, 1),
                Roofv = new Vector3(0, 1, 0),
                Sidev = new Vector3(1, 0, 0)
            };
        }

        var entity = new ShipInstance(blueprint)
        {
            Position = position,
            Speed = entitySpeed,
            Orientation = orientation
        };

        // For ships flying toward player (non-ram mode), nose points toward +Z (toward player)
        // so they fly nose-first — already handled above, this block removed

        _bubbleManager.TrySpawn(entity);

        // Show spawn event on HUD for 2 seconds (120 frames)
        string direction = towardPlayer ? "approaching" : "departing";
        _lastEventMessage = $"{chosen.Name} {direction}";
        _eventMessageTimer = 120;
    }

    /// <summary>
    /// Spawn a stationary Viper directly ahead for target practice.
    /// Clears all entities except player, planet, and sun/station for a clean test range.
    /// </summary>
    private void SpawnTargetPracticeShip()
    {
        // Save planet and sun/station before clearing
        var planet = _bubbleManager.Planet;
        var sunStation = _bubbleManager.GetSlot(GameConstants.SunStationSlot);

        // Clear ships/entities
        _bubbleManager.Clear();

        // Restore planet
        if (planet != null)
        {
            planet.IsActive = true;
            _bubbleManager.SetSlot(GameConstants.PlanetSlot, planet);
        }

        // Restore sun/station
        if (sunStation != null)
        {
            sunStation.IsActive = true;
            _bubbleManager.SetSlot(GameConstants.SunStationSlot, sunStation);
        }

        var model = ViperModel.Create(24f);
        var blueprint = new ShipBlueprint
        {
            Name = "Target Viper",
            Model = model,
            MaxSpeed = 0,
            MaxEnergy = 255,
            HullStrength = 255,
            ShieldStrength = 255
        };

        var target = new ShipInstance(blueprint)
        {
            Position = new Vector3(0, 0, 400), // 400 units ahead along +Z
            Speed = 0,
            Orientation = OrientationMatrix.Identity,
            IsTargetPractice = true
        };

        // Add cargo for drop testing
        target.AddCargo(0, 3); // Food
        target.AddCargo(6, 2); // Metals

        _bubbleManager.TrySpawn(target);
        _lastEventMessage = "Target practice: L to toggle off";
        _eventMessageTimer = 120;
    }

    /// <summary>
    /// Remove the target practice ship from the bubble.
    /// </summary>
    private void ClearTargetPracticeShip()
    {
        foreach (var entity in _bubbleManager.GetAllActive())
        {
            if (entity.IsTargetPractice)
            {
                entity.IsActive = false;
                break;
            }
        }
    }

    private void DrawHUD(SpriteBatch spriteBatch, Rectangle viewRect, Rectangle hudRect, Rectangle screenRect)
    {
        // Build HUD state from current game data
        var sunEffect = _bubbleManager.CheckSunProximity();
        string statusMsg = "";
        Color statusColor = Color.Gray;

        if (sunEffect == LocalBubbleManager.SunProximityEffect.Fatal)
        {
            statusMsg = "DANGER - FATAL PROXIMITY";
            statusColor = Color.Red;
        }
        else if (sunEffect == LocalBubbleManager.SunProximityEffect.FuelScoop)
        {
            statusMsg = "FUEL SCOOP ACTIVE";
            statusColor = Color.Green;
        }
        else if (sunEffect == LocalBubbleManager.SunProximityEffect.HeatWarning)
        {
            statusMsg = "HEAT WARNING";
            statusColor = Color.Orange;
        }

        if (_bubbleManager.SunOrStation?.Blueprint?.Name == "Coriolis Station")
        {
            statusMsg = "STATION IN VIEW";
            statusColor = Color.Yellow;
        }

        var hudState = new HUDState
        {
            Speed = _playerSpeed,
            Energy = _bubbleManager.PlayerShip?.Energy ?? 200,
            MaxEnergy = 255,
            Fuel = _bubbleManager.Commander.Fuel,
            CabinTemp = 0,
            LaserTemp = 0,
            Altitude = (int)(_bubbleManager.Planet?.Position.Length() ?? 0),
            EnergyBanks = 0,
            Missiles = _bubbleManager.PlayerMissiles,
            MaxMissiles = 4,
            ShieldForward = (byte)(_bubbleManager.PlayerShip?.Energy ?? 200),
            ShieldAft = (byte)(_bubbleManager.PlayerShip?.Energy ?? 200),
            Pitch = _lastControl.PitchAngle / GameConstants.PitchMax, // Normalized rate -1 to 1
            Roll = _lastControl.RollAngle / GameConstants.RollMax,   // Normalized rate -1 to 1
            CompassHeading = _cumulativeRoll,
            ECMBulbs = 0,
            ViewMode = _viewMode switch
            {
                0 => "FRONT",
                1 => "REAR",
                2 => "LEFT",
                3 => "RIGHT",
                _ => "FRONT"
            },
            StatusMessage = statusMsg,
            StatusColor = statusColor,
            LegalStatus = _bubbleManager.Commander.LegalStatus,
            CombatRank = _bubbleManager.Commander.RankName,
            ShowHiddenEdges = _showHiddenEdges
        };

        // Draw dashboard
        _hudRenderer.Draw(spriteBatch, hudState, _font, hudRect, screenRect);

        // Scanner display
        int leftW = (int)MathF.Round(hudRect.Width * 0.25f);
        int rightW = leftW;
        int centerW = hudRect.Width - leftW - rightW;
        var centerPanelRect = new Rectangle(hudRect.X + leftW, hudRect.Y, centerW, hudRect.Height);
        _scannerRenderer.Draw(spriteBatch, _bubbleManager, GameConstants.PlayerSlot, _universeOrientation, centerPanelRect);

        // Entity event messages (spawn/despawn)
        if (_eventMessageTimer > 0 && !string.IsNullOrEmpty(_lastEventMessage))
        {
            bool isMilestone = _lastEventMessage == "RIGHT ON COMMANDER!";
            float scale = isMilestone ? 2.0f : 1.2f;
            Color msgColor = isMilestone ? Color.Gold : Color.Yellow;
            var msgSize = _font.MeasureString(_lastEventMessage);
            float msgX = isMilestone ? (screenRect.Width - msgSize.X) / 2 : EventMsgX;
            float msgY = isMilestone ? MilestoneMsgY : EventMsgY;
            _font.DrawString(spriteBatch, $">> {_lastEventMessage}", new Vector2(msgX, msgY), msgColor, scale);
            _eventMessageTimer--;
        }

        // Save confirmation message
        if (_saveMessageTimer > 0 && !string.IsNullOrEmpty(_lastSaveMessage))
        {
            _font.DrawString(spriteBatch, _lastSaveMessage, new Vector2(400, 380), Color.Green, 1.4f);
            _saveMessageTimer--;
        }

        if (_paused)
            _font.DrawString(spriteBatch, "PAUSED", new Vector2(400, 350), Color.Red, 2f);
    }

    private void DrawViewOverlay(SpriteBatch spriteBatch, Rectangle viewContentRect)
    {
        float x = viewContentRect.X + 10;
        float y = viewContentRect.Y + 10;

        // View mode (top-left)
        string viewMode = _viewMode switch
        {
            0 => "FRONT",
            1 => "REAR",
            2 => "LEFT",
            3 => "RIGHT",
            _ => "FRONT"
        };
        _font.DrawString(spriteBatch, viewMode, new Vector2(x, y), new Color(255, 180, 50), 1.5f);

        // Status message (top-center)
        var sunEffect = _bubbleManager.CheckSunProximity();
        string statusMsg = "";
        Color statusColor = Color.Gray;

        if (sunEffect == LocalBubbleManager.SunProximityEffect.Fatal)
        {
            statusMsg = "DANGER - FATAL PROXIMITY";
            statusColor = Color.Red;
        }
        else if (sunEffect == LocalBubbleManager.SunProximityEffect.FuelScoop)
        {
            statusMsg = "FUEL SCOOP ACTIVE";
            statusColor = Color.Green;
        }
        else if (sunEffect == LocalBubbleManager.SunProximityEffect.HeatWarning)
        {
            statusMsg = "HEAT WARNING";
            statusColor = Color.Orange;
        }
        if (_bubbleManager.SunOrStation?.Blueprint?.Name == "Coriolis Station")
        {
            statusMsg = "STATION IN VIEW";
            statusColor = Color.Yellow;
        }
        if (!string.IsNullOrEmpty(statusMsg))
        {
            var size = _font.MeasureString(statusMsg);
            _font.DrawString(spriteBatch, statusMsg,
                new Vector2(viewContentRect.X + (viewContentRect.Width - size.X) / 2f, y + 5),
                statusColor, 1.2f);
        }

        // Legal + rank (top-right)
        string legalText = _bubbleManager.Commander.LegalStatus switch
        {
            0 => "CLEAN",
            < 50 => "OFFENDER",
            _ => "FUGITIVE"
        };
        var legalSz = _font.MeasureString(legalText);
        _font.DrawString(spriteBatch, legalText,
            new Vector2(viewContentRect.Right - legalSz.X - 10, y),
            _bubbleManager.Commander.LegalStatus >= 50 ? Color.OrangeRed : Color.Lime, 1.0f);

        string rankText = _bubbleManager.Commander.RankName;
        if (!string.IsNullOrEmpty(rankText))
        {
            var rankSz = _font.MeasureString(rankText);
            _font.DrawString(spriteBatch, rankText,
                new Vector2(viewContentRect.Right - rankSz.X - 10, y + 22),
                Color.Gold, 1.0f);
        }

        // Distances (left)
        float planetDist = _bubbleManager.Planet?.Position.Length() ?? 0;
        float sunDist = _bubbleManager.SunOrStation?.Position.Length() ?? 0;
        if (_bubbleManager.Planet != null)
        {
            _planetDistDelta = planetDist - _prevPlanetDist;
            _planetZDelta = _bubbleManager.Planet.Position.Z - _prevPlanetZ;
            _prevPlanetDist = planetDist;
            _prevPlanetZ = _bubbleManager.Planet.Position.Z;
        }

        _font.DrawString(spriteBatch, $"PLANET DIST: {planetDist:F4}  (Δ { _planetDistDelta:+0.0000;-0.0000;0.0000})", new Vector2(x, y + 50), Color.Cyan, 1.2f);
        _font.DrawString(spriteBatch, $"SUN DIST: {sunDist:F4}", new Vector2(x, y + 72), Color.Orange, 1.2f);
        if (_bubbleManager.Planet != null)
            _font.DrawString(spriteBatch, $"PLANET Z: {_bubbleManager.Planet.Position.Z:F4}  (Δ { _planetZDelta:+0.0000;-0.0000;0.0000})", new Vector2(x, y + 94), Color.Cyan, 0.9f);
        if (_bubbleManager.SunOrStation != null)
            _font.DrawString(spriteBatch, $"SUN Z: {_bubbleManager.SunOrStation.Position.Z:F0}", new Vector2(x, y + 112), Color.Orange, 0.9f);
        _font.DrawString(spriteBatch, $"SPEED: {_playerSpeed:F4}", new Vector2(x, y + 130), Color.Gray, 0.9f);
        if (_planetHit)
            _font.DrawString(spriteBatch, "PLANET HIT: TRUE", new Vector2(x, y + 148), Color.Red, 0.9f);
        _font.DrawString(spriteBatch, $"PAUSED: {_paused}  CTRL-PAUSE: {_lastControl.IsPaused}", new Vector2(x, y + 154), Color.Gray, 0.8f);
        _font.DrawString(spriteBatch, $"DT: {_lastDt * 1000f:F3}ms  STEP: {_lastMoveStep:F4}", new Vector2(x, y + 172), Color.Gray, 0.8f);

        // Modes (left)
        _font.DrawString(spriteBatch, _showHiddenEdges ? "HIDDEN: ON" : "HIDDEN: OFF", new Vector2(x, y + 190), Color.White, 0.8f);
        Color ramColor = _ramMode ? Color.Red : Color.DarkGray;
        _font.DrawString(spriteBatch, _ramMode ? "RAM MODE: ON (press R to toggle)" : "RAM MODE: OFF (press R to toggle)", new Vector2(x, y + 212), ramColor, 0.8f);

        // Nearby entity distances (left)
        int debugY = (int)(y + 235);
        foreach (var entity in _bubbleManager.GetActiveShips())
        {
            float dist = entity.Position.Length();
            if (dist < 500)
            {
                Color distColor = dist < 50 ? Color.Red : dist < 200 ? Color.Orange : Color.Yellow;
                _font.DrawString(spriteBatch, $"{entity.Blueprint.Name}: {dist:F0}", new Vector2(x, debugY), distColor, 0.8f);
                debugY += 22;
                if (debugY > viewContentRect.Bottom - 24) break;
            }
        }

        // Controls (bottom of view frame)
        string controls = "ARROWS: PITCH/ROLL  W/S: SPEED  V: VIEW  SPACE: FIRE  P: PAUSE  +/-: ZOOM  I: EDGES  F5: SAVE  ESC: MENU";
        _font.DrawString(spriteBatch, controls, new Vector2(viewContentRect.X + 10, viewContentRect.Bottom - 24), Color.Gray, 0.8f);
    }

    private static int GetOuterMarginPixels(int w, int h)
        => Math.Max(2, (int)MathF.Round(MathF.Min(w, h) * 0.008f));

    private static Rectangle InsetRect(Rectangle r, int inset)
    {
        int x = r.X + inset;
        int y = r.Y + inset;
        int w = Math.Max(1, r.Width - inset * 2);
        int h = Math.Max(1, r.Height - inset * 2);
        return new Rectangle(x, y, w, h);
    }

    private void BeginScissored(SpriteBatch spriteBatch, Rectangle scissorRect)
    {
        if (_graphicsDevice == null) return;
        _graphicsDevice.ScissorRectangle = scissorRect;
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, _scissorRasterizer);
    }

    private void DrawFrame(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        if (rect.Width <= 0 || rect.Height <= 0) return;
        thickness = Math.Max(1, thickness);

        // Top
        spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        // Bottom
        spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        // Left
        spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        // Right
        spriteBatch.Draw(_whitePixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    private readonly record struct CelestialDisc(Vector3 WorldPosElite, Vector2 ScreenCenter, float ScreenRadius, float ViewZ);

    private CelestialDisc? ComputeCelestialDisc(Vector3 worldPosElite, float radiusElite)
    {
        if (_graphicsDevice == null) return null;

        // View-space depth (for correct front/back ordering)
        Vector3 worldMg = ToMonoGameWorld(worldPosElite);
        Vector3 viewPos = Vector3.Transform(worldMg, _view);

        // In a standard RH camera, objects in front have negative Z in view space.
        if (viewPos.Z >= -0.001f)
            return null;

        Vector2 screenPos = ProjectToScreenElite(worldPosElite);
        float dist = worldMg.Length();
        if (dist < 0.001f) return null;

        int w = _graphicsDevice.Viewport.Width;
        int h = _graphicsDevice.Viewport.Height;
        int hudH = (int)MathF.Round(h * HudHeightFraction);
        int viewH = Math.Max(1, h - hudH);

        // FOV is 75 deg. tan(75/2) ≈ 0.767
        float screenRadius = ((radiusElite * RenderScale) / dist) * (1.0f / 0.767f) * (viewH / 2f);
        if (screenRadius <= 0 || screenRadius > 4000) return null;

        return new CelestialDisc(worldPosElite, screenPos, screenRadius, viewPos.Z);
    }

    private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, int thickness)
    {
        Vector2 edge = end - start;
        float angle = (float)Math.Atan2(edge.Y, edge.X);
        spriteBatch.Draw(_whitePixel,
            start,
            null,
            color,
            angle,
            new Vector2(0, 0.5f),
            new Vector2(edge.Length(), thickness),
            SpriteEffects.None,
            0);
    }

    private bool IsInFrontOfCamera(Vector3 worldPos)
    {
        // Camera look dir is expressed in MonoGame space; convert worldPos accordingly.
        Vector3 mg = ToMonoGameWorld(worldPos);
        if (mg.LengthSquared() < 0.001f) return true;
        return Vector3.Dot(Vector3.Normalize(mg), _cameraLookDir) > 0;
    }

    private static Vector3 ToMonoGameWorld(Vector3 eliteWorldPos)
        => new Vector3(eliteWorldPos.X, eliteWorldPos.Y, -eliteWorldPos.Z) * RenderScale;

    /// <summary>
    /// Check for inactive entities and spawn explosion effects.
    /// Inactive entities are kept for explosion animation, then cleaned up.
    /// </summary>
    private void CheckExplosions()
    {
        for (int i = GameConstants.FirstAvailableSlot; i < GameConstants.MaxSlots; i++)
        {
            var entity = _bubbleManager.GetSlot(i);
            if (entity != null && !entity.IsActive && !_explosions.Any(e => e.Tag == entity))
            {
                System.Diagnostics.Debug.WriteLine($"[EXPLOSION] Creating explosion for {entity.Blueprint.Name} at {entity.Position}, slot {entity.SlotIndex}");
                _lastEventMessage = $"{entity.Blueprint.Name} destroyed!";
                _eventMessageTimer = 120;

                // Play explosion sound
                _gameInstance?.Audio.PlayExplosion();

                // Create explosion at entity position
                var cloud = _explosionRenderer.CreateExplosion(entity.Blueprint.Model, entity.Position);
                cloud.Tag = entity;
                _explosions.Add(cloud);
            }
        }

        // Update and clean up explosions (with delay after visual completion)
        _explosions.RemoveAll(cloud =>
        {
            // Hard TTL: prevent any lingering artifacts from sticking around indefinitely.
            if (cloud.AgeSeconds >= 3f)
            {
                if (cloud.Tag is ShipInstance taggedTtl)
                    _bubbleManager.Despawn(taggedTtl.SlotIndex, "explosion ttl");
                return true;
            }

            if (cloud.Counter <= 0)
            {
                cloud.CleanupDelayFrames--;
                if (cloud.CleanupDelayFrames <= 0 && cloud.Tag is ShipInstance tagged)
                {
                    System.Diagnostics.Debug.WriteLine($"[EXPLOSION] Cleaning up {tagged.Blueprint.Name} from slot {tagged.SlotIndex}");
                    _bubbleManager.Despawn(tagged.SlotIndex, "explosion complete");
                    return true;
                }
            }
            return false;
        });
    }

    /// <summary>
    /// Project an Elite-world position to screen coordinates using the view/projection matrices.
    /// Converts Elite (X,Y,Z) to MonoGame (X,Y,-Z) and applies RenderScale.
    /// </summary>
    private Vector2 ProjectToScreenElite(Vector3 eliteWorldPos)
    {
        Vector3 worldPos = ToMonoGameWorld(eliteWorldPos);

        // Apply view transform
        Vector3 viewPos = Vector3.Transform(worldPos, _view);

        // Apply projection transform
        Vector4 projected = Vector4.Transform(new Vector4(viewPos, 1f), _projection);

        // Perspective divide — guard against near-zero W to avoid huge values
        if (MathF.Abs(projected.W) < 0.001f)
        {
            int w0 = _graphicsDevice?.Viewport.Width ?? 1024;
            int h0 = _graphicsDevice?.Viewport.Height ?? 768;
            int hudH0 = (int)MathF.Round(h0 * HudHeightFraction);
            int viewH0 = Math.Max(1, h0 - hudH0);
            return new Vector2(w0 / 2f, viewH0 / 2f);
        }
        float ndcX = projected.X / projected.W;
        float ndcY = projected.Y / projected.W;

        // NDC to screen space: [-1,1] → [0,W] × [0,viewH]
        int w = _graphicsDevice?.Viewport.Width ?? 1024;
        int h = _graphicsDevice?.Viewport.Height ?? 768;
        int hudH = (int)MathF.Round(h * HudHeightFraction);
        int viewH = Math.Max(1, h - hudH);
        float screenX = (ndcX + 1f) * 0.5f * w;
        float screenY = (1 - ndcY) * 0.5f * viewH;

        return new Vector2(screenX, screenY);
    }

    private void DrawCelestialSun(SpriteBatch spriteBatch, Vector3 worldPosElite, float radiusElite, Color color)
    {
        if (!IsInFrontOfCamera(worldPosElite)) return;

        Vector2 screenPos = ProjectToScreenElite(worldPosElite);
        float dist = ToMonoGameWorld(worldPosElite).Length();
        if (dist < 0.001f) return;

        int w = _graphicsDevice?.Viewport.Width ?? 1024;
        int h = _graphicsDevice?.Viewport.Height ?? 768;
        int hudH = (int)MathF.Round(h * HudHeightFraction);
        int viewH = Math.Max(1, h - hudH);
        float screenRadius = ((radiusElite * RenderScale) / dist) * (1.0f / 0.767f) * (viewH / 2f);
        if (screenRadius > 0 && screenRadius < 4000)
            _circleRenderer.DrawFilledCircle(spriteBatch, screenPos, screenRadius, color, _gameInstance?.DrawWhite ?? false);
    }

    private void DrawCelestialPlanet(SpriteBatch spriteBatch, Vector3 worldPosElite, float radiusElite, Color color)
    {
        if (!IsInFrontOfCamera(worldPosElite)) return;

        Vector2 screenPos = ProjectToScreenElite(worldPosElite);
        float dist = ToMonoGameWorld(worldPosElite).Length();
        if (dist < 0.001f) return;

        int w = _graphicsDevice?.Viewport.Width ?? 1024;
        int h = _graphicsDevice?.Viewport.Height ?? 768;
        int hudH = (int)MathF.Round(h * HudHeightFraction);
        int viewH = Math.Max(1, h - hudH);
        float screenRadius = ((radiusElite * RenderScale) / dist) * (1.0f / 0.767f) * (viewH / 2f);
        if (screenRadius > 0 && screenRadius < 4000)
            _circleRenderer.DrawCircle(spriteBatch, screenPos, screenRadius, color, 48, _gameInstance?.DrawWhite ?? false);
    }

    /// <summary>
    /// Handle entity spawn/despawn events for HUD notifications.
    /// </summary>
    private void OnEntityEvent(object? sender, EntityEventArgs e)
    {
        _lastEventMessage = e.Reason switch
        {
            "lifetime expired" => $"{e.EntityName} disappeared",
            "out of bounds" => $"{e.EntityName} left sector",
            _ => $"{e.EntityName} detected"
        };
        _eventMessageTimer = 120; // show for 2 seconds
    }

    /// <summary>
    /// Handle collision events for HUD notifications.
    /// </summary>
    private void OnCollision(object? sender, CollisionEventArgs e)
    {
        _lastEventMessage = $"COLLISION with {e.OtherShipName}!";
        _eventMessageTimer = 120; // show for 2 seconds
    }

    /// <summary>
    /// Save current game state to disk.
    /// </summary>
    private void SaveGame()
    {
        try
        {
            var savePath = SaveGameManager.GetDefaultSavePath();
            // Use Galaxy 0, System 0 as default (full galaxy context tracking to be added)
            var seed = GalaxySeed.Galaxy0System0;
            SaveGameManager.Save(savePath, _bubbleManager, 0, 0, seed);
            _lastSaveMessage = "GAME SAVED";
            _saveMessageTimer = 120; // show for 2 seconds
        }
        catch (Exception ex)
        {
            _lastSaveMessage = $"SAVE FAILED: {ex.Message}";
            _saveMessageTimer = 180;
        }
    }

    /// <summary>
    /// Fire laser at target in crosshairs.
    /// Uses the current view direction in Elite-world coordinates.
    /// </summary>
    private void FireLaserAtTarget()
    {
        var player = _bubbleManager.PlayerShip;
        if (player == null) return;

        const float hitConeCos = 0.96f; // ~15° cone
        const float maxRange = 600f;

        // Fire direction depends on current camera view mode.
        // NOTE: entity.Position is stored in Elite-world coordinates (same space used by spawning and movement),
        // so we keep the laser targeting math in that space as well.
        // Important: our simulation state (`entity.Position`) is in "Elite-world" coordinates.
        // Rendering converts Elite->MonoGame by flipping Z: mgZ = -eliteZ.
        // With the front camera looking down -Z in MonoGame space, "in front" corresponds to +Z in Elite-world.
        Vector3 forward = _viewMode switch
        {
            0 => new Vector3(0, 0, 1),   // front
            1 => new Vector3(0, 0, -1),  // rear
            2 => new Vector3(-1, 0, 0),  // left
            3 => new Vector3(1, 0, 0),   // right
            _ => new Vector3(0, 0, 1),
        };

        ShipInstance? bestTarget = null;
        float bestDot = -1f;

        foreach (var entity in _bubbleManager.GetAllActive())
        {
            if (entity.SlotIndex == GameConstants.PlayerSlot) continue;
            if (!entity.IsActive) continue;
            if (entity.Blueprint.Name == "Planet" || entity.Blueprint.Name == "Sun") continue;

            float distSq = entity.Position.LengthSquared();
            if (distSq > maxRange * maxRange) continue;

            float dist = (float)Math.Sqrt(distSq);
            float dot;
            
            if (dist < 5.0f) 
            {
                // Extremely close: automatic hit if in front
                dot = 1.0f;
            }
            else
            {
                Vector3 toTarget = entity.Position / dist;
                dot = Vector3.Dot(forward, toTarget);
            }
            
            if (dot >= hitConeCos && dot > bestDot)
            {
                bestDot = dot;
                bestTarget = entity;
            }
        }

        if (bestTarget != null)
        {
            // Hit! Play sound and deal damage
            _gameInstance?.Audio.PlayLaserHit();
            
            // Deal damage (shields first, then hull)
            int laserDamage = 90;
            bool destroyed = false;

            byte oldEnergy = bestTarget.Energy;
            byte oldHull = bestTarget.Hull;
            
            if (bestTarget.Energy > 0)
            {
                int shieldDmg = Math.Min(laserDamage, (int)bestTarget.Energy);
                bestTarget.Energy = (byte)(bestTarget.Energy - shieldDmg);
                int hullDmg = laserDamage - shieldDmg;
                if (hullDmg > 0)
                    destroyed = bestTarget.TakeDamage(hullDmg);
            }
            else
            {
                destroyed = bestTarget.TakeDamage(laserDamage);
            }

            System.Diagnostics.Debug.WriteLine($"[LASER] HIT on {bestTarget.Blueprint.Name}! Dist: {bestTarget.Position.Length():F0}, Dot: {bestDot:F4}, E: {oldEnergy}->{bestTarget.Energy}, H: {oldHull}->{bestTarget.Hull}");

            // Provide visual/text feedback for hits
            _lastEventMessage = "HIT!";
            _eventMessageTimer = 10;

            if (destroyed)
            {
                // Track kill and check for milestones
                bool milestone = _bubbleManager.Commander.AddKill();
                System.Diagnostics.Debug.WriteLine($"[KILL] TALLY={_bubbleManager.Commander.Tally}, milestone={milestone}");
                if (milestone)
                {
                    _lastEventMessage = "RIGHT ON COMMANDER!";
                    _eventMessageTimer = 180;
                }
                else if ((bestTarget.Blueprint.Personality & NewbFlags.Cop) != 0)
                {
                    _bubbleManager.Commander.LegalStatus = Math.Max(_bubbleManager.Commander.LegalStatus, (byte)64);
                    _lastEventMessage = "FUGITIVE! Killed a cop!";
                    _eventMessageTimer = 180;
                }

                // Spawn cargo drops before deactivating
                CollisionSystem.SpawnCargoDrops(bestTarget, _bubbleManager);

                bestTarget.IsActive = false;
                if (string.IsNullOrEmpty(_lastEventMessage) || _eventMessageTimer <= 0)
                {
                    _lastEventMessage = $"{bestTarget.Blueprint.Name} destroyed!";
                    _eventMessageTimer = 120;
                }
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[LASER] MISS (view={_viewMode}, forward={forward})");
        }
    }

    public override void UnloadContent()
    {
        if (_bubbleManager != null)
        {
            _bubbleManager.EntityEvent -= OnEntityEvent;
            _bubbleManager.CollisionEvent -= OnCollision;
        }

        _whitePixel?.Dispose();
    }
}
