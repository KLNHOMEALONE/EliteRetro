using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EliteRetro.Core.Scenes;
using EliteRetro.Core.Managers;
using EliteRetro.Core.Systems;

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
        base.Initialize();
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
