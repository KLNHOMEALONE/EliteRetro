using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EliteRetro.Core.Scenes;

public class SceneManager
{
    private readonly Stack<GameScene> _sceneStack = new();
    private GameScene? _nextScene;
    private ContentManager _content = null!;
    private BitmapFont _font = null!;
    private Game? _game;

    public void ChangeScene(GameScene newScene, ContentManager content, Game? game = null, BitmapFont? font = null)
    {
        _sceneStack.Clear();
        _content = content;
        _game = game;
        if (font != null) _font = font;

        newScene.LoadContent(content, _font);
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
            _game?.Exit();
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
            _nextScene.LoadContent(_content, _font);
            _sceneStack.Push(_nextScene);
            _nextScene = null;
        }

        // Check for Escape to go back
        var kb = Keyboard.GetState();
        if (kb.IsKeyDown(Keys.Escape) && _sceneStack.Count > 1)
        {
            PopScene();
            return;
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
