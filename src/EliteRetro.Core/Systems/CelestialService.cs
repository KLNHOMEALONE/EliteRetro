using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EliteRetro.Core.Rendering;
using EliteRetro.Core.Managers;
using EliteRetro.Core.Entities;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Implementation of ICelestialService that handles the projection and ordering of celestial bodies.
/// Verbatim preservation of authentic BBC Elite projection math.
/// </summary>
public class CelestialService : ICelestialService
{
    private readonly CircleRenderer _circleRenderer;
    private const float RenderScale = 0.001f;
    private bool _disposed;

    public CelestialService(GraphicsDevice graphicsDevice)
    {
        _circleRenderer = new CircleRenderer(graphicsDevice);
    }

    public void Draw(SpriteBatch spriteBatch, IBubbleManager bubbleManager, Matrix view, Matrix projection, Vector3 cameraLookDir, GraphicsDevice graphicsDevice, float hudHeightFraction, bool drawWhite)
    {
        var planetEntity = bubbleManager.Planet;
        var sunEntity = (bubbleManager.SunOrStation?.Blueprint?.Name == "Sun") ? bubbleManager.SunOrStation : null;

        CelestialDisc? planetDisc = planetEntity != null ? ComputeCelestialDisc(planetEntity.Position, GameConstants.PlanetRadius, view, projection, cameraLookDir, graphicsDevice, hudHeightFraction) : null;
        CelestialDisc? sunDisc = sunEntity != null ? ComputeCelestialDisc(sunEntity.Position, GameConstants.PlanetRadius * 6, view, projection, cameraLookDir, graphicsDevice, hudHeightFraction) : null;

        if (planetDisc.HasValue || sunDisc.HasValue)
        {
            // Eclipse/Occlusion logic
            if (planetDisc.HasValue && sunDisc.HasValue)
            {
                var p = planetDisc.Value;
                var s = sunDisc.Value;
                // If sun is farther than planet (view-space Z is more negative) 
                // and projects inside the planet disc, it's occluded.
                if (s.ViewZ < p.ViewZ)
                {
                    float d = Vector2.Distance(p.ScreenCenter, s.ScreenCenter);
                    if (d < p.ScreenRadius - 0.5f)
                        sunDisc = null;
                }
            }

            // Painters algorithm using view-space Z (more negative = farther)
            if (sunDisc.HasValue && planetDisc.HasValue)
            {
                if (sunDisc.Value.ViewZ < planetDisc.Value.ViewZ)
                {
                    DrawCelestialSun(spriteBatch, sunDisc.Value, drawWhite);
                    DrawCelestialPlanet(spriteBatch, planetDisc.Value, drawWhite);
                }
                else
                {
                    DrawCelestialPlanet(spriteBatch, planetDisc.Value, drawWhite);
                    DrawCelestialSun(spriteBatch, sunDisc.Value, drawWhite);
                }
            }
            else
            {
                if (sunDisc.HasValue)
                    DrawCelestialSun(spriteBatch, sunDisc.Value, drawWhite);
                if (planetDisc.HasValue)
                    DrawCelestialPlanet(spriteBatch, planetDisc.Value, drawWhite);
            }
        }
    }

    private readonly record struct CelestialDisc(Vector3 WorldPosElite, Vector2 ScreenCenter, float ScreenRadius, float ViewZ);

    private CelestialDisc? ComputeCelestialDisc(Vector3 worldPosElite, float radiusElite, Matrix view, Matrix projection, Vector3 cameraLookDir, GraphicsDevice graphicsDevice, float hudHeightFraction)
    {
        Vector3 worldMg = new Vector3(worldPosElite.X, worldPosElite.Y, -worldPosElite.Z) * RenderScale;
        
        // Visibility check relative to camera look direction
        if (worldMg.LengthSquared() >= 0.001f)
        {
            if (Vector3.Dot(Vector3.Normalize(worldMg), cameraLookDir) <= 0)
                return null;
        }

        Vector3 viewPos = Vector3.Transform(worldMg, view);

        // In a standard RH camera, objects in front have negative Z in view space.
        if (viewPos.Z >= -0.001f)
            return null;

        Vector2 screenPos = ProjectToScreenElite(worldPosElite, view, projection, graphicsDevice, hudHeightFraction);
        float dist = worldMg.Length();
        if (dist < 0.001f) return null;

        int h = graphicsDevice.Viewport.Height;
        int viewH = Math.Max(1, h - (int)MathF.Round(h * hudHeightFraction));

        // FOV is 75 deg. tan(75/2) ≈ 0.767
        float screenRadius = ((radiusElite * RenderScale) / dist) * (1.0f / 0.767f) * (viewH / 2f);
        if (screenRadius <= 0 || screenRadius > 4000) return null;

        return new CelestialDisc(worldPosElite, screenPos, screenRadius, viewPos.Z);
    }

    private Vector2 ProjectToScreenElite(Vector3 eliteWorldPos, Matrix view, Matrix projection, GraphicsDevice graphicsDevice, float hudHeightFraction)
    {
        Vector3 worldPos = new Vector3(eliteWorldPos.X, eliteWorldPos.Y, -eliteWorldPos.Z) * RenderScale;
        Vector3 viewPos = Vector3.Transform(worldPos, view);
        Vector4 projected = Vector4.Transform(new Vector4(viewPos, 1f), projection);
        
        int w = graphicsDevice.Viewport.Width;
        int h = graphicsDevice.Viewport.Height;
        int viewH = Math.Max(1, h - (int)MathF.Round(h * hudHeightFraction));
        
        if (MathF.Abs(projected.W) < 0.001f) return new Vector2(w / 2f, viewH / 2f);
        
        float screenX = (projected.X / projected.W + 1f) * 0.5f * w;
        float screenY = (1 - projected.Y / projected.W) * 0.5f * viewH;

        return new Vector2(screenX, screenY);
    }

    private void DrawCelestialSun(SpriteBatch spriteBatch, CelestialDisc disc, bool drawWhite)
    {
        _circleRenderer.DrawFilledCircle(spriteBatch, disc.ScreenCenter, disc.ScreenRadius, Color.White, drawWhite);
    }

    private void DrawCelestialPlanet(SpriteBatch spriteBatch, CelestialDisc disc, bool drawWhite)
    {
        _circleRenderer.DrawCircle(spriteBatch, disc.ScreenCenter, disc.ScreenRadius, Color.White, 48, drawWhite);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _circleRenderer?.Dispose();
    }
}
