# New Game Flow — "Appearing in the Local Bubble"

## User Experience

Player presses "START NEW GAME" in main menu → screen fades → player appears in cockpit view inside the local bubble, planet ahead, sun visible, station orbiting.

## Scene Transition

```
MainMenuScene.HandleSelection()
  → case "START NEW GAME":
    → GameInstance.ChangeScene(new FlightScene())
      → FlightScene.LoadContent()
        → LocalBubbleManager.Initialize()
          → Place planet at slot 0 (position: 0, 0, -24576)
          → Place sun at slot 1 (position derived from system seed)
          → Player at origin (0, 0, 0)
```

## Initial State

### Player
- Position: (0, 0, 0)
- Orientation: Identity matrix (nosev = forward, roofv = up, sidev = right)
- Speed: 0
- Energy: 100 (max varies by ship)
- Ship: Cobra Mk III (default starter ship)

### Planet (Slot 0)
- Position: (0, 0, -24576) — directly ahead, 1 planet radius away
- Orientation: Identity (nosev points toward player)
- Type: Derived from system's tech level (craters vs meridians)
- Radius: 24,576 world coordinates
- Rendered as: Large circle with surface features (craters/meridians based on tech level)

### Sun (Slot 1)
- Position: Behind player, distance 2.67–18.67 × planet radius
  - Calculated from system seed: `z_sign = (s1_hi & 0b111) | 0b10000001` (always negative/behind)
  - x_sign, y_sign: small offset (0–2)
- Rendered as: Horizontal scan lines with random fringe flicker

### Station
- Not yet spawned (waiting for safe zone trigger)

## First Moments

### Frame 1: Spawn
- Fade in from black
- Planet fills lower portion of view (large wireframe circle with features)
- Sun visible behind/above as horizontal scan-line disc
- Stars (stardust particles) streaming past based on player speed
- HUD appears: speed bar, energy bar, compass, scanner

### Frame 2+: Flight Physics Active
- Player input (arrows, Q/W) rotates the **universe** around the player via Minsky algorithm
- All entity positions rotated each frame: `RotatePosition(entityPos, alpha, beta)`
- All entity orientations rotated: `ApplyUniverseRotation(alpha, beta)`
- Player "forward" = negative Z in world space; pressing Up pitches nose down

### Safe Zone Trigger
Each frame, check:
```csharp
Vector3 orbitPoint = planet.Position + 2 * planet.Orientation.Nosev * GameConstants.PlanetRadius;
Vector3 delta = playerPosition - orbitPoint;

if (Math.Abs(delta.X) <= 192 &&
    Math.Abs(delta.Y) <= 192 &&
    Math.Abs(delta.Z) <= 192)
{
    // Spawn station
    LocalBubbleManager.SpawnStation(orbitPoint, -planet.Orientation.Nosev);
    LocalBubbleManager.RemoveSun();
}
```

When triggered:
- Sun removed from slot 1
- Station placed at orbit point
- Station nosev inverted to face planet center
- Station rendered via WireframeRenderer (Coriolis station wireframe)

## Rendering Pipeline (per frame)

1. **Clear** — black
2. **Stardust** — draw starfield particles (perspective-correct, affected by roll/pitch)
3. **Planet** — draw large circle with surface features (craters, meridians)
4. **Sun** — draw scan-line disc with fringe (if present)
5. **Station** — draw wireframe (if spawned)
6. **Other entities** — draw wireframe ships (if any spawned)
7. **HUD** — overlay dashboard elements
8. **Messages** — "STATION IN VIEW" etc.

## HUD Elements

| Element | Position | Data |
|---------|----------|------|
| Speed bar | Bottom-left | 0–40 scale |
| Energy bar | Bottom-left | 0–max_energy scale |
| Compass | Bottom-center | Rotating indicator based on nosev |
| Scanner | Bottom-right | 2D dots for nearby entities |
| Status messages | Top-center | "STATION IN VIEW", "DANGER", etc. |
| Front text | Top-left | "FRONT" / view indicator |

## Controls (Flight Scene)

| Key | Action |
|-----|--------|
| Up/Down | Pitch nose down/up |
| Left/Right | Roll left/right |
| Q/W | Yaw left/right (if supported) |
| +/- | Zoom view |
| V | Cycle view (Front/Rear/Left/Right) |
| Space | Pause |
| Escape | Return to menu |
| F | Fire laser (future) |
| M | Fire missile (future) |
| J | Jump to witchspace (future) |

## Files Involved

| File | Role |
|------|------|
| `FlightScene.cs` | Main scene, orchestrates everything |
| `LocalBubbleManager.cs` | Entity lifecycle, slot management |
| `FlightController.cs` | Input → pitch/roll angles |
| `OrientationMatrix.cs` | Minsky rotation math |
| `PlanetRenderer.cs` | Planet circle + surface features |
| `SunRenderer.cs` | Sun scan-line rendering |
| `StardustRenderer.cs` | Starfield particles |
| `WireframeRenderer.cs` | Station and ship wireframes |
| `HudRenderer.cs` | Dashboard overlay |

## Open Questions

1. **Starting ship**: Cobra Mk III by default? Or let player choose?
2. **Initial credits**: 100 Cr starter amount?
3. **Starting system**: Always Lave (Galaxy 0, System 1)? Or random?
4. **Difficulty**: Affect initial danger level, cargo capacity?
