using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EliteRetro.Core.Rendering;

/// <summary>
/// Sun rendering with horizontal scan lines and random fringe.
/// Authentic Elite-style sun: filled with horizontal lines, glowing edge particles.
/// </summary>
public class SunRenderer
{
    private readonly Texture2D _whitePixel;

    // Sun color schemes based on temperature (original Elite: yellow/white/blue)
    private static readonly Color[] SunColors =
    {
        new Color(255, 200, 50),   // F9E432 - yellow (cool)
        new Color(255, 255, 200),  // FFFFC8 - white (medium)
        new Color(150, 200, 255),  // 96C8FF - blue (hot)
    };

    public SunRenderer(GraphicsDevice graphicsDevice)
    {
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Get sun color by type (0=yellow, 1=white, 2=blue).
    /// </summary>
    public static Color GetSunColor(int type)
    {
        return SunColors[Math.Clamp(type, 0, SunColors.Length - 1)];
    }

    /// <summary>
    /// Draw a sun with horizontal scan lines and fringe glow.
    /// </summary>
    /// <param name="spriteBatch">Active SpriteBatch.</param>
    /// <param name="center">Screen-space center position.</param>
    /// <param name="radius">Sun radius in pixels.</param>
    /// <param name="color">Sun color (use GetSunColor for type-based color).</param>
    public void DrawSun(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
    {
        if (radius <= 0) return;

        DrawScanLines(spriteBatch, center, radius, color);
        DrawFringe(spriteBatch, center, radius, color);
    }

    private void DrawScanLines(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
    {
        int r = (int)MathF.Ceiling(radius);

        // Draw horizontal scan lines with gap pattern (every other line, Elite-style)
        for (int dy = -r; dy <= r; dy += 2)
        {
            float halfWidth = MathF.Sqrt(MathF.Max(0, radius * radius - dy * dy));
            float left = center.X - halfWidth;
            float right = center.X + halfWidth;
            int x = (int)left;
            int width = (int)(right - left);

            if (width > 0)
            {
                // Vary brightness: center brighter, edges dimmer
                float distanceFromCenter = MathF.Abs(dy) / radius;
                float brightness = 1.0f - distanceFromCenter * 0.4f;
                Color lineColor = new Color(
                    (int)(color.R * brightness),
                    (int)(color.G * brightness),
                    (int)(color.B * brightness),
                    color.A);

                spriteBatch.Draw(_whitePixel,
                    new Rectangle(x, (int)center.Y + dy, width, 1),
                    lineColor);
            }
        }
    }

    private void DrawFringe(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
    {
        // Deterministic fringe particles based on screen position to avoid flicker.
        // Use center coordinates as seed so particles stay fixed relative to the sun.
        int seed = HashCode.Combine((int)center.X, (int)center.Y, (int)radius);
        Random rng = new Random(seed);
        int fringeCount = (int)(radius * 1.5f); // scale with size

        for (int i = 0; i < fringeCount; i++)
        {
            int step = rng.Next(64);
            var (sin, cos) = SineTable.SinCos(step);

            // Random distance: mostly near edge, some further out
            float dist = radius + (float)rng.NextDouble() * radius * 0.3f;
            Vector2 pos = center + new Vector2(cos * dist, sin * dist);

            // Random size (1-3 pixels)
            int size = rng.Next(1, 4);

            // Fading brightness for outer particles
            float fade = 0.3f + (float)rng.NextDouble() * 0.7f;
            Color fringeColor = new Color(
                (int)(color.R * fade),
                (int)(color.G * fade),
                (int)(color.B * fade),
                (int)(color.A * fade * 0.6f));

            spriteBatch.Draw(_whitePixel,
                new Rectangle((int)pos.X, (int)pos.Y, size, size),
                fringeColor);
        }
    }
}
