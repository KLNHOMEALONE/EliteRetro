using Microsoft.Xna.Framework;

namespace EliteRetro.Core.HUD;

/// <summary>
/// Dashboard state for HUD rendering — all values needed by HudRenderer.
/// </summary>
public struct HUDState
{
    // Bar indicators
    public float Speed;           // 0-40 (maps to 0-16 bar segments)
    public float Energy;          // 0-max_energy (ship-dependent)
    public float MaxEnergy;       // for scaling energy bar
    public float Fuel;            // 0-70 (maps to 0-16 bar segments)
    public float CabinTemp;       // 0-255
    public float LaserTemp;       // 0-255
    public float Altitude;        // 0-255
    public int EnergyBanks;       // 0-16
    public int Missiles;          // 0-max_missiles
    public int MaxMissiles;       // for scaling missile bar

    // Shield bars (forward/aft)
    public float ShieldForward;   // 0-255
    public float ShieldAft;       // 0-255

    // Orientation
    public float Pitch;           // -1 to 1
    public float Roll;            // 0 to 2π

    // Compass
    public float CompassHeading;  // 0 to 2π

    // ECM
    public int ECMBulbs;          // 0-3 (number of active bulbs)

    // Text indicators
    public string ViewMode;       // "FRONT"/"REAR"/"LEFT"/"RIGHT"
    public string StatusMessage;  // "STATION IN VIEW", "DANGER", etc.
    public Color StatusColor;     // color for status message

    // Legal status and combat rank
    public byte LegalStatus;      // 0=clean, 1-49=offender, 50+=fugitive
    public string CombatRank;     // "Harmless", "Elite", etc.

    // Hidden edges toggle
    public bool ShowHiddenEdges;
}
