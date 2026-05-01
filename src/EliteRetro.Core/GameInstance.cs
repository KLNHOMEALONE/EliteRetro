using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EliteRetro.Core.Scenes;
using EliteRetro.Core.Managers;
using EliteRetro.Core.Systems;
using EliteRetro.Core.Entities;

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
                int slotIndex = 2 + off;
                if (slotIndex < 20)
                {
                    var entity = _bubbleManager.GetSlot(slotIndex);
                    if (entity != null && entity.IsActive)
                    {
                        if (entity.Energy < (byte)entity.Blueprint.MaxEnergy)
                            entity.Energy = (byte)Math.Min(entity.Energy + 2, entity.Blueprint.MaxEnergy);
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

        // Every 32 frames, offset 10: altitude, crash landing, low energy warnings
        _taskScheduler.RegisterEvery(32, 10, () =>
        {
            // TODO: calculate altitude from planet, check crash landing, low energy warning
            // Placeholder for Phase 7 HUD warnings
        });

        // Every 32 frames, offset 20: sun effects (heat, fuel scooping)
        _taskScheduler.RegisterEvery(32, 20, () =>
        {
            // TODO: apply sun proximity effects
            // Placeholder for Phase 7 fuel scooping and heat damage
        });

        // Every 256 frames, offset 0: consider spawning a new ship
        _taskScheduler.RegisterEvery(256, 0, () =>
        {
            // TODO: spawn system based on danger level and altitude
            // Placeholder — random spawn for now (Phase 6)
        });
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = new BitmapFont(GraphicsDevice);
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
        _sceneManager.PushScene(scene);
    }

    public void PushScene(GameScene scene)
    {
        _sceneManager.PushScene(scene);
    }
}
