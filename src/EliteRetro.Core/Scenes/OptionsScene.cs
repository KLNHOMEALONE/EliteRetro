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
    private Game? _game;
    private GameInstance? _gameInstance;
    private KeyboardState _prevKb;
    private Texture2D _whitePixel = null!;

    // Option definitions: name, getter, setter
    private readonly (string name, Func<bool> getter, Action<bool> setter)[] _options;

    public OptionsScene(Game? game = null)
    {
        _game = game;
        if (game is GameInstance gi)
            _gameInstance = gi;

        _options = new (string name, Func<bool> getter, Action<bool> setter)[]
        {
            ("DRAW WHITE",
                () => _gameInstance?.DrawWhite ?? false,
                v => { if (_gameInstance != null) _gameInstance.DrawWhite = v; }),
        };
    }

    public override void LoadContent(ContentManager content, BitmapFont font, GraphicsDevice graphicsDevice)
    {
        _font = font;
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
        // Sync so Enter used to open this menu is not treated as a new press on the first Update
        // (otherwise DRAW WHITE toggles immediately when the scene appears).
        _prevKb = Keyboard.GetState();
    }

    public override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();

        if (kb.IsKeyDown(Keys.Up) && _prevKb.IsKeyUp(Keys.Up))
        {
            _selectedItem = (_selectedItem - 1 + _options.Length) % _options.Length;
            _gameInstance?.Audio.PlayMenuSelect();
        }

        if (kb.IsKeyDown(Keys.Down) && _prevKb.IsKeyUp(Keys.Down))
        {
            _selectedItem = (_selectedItem + 1) % _options.Length;
            _gameInstance?.Audio.PlayMenuSelect();
        }

        if (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter))
        {
            // Toggle selected option
            var (name, getter, setter) = _options[_selectedItem];
            setter(!getter());
            // Save immediately on toggle
            bool val = _gameInstance?.DrawWhite ?? false;
            Systems.OptionsManager.Save(val);
        }

        _prevKb = kb;
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
