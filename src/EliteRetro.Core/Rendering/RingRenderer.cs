using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EliteRetro.Core.Rendering;

/// <summary>
/// Saturn-style ring rendering using random points in an elliptical band.
/// Points behind the planet disk are occluded for authentic depth.
/// </summary>
public class RingRenderer
{
    private readonly Texture2D _whitePixel;

    public RingRenderer(GraphicsDevice graphicsDevice)
    {
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Draw rings around a planet using random points in an elliptical band.
    /// </summary>
    /// <param name="spriteBatch">Active SpriteBatch.</param>
    /// <param name="center">Screen-space center of the planet.</param>
    /// <param name="planetRadius">Planet radius in pixels.</param>
    /// <param name="innerRadius">Inner ring radius as multiple of planet radius (default 1.4x).</param>
    /// <param name="outerRadius">Outer ring radius as multiple of planet radius (default 2.2x).</param>
    /// <param name="color">Ring color.</param>
    /// <param name="tiltAngle">Ring tilt in 1/64-turn units (0-63). Default 16 = 90 degrees (horizontal).</param>
    public void DrawRings(
        SpriteBatch spriteBatch,
        Vector2 center,
        float planetRadius,
        float innerRadius,
        float outerRadius,
        Color color,
        int tiltAngle = 16)
    {
        if (planetRadius <= 0 || outerRadius <= innerRadius) return;

        float inner = planetRadius * innerRadius;
        float outer = planetRadius * outerRadius;

        int pointCount = (int)((outer * outer - inner * inner) * 0.15f);
        pointCount = Math.Clamp(pointCount, 30, 300);

        var (sin, cos) = SineTable.SinCos(tiltAngle);

        // Seed from screen position for stable particles (no flicker)
        int seed = HashCode.Combine((int)center.X, (int)center.Y, (int)planetRadius, tiltAngle);
        Random rng = new Random(seed);

        for (int i = 0; i < pointCount; i++)
        {
            float t = (float)rng.NextDouble() * MathHelper.TwoPi;
            float radius = inner + (float)rng.NextDouble() * (outer - inner);

            float ex = (float)Math.Cos(t) * radius;
            float ey = (float)Math.Sin(t) * radius * 0.4f;

            float rx = cos * ex - sin * ey;
            float ry = sin * ex + cos * ey;

            Vector2 pos = center + new Vector2(rx, ry);

            float distFromCenter = new Vector2(rx, ry * 2.5f).Length();
            if (distFromCenter < planetRadius * 0.8f)
                continue;

            if (ey < -planetRadius * 0.3f)
                continue;

            int size = rng.Next(1, 3);
            float brightness = 0.5f + (float)rng.NextDouble() * 0.5f;
            Color pointColor = new Color(
                (int)(color.R * brightness),
                (int)(color.G * brightness),
                (int)(color.B * brightness),
                (int)(color.A * 0.7f));

            spriteBatch.Draw(_whitePixel,
                new Rectangle((int)pos.X, (int)pos.Y, size, size),
                pointColor);
        }
    }

    /// <summary>
    /// Draw rings with axis-aligned ellipse (no tilt rotation, horizontal rings).
    /// </summary>
    public void DrawAxisAlignedRings(
        SpriteBatch spriteBatch,
        Vector2 center,
        float planetRadius,
        float innerRadius,
        float outerRadius,
        Color color)
    {
        if (planetRadius <= 0 || outerRadius <= innerRadius) return;

        float inner = planetRadius * innerRadius;
        float outer = planetRadius * outerRadius;

        int pointCount = (int)((outer * outer - inner * inner) * 0.15f);
        pointCount = Math.Clamp(pointCount, 30, 300);

        // Seed from screen position for stable particles (no flicker)
        int seed = HashCode.Combine((int)center.X, (int)center.Y, (int)planetRadius);
        Random rng = new Random(seed);

        for (int i = 0; i < pointCount; i++)
        {
            float t = (float)rng.NextDouble() * MathHelper.TwoPi;
            float radius = inner + (float)rng.NextDouble() * (outer - inner);

            float ex = (float)Math.Cos(t) * radius;
            float ey = (float)Math.Sin(t) * radius * 0.4f;

            Vector2 pos = center + new Vector2(ex, ey);

            if (ey < -planetRadius * 0.3f)
                continue;

            int size = rng.Next(1, 3);
            float brightness = 0.5f + (float)rng.NextDouble() * 0.5f;
            Color pointColor = new Color(
                (int)(color.R * brightness),
                (int)(color.G * brightness),
                (int)(color.B * brightness),
                (int)(color.A * 0.7f));

            spriteBatch.Draw(_whitePixel,
                new Rectangle((int)pos.X, (int)pos.Y, size, size),
                pointColor);
        }
    }
}
