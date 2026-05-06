using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EliteRetro.Core.Rendering;

/// <summary>
/// Conjugate-diameter ellipse drawing using parametric equation:
/// P(θ) = center + cos(θ)·u + sin(θ)·v
/// where u and v are conjugate semi-diameter vectors.
/// Used for planet surface features (equator, meridians, crater rims).
/// </summary>
public class EllipseRenderer
{
    private readonly Texture2D _whitePixel;

    public EllipseRenderer(GraphicsDevice graphicsDevice)
    {
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Draw a full ellipse outline using conjugate-diameter vectors.
    /// </summary>
    /// <param name="spriteBatch">Active SpriteBatch.</param>
    /// <param name="center">Screen-space center position.</param>
    /// <param name="u">First conjugate semi-diameter vector.</param>
    /// <param name="v">Second conjugate semi-diameter vector.</param>
    /// <param name="color">Ellipse color.</param>
    /// <param name="stepCount">Number of line segments (default 32).</param>
    public void DrawEllipse(SpriteBatch spriteBatch, Vector2 center, Vector2 u, Vector2 v, Color color, int stepCount = 32)
    {
        DrawEllipseArc(spriteBatch, center, u, v, color, 0, 64, stepCount);
    }

    /// <summary>
    /// Draw an ellipse arc using conjugate-diameter vectors.
    /// </summary>
    /// <param name="startStep">Start angle in 1/64-turn units (0-64 = full circle).</param>
    /// <param name="endStep">End angle in 1/64-turn units.</param>
    public void DrawEllipseArc(SpriteBatch spriteBatch, Vector2 center, Vector2 u, Vector2 v, Color color, int startStep, int endStep, int stepCount = 32)
    {
        if (startStep >= endStep) return;

        // Scale step resolution to arc length
        int totalSteps = Math.Max(4, (endStep - startStep) * stepCount / 64);

        for (int i = 0; i < totalSteps; i++)
        {
            float t1 = (float)(startStep + (endStep - startStep) * i / totalSteps) / 64f;
            float t2 = (float)(startStep + (endStep - startStep) * (i + 1) / totalSteps) / 64f;

            var (sin1, cos1) = SineTable.SinCos((int)(t1 * 64f) & 63);
            var (sin2, cos2) = SineTable.SinCos((int)(t2 * 64f) & 63);

            var p1 = center + cos1 * u + sin1 * v;
            var p2 = center + cos2 * u + sin2 * v;

            DrawLine(spriteBatch, p1, p2, color);
        }
    }

    /// <summary>
    /// Draw an axis-aligned ellipse (convenience method).
    /// </summary>
    /// <param name="spriteBatch">Active SpriteBatch.</param>
    /// <param name="center">Screen-space center position.</param>
    /// <param name="radiusX">Horizontal radius in pixels.</param>
    /// <param name="radiusY">Vertical radius in pixels.</param>
    /// <param name="color">Ellipse color.</param>
    /// <param name="stepCount">Number of line segments (default 32).</param>
    public void DrawAxisAlignedEllipse(SpriteBatch spriteBatch, Vector2 center, float radiusX, float radiusY, Color color, int stepCount = 32)
    {
        DrawEllipse(spriteBatch, center, new Vector2(radiusX, 0), new Vector2(0, radiusY), color, stepCount);
    }

    /// <summary>
    /// Draw a filled ellipse using horizontal scan lines.
    /// </summary>
    public void DrawFilledEllipse(SpriteBatch spriteBatch, Vector2 center, Vector2 u, Vector2 v, Color color)
    {
        // Compute bounding box
        float maxX = u.X * u.X + v.X * v.X;
        float maxY = u.Y * u.Y + v.Y * v.Y;
        float extentX = MathF.Sqrt(maxX);
        float extentY = MathF.Sqrt(maxY);

        int top = (int)(center.Y - extentY);
        int bottom = (int)(center.Y + extentY);

        for (int scanY = top; scanY <= bottom; scanY++)
        {
            // For each scan line, find intersection with ellipse
            // Ellipse: P = center + cos(t)*u + sin(t)*v
            // We need points where P.y = scanY
            // This is equivalent to solving: center.Y + cos(t)*u.Y + sin(t)*v.Y = scanY
            // Let dy = scanY - center.Y, then: cos(t)*u.Y + sin(t)*v.Y = dy
            // This is: R*cos(t - phi) = dy where R = sqrt(u.Y^2 + v.Y^2)
            float dy = scanY - center.Y;
            float r = MathF.Sqrt(u.Y * u.Y + v.Y * v.Y);
            if (r < 0.001f) continue;

            float cosPhi = u.Y / r;
            float sinPhi = v.Y / r;
            float cosAlpha = dy / r;

            if (MathF.Abs(cosAlpha) > 1f) continue;

            float alpha = MathF.Acos(cosAlpha);
            float phi = MathF.Atan2(sinPhi, cosPhi);

            float t1 = phi + alpha;
            float t2 = phi - alpha;

            var (sin1, cos1) = (MathF.Sin(t1), MathF.Cos(t1));
            var (sin2, cos2) = (MathF.Sin(t2), MathF.Cos(t2));

            float x1 = center.X + cos1 * u.X + sin1 * v.X;
            float x2 = center.X + cos2 * u.X + sin2 * v.X;

            float left = MathF.Min(x1, x2);
            float right = MathF.Max(x1, x2);
            int ix = (int)left;
            int width = (int)(right - left);
            if (width > 0)
            {
                spriteBatch.Draw(_whitePixel, new Rectangle(ix, scanY, width, 1), color);
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
