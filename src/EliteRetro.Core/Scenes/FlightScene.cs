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
    private KeyboardState _prevKb;
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
        _stardustRenderer.Initialize(42); // Fixed seed for consistent starfield

        // Create 1x1 white texture for damage flash overlay
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });

        _projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(75f),
            _graphicsDevice.Viewport.AspectRatio,
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
    }

    public override void Update(GameTime gameTime)
    {
        _lastGameTime = gameTime;
        var kb = Keyboard.GetState();
        var control = _flightControlService.Update(gameTime);

        if (!control.IsPaused)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Apply Minsky rotation to the player's orientation (frame-rate independent)
            // This rotates the view matrix, which makes the scene appear to rotate.
            // Entity positions are in world space and are NOT rotated — they stay fixed.
            float rollDelta = -control.RollAngle * dt * 60f;
            float pitchDelta = -control.PitchAngle * dt * 60f;
            _universeOrientation.ApplyUniverseRotation(rollDelta, pitchDelta);

            // Track cumulative roll for planet/ring counter-rotation
            _cumulativeRoll += rollDelta;

            // Move entities forward
            // Player speed makes all entities move toward the player (Z increases)
            // Entity's own speed adds to this motion
            foreach (var entity in _bubbleManager.GetAllActive())
            {
                // Player's forward speed: entities move toward camera
                // In Elite, speed makes the universe scroll toward the player
                if (_playerSpeed > 0)
                {
                    // Entities move in +Z direction (toward player at origin)
                    entity.Position.Z += _playerSpeed;
                }
                // Entity's own movement
                if (entity.Speed != 0)
                    entity.MoveForward();
            }

            // Periodic TIDY orthonormalization is handled by MCNT scheduler (every 16, offsets 0-11)

            // Check for newly destroyed entities and spawn explosions
            CheckExplosions();

            // Cleanup expired entities (lifetime or out of bounds)
            _bubbleManager.CleanupExpired();

            // Slow planet rotation
            if (_planetRotationCounter++ % 32 == 0)
                _planetRotation = (_planetRotation + 1) % 64;

            // Random ship/asteroid spawning (about 1 per 3 seconds at 60fps)
            _spawnCounter++;
            if (_spawnCounter % 180 == 0 && _rng.NextDouble() < 0.3)
                SpawnRandomEntity();

            // View switching from control service
            _viewMode = control.ViewIndex;

            // Zoom with +/-
            float speed = 2f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (kb.IsKeyDown(Keys.OemPlus) || kb.IsKeyDown(Keys.Add)) _cameraDistance -= speed;
            if (kb.IsKeyDown(Keys.OemMinus) || kb.IsKeyDown(Keys.Subtract)) _cameraDistance += speed;
            _cameraDistance = MathHelper.Clamp(_cameraDistance, 2f, 20f);

            // Speed control via W/S keys
            if (control.SpeedDelta != 0)
                _playerSpeed = Math.Clamp(_playerSpeed + control.SpeedDelta * (float)gameTime.ElapsedGameTime.TotalSeconds * 60, 0f, 40f);

            // Update stardust
            _stardustRenderer.Update(_playerSpeed);

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
        }

        // Toggle local pause on Space (separate from flight control pause)
        if (kb.IsKeyDown(Keys.Space) && _prevKb.IsKeyUp(Keys.Space) && !control.IsPaused)
            _paused = !_paused;

        // Toggle ram mode with R
        if (kb.IsKeyDown(Keys.R) && _prevKb.IsKeyUp(Keys.R))
            _ramMode = !_ramMode;

        // Toggle hidden edges with I
        if (kb.IsKeyDown(Keys.I) && _prevKb.IsKeyUp(Keys.I))
            _showHiddenEdges = !_showHiddenEdges;

        // Build view matrix from orientation.
        // The view matrix maps world space to camera space.
        // Rows: camera-right, camera-up, camera-forward (camera look direction = -Z in camera space)
        // Controls always act on the ship's actual orientation axes (nosev/roofv/sidev),
        // regardless of view mode. Only the camera direction changes.
        Vector3 side = _universeOrientation.Sidev;
        Vector3 roof = _universeOrientation.Roofv;
        Vector3 nose = _universeOrientation.Nosev;
        Vector3 forward = -nose; // default: camera looks along the ship's nose direction

        // Apply view direction changes based on current view mode
        switch (_viewMode)
        {
            case 1: // Rear view: look opposite direction
                forward = nose;
                break;
            case 2: // Left view: look 90° to port
                forward = Vector3.Normalize(Vector3.Cross(roof, nose));
                break;
            case 3: // Right view: look 90° to starboard
                forward = Vector3.Normalize(Vector3.Cross(-roof, nose));
                break;
        }

        _view = new Matrix(
            side.X, side.Y, side.Z, 0,
            roof.X, roof.Y, roof.Z, 0,
            forward.X, forward.Y, forward.Z, 0,
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
        _stardustRenderer.Draw(spriteBatch, new Vector2(512, 384), 500f);

        // Draw explosions
        foreach (var cloud in _explosions)
            _explosionRenderer.UpdateAndDraw(spriteBatch, cloud, _lastGameTime);

        // Render bubble entities (skip planet and sun - rendered separately)
        foreach (var entity in _bubbleManager.GetAllActive())
        {
            if (entity.Blueprint?.Model != null &&
                entity.Blueprint.Name != "Planet" &&
                entity.Blueprint.Name != "Sun" &&
                IsInFrontOfCamera(entity.Position))
            {
                // Build world matrix from entity orientation.
                // Sidev = ship's right axis, Roofv = ship's up axis, Nosev = ship's forward (nose).
                // In a model matrix: col0=right, col1=up, col2=forward.
                // The ship model's nose points in +Z model space (e.g. Sidewinder nose at Z=+36).
                Matrix entityOrientation = new Matrix(
                    entity.Orientation.Sidev.X, entity.Orientation.Roofv.X, entity.Orientation.Nosev.X, 0,
                    entity.Orientation.Sidev.Y, entity.Orientation.Roofv.Y, entity.Orientation.Nosev.Y, 0,
                    entity.Orientation.Sidev.Z, entity.Orientation.Roofv.Z, entity.Orientation.Nosev.Z, 0,
                    0, 0, 0, 1);
                Matrix entityWorld = Matrix.CreateScale(0.0004f) *
                                     entityOrientation *
                                     Matrix.CreateTranslation(entity.Position * 0.0001f);
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
            Fuel = _bubbleManager.PlayerFuel,
            CabinTemp = 0,
            LaserTemp = 0,
            Altitude = (int)(_bubbleManager.Planet?.Position.Length() ?? 0),
            EnergyBanks = 0,
            Missiles = _bubbleManager.PlayerMissiles,
            MaxMissiles = 4,
            ShieldForward = (byte)(_bubbleManager.PlayerShip?.Energy ?? 200),
            ShieldAft = (byte)(_bubbleManager.PlayerShip?.Energy ?? 200),
            Pitch = 0,
            Roll = _cumulativeRoll,
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
        _font.DrawString(spriteBatch, "ARROWS: PITCH/ROLL  W/S: SPEED  V: VIEW  +/-: ZOOM  SPACE: PAUSE  I: EDGES  ESC: MENU", new Vector2(10, 740), Color.Gray, 0.8f);

        // Entity event messages (spawn/despawn)
        if (_eventMessageTimer > 0 && !string.IsNullOrEmpty(_lastEventMessage))
        {
            _font.DrawString(spriteBatch, $">> {_lastEventMessage}", new Vector2(300, 350), Color.Yellow, 1.2f);
            _eventMessageTimer--;
        }

        if (_paused)
            _font.DrawString(spriteBatch, "PAUSED", new Vector2(400, 350), Color.Red, 2f);
    }

    private bool IsInFrontOfCamera(Vector3 worldPos)
    {
        // Camera is at origin, looking along 'forward' direction
        // Object is in front if dot(worldPos, forward) > 0
        Vector3 forward = new Vector3(-_view.M31, -_view.M32, -_view.M33);
        return Vector3.Dot(worldPos, forward) > 0;
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
    /// </summary>
    private Vector2 ProjectToScreen(Vector3 worldPos)
    {
        // Apply view transform (camera at origin, looking along -Z in Elite coords)
        // The view matrix transforms world → view space
        Vector3 viewPos = Vector3.Transform(worldPos, _view);

        // Apply projection transform
        Vector4 projected = Vector4.Transform(new Vector4(viewPos, 1f), _projection);

        // Perspective divide
        if (projected.W == 0) return new Vector2(512, 384);
        float ndcX = projected.X / projected.W;
        float ndcY = projected.Y / projected.W;

        // NDC to screen space: [-1,1] → [0,1024] × [0,768]
        float screenX = (ndcX + 1f) * 0.5f * 1024f;
        float screenY = (-ndcY + 1f) * 0.5f * 768f;

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

    private void DrawCelestialRings(SpriteBatch spriteBatch, Vector3 worldPos, float radius, Color color, string layer = "all", int tiltAngle = 16)
    {
        // Skip if object is behind the camera
        if (!IsInFrontOfCamera(worldPos)) return;

        Vector3 pos = worldPos * 0.0001f;
        Vector3 projected = Vector3.Transform(pos, _view * _projection);
        if (projected.Z == 0) return;

        float ndcX = projected.X / projected.Z;
        float ndcY = projected.Y / projected.Z;
        var viewport = _graphicsDevice.Viewport;
        float screenX = (ndcX + 1) / 2 * viewport.Width;
        float screenY = (1 - ndcY) / 2 * viewport.Height;

        float screenRadius = radius * 0.0001f / Math.Abs(projected.Z) * viewport.Height / 2;
        if (screenRadius > 0 && screenRadius < 500)
            _ringRenderer.DrawAxisAlignedRings(spriteBatch, new Vector2(screenX, screenY), screenRadius, 1.4f, 2.2f, color, tiltAngle, layer);
    }

    private void DrawCelestialSun(SpriteBatch spriteBatch, Vector3 worldPos, float radius, Color color)
    {
        if (!IsInFrontOfCamera(worldPos)) return;

        Vector3 pos = worldPos * 0.0001f;
        Vector3 projected = Vector3.Transform(pos, _view * _projection);
        if (projected.Z == 0) return;

        float ndcX = projected.X / projected.Z;
        float ndcY = projected.Y / projected.Z;
        var viewport = _graphicsDevice.Viewport;
        float screenX = (ndcX + 1) / 2 * viewport.Width;
        float screenY = (1 - ndcY) / 2 * viewport.Height;

        float screenRadius = radius * 0.0001f / Math.Abs(projected.Z) * viewport.Height / 2;
        if (screenRadius > 0 && screenRadius < 500)
            _sunRenderer.DrawSun(spriteBatch, new Vector2(screenX, screenY), screenRadius, color);
    }

    private void DrawCelestialPlanet(SpriteBatch spriteBatch, Vector3 worldPos, float radius, Color color, int rotationAngle = 0)
    {
        if (!IsInFrontOfCamera(worldPos)) return;

        Vector3 pos = worldPos * 0.0001f;
        Vector3 projected = Vector3.Transform(pos, _view * _projection);
        if (projected.Z == 0) return;

        float ndcX = projected.X / projected.Z;
        float ndcY = projected.Y / projected.Z;
        var viewport = _graphicsDevice.Viewport;
        float screenX = (ndcX + 1) / 2 * viewport.Width;
        float screenY = (1 - ndcY) / 2 * viewport.Height;

        float screenRadius = radius * 0.0001f / Math.Abs(projected.Z) * viewport.Height / 2;
        if (screenRadius > 0 && screenRadius < 500)
            _planetRenderer.DrawPlanet(spriteBatch, new Vector2(screenX, screenY), screenRadius, color, rotationAngle);
    }

    public override void UnloadContent() { }
}
