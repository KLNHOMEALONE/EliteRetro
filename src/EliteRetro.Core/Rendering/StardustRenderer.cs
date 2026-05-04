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
    private const int StarCount = 400;
    private float _currentSpeed;

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
            // Distribute stars uniformly in a sphere segment in front of the camera
            // Use spherical coordinates for even distribution
            float theta = (float)rng.NextDouble() * MathHelper.TwoPi; // 0 to 2π (azimuth)
            float phi = (float)rng.NextDouble() * MathHelper.PiOver2; // 0 to π/2 (elevation, front hemisphere only)
            float r = (float)rng.NextDouble() * 16000 + 384; // distance from 384 to 16383

            int x = (int)(r * Math.Sin(phi) * Math.Cos(theta));
            int y = (int)(r * Math.Sin(phi) * Math.Sin(theta));
            int z = (int)(r * Math.Cos(phi));

            _stars[i] = new StarData
            {
                X = SignedToSignMag(Math.Clamp(x, -16383, 16383)),
                Y = SignedToSignMag(Math.Clamp(y, -16383, 16383)),
                Z = SignedToSignMag(Math.Clamp(z, 1, 16383)),
                Brightness = (byte)rng.Next(128, 255)
            };
        }
    }

    /// <summary>
    /// Update star positions based on player speed and universe rotation.
    /// Implements the authentic Elite MVS4 rotation and movement.
    /// </summary>
    public void Update(float speed, float alpha, float beta)
    {
        _currentSpeed = speed;

        // Scale speed for stardust motion
        float effectiveSpeed = Math.Min(speed * 8f, 40f);
        float zDelta = effectiveSpeed * 64 / 40f;

        for (int i = 0; i < StarCount; i++)
        {
            ref StarData s = ref _stars[i];

            // Extract signed values from sign-magnitude
            // sz represents depth magnitude. World Z = -sz.
            int sx = SignMagToSigned(s.X);
            int sy = SignMagToSigned(s.Y);
            int sz = SignMagToSigned(s.Z);

            if (sz <= 0) continue;

            // 1. ROTATE UNIVERSE (Minsky MVS4 routine)
            // Roll then Pitch applied to star world coordinates
            // Use float math to maintain precision with ship positions
            float fsx = sx;
            float fsy = sy;
            float fsz = -sz; // Depth magnitude to world Z

            float k2 = fsy - alpha * fsx;
            fsz = fsz + beta * k2;
            fsy = k2 - beta * fsz;
            fsx = fsx + alpha * fsy;

            // 2. MOVE FORWARD
            // World position uses Z = -depth (ahead is negative). Moving "forward" reduces depth,
            // so world Z moves toward 0 (less negative), i.e. we ADD zDelta here.
            fsz += zDelta;

            sx = (int)fsx;
            sy = (int)fsy;
            sz = (int)(-fsz); // Back to depth magnitude

            // Perspective expansion is handled by the 3D projection in Draw.
            // Wrap around: if star goes too far or behind camera, respawn in distance
            if (sz <= 0 || sz > 16383 || Math.Abs(sx) > 16000 || Math.Abs(sy) > 16000)
            {
                // Respawn at far distance with random X/Y
                float theta = (float)(s.Brightness * 0.0245 + i * 0.001);
                float r = 14000 + (s.Brightness % 2000);
                sx = (int)(r * Math.Sin(theta) * Math.Cos(theta * 7));
                sy = (int)(r * Math.Sin(theta) * Math.Sin(theta * 7));
                sz = (int)r;
            }

            // Store back as sign-magnitude
            s.X = SignedToSignMag(sx);
            s.Y = SignedToSignMag(sy);
            s.Z = SignedToSignMag(sz);
        }
    }

    /// <summary>
    /// Draw the starfield projected onto screen space.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Vector2 center, float scale, Matrix view)
    {
        for (int i = 0; i < StarCount; i++)
        {
            ref StarData s = ref _stars[i];

            int x = SignMagToSigned(s.X);
            int y = SignMagToSigned(s.Y);
            int z = SignMagToSigned(s.Z); // magnitude of depth

            if (z <= 0 || z > 16383) continue;

            // World position: X=right, Y=up, Z=-depth (ahead)
            Vector3 worldPos = new Vector3(x, y, -z);
            Vector3 viewPos = Vector3.Transform(worldPos, view);

            // Objects in front have negative Z in RH view space
            if (viewPos.Z >= 0) continue;

            // Perspective projection: screen pos = center + (x, y) * scale / -z
            float factor = scale / -viewPos.Z;
            float screenX = center.X + viewPos.X * factor;
            float screenY = center.Y + viewPos.Y * factor;

            // Skip if off-screen
            if (screenX < -10 || screenX > center.X + 1024 + 10) continue;
            if (screenY < -10 || screenY > center.Y + 768 + 10) continue;

            // Star size: closer stars are larger
            int size = z < 200 ? 3 : z < 1000 ? 2 : 1;

            // At high speed, stars stretch into dashes (motion blur effect)
            float speedFactor = Math.Max(0, _currentSpeed - 7f) / 33f; // 0 at speed 7, 1 at speed 40
            speedFactor = Math.Min(speedFactor, 1f); // clamp at speed 40+
            int dashLength = 1 + (int)(speedFactor * 8); // 1 to 9 pixels long

            // Brightness: closer stars are brighter, with higher base brightness
            float brightness = s.Brightness / 255f * Math.Clamp(1.0f - z / 16384f, 0.4f, 1.0f);
            brightness = Math.Clamp(brightness * 1.5f * (1f + speedFactor * 0.5f), 0.1f, 1.0f);

            Color starColor = new Color(
                (int)(255 * brightness),
                (int)(255 * brightness),
                (int)(255 * brightness));

            if (dashLength > 1)
            {
                // Draw star as a dash (line) radiating from screen center
                Vector2 dir = Vector2.Normalize(new Vector2((float)(screenX - center.X), (float)(screenY - center.Y)));
                if (dir.Length() < 0.01f) dir = Vector2.UnitX; // center star edge case
                Vector2 start = new Vector2(screenX, screenY) - dir * dashLength / 2f;
                Vector2 end = new Vector2(screenX, screenY) + dir * dashLength / 2f;

                // Draw line using rotation
                float angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);
                float length = dashLength;
                spriteBatch.Draw(_whitePixel, start, null, starColor, angle, Vector2.Zero,
                    new Vector2(length, 1), SpriteEffects.None, 0);
            }
            else
            {
                spriteBatch.Draw(_whitePixel,
                    new Rectangle((int)screenX, (int)screenY, size, size),
                    starColor);
            }
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
