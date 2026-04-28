using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EliteRetro.Core.Rendering;

/// <summary>
/// Parametric circle drawing using SineTable for authentic Elite-style circles.
/// Draws circles via SpriteBatch for compatibility with the scene rendering pipeline.
/// </summary>
public class CircleRenderer
{
    private readonly Texture2D _whitePixel;

    public CircleRenderer(GraphicsDevice graphicsDevice)
    {
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Draw a circle outline.
    /// </summary>
    /// <param name="spriteBatch">Active SpriteBatch (must be between Begin/End).</param>
    /// <param name="center">Screen-space center position.</param>
    /// <param name="radius">Radius in pixels.</param>
    /// <param name="color">Circle color.</param>
    /// <param name="stepCount">Number of line segments (default 32).</param>
    public void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, int stepCount = 32)
    {
        if (radius <= 0) return;

        for (int i = 0; i < stepCount; i++)
        {
            int next = (i + 1) % stepCount;
            var (sin1, cos1) = SineTable.SinCos(i * 64 / stepCount);
            var (sin2, cos2) = SineTable.SinCos(next * 64 / stepCount);

            var p1 = center + new Vector2(cos1 * radius, sin1 * radius);
            var p2 = center + new Vector2(cos2 * radius, sin2 * radius);

            DrawLine(spriteBatch, p1, p2, color);
        }
    }

    /// <summary>
    /// Draw a filled circle using horizontal scan lines.
    /// </summary>
    public void DrawFilledCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
    {
        if (radius <= 0) return;

        int r = (int)MathF.Ceiling(radius);
        for (int dy = -r; dy <= r; dy++)
        {
            float halfWidth = MathF.Sqrt(MathF.Max(0, radius * radius - dy * dy));
            float left = center.X - halfWidth;
            float right = center.X + halfWidth;
            int x = (int)left;
            int width = (int)(right - left);
            if (width > 0)
            {
                spriteBatch.Draw(_whitePixel, new Rectangle(x, (int)center.Y + dy, width, 1), color);
            }
        }
    }

    private void DrawLine(SpriteBatch spriteBatch, Vector2 p1, Vector2 p2, Color color)
    {
        var direction = p2 - p1;
        var length = direction.Length();
        if (length < 0.01f) return;

        var rotation = MathF.Atan2(direction.Y, direction.X);
        spriteBatch.Draw(_whitePixel, p1, null, color, rotation, Vector2.Zero,
            new Vector2(length, 2), SpriteEffects.None, 0);
    }
}
