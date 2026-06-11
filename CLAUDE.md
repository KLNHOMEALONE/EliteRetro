# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

EliteRetro is an authentic BBC Elite (1984) recreation in C# using MonoGame. Features wireframe 3D ship rendering, procedurally generated galaxies, and keyboard-driven gameplay. Target resolution: 1024×768.

## Build & Run

```bash
dotnet build EliteRetro.sln
dotnet run --project src/EliteRetro.DesktopGL/EliteRetro.DesktopGL.csproj
dotnet test tests/EliteRetro.Tests/EliteRetro.Tests.csproj
```

No linter configured.

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

**Virtual 1024×768 render target (aspect-preserving):**
- `GameInstance` owns a fixed `RenderTarget2D` (1024×768) and blits it to the backbuffer with letterbox/pillarbox math (`Rendering/VirtualFrame.cs`)
- All scenes draw into the virtual target; backbuffer is cleared black outside the centered gameplay rect
- Layout inside the virtual frame: 3D view on top (1024×~553, 72% height) + HUD strip below (1024×~215, 28% height), matching original BBC Elite vertical stack
- `IGameContext` exposes `VirtualWidth`, `VirtualHeight`, `VirtualHudWidth`; scenes use these for layout, never the actual backbuffer size
- `FlightScene` defines `HudHeightFraction = 0.28f`; `MainMenuScene` uses a left sidebar instead of the vertical 3D+HUD split
- Render target is recreated on `GraphicsDeviceManager.DeviceReset` (resolution change, fullscreen toggle)

**3D wireframe rendering:**
- `ShipModel` defines vertices (`Vertex3`), edges (`Edge`), faces (`Face`)
- `WireframeRenderer` draws via `SpriteBatch` with back-face culling and hidden-line removal
- `WireframeRenderer.Project` outputs to `_graphicsDevice.Viewport` — when the virtual RT is bound, projection math naturally lands in virtual 1024×768 coordinates
- 32 ship models implemented with authentic Elite coordinates

**Galaxy generation:**
- `GalaxyGenerator` produces 8 galaxies × 256 systems using Tribonacci twist seeded RNG

**Current scenes:** `MainMenuScene` (ship showcase + left-sidebar menu), `SpaceScene` (3D viewer + bottom-strip debug), `GalaxyMapScene` (pannable map + bottom-strip info), `FlightScene` (first-person cockpit with 3D top + HUD bottom + save/load)

## Key Files

| File | Purpose |
|------|---------|
| `src/EliteRetro.Core/GameInstance.cs` | Main game class, owns SceneManager + virtual RT |
| `src/EliteRetro.Core/Rendering/VirtualFrame.cs` | Letterbox/pillarbox math for RT blit |
| `src/EliteRetro.Core/Scenes/SceneManager.cs` | Stack-based scene management |
| `src/EliteRetro.Core/Scenes/GameScene.cs` | Base class for all scenes |
| `src/EliteRetro.Core/IGameContext.cs` | Interface; exposes VirtualWidth/Height/HudWidth |
| `src/EliteRetro.Core/Rendering/WireframeRenderer.cs` | 3D wireframe with back-face culling |
| `src/EliteRetro.Core/Entities/ShipModel.cs` | Base class for ship definitions |
| `src/EliteRetro.Core/Systems/GalaxyGenerator.cs` | Procedural galaxy generation |
| `docs/IMPLEMENTATION_PLAN.md` | Full 8-phase roadmap |
| `docs/TASKTRACKER.md` | Current task tracking |

## Implementation Status

Phases 0-7 complete. Phase 8 (Polish & Integration) mostly complete — save/load system, procedural audio, and aspect-preserving virtual 1024×768 frame implemented.

See `docs/IMPLEMENTATION_PLAN.md` for full roadmap and `docs/TASKTRACKER.md` for current task tracking.
