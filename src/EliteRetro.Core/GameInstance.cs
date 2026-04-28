using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EliteRetro.Core.Scenes;
using EliteRetro.Core.Managers;

namespace EliteRetro.Core;

public class GameInstance : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private SceneManager _sceneManager = null!;
    private BitmapFont _font = null!;
    private LocalBubbleManager _bubbleManager = null!;

    /// <summary>
    /// Global access to the local bubble manager.
    /// </summary>
    public LocalBubbleManager BubbleManager => _bubbleManager;

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
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = new BitmapFont(GraphicsDevice);
        _sceneManager.ChangeScene(new MainMenuScene(this), Content, this, _font);
    }

    protected override void Update(GameTime gameTime)
    {
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
