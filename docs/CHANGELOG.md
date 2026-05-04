# EliteRetro — Changelog

All notable changes to this project.

---

## [Unreleased]

### Added
- **Smooth speed acceleration** — replaced discrete ±1 speed steps with smooth interpolation
  - `SpeedAccel` (30 u/s²) and `SpeedDecel` (45 u/s²) constants for inertia feel
  - Matches original Elite timing: 0→40 in ~1.3 seconds
  - Uses `MoveTowards` pattern consistent with roll/pitch handling

### Fixed
- **Target practice sun loss** — target practice mode now preserves sun/station when clearing the bubble
- **Near-zero projection W** — guards against near-singular perspective divide (`|W| < 0.001f`)
- **Ram mode orientation** — fixed object initializer referencing its own not-yet-set `Roofv` field
- **Screen magic numbers** — extracted 1024, 768, 480, 512, 240 into named constants for future resolution changes

### Added
- **Laser targeting system** — cone-based hit detection (~32° cone, 500 unit range)
  - Shields absorb damage first, then hull
  - 90 damage per shot (3 shots strip shields, 3 more destroy hull)
  - Visual crosshair at screen center (BBC Elite diamond reticle)
  - Laser fire on Space key, pause on P key only
- **Target practice mode** — L key spawns stationary Viper for testing
  - Clears other ships for clean test range
  - Fixed world position — unaffected by pitch/roll or collision
  - "TARGET PRACTICE" indicator on screen
- **Cargo canister drops** — destroyed ships release cargo as shootable canisters
  - Canisters have hull=1, destructible by single laser hit
  - No longer auto-destroyed by collision system
  - Collision with canisters still damages player ship (authentic to original)
- **SaveGameManager** — 256-byte binary commander file format
  - Layout matches BBC Elite: galactic coords (0x00-0x01), galaxy seed (0x02-0x07), credits (0x08-0x0B), fuel (0x0C), cargo (0x16-0x26), TALLY (0x47-0x48)
  - CHECK checksum: sum of bytes 0x00-0x4A, stored at 0x4B
  - CHK2 secondary checksum: XOR of bytes 0x00-0x49, stored at 0x4A
  - Save path: `%LOCALAPPDATA%/EliteRetro/commander.bin`
  - Load validates checksum, restores commander data and bubble state
- **AudioManager** — procedural sound generation (no external files)
  - Menu select: 880Hz A5 beep with cosine attack/decay envelope
  - Laser shot: noise burst with frequency sweep (2000→200Hz)
  - Explosion: low-frequency noise with 60Hz rumble and slow decay
  - All sounds generated as WAV in-memory via `DynamicSoundEffectInstance`
- **MainMenuScene redesign** — Elite-style left sidebar layout
  - 300px dark blue sidebar panel with separator lines
  - Combat rating displayed prominently (large yellow text)
  - 8 menu items with ">" selector prefix
  - "LOAD GAME" item shows "(no save)" when no saved game exists
  - Controls listed at bottom of sidebar
- **FlightScene enhancements** — F5 to save, ESC to return to menu
  - Save confirmation message on HUD
  - Explosion sound on ship destruction
  - Menu select sound on UP/DOWN navigation

- **Phase 7: Game Systems** — market, docking, scanner, fuel scooping (8 of 9 tasks complete)
- **MarketSystem** — QQ23 commodity table with 16 items (food, textiles, narcotics, luxuries, furs, liquor/wines, metals, gold, platinum, gem-stones, aliens, firearms, medical, machines, alcohols, computers)
  - Price formula: `(base + (rand & mask) + economy × factor) × 4`
  - Availability formula: `(base_qty + (rand & mask) - economy × factor) mod 64`
  - Tech level adjustments: scarce/expensive outside production range
  - `Buy()` and `Sell()` operations with credit/cargo validation
- **DockingSystem** — 5 geometric clearance checks
  - Friendliness: station not hostile
  - Approach angle: nosev_z ≤ 214 (within 26° of head-on)
  - Heading: ship faces station (z-component positive)
  - Safe cone: position within 22° cone (z ≥ 89)
  - Slot horizontal: |roofv_x| ≥ 80 (within 33.6°)
  - Docking computer state machine: approaching → aligning → accelerating → docked
  - Fake keypress injection for automated approach (intentionally imperfect)
- **ScannerRenderer** — 3D elliptical scanner display (space compass)
  - Ellipse: 480×240, centered in dashboard center panel
  - 3 dashed grey horizontal grid lines (top, center, bottom)
  - Vertical dashed line from bottom to center
  - W-shape pattern in upper half (dashed grey)
  - Sun indicator: orange circle with yellow center (upper left)
  - Station indicator: cyan circle with filled/outline square (upper right)
  - Ship contacts: colored dots with sticks (green=friendly, orange=hostile)
  - Station appears as large white dot on scanner for navigation
  - Contacts transform with universe orientation (move on pitch/roll)
- **Fuel scooping** — +1 fuel per 32 frames when within 1.33× planet diameter of sun
  - Wired via MCNT scheduler (every 32 frames, offset 20)
  - Fuel level tracked in CommanderData (0-70 range)
- **CommanderData enhancements**
  - `Fuel` property (0-70, default 35)
  - `CargoCapacity` property (default 10 tons)
  - `AddCargo()` now respects capacity limit
- **HUD integration** — fuel display reads from CommanderData.Fuel
- **Scanner integration** — wired into FlightScene.Draw() with universe orientation transform
  - Explosion visual effects wired into FlightScene (particle expansion/contraction cycle)
  - Destroyed ships remain in bubble during explosion animation, cleaned up after 10-frame delay
  - Proper screen projection using view/projection matrices
  - HUD messages: ">> {ship} destroyed!" on death, ">> COLLISION with {ship}!" on player hit
  - Debug logging for collision and explosion events
- **Small ship instant destruction** — ships with < 15 vertices (Sidewinder, Viper, Mamba) destroyed on any collision
  - Large ships (≥ 15 vertices) take proportional shield + hull damage
  - Player ship unaffected — uses gradual shield/hull damage
- **Player energy/shield regeneration** — fixed regen task to include player (was incorrectly excluded)
- **Rock entity system** — `IsRock` property on ShipModel and ShipBlueprint
  - Asteroid, Boulder, Rock Hermit marked as rocks — zero energy/shields/speed, hull=1
  - No longer uses fragile string-based name checks
- **Phase 6: Ship AI & Combat complete** — full TACTICS routine, bounty system, cargo release
- **MCNT-driven task scheduler** — authentic Elite-style frame-spread task scheduling (Phase 1.5)
  - `MainLoopCounter` — 8-bit counter cycling 0-255, decrements each Update()
  - `TaskScheduler` — registers actions with (mask, offset) pairs, fires when (mcnt & mask) == offset
  - Energy/shield regen every 8 frames
  - Tactics placeholders every 8 frames (offsets 0-3) for Phase 6 AI
  - TIDY orthonormalization every 16 frames (offsets 0-11, replaces round-robin)
  - Station proximity check every 32 frames
  - Altitude/crash/low-energy warnings every 32 frames (placeholders)
  - Sun effects every 32 frames (placeholders)
  - Ship spawn consideration every 256 frames
  - `GetSlot()` added to LocalBubbleManager for slot-based access
- **HUD Dashboard** — authentic BBC Elite-style dashboard overlay
  - `HudRenderer.cs` with DILX-style vertical bar indicators (8 indicators)
  - Speed, energy, fuel, cabin temp, laser temp, altitude, energy banks, missiles
  - Compass strip with cardinal direction indicator
  - ECM bulb indicators (3 bulbs)
  - Temperature bars use cyan→yellow→red gradient
  - `HUDState` struct for passing dashboard data
  - `BitmapFont.MeasureString()` added for text centering
- **Screen-space outline detection** for wireframe rendering
  - Projects edges to 2D, tests if edge lies on the screen-space contour
  - Edges on the outline are always drawn solid, even if their faces are culled
  - Fixes wireframe silhouettes for open-shell models (ships without bottom caps)
- **Boulder model rewrite** — proper convex hull with 7 vertices, 15 edges, 10 faces
  - Pre-computed outward-pointing face normals (no Newell fallback needed)
  - Correct back-face culling from all viewing angles
- **Anaconda model fix** — corrected 6 face normals and 4 face windings
  - F0, F5, F7, F8, F9, F10 normals were inward-pointing → flipped
  - F5 winding was self-intersecting `{0,4,9,5}` → fixed to `{0,5,9,4}`
  - F7-F10 vertex orders corrected for proper CCW winding
- **Mass model fix** — verified and corrected all 27 ship models for correct back-face culling
  - Python: reversed 7 faces (F1,F3,F4,F6,F9,F11,F12), added missing edge (6,7)
  - Fer-de-Lance: reversed 7 faces (F1-F4,F6-F8), added missing edge (1,6)
  - Mamba: reversed 3 faces (F1,F2,F3)
  - Adder: reversed 7 faces (F0,F2-F4,F6-F8), added missing edges (1,4),(4,5)
  - Boa: reversed 9 faces, added 7 missing edges (cockpit/rear connectivity)
  - Constrictor: reversed 8 faces, added 8 missing edges (rear structure)
  - Cougar: reversed 5 faces, added 4 missing edges
  - Gecko: reversed 6 faces, added 9 missing edges
  - Krait: reversed 1 face (F5), added 5 missing edges
  - Worm: reversed 6 faces, added 7 missing edges
  - Missile: reversed 4 faces (F0-F3)
  - Thargoid: reversed 7 faces, added 8 missing edges
  - Thargon: reversed 1 face (F0 back pentagon)
  - Shuttle: corrected 1 face winding (F1)
  - Moray: reversed 6 faces, added 9 missing edges
  - Models already correct: Viper, Sidewinder, Cobra Mk1, Cobra Mk3, Asp Mk2, Rock Hermit, Escape Pod, Transporter, Coriolis Station, Dodo Station, Asteroid, Canister
- **Sun distance effects** — proximity-based interactions with the sun
  - Heat warning at 2.67× planet diameter
  - Fuel scooping at 1.33× planet diameter
  - Fatal damage at 0.90× planet diameter
- **Energy bomb** — clears all non-reserved bubble slots within 1.17× planet diameter blast radius
- **OrientationMatrix.ApplyOwnRotation** — AI ship turning via fixed 1/16 rad increments
- **Tribonacci galaxy generation** — authentic BBC Elite algorithm replacing simple RNG
  - `GalaxySeed` struct with Twist() and NextGalaxy() methods
  - 8 galaxies × 256 systems derived from three 16-bit seeds (0x5A4A, 0x0248, 0xB753)
  - System data: economy, government, tech level, population, productivity, radius, species
  - Name generation with 31-entry two-letter token table (cpl routine)
- **Minsky flight system** — replaced Euler angles with Minsky circle algorithm
  - `OrientationMatrix` with ApplyUniverseRotation() and Tidy() orthonormalization
  - FlightController for keyboard input (arrows=pitch, Q/W=roll, V=view switch)
  - Periodic TIDY to prevent floating-point drift
- **Local Bubble Manager** — slot-based entity lifecycle (20 slots)
  - Reserved slots: planet (0), sun/station (1), dynamic (2+)
  - Spawn/despawn, bubble culling, safe zone trigger
  - Station orientation (faces planet), universe rotation broadcast
- **Phase 4: Circle & Planet Rendering**
  - SineTable: 64-entry sine/cosine lookup
  - CircleRenderer: parametric circle drawing via SineTable
  - EllipseRenderer: conjugate-diameter ellipses with arc support
  - PlanetRenderer: craters, meridians, equator with front/back visibility
  - SunRenderer: scan-line sun with corona fringe
  - RingRenderer: Saturn-style rings with concentric elliptical bands + particle texture, proper planet occlusion (front/back layer separation)
  - ExplosionRenderer: vertex-based particle clouds with counter-driven lifecycle (expand→contract)
  - StardustRenderer: 400-star particle system with 16-bit sign-magnitude coords, perspective expansion, roll/pitch transforms, speed-based dash effects
  - WireframeRenderer: added DrawCircle/DrawEllipse convenience methods
- **Galaxy map improvements** — crosshair cursor, names on hover only, auto-centered view
- SpaceScene now uses CircleRenderer for planet and sun rendering (circle outlines instead of squares)
- Corrected celestial body visual hierarchy — sun larger than planet, planet larger than wireframe cube

### Changed
- Main menu: "LOAD COMMANDER" renamed to "GALAXY MAP"
- EconomyType enum expanded to 8 values (RichIndustrial through PoorAgricultural)
- TechLevel changed from enum to int (0-14 range)
- Galaxy map zoom defaults to 3.0, centered on galaxy
- Scene switching: `GameInstance.ChangeScene()` now truly replaces the active scene stack (previously pushed a new scene)
- Collision detection: collision radii now derive from model bounding radius (instead of vertex-count scaling)
- Player ship state: Player energy/hull now uses `PlayerShip` as the single source of truth (manager fields forward to the ship instance)

### Fixed
- Planet wireframe vertex overflow (removed erroneous ×1000 scaling)
- Escape key double-handler causing immediate exit
- Texture2D creation per frame in DrawFilledCircle (now cached)
- Integer overflow in LocalBubbleManager (BubbleRadius² exceeded int range)
- Back-face culling freeze on large models
- Laser hit detection: corrected front-view forward vector (Elite→MonoGame Z flip)
- Stardust: corrected forward-motion sign so stars move toward viewer
- Scanner: fixed front/rear swap by flipping Z to match scanner convention
- FlightScene: unsubscribed LocalBubbleManager event handlers on scene unload (prevents handler accumulation on re-entry)
- FlightScene: removed unconditional debug station spawn so sun can be observed initially (station still replaces sun via the proper spawn path)
- ShipInstance: hardened `FaceTarget()` to avoid NaNs when target direction is near-parallel to `UnitY`
- MCNT: fixed `MainLoopCounter.DecrementTimeBased()` to persist its accumulator across frames (variable timestep ready)

---

## [0.3.0] — Unreleased

### Added
- **FlightScene** — main gameplay scene with first-person cockpit view
  - Player at origin (0,0,0), universe moves around them
  - Planet in slot 0, sun in slot 1, player flies toward planet
  - Safe zone trigger spawns Coriolis station, removes sun
  - Sun proximity effects (heat warning, fuel scoop, fatal)
  - View switching (V key): Front, Rear, Left, Right
  - Speed control (W/S keys), zoom (+/-)
  - HUD overlay: view mode, speed, planet/sun distances, sun status, station status, controls
  - Entity lifecycle events displayed on HUD (spawn/despawn notifications)

- **FlightControlService** — unified input processing for consistent controls across all scenes
  - Replaced FlightController with frame-rate independent control state
  - Arrow keys: Left/Right = roll, Up/Down = pitch (inverted: Down = pitch up)
  - W/S = speed increase/decrease
  - V = view switch, P/Space = pause
  - Same control behavior in FlightScene and SpaceScene

- **Stardust improvements**
  - Speed-based dash effect: dashes start at speed 7, scale to max at speed 40
  - Brighter stars with distance-based brightness
  - Perspective expansion as stars approach
  - Roll/pitch handled via universe orientation (no double-rotation)

- **Random ship/asteroid spawning** in FlightScene
  - Ships: Sidewinder, Viper, Cobra Mk3, Python
  - Asteroids: Asteroid, Boulder
  - 50% fly toward player (nose-first), 50% fly toward planet (nose-first)
  - Size: 16-32 units (large, easily visible)
  - Lifetime: 60 seconds max, or despawn when leaving bubble boundary
  - Spawn rate: ~1 per 3 seconds (30% chance every 180 frames)

- **Planet counter-rotation** — planet surface features and rings rotate opposite to ship roll
  - Cumulative roll angle tracked and applied to planet/ring rendering
  - Consistent behavior in both FlightScene and SpaceScene

- **Entity orientation in world matrix** — ships rendered with proper 3D orientation
  - World matrix includes entity's orientation (sidev/roofv/nosev as columns)
  - Ships fly nose-first in their direction of travel

### Changed
- PitchMax increased to match RollMax (0.125 rad/frame at 60fps) for unified control feel
- Flight controls are frame-rate independent (dt-scaled)
- SpaceScene uses same view matrix construction as FlightScene (pure rotation, no orbital camera)
- Stardust no longer applies its own roll/pitch (universe orientation handles it)

### Fixed
- View switching (V key) now persists across frames
- Ship orientation now correctly included in world matrix for rendering
- Entity positions no longer rotated by ApplyUniverseRotation (only view rotates)
- Ships flying toward player now properly face nose-first
- **Wireframe rendering — back-face culling with proper face normals**
  - Anaconda: manually verified normals for all 12 faces (non-planar faces had degenerate Newell-computed normals)
  - Viper: added 2 wing-surface faces so edges 10 and 12 render solid from above (silhouette detection)
  - Asp Mk II: added pre-computed normals for all 10 faces, corrected top-rear face normal
  - All models: face normals point outward from ship center for correct visibility testing
- Hidden edges toggle (I key) — press to show/hide dashed lines in both MainMenuScene and FlightScene
- Edge highlighting for debugging ([] keys cycle highlighted edge in red)
- Ship scale in FlightScene increased 4x for better visibility

---

## [0.2.0] — 2024-XX-XX

### Added
- Galaxy map scene with 8 galaxies × 256 systems each
- Procedural star system generation (name, government, economy, tech level, population)
- System color-coding by economy type on galaxy map
- Zoom and pan controls for galaxy view
- Hover inspection for star systems

### Changed
- N/A

### Fixed
- N/A

---

## [0.1.0] — 2024-XX-XX

### Added
- MonoGame framework (1024×768, DesktopGL)
- Scene manager with stack-based navigation
- Main menu scene with 30 ship wireframe models
- Space scene with 3D wireframe rendering
- Back-face culling (visible edges solid, hidden edges dashed)
- Bitmap font system (runtime GDI+ TrueType rendering)
- Ship models: Sidewinder, Viper, Cobra Mk3, Python, Anaconda, Coriolis Station, and 24 more
- Keyboard controls: arrow keys (rotate), Q/W (roll), +/- (zoom), Space (pause)

### Changed
- N/A

### Fixed
- N/A
