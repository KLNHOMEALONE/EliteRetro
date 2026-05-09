# EliteRetro: Retro Experience Scaling Documentation

## Overview
This document outlines the architectural changes made to align **EliteRetro** with the original **BBC Micro Elite (1984)** flight experience. These changes affect the game's scale, pacing, and visual proportions.

## Rationale
The original Elite was designed for an 8-bit, 60 FPS (or 50 FPS PAL) environment with specific mathematical constraints. To achieve the same "feel," we transitioned from an ad-hoc modern scale to an authentic retro-proportional scale.

## Mathematical Model

### 1. Distance Scale
In the original Elite, the player starts at the **Witchpoint**, exactly **65,536 units** (0x010000) from the planet center. 
- **EliteRetro Implementation:** The planet spawns at a hi-byte distance shifted by 15 bits, resulting in a starting distance of **~100,000 units** (specifically 98,304 to 229,376 range based on system seed).
- **Sun Positioning:** The sun is placed **~150,000 to 230,000 units behind** the player, ensuring it feels distant and correctly occluded.

### 2. Speed and Travel Time
To match the original game's pacing (approx. 2.5 to 3 minutes of flight to reach the planet at max speed):
- **Max Speed:** Set to **10 units/frame**.
- **Math:** 100,000 units / (10 units/frame * 60 FPS) = **166.6 seconds** (~2.77 minutes).
- **Acceleration:** Tuned to **8 units/sec²** (Accel) and **12 units/sec²** (Decel) for a smooth but weighty inertia feel.

### 3. Visual Proportions
A critical aspect of the retro feel is the size of the planet on the screen.
- **Original Visual Ratio:** 6,144 (Radius) / 65,536 (Distance) ≈ **0.09375**.
- **EliteRetro Implementation:** With a distance of 100,000, we set `PlanetRadius` to **9,375**.
- **Result:** The planet appears at the exact same screen-size ratio as the 1984 original at the start of a flight.

## Summary of Constants (`GameConstants.cs`)

| Constant | Value | Description |
| :--- | :--- | :--- |
| `PlanetRadius` | 9,375 | Preserves 0.09375 visual ratio at 100k distance. |
| `SpeedMax` | 10f | ~2.7 min travel time to 100k planet at 60 FPS. |
| `StationOrbitalDistance` | 11,718 | 1.25 × PlanetRadius (standard Elite orbit). |
| `JumpOffset` | 100,000 | Matches starting distance scale for hyperspace arrival. |

## Implementation Details

### Scene Initialization (`FlightScene.cs`)
The `ComputeSolarSpawn` routine was adjusted to use the new 15-bit scaling factor:
```csharp
// Scaling: << 15 instead of << 16 yields ~100,000 units
int planetZHi = ((seed.W0Hi & 0x07) + 6 + (fistBit0 & 1)) >> 1;
Vector3 planetPos = new Vector3(..., +(planetZHi << 15));

// Sun magnitude in range [4..7] shifted by 15 bits
int sunZHi = (seed.W1Hi & 0x03) + 4;
Vector3 sunPos = new Vector3(..., -(sunZHi << 15));
```

### Global Speed Consistency
All hardcoded references to the previous `40f` speed cap were replaced with `GameConstants.SpeedMax`. This ensures that:
1. **HUD Speed Bars** scale correctly (0–16 segments map to 0–10 units).
2. **Stardust Motion Blur** (dashes) triggers and scales relative to the new max speed.
3. **Player Ship Initialization** starts with the correct max speed capability.

## Planet Proximity and Altitude Scaling

To provide authentic feedback during atmospheric descent, the **Altitude (AL)** indicator has been re-calibrated:
- **Surface-Relative:** Altitude is now measured as `distance - PlanetRadius`, ensuring the bar hits zero exactly at the surface.
- **Dynamic Range:** The bar is scaled such that a distance of **2.0 × PlanetRadius** (one full diameter above the surface) represents a full bar (255). This provides high-resolution feedback for the final approach.
- **Immediate Stop:** Upon surface impact, `PlayerSpeed` is immediately set to **0** and all movement/rotation is frozen to prevent "glitchy" jitter or planet-piercing, matching the definitive "thud" of a crash or landing.
