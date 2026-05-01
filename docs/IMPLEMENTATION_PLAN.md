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

## Phase 1.5: Main Loop Counter (Task Scheduling)

**Goal:** Authentic MCNT-driven task scheduler that spreads work across frames using modulo arithmetic, preventing frame spikes.

### Key Mechanics

- **MCNT counter:** 8-bit value cycling 0-255, decrements each frame
- **Modulo via AND:** `MCNT & (n-1)` checks divisibility by power-of-2 intervals
- **Offset scheduling:** `MCNT & mask == offset` spreads tasks within a cycle

### Scheduled Tasks (from original)

| Mask | Offset | Frequency | Task |
|------|--------|-----------|------|
| 0b11 | 0 | Every 4 | Update dashboard indicators |
| 0b111 | 0 | Every 8 | Regenerate ship energy/shields |
| 0b111 | 0-3 | Every 8 | Apply tactics to ships 1-2 |
| 0b1111 | 0 | Every 16 | Flash dashboard dials (on) |
| 0b1111 | 8 | Every 16 | Flash dashboard dials (off) |
| 0b1111 | 0-11 | Every 16 | Tidy ship orientation vectors |
| 0b11111 | 0 | Every 32 | Check station proximity/spawn |
| 0b11111 | 10 | Every 32 | Calculate altitude, crash landing, low energy warning |
| 0b11111 | 20 | Every 32 | Sun altitude, cabin temp, fuel scooping |
| 0b11111111 | 0 | Every 256 | Consider spawning a new ship |

### Counter Resets

- **Set to 0:** After fueling, launching, hyperspace arrive → delays spawning 256 iterations
- **Set to 1:** After in-system jump → immediate spawn consideration

### Implementation Steps

1. Create `MainLoopCounter` class — MCNT field (byte), Decrement() wrapping 255→0, Reset(byte value)
2. Create `TaskScheduler` — RegisterTask(mask, offset, action), Evaluate(mcnt) method
3. Register all scheduled tasks from table above
4. Integrate into `FlightScene.Update()` — decrement MCNT, evaluate all tasks
5. Wire counter resets to fuel/dock/launch/hyperspace events
6. Replace round-robin TIDY (Phase 2.7) with MCNT-based scheduling (every 16, offsets 0-11)

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
| `src/EliteRetro.Core/Rendering/ExplosionRenderer.cs` | Vertex-based explosion clouds |
| `src/EliteRetro.Core/Rendering/StardustRenderer.cs` | Starfield particle system |

### Implementation Steps

1. SineTable: 64 entries, Sin(step), Cos(step) with quadrant wrapping
2. CircleRenderer: iterate CNT 0–64, project via sine table, draw line segments
3. EllipseRenderer: `P(θ) = center + cos(θ)·u + sin(θ)·v`
4. PlanetRenderer: craters (small ellipses), meridians (half-ellipses with start angle)
5. SunRenderer: horizontal scan lines with random fringe
6. RingRenderer: random points in elliptical band
7. ExplosionRenderer: vertex-based explosion clouds — store cloud size, counter (starts at 18, +4 per frame, expands to 128 then shrinks), explosion count (first n vertices from blueprint), 4 random seed bytes for reproducible redraws. Render: erase old cloud, increment counter, compute size = counter/distance, for each origin vertex plot random particles within radius (count modulated by counter: increases until 128, then decreases)
8. StardustRenderer: 16-bit sign-magnitude star coordinates (SX, SY, SZ). Per frame: `q = 64 * speed / z_hi; z -= speed*64; y += |y_hi|*q; x += |x_hi|*q`. Roll: `y += alpha*x/256; x -= alpha*y/256`. Pitch: `y -= beta*256; x += 2*(beta*y/256)^2`. Side/rear views: different transformation stages (sideways movement, pitch rotation around mid-point, roll vertical movement). Stars wrap around on overflow.

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

### Ship Personalities (NEWB flags, byte #37)

| Bit | Name | Effect |
|-----|------|--------|
| 0 | Trader | Flies between station/planet |
| 1 | Bounty hunter | Attacks fugitives |
| 2 | Hostile | Attacks on sight |
| 3 | Pirate | Stops attacking in safe zone |
| 4 | Docking | Traders head to station (dynamic) |
| 5 | Innocent | Station defends them |
| 6 | Cop | Destroying makes player fugitive |
| 7 | Scooped | Docked or escape pod (dynamic) |

### Tactics Routine (TACTICS / MVEIT)

**Entry:** Called via MVEIT for ships with bit 7 set in byte #32, 1-2 ships per frame (MCNT every 8, offsets 0-3).

**Flow:**
1. **Energy recharge:** +1 per iteration
2. **Part 3 (Targeting):** Dot product of ship's nosev with vector to player — determines if enemy can hit with lasers
3. **Part 4 (Energy check):** 2.5% chance of random roll. If energy ≥ half → laser consideration. If very low and 10% unlucky → bail (launch escape pod, drift)
4. **Part 5 (Missile):** If has missiles, no E.C.M. active → randomly fire (Thargoids release Thargon)
5. **Part 6 (Laser):** If pointing at player but inaccurate → fire. If player in crosshairs → register damage, slow attacker, play hit sound
6. **Part 7 (Movement):** Vector XX15 (victim→attacker) determines direction:
   - Traders/escape pods: toward planet
   - Close/not aggressive ships: away from player
   - Aggressive ships: toward player
   - Missiles: home toward target
   - Set pitch/roll, adjust speed

### Targeting (HITCH routine)

Checks if ship is targetable:
1. Ship in front (z_sign positive)
2. x_hi and y_hi both 0 (close to crosshairs center)
3. Calculate `(S R) = x_lo² + y_lo²` (distance² from crosshair center)
4. Compare against blueprint's **targetable area** — if less, ship can be locked/hit

### Aggression (0-63)

Stored in byte #32 bits 1-6. Higher values increase probability of turning toward target. Separate from hostility flag (bit 2 of NEWB).

### Combat Rank

Based on TALLY (16-bit kill count):

| Rank | Kills |
|------|-------|
| Harmless | 0-7 |
| Mostly Harmless | 8-15 |
| Poor | 16-31 |
| Average | 32-63 |
| Above Average | 64-127 |
| Competent | 128-511 |
| Dangerous | 512-2,559 |
| Deadly | 2,560-6,399 |
| Elite | 6,400+ |

### Implementation Steps

1. Spawn system: danger level × altitude → ship type selection
2. NEWB flags: personality byte with 8 behavior bits
3. AI tactics: full TACTICS routine flow (energy, targeting, missile, laser, movement)
4. HITCH targeting: crosshair alignment check with targetable area
5. Combat: laser firing, missile launch, E.C.M.
6. Collision detection: entity vs entity
7. Bounty system: TALLY-based rank with 9 tiers
8. Cargo release: destroyed ships drop canisters

---

## Phase 7: Game Systems

**Goal:** Economy, trading, equipment, missions, docking.

### Docking System

**Five checks (all must pass):**
1. **Friendliness:** Station not hostile (bit 7 of status byte clear)
2. **Approach angle:** Ship within 26° of head-on; `nosev_z ≤ 214` (fixed-point cosine threshold)
3. **Heading:** Ship faces station; z-component of direction-to-station positive
4. **Safe cone:** Position within 22° cone from station center; `z ≥ 89` (fixed-point)
5. **Slot horizontal:** Slot within 33.6° of horizontal; `|roofv_x| ≥ 80` (fixed-point)

**Docking computer (press "C"):** Automates approach by injecting fake keypresses:
- **Stage 1 (far):** Head for planet/station zone
- **Stage 2 (approaching from wrong angle, >69° off):** Aim for "docking point" (8 units from station through slot)
- **Stage 3 (approaching from front):**
  - If pointing at station: refine approach (pitch/roll to center station, match roll for horizontal slot, accelerate)
  - If not pointing at station and too close: turn away
  - If not pointing and not too close: refine approach (player) or turn away (NPC)
- Intentionally imperfect — can crash into station or hit slot edges

### Implementation Steps

1. Market prices: `price = (base + (rand & mask) + economy × factor) × 4`
2. Commodity availability: `(base_qty + (rand & mask) - economy × factor) mod 64`
3. Player inventory: cargo hold, equipment slots
4. Fuel scooping: near sun (1.33 radii)
5. Docking checks: implement all 5 geometric tests with fixed-point thresholds
6. Docking computer: state machine with fake keypress injection
7. Mission system: delivery, assassination, mining

### Save/Load System

**Commander file format: 256 bytes total, 75 bytes used.**

| Offset | Size | Field | Description |
|--------|------|-------|-------------|
| &00-01 | 2 | QQ0/QQ1 | Current system galactic coords (X, Y) |
| &02-07 | 6 | QQ21 | Galaxy seed (s0, s1, s2) |
| &08-0B | 4 | CASH | Credit balance (×100 Cr) |
| &0C | 1 | QQ14 | Fuel level |
| &0D | 1 | COK | Competition flags |
| &0E | 1 | GCNT | Galaxy number (0-7) |
| &0F-12 | 4 | LASER | Laser types (front, rear, left, right) |
| &15 | 1 | CRGO | Cargo capacity |
| &16-26 | 17 | QQ20 | Cargo hold (17 commodities) |
| &27 | 1 | ECM | E.C.M. equipped |
| &28 | 1 | BST | Fuel scoops |
| &29 | 1 | BOMB | Energy bomb |
| &2A | 1 | ENGY | Energy/shield level |
| &2B | 1 | DKCMP | Docking computer |
| &2C | 1 | GHYP | Galactic hyperdrive |
| &2D | 1 | ESCP | Escape pod |
| &32 | 1 | NOMSL | Missiles |
| &33 | 1 | FIST | Legal status |
| &34-45 | 18 | AVL | Market availability |
| &46 | 1 | QQ26 | Market random seed |
| &47-48 | 2 | TALLY | Kill count |
| &49 | 1 | SVC | Save count |
| &4A | 1 | CHK2 | Secondary checksum |
| &4B | 1 | CHK | Primary checksum |

**Competition code:** 4 bytes encoding credit balance, combat rank, platform, and tamper detection:
- `K   = CHK OR %10000000`
- `K+2 = K EOR COK`
- `K+1 = K+2 EOR CASH+2`
- `K+3 = K+1 EOR &5A EOR TALLY+1`

**Checksum:** CHECK routine sums bytes from &00 to &49.

### Implementation Steps (cont.)

8. Create `SaveGameManager.cs` — serialize/deserialize 256-byte commander file
9. Implement checksum calculation (CHECK routine)
10. Implement competition code generation (SVE routine)
11. Save to persistent storage (JSON wrapper around binary blob, or pure binary)

### HUD & Dashboard

**Dashboard indicators (11 bars, 16 pixels each):**
- Forward/aft shields (0-255)
- Fuel (0-70 → 0-16 bar, each pixel = 16 units)
- Cabin temperature (0-255)
- Laser temperature (0-255)
- Altitude (0-255)
- Speed (0-40 → 0-16 bar)
- Energy banks (0-16)
- Missiles, pitch/roll indicator, compass, ECM bulbs (separate routines)

**3D Elliptical Scanner:**
- Ellipse: 138×36 screen coords, centered at (124, 220)
- Range: ships within ±63 on all axes (x_hi, y_hi, z_hi in [-63, 63])
- Projection:
  - Screen X = `123 + (x_sign * x_hi)` → range 60–186
  - Stick base Y = `220 - (z_sign * z_hi) / 4` → range 205–235
  - Stick height = `-(y_sign * y_hi) / 2` → ±31 pixels
  - Dot Y = stick base + stick height, clipped to 194–246
- Visual: 2-pixel-wide dot with 1-pixel stick
- IFF: friend/foe color coding (enhanced versions)

### Implementation Steps (Phase 7)

1. Market prices: `price = (base + (rand & mask) + economy × factor) × 4`
2. Commodity availability: `(base_qty + (rand & mask) - economy × factor) mod 64`
3. Player inventory: cargo hold, equipment slots
4. Fuel scooping: near sun (1.33 radii)
5. Docking checks: implement all 5 geometric tests with fixed-point thresholds
6. Docking computer: state machine with fake keypress injection
7. Mission system: delivery, assassination, mining
8. Create `HudRenderer.cs` — 11 dashboard bar indicators (DILX routine, 16px bars)
9. Create `ScannerRenderer.cs` — 3D elliptical scanner with dot+stick projection, IFF coloring

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
src/EliteRetro.Core/Systems/MainLoopCounter.cs
src/EliteRetro.Core/Systems/TaskScheduler.cs
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
src/EliteRetro.Core/Systems/SaveGameManager.cs
src/EliteRetro.Core/Utilities/ShipBlueprintLoader.cs
src/EliteRetro.Core/Utilities/NameGenerator.cs
src/EliteRetro.Core/Utilities/TribonacciTwist.cs
src/EliteRetro.Core/Entities/ShipType.cs
src/EliteRetro.Core/Entities/ShipSpecifications.cs
src/EliteRetro.Core/HUD/HudRenderer.cs
src/EliteRetro.Core/HUD/ScannerRenderer.cs
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
3. **MainLoopCounter + TaskScheduler** — frame-spread task scheduling
4. **OrientationMatrix + FlightController** — flight physics
5. **FlightScene** — new game experience
6. **Tribonacci GalaxyGenerator** — authentic procedural generation
7. **CircleRenderer + PlanetRenderer** — visual polish
8. **ShipAI + SpawnSystem** — living galaxy
9. **MarketSystem + DockingSystem** — game loop

---

## Verification Checklist

- [x] Tibedied (Galaxy 0, System 0) generates with correct stats (TIBIDIED, Feudal, Poor Ind, Tech 8, Pop 36)
- [x] System 1 (Galaxy 0, System 1) generates with correct stats (USBI, CorpState, Rich Ag, Tech 6, Pop 37)
- [ ] Planet radius = 24,576, station distance = 65,536
- [ ] Safe zone triggers at 192 local coords
- [ ] Minsky rotation maintains orthonormality over 1000+ iterations
- [ ] 12-slot bubble enforces capacity
- [ ] Back-face culling produces solid-looking wireframes
- [ ] Station spawns with nosev facing planet
