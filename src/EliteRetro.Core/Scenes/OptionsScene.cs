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

    private const int ItemDrawWhite = 0;
    private const int ItemDrawInvisible = 1;
    private const int ItemResolution = 2;
    private const int ItemFullscreen = 3;
    private const int ItemCount = 4;

    public OptionsScene(IGameContext? game = null)
    {
        if (game != null)
            _gameInstance = game;
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
            _selectedItem = (_selectedItem - 1 + ItemCount) % ItemCount;
            if (_gameInstance is GameInstance gi)
                gi.Audio.PlayMenuSelect();
        }

        if (input.IsKeyPressed(Keys.Down))
        {
            _selectedItem = (_selectedItem + 1) % ItemCount;
            if (_gameInstance is GameInstance gi)
                gi.Audio.PlayMenuSelect();
        }

        // RESOLUTION row: Left/Right cycles the display mode.
        if (_selectedItem == ItemResolution)
        {
            if (input.IsKeyPressed(Keys.Left))
            {
                _gameInstance.ResolutionIndex -= 1;
                _gameInstance.ApplyDisplayMode();
                PlaySelect();
                SaveOptions();
            }
            else if (input.IsKeyPressed(Keys.Right))
            {
                _gameInstance.ResolutionIndex += 1;
                _gameInstance.ApplyDisplayMode();
                PlaySelect();
                SaveOptions();
            }
        }

        if (input.IsKeyPressed(Keys.Enter))
        {
            switch (_selectedItem)
            {
                case ItemDrawWhite:
                    _gameInstance.DrawWhite = !_gameInstance.DrawWhite;
                    SaveOptions();
                    break;
                case ItemDrawInvisible:
                    _gameInstance.DrawInvisible = !_gameInstance.DrawInvisible;
                    SaveOptions();
                    break;
                case ItemFullscreen:
                    _gameInstance.IsFullScreen = !_gameInstance.IsFullScreen;
                    _gameInstance.ApplyDisplayMode();
                    SaveOptions();
                    break;
            }
        }
    }

    private void PlaySelect()
    {
        if (_gameInstance is GameInstance gi)
            gi.Audio.PlayMenuSelect();
    }

    private void SaveOptions()
    {
        if (_gameInstance == null) return;
        Systems.OptionsManager.Save(
            _gameInstance.DrawWhite,
            _gameInstance.DrawInvisible,
            _gameInstance.ResolutionIndex,
            _gameInstance.IsFullScreen);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        int w = _gameInstance?.VirtualWidth ?? GameInstance.VirtualWidth;
        int h = _gameInstance?.VirtualHeight ?? GameInstance.VirtualHeight;

        // Dark background — full virtual display
        spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, w, h), Color.Black);

        // Title panel — full width, fixed height
        spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, w, 120), new Color(10, 10, 30));

        // Center the title horizontally
        const float titleScale = 2.5f;
        var titleSize = _font.MeasureString("OPTIONS") * titleScale;
        _font.DrawString(spriteBatch, "OPTIONS",
            new Vector2((w - titleSize.X) / 2f, 40), Color.Lime, titleScale);

        // Separator — centered, 70% of width
        int sepW = (int)(w * 0.7f);
        int sepX = (w - sepW) / 2;
        spriteBatch.Draw(_whitePixel, new Rectangle(sepX, 115, sepW, 2), Color.DarkCyan);

        // Layout columns
        const int labelX = 60;           // left margin
        const int rowHeight = 60;
        const int rightMargin = 40;      // space reserved on the right for the value
        int valueX = w - rightMargin;    // right-edge anchor for the value text
        const float valueScale = 1.5f;

        int y = 160;

        // DRAW WHITE
        DrawRow(spriteBatch, "DRAW WHITE", _gameInstance?.DrawWhite == true ? "ON" : "OFF",
            _selectedItem == 0, y, labelX, valueX, valueScale,
            _gameInstance?.DrawWhite == true ? Color.Lime : Color.Gray);
        y += rowHeight;

        // DRAW INVISIBLE
        DrawRow(spriteBatch, "DRAW INVISIBLE", _gameInstance?.DrawInvisible == true ? "ON" : "OFF",
            _selectedItem == 1, y, labelX, valueX, valueScale,
            _gameInstance?.DrawInvisible == true ? Color.Lime : Color.Gray);
        y += rowHeight;

        // RESOLUTION
        var resLabel = FormatResolution(_gameInstance);
        DrawRow(spriteBatch, "RESOLUTION", resLabel, _selectedItem == 2, y, labelX, valueX, valueScale,
            _selectedItem == 2 ? Color.Cyan : Color.White);
        y += rowHeight;

        // FULLSCREEN
        DrawRow(spriteBatch, "FULLSCREEN", _gameInstance?.IsFullScreen == true ? "ON" : "OFF",
            _selectedItem == 3, y, labelX, valueX, valueScale,
            _gameInstance?.IsFullScreen == true ? Color.Lime : Color.Gray);
        y += rowHeight;

        // Bottom separator
        int sepY = y + 10;
        spriteBatch.Draw(_whitePixel, new Rectangle(sepX, sepY, sepW, 2), Color.DarkCyan);

        // Instructions — centered
        string instructions = _selectedItem == ItemResolution
            ? "UP/DOWN: SELECT   LEFT/RIGHT: CHANGE   ESC: BACK"
            : "UP/DOWN: SELECT   ENTER: TOGGLE   ESC: BACK";
        var instrSize = _font.MeasureString(instructions);
        _font.DrawString(spriteBatch, instructions,
            new Vector2((w - instrSize.X) / 2f, sepY + 30), Color.Gray, 1.0f);

        spriteBatch.End();
    }

    private void DrawRow(SpriteBatch spriteBatch, string label, string value, bool selected,
        int y, int labelX, int valueRightX, float valueScale, Color valueColor)
    {
        var color = selected ? Color.Yellow : Color.White;
        var prefix = selected ? "> " : "  ";
        _font.DrawString(spriteBatch, $"{prefix}{label}", new Vector2(labelX, y), color, 1.5f);

        // Right-align the value text (with "< " and " >" decorations) at valueRightX
        string decorated = $"< {value} >";
        var valueSize = _font.MeasureString(decorated) * valueScale;
        _font.DrawString(spriteBatch, decorated,
            new Vector2(valueRightX - valueSize.X, y), valueColor, valueScale);
    }

    private static string FormatResolution(IGameContext? ctx)
    {
        if (ctx == null || ctx.SupportedResolutions.Count == 0)
            return $"{ctx?.DisplayWidth}x{ctx?.DisplayHeight}";
        var (w, h) = ctx.SupportedResolutions[ctx.ResolutionIndex];
        return $"{w}x{h}";
    }

    public override void UnloadContent()
    {
        _whitePixel?.Dispose();
    }
}
