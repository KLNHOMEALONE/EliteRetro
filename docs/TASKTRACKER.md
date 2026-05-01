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
- [x] **1.8** Implement sun distance effects — heat (>2.67r), fuel scoop (>1.33r), fatal (<0.90r)
- [x] **1.9** Implement energy bomb — 1.17 × planet diameter blast radius, clear all non-reserved slots

## Phase 1.5: Main Loop Counter (Task Scheduling)

- [x] **1.5.1** Create `MainLoopCounter.cs` — MCNT byte field, Decrement() wrapping 255→0, Reset(byte)
- [x] **1.5.2** Create `TaskScheduler.cs` — RegisterTask(mask, offset, action), Evaluate(mcnt) method
- [ ] **1.5.3** Register energy/shield regen (every 8, offset 0)
- [ ] **1.5.4** Register tactics processing (every 8, offsets 0-3 for 1-2 ships)
- [ ] **1.5.5** Register TIDY scheduling (every 16, offsets 0-11) — replaces round-robin from Phase 2.7
- [ ] **1.5.6** Register station proximity check (every 32, offset 0)
- [ ] **1.5.7** Register altitude/crash/low-energy checks (every 32, offset 10)
- [ ] **1.5.8** Register sun effects (every 32, offset 20)
- [ ] **1.5.9** Register ship spawn consideration (every 256, offset 0)
- [ ] **1.5.10** Integrate into `FlightScene.Update()` — decrement MCNT, evaluate tasks
- [ ] **1.5.11** Wire counter resets — set to 0 on fuel/dock/launch/arrive, set to 1 on in-system jump

## Phase 2: Minsky Flight System

- [x] **2.1** Implement `OrientationMatrix.RotatePosition()` — Minsky circle algorithm for entity positions
- [x] **2.2** Implement `OrientationMatrix.ApplyUniverseRotation()` — Minsky rotation for orientation vectors
- [x] **2.3** Implement `OrientationMatrix.Tidy()` — normalize nosev, orthogonalize roofv, cross-product sidev
- [x] **2.4** Implement `OrientationMatrix.ApplyOwnRotation()` — fixed 1/16 rad rotation for AI ships
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

- [x] **4.1** Create `SineTable.cs` — 64-entry sine lookup, Sin(step), Cos(step)
- [x] **4.2** Create `CircleRenderer.cs` — parametric circle drawing, DrawCircle(center, radius, color, stepSize), segment clipping
- [x] **4.3** Create `EllipseRenderer.cs` — conjugate-diameter ellipse, DrawEllipse(center, u, v, color), convenience method for axis-aligned
- [x] **4.4** Create `PlanetRenderer.cs` — DrawCrater(), DrawMeridiansAndEquator(), feature visibility based on tech level
- [x] **4.5** Create `SunRenderer.cs` — horizontal scan lines, random fringe, color schemes
- [x] **4.6** Create `RingRenderer.cs` — random points in elliptical band, planet occlusion check
- [x] **4.7** Create `ExplosionRenderer.cs` — vertex-based explosion clouds: cloud data (size, counter starting at 18 incrementing by 4 until 128 then shrinking, explosion count from blueprint vertex count, 4 stored random seeds). Render: erase old, increment counter, size=counter/distance, per-origin-vertex scatter random particles (count peaks at counter=128)
- [x] **4.8** Create `StardustRenderer.cs` — 16-bit sign-magnitude star coords (SX,SY,SZ). Per-frame motion: `q=64*speed/z_hi; z-=speed*64; y+=|y_hi|*q; x+=|x_hi|*q`. Roll: `y+=alpha*x/256; x-=alpha*y/256`. Pitch: `y-=beta*256; x+=2*(beta*y/256)^2`. Side/rear views use different transforms. Stars wrap on overflow.
- [x] **4.9** Modify `WireframeRenderer` — add DrawCircle/DrawEllipse convenience methods

## Phase 5: Flight Scene (New Game Flow)

- [x] **5.1** Create `FlightScene.cs : GameScene`
- [x] **5.2** Initialize local bubble on enter — planet (slot 0), sun (slot 1), player at origin
- [x] **5.3** Place sun at 2.67–18.67 planet radii, behind player
- [x] **5.4** Integrate FlightController + Minsky rotation for all entities
- [x] **5.5** Render planet via PlanetRenderer (large circle with surface features)
- [x] **5.6** Render sun via SunRenderer (scan lines with fringe)
- [x] **5.7** Render other entities via WireframeRenderer
- [x] **5.8** Implement HUD overlay — speed, energy, compass, scanner, hidden edges indicator
- [x] **5.9** Safe zone check each frame → spawn station, remove sun
- [x] **5.10** Modify `MainMenuScene` — "Start New Game" navigates to FlightScene, add "Space View" menu item
- [x] **5.11** Wireframe rendering improvements — back-face culling with pre-computed face normals, screen-space outline detection for open-shell models
- [x] **5.12** Ship scale in FlightScene — 4x larger (0.0004 instead of 0.0001) for better visibility
- [x] **5.13** Boulder model rewrite — proper convex hull geometry with verified outward normals
- [x] **5.14** Anaconda model fix — corrected 6 inverted face normals and 4 wrong face windings

## Phase 6: Ship AI & Combat

- [ ] **6.1** Create `ShipAISystem.cs` — full TACTICS routine: energy recharge (+1/iter), Part 3 targeting (nosev·toPlayer dot product), Part 4 energy check (2.5% random roll, bail at low energy), Part 5 missile decision, Part 6 laser firing (crosshair check), Part 7 movement (XX15 vector-based: traders→planet, aggressive→player, missiles→home)
- [ ] **6.2** Implement NEWB flags (byte #37) — 8 personality bits (trader, bounty hunter, hostile, pirate, docking, innocent, cop, scooped), default table E% per ship type
- [ ] **6.3** Implement HITCH targeting — z_sign positive, x_hi=y_hi=0, distance² = x_lo²+y_lo² vs blueprint targetable area
- [ ] **6.4** Implement aggression (0-63 in byte #32 bits 1-6) — probability of turning toward target, separate from hostility flag
- [ ] **6.5** Create `SpawnSystem.cs` — danger level × altitude → ship type selection, pack spawning
- [ ] **6.6** Implement combat — laser firing (4 mounts, power from blueprint), missile launch (homing), E.C.M. (countermeasure, mutual cancellation), energy depletion on hit
- [ ] **6.7** Create `CollisionSystem.cs` — entity vs entity collision detection
- [ ] **6.8** Implement bounty system — TALLY (16-bit) → 9 ranks (Harmless 0-7, Mostly Harmless 8-15, Poor 16-31, Average 32-63, Above Average 64-127, Competent 128-511, Dangerous 512-2559, Deadly 2560-6399, Elite 6400+)
- [ ] **6.9** Implement cargo release — destroyed ships drop canisters (max_cargo from blueprint)

## Phase 7: Game Systems

- [ ] **7.1** Create `MarketSystem.cs` — QQ23 commodity table, price formula, availability formula
- [ ] **7.2** Implement commodity data — food, textiles, narcotics, luxuries, etc. (16 items)
- [ ] **7.3** Create player inventory — cargo hold capacity, equipment slots
- [ ] **7.4** Implement fuel scooping — near sun (1.33 radii), fuel increases over time
- [ ] **7.5** Create `DockingSystem.cs` — 5 geometric checks (friendliness, approach angle nosev_z<=214, heading z>0, safe cone z>=89, slot horizontal |roofv_x|>=80)
- [ ] **7.6** Implement docking computer — state machine with fake keypress injection (approach -> align -> accelerate), intentionally imperfect
- [ ] **7.7** Implement mission system — delivery, assassination, mining contracts
- [ ] **7.8** Create `HudRenderer.cs` — 11 dashboard bar indicators (DILX routine, 16px bars): shields (0-255), fuel (0-70→0-16), cabin temp, laser temp, altitude, speed (0-40→0-16), energy banks (0-16), missiles, pitch/roll, compass, ECM bulbs
- [ ] **7.9** Create `ScannerRenderer.cs` — 3D elliptical scanner (138×36 at screen (124,220)), range ±63 on all axes, dot+stick projection (X=123+x_sign*x_hi, stick_base_Y=220-z_sign*z_hi/4, stick_height=-y_sign*y_hi/2), 2px dot with 1px stick, IFF coloring

## Phase 8: Polish & Integration

- [ ] **8.1** Add audio — engine hum, laser shots, explosions
- [ ] **8.2** Add save/load — 256-byte commander file (75 bytes used), CHECK checksum, competition code (4-byte encoded credit+rank+platform+tamper)
- [ ] **8.3** Create `SaveGameManager.cs` — serialize/deserialize commander binary format, checksum validation
- [ ] **8.4** Add top pilots leaderboard — generated from galaxy data
- [ ] **8.5** Add options menu — key bindings, difficulty settings
- [ ] **8.6** Performance optimization — object pooling, batched rendering
- [ ] **8.7** Cougar easter egg — 1 in 9,000 spawn chance

---

## Current Focus

**Phase 5: Flight Scene** — COMPLETE. FlightScene fully implemented with:
- Minsky flight system with frame-rate independent controls
- Unified `FlightControlService` for consistent input across all scenes
- Stardust renderer with perspective motion, roll/pitch, and speed-based dash effects
- Planet and sun rendering with counter-rotating surface features during roll
- Random ship/asteroid spawning with lifetime/boundary cleanup
- HUD showing speed, planet/sun distances, sun proximity warnings, and entity lifecycle events
- View switching (Front/Rear/Left/Right)

**Next: Phase 6** — Ship AI & Combat (TACTICS, NEWB flags, aggression, spawning, combat)

---

## Completed

- **Phase 0** — All 9 foundation tasks
- **Phase 1** — All 9 tasks (sun effects, energy bomb, slot system)
- **Phase 1.5** — 2 of 11 tasks (MainLoopCounter, TaskScheduler infrastructure)
- **Phase 2** — All 8 tasks (Minsky rotation, Tidy, forward movement, view switching)
- **Phase 3** — All 9 tasks (Tribonacci galaxy generation)
- **Phase 4** — All 9 tasks (Circle & Planet Rendering: SineTable, CircleRenderer, EllipseRenderer, PlanetRenderer, SunRenderer, RingRenderer, ExplosionRenderer, StardustRenderer)
- **Phase 5** — All 10 tasks (FlightScene complete)
