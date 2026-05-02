using EliteRetro.Core.Entities;
using EliteRetro.Core.Managers;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Commander save file format: 256 bytes total, 75 bytes used.
/// Matches BBC Elite commander file layout with CHECK checksum
/// and competition code (4-byte encoded credit+rank+platform+tamper).
/// </summary>
public static class SaveGameManager
{
    // File format offsets
    private const int OffQQ0 = 0x00;      // Current system galactic X
    private const int OffQQ1 = 0x01;      // Current system galactic Y
    private const int OffQQ21S0 = 0x02;   // Galaxy seed s0 (2 bytes)
    private const int OffQQ21S1 = 0x04;
    private const int OffQQ21S2 = 0x06;
    private const int OffCash = 0x08;     // Credit balance (4 bytes, ×100 Cr)
    private const int OffFuel = 0x0C;     // Fuel level (0-70)
    private const int OffCOK = 0x0D;      // Competition flags
    private const int OffGCNT = 0x0E;     // Galaxy number (0-7)
    private const int OffLaser0 = 0x0F;   // Laser types (4 bytes: front, rear, left, right)
    private const int OffLaser1 = 0x10;
    private const int OffLaser2 = 0x11;
    private const int OffLaser3 = 0x12;
    private const int OffCRGO = 0x15;     // Cargo capacity
    private const int OffCARGO = 0x16;    // Cargo hold (17 commodities, 1 byte each)
    private const int OffECM = 0x27;      // E.C.M. equipped (bit 0)
    private const int OffBST = 0x28;      // Fuel scoops (bit 0)
    private const int OffBOMB = 0x29;     // Energy bomb (bit 0)
    private const int OffENGY = 0x2A;     // Energy/shield level
    private const int OffDKCMP = 0x2B;    // Docking computer (bit 0)
    private const int OffGHYP = 0x2C;     // Galactic hyperdrive (bit 0)
    private const int OffESCP = 0x2D;     // Escape pod (bit 0)
    private const int OffNOMSL = 0x32;    // Missiles
    private const int OffFIST = 0x33;     // Legal status
    private const int OffAVL = 0x34;      // Market availability (18 bytes)
    private const int OffQQ26 = 0x46;     // Market random seed
    private const int OffTALLY = 0x47;    // Kill count (2 bytes)
    private const int OffSVC = 0x49;      // Save count
    private const int OffCHK2 = 0x4A;     // Secondary checksum
    private const int OffCHK = 0x4B;      // Primary checksum

    private const int FileSize = 256;
    private const int UsedBytes = 0x4C;   // Bytes 0x00-0x4B (76 bytes, but CHK is at 0x4B)
    private const byte SaveVersion = 1;

    /// <summary>
    /// Save commander data to a 256-byte binary file.
    /// </summary>
    public static void Save(string filePath, LocalBubbleManager bubble, int galaxyIndex, int systemIndex, GalaxySeed seed)
    {
        var data = new byte[FileSize];
        var commander = bubble.Commander;

        // Galactic coordinates
        data[OffQQ0] = (byte)(systemIndex & 0xFF);
        data[OffQQ1] = (byte)(galaxyIndex & 0xFF);

        // Galaxy seed (3 × 16-bit = 6 bytes)
        data[OffQQ21S0] = (byte)(seed.W0 & 0xFF);
        data[OffQQ21S0 + 1] = (byte)((seed.W0 >> 8) & 0xFF);
        data[OffQQ21S1] = (byte)(seed.W1 & 0xFF);
        data[OffQQ21S1 + 1] = (byte)((seed.W1 >> 8) & 0xFF);
        data[OffQQ21S2] = (byte)(seed.W2 & 0xFF);
        data[OffQQ21S2 + 1] = (byte)((seed.W2 >> 8) & 0xFF);

        // Credits (4 bytes, little-endian, ×100 Cr)
        int cash = commander.Credits;
        data[OffCash] = (byte)(cash & 0xFF);
        data[OffCash + 1] = (byte)((cash >> 8) & 0xFF);
        data[OffCash + 2] = (byte)((cash >> 16) & 0xFF);
        data[OffCash + 3] = (byte)((cash >> 24) & 0xFF);

        // Fuel (0-70)
        data[OffFuel] = (byte)Math.Clamp(commander.Fuel, 0, 70);

        // Competition flags
        data[OffCOK] = 0; // Reserved for competition use

        // Galaxy number
        data[OffGCNT] = (byte)(galaxyIndex & 0xFF);

        // Laser types (4 bytes) — 0=none, 1=short, 2=medium, 3=long, 4=military
        // For now, all players start with no lasers; equipment system would set these
        data[OffLaser0] = 0;
        data[OffLaser1] = 0;
        data[OffLaser2] = 0;
        data[OffLaser3] = 0;

        // Cargo capacity
        data[OffCRGO] = (byte)commander.CargoCapacity;

        // Cargo hold (17 commodities)
        for (int i = 0; i < 17; i++)
        {
            if (commander.CargoHold.TryGetValue(i, out int tons))
                data[OffCARGO + i] = (byte)Math.Clamp(tons, 0, 255);
            else
                data[OffCARGO + i] = 0;
        }

        // Equipment flags
        data[OffECM] = 0;   // E.C.M. — would be set by equipment system
        data[OffBST] = 0;   // Fuel scoops — would be set by equipment system
        data[OffBOMB] = 0;  // Energy bomb — would be set by equipment system

        // Energy/shield level
        data[OffENGY] = bubble.PlayerEnergy;

        // Docking computer
        data[OffDKCMP] = 0; // Would be set by equipment system

        // Galactic hyperdrive
        data[OffGHYP] = 0; // Would be set by equipment system

        // Escape pod
        data[OffESCP] = 0; // Would be set by equipment system

        // Missiles
        data[OffNOMSL] = bubble.PlayerMissiles;

        // Legal status (0=clean, 1=fugitive, 2=offender, 3=criminal)
        data[OffFIST] = commander.LegalStatus;

        // Market availability (18 bytes) — zeroed (regenerated on dock)
        for (int i = 0; i < 18; i++)
            data[OffAVL + i] = 0;

        // Market random seed
        data[OffQQ26] = 0; // Would be set when docking

        // TALLY (kill count, 2 bytes, little-endian)
        int tally = Math.Clamp(commander.Tally, 0, 65535);
        data[OffTALLY] = (byte)(tally & 0xFF);
        data[OffTALLY + 1] = (byte)((tally >> 8) & 0xFF);

        // Save count
        data[OffSVC] = SaveVersion;

        // Secondary checksum (CHK2)
        data[OffCHK2] = ComputeCHK2(data);

        // Primary checksum (CHECK: sum of bytes 0x00-0x4B)
        data[OffCHK] = ComputeCHECK(data);

        File.WriteAllBytes(filePath, data);
    }

    /// <summary>
    /// Load commander data from a 256-byte binary file.
    /// Returns true if loaded successfully, false if file is invalid or checksum fails.
    /// </summary>
    public static bool TryLoad(string filePath, LocalBubbleManager bubble, out int galaxyIndex, out int systemIndex, out GalaxySeed seed)
    {
        galaxyIndex = 0;
        systemIndex = 0;
        seed = default;

        if (!File.Exists(filePath))
            return false;

        var data = File.ReadAllBytes(filePath);
        if (data.Length < FileSize)
            return false;

        // Verify checksum
        byte expectedCheck = data[OffCHK];
        byte actualCheck = ComputeCHECK(data);
        if (expectedCheck != actualCheck)
            return false;

        var commander = bubble.Commander;

        // Galactic coordinates
        systemIndex = data[OffQQ0];
        galaxyIndex = data[OffQQ1];

        // Galaxy seed
        ushort s0 = (ushort)(data[OffQQ21S0] | (data[OffQQ21S0 + 1] << 8));
        ushort s1 = (ushort)(data[OffQQ21S1] | (data[OffQQ21S1 + 1] << 8));
        ushort s2 = (ushort)(data[OffQQ21S2] | (data[OffQQ21S2 + 1] << 8));
        seed = new GalaxySeed(s0, s1, s2);

        // Credits
        commander.Credits = data[OffCash] | (data[OffCash + 1] << 8) | (data[OffCash + 2] << 16) | (data[OffCash + 3] << 24);

        // Fuel
        commander.Fuel = data[OffFuel];

        // Cargo capacity
        commander.CargoCapacity = data[OffCRGO];

        // Cargo hold
        commander.CargoHold.Clear();
        for (int i = 0; i < 17; i++)
        {
            if (data[OffCARGO + i] > 0)
                commander.CargoHold[i] = data[OffCARGO + i];
        }

        // Energy
        bubble.PlayerEnergy = data[OffENGY];

        // Missiles
        bubble.PlayerMissiles = data[OffNOMSL];

        // Legal status
        commander.LegalStatus = data[OffFIST];

        // TALLY
        commander.Tally = data[OffTALLY] | (data[OffTALLY + 1] << 8);

        return true;
    }

    /// <summary>
    /// CHECK checksum: sum of bytes 0x00-0x4B, keep low byte.
    /// The checksum byte itself (0x4B) is excluded from the sum.
    /// </summary>
    private static byte ComputeCHECK(byte[] data)
    {
        int sum = 0;
        for (int i = 0; i < OffCHK; i++)
            sum += data[i];
        return (byte)(sum & 0xFF);
    }

    /// <summary>
    /// CHK2 secondary checksum: simple XOR of bytes 0x00-0x49.
    /// </summary>
    private static byte ComputeCHK2(byte[] data)
    {
        byte xor = 0;
        for (int i = 0; i < OffCHK2; i++)
            xor ^= data[i];
        return xor;
    }

    /// <summary>
    /// Get the default save file path.
    /// </summary>
    public static string GetDefaultSavePath()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EliteRetro");
        Directory.CreateDirectory(appData);
        return Path.Combine(appData, "commander.bin");
    }

    /// <summary>
    /// Check if a save file exists.
    /// </summary>
    public static bool SaveExists(string? filePath = null)
    {
        filePath ??= GetDefaultSavePath();
        return File.Exists(filePath);
    }
}
