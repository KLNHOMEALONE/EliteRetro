# Local Bubble Game Logic Implementation Plan

## 1. Background & Motivation
The original 8-bit *Elite* used a clever "local bubble" system to simulate the universe around the player's ship due to hardware limitations. This document outlines the logic and scale requirements to accurately recreate this local bubble, including the space station's safe zone and relative scale, for the EliteRetro project.

## 2. Objective
To implement a robust `LocalBubbleManager` that mimics the constraints, scale, and lifecycle mechanics of the original Elite's local bubble, ensuring authenticity in entity management, distances, and spatial awareness.

## 3. Data Structures & Capacity (The Local Bubble)
*   **Slot Management (`FRIN` / `UNIV` equivalent):** Create a fixed-size array or list to hold active entities within the bubble.
*   **Capacity Limit:** Restrict the maximum number of entities (ship slots) to 12 (BBC Micro standard) or up to 20 (6502 Second Processor standard) based on configuration.
*   **Reserved Slots:**
    *   **Slot 1:** Always reserved for the **Planet**.
    *   **Slot 2:** Exclusively shared between the **Sun** and the **Space Station** (they cannot exist in the bubble simultaneously).
    *   **Slots 3+:** Used for active ships, missiles, asteroids, and cargo canisters.
*   **Entity Data (`K%` equivalent):** Each entity must track 3D position, speed, rotation vectors (`nosev`, `roofv`, `sidev`), and energy levels.

## 4. Scale and Proportions
*   **Coordinate System:** Use a bounding box of +/- 255 scaled units for ship geometry.
*   **Planetary Scale:**
    *   **Planet/Sun Radius:** 24,576 units.
    *   **Distance to Station:** The station is located 65,536 coordinates from the planet's center (approx. 1.67 planet radii above the surface).
*   **Sun Boundaries:**
    *   **Heat Radius:** Temperature rises at 2.67 sun radii.
    *   **Fuel Scooping:** Optimal at 1.33 sun radii.
    *   **Fatal Limit:** Ship is destroyed at 0.90 sun radii (10% into the sun).
*   **Movement Limits:**
    *   **Top Speed:** Cobra Mk III top speed is 40 coordinates per iteration.
    *   **Bubble Radius:** 57,344 coordinates (approx. 2.34 planet radii). Entities beyond this are removed.

## 5. Space Station Safe Zone Logic
*   **The Orbiting Point:** Calculate a dynamic point in space that "orbits" the planet. This point is positioned exactly one planet radius above the surface and moves with the planet's pitch and yaw rotation.
*   **Spawning the Station:** Continuously monitor the distance between the player's ship and this orbiting point. If the player comes within a distance of `2r` (where `r` is planet radius, approx. 192 scaled units locally), trigger the station spawn (`NWSPS` equivalent).
*   **Station Orientation:** Upon spawning, invert the `nosev` vector to ensure the docking slot always faces directly toward the planet's center.
*   **Sun Removal:** When the space station spawns in Slot 2, the Sun must be removed from the bubble.

## 6. Entity Lifecycle (Spawning and Despawning)
*   **Spawning (`NWSHP` equivalent):** Logic to populate empty slots with new ships, asteroids, or cargo depending on the system's danger level and current altitude.
*   **Despawning (`KILLSHP` equivalent):** 
    *   Remove entities that are destroyed.
    *   Remove entities that travel beyond the local bubble radius (57,344 coordinates) from the player.
*   **Energy Bomb:** Implement a blast radius of 1.17 planet diameters, clearing all non-reserved slots in the local bubble when activated.

## 7. Implementation Steps
1. Create `LocalBubbleManager` class to handle slot allocation and entity tracking.
2. Define coordinate and scale constants in a `GameConstants` or similar file.
3. Implement the Planet and Sun/Station reserved slot logic.
4. Add the distance checks for the bubble boundary to cull distant entities.
5. Implement the Safe Zone calculation (orbiting point) and trigger station spawning/sun despawning.
6. Verify orientations and speeds align with original 8-bit values.
