# Local Bubble Implementation Plan

## 1. Overview

The local bubble is the coordinate space surrounding the player ship that contains all active game entities. Due to 8-bit hardware constraints, the original Elite used a fixed-capacity slot system with reserved slots for celestial bodies. This plan recreates that architecture in C# with MonoGame.

### Key Sources
- [The Local Bubble of Universe](https://elite.bbcelite.com/deep_dives/the_local_bubble_of_universe.html)
- [A Sense of Scale](https://elite.bbcelite.com/deep_dives/a_sense_of_scale.html)
- [The Space Station Safe Zone](https://elite.bbcelite.com/deep_dives/the_space_station_safe_zone.html)

## 2. Coordinate System & Scale

### World Coordinates (universe-scale)
| Quantity | Value | Notes |
|----------|-------|-------|
| Planet/Sun radius | 24,576 | Core unit of scale |
| Planet/Sun diameter | 49,152 | 2 × radius |
| Station orbital distance (from planet center) | 65,536 | Exactly 2.67 × radius |
| Station altitude (above surface) | 40,960 | 65,536 - 24,576 |
| Local bubble radius | 57,344 | Entities beyond this are culled |
| Cobra Mk III top speed | 40 | Coordinates per iteration |
| In-system jump offset | 65,536 | 2.67 × planet radii |

### Local Coordinates (ship-model scale)
| Quantity | Value | Notes |
|----------|-------|-------|
| Ship bounding box | +/- 255 | All ship wireframes fit within |
| Planet radius (local) | (96 0) | 96 in high byte = 24,576 |
| Safe zone trigger distance | (192 0) | 2 × planet radius |

### Sun Interaction Radii
| Zone | Distance | Effect |
|------|----------|--------|
| Heat radius | 2.67 × sun radii | Temperature begins rising |
| Fuel scooping | 1.33 × sun radii | Optimal range for fuel scoop |
| Fatal limit | 0.90 × sun radii | Ship destroyed (10% into sun) |

## 3. Data Structures

### 3.1 GameConstants (new file: `src/EliteRetro.Core/GameConstants.cs`)

```csharp
public static class GameConstants
{
    // Local bubble capacity
    public const int MaxShipSlots = 12;        // BBC Micro standard
    public const int MaxShipSlotsExtended = 20; // 6502 Second Processor

    // Scale (world coordinates)
    public const int PlanetRadius = 24576;
    public const int PlanetDiameter = 49152;
    public const int StationOrbitalDistance = 65536; // from planet center
    public const int StationAltitude = 40960;        // above surface
    public const int BubbleRadius = 57344;
    public const int JumpOffset = 65536;
    public const int CobraTopSpeed = 40;

    // Sun
    public const float HeatRadiusMultiplier = 2.67f;
    public const float FuelScoopRadiusMultiplier = 1.33f;
    public const float FatalRadiusMultiplier = 0.90f;

    // Safe zone
    public const int SafeZoneTriggerDistance = 192;  // local coords, 2r
    public const int PlanetRadiusLocal = 96;          // local coords

    // Ship data
    public const int ShipDataBlockSize = 36; // NI% = 36 bytes per ship

    // Energy bomb
    public const float EnergyBombBlastRadius = 1.17f; // planet diameters
}
```

### 3.2 EntityInstance (new file: `src/EliteRetro.Core/Entities/EntityInstance.cs`)

Runtime wrapper around a `ShipModel` with position, velocity, and state:

```csharp
public class EntityInstance
{
    public ShipModel Model { get; }
    public Vector3 Position { get; set; }      // World coordinates
    public Vector3 Velocity { get; set; }      // Coordinates per iteration
    public Matrix Rotation { get; set; }       // 3×3 rotation (nosev/roofv/sidev)
    public float Energy { get; set; }
    public int SlotIndex { get; set; }
    public bool IsDestroyed { get; set; }
}
```

### 3.3 LocalBubbleManager (new file: `src/EliteRetro.Core/Managers/LocalBubbleManager.cs`)

Core manager class. Responsibilities:
- Maintain fixed-size entity slot array
- Enforce reserved slot semantics (planet in slot 0, sun/station in slot 1)
- Spawn/despawn entities
- Cull entities beyond bubble radius
- Calculate safe zone trigger

```csharp
public class LocalBubbleManager
{
    // Slot 0: Planet (always present)
    // Slot 1: Sun OR Station (mutually exclusive)
    // Slots 2..N: Active ships, missiles, asteroids, cargo

    private readonly EntityInstance?[] _slots;
    private readonly int _maxSlots;

    public EntityInstance? Planet => _slots[0];
    public EntityInstance? Sun => _slots[1]?.Model is SunModel ? _slots[1] : null;
    public EntityInstance? Station => _slots[1]?.Model is CoriolisStationModel ? _slots[1] : null;

    public IEnumerable<EntityInstance> Ships =>
        _slots.Skip(2).Where(s => s != null && !s.IsDestroyed)!;

    public void SpawnShip(ShipModel model, Vector3 position);
    public void DespawnShip(int slotIndex);
    public void CullBeyondBubble(Vector3 playerPosition);
    public void UpdateSafeZone(Vector3 playerPosition);
    public void TriggerEnergyBomb(Vector3 playerPosition);
}
```

## 4. Slot System

### 4.1 Slot Allocation

| Slot | Reserved For | Notes |
|------|-------------|-------|
| 0 | Planet | Always present, never removed |
| 1 | Sun / Station | Mutually exclusive — spawning station removes sun |
| 2+ | Dynamic entities | Ships, missiles, asteroids, cargo, escape pods |

### 4.2 Slot Operations

**Spawning (NWSHP equivalent):**
1. Find first empty slot from slot 2 upward
2. If no empty slot, do not spawn (bubble is full)
3. Create `EntityInstance`, assign position relative to player
4. Add to slot

**Despawning (KILLSHP equivalent):**
1. Mark entity as destroyed
2. Remove from slot
3. Shuffle remaining entities down to close gap (preserves contiguous allocation)

**Culling:**
1. Each frame, check distance from player to each dynamic entity
2. If distance > `BubbleRadius` (57,344), despawn
3. Planet and Sun/Station slots are never culled

## 5. Space Station Safe Zone

### 5.1 Orbit Point Calculation

The station's orbit point is derived from the planet's orientation:

```
orbitPoint = planetPosition + 2 × planetNosev × PlanetRadius
```

Where `planetNosev` is the planet's nose vector (the forward orientation vector from the planet's rotation matrix). This places the point at exactly 1 planet radius above the surface.

### 5.2 Safe Zone Trigger

Each frame, compute distance from player to orbit point:

```csharp
Vector3 orbitPoint = planet.Position + 2 * planet.Nosev * GameConstants.PlanetRadius;
Vector3 delta = playerPosition - orbitPoint;

if (Math.Abs(delta.X) <= 192 &&
    Math.Abs(delta.Y) <= 192 &&
    Math.Abs(delta.Z) <= 192)
{
    // Player is in safe zone — spawn station, remove sun
}
```

The check uses per-axis bounds (a bounding box), not Euclidean distance. The threshold `(192 0)` = 2 × planet radius in local coordinates.

### 5.3 Station Spawning (NWSPS equivalent)

When the safe zone is triggered:
1. Remove the Sun from slot 1 (if present)
2. Create station `EntityInstance` at the orbit point
3. Place in slot 1
4. Invert the station's `nosev` vector so the docking slot faces the planet center:

```csharp
station.Nosev = -planet.Nosev;
```

The station remains fixed at the orbit point — it does not orbit dynamically.

## 6. Sun Mechanics

### 6.1 Sun Spawning

When the player enters a system (in-system jump):
1. Calculate sun direction from system data
2. Place sun at distance between 2.67 and 18.67 planet radii from planet
3. Assign to slot 1

### 6.2 Sun Effects

Each frame while sun is present:
1. Calculate distance from player to sun
2. Apply effects based on distance:
   - `< 0.90 × sunRadius`: Destroy ship (fatal)
   - `< 1.33 × sunRadius`: Enable fuel scooping
   - `< 2.67 × sunRadius`: Apply heat damage over time

## 7. Energy Bomb

When an energy bomb is activated:

```csharp
float blastRadius = GameConstants.EnergyBombBlastRadius * GameConstants.PlanetDiameter;

for (int i = 2; i < _maxSlots; i++)
{
    if (_slots[i] != null &&
        Vector3.Distance(playerPosition, _slots[i].Position) <= blastRadius)
    {
        DespawnShip(i); // Clear all non-reserved slots in blast radius
    }
}
```

Blast radius = 1.17 × planet diameter = 57,507 coordinates.

## 8. Implementation Steps

### Phase 1: Foundations
1. Create `GameConstants.cs` with all scale values
2. Create `EntityInstance.cs` — runtime entity wrapper
3. Create `PlanetModel.cs` — simple sphere/icosahedron wireframe
4. Create `SunModel.cs` — sun wireframe (same geometry as planet, different rendering)

### Phase 2: Local Bubble Manager
5. Create `LocalBubbleManager.cs` in new `Managers/` directory
6. Implement slot array with reserved slots (0=planet, 1=sun/station)
7. Implement spawn/despawn with table shuffling
8. Implement bubble boundary culling (distance > 57,344)

### Phase 3: Safe Zone
9. Implement orbit point calculation from planet nosev
10. Implement safe zone trigger (bounding box check at 192 local coords)
11. Implement station spawn / sun removal on trigger
12. Implement station orientation (nosev inversion toward planet)

### Phase 4: Sun Mechanics
13. Implement sun distance effects (heat, fuel scoop, fatal)
14. Implement in-system jump positioning (sun at 2.67-18.67 radii)

### Phase 5: Integration
15. Modify `SpaceScene.cs` to use `LocalBubbleManager`
16. Modify `GameInstance.cs` to initialize and own `LocalBubbleManager`
17. Wire up entity rendering through `WireframeRenderer`
18. Add energy bomb support

## 9. Files to Create

| File | Purpose |
|------|---------|
| `src/EliteRetro.Core/GameConstants.cs` | Centralized constants |
| `src/EliteRetro.Core/Entities/EntityInstance.cs` | Runtime entity wrapper |
| `src/EliteRetro.Core/Entities/PlanetModel.cs` | Planet wireframe model |
| `src/EliteRetro.Core/Entities/SunModel.cs` | Sun wireframe model |
| `src/EliteRetro.Core/Managers/LocalBubbleManager.cs` | Core bubble management |

## 10. Files to Modify

| File | Changes |
|------|---------|
| `src/EliteRetro.Core/GameInstance.cs` | Initialize LocalBubbleManager |
| `src/EliteRetro.Core/Scenes/SpaceScene.cs` | Use LocalBubbleManager for entity lifecycle |
| `src/EliteRetro.Core/Rendering/WireframeRenderer.cs` | Support planet/sun rendering at scale |

## 11. Open Questions

1. **Planet rendering at scale**: The planet radius (24,576) far exceeds the ship bounding box (+/- 255). Need a separate rendering path or level-of-detail system for the planet disc.
2. **Coordinate system bridging**: Need clear conversion between local bubble coordinates and the galaxy map's 2D `Vector2` positions.
3. **Ship AI**: The local bubble manages positions but ship behavior (attack, flee, trade) is a separate system not covered here.
4. **Collision detection**: Not covered — needs a separate collision system that queries the bubble's entity list.
