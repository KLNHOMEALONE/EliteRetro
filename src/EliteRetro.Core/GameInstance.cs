using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EliteRetro.Core.Scenes;
using EliteRetro.Core.Managers;
using EliteRetro.Core.Systems;
using EliteRetro.Core.Entities;
using EliteRetro.Core.Audio;

namespace EliteRetro.Core;

public class GameInstance : Game, IGameContext
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private SceneManager _sceneManager = null!;
    private BitmapFont _font = null!;
    private IBubbleManager _bubbleManager = null!;
    private IPlayerManager _playerManager = null!;
    private MainLoopCounter _mcnt = null!;
    private Systems.TaskScheduler _taskScheduler = null!;
    private IAudioManager _audioManager = null!;
    private Systems.FlightControlService _flightControl = null!;
    private Systems.ICombatService _combatService = null!;
    private Input.IInputService _input = null!;
    private Systems.IExplosionService _explosionService = null!;
    private Systems.IHudService _hudService = null!;
    private Systems.IStardustService _stardustService = null!;
    private Systems.IMessageSystem _messageSystem = null!;
    private Systems.IWorldSimulationService _simulationService = null!;
    private Systems.ICelestialService _celestialService = null!;

    /// <summary>
    /// Global access to the local bubble manager.
    /// </summary>
    public IBubbleManager BubbleManager => _bubbleManager;

    /// <summary>
    /// Global access to the player state manager.
    /// </summary>
    public IPlayerManager PlayerManager => _playerManager;

    /// <summary>
    /// Global access to the centralized input service.
    /// </summary>
    public Input.IInputService Input => _input;

    /// <summary>
    /// Global access to the flight control service.
    /// </summary>
    public Systems.FlightControlService FlightControl => _flightControl;

    /// <summary>
    /// Main loop counter for frame-spread task scheduling.
    /// </summary>
    public MainLoopCounter MCNT => _mcnt;

    /// <summary>
    /// Task scheduler driven by MCNT.
    /// </summary>
    public Systems.TaskScheduler Scheduler => _taskScheduler;

    /// <summary>
    /// Audio manager for procedural sound effects.
    /// </summary>
    public IAudioManager Audio => _audioManager;

    /// <summary>
    /// Combat and laser fire service.
    /// </summary>
    public Systems.ICombatService Combat => _combatService;

    /// <summary>
    /// Explosion effect service.
    /// </summary>
    public Systems.IExplosionService Explosions => _explosionService;

    /// <summary>
    /// HUD state calculation service.
    /// </summary>
    public Systems.IHudService Hud => _hudService;

    /// <summary>
    /// Starfield (stardust) effect service.
    /// </summary>
    public Systems.IStardustService Stardust => _stardustService;

    /// <summary>
    /// On-screen message system.
    /// </summary>
    public Systems.IMessageSystem Messages => _messageSystem;

    /// <summary>
    /// Rotating universe simulation service.
    /// </summary>
    public Systems.IWorldSimulationService Simulation => _simulationService;

    /// <summary>
    /// Celestial body projection and rendering service.
    /// </summary>
    public Systems.ICelestialService Celestial => _celestialService;

    /// <summary>
    /// When true, all rendered objects use white color.
    /// </summary>
    public bool DrawWhite { get; set; }

    /// <summary>
    /// When true, hidden/invisible edges are drawn.
    /// </summary>
    public bool DrawInvisible { get; set; }

    public GameInstance()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "EliteRetro";

        _graphics.PreferredBackBufferWidth = 1024;
        _graphics.PreferredBackBufferHeight = 768;
        _graphics.ApplyChanges();
    }

    protected override void Initialize()
    {
        _sceneManager = new SceneManager();
        _input = new Input.InputService();
        _flightControl = new Systems.FlightControlService();
        _playerManager = new PlayerManager();
        _bubbleManager = new LocalBubbleManager(_playerManager);
        _mcnt = new MainLoopCounter();
        _taskScheduler = new Systems.TaskScheduler(_mcnt);
        _audioManager = new AudioManager();
        _combatService = new Systems.CombatService();
        _explosionService = new Systems.ExplosionService(GraphicsDevice);
        _hudService = new Systems.HudService();
        _stardustService = new Rendering.StardustRenderer(GraphicsDevice);
        _messageSystem = new Systems.MessageSystem();
        _simulationService = new Systems.WorldSimulationService();
        _celestialService = new Systems.CelestialService(GraphicsDevice);

        // Register MCNT-driven scheduled tasks (Phase 1.5)
        RegisterScheduledTasks();

        base.Initialize();
    }

    /// <summary>
    /// Register all MCNT-driven scheduled tasks per the authentic Elite task schedule.
    /// Tasks are spread across frames using (mcnt & mask) == offset checks.
    /// </summary>
    private void RegisterScheduledTasks()
    {
        // Every 8 frames, offset 0: energy/shield regen for all active entities
        _taskScheduler.RegisterEvery(8, 0, () =>
        {
            foreach (var entity in _bubbleManager.GetAllActive())
            {
                if (entity.Energy < (byte)entity.Blueprint.MaxEnergy)
                    entity.Energy = (byte)Math.Min(entity.Energy + 1, entity.Blueprint.MaxEnergy);
            }
        });

        // Every 8 frames, offsets 0-3: tactics processing for 1-2 ships
        for (int offset = 0; offset < 4; offset++)
        {
            int off = offset;
            _taskScheduler.RegisterEvery(8, (byte)offset, () =>
            {
                int slotIndex = GameConstants.FirstAvailableSlot + off;
                if (slotIndex < 20)
                {
                    var entity = _bubbleManager.GetSlot(slotIndex);
                    if (entity != null && entity.IsActive)
                    {
                        // Execute TACTICS routine for this ship, targeting player
                        ShipAISystem.ExecuteTactics(entity, _bubbleManager.PlayerShip, _bubbleManager, _mcnt.Value);
                    }
                }
            });
        }

        // Every 16 frames, offsets 0-11: TIDY orthonormalization
        for (int offset = 0; offset < 12; offset++)
        {
            int off = offset;
            _taskScheduler.RegisterEvery(16, (byte)off, () =>
            {
                var entity = _bubbleManager.GetSlot(off);
                if (entity != null && entity.IsActive)
                    entity.Orientation.Tidy();
            });
        }

        // Every 32 frames, offset 0: station proximity check
        _taskScheduler.RegisterEvery(32, 0, () =>
        {
            // Check if player is in safe zone → spawn station
            if (_bubbleManager.SunOrStation?.Blueprint?.Name == "Sun" && _bubbleManager.IsInSafeZone())
            {
                var stationModel = CoriolisStationModel.Create(1.0f);
                _bubbleManager.SpawnStation(new ShipBlueprint
                {
                    Name = "Coriolis Station",
                    Model = stationModel,
                    MaxSpeed = 0,
                    MaxEnergy = 255,
                    HullStrength = 255,
                    ShieldStrength = 255
                });
            }
        });

        // Every 32 frames, offset 10: altitude, crash landing, low energy warnings + collision check
        _taskScheduler.RegisterEvery(32, 10, () =>
        {
            // Check entity collisions
            CollisionSystem.CheckPlayerCollisions(_bubbleManager);

            // Check planet crash for player-adjacent ships
            var planet = _bubbleManager.Planet;
            foreach (var entity in _bubbleManager.GetAllActive())
            {
                if (entity.SlotIndex == GameConstants.PlanetSlot) continue;
                if (CollisionSystem.CheckPlanetCrash(entity, planet))
                    entity.IsActive = false;
            }

            // TODO: calculate altitude from planet, check crash landing, low energy warning
            // Placeholder for Phase 7 HUD warnings
        });

        // Every 32 frames, offset 20: sun effects (heat, fuel scooping)
        _taskScheduler.RegisterEvery(32, 20, () =>
        {
            var sunEffect = _playerManager.CheckSunProximity(_bubbleManager);
            if (sunEffect == SunProximityEffect.FuelScoop)
            {
                // Scoop 1 fuel unit per ~32 frames (about 0.5s at 60fps)
                if (_playerManager.Commander.Fuel < 70)
                    _playerManager.Commander.Fuel++;
            }
            // TODO: apply heat damage based on sunEffect
        });

        // Every 256 frames, offset 0: consider spawning a new ship
        _taskScheduler.RegisterEvery(256, 0, () =>
        {
            var planet = _bubbleManager.Planet;
            if (planet == null) return;

            // Altitude in Elite is roughly distance from planet (scaled)
            float dist = planet.Position.Length();
            float altitude = (dist - GameConstants.PlanetRadius) / 2000f;
            altitude = Math.Clamp(altitude, 0, 70);

            // Danger level influenced by altitude and commander legal status
            byte dangerLevel = SpawnSystem.CalculateDangerLevel(altitude, GovernmentType.Anarchy); // Default to Anarchy for maximum action in this phase
            if (_playerManager.Commander.LegalStatus > 50)
                dangerLevel = (byte)Math.Min(dangerLevel + 2, 7);

            // Try spawning a pack or single ship
            if (_playerManager.Commander.LegalStatus > 100 && dangerLevel > 4)
                SpawnSystem.TrySpawnPack(_bubbleManager, dangerLevel, altitude);
            else
                SpawnSystem.TrySpawnShip(_bubbleManager, dangerLevel, altitude);
        });
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = new BitmapFont(GraphicsDevice);

        // Load persistent options
        if (Systems.OptionsManager.TryLoad(out bool drawWhite, out bool drawInvisible))
        {
            DrawWhite = drawWhite;
            DrawInvisible = drawInvisible;
        }

        _sceneManager.ChangeScene(new MainMenuScene(this), Content, GraphicsDevice, this, _font);
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update();

        // Decrement MCNT each frame (wraps 0 → 255)
        _mcnt.Decrement();

        // Evaluate all scheduled tasks
        _taskScheduler.Evaluate();

        _messageSystem.Update();
        _combatService.Update();

        _sceneManager.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _sceneManager.Draw(_spriteBatch);
        base.Draw(gameTime);
    }

    public void ChangeScene(GameScene scene)
    {
        _sceneManager.ChangeScene(scene);
    }

    public void PushScene(GameScene scene)
    {
        _sceneManager.PushScene(scene);
    }

    public void PopScene()
    {
        _sceneManager.PopScene();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _audioManager?.Dispose();
            _explosionService?.Dispose();
            _stardustService?.Dispose();
            _celestialService?.Dispose();
            _simulationService?.Dispose();
        }
        base.Dispose(disposing);
    }
}
