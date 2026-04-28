using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EliteRetro.Core.Systems;

namespace EliteRetro.Core.Scenes;

public class GalaxyMapScene : GameScene
{
    private Galaxy[] _galaxies = null!;
    private BitmapFont _font = null!;
    private Texture2D _whitePixel = null!;
    private int _currentGalaxy;
    private Vector2 _scrollOffset;
    private float _zoom = 0.3f;
    private StarSystem? _hoveredSystem;
    private MouseState _mouseState;

    public GalaxyMapScene()
    {
    }

    public override void LoadContent(ContentManager content, BitmapFont font)
    {
        _font = font;
        var generator = new GalaxyGenerator();
        _galaxies = generator.GenerateAllGalaxies();
        _currentGalaxy = 0;
        // Center galaxy on screen: coords span 0-255 x 0-127, center at (128, 64)
        // Zoom 3.0 makes 256 units ≈ 768 pixels wide, fitting 1024x768
        _zoom = 3.0f;
        _scrollOffset = new Vector2(1024 / 2 - 128 * _zoom, 768 / 2 - 64 * _zoom);

        // Pre-create white pixel for drawing rectangles efficiently
        _whitePixel = new Texture2D(font.Atlas.GraphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();
        _mouseState = mouse;

        if (kb.IsKeyDown(Keys.Left)) _currentGalaxy = (_currentGalaxy - 1 + 8) % 8;
        if (kb.IsKeyDown(Keys.Right)) _currentGalaxy = (_currentGalaxy + 1) % 8;

        if (kb.IsKeyDown(Keys.OemPlus) || kb.IsKeyDown(Keys.Add)) _zoom *= 1.1f;
        if (kb.IsKeyDown(Keys.OemMinus) || kb.IsKeyDown(Keys.Subtract)) _zoom /= 1.1f;

        if (kb.IsKeyDown(Keys.Up)) _scrollOffset.Y -= 5f;
        if (kb.IsKeyDown(Keys.Down)) _scrollOffset.Y += 5f;

        // Escape handled by SceneManager — pops back to previous scene

        _hoveredSystem = null;
        var galaxy = _galaxies[_currentGalaxy];
        foreach (var system in galaxy.Systems)
        {
            var screenPos = WorldToScreen(system.Position);
            if (Vector2.Distance(screenPos, new Vector2(mouse.X, mouse.Y)) < 15)
            {
                _hoveredSystem = system;
                break;
            }
        }
    }

    private Vector2 WorldToScreen(Vector2 worldPos)
        => new Vector2(worldPos.X * _zoom + _scrollOffset.X, worldPos.Y * _zoom + _scrollOffset.Y);

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin();

        _font.DrawString(spriteBatch, $"GALAXY {_currentGalaxy + 1}/8", new Vector2(10, 10), Color.Lime, 1.5f);
        _font.DrawString(spriteBatch, "Left/Right: Galaxy  Up/Down: Pan  +/-: Zoom  ESC: Back", new Vector2(10, 35), Color.White, 1f);

        var galaxy = _galaxies[_currentGalaxy];
        foreach (var system in galaxy.Systems)
        {
            var sp = WorldToScreen(system.Position);
            if (sp.X < -10 || sp.X > 1034 || sp.Y < -10 || sp.Y > 778) continue;

            Color color = system.Economy switch
            {
                EconomyType.RichIndustrial => Color.Yellow,
                EconomyType.AverageIndustrial => Color.Orange,
                EconomyType.PoorIndustrial => Color.Red,
                EconomyType.MainlyIndustrial => Color.OrangeRed,
                EconomyType.RichAgricultural => Color.Green,
                EconomyType.AverageAgricultural => Color.Cyan,
                EconomyType.PoorAgricultural => Color.Blue,
                EconomyType.MainlyAgricultural => Color.LightGreen,
                _ => Color.Gray
            };

            spriteBatch.Draw(_whitePixel, new Rectangle((int)sp.X - 2, (int)sp.Y - 2, 4, 4), color);

            if (_hoveredSystem == system)
                _font.DrawString(spriteBatch, system.Name, sp + new Vector2(5, -5), Color.White, 1f);
        }

        if (_hoveredSystem.HasValue)
        {
            var s = _hoveredSystem.Value;
            int y = 60;
            _font.DrawString(spriteBatch, s.Name, new Vector2(10, y), Color.Yellow, 1f); y += 20;
            _font.DrawString(spriteBatch, $"Gov: {s.Government}  Eco: {s.Economy}", new Vector2(10, y), Color.White, 1f); y += 20;
            _font.DrawString(spriteBatch, $"Tech: {s.TechLevel}  Pop: {s.Population}M", new Vector2(10, y), Color.White, 1f); y += 20;
            _font.DrawString(spriteBatch, $"Radius: {s.Radius}km", new Vector2(10, y), Color.White, 1f);
        }

        // Draw crosshair at mouse position
        spriteBatch.Draw(_whitePixel, new Rectangle(_mouseState.X - 1, _mouseState.Y - 10, 2, 20), Color.White);
        spriteBatch.Draw(_whitePixel, new Rectangle(_mouseState.X - 10, _mouseState.Y - 1, 20, 2), Color.White);

        spriteBatch.End();
    }

    public override void UnloadContent()
    {
        _whitePixel?.Dispose();
    }
}
