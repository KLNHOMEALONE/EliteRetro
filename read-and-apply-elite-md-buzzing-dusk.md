# Elite (ZX Spectrum 128k) — C#/MonoGame Port Plan

## Context

Project starts from scratch. Goal: port original Elite space sim to modern C#/MonoGame. Three approaches considered; **Option 2: Enhanced Remaster** recommended — preserves soul of original while modernizing for today's players.

## Recommended Approach: Enhanced Remaster

- Faithful core mechanics (procedural galaxy, wireframe rendering, trading, combat)
- Modern QoL: scalable UI, controller support, optional visual enhancements
- Phased delivery: foundation → gameplay → combat/docking → polish

## Project Structure (Created)

```
EliteRetro/
├── src/
│   ├── EliteRetro.Core/           # Shared core library
│   │   ├── Scenes/                # Menu, GalaxyMap, Space, Docking, Market
│   │   ├── Systems/               # GalaxyGenerator, Economy, Combat, Collision
│   │   ├── Entities/              # Ship, SpaceStation, Planet, Projectile
│   │   ├── Rendering/             # WireframeRenderer, HUDRenderer, Effects
│   │   ├── Input/                 # InputManager, InputActions
│   │   └── Utilities/             # RNG, Math helpers, ColorPalette
│   └── EliteRetro.DesktopGL/      # Desktop platform entry point
├── content/
│   ├── Fonts/, Textures/, Audio/, Data/
└── docs/
```

## Implementation Phases

### Phase 1: Foundation (Weeks 1-4)
- MonoGame solution setup, scene management, input system
- Galaxy generation (8 galaxies x 256 systems, seeded RNG)
- Wireframe rendering engine with back-face culling

### Phase 2: Core Gameplay (Weeks 5-10)
- Ship management, 6DOF flight controls, physics
- Trading/economy system with supply/demand pricing
- NPC ships with AI (trader, pirate, police behaviors)

### Phase 3: Combat & Docking (Weeks 11-14)
- Weapons, projectiles, collision, shields/hull damage
- Space station docking (auto + manual challenge mode)
- Mission system (delivery, escort, assassination)

### Phase 4: Polish (Weeks 15-18)
- Particle effects, CRT filter option, starfield rendering
- Audio system (SFX + music with dynamic transitions)
- Save/load, achievements, statistics

### Phase 5: Release (Weeks 19-20)
- Testing (unit, integration, cross-platform)
- Packaging and documentation

## Key Algorithms to Preserve

1. **Procedural galaxy generation** — deterministic seeded RNG, same seed = same galaxy
2. **Wireframe rendering with hidden line removal** — back-face culling, edge-only display
3. **Seeded RNG** — consistent across all procedural content

## Critical Files

- `src/EliteRetro.Core/Systems/GalaxyGenerator.cs` — procedural generation engine
- `src/EliteRetro.Core/Rendering/WireframeRenderer.cs` — 3D wireframe system
- `src/EliteRetro.Core/GameInstance.cs` — main game orchestration
- `src/EliteRetro.Core/Scenes/SpaceScene.cs` — primary gameplay scene
- `content/Data/ShipDefinitions.json` — ship configuration

## Verification

Each phase ends with playable milestones. Phase 1 = navigable galaxy map + rotating wireframe ships. Phase 2 = fly between systems, trade goods, encounter NPCs. Phase 3 = full combat + docking loop. Phase 4 = polished experience. Phase 5 = cross-platform release build.

## Options Presented to User

| Option | Description | Trade-off |
|--------|-------------|-----------|
| 1. Faithful Recreation | Pixel-perfect port, green wireframe, original mechanics | Authentic but dated |
| 2. Enhanced Remaster (Recommended) | Original soul + modern QoL, optional visual upgrades | Balanced scope |
| 3. Spiritual Successor | Modern reimagining, full 3D, multiplayer, modding | Largest scope, risk of losing essence |
