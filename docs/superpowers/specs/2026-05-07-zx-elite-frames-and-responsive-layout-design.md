## Goal

Match the original ZX Spectrum Elite framing style by drawing **thin white rectangular frames** around:

- **Main 3D view** (top window)
- **Dashboard / HUD** (bottom instrument panel)

While ensuring the layout remains correct when the game resolution changes via settings.

## Non-goals

- Pixel-perfect reproduction of ZX Spectrum font, palette artifacts, or scanline effects.
- Rebuilding the full ZX cockpit geometry; this spec is strictly about **framing + responsive bounds**.

## Current state (codebase)

- `FlightScene` and `HudRenderer` use hard-coded 1024×768 constants.
- Space view is implicitly the top portion (`HudViewportHeight = 480`), dashboard starts at Y=480.
- Scanner and bar geometry are also hard-coded to that design resolution.

This works at 1024×768 but will misalign at other resolutions.

## Proposed layout model (percentage-based)

All layout rectangles are computed from the **actual backbuffer size** each draw:

- Let \(W,H\) = `GraphicsDevice.PresentationParameters.BackBufferWidth/Height` (or viewport width/height).
- Define a constant dashboard height fraction:
  - `hudFrac = 0.375` (since 288/768 = 37.5%)
  - `hudH = round(H * hudFrac)`
  - `viewH = H - hudH`
- Define:
  - `MainViewRect = (0, 0, W, viewH)`
  - `HudRect = (0, viewH, W, hudH)`

### Insets / margins

The ZX reference frame does not touch the very edge of the bitmap; it leaves a small margin.
We will apply an inset in pixels computed from resolution:

- `outerMargin = max(2, round(min(W,H) * 0.008))`  (about ~6px at 768p height)
- `framedMainViewRect = MainViewRect.Inflate(-outerMargin, -outerMargin)`
- `framedHudRect = HudRect.Inflate(-outerMargin, -outerMargin)`

This preserves “breathing room” across different resolutions without letterboxing.

## Border rendering

Draw a **white rectangle outline** (no fill) around `framedMainViewRect` and `framedHudRect`.

- Color: `Color.White`
- Thickness:
  - Default: `1` pixel (authentic thin frame)
  - Optional future setting: scale thickness for very high resolutions (e.g. `max(1, round(min(W,H)/768))`)

Frame is drawn **after** the view content and HUD content so it stays visible.

## Integration points

### `FlightScene`

- Replace hard-coded `ScreenWidth`, `ScreenHeight`, `HudViewportHeight`, `ScreenCenter` with values derived from `(W,H)` and `viewH`.
- Projection aspect ratio should use `W / (float)viewH` (guard against `viewH <= 0`).
- Damage flash, crosshair, and any HUD text positions should be based on computed bounds.
- Call a small helper to draw the two frames each draw.

### `HudRenderer` / `ScannerRenderer`

Phase 1 (minimal change to satisfy this feature):

- Keep existing HUD drawing logic, but route it through `HudRect` coordinates:
  - `HudRenderer.Draw(...)` accepts `Rectangle hudRect` and uses it instead of hard-coded `DashY/DashH`.
  - Left/center/right widths are computed as fractions of `hudRect.Width`:
    - left = 0.25W, center = 0.50W, right = 0.25W
- `ScannerRenderer` should accept the computed center panel rect (or at least the `hudRect`) and compute its ellipse geometry from that.

Phase 1 ensures that bars/scanner remain centered and proportional at any resolution.

## Success criteria

- At any resolution, the main view and dashboard are clearly separated and each has a visible white rectangular frame.
- No hard-coded 1024×768 layout constants remain in the rendering path for `FlightScene` HUD + view bounds.
- The framing visually matches the ZX reference: thin, clean, and enclosing the intended regions.

