# EliteRetro — Comprehensive Implementation Plan

## Vision

Authentic BBC Elite recreation in C# with MonoGame: wireframe 3D ships, procedurally generated galaxies, local bubble entity management, Minsky circle-algorithm flight, space station docking, and all original game systems.

---

## Architecture Overview

```
GameInstance (1024x768 MonoGame)
  └─ SceneManager (stack-based)
       ├─ MainMenuScene ─ current: ship showcase + menu
       ├─ SpaceScene ─ current: wireframe viewer (Euler angles)
       ├─ GalaxyMapScene ─ current: 8×256 star systems
       └─ [NEW] FlightScene ─ target: first-person cockpit view

Core Systems (to implement):
  ├─ LocalBubbleManager ─ slot-based entity lifecycle
  ├─ FlightController ─ Minsky universe rotation
  ├─ GalaxyGenerator ─ exists; needs Tribonacci seeding
  ├─ ShipAI ─ tactics, spawning, combat
  ├─ CircleRenderer / EllipseRenderer ─ planets, suns
  └─ PlanetRenderer ─ craters, meridians, shading
```

---

## Phase 0: Foundation (Core Data & Constants)

**Goal:** Centralized constants, entity types, and data models.

### Files to Create

| File | Purpose |
|------|---------|
| `src/EliteRetro.Core/GameConstants.cs` | All scale values: planet radius (24576), station distance (65536), bubble radius (57344), slot counts |
| `src/EliteRetro.Core/Entities/EntityInstance.cs` | Runtime wrapper: position, velocity, orientation matrix, speed, energy, slot index |
| `src/EliteRetro.Core/Entities/OrientationMatrix.cs` | 3×3 rotation matrix (nosev/roofv/sidev), Minsky rotation, TIDY orthonormalization |
| `src/EliteRetro.Core/Entities/ShipBlueprint.cs` | Static ship data: vertices, edges, faces, characteristics (bounty, speed, energy, cargo) |
| `src/EliteRetro.Core/Entities/ShipInstance.cs` | 36-byte-equivalent runtime data: position, orientation, speed, flags, AI |
| `src/EliteRetro.Core/Entities/PlanetModel.cs` | Planet wireframe (icosahedron-based) |
| `src/EliteRetro.Core/Entities/SunModel.cs` | Sun wireframe model |
| `src/EliteRetro.Core/Systems/FlightController.cs` | Keyboard input → pitch/roll angles, angle scaling |
| `src/EliteRetro.Core/Managers/LocalBubbleManager.cs` | Slot array, spawn/despawn, culling, safe zone |

### Files to Modify

| File | Changes |
|------|---------|
| `src/EliteRetro.Core/GameInstance.cs` | Own `LocalBubbleManager`, `FlightController` |
| `src/EliteRetro.Core/Scenes/SpaceScene.cs` | Replace Euler angles with Minsky rotation |
| `src/EliteRetro.Core/Scenes/MainMenuScene.cs` | Wire "Start New Game" to new `FlightScene` |
| `src/EliteRetro.Core/Systems/GalaxyGenerator.cs` | Replace simple RNG with Tribonacci twist |

---

## Phase 1: Local Bubble & Entity Lifecycle

**Goal:** Fixed-capacity slot system with reserved slots for planet (slot 0) and sun/station (slot 1).

### Key Mechanics

- **12 slots** (BBC Micro standard),可扩展 to 20
- **Slot 0:** Planet (always present)
- **Slot 1:** Sun OR Station (mutually exclusive)
- **Slots 2+:** Ships, missiles, asteroids, cargo
- **Culling:** Entities beyond 57,344 coordinates from player
- **Safe zone:** Player within 192 local coords of orbit point → station spawns, sun removed

### Implementation Steps

1. Create `LocalBubbleManager` with `EntityInstance?[20]` slot array
2. Implement `SpawnShip()` — find first empty slot from index 2
3. Implement `DespawnShip()` — mark destroyed, shuffle down
4. Implement `CullBeyondBubble()` — distance check per frame
5. Implement safe zone trigger: `orbitPoint = planetPos + 2 * planetNosev * PlanetRadius`
6. Station orientation: invert nosev to face planet
7. Sun distance effects: heat (>2.67r), fuel scoop (>1.33r), fatal (<0.90r)
8. Energy bomb: 1.17 × planet diameter blast radius

---

## Phase 2: Minsky Flight System

**Goal:** Universe rotates around player (not player rotates). Small-angle approximations for pitch/roll.

### Key Mechanics

- **Minsky circle algorithm:** `K2 = y - α·x; z = z + β·K2; y = K2 - β·z; x = x + α·y`
- **Roll (α):** Q/W keys, range 0–0.125 rad (angle/256)
- **Pitch (β):** Up/Down keys, range 0–0.03125 rad (angle/256)
- **Entity forward movement:** Along its own nosev vector
- **Entity AI rotation:** Fixed angle 1/16 rad (3.6°)
- **TIDY routine:** Periodic orthonormalization (round-robin across entities)
- **View switching:** Front/Rear/Left/Right via axis flipping

### Implementation Steps

1. Create `OrientationMatrix` with identity, RotatePosition, ApplyUniverseRotation
2. Implement `Tidy()` — normalize nosev, orthogonalize roofv, cross-product sidev
3. Create `FlightController` — read arrow keys, Q/W, scale by /256
4. Modify `SpaceScene` — replace `_rotation` Euler angles with Minsky
5. Each entity: rotate position → apply player velocity → rotate orientation → apply own rotation → move forward
6. Add view switching (V key cycles Front/Rear/Left/Right)

---

## Phase 3: Galaxy Generation (Tribonacci)

**Goal:** Replace current simple RNG with authentic Tribonacci twist algorithm.

### Key Mechanics

- **Three 16-bit seeds:** s0=0x5A4A, s1=0x0248, s2=0xB753 (Tibedied)
- **Twist:** `s0'=s1, s1'=s2, s2'=s0+s1+s2` (16-bit wraparound)
- **4 twists per system**, 256 systems per galaxy, 8 galaxies
- **Next galaxy:** Rotate each byte left by 1 bit
- **Name generation:** 2-letter token table (31 entries), 3-4 tokens per name
- **System data from seed bits:**
  - Economy: `s0_hi & 0b111` (with anarchy/feudal constraint)
  - Government: `(s1_lo >> 3) & 0b111`
  - Tech level: `flipped_economy + (s1_hi & 0b11) + (gov / 2)`
  - Population: `techLevel * 4 + economy + government + 1`
  - Species: bit 7 of s2_lo (human vs alien)

### Implementation Steps

1. Create `GalaxySeed` struct with Twist() and NextGalaxy()
2. Implement system data derivation (TT24/TT25)
3. Implement name generation (cpl routine) with token table
4. Verify: Lave (Galaxy 0, System 1) = Dictatorship, Rich Agri, Tech 5, Pop 25, Radius 4116
5. Replace current `GalaxyGenerator` implementation

---

## Phase 4: Circle & Planet Rendering

**Goal:** Parametric circles, conjugate-diameter ellipses, planet surface features.

### Files to Create

| File | Purpose |
|------|---------|
| `src/EliteRetro.Core/Rendering/SineTable.cs` | 64-entry sine lookup (0–2π) |
| `src/EliteRetro.Core/Rendering/CircleRenderer.cs` | Parametric circle drawing |
| `src/EliteRetro.Core/Rendering/EllipseRenderer.cs` | Conjugate-diameter ellipses |
| `src/EliteRetro.Core/Rendering/PlanetRenderer.cs` | Craters, meridians, equators |
| `src/EliteRetro.Core/Rendering/SunRenderer.cs` | Scan-line sun with fringe |
| `src/EliteRetro.Core/Rendering/RingRenderer.cs` | Saturn-style rings |
| `src/EliteRetro.Core/Rendering/ExplosionRenderer.cs` | Particle explosion clouds |
| `src/EliteRetro.Core/Rendering/StardustRenderer.cs` | Starfield particle system |

### Implementation Steps

1. SineTable: 64 entries, Sin(step), Cos(step) with quadrant wrapping
2. CircleRenderer: iterate CNT 0–64, project via sine table, draw line segments
3. EllipseRenderer: `P(θ) = center + cos(θ)·u + sin(θ)·v`
4. PlanetRenderer: craters (small ellipses), meridians (half-ellipses with start angle)
5. SunRenderer: horizontal scan lines with random fringe
6. RingRenderer: random points in elliptical band
7. ExplosionRenderer: expanding/contracting particle sphere
8. StardustRenderer: perspective-correct star particles with roll/pitch

---

## Phase 5: Flight Scene (New Game Flow)

**Goal:** "Start New Game" → player appears in local bubble, facing planet, station visible.

### New Scene: `FlightScene`

```
Flow:
1. Player spawns at origin, planet at (0, 0, -24576)
2. Sun placed at distance 2.67–18.67 planet radii
3. Player flies toward planet
4. When within safe zone (192 coords of orbit point) → station spawns
5. Station nosev inverted to face planet
6. Player can dock (future) or fly away
```

### Implementation Steps

1. Create `FlightScene : GameScene`
2. On enter: initialize `LocalBubbleManager`, place planet (slot 0), sun (slot 1)
3. Use `FlightController` + Minsky rotation for all entities
4. Render planet via `PlanetRenderer` (large circle with features)
5. Render sun via `SunRenderer` (scan lines)
6. Render other entities via `WireframeRenderer`
7. HUD overlay: speed, energy, compass, scanner
8. Safe zone check → spawn station, remove sun

---

## Phase 6: Ship AI & Combat

**Goal:** NPC ships with tactics, spawning, combat behavior.

### Ship Personalities

| Behavior | Ships |
|----------|-------|
| Hostile/Pirate | Sidewinder, Mamba, Cobra Mk3 (pirate) |
| Bounty Hunter | Viper, Fer-de-Lance, Asp Mk II |
| Trader | Cobra Mk3, Python, Anaconda |
| Cop | Viper |
| Innocent | Python, Anaconda |

### Implementation Steps

1. Spawn system: danger level × altitude → ship type selection
2. AI tactics: aggression (0-63) → attack, flee, circle
3. Combat: laser firing, missile launch, E.C.M.
4. Collision detection: entity vs entity
5. Bounty system: kills → rating increase
6. Cargo release: destroyed ships drop canisters

---

## Phase 7: Game Systems

**Goal:** Economy, trading, equipment, missions.

### Implementation Steps

1. Market prices: `price = (base + (rand & mask) + economy × factor) × 4`
2. Commodity availability: `(base_qty + (rand & mask) - economy × factor) mod 64`
3. Player inventory: cargo hold, equipment slots
4. Fuel scooping: near sun (1.33 radii)
5. Docking: approach station within range, align to slot
6. Mission system: delivery, assassination, mining

---

## File Inventory

### New Files (30)

```
src/EliteRetro.Core/GameConstants.cs
src/EliteRetro.Core/Entities/EntityInstance.cs
src/EliteRetro.Core/Entities/OrientationMatrix.cs
src/EliteRetro.Core/Entities/ShipBlueprint.cs
src/EliteRetro.Core/Entities/ShipInstance.cs
src/EliteRetro.Core/Entities/PlanetModel.cs
src/EliteRetro.Core/Entities/SunModel.cs
src/EliteRetro.Core/Systems/FlightController.cs
src/EliteRetro.Core/Managers/LocalBubbleManager.cs
src/EliteRetro.Core/Rendering/SineTable.cs
src/EliteRetro.Core/Rendering/CircleRenderer.cs
src/EliteRetro.Core/Rendering/EllipseRenderer.cs
src/EliteRetro.Core/Rendering/PlanetRenderer.cs
src/EliteRetro.Core/Rendering/SunRenderer.cs
src/EliteRetro.Core/Rendering/RingRenderer.cs
src/EliteRetro.Core/Rendering/ExplosionRenderer.cs
src/EliteRetro.Core/Rendering/StardustRenderer.cs
src/EliteRetro.Core/Scenes/FlightScene.cs
src/EliteRetro.Core/Systems/ShipAISystem.cs
src/EliteRetro.Core/Systems/MarketSystem.cs
src/EliteRetro.Core/Systems/SpawnSystem.cs
src/EliteRetro.Core/Systems/CollisionSystem.cs
src/EliteRetro.Core/Systems/DockingSystem.cs
src/EliteRetro.Core/Utilities/ShipBlueprintLoader.cs
src/EliteRetro.Core/Utilities/NameGenerator.cs
src/EliteRetro.Core/Utilities/TribonacciTwist.cs
src/EliteRetro.Core/Entities/ShipType.cs
src/EliteRetro.Core/Entities/ShipSpecifications.cs
src/EliteRetro.Core/HUD/HudRenderer.cs
src/EliteRetro.Core/Audio/AudioManager.cs
```

### Modified Files (8)

```
src/EliteRetro.Core/GameInstance.cs
src/EliteRetro.Core/Scenes/SpaceScene.cs
src/EliteRetro.Core/Scenes/MainMenuScene.cs
src/EliteRetro.Core/Scenes/GalaxyMapScene.cs
src/EliteRetro.Core/Systems/GalaxyGenerator.cs
src/EliteRetro.Core/Rendering/WireframeRenderer.cs
src/EliteRetro.Core/Entities/ShipModel.cs
src/EliteRetro.DesktopGL/Program.cs
```

---

## Implementation Order (Priority)

1. **GameConstants + EntityInstance** — foundation for everything
2. **LocalBubbleManager** — entity lifecycle
3. **OrientationMatrix + FlightController** — flight physics
4. **FlightScene** — new game experience
5. **Tribonacci GalaxyGenerator** — authentic procedural generation
6. **CircleRenderer + PlanetRenderer** — visual polish
7. **ShipAI + SpawnSystem** — living galaxy
8. **MarketSystem + DockingSystem** — game loop

---

## Verification Checklist

- [ ] Lave (Galaxy 0, System 1) generates with correct stats
- [ ] Tibedied (Galaxy 0, System 0) name matches
- [ ] Planet radius = 24,576, station distance = 65,536
- [ ] Safe zone triggers at 192 local coords
- [ ] Minsky rotation maintains orthonormality over 1000+ iterations
- [ ] 12-slot bubble enforces capacity
- [ ] Back-face culling produces solid-looking wireframes
- [ ] Station spawns with nosev facing planet
