using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EliteRetro.Core.Rendering;

/// <summary>
/// Starfield particle system (stardust) using 16-bit sign-magnitude coordinates.
/// Implements authentic Elite star motion: stars move toward viewer (Z decreases),
/// with perspective-driven X/Y expansion. Roll and pitch affect star positions.
/// Stars wrap around on overflow.
/// </summary>
public class StardustRenderer
{
    private readonly Texture2D _whitePixel;
    private readonly StarData[] _stars;
    private const int StarCount = 200;

    /// <summary>
    /// 16-bit sign-magnitude star coordinate.
    /// </summary>
    private struct StarData
    {
        public short X; // Sign-magnitude: bit 15 = sign, bits 0-14 = magnitude
        public short Y;
        public short Z;
        public byte Brightness;
    }

    public StardustRenderer(GraphicsDevice graphicsDevice)
    {
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
        _stars = new StarData[StarCount];
    }

    /// <summary>
    /// Initialize stars with a seed for reproducible starfield.
    /// </summary>
    public void Initialize(int seed)
    {
        Random rng = new Random(seed);

        for (int i = 0; i < StarCount; i++)
        {
            _stars[i] = new StarData
            {
                X = (short)rng.Next(-16383, 16383),
                Y = (short)rng.Next(-16383, 16383),
                Z = (short)rng.Next(1, 16383),
                Brightness = (byte)rng.Next(64, 255)
            };
        }
    }

    /// <summary>
    /// Update star positions based on player speed and rotation.
    /// </summary>
    /// <param name="speed">Player forward speed (positive = forward).</param>
    /// <param name="rollAngle">Roll angle in 1/256 rad units.</param>
    /// <param name="pitchAngle">Pitch angle in 1/256 rad units.</param>
    /// <param name="viewMode">0=front, 1=rear, 2=left, 3=right.</param>
    public void Update(float speed, int rollAngle, int pitchAngle, int viewMode)
    {
        float alpha = rollAngle / 256f;   // Roll in radians (approx)
        float beta = pitchAngle / 256f;    // Pitch in radians (approx)

        for (int i = 0; i < StarCount; i++)
        {
            ref StarData s = ref _stars[i];

            // Extract sign-magnitude values
            int sx = SignMagToSigned(s.X);
            int sy = SignMagToSigned(s.Y);
            int sz = SignMagToSigned(s.Z);

            if (sz <= 0) continue;

            int zHi = (sz >> 8) & 0x7F;
            if (zHi == 0) zHi = 1;

            // Forward motion: q = 64 * speed / z_hi
            float q = 64f * speed / zHi;

            // Z decreases by speed * 64
            sz -= (int)(speed * 64);

            // Y expands: y += |y_hi| * q
            int yHi = Math.Abs((s.Y >> 8) & 0x7F);
            sy += (int)(yHi * q);

            // X expands: x += |x_hi| * q
            int xHi = Math.Abs((s.X >> 8) & 0x7F);
            sx += (int)(xHi * q);

            // Roll: y += alpha * x / 256; x -= alpha * y / 256
            sy += (int)(alpha * sx / 256f);
            sx -= (int)(alpha * sy / 256f);

            // Pitch: y -= beta * 256; x += 2 * (beta * y / 256)^2
            sy -= (int)(beta * 256);
            float pitchOffset = beta * sy / 256f;
            sx += (int)(2 * pitchOffset * pitchOffset);

            // View switching transformations
            switch (viewMode)
            {
                case 1: // Rear view: invert X and Z
                    sx = -sx;
                    sz = -sz;
                    break;
                case 2: // Left view: swap X/Z, invert X
                    {
                        int tmp = sx;
                        sx = -sz;
                        sz = tmp;
                    }
                    break;
                case 3: // Right view: swap X/Z, invert Z
                    {
                        int tmp = sx;
                        sx = sz;
                        sz = -tmp;
                    }
                    break;
            }

            // Wrap around on overflow
            if (sz <= 0 || sz > 16383)
            {
                sz = 16383;
                sx = (short)(-16383 + (i * 137) % 32766);
                sy = (short)(-16383 + (i * 251) % 32766);
            }

            // Clamp X/Y to prevent extreme values
            sx = Math.Clamp(sx, -16383, 16383);
            sy = Math.Clamp(sy, -16383, 16383);

            // Store back as sign-magnitude
            s.X = SignedToSignMag(sx);
            s.Y = SignedToSignMag(sy);
            s.Z = SignedToSignMag(sz);
        }
    }

    /// <summary>
    /// Draw the starfield projected onto screen space.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Vector2 center, float scale)
    {
        for (int i = 0; i < StarCount; i++)
        {
            ref StarData s = ref _stars[i];

            int x = SignMagToSigned(s.X);
            int y = SignMagToSigned(s.Y);
            int z = SignMagToSigned(s.Z);

            if (z <= 0 || z > 16383) continue;

            // Perspective projection: screen pos = center + (x, y) * scale / z
            float factor = scale / z;
            float screenX = center.X + x * factor;
            float screenY = center.Y + y * factor;

            // Skip if off-screen
            if (screenX < -10 || screenX > center.X + 1024 + 10) continue;
            if (screenY < -10 || screenY > center.Y + 768 + 10) continue;

            // Star size: closer stars are larger
            int size = z < 200 ? 3 : z < 1000 ? 2 : 1;

            // Brightness: closer stars are brighter
            float brightness = s.Brightness / 255f * Math.Clamp(1.0f - z / 16384f, 0.2f, 1.0f);

            Color starColor = new Color(
                (int)(255 * brightness),
                (int)(255 * brightness),
                (int)(255 * brightness));

            spriteBatch.Draw(_whitePixel,
                new Rectangle((int)screenX, (int)screenY, size, size),
                starColor);
        }
    }

    /// <summary>
    /// Convert 16-bit sign-magnitude to signed int.
    /// Bit 15 = sign (1=negative), bits 0-14 = magnitude.
    /// </summary>
    private static int SignMagToSigned(short value)
    {
        int magnitude = value & 0x7FFF;
        int sign = (value & 0x8000) != 0 ? -1 : 1;
        return magnitude * sign;
    }

    /// <summary>
    /// Convert signed int to 16-bit sign-magnitude.
    /// </summary>
    private static short SignedToSignMag(int value)
    {
        int magnitude = Math.Abs(value) & 0x7FFF;
        if (value < 0)
            magnitude |= 0x8000;
        return (short)magnitude;
    }
}
