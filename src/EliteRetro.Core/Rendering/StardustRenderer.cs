using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EliteRetro.Core.Systems;

namespace EliteRetro.Core.Rendering;

/// <summary>
/// Starfield particle system (stardust) using 16-bit sign-magnitude coordinates.
/// Implements authentic Elite star motion: stars move toward viewer (Z decreases),
/// with perspective-driven X/Y expansion. Roll and pitch affect star positions.
/// Stars wrap around on overflow.
/// </summary>
public class StardustRenderer : IStardustService
{
    private readonly Texture2D _whitePixel;
    private bool _isDisposed;
    private readonly StarData[] _stars;
    private const int StarCount = 16;
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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        if (disposing)
        {
            _whitePixel?.Dispose();
        }
        _isDisposed = true;
    }

    ~StardustRenderer()
    {
        Dispose(false);
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
    /// </summary>
    public void Update(float speed, float alpha, float beta, GameTime gameTime)
    {
        _currentSpeed = speed;

        // Scale speed for stardust motion
        float effectiveSpeed = Math.Min(speed * 8f, GameConstants.SpeedMax);
        float zDelta = effectiveSpeed * 64 / GameConstants.SpeedMax;

        for (int i = 0; i < StarCount; i++)
        {
            ref StarData s = ref _stars[i];

            int sx = SignMagToSigned(s.X);
            int sy = SignMagToSigned(s.Y);
            int sz = SignMagToSigned(s.Z);

            if (sz <= 0) continue;

            float fsx = sx;
            float fsy = sy;
            float fsz = -sz; // Depth magnitude to world Z

            // Minsky MVS4 rotation
            float k2 = fsy - alpha * fsx;
            fsz = fsz + beta * k2;
            fsy = k2 - beta * fsz;
            fsx = fsx + alpha * fsy;

            fsz += zDelta;

            sx = (int)fsx;
            sy = (int)fsy;
            sz = (int)(-fsz);

            if (sz <= 0 || sz > 16383 || Math.Abs(sx) > 16000 || Math.Abs(sy) > 16000)
            {
                float theta = (float)(s.Brightness * 0.0245 + i * 0.001);
                float r = 14000 + (s.Brightness % 2000);
                sx = (int)(r * Math.Sin(theta) * Math.Cos(theta * 7));
                sy = (int)(r * Math.Sin(theta) * Math.Sin(theta * 7));
                sz = (int)r;
            }

            s.X = SignedToSignMag(sx);
            s.Y = SignedToSignMag(sy);
            s.Z = SignedToSignMag(sz);
        }
    }

    /// <summary>
    /// Draw the starfield projected onto screen space.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Vector2 screenCenter, float scale, Matrix view, bool drawWhite = false)
    {
        for (int i = 0; i < StarCount; i++)
        {
            ref StarData s = ref _stars[i];

            int x = SignMagToSigned(s.X);
            int y = SignMagToSigned(s.Y);
            int z = SignMagToSigned(s.Z);

            if (z <= 0 || z > 16383) continue;

            Vector3 worldPos = new Vector3(x, y, -z);
            Vector3 viewPos = Vector3.Transform(worldPos, view);

            if (viewPos.Z >= 0) continue;

            float factor = scale / -viewPos.Z;
            float screenX = screenCenter.X + viewPos.X * factor;
            float screenY = screenCenter.Y + viewPos.Y * factor;

            if (screenX < -10 || screenX > screenCenter.X + 512 + 10) continue;
            if (screenY < -10 || screenY > screenCenter.Y + 384 + 10) continue;

            int size = 7;
            float speedFactor = Math.Max(0, _currentSpeed - (GameConstants.SpeedMax * 0.175f)) / (GameConstants.SpeedMax * 0.825f); 
            speedFactor = Math.Min(speedFactor, 1f);
            int dashLength = 1 + (int)(speedFactor * 8);

            float brightness = s.Brightness / 255f * Math.Clamp(1.0f - z / 16384f, 0.4f, 1.0f);
            brightness = Math.Clamp(brightness * 1.5f * (1f + speedFactor * 0.5f), 0.1f, 1.0f);

            Color starColor = new Color((int)(255 * brightness), (int)(255 * brightness), (int)(255 * brightness));

            if (dashLength > 1)
            {
                Vector2 dir = Vector2.Normalize(new Vector2((float)(screenX - screenCenter.X), (float)(screenY - screenCenter.Y)));
                if (dir.Length() < 0.01f) dir = Vector2.UnitX;
                Vector2 start = new Vector2(screenX, screenY) - dir * dashLength / 2f;
                Vector2 end = new Vector2(screenX, screenY) + dir * dashLength / 2f;

                float angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);
                spriteBatch.Draw(_whitePixel, start, null, starColor, angle, Vector2.Zero, new Vector2(dashLength, 3), SpriteEffects.None, 0);
            }
            else
            {
                spriteBatch.Draw(_whitePixel, new Rectangle((int)screenX, (int)screenY, size, size), starColor);
            }
        }
    }

    private static int SignMagToSigned(short value)
    {
        int magnitude = value & 0x7FFF;
        bool isNegative = (value & 0x8000) != 0;
        return isNegative ? -magnitude : magnitude;
    }

    private static short SignedToSignMag(int value)
    {
        if (value < 0)
            return (short)(0x8000 | (-value & 0x7FFF));
        return (short)(value & 0x7FFF);
    }
}
