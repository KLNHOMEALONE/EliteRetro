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

### 3. Visual Proportions (Large Planet Feel)
To match the "large planet" feel of the original game, where the planet occupies a significant portion of the view during approach:
- **Original Visual Ratio:** 6,144 (Radius) / 65,536 (Distance) ≈ **0.09375**.
- **EliteRetro Implementation:** We increased the `PlanetRadius` by **2.5x** to **23,437**.
- **Result:** At the 100,000-unit arrival distance, the planet appears significantly more imposing and authentic to the retro memory.

## Summary of Constants (`GameConstants.cs`)

| Constant | Value | Description |
| :--- | :--- | :--- |
| `PlanetRadius` | 23,437 | 2.5x increase for "Large Planet" retro feel. |
| `SpeedMax` | 10f | ~2.7 min travel time to 100k planet at 60 FPS. |
| `StationOrbitalDistance` | 29,296 | 1.25 × PlanetRadius (standard Elite orbit). |
| `JumpOffset` | 100,000 | Matches arrival distance scale for hyperspace. |

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

To provide authentic and intuitive feedback during atmospheric maneuvers, the **Altitude (AL)** indicator and collision logic have been re-engineered:

### 1. High-Resolution Approach Scaling
Standard linear scaling makes the altitude bar feel static for too long. We implemented **Atmospheric Resolution Scaling**:
- **Behavior:** The altitude bar only begins to drop significantly when you are within **0.4 × PlanetRadius** (~9,300 units) of the surface.
- **Context:** At this distance, the planet's diameter is roughly **equal to the 3D viewport width**.
- **Pilot Feedback:** As you enter this "low altitude" phase, the planet will begin to exceed the screen edges, and the **AL bar** will simultaneously start its dramatic drop to zero, reinforcing the sensation of landing or high-speed overflight.

### 2. Visual Clearance Factor
Standard radial altitude (`distance - Radius`) can feel static during overflight maneuvers. We implemented a **Visual Clearance Factor** that artificially increases the altitude bar as the planet moves away from the ship's center-line (nose).
- **Formula:** `EffectiveAltitude = SurfaceDist + (1.0 - dot(Forward, ToPlanet)) * (PlanetRadius * 0.5)`
- **Behavior:** Pulling up (nose away from planet) visibly raises the altitude bar, matching the pilot's intuition of "climbing away" from the obstacle.

### 3. Glancing Collisions (Scrapes)
To prevent frustrating instant-crashes during low-altitude passes, collisions are now categorized based on approach angle:
- **Crash (Steep):** If the approach angle is > 60° (heading directly into the surface), the ship stops immediately (`Speed = 0`).
- **Scrape (Glancing):** If the approach angle is shallow, the ship takes significant hull damage (15 units) but is pushed back to the surface threshold, allowing the pilot to continue the maneuver.

### 4. Scaling & Feedback
- **Impact feel:** Immediate speed stops and "ALTITUDE CRITICAL" messages provide definitive feedback on surface interaction.
- **Synchronized Thresholds:** The physical collision boundary has been matched exactly to the planet radius (`PlanetRadius`), ensuring that as long as the **AL bar** is above zero, no collision will trigger, regardless of visual clearance or radial distance.
