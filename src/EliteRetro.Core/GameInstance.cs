using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EliteRetro.Core.Scenes;
using EliteRetro.Core.Managers;
using EliteRetro.Core.Systems;
using EliteRetro.Core.Entities;
using EliteRetro.Core.Audio;

namespace EliteRetro.Core;

public class GameInstance : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private SceneManager _sceneManager = null!;
    private BitmapFont _font = null!;
    private LocalBubbleManager _bubbleManager = null!;
    private MainLoopCounter _mcnt = null!;
    private Systems.TaskScheduler _taskScheduler = null!;
    private AudioManager _audioManager = null!;

    /// <summary>
    /// Global access to the local bubble manager.
    /// </summary>
    public LocalBubbleManager BubbleManager => _bubbleManager;

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
    public AudioManager Audio => _audioManager;

    /// <summary>
    /// When true, all rendered objects use white color.
    /// </summary>
    public bool DrawWhite { get; set; }

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
        _bubbleManager = new LocalBubbleManager();
        _mcnt = new MainLoopCounter();
        _taskScheduler = new Systems.TaskScheduler(_mcnt);
        _audioManager = new AudioManager();

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
            CollisionSystem.CheckCollisions(_bubbleManager);

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
            var sunEffect = _bubbleManager.CheckSunProximity();
            if (sunEffect == LocalBubbleManager.SunProximityEffect.FuelScoop)
            {
                // Scoop 1 fuel unit per ~32 frames (about 0.5s at 60fps)
                if (_bubbleManager.Commander.Fuel < 70)
                    _bubbleManager.Commander.Fuel++;
            }
            // TODO: apply heat damage based on sunEffect
        });

        // Every 256 frames, offset 0: consider spawning a new ship
        _taskScheduler.RegisterEvery(256, 0, () =>
        {
            if (_bubbleManager.TargetPracticeMode) return;
            // TODO: calculate danger level and altitude from current system
            byte dangerLevel = 3; // placeholder
            byte altitude = 10;   // placeholder
            SpawnSystem.TrySpawnShip(_bubbleManager, dangerLevel, altitude);
        });
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = new BitmapFont(GraphicsDevice);

        // Load persistent options
        if (Systems.OptionsManager.TryLoad(out bool drawWhite))
            DrawWhite = drawWhite;

        _sceneManager.ChangeScene(new MainMenuScene(this), Content, GraphicsDevice, this, _font);
    }

    protected override void Update(GameTime gameTime)
    {
        // Decrement MCNT each frame (wraps 0 → 255)
        _mcnt.Decrement();

        // Evaluate all scheduled tasks
        _taskScheduler.Evaluate();

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _audioManager?.Dispose();
        base.Dispose(disposing);
    }
}
