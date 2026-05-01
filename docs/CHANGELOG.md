# EliteRetro — Changelog

All notable changes to this project.

---

## [Unreleased]

### Added
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
- **Wireframe rendering improvements and FlightScene polish**
  - 8-bit counter cycling 0-255, decrements each Update()
  - Task registration via (mask, offset) pairs: fires when (mcnt & mask) == offset
  - Integrated into GameInstance for global access
  - Remaining tasks (energy regen, tactics, TIDY, station checks, spawn) to be registered when dependent systems are implemented
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

### Fixed
- Planet wireframe vertex overflow (removed erroneous ×1000 scaling)
- Escape key double-handler causing immediate exit
- Texture2D creation per frame in DrawFilledCircle (now cached)
- Integer overflow in LocalBubbleManager (BubbleRadius² exceeded int range)
- Back-face culling freeze on large models

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
