namespace EliteRetro.Core.Systems;

/// <summary>
/// Three 16-bit seed values for Elite's Tribonacci galaxy generation.
/// Initial seeds for Galaxy 0, System 0 (Tibedied): 0x5A4A, 0x0248, 0xB753
/// </summary>
public struct GalaxySeed
{
    public ushort W0; // s0
    public ushort W1; // s1
    public ushort W2; // s2

    public GalaxySeed(ushort w0, ushort w1, ushort w2)
    {
        W0 = w0;
        W1 = w1;
        W2 = w2;
    }

    /// <summary>
    /// Initial seed for Galaxy 0, System 0 (Tibedied).
    /// </summary>
    public static GalaxySeed Galaxy0System0 => new GalaxySeed(0x5A4A, 0x0248, 0xB753);

    // Byte accessors for bit-level operations
    public byte W0Lo => (byte)(W0 & 0xFF);
    public byte W0Hi => (byte)(W0 >> 8);
    public byte W1Lo => (byte)(W1 & 0xFF);
    public byte W1Hi => (byte)(W1 >> 8);
    public byte W2Lo => (byte)(W2 & 0xFF);
    public byte W2Hi => (byte)(W2 >> 8);

    /// <summary>
    /// Twist: advance one step in the Tribonacci sequence.
    /// s0' = s1, s1' = s2, s2' = s0 + s1 + s2 (16-bit wraparound)
    /// </summary>
    public void Twist()
    {
        // Compute new s2 = s0 + s1 + s2 (16-bit wraparound)
        ushort newW2 = (ushort)(W0 + W1 + W2);
        W0 = W1;
        W1 = W2;
        W2 = newW2;
    }

    /// <summary>
    /// Advance to the next galaxy: rotate each byte left by 1 bit.
    /// </summary>
    public void NextGalaxy()
    {
        W0 = RotateLeftBytePair(W0);
        W1 = RotateLeftBytePair(W1);
        W2 = RotateLeftBytePair(W2);
    }

    private static ushort RotateLeftBytePair(ushort val)
    {
        byte lo = (byte)(val & 0xFF);
        byte hi = (byte)(val >> 8);
        lo = (byte)((lo << 1) | (lo >> 7));
        hi = (byte)((hi << 1) | (hi >> 7));
        return (ushort)((hi << 8) | lo);
    }

    public override readonly string ToString() => $"[{W0:X4}] [{W1:X4}] [{W2:X4}]";
}
