using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EliteRetro.Core.Rendering;

/// <summary>
/// Saturn-style ring rendering using concentric elliptical bands with scattered particle texture.
/// Concentric ellipses define ring structure; random points add surface texture.
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
    /// Draw rings around a planet using concentric elliptical bands with scattered point texture.
    /// </summary>
    /// <param name="spriteBatch">Active SpriteBatch.</param>
    /// <param name="center">Screen-space center of the planet.</param>
    /// <param name="planetRadius">Planet radius in pixels.</param>
    /// <param name="innerRadius">Inner ring radius as multiple of planet radius (default 1.4x).</param>
    /// <param name="outerRadius">Outer ring radius as multiple of planet radius (default 2.2x).</param>
    /// <param name="color">Ring color.</param>
    /// <param name="tiltAngle">Ring tilt in 1/64-turn units (0-63). Default 16 = 90 degrees (horizontal).</param>
    /// <param name="layer">Which half to draw: "back" for behind planet, "front" for in front, "all" for both.</param>
    public void DrawRings(
        SpriteBatch spriteBatch,
        Vector2 center,
        float planetRadius,
        float innerRadius,
        float outerRadius,
        Color color,
        int tiltAngle = 16,
        string layer = "all")
    {
        if (planetRadius <= 0 || outerRadius <= innerRadius) return;

        float inner = planetRadius * innerRadius;
        float outer = planetRadius * outerRadius;

        var (sin, cos) = SineTable.SinCos(tiltAngle);

        // Draw concentric elliptical rings for structure
        int ringCount = Math.Clamp((int)((outer - inner) / 8), 3, 6);
        for (int r = 0; r < ringCount; r++)
        {
            float t = inner + (outer - inner) * r / Math.Max(ringCount - 1, 1);
            DrawEllipticalRing(spriteBatch, center, t, color, sin, cos, planetRadius, layer);
        }

        // Draw scattered points for texture
        int pointCount = (int)((outer * outer - inner * inner) * 0.08f);
        pointCount = Math.Clamp(pointCount, 20, 150);

        // Seed from screen position for stable particles (no flicker)
        int seed = HashCode.Combine((int)center.X, (int)center.Y, (int)planetRadius, tiltAngle);
        Random rng = new Random(seed);

        for (int i = 0; i < pointCount; i++)
        {
            float angle = (float)rng.NextDouble() * MathHelper.TwoPi;
            float radius = inner + (float)rng.NextDouble() * (outer - inner);

            float ex = (float)Math.Cos(angle) * radius;
            float ey = (float)Math.Sin(angle) * radius * 0.4f;

            // Only draw requested layer
            bool isBack = ey < 0;
            if (layer == "front" && isBack) continue;
            if (layer == "back" && !isBack) continue;

            float rx = cos * ex - sin * ey;
            float ry = sin * ex + cos * ey;

            Vector2 pos = center + new Vector2(rx, ry);

            // Occlusion: behind planet disk (only applies to back layer)
            float screenDistSq = rx * rx + ry * ry;
            if (isBack && screenDistSq < planetRadius * planetRadius)
                continue;

            int size = rng.Next(1, 3);
            float brightness = isBack ? 0.3f : 0.6f;
            brightness += 0.2f * (float)rng.NextDouble();
            Color pointColor = new Color(
                (int)(color.R * brightness),
                (int)(color.G * brightness),
                (int)(color.B * brightness),
                (int)(color.A * (isBack ? 0.4f : 0.6f)));

            spriteBatch.Draw(_whitePixel,
                new Rectangle((int)pos.X, (int)pos.Y, size, size),
                pointColor);
        }
    }

    /// <summary>
    /// Draw a single elliptical ring band with planet occlusion.
    /// Rings share the planet's center and lie in the equator plane.
    /// Front half (ey >= 0) passes in front of planet; back half passes behind.
    /// </summary>
    private void DrawEllipticalRing(
        SpriteBatch spriteBatch,
        Vector2 center,
        float radius,
        Color color,
        float sin,
        float cos,
        float planetRadius,
        string layer)
    {
        const int segments = 64;
        float prevScreenX = 0, prevScreenY = 0;
        bool prevValid = false;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i * MathHelper.TwoPi / segments;
            float ex = (float)Math.Cos(angle) * radius;
            float ey = (float)Math.Sin(angle) * radius * 0.4f;

            // Only draw requested layer
            bool isBack = ey < 0;
            if (layer == "front" && isBack)
            {
                prevValid = false;
                continue;
            }
            if (layer == "back" && !isBack)
            {
                prevValid = false;
                continue;
            }

            // Rotate into tilt
            float sx = cos * ex - sin * ey;
            float sy = sin * ex + cos * ey;

            // Occlusion: back half segments behind planet disk are hidden
            float distSq = sx * sx + sy * sy;
            if (isBack && distSq < planetRadius * planetRadius)
            {
                prevValid = false;
                continue;
            }

            Vector2 pos = center + new Vector2(sx, sy);

            if (prevValid)
            {
                Vector2 prevPos = center + new Vector2(prevScreenX, prevScreenY);
                float brightness = isBack ? 0.3f : 0.7f;
                brightness += 0.2f * (float)Math.Sin(angle * 3 + radius * 0.1f);
                Color segColor = new Color(
                    (int)(color.R * brightness),
                    (int)(color.G * brightness),
                    (int)(color.B * brightness),
                    (int)(color.A * (isBack ? 0.5f : 0.8f)));

                DrawLine(spriteBatch, prevPos, pos, segColor, 2);
            }

            prevScreenX = sx;
            prevScreenY = sy;
            prevValid = true;
        }
    }

    /// <summary>
    /// Draw a line segment using the white pixel texture.
    /// </summary>
    private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, int thickness)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length < 1) return;

        float angle = (float)Math.Atan2(delta.Y, delta.X);
        Vector2 origin = new Vector2(0, thickness / 2f);

        spriteBatch.Draw(_whitePixel, start,
            new Rectangle(0, 0, (int)length, thickness),
            color, angle, origin, 1f, SpriteEffects.None, 0);
    }

    /// <summary>
    /// Draw rings with optional tilt rotation.
    /// </summary>
    /// <param name="tiltAngle">Ring tilt in 1/64-turn units (0-63). Default 16 = 90 degrees (horizontal).</param>
    /// <param name="layer">Which half to draw: "back" for behind planet, "front" for in front, "all" for both.</param>
    public void DrawAxisAlignedRings(
        SpriteBatch spriteBatch,
        Vector2 center,
        float planetRadius,
        float innerRadius,
        float outerRadius,
        Color color,
        int tiltAngle = 16,
        string layer = "all")
    {
        if (planetRadius <= 0 || outerRadius <= innerRadius) return;

        float inner = planetRadius * innerRadius;
        float outer = planetRadius * outerRadius;

        var (sin, cos) = SineTable.SinCos(tiltAngle);

        // Draw concentric elliptical rings for structure
        int ringCount = Math.Clamp((int)((outer - inner) / 8), 3, 6);
        for (int r = 0; r < ringCount; r++)
        {
            float t = inner + (outer - inner) * r / Math.Max(ringCount - 1, 1);
            DrawEllipticalRing(spriteBatch, center, t, color, sin, cos, planetRadius, layer);
        }

        // Draw scattered points for texture
        int pointCount = (int)((outer * outer - inner * inner) * 0.08f);
        pointCount = Math.Clamp(pointCount, 20, 150);

        // Seed from screen position for stable particles (no flicker)
        int seed = HashCode.Combine((int)center.X, (int)center.Y, (int)planetRadius, tiltAngle);
        Random rng = new Random(seed);

        for (int i = 0; i < pointCount; i++)
        {
            float angle = (float)rng.NextDouble() * MathHelper.TwoPi;
            float radius = inner + (float)rng.NextDouble() * (outer - inner);

            float ex = (float)Math.Cos(angle) * radius;
            float ey = (float)Math.Sin(angle) * radius * 0.4f;

            // Rotate into tilt
            float rx = cos * ex - sin * ey;
            float ry = sin * ex + cos * ey;

            // Only draw requested layer
            bool isBack = ry < 0;
            if (layer == "front" && isBack) continue;
            if (layer == "back" && !isBack) continue;

            Vector2 pos = center + new Vector2(rx, ry);

            // Occlusion: back half segments behind planet disk are hidden
            float screenDistSq = rx * rx + ry * ry;
            if (isBack && screenDistSq < planetRadius * planetRadius)
                continue;

            int size = rng.Next(1, 3);
            float brightness = isBack ? 0.3f : 0.6f;
            brightness += 0.2f * (float)rng.NextDouble();
            Color pointColor = new Color(
                (int)(color.R * brightness),
                (int)(color.G * brightness),
                (int)(color.B * brightness),
                (int)(color.A * (isBack ? 0.4f : 0.6f)));

            spriteBatch.Draw(_whitePixel,
                new Rectangle((int)pos.X, (int)pos.Y, size, size),
                pointColor);
        }
    }

    /// <summary>
    /// Draw a single axis-aligned elliptical ring band with planet occlusion.
    /// Rings share the planet's center and lie in the equator plane.
    /// Front half (ey >= 0) passes in front of planet; back half passes behind.
    /// </summary>
    private void DrawAxisAlignedEllipticalRing(
        SpriteBatch spriteBatch,
        Vector2 center,
        float radius,
        Color color,
        float planetRadius,
        string layer)
    {
        const int segments = 64;
        float prevScreenX = 0, prevScreenY = 0;
        bool prevValid = false;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i * MathHelper.TwoPi / segments;
            float ex = (float)Math.Cos(angle) * radius;
            float ey = (float)Math.Sin(angle) * radius * 0.4f;

            // Only draw requested layer
            bool isBack = ey < 0;
            if (layer == "front" && isBack)
            {
                prevValid = false;
                continue;
            }
            if (layer == "back" && !isBack)
            {
                prevValid = false;
                continue;
            }

            // Occlusion: back half segments behind planet disk are hidden
            float distSq = ex * ex + ey * ey;
            if (isBack && distSq < planetRadius * planetRadius)
            {
                prevValid = false;
                continue;
            }

            Vector2 pos = center + new Vector2(ex, ey);

            if (prevValid)
            {
                Vector2 prevPos = center + new Vector2(prevScreenX, prevScreenY);
                float brightness = isBack ? 0.3f : 0.7f;
                brightness += 0.2f * (float)Math.Sin(angle * 3 + radius * 0.1f);
                Color segColor = new Color(
                    (int)(color.R * brightness),
                    (int)(color.G * brightness),
                    (int)(color.B * brightness),
                    (int)(color.A * (isBack ? 0.5f : 0.8f)));

                DrawLine(spriteBatch, prevPos, pos, segColor, 2);
            }

            prevScreenX = ex;
            prevScreenY = ey;
            prevValid = true;
        }
    }
}
