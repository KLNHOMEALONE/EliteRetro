using Microsoft.Xna.Framework.Audio;

namespace EliteRetro.Core.Audio;

/// <summary>
/// Procedural audio system for EliteRetro. Generates all sounds
/// in-memory without external files using MonoGame's audio API.
///
/// Sounds:
///   - Menu select: short beep for menu navigation
///   - Laser shot: short noise burst with frequency sweep
///   - Explosion: noise envelope with decay
/// </summary>
public class AudioManager : IDisposable
{
    private SoundEffect? _menuSelectEffect;
    private SoundEffect? _laserEffect;
    private SoundEffect? _explosionEffect;
    private bool _disposed;

    // Audio format
    private const int SampleRate = 22050;
    private const float MasterVolume = 0.3f;

    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Initialize audio system. Call from LoadContent.
    /// </summary>
    public void Initialize()
    {
        if (IsInitialized) return;

        try
        {
            // Generate one-shot effects
            _menuSelectEffect = GenerateMenuSelectEffect();
            _laserEffect = GenerateLaserEffect();
            _explosionEffect = GenerateExplosionEffect();
            IsInitialized = true;
        }
        catch
        {
            // Audio not available on this platform
            IsInitialized = false;
        }
    }

    /// <summary>
    /// Play menu selection sound (short pleasant beep).
    /// </summary>
    public void PlayMenuSelect()
    {
        if (!IsInitialized) return;
        _menuSelectEffect?.Play(volume: MasterVolume * 0.5f, pitch: 0f, pan: 0f);
    }

    /// <summary>
    /// Play laser shot sound.
    /// </summary>
    public void PlayLaser()
    {
        if (!IsInitialized) return;
        _laserEffect?.Play(volume: MasterVolume, pitch: 0f, pan: 0f);
    }

    /// <summary>
    /// Play explosion sound.
    /// </summary>
    public void PlayExplosion()
    {
        if (!IsInitialized) return;
        _explosionEffect?.Play(volume: MasterVolume, pitch: 0f, pan: 0f);
    }

    /// <summary>
    /// Generate a menu select sound (short pleasant beep at 880Hz with soft attack/decay).
    /// </summary>
    private SoundEffect? GenerateMenuSelectEffect()
    {
        const int durationMs = 100;
        const int sampleCount = SampleRate * durationMs / 1000;
        var samples = new short[sampleCount * 2]; // stereo
        const float freq = 880f; // A5 note

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;

            // Soft attack and decay envelope (raised cosine bell)
            float envelope = (float)(0.5f * (1 - Math.Cos(Math.PI * t)));

            // Pure sine wave
            float sample = (float)Math.Sin(2 * Math.PI * freq * t / 1000 * durationMs) * envelope * 0.5f;

            short value = (short)(sample * 32767);
            samples[i * 2] = value;
            samples[i * 2 + 1] = value;
        }

        return CreateSoundEffect(samples);
    }

    /// <summary>
    /// Generate a laser shot sound effect (noise burst with frequency sweep).
    /// Returns a SoundEffect from a WAV byte array.
    /// </summary>
    private SoundEffect? GenerateLaserEffect()
    {
        const int durationMs = 150;
        const int sampleCount = SampleRate * durationMs / 1000;
        var samples = new short[sampleCount * 2]; // stereo

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            float envelope = (float)Math.Exp(-t * 15); // Quick decay

            // Frequency sweep: high to low (pew effect)
            float freq = 2000f * (1 - t) + 200f * t;
            float carrier = (float)Math.Sin(2 * Math.PI * freq * t / 1000 * durationMs);

            // Mix with noise
            float noise = (float)(Random.Shared.NextDouble() * 2 - 1) * 0.3f;
            float sample = (carrier * 0.7f + noise) * envelope * 0.5f;

            short value = (short)(sample * 32767);
            samples[i * 2] = value;
            samples[i * 2 + 1] = value;
        }

        return CreateSoundEffect(samples);
    }

    /// <summary>
    /// Generate an explosion sound effect (noise with slow decay).
    /// </summary>
    private SoundEffect? GenerateExplosionEffect()
    {
        const int durationMs = 800;
        const int sampleCount = SampleRate * durationMs / 1000;
        var samples = new short[sampleCount * 2]; // stereo

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            float envelope = (float)Math.Exp(-t * 5); // Slower decay

            // Low-frequency noise burst
            float noise = (float)(Random.Shared.NextDouble() * 2 - 1);

            // Add rumble
            float rumble = (float)Math.Sin(2 * Math.PI * 60 * t) * 0.3f;

            float sample = (noise * 0.6f + rumble) * envelope * 0.6f;
            short value = (short)(sample * 32767);
            samples[i * 2] = value;
            samples[i * 2 + 1] = value;
        }

        return CreateSoundEffect(samples);
    }

    /// <summary>
    /// Create a SoundEffect from PCM samples by wrapping in a WAV header.
    /// </summary>
    private static SoundEffect? CreateSoundEffect(short[] samples)
    {
        // WAV header: 44 bytes
        int byteRate = SampleRate * 4; // stereo 16-bit
        int blockAlign = 4;
        int dataBytes = samples.Length * 2;
        int fileSize = 36 + dataBytes;

        var wav = new byte[44 + dataBytes];

        // RIFF header
        wav[0] = (byte)'R'; wav[1] = (byte)'I'; wav[2] = (byte)'F'; wav[3] = (byte)'F';
        BitConverter.GetBytes(fileSize).CopyTo(wav, 4);
        wav[8] = (byte)'W'; wav[9] = (byte)'A'; wav[10] = (byte)'V'; wav[11] = (byte)'E';

        // fmt chunk
        wav[12] = (byte)'f'; wav[13] = (byte)'m'; wav[14] = (byte)'t'; wav[15] = (byte)' ';
        BitConverter.GetBytes(16).CopyTo(wav, 16); // chunk size
        BitConverter.GetBytes((short)1).CopyTo(wav, 20); // PCM format
        BitConverter.GetBytes((short)2).CopyTo(wav, 22); // stereo
        BitConverter.GetBytes(SampleRate).CopyTo(wav, 24);
        BitConverter.GetBytes(byteRate).CopyTo(wav, 28);
        BitConverter.GetBytes((short)blockAlign).CopyTo(wav, 32);
        BitConverter.GetBytes((short)16).CopyTo(wav, 34); // 16 bits per sample

        // data chunk
        wav[36] = (byte)'d'; wav[37] = (byte)'a'; wav[38] = (byte)'t'; wav[39] = (byte)'a';
        BitConverter.GetBytes(dataBytes).CopyTo(wav, 40);

        // PCM data
        for (int i = 0; i < samples.Length; i++)
        {
            var bytes = BitConverter.GetBytes(samples[i]);
            wav[44 + i * 2] = bytes[0];
            wav[44 + i * 2 + 1] = bytes[1];
        }

        return SoundEffect.FromStream(new MemoryStream(wav));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _menuSelectEffect?.Dispose();
        _laserEffect?.Dispose();
        _explosionEffect?.Dispose();
    }
}
