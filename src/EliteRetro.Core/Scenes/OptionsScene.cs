using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EliteRetro.Core.Scenes;

/// <summary>
/// Options submenu — toggleable game settings.
/// Pushed on top of MainMenuScene; ESC returns to main menu.
/// </summary>
public class OptionsScene : GameScene
{
    private BitmapFont _font = null!;
    private int _selectedItem;
    private IGameContext? _gameInstance;
    private Texture2D _whitePixel = null!;

    // Option definitions: name, getter, setter
    private readonly (string name, Func<bool> getter, Action<bool> setter)[] _options;

    public OptionsScene(IGameContext? game = null)
    {
        if (game != null)
            _gameInstance = game;

        _options = new (string name, Func<bool> getter, Action<bool> setter)[]
        {
            ("DRAW WHITE",
                () => _gameInstance?.DrawWhite ?? false,
                v => { if (_gameInstance != null) _gameInstance.DrawWhite = v; }),
            ("DRAW INVISIBLE",
                () => _gameInstance?.DrawInvisible ?? false,
                v => { if (_gameInstance != null) _gameInstance.DrawInvisible = v; }),
        };
    }

    public override void LoadContent(ContentManager content, BitmapFont font, GraphicsDevice graphicsDevice)
    {
        _font = font;
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public override void Update(GameTime gameTime)
    {
        if (_gameInstance == null) return;
        var input = _gameInstance.Input;

        if (input.IsKeyPressed(Keys.Up))
        {
            _selectedItem = (_selectedItem - 1 + _options.Length) % _options.Length;
            if (_gameInstance is GameInstance gi)
                gi.Audio.PlayMenuSelect();
        }

        if (input.IsKeyPressed(Keys.Down))
        {
            _selectedItem = (_selectedItem + 1) % _options.Length;
            if (_gameInstance is GameInstance gi)
                gi.Audio.PlayMenuSelect();
        }

        if (input.IsKeyPressed(Keys.Enter))
        {
            // Toggle selected option
            var (name, getter, setter) = _options[_selectedItem];
            setter(!getter());
            // Save immediately on toggle
            bool drawWhite = _gameInstance?.DrawWhite ?? false;
            bool drawInvisible = _gameInstance?.DrawInvisible ?? false;
            Systems.OptionsManager.Save(drawWhite, drawInvisible);
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        // Dark background
        spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, 1024, 768), Color.Black);

        // Title panel
        spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, 1024, 120), new Color(10, 10, 30));
        _font.DrawString(spriteBatch, "OPTIONS", new Vector2(420, 40), Color.Lime, 2.5f);

        // Separator
        spriteBatch.Draw(_whitePixel, new Rectangle(200, 115, 624, 2), Color.DarkCyan);

        // Options list
        for (int i = 0; i < _options.Length; i++)
        {
            int y = 160 + i * 60;
            var (name, getter, setter) = _options[i];
            bool value = getter();
            bool selected = i == _selectedItem;

            var color = selected ? Color.Yellow : Color.White;
            var prefix = selected ? "> " : "  ";
            var stateText = value ? "ON" : "OFF";
            var stateColor = value ? Color.Lime : Color.Gray;

            _font.DrawString(spriteBatch, $"{prefix}{name}", new Vector2(300, y), color, 1.5f);
            _font.DrawString(spriteBatch, $"[{stateText}]", new Vector2(700, y), stateColor, 1.5f);
        }

        // Separator
        int sepY = 160 + _options.Length * 60 + 10;
        spriteBatch.Draw(_whitePixel, new Rectangle(200, sepY, 624, 2), Color.DarkCyan);

        // Instructions
        _font.DrawString(spriteBatch, "UP/DOWN: SELECT   ENTER: TOGGLE   ESC: BACK",
            new Vector2(280, sepY + 30), Color.Gray, 1.0f);

        spriteBatch.End();
    }

    public override void UnloadContent()
    {
        _whitePixel?.Dispose();
    }
}
