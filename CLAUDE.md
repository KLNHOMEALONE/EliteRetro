# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

EliteRetro is an authentic BBC Elite (1984) recreation in C# using MonoGame. Features wireframe 3D ship rendering, procedurally generated galaxies, and keyboard-driven gameplay. Target resolution: 1024×768.

## Build & Run

```bash
dotnet build EliteRetro.sln
dotnet run --project src/EliteRetro.DesktopGL/EliteRetro.DesktopGL.csproj
```

No test projects exist yet. No linter configured.

## Architecture

**Stack-based scene management:**
- `GameInstance` (MonoGame `Game`) owns `SceneManager`
- `SceneManager` maintains `Stack<GameScene>` — only top scene receives Update/Draw
- `ChangeScene()` replaces entire stack; `PushScene()` queues a push (executed during Update); `PopScene()` removes top
- Escape key auto-pops when stack depth > 1

**Scene base class (`GameScene`):**
- `LoadContent(ContentManager, BitmapFont)` — called on push
- `Update(GameTime)` / `Draw(SpriteBatch)` — called per-frame for top scene only
- `UnloadContent()` — called on pop

**3D wireframe rendering:**
- `ShipModel` defines vertices (`Vertex3`), edges (`Edge`), faces (`Face`)
- `WireframeRenderer` draws via `SpriteBatch` with back-face culling and hidden-line removal
- 32 ship models implemented with authentic Elite coordinates

**Galaxy generation:**
- `GalaxyGenerator` produces 8 galaxies × 256 systems using `SeededRandom`
- Current implementation uses simple RNG; Tribonacci twist algorithm planned

**Current scenes:** `MainMenuScene` (ship showcase), `MenuScene` (text menu), `SpaceScene` (interactive 3D viewer with Euler rotation), `GalaxyMapScene` (pannable galaxy map)

## Key Files

| File | Purpose |
|------|---------|
| `src/EliteRetro.Core/GameInstance.cs` | Main game class, owns SceneManager |
| `src/EliteRetro.Core/Scenes/SceneManager.cs` | Stack-based scene management |
| `src/EliteRetro.Core/Scenes/GameScene.cs` | Base class for all scenes |
| `src/EliteRetro.Core/Rendering/WireframeRenderer.cs` | 3D wireframe with back-face culling |
| `src/EliteRetro.Core/Entities/ShipModel.cs` | Base class for ship definitions |
| `src/EliteRetro.Core/Systems/GalaxyGenerator.cs` | Procedural galaxy generation |
| `docs/IMPLEMENTATION_PLAN.md` | Full 8-phase roadmap |
| `docs/TASKTRACKER.md` | Current task tracking |

## Implementation Status

Phase 0 (Foundation) in progress. See `docs/IMPLEMENTATION_PLAN.md` for full roadmap covering: LocalBubbleManager, Minsky flight system, Tribonacci galaxy generation, circle/planet rendering, FlightScene, ship AI, and game systems.
