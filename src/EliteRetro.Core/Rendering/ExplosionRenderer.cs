using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EliteRetro.Core.Entities;

namespace EliteRetro.Core.Rendering;

/// <summary>
/// Vertex-based explosion cloud rendering.
/// Explosion originates from specific vertices of a ship model, scattering
/// particles outward. The cloud expands (counter increases to 128) then contracts.
/// Four stored random seeds ensure reproducible redraws across frames.
/// </summary>
public class ExplosionRenderer
{
    private readonly Texture2D _whitePixel;

    public ExplosionRenderer(GraphicsDevice graphicsDevice)
    {
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Explosion cloud data — stored per active explosion.
    /// </summary>
    public class ExplosionCloud
    {
        /// <summary>Screen-space center of explosion.</summary>
        public Vector2 Center { get; set; }

        /// <summary>Origin vertices (indices into the ship model) where particles spawn.</summary>
        public int[] OriginVertices { get; set; } = Array.Empty<int>();

        /// <summary>Expansion counter: starts at 18, increments by 4/frame, peaks at 128, then shrinks.</summary>
        public int Counter { get; set; } = 18;

        /// <summary>Four random seed bytes for reproducible particle positions.</summary>
        public byte[] Seeds { get; set; } = new byte[4];

        /// <summary>Distance factor — affects particle size and spread.</summary>
        public float Distance { get; set; } = 1.0f;

        /// <summary>Explosion color (default: orange/yellow fire).</summary>
        public Color Color { get; set; } = Color.Orange;

        /// <summary>True when counter has completed its full lifecycle (expand + contract).</summary>
        public bool IsComplete => Counter <= 0;
    }

    /// <summary>
    /// Create a new explosion cloud for a ship at the given screen position.
    /// </summary>
    public ExplosionCloud CreateExplosion(ShipModel model, Vector2 screenCenter, float distance)
    {
        var rng = new Random((int)screenCenter.X * 17 + (int)screenCenter.Y * 31);

        // Use first N vertices as explosion origins (from blueprint)
        int explosionCount = Math.Min(model.Vertices.Count, 8);
        var origins = new int[explosionCount];
        for (int i = 0; i < explosionCount; i++)
            origins[i] = i;

        // Generate 4 random seeds
        byte[] seeds = new byte[4];
        rng.NextBytes(seeds);

        return new ExplosionCloud
        {
            Center = screenCenter,
            OriginVertices = origins,
            Counter = 18,
            Seeds = seeds,
            Distance = Math.Max(distance, 0.5f),
            Color = Color.Orange
        };
    }

    /// <summary>
    /// Update and render an explosion cloud. Returns false when complete.
    /// </summary>
    public bool UpdateAndDraw(SpriteBatch spriteBatch, ExplosionCloud cloud, GameTime gameTime)
    {
        // Increment counter by 4 per frame
        if (cloud.Counter < 128)
        {
            cloud.Counter += 4;
        }
        else
        {
            cloud.Counter -= 8; // Shrink faster than expansion
        }

        if (cloud.Counter <= 0)
            return false;

        DrawCloud(spriteBatch, cloud);
        return true;
    }

    /// <summary>
    /// Draw the explosion cloud at current state.
    /// </summary>
    private void DrawCloud(SpriteBatch spriteBatch, ExplosionCloud cloud)
    {
        if (cloud.OriginVertices.Length == 0) return;

        // Size = counter / distance — farther explosions are smaller
        float baseSize = cloud.Counter / cloud.Distance;
        if (baseSize < 1) return;

        // Particle count peaks at counter=128
        int particleCount = cloud.Counter <= 128
            ? cloud.Counter
            : 128;
        particleCount = Math.Clamp(particleCount / 4, 4, 32);

        // Use seeds for reproducible randomness
        int seed = HashCode.Combine(cloud.Seeds[0], cloud.Seeds[1], cloud.Seeds[2], cloud.Seeds[3], cloud.Counter);
        Random rng = new Random(seed);

        foreach (int vertexIdx in cloud.OriginVertices)
        {
            // Each origin vertex scatters particles within radius
            float scatterRadius = baseSize * (0.5f + 0.5f * (float)rng.NextDouble());

            for (int p = 0; p < particleCount / cloud.OriginVertices.Length; p++)
            {
                float angle = (float)rng.NextDouble() * MathHelper.TwoPi;
                float dist = (float)rng.NextDouble() * scatterRadius;

                float px = cloud.Center.X + (float)Math.Cos(angle) * dist;
                float py = cloud.Center.Y + (float)Math.Sin(angle) * dist;

                // Size modulated by counter: grows then shrinks
                int size = Math.Max(1, (int)(baseSize * 0.08f * (0.5f + 0.5f * (float)rng.NextDouble())));

                // Color transitions from white-hot center to orange to dark red
                float heat = cloud.Counter / 128f;
                Color particleColor = heat > 0.7f ? Color.White :
                                      heat > 0.4f ? Color.Orange :
                                      heat > 0.2f ? Color.OrangeRed :
                                      Color.DarkRed;
                particleColor = new Color(
                    particleColor.R, particleColor.G, particleColor.B,
                    (byte)(particleColor.A * Math.Min(1f, heat * 2f)));

                spriteBatch.Draw(_whitePixel,
                    new Rectangle((int)px, (int)py, size, size),
                    particleColor);
            }
        }
    }
}
