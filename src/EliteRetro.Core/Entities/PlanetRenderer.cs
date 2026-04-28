using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EliteRetro.Core.Rendering;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Planet surface rendering with craters, meridians, and equator.
/// Uses conjugate-diameter ellipses to draw features that rotate with the planet.
/// </summary>
public class PlanetRenderer
{
    private readonly EllipseRenderer _ellipseRenderer;
    private readonly CircleRenderer _circleRenderer;

    public PlanetRenderer(GraphicsDevice graphicsDevice)
    {
        _ellipseRenderer = new EllipseRenderer(graphicsDevice);
        _circleRenderer = new CircleRenderer(graphicsDevice);
    }

    /// <summary>
    /// Draw a planet with surface features.
    /// </summary>
    /// <param name="spriteBatch">Active SpriteBatch.</param>
    /// <param name="center">Screen-space center position.</param>
    /// <param name="radius">Planet radius in pixels.</param>
    /// <param name="color">Base planet color.</param>
    /// <param name="rotationAngle">Planet rotation angle in 1/64-turn units (0-63).</param>
    public void DrawPlanet(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, int rotationAngle = 0)
    {
        if (radius <= 0) return;

        // Draw planet outline
        _circleRenderer.DrawCircle(spriteBatch, center, radius, color, 32);

        // Draw surface features
        DrawEquator(spriteBatch, center, radius, color, rotationAngle);
        DrawMeridians(spriteBatch, center, radius, color, rotationAngle);
        DrawCraters(spriteBatch, center, radius, color, rotationAngle);
    }

    private void DrawEquator(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, int rotationAngle)
    {
        // Equator is an ellipse: full width, compressed height based on tilt
        // For simplicity, draw as horizontal line when viewed pole-on, ellipse when tilted
        float tiltFactor = 0.3f; // apparent tilt
        Vector2 u = new(radius, 0);
        Vector2 v = new(0, radius * tiltFactor);

        // Rotate the equator plane by planet rotation
        var (sin, cos) = SineTable.SinCos(rotationAngle);
        u = new Vector2(cos * radius, sin * radius * tiltFactor);
        v = new Vector2(-sin * radius, cos * radius * tiltFactor);

        _ellipseRenderer.DrawEllipse(spriteBatch, center, u * 0.95f, v * 0.95f, Darken(color, 0.7f), 24);
    }

    private void DrawMeridians(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, int rotationAngle)
    {
        // Draw 3 meridians (full ellipses from pole to pole)
        // Front-facing arcs drawn solid, back-facing arcs drawn dashed/invisible
        float tiltFactor = 0.3f;
        Color frontColor = Darken(color, 0.6f);
        Color backColor = Darken(color, 0.25f); // faint for back side

        for (int i = 0; i < 3; i++)
        {
            int angle = rotationAngle + i * 21; // 120 degrees apart
            var (sin, cos) = SineTable.SinCos(angle);

            // Meridian ellipse: pole-to-pole with rotation
            Vector2 u = new Vector2(cos * radius * 0.9f, sin * radius * 0.9f * tiltFactor);
            Vector2 v = new Vector2(0, -radius * 0.9f);

            // Draw front half (steps 0-32) and back half (steps 32-64) separately
            // The sign of the x-component at the equator determines front/back
            float equatorX = u.X; // x at equator (step 0 of the parametric)
            if (equatorX >= 0)
            {
                // Front half is steps 0-32 (right side of ellipse)
                _ellipseRenderer.DrawEllipseArc(spriteBatch, center, u, v, frontColor, 16, 48, 16);
                // Back half is steps 48-16 (wrapping around left side)
                _ellipseRenderer.DrawEllipseArc(spriteBatch, center, u, v, backColor, 48, 16, 8);
            }
            else
            {
                // Flipped: left side is front
                _ellipseRenderer.DrawEllipseArc(spriteBatch, center, u, v, frontColor, 48, 16, 16);
                _ellipseRenderer.DrawEllipseArc(spriteBatch, center, u, v, backColor, 16, 48, 8);
            }
        }
    }

    private void DrawCraters(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, int rotationAngle)
    {
        // Draw a few craters as small ellipses
        // Only draw craters on the visible hemisphere
        Color craterColor = Darken(color, 0.5f);

        // Crater positions in planet-local space (normalized)
        var craterPositions = new[]
        {
            new Vector2(0.4f, -0.3f),
            new Vector2(-0.3f, 0.4f),
            new Vector2(0.2f, 0.5f),
            new Vector2(-0.5f, -0.2f),
        };

        float[] craterSizes = { 0.12f, 0.08f, 0.1f, 0.06f };

        for (int i = 0; i < craterPositions.Length; i++)
        {
            var localPos = craterPositions[i];
            float craterSize = craterSizes[i] * radius;

            // Check if crater is on visible hemisphere (simple: only if x is positive enough)
            // Rotate crater position by planet rotation
            var (sin, cos) = SineTable.SinCos(rotationAngle + i * 10);
            float rx = cos * localPos.X - sin * localPos.Y;
            float ry = sin * localPos.X + cos * localPos.Y;

            // Only draw if on visible face (simple depth check)
            if (rx > -0.3f)
            {
                Vector2 craterCenter = center + new Vector2(rx * radius, ry * radius);
                float foreshortening = MathF.Max(0.3f, MathF.Sqrt(MathF.Max(0, 1 - rx * rx)));
                _ellipseRenderer.DrawAxisAlignedEllipse(spriteBatch, craterCenter,
                    craterSize, craterSize * foreshortening, craterColor, 12);
            }
        }
    }

    private static Color Darken(Color color, float factor)
    {
        return new Color(
            (int)(color.R * factor),
            (int)(color.G * factor),
            (int)(color.B * factor),
            color.A);
    }
}
