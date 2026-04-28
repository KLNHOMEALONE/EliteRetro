# EliteRetro — Changelog

All notable changes to this project.

---

## [Unreleased]

### Added
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
- **Galaxy map improvements** — crosshair cursor, names on hover only, auto-centered view
- **Phase 4.2: CircleRenderer** — parametric circle drawing using SineTable
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
