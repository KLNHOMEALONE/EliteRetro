# Drawing Circles and Ellipses — Development Plan

## Overview

This plan adapts the circle and ellipse drawing algorithms from BBC Micro Elite to the EliteRetro Monogame framework. The goal is to render authentic-looking circles and ellipses (for planets, suns, space stations, and the galaxy chart) while leveraging Monogame's `SpriteBatch` instead of the original EOR-based pixel plotting.

## Source Algorithms (from BBC Elite)

### Circles

Elite draws circles using a parametric approach with a 32-entry sine lookup table:

- **Equation**: `x = K * sin(CNT) + cx`, `y = K * cos(CNT) + cy`
- **64 steps** around the circle, CNT 0–63
- Sine table covers 0–90 degrees (quadrant symmetry)
- Step size (STP) controls smoothness: smaller = smoother
- Points are connected via `BLINE` (ball line) segments stored in a **line heap**

### Ellipses

Elite draws ellipses using conjugate half-diameter vectors, supporting arbitrary rotation:

- **Equation**: `x = cx + ux*cos(θ) + vx*sin(θ)`, `y = cy + uy*cos(θ) + vy*sin(θ)`
- Vectors `u` and `v` are conjugate half-diameters (need not be perpendicular)
- Uses same 64-entry sine/cosine table
- `PLS22` iterates θ from 0 to 2π, computing each point and drawing line segments

### Ball Line Heap

The ball line heap is a memory structure for storing line segments:

- **LSX2/LSY2** arrays (78 bytes each) store x/y coordinates of line endpoints
- **LSP** pointer tracks the first free entry
- `&FF` marker in LSY2 indicates segment breaks (for clipping)
- Original purpose: fast erasure via EOR drawing mode
- In Monogame, we skip EOR but keep the segment-buffering approach for clipping

## Current Codebase State

- All rendering goes through `SpriteBatch`
- `WireframeRenderer` already draws lines using a 1x1 white `Texture2D` stretched and rotated
- No circle, ellipse, or arc drawing utilities exist
- Resolution: 1024x768

## Adaptation Strategy

### What to Keep from Elite

1. **Parametric circle algorithm** — sine table lookup with quadrant handling
2. **Conjugate-diameter ellipse algorithm** — supports rotated ellipses naturally
3. **Segment-based drawing** — build lists of line segments, then render (enables clipping)

### What to Adapt for Monogame

1. **No EOR drawing** — Monogame `SpriteBatch` doesn't support XOR blending natively. We draw directly with alpha blending.
2. **No line heap needed for erasure** — each frame is a fresh render. The segment buffer exists only for clipping.
3. **Use existing `DrawLine`** — `WireframeRenderer.DrawLine()` already handles line rendering via `SpriteBatch`.
4. **Floating point math** — Elite used 8-bit fixed-point arithmetic; we use `float`/`Vector2`.

## Implementation Plan

### Phase 1: Sine/Cosine Lookup Table

**File**: `src/EliteRetro.Core/Rendering/SineTable.cs` (new)

- Precompute 64-entry sine table (covering 0–64 steps = 0–2π)
- Provide `Sin(step)` and `Cos(step)` methods (cos = sin with offset)
- Elite used a 32-entry table with quadrant logic; we can store 64 entries for simplicity

```csharp
public static class SineTable
{
    // 64 entries: step 0-64 maps to 0 to 2*PI
    private static readonly float[] Table;

    public static float Sin(int step) => Table[step & 63];
    public static float Cos(int step) => Table[(step + 16) & 63]; // 90 degree offset
}
```

### Phase 2: Circle Drawing

**File**: `src/EliteRetro.Core/Rendering/CircleRenderer.cs` (new)

**Method**: `DrawCircle(Vector2 center, float radius, Color color, int stepSize = 2)`

Algorithm:
1. Iterate CNT from 0 to 64 in steps of `stepSize`
2. For each step, compute: `x = radius * Sin(CNT) + center.X`, `y = radius * Cos(CNT) + center.Y`
3. Store points in a `List<Vector2>`
4. Draw line segments between consecutive points using `WireframeRenderer.DrawLine()`

**Clipping**: Before drawing each segment, check if both endpoints are within screen bounds. Skip off-screen segments.

### Phase 3: Ellipse Drawing

**File**: `src/EliteRetro.Core/Rendering/EllipseRenderer.cs` (new)

**Method**: `DrawEllipse(Vector2 center, Vector2 u, Vector2 v, Color color, int stepCount = 64, int startStep = 0)`

Where:
- `u` = first conjugate half-diameter vector
- `v` = second conjugate half-diameter vector
- `stepCount` = number of steps (64 = full, 32 = half)
- `startStep` = starting angle step (for partial ellipses)

Algorithm:
1. Iterate θ from `startStep` to `startStep + stepCount`
2. For each step, compute:
   ```
   cos = Cos(θ), sin = Sin(θ)
   x = center.X + u.X * cos + v.X * sin
   y = center.Y + u.Y * cos + v.Y * sin
   ```
3. Store points, draw line segments between consecutive points

**Convenience methods**:
- `DrawEllipse(Vector2 center, float radiusX, float radiusY, float rotation, Color color)` — converts axis-aligned ellipse to conjugate vectors

### Phase 4: Integration with WireframeRenderer

**File**: `src/EliteRetro.Core/Rendering/WireframeRenderer.cs` (modify)

Add convenience methods that delegate to the new renderers:

```csharp
public void DrawCircle(Vector2 center, float radius, Color color, int stepSize = 2)
    => CircleRenderer.DrawCircle(_spriteBatch, center, radius, color, stepSize);

public void DrawEllipse(Vector2 center, Vector2 u, Vector2 v, Color color, int stepCount = 64)
    => EllipseRenderer.DrawEllipse(_spriteBatch, center, u, v, color, stepCount);
```

This keeps the existing `SpriteBatch` and `_lineTexture` usage centralized.

### Phase 5: Usage Examples

Wire the new drawing into existing scenes:

1. **Space Scene** — Draw a planet circle behind the wireframe ship
2. **Galaxy Map Scene** — Replace rectangle star markers with small circles
3. **Main Menu Scene** — Optional: add a sun/planet in the background

## File Summary

| File | Action |
|------|--------|
| `src/EliteRetro.Core/Rendering/SineTable.cs` | New — 64-entry sine lookup |
| `src/EliteRetro.Core/Rendering/CircleRenderer.cs` | New — parametric circle drawing |
| `src/EliteRetro.Core/Rendering/EllipseRenderer.cs` | New — conjugate-diameter ellipse drawing |
| `src/EliteRetro.Core/Rendering/WireframeRenderer.cs` | Modify — add `DrawCircle`/`DrawEllipse` convenience methods |
| `src/EliteRetro.Core/Scenes/SpaceScene.cs` | Modify — add circle/ellipse rendering demo |
| `src/EliteRetro.Core/Scenes/GalaxyMapScene.cs` | Modify — use circles for star markers |

## Technical Notes

- **No fixed-point math needed** — Elite used 8-bit fixed-point for the 6502's lack of FPU. We use `float`.
- **No line heap for erasure** — Elite needed the heap because it used EOR drawing (drawing the same pixels twice erases them). Monogame clears and redraws each frame.
- **Segment clipping** — Simple Cohen-Sutherland or boundary check per segment endpoint.
- **Step size defaults** — `stepSize = 2` (32 segments) gives smooth circles at game resolution. `stepSize = 4` (16 segments) for distant/small circles.
- **Filled circles** — Not part of original Elite (wireframe only), but could be added later via `SpriteBatch` primitive fill or a circular texture.

## Verification

After implementation:
1. Circles should appear smooth at default step size
2. Ellipses should support arbitrary rotation via conjugate vectors
3. Off-screen circles should clip cleanly
4. Performance should be negligible (these are cheap line-segment draws)
