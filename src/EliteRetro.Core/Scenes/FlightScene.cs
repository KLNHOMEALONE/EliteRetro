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
    private WireframeRenderer _wireframeRenderer = null!;
    private CircleRenderer _circleRenderer = null!;
    private PlanetRenderer _planetRenderer = null!;
    private SunRenderer _sunRenderer = null!;
    private RingRenderer _ringRenderer = null!;
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

    public FlightScene(Game? game = null)
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
    }

    public override void LoadContent(ContentManager content, BitmapFont font, GraphicsDevice graphicsDevice)
    {
        _font = font;
        _graphicsDevice = graphicsDevice;
        _wireframeRenderer = new WireframeRenderer(_graphicsDevice);
        _circleRenderer = new CircleRenderer(_graphicsDevice);
        _planetRenderer = new PlanetRenderer(_graphicsDevice);
        _sunRenderer = new SunRenderer(_graphicsDevice);
        _ringRenderer = new RingRenderer(_graphicsDevice);
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

        _projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(75f),
            1024f / 480f, // Space view aspect ratio (1024x480)
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
            Position = new Vector3(0, 0, -GameConstants.PlanetRadius * 5),
            Speed = 0
        };
        _bubbleManager.SetSlot(GameConstants.PlanetSlot, planet);

        // Slot 1: Sun - placed at 2.67-18.67 planet radii, behind player
        float sunDistance = GameConstants.PlanetRadius * (2.67f + (float)(new Random().NextDouble() * 16));
        var sunModel = SunModel.Create(GameConstants.PlanetRadius * 80);
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
            Position = new Vector3(0, 0, sunDistance), // Behind player (positive Z)
            Speed = 0
        };
        _bubbleManager.SetSlot(GameConstants.SunStationSlot, sun);

        // DEBUG: Spawn station immediately for testing
        // Remove this when safe zone approach works naturally
        var stationModel = ShuttleModel.Create(48);
        var stationBlueprint = new ShipBlueprint
        {
            Name = "Coriolis Station",
            Model = stationModel,
            MaxSpeed = 0,
            MaxEnergy = 255,
            HullStrength = 255,
            ShieldStrength = 255
        };
        var station = new ShipInstance(stationBlueprint)
        {
            Position = new Vector3(0, 0, -GameConstants.PlanetRadius * 2),
            Speed = 0
        };
        station.Orientation.Nosev = Vector3.UnitZ; // Face player
        _bubbleManager.SetSlot(GameConstants.SunStationSlot, station);
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
            // Forward = objects' Z increases towards camera (0) in RH.
            float moveStep = _playerSpeed * dt * 60f;
            foreach (var entity in _bubbleManager.GetAllActive())
            {
                // Skip player - player does not move
                if (entity.SlotIndex == GameConstants.PlayerSlot) continue;

                if (_playerSpeed != 0)
                {
                    entity.Position.Z += moveStep;
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

            // Speed control via W/S keys
            if (_lastControl.SpeedDelta != 0)
                _playerSpeed = Math.Clamp(_playerSpeed + _lastControl.SpeedDelta * (float)gameTime.ElapsedGameTime.TotalSeconds * 60, 0f, 40f);

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
            _showHiddenEdges = !_showHiddenEdges;

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

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        // Draw stardust (starfield)
        _stardustRenderer.Draw(spriteBatch, new Vector2(512, 240), 500f, _view);

        // Draw explosions
        foreach (var cloud in _explosions)
            _explosionRenderer.UpdateAndDraw(spriteBatch, cloud, _lastGameTime);

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
                Vector3 entityPosMG = new Vector3(entity.Position.X, entity.Position.Y, -entity.Position.Z);
                
                Matrix entityWorld = entityOrientation * Matrix.CreateTranslation(entityPosMG);
                _wireframeRenderer.Draw(entity.Blueprint.Model, entityWorld, _view, _projection, spriteBatch, drawHiddenEdges: _showHiddenEdges);
            }
        }

        // Draw planet with surface features and rings
        if (_bubbleManager.Planet != null)
        {
            // Convert cumulative roll to 0-63 units (1/64-turn)
            // Negative roll (left) → planet features rotate right (counter-rotation)
            int rollAngle64 = ((int)(_cumulativeRoll * 64 / MathHelper.TwoPi) % 64 + 64) % 64;
            int totalPlanetRotation = (_planetRotation + rollAngle64) % 64;

            // Back rings first (with counter-rotation)
            DrawCelestialRings(spriteBatch, _bubbleManager.Planet.Position, GameConstants.PlanetRadius, new Color(180, 160, 120), "back", rollAngle64);

            // Planet (with counter-rotation)
            DrawCelestialPlanet(spriteBatch, _bubbleManager.Planet.Position, GameConstants.PlanetRadius, new Color(50, 100, 180), totalPlanetRotation);

            // Front rings (with counter-rotation)
            DrawCelestialRings(spriteBatch, _bubbleManager.Planet.Position, GameConstants.PlanetRadius, new Color(180, 160, 120), "front", rollAngle64);
        }

        // Draw sun
        if (_bubbleManager.SunOrStation != null && _bubbleManager.SunOrStation.Blueprint?.Name == "Sun")
        {
            DrawCelestialSun(spriteBatch, _bubbleManager.SunOrStation.Position, GameConstants.PlanetRadius * 6, SunRenderer.GetSunColor(0));
        }

        // Damage flash overlay (red vignette when hit)
        if (_damageFlashTimer > 0)
        {
            float intensity = _damageFlashTimer / 15f;
            byte alpha = (byte)(intensity * 128);
            Color flashColor = new Color(255, 0, 0) { A = alpha };
            spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, 1024, 768), flashColor);
        }

        // Crosshair at center of space view (BBC Elite targeting reticle)
        const int cx = 512;
        const int cy = 240;
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

        // Target practice mode indicator and health status
        if (_targetPracticeMode)
        {
            var sz = _font.MeasureString("TARGET PRACTICE");
            _font.DrawString(spriteBatch, "TARGET PRACTICE",
                new Vector2(1024 / 2 - sz.X / 2, 10), new Color(255, 200, 50), 1.0f);

            // Find the target ship to show its health
            var targetShip = _bubbleManager.GetAllActive().FirstOrDefault(e => e.IsTargetPractice);
            if (targetShip != null)
            {
                string status = $"S: {targetShip.Energy} H: {targetShip.Hull}";
                var statSz = _font.MeasureString(status);
                _font.DrawString(spriteBatch, status,
                    new Vector2(1024 / 2 - statSz.X / 2, 40), Color.White, 0.8f);
            }
        }

        // Draw lasers when firing
        if (_laserFlashTimer > 0)
        {
            DrawLine(spriteBatch, new Vector2(0, 480), new Vector2(cx, cy), Color.Yellow, 2);
            DrawLine(spriteBatch, new Vector2(1024, 480), new Vector2(cx, cy), Color.Yellow, 2);
        }

        // HUD overlay
        DrawHUD(spriteBatch);

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
            // Spawn ahead (negative Z), fly toward player (positive Z direction)
            position = new Vector3(lateralX, lateralY, -distance);
            entitySpeed = speed;

            // In ram mode, aim directly at player position
            if (_ramMode)
            {
                Vector3 toPlayer = -position;
                if (toPlayer.LengthSquared() > 0.01f)
                {
                    toPlayer = Vector3.Normalize(toPlayer);
                    orientation = new OrientationMatrix
                    {
                        Nosev = toPlayer,
                        Roofv = Vector3.Normalize(Vector3.Cross(toPlayer, Vector3.UnitY)),
                        Sidev = Vector3.Normalize(Vector3.Cross(orientation.Roofv, toPlayer))
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
            // Spawn between player and planet, fly away from player toward planet
            position = new Vector3(lateralX, lateralY, -distance * 0.3f);
            entitySpeed = speed;
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
    /// Clears all entities except player and planet for a clean test range.
    /// </summary>
    private void SpawnTargetPracticeShip()
    {
        // Save planet before clearing
        var planet = _bubbleManager.Planet;

        // Clear ships/entities
        _bubbleManager.Clear();

        // Restore planet
        if (planet != null)
        {
            planet.IsActive = true;
            _bubbleManager.SetSlot(GameConstants.PlanetSlot, planet);
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
            Position = new Vector3(0, 0, -400), // 400 units ahead along -Z (Nosev direction at identity)
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

    private void DrawHUD(SpriteBatch spriteBatch)
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
            ShowHiddenEdges = _showHiddenEdges
        };

        // Draw dashboard
        _hudRenderer.Draw(spriteBatch, hudState, _font);

        // Scanner display
        _scannerRenderer.Draw(spriteBatch, _bubbleManager, GameConstants.PlayerSlot, _universeOrientation);

        // Flight data text (left side, original positions)
        float planetDist = _bubbleManager.Planet?.Position.Length() ?? 0;
        float sunDist = _bubbleManager.SunOrStation?.Position.Length() ?? 0;
        _font.DrawString(spriteBatch, $"PLANET DIST: {planetDist:F0}", new Vector2(10, 60), Color.Cyan, 1.2f);
        _font.DrawString(spriteBatch, $"SUN DIST: {sunDist:F0}", new Vector2(10, 82), Color.Orange, 1.2f);

        // Hidden edges indicator
        _font.DrawString(spriteBatch, _showHiddenEdges ? "HIDDEN: ON" : "HIDDEN: OFF", new Vector2(10, 160), Color.White, 0.8f);

        // Ram mode indicator
        Color ramColor = _ramMode ? Color.Red : Color.DarkGray;
        _font.DrawString(spriteBatch, _ramMode ? "RAM MODE: ON (press R to toggle)" : "RAM MODE: OFF (press R to toggle)", new Vector2(10, 182), ramColor, 0.8f);

        // Debug: show distances to nearby entities
        int debugY = 205;
        foreach (var entity in _bubbleManager.GetActiveShips())
        {
            float dist = entity.Position.Length();
            if (dist < 500)
            {
                Color distColor = dist < 50 ? Color.Red : dist < 200 ? Color.Orange : Color.Yellow;
                _font.DrawString(spriteBatch, $"{entity.Blueprint.Name}: {dist:F0}", new Vector2(10, debugY), distColor, 0.8f);
                debugY += 22;
            }
        }

        // Controls
        _font.DrawString(spriteBatch, "ARROWS: PITCH/ROLL  W/S: SPEED  V: VIEW  SPACE: FIRE  P: PAUSE  +/-: ZOOM  I: EDGES  F5: SAVE  ESC: MENU", new Vector2(10, 740), Color.Gray, 0.8f);

        // Entity event messages (spawn/despawn)
        if (_eventMessageTimer > 0 && !string.IsNullOrEmpty(_lastEventMessage))
        {
            _font.DrawString(spriteBatch, $">> {_lastEventMessage}", new Vector2(300, 350), Color.Yellow, 1.2f);
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
        // Object is in front of camera if the vector to it from origin
        // has a positive dot product with the current camera look direction.
        if (worldPos.LengthSquared() < 0.001f) return true;
        return Vector3.Dot(Vector3.Normalize(worldPos), _cameraLookDir) > 0;
    }

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
                Vector2 screenPos = ProjectToScreen(entity.Position);
                float distance = entity.Position.Length();
                var cloud = _explosionRenderer.CreateExplosion(entity.Blueprint.Model, screenPos, distance);
                cloud.Tag = entity;
                _explosions.Add(cloud);
            }
        }

        // Update and clean up explosions (with delay after visual completion)
        _explosions.RemoveAll(cloud =>
        {
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
    /// Project a world position to screen coordinates using the view/projection matrices.
    /// World coordinates use standard MonoGame convention (-Z is ahead).
    /// </summary>
    private Vector2 ProjectToScreen(Vector3 worldPos)
    {
        // Apply view transform
        Vector3 viewPos = Vector3.Transform(worldPos, _view);

        // Apply projection transform
        Vector4 projected = Vector4.Transform(new Vector4(viewPos, 1f), _projection);

        // Perspective divide
        if (projected.W == 0) return new Vector2(512, 240);
        float ndcX = projected.X / projected.W;
        float ndcY = projected.Y / projected.W;

        // NDC to screen space: [-1,1] → [0,1024] × [0,480]
        float screenX = (ndcX + 1f) * 0.5f * 1024f;
        float screenY = (1 - ndcY) * 0.5f * 480f;

        return new Vector2(screenX, screenY);
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
    /// Uses standard MonoGame coordinates (-Z is ahead).
    /// </summary>
    private void FireLaserAtTarget()
    {
        var player = _bubbleManager.PlayerShip;
        if (player == null) return;

        const float hitConeCos = 0.96f; // ~15° cone
        const float maxRange = 600f;

        // Forward is -Z in standard MonoGame convention.
        Vector3 forward = new Vector3(0, 0, -1);

        ShipInstance? bestTarget = null;
        float bestDot = -1f;

        foreach (var entity in _bubbleManager.GetAllActive())
        {
            if (entity.SlotIndex == GameConstants.PlayerSlot) continue;
            if (!entity.IsActive) continue;
            if (entity.Blueprint.Name == "Planet" || entity.Blueprint.Name == "Sun") continue;

            float distSq = entity.Position.LengthSquared();
            if (distSq > maxRange * maxRange) continue;

            // Objects must be in front (negative Z in MonoGame system)
            if (entity.Position.Z >= 0) continue;

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
                // Spawn cargo drops before deactivating
                CollisionSystem.SpawnCargoDrops(bestTarget, _bubbleManager);

                bestTarget.IsActive = false;
                _lastEventMessage = $"{bestTarget.Blueprint.Name} destroyed!";
                _eventMessageTimer = 120;
            }
        }
    }

    private void DrawCelestialRings(SpriteBatch spriteBatch, Vector3 worldPos, float radius, Color color, string layer = "all", int tiltAngle = 16)
    {
        // Skip if object is behind the camera
        if (!IsInFrontOfCamera(worldPos)) return;

        Vector2 screenPos = ProjectToScreen(worldPos);
        float dist = worldPos.Length();
        if (dist < 0.001f) return;

        // Screen radius calculation: (worldRadius / distance) * verticalFOVFactor * screenHeight
        // FOV is 75 deg. tan(75/2) = 0.767
        float screenRadius = (radius / dist) * (1.0f / 0.767f) * (480 / 2);

        if (screenRadius > 0 && screenRadius < 1000)
            _ringRenderer.DrawAxisAlignedRings(spriteBatch, screenPos, screenRadius, 1.4f, 2.2f, color, tiltAngle, layer);
    }

    private void DrawCelestialSun(SpriteBatch spriteBatch, Vector3 worldPos, float radius, Color color)
    {
        if (!IsInFrontOfCamera(worldPos)) return;

        Vector2 screenPos = ProjectToScreen(worldPos);
        float dist = worldPos.Length();
        if (dist < 0.001f) return;

        float screenRadius = (radius / dist) * (1.0f / 0.767f) * (480 / 2);

        if (screenRadius > 0 && screenRadius < 2000)
            _sunRenderer.DrawSun(spriteBatch, screenPos, screenRadius, color);
    }

    private void DrawCelestialPlanet(SpriteBatch spriteBatch, Vector3 worldPos, float radius, Color color, int rotationAngle = 0)
    {
        if (!IsInFrontOfCamera(worldPos)) return;

        Vector2 screenPos = ProjectToScreen(worldPos);
        float dist = worldPos.Length();
        if (dist < 0.001f) return;

        float screenRadius = (radius / dist) * (1.0f / 0.767f) * (480 / 2);

        if (screenRadius > 0 && screenRadius < 1000)
            _planetRenderer.DrawPlanet(spriteBatch, screenPos, screenRadius, color, rotationAngle);
    }

    public override void UnloadContent()
    {
    }
}
