using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EliteRetro.Core.Systems;
using EliteRetro.Core;

namespace EliteRetro.Core.Scenes;

public class GalaxyMapScene : GameScene
{
    /// <summary>
    /// BBC Elite scaling: distance (LY) ≈ 0.4 × √(dx² + dy²), where dx/dy are galactic coordinate deltas.
    /// </summary>
    private const float LightYearsPerCoordUnit = 0.4f;
    // The original UI renders the jump range circle a bit “larger” than the numeric distance scaling suggests.
    // Keep distance readout authentic (0.4), but use a slightly different visual scale for the ring.
    private const float JumpCircleLightYearsPerCoordUnit = 0.2f;
    private const int CircleSteps = 64;

    private Galaxy[] _galaxies = null!;
    private BitmapFont _font = null!;
    private Texture2D _whitePixel = null!;
    private int _screenW;
    private int _screenH;
    private int _currentGalaxy;
    private Vector2 _scrollOffset;
    private float _zoom = 0.3f;
    private StarSystem? _originSystem;   // current ship position (Lave)
    private StarSystem? _cursorSystem;   // destination under cursor
    private StarSystem? _lockedSystem;   // locked destination (Enter)
    private GameInstance? _game;
    private KeyboardState _prevKb;

    public GalaxyMapScene(GameInstance? game = null)
    {
        _game = game;
    }

    public override void LoadContent(ContentManager content, BitmapFont font, GraphicsDevice graphicsDevice)
    {
        _font = font;
        _screenW = graphicsDevice.Viewport.Width;
        _screenH = graphicsDevice.Viewport.Height;
        var generator = new GalaxyGenerator();
        _galaxies = generator.GenerateAllGalaxies();
        _currentGalaxy = 0;
        // Center galaxy on screen: coords span 0-255 x 0-127, center at (128, 64)
        // Zoom 3.0 makes 256 units ≈ 768 pixels wide, fitting 1024x768
        _zoom = 3.0f;
        _scrollOffset = new Vector2(_screenW / 2f - 128 * _zoom, _screenH / 2f - 64 * _zoom);

        // Pre-create white pixel for drawing rectangles efficiently
        _whitePixel = new Texture2D(font.Atlas.GraphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
        _prevKb = Keyboard.GetState();

        // Current ship position is Lave. Find it and initialize cursor there.
        _originSystem = FindSystemByName(_galaxies[_currentGalaxy], "Lave") ?? _galaxies[_currentGalaxy].Systems[0];
        _cursorSystem = _originSystem;
        _lockedSystem = null;
    }

    public override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        // No panning. Cursor keys move destination selection (snap to stars).

        if (kb.IsKeyDown(Keys.OemPlus) || kb.IsKeyDown(Keys.Add)) _zoom *= 1.1f;
        if (kb.IsKeyDown(Keys.OemMinus) || kb.IsKeyDown(Keys.Subtract)) _zoom /= 1.1f;

        // Escape handled by SceneManager — pops back to previous scene

        var galaxy = _galaxies[_currentGalaxy];

        if (_cursorSystem.HasValue)
        {
            if (kb.IsKeyDown(Keys.Left) && _prevKb.IsKeyUp(Keys.Left))
                _cursorSystem = SelectInDirection(galaxy, _cursorSystem.Value, dxSign: -1, dySign: 0);
            if (kb.IsKeyDown(Keys.Right) && _prevKb.IsKeyUp(Keys.Right))
                _cursorSystem = SelectInDirection(galaxy, _cursorSystem.Value, dxSign: 1, dySign: 0);
            if (kb.IsKeyDown(Keys.Up) && _prevKb.IsKeyUp(Keys.Up))
                _cursorSystem = SelectInDirection(galaxy, _cursorSystem.Value, dxSign: 0, dySign: -1);
            if (kb.IsKeyDown(Keys.Down) && _prevKb.IsKeyUp(Keys.Down))
                _cursorSystem = SelectInDirection(galaxy, _cursorSystem.Value, dxSign: 0, dySign: 1);
        }

        // Enter locks on destination only if within jump range.
        if (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter) && _originSystem.HasValue && _cursorSystem.HasValue)
        {
            float fuel = Math.Clamp(_game?.BubbleManager?.Commander?.Fuel ?? 35, 0, 70);
            float jumpRangeLy = fuel / 10f; // original Elite max 7.0 LY
            var d = _cursorSystem.Value.Position - _originSystem.Value.Position;
            float coordDist = MathF.Sqrt(d.X * d.X + d.Y * d.Y);
            float maxCoordDist = jumpRangeLy / JumpCircleLightYearsPerCoordUnit;
            _lockedSystem = coordDist <= maxCoordDist ? _cursorSystem : null;
        }

        if (kb.IsKeyDown(Keys.I) && _prevKb.IsKeyUp(Keys.I) && _cursorSystem.HasValue && _originSystem.HasValue && _game != null)
        {
            float ly = DistanceLightYears(_originSystem.Value, _cursorSystem.Value);
            _game.PushScene(new GalaxyStarDescriptionScene(_cursorSystem.Value, ly));
        }

        _prevKb = kb;
    }

    private static float DistanceLightYears(StarSystem from, StarSystem to)
    {
        var d = to.Position - from.Position;
        return MathF.Sqrt(d.X * d.X + d.Y * d.Y) * LightYearsPerCoordUnit;
    }

    private Vector2 WorldToScreen(Vector2 worldPos)
        => new Vector2(worldPos.X * _zoom + _scrollOffset.X, worldPos.Y * _zoom + _scrollOffset.Y);

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin();

        _font.DrawString(spriteBatch, $"GALAXY {_currentGalaxy + 1}/8", new Vector2(10, 10), Color.Lime, 1.5f);
        _font.DrawString(spriteBatch, "CURSOR KEYS: SELECT  ENTER: LOCK  +/-: ZOOM  I: DATA  ESC: BACK", new Vector2(10, 35), Color.White, 1f);

        var galaxy = _galaxies[_currentGalaxy];
        foreach (var system in galaxy.Systems)
        {
            var sp = WorldToScreen(system.Position);
            if (sp.X < -10 || sp.X > _screenW + 10 || sp.Y < -10 || sp.Y > _screenH + 10) continue;

            // Reference look: stars are plain white points.
            spriteBatch.Draw(_whitePixel, new Rectangle((int)sp.X - 1, (int)sp.Y - 1, 2, 2), Color.White);

            if (_cursorSystem.HasValue && _cursorSystem.Value.SystemIndex == system.SystemIndex)
                _font.DrawString(spriteBatch, system.Name, sp + new Vector2(5, -5), Color.White, 1f);
        }

        // Jump range circle (fuel-based), centered on current system (Lave).
        if (_originSystem.HasValue)
        {
            int fuel = _game?.BubbleManager?.Commander?.Fuel ?? 35;
            fuel = Math.Clamp(fuel, 0, 70);
            float jumpRangeLy = fuel / 10f;
            float radiusCoords = jumpRangeLy / JumpCircleLightYearsPerCoordUnit;
            float radiusPixels = radiusCoords * _zoom;
            if (radiusPixels >= 2f)
            {
                var originScreen = WorldToScreen(_originSystem.Value.Position);
                DrawCircle(spriteBatch, originScreen, radiusPixels, Color.White);
            }
        }

        // Draw destination cursor crosshair on selected system.
        if (_cursorSystem.HasValue)
        {
            var c = WorldToScreen(_cursorSystem.Value.Position);
            spriteBatch.Draw(_whitePixel, new Rectangle((int)c.X - 1, (int)c.Y - 10, 2, 20), Color.White);
            spriteBatch.Draw(_whitePixel, new Rectangle((int)c.X - 10, (int)c.Y - 1, 20, 2), Color.White);
        }

        // Bottom line: selected system + distance.
        if (_cursorSystem.HasValue && _originSystem.HasValue)
        {
            float dist = DistanceLightYears(_originSystem.Value, _cursorSystem.Value);
            string name = _cursorSystem.Value.Name.ToUpperInvariant();
            // Lift readout off the bottom edge (closer to reference bottom bar).
            int readoutTopY = Math.Max(0, _screenH - 64);
            _font.DrawString(spriteBatch, $"{name}", new Vector2(10, readoutTopY), Color.White, 1.2f);
            _font.DrawString(spriteBatch, $"Distance: {dist:F1} Light Years", new Vector2(10, readoutTopY + 22), Color.White, 1.0f);

            if (_lockedSystem.HasValue)
            {
                const string lockedText = "LOCKED";
                const int pad = 10;
                float lockedScale = 1.1f;
                float textW = _font.MeasureString(lockedText).X * lockedScale;
                float x = MathF.Max(pad, _screenW - pad - textW);
                _font.DrawString(spriteBatch, lockedText, new Vector2(x, readoutTopY + 10), Color.Cyan, lockedScale);
            }
        }

        spriteBatch.End();
    }

    private void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
    {
        Vector2 prev = center + new Vector2(radius, 0);
        for (int i = 1; i <= CircleSteps; i++)
        {
            float a = MathHelper.TwoPi * i / CircleSteps;
            var next = center + new Vector2(MathF.Cos(a) * radius, MathF.Sin(a) * radius);
            DrawLine(spriteBatch, prev, next, color, 1f);
            prev = next;
        }
    }

    private void DrawLine(SpriteBatch spriteBatch, Vector2 a, Vector2 b, Color color, float thickness)
    {
        var delta = b - a;
        float len = delta.Length();
        if (len <= 0.01f) return;
        float rot = MathF.Atan2(delta.Y, delta.X);
        spriteBatch.Draw(_whitePixel, a, null, color, rot, Vector2.Zero, new Vector2(len, thickness), SpriteEffects.None, 0f);
    }

    private static StarSystem? FindSystemByName(Galaxy galaxy, string name)
    {
        for (int i = 0; i < galaxy.Systems.Length; i++)
        {
            if (string.Equals(galaxy.Systems[i].Name, name, StringComparison.OrdinalIgnoreCase))
                return galaxy.Systems[i];
        }
        return null;
    }

    private static StarSystem SelectInDirection(Galaxy galaxy, StarSystem from, int dxSign, int dySign)
    {
        StarSystem best = from;
        bool found = false;
        float bestScore = float.MaxValue;

        for (int i = 0; i < galaxy.Systems.Length; i++)
        {
            var s = galaxy.Systems[i];
            if (s.SystemIndex == from.SystemIndex) continue;

            float dx = s.Position.X - from.Position.X;
            float dy = s.Position.Y - from.Position.Y;

            if (dxSign != 0)
            {
                if (dxSign < 0 && dx >= 0) continue;
                if (dxSign > 0 && dx <= 0) continue;
            }
            if (dySign != 0)
            {
                if (dySign < 0 && dy >= 0) continue;
                if (dySign > 0 && dy <= 0) continue;
            }

            float absDx = MathF.Abs(dx);
            float absDy = MathF.Abs(dy);
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            // Bias toward the chosen axis to mimic classic “snap” cursor movement.
            float axisPenalty = (dxSign != 0) ? absDy * 3f : absDx * 3f;
            float score = dist + axisPenalty;

            if (score < bestScore)
            {
                bestScore = score;
                best = s;
                found = true;
            }
        }

        return found ? best : from;
    }

    public override void UnloadContent()
    {
        _whitePixel?.Dispose();
    }
}
