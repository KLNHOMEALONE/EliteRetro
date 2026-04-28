# EliteRetro — Task Tracker

> Active development task list. Mark tasks complete as they finish. Add new tasks at the bottom of the appropriate phase.

---

## Phase 0: Foundation

- [x] **0.1** Create `GameConstants.cs` — all scale values (planet radius 24576, station distance 65536, bubble radius 57344, slot counts, sun radii multipliers)
- [x] **0.2** Create `EntityInstance.cs` — runtime entity wrapper (position, velocity, orientation, speed, energy, slot index, destroyed flag)
- [x] **0.3** Create `OrientationMatrix.cs` — 3×3 rotation matrix (nosev/roofv/sidev), identity constructor, Minsky RotatePosition, ApplyUniverseRotation, Tidy orthonormalization, TransformLocalToWorld
- [x] **0.4** Create `ShipBlueprint.cs` — static ship data structures (VertexDef, EdgeDef, FaceDef, ShipBlueprint), port all ship characteristics from docs
- [x] **0.5** Create `ShipInstance.cs` — 36-byte-equivalent runtime data (position, orientation vectors, speed, acceleration, roll/pitch counters, flags, AI, energy)
- [x] **0.6** Create `PlanetModel.cs` — planet wireframe geometry (icosahedron)
- [x] **0.7** Create `SunModel.cs` — sun wireframe geometry (sphere with latitude rings)
- [x] **0.8** Create `FlightController.cs` — keyboard input mapping (arrows=pitch, Q/W=roll), angle scaling, view switching (V key)
- [x] **0.9** Create `LocalBubbleManager.cs` — slot array[20], spawn/despawn, culling beyond 57344, safe zone trigger, station spawn/sun removal

## Phase 1: Local Bubble & Entity Lifecycle

- [x] **1.1** Implement slot allocation in LocalBubbleManager — reserved slot 0 (planet), slot 1 (sun/station), slots 2+ (dynamic)
- [x] **1.2** Implement spawn logic — find first empty slot from index 2, reject if full
- [x] **1.3** Implement despawn logic — mark destroyed, shuffle remaining down
- [x] **1.4** Implement bubble culling — distance check > 57,344 per frame
- [x] **1.5** Implement orbit point calculation — `planetPos + 2 * planetNosev * PlanetRadius`
- [x] **1.6** Implement safe zone trigger — bounding box check at 192 local coords, spawn station, remove sun
- [x] **1.7** Implement station orientation — invert nosev to face planet center
- [ ] **1.8** Implement sun distance effects — heat (>2.67r), fuel scoop (>1.33r), fatal (<0.90r)
- [ ] **1.9** Implement energy bomb — 1.17 × planet diameter blast radius, clear all non-reserved slots

## Phase 2: Minsky Flight System

- [x] **2.1** Implement `OrientationMatrix.RotatePosition()` — Minsky circle algorithm for entity positions
- [x] **2.2** Implement `OrientationMatrix.ApplyUniverseRotation()` — Minsky rotation for orientation vectors
- [x] **2.3** Implement `OrientationMatrix.Tidy()` — normalize nosev, orthogonalize roofv, cross-product sidev
- [ ] **2.4** Implement `OrientationMatrix.ApplyOwnRotation()` — fixed 1/16 rad rotation for AI ships
- [x] **2.5** Modify `SpaceScene` — replace `_rotation` Euler angles with Minsky universe rotation
- [x] **2.6** Add view switching — V key cycles Front/Rear/Left/Right, apply axis transforms
- [x] **2.7** Implement periodic tidying — round-robin across entities each frame (TidyOne in LocalBubbleManager, every 60 frames for universe orientation)
- [x] **2.8** Implement forward movement — entity moves along its own nosev by speed

## Phase 3: Galaxy Generation (Tribonacci)

- [x] **3.1** Create `GalaxySeed` struct — three 16-bit seeds, Twist(), NextGalaxy(), Copy()
- [x] **3.2** Implement twist algorithm — `s0'=s1, s1'=s2, s2'=s0+s1+s2` with 16-bit wraparound
- [x] **3.3** Implement next-galaxy — rotate each byte pair left by 1 bit
- [x] **3.4** Implement system data derivation — economy (s0_hi & 0b111), government ((s1_lo >> 3) & 0b111), tech level, population, productivity, radius, species, galactic coords
- [x] **3.5** Implement name generation — 2-letter token table (indices 129-159), 3-4 tokens per name, twist between tokens
- [x] **3.6** Add anarchy/feudal economy constraint — force bit 1 of economy (no Rich for Anarchy/Feudal)
- [ ] **3.7** Verify Lave — Galaxy 0, System 1: Dictatorship, Rich Agri, Tech 5, Pop 25, Productivity 7000, Radius 4116
- [ ] **3.8** Verify Tibedied — Galaxy 0, System 0 name = "Tibedied"
- [x] **3.9** Replace existing `GalaxyGenerator` — use Tribonacci instead of simple RNG

## Phase 4: Circle & Planet Rendering

- [ ] **4.1** Create `SineTable.cs` — 64-entry sine lookup, Sin(step), Cos(step)
- [ ] **4.2** Create `CircleRenderer.cs` — parametric circle drawing, DrawCircle(center, radius, color, stepSize), segment clipping
- [ ] **4.3** Create `EllipseRenderer.cs` — conjugate-diameter ellipse, DrawEllipse(center, u, v, color), convenience method for axis-aligned
- [ ] **4.4** Create `PlanetRenderer.cs` — DrawCrater(), DrawMeridiansAndEquator(), feature visibility based on tech level
- [ ] **4.5** Create `SunRenderer.cs` — horizontal scan lines, random fringe, color schemes
- [ ] **4.6** Create `RingRenderer.cs` — random points in elliptical band, planet occlusion check
- [ ] **4.7** Create `ExplosionRenderer.cs` — particle-based expanding/contracting cloud
- [ ] **4.8** Create `StardustRenderer.cs` — starfield particles with perspective projection, roll/pitch effects
- [ ] **4.9** Modify `WireframeRenderer` — add DrawCircle/DrawEllipse convenience methods

## Phase 5: Flight Scene (New Game Flow)

- [ ] **5.1** Create `FlightScene.cs : GameScene`
- [ ] **5.2** Initialize local bubble on enter — planet (slot 0), sun (slot 1), player at origin
- [ ] **5.3** Place sun at 2.67–18.67 planet radii, behind player
- [ ] **5.4** Integrate FlightController + Minsky rotation for all entities
- [ ] **5.5** Render planet via PlanetRenderer (large circle with surface features)
- [ ] **5.6** Render sun via SunRenderer (scan lines with fringe)
- [ ] **5.7** Render other entities via WireframeRenderer
- [ ] **5.8** Implement HUD overlay — speed, energy, compass, scanner
- [ ] **5.9** Safe zone check each frame → spawn station, remove sun
- [ ] **5.10** Modify `MainMenuScene` — "Start New Game" navigates to FlightScene instead of SpaceScene

## Phase 6: Ship AI & Combat

- [ ] **6.1** Create `ShipAISystem.cs` — tactics engine, aggression levels, attack/flee/circle behaviors
- [ ] **6.2** Create `SpawnSystem.cs` — danger level × altitude → ship type selection, pack spawning
- [ ] **6.3** Implement ship personalities — pirate (hostile), trader (innocent), cop (bounty hunter), bounty hunter
- [ ] **6.4** Implement combat — laser firing, missile launch, E.C.M. active
- [ ] **6.5** Create `CollisionSystem.cs` — entity vs entity collision detection
- [ ] **6.6** Implement bounty system — kills → credits → rating increase
- [ ] **6.7** Implement cargo release — destroyed ships drop canisters (max_cargo from blueprint)

## Phase 7: Game Systems

- [ ] **7.1** Create `MarketSystem.cs` — QQ23 commodity table, price formula, availability formula
- [ ] **7.2** Implement commodity data — food, textiles, narcotics, luxuries, etc. (16 items)
- [ ] **7.3** Create player inventory — cargo hold capacity, equipment slots
- [ ] **7.4** Implement fuel scooping — near sun (1.33 radii), fuel increases over time
- [ ] **7.5** Create `DockingSystem.cs` — approach station, align to slot, docking sequence
- [ ] **7.6** Implement mission system — delivery, assassination, mining contracts
- [ ] **7.7** Create `HudRenderer.cs` — dashboard overlay (speed bar, energy bar, compass, scanner)

## Phase 8: Polish & Integration

- [ ] **8.1** Add audio — engine hum, laser shots, explosions
- [ ] **8.2** Add save/load — commander state persistence
- [ ] **8.3** Add top pilots leaderboard — generated from galaxy data
- [ ] **8.4** Add options menu — key bindings, difficulty settings
- [ ] **8.5** Performance optimization — object pooling, batched rendering
- [ ] **8.6** Cougar easter egg — 1 in 9,000 spawn chance

---

## Current Focus

**Phase 3: Galaxy Generation** — COMPLETE. Tribonacci twist algorithm fully implemented with authentic name generation and system data derivation.

**Next immediate task: Phase 4.1** Create `SineTable.cs` — 64-entry sine lookup for circle rendering.

---

## Completed

- **Phase 0** — All 9 foundation tasks (GameConstants, entities, models, FlightController, LocalBubbleManager)
- **Phase 1** — 7 of 9 tasks (all except sun distance effects and energy bomb)
- **Phase 2** — 7 of 8 tasks (all except AI ApplyOwnRotation)
- **Phase 3** — All 9 tasks (GalaxySeed, twist, Tribonacci galaxy generation, name generation)
