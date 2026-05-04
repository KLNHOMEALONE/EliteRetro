namespace EliteRetro.Core;

/// <summary>
/// Centralized constants for EliteRetro game world scaling and limits.
/// All distances use Elite's internal coordinate system.
/// </summary>
public static class GameConstants
{
    // --- World Scale ---
    public const int PlanetRadius = 24576;
    public const int StationOrbitalDistance = 65536; // from planet center
    public const int BubbleRadius = 57344;
    public const int JumpOffset = 65536;

    // --- Sun distances (multipliers of planet radius) ---
    public const float SunHeatDistanceMultiplier = 2.67f;      // heat begins
    public const float SunFuelScoopDistanceMultiplier = 1.33f;  // fuel scooping range
    public const float SunFatalDistanceMultiplier = 0.90f;      // fatal proximity

    // --- Safe zone ---
    public const int SafeZoneRadius = 192; // local coords from orbit point

    // --- Energy bomb ---
    public const float EnergyBombMultiplier = 1.17f; // × planet diameter

    // --- Slot system ---
    public const int MinSlots = 12;
    public const int MaxSlots = 20;
    public const int PlanetSlot = 0;
    public const int SunStationSlot = 1;
    public const int PlayerSlot = 2;
    public const int FirstAvailableSlot = 3;

    // --- Orientation matrix ---
    public const float TidyThreshold = 0.0001f;
    public const int TidyInterval = 64; // TIDY every N frames per entity (round-robin)

    // --- Flight ---
    // Rotation rates per frame at 60fps. Roll ≈120°/sec, pitch ≈80°/sec.
    // Roll is faster than pitch, matching Elite's handling feel.
    public const float RollMax = 2f / 90f;     // ≈0.0222 rad/frame
    public const float PitchMax = 1f / 72f;    // ≈0.0139 rad/frame
    public const float AiRotationAngle = 1f / 16f; // 3.6 degrees for NPC rotation

    // ZX-style "digital input, smooth response" tuning.
    // Keys still map to -max/0/+max, but the commanded turn rate ramps toward the target.
    public const float TurnRampUpSeconds = 0.10f;    // reach max from 0
    public const float TurnRampDownSeconds = 0.15f;  // ease back to 0
    // 0 = no quantization. Otherwise quantize turn rate into N discrete steps across [-max, +max].
    public static int TurnQuantizationSteps = 0;

    // Speed control: max speed 40 units/sec, accel/decel in units/sec^2.
    public const float SpeedMax = 40f;
    public const float SpeedAccel = 30f;   // units/sec^2 when pressing W/S
    public const float SpeedDecel = 45f;   // units/sec^2 when releasing (stronger braking)

    // --- Galaxy ---
    public const int GalaxiesCount = 8;
    public const int SystemsPerGalaxy = 256;
    public const int TwistsPerSystem = 4;
}
