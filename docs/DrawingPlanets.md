# Planet Drawing Plan

Based on the original BBC Elite techniques documented at elite.bbcelite.com.

## Overview

Planets in Elite are rendered as 2D circles with projected surface features (craters, meridians, equators) and shading. The rendering is purely geometric — no textures. Features are projected from 3D orientation vectors onto 2D ellipses using orthographic projection.

## Core Components

### 1. CircleRenderer (Planet Body)

The planet itself is a filled circle. Use the parametric circle algorithm from `DrawingCircles.md`:

- Precompute a 64-entry sine table
- Draw circle vertices as a triangle fan from screen center
- Radius determined by planet distance

```
src/EliteRetro.Core/Rendering/CircleRenderer.cs
- DrawFilledCircle(SpriteBatch, center, radius, color)
- DrawCircleOutline(SpriteBatch, center, radius, color)
```

### 2. EllipseRenderer (Craters, Meridians, Equators)

Use the conjugate-diameter ellipse technique. An ellipse is defined by two conjugate radius vectors **u** and **v**, derived from 3D orientation vectors:

```csharp
// Given orientation vectors (nosev, roofv, sidev) and distance z:
Vector2 u = new(nosev.X / z, nosev.Y / z);  // x-axis of ellipse
Vector2 v = new(roofv.X / z, roofv.Y / z);  // y-axis of ellipse
```

Points on the ellipse: `center + cos(θ) * u + sin(θ) * v`

```
src/EliteRetro.Core/Rendering/EllipseRenderer.cs
- DrawEllipse(SpriteBatch, center, u, v, color)        // full ellipse
- DrawHalfEllipse(SpriteBatch, center, u, v, startAngle, span, color)
```

## Feature Implementation

### 3. Craters (Drawing Craters)

Craters are small ellipses projected from 3D circles on the planet surface.

**Algorithm:**
1. For each crater, compute its 3D position on the planet sphere
2. Project to screen using orientation vectors:
   - Ellipse center offset: `0.87 * roofv.xy / z` (0.87 = crater depth factor)
   - Conjugate vectors: `u = nosev.xy / (2z)`, `v = sidev.xy / (2z)` (half diameter)
3. Only draw if `roofv.Z > 0` (crater faces the viewer)
4. Draw as shaded ellipse outline

```
src/EliteRetro.Core/Rendering/PlanetRenderer.cs
- DrawCrater(SpriteBatch, center, u, v, color)
```

### 4. Meridians and Equators

Rendered as half-ellipses on the planet surface.

**Algorithm:**
1. Determine if planet has meridians (tech level check: bit 1 clear)
2. For each meridian pair:
   - **Meridian 1**: `u = nosev.xy/z`, `v = roofv.xy/z`
   - **Meridian 2**: `u = nosev.xy/z`, `v = sidev.xy/z`
   - **Equator**: `u = nosev.xy/z`, `v = sidev.xy/z`
3. Compute start angle: `θ = arctan(-nosev_z / roofv_z) / 4`
   - This ensures the half-ellipse touches the planet's outer circle
4. Draw only the visible half (TGT=32 steps in the 64-step ellipse routine)

```
src/EliteRetro.Core/Rendering/PlanetRenderer.cs
- DrawMeridiansAndEquator(SpriteBatch, center, radius, orientation, color)
```

### 5. Saturn's Rings

Rendered as random points within an elliptical band.

**Algorithm:**
1. Define outer ellipse (ring outer edge) and inner ellipse (ring inner edge)
2. Generate random points within the diagonal band between ellipses
3. For each point, check it lies outside inner ellipse AND inside outer ellipse
4. Skip points that fall behind the planet body (check against planet disk equation)
5. Plot valid points as single pixels

```
src/EliteRetro.Core/Rendering/RingRenderer.cs
- DrawRings(SpriteBatch, planetCenter, planetRadius, ringInner, ringOuter, orientation, random)
```

### 6. Sun

Drawn as horizontal scan lines with color gradients.

**Algorithm:**
1. For each pixel row within the sun's radius:
   - Half-width = `sqrt(radius² - row_offset²)`
   - Add random fringe (0-7 pixels) for flicker effect
2. Store lines in a heap for efficient erase/redraw
3. Update only differing line ends between frames (minimize flicker)
4. Color varies by context: white (normal), red-yellow (damage), blue (special)

```
src/EliteRetro.Core/Rendering/SunRenderer.cs
- DrawSun(SpriteBatch, center, radius, colorScheme, random)
- EraseSun(SpriteBatch, previousLines)  // efficient line-by-line erase
```

### 7. Explosion Clouds

Particle-based expanding/contracting sphere.

**Algorithm:**
1. Cloud counter starts at 18, increases by 4 each frame
2. Cloud size = `counter / distance`
3. Particle count = counter (expands until counter > 128, then contracts)
4. For each vertex in the ship's blueprint:
   - Seed RNG with stored bytes
   - Plot random points within vertex radius
   - Random point sizes
5. Cloud disappears when counter overflows

```
src/EliteRetro.Core/Rendering/ExplosionRenderer.cs
- DrawExplosion(SpriteBatch, position, distance, shipModel, counter, random)
```

### 8. Stardust (Starfield)

Particle system with perspective-correct movement.

**Front View:**
- 16-bit sign-magnitude coordinates (x, y, z) with origin at screen center
- Movement: `z -= speed * 64`
- Perspective projection: `q = 64 * speed / z_hi`
- Screen offset: `y += |y_hi| * q`, `x += |x_hi| * q`
- Roll: `y += alpha * x / 256`, `x -= alpha * y / 256`
- Pitch: `y -= beta * 256`, `x += 2 * (beta * y / 256)²`

**Side View:**
- Sideways movement: `delta_x = 8 * 256 * speed / z_hi` (signed by view direction)
- Pitch rotation around midpoint (like front view roll)
- Roll applied to y-axis

```
src/EliteRetro.Core/Rendering/StardustRenderer.cs
- UpdateStardust(float speed, float roll, float pitch)
- DrawStardust(SpriteBatch)
```

## Planet Data Model

```csharp
// src/EliteRetro.Core/Entities/Planet.cs
public enum PlanetType
{
    Rocky,          // Craters only
    Volcanic,       // Craters + different coloring
    Terran,         // Meridians + equator
    Ice,            // Smooth, no features
    GasGiant,       // Horizontal bands
    RingedGasGiant  // Gas giant + rings
}

public record struct Planet(
    PlanetType Type,
    float Radius,
    Vector3 Position,
    OrientationMatrix Orientation,  // nosev, roofv, sidev
    Color PrimaryColor,
    Color SecondaryColor,
    int TechLevel,  // determines feature visibility
    uint Seed       // deterministic random for features
);
```

## Implementation Order

1. **CircleRenderer** — prerequisite for everything
2. **EllipseRenderer** — prerequisite for craters/meridians
3. **PlanetRenderer** — core planet body + craters + meridians/equator
4. **SunRenderer** — special case of circle rendering
5. **RingRenderer** — Saturn-style rings
6. **ExplosionRenderer** — particle-based clouds
7. **StardustRenderer** — starfield (independent of above)

## Key Mathematical Insights

### Conjugate Diameter Ellipses
An ellipse can be defined by two vectors **u** and **v** from center to edge, where the vectors are projections of perpendicular 3D axes. Any point: `P(θ) = center + cos(θ)·u + sin(θ)·v`. This avoids expensive ellipse parameter computation.

### Orthographic Projection
Elite uses orthographic (not perspective) projection for surface features. Only the x,y components of orientation vectors are used, divided by z (distance). The z-component is only used for visibility culling and start angle calculation.

### Half-Ellipse Start Angle
The formula `θ = arctan(-u_z, v_z)` computes where the projected great circle intersects the planet's silhouette. Drawing only the half where the surface faces the viewer creates the correct 3D illusion.

### Probabilistic 3D Lighting (Saturn)
The `x = sqrt(r² - (r1² + r2²))` formula packs Lambertian reflectance into a single line. Points are denser near the center (brighter) and sparser near the edge (darker), creating a fake 3D lighting effect without per-pixel computation.

## References

- [Drawing Craters](https://elite.bbcelite.com/deep_dives/drawing_craters.html)
- [Drawing Meridians and Equators](https://elite.bbcelite.com/deep_dives/drawing_meridians_and_equators.html)
- [Drawing Saturn on the Loading Screen](https://elite.bbcelite.com/deep_dives/drawing_saturn_on_the_loading_screen.html)
- [Drawing the Sun](https://elite.bbcelite.com/deep_dives/drawing_the_sun.html)
- [Drawing Explosion Clouds](https://elite.bbcelite.com/deep_dives/drawing_explosion_clouds.html)
- [Stardust in the Front View](https://elite.bbcelite.com/deep_dives/stardust_in_the_front_view.html)
- [Stardust in the Side Views](https://elite.bbcelite.com/deep_dives/stardust_in_the_side_views.html)
