using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EliteRetro.Core.Scenes;

/// <summary>
/// Manages a stack of game scenes, handling transitions and lifecycle.
/// </summary>
public class SceneManager
{
    private readonly Stack<GameScene> _sceneStack = new();
    private GameScene? _nextScene;
    private ContentManager _content = null!;
    private BitmapFont _font = null!;
    private GraphicsDevice _graphicsDevice = null!;
    private IGameContext? _gameInstance;

    public void ChangeScene(GameScene newScene, ContentManager content, GraphicsDevice graphicsDevice, IGameContext? game = null, BitmapFont? font = null)
    {
        while (_sceneStack.Count > 0)
        {
            var old = _sceneStack.Pop();
            old.UnloadContent();
        }
        _content = content;
        _graphicsDevice = graphicsDevice;
        _gameInstance = game;
        if (font != null) _font = font;

        newScene.LoadContent(content, _font, graphicsDevice);
        _sceneStack.Push(newScene);
    }

    public void ChangeScene(GameScene newScene)
    {
        while (_sceneStack.Count > 0)
        {
            var old = _sceneStack.Pop();
            old.UnloadContent();
        }

        newScene.LoadContent(_content, _font, _graphicsDevice);
        _sceneStack.Push(newScene);
    }

    public void PushScene(GameScene newScene)
    {
        _nextScene = newScene;
    }

    public void PopScene()
    {
        if (_sceneStack.Count <= 1)
        {
            _gameInstance?.Exit();
            return;
        }

        var old = _sceneStack.Pop();
        old.UnloadContent();
    }

    public void Update(GameTime gameTime)
    {
        // Handle queued scene push
        if (_nextScene != null)
        {
            _nextScene.LoadContent(_content, _font, _graphicsDevice);
            _sceneStack.Push(_nextScene);
            _nextScene = null;
        }

        // Check for Escape to go back (using centralized input if available)
        if (_gameInstance != null)
        {
            if (_gameInstance.Input.IsKeyPressed(Keys.Escape))
            {
                if (_sceneStack.Count > 1)
                {
                    PopScene();
                    return;
                }
            }
        }

        if (_sceneStack.Count > 0)
            _sceneStack.Peek().Update(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_sceneStack.Count > 0)
            _sceneStack.Peek().Draw(spriteBatch);
    }
}
