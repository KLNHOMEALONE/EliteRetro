# Moving and Rotating in Space — Implementation Plan

## 1. Overview

In Elite, the player's ship never rotates. Instead, the entire universe rotates *around* the ship. This "inverse rotation" approach means pitch, roll, and yaw are applied to every entity's position and orientation vectors each frame. Combined with small-angle approximations (Minsky circle algorithm), this creates stable, efficient rotation on limited hardware.

### Key Sources
- [Pitching and Rolling](https://elite.bbcelite.com/deep_dives/pitching_and_rolling.html)
- [Pitching and Rolling by a Fixed Angle](https://elite.bbcelite.com/deep_dives/pitching_and_rolling_by_a_fixed_angle.html)
- [Program Flow of the Ship-Moving Routine](https://elite.bbcelite.com/deep_dives/program_flow_of_the_ship-moving_routine.html)
- [Rotating the Universe](https://elite.bbcelite.com/deep_dives/rotating_the_universe.html)
- [Orientation Vectors](https://elite.bbcelite.com/deep_dives/orientation_vectors.html)
- [Tidying Orthonormal Vectors](https://elite.bbcelite.com/deep_dives/tidying_orthonormal_vectors.html)
- [Flipping Axes Between Space Views](https://elite.bbcelite.com/deep_dives/flipping_axes_between_space_views.html)

## 2. Orientation Vectors (Rotation Representation)

Each entity has three orthonormal vectors stored as a 3×3 rotation matrix:

| Vector | Meaning | Initial Value |
|--------|---------|---------------|
| **nosev** | Forward direction (local +z) | (0, 0, 96) |
| **roofv** | Up direction (local +y) | (0, 96, 0) |
| **sidev** | Right direction (local +x) | (96, 0, 0) |

Unit length is represented as **96** (0x6000 in 16-bit sign-magnitude). The three vectors form the rows of a left-handed rotation matrix:

```
| sidev.x  sidev.y  sidev.z |   ← row 0 (right vector)
| roofv.x  roofv.y  roofv.z |   ← row 1 (up vector)
| nosev.x  nosev.y  nosev.z |   ← row 2 (forward vector)
```

**Orthonormality requirements:**
- All three vectors must be unit length (magnitude = 96)
- All three vectors must be mutually perpendicular (dot product = 0)
- Must form a left-handed coordinate system (thumb=roofv, index=nosev, middle=sidev)

Due to small-angle approximation drift, vectors must be periodically corrected using the TIDY routine.

## 3. Small-Angle Rotation (Minsky Circle Algorithm)

### 3.1 Player Pitch and Roll — Universe Rotation

The player's pitch (β) and roll (α) rotate all entity positions around the player. Using small-angle approximations (sin θ ≈ θ, cos θ ≈ 1), the transformation becomes:

```
Roll (α) then Pitch (β), reusing updated values:

K2 = y - α·x
z  = z + β·K2
y  = K2 - β·z          ← uses updated z
x  = x + α·y           ← uses updated y
```

Expanded form:
- x' = x + α·(y - α·x - β·(z + β·(y - α·x)))
- y' = y - α·x - β·(z + β·(y - α·x))
- z' = z + β·(y - α·x)

This is a "mixed-up order" Minsky rotation — slightly sheared but numerically stable with integer arithmetic.

**Angle scaling:** Input angles are divided by 256, mapping to ranges of 0–0.125 radians (roll) and 0–0.03125 radians (pitch).

### 3.2 Entity Pitch and Roll — Fixed Angle

Enemy ships pitch and roll by a fixed angle of **1/16 radians (3.6°)** per iteration. At this small angle:

```
sin(1/16) ≈ 1/16
cos(1/16) ≈ 1 - 1/512
```

The rotation equations for an entity's own pitch/roll become:

```
x' = x·(1 - 1/512) - y/16
y' = y·(1 - 1/512) + x/16
```

Applied separately for roll (on roofv and sidev) and pitch (on roofv and nosev), processing x, y, z components sequentially.

## 4. Ship-Moving Routine (MVEIT) — Program Flow

Called for each nearby ship in the main flight loop. Nine phases:

| Phase | Operation | Details |
|-------|-----------|---------|
| 1 | **Tidying** | Call TIDY on ship's orientation vectors |
| 2 | **Tactics & Scanner** | Apply AI tactics, remove from scanner |
| 3 | **Forward Movement** | Ship moves along its nosev direction by speed |
| 4 | **Acceleration** | Apply acceleration to speed, then zero acceleration |
| 5 | **Universe Rotation (Position)** | Rotate ship's position by player pitch/roll |
| 6 | **Player Speed Effect** | Translate ship by player's forward velocity |
| 7 | **Universe Rotation (Orientation)** | Rotate ship's orientation vectors by player pitch/roll (MVS4) |
| 8 | **Ship's Own Rotation** | Apply ship's own pitch/roll with damping |
| 9 | **Scanner Update** | Redraw ship on scanner (or hide if destroyed) |

## 5. Tidying Orthonormal Vectors (TIDY Routine)

Applied to one ship per iteration (round-robin across 12 of 16 loop iterations). Three steps:

### Step 1: Normalize nosev
```
nosev' = normalize(nosev) × 96
```

### Step 2: Make roofv orthogonal to nosev'
Choose the axis corresponding to the **smallest** component of nosev' (avoids division by small numbers). If nosev_x' is smallest:

```
roofv_x' = -(nosev_y' · roofv_y + nosev_z' · roofv_z) / nosev_x'
```

Then normalize roofv'.

### Step 3: Recompute sidev via cross-product
```
sidev_x' = (nosev_z' · roofv_y' - nosev_y' · roofv_z') / 96
sidev_y' = (nosev_x' · roofv_z' - nosev_z' · roofv_x') / 96
sidev_z' = (nosev_y' · roofv_x' - nosev_x' · roofv_y') / 96
```

Division by 96 scales to unit length (since internal representation uses 96 = 1.0).

## 6. Flipping Axes Between Space Views (PLUT Routine)

The local universe is stored as if looking forward (z-axis = direction of travel). Different views reuse the same drawing routines by transforming axes:

| View | Transformation |
|------|---------------|
| **Front** | No change |
| **Rear** | Negate x and z coordinates and all x/z components of orientation vectors |
| **Left** | Swap x↔z axes, then negate new z (the axis going in/out of screen) |
| **Right** | Swap x↔z axes, then negate new x (the axis going to the right) |

Applied after ship data is copied to INWK workspace. Used only for drawing and target lock calculations — the underlying data is unchanged.

## 7. Implementation

### 7.1 New Types

#### `OrientationMatrix` (new file: `src/EliteRetro.Core/Entities/OrientationMatrix.cs`)

```csharp
public struct OrientationMatrix
{
    // Rows of the 3x3 rotation matrix
    public Vector3 Sidev;   // Right vector (row 0)
    public Vector3 Roofv;   // Up vector (row 1)
    public Vector3 Nosev;   // Forward vector (row 2)

    // Unit length representation: 96.0f = 1.0
    public const float UnitLength = 96.0f;

    public static OrientationMatrix Identity => new()
    {
        Sidev = new Vector3(UnitLength, 0, 0),
        Roofv = new Vector3(0, UnitLength, 0),
        Nosev = new Vector3(0, 0, UnitLength)
    };

    // Apply player's pitch (beta) and roll (alpha) to a position
    public static Vector3 RotatePosition(Vector3 pos, float alpha, float beta);

    // Apply player's pitch and roll to orientation vectors
    public void ApplyUniverseRotation(float alpha, float beta);

    // Apply entity's own pitch/roll (fixed angle: 1/16 rad)
    public void ApplyOwnRotation(bool pitchUp, bool rollLeft);

    // Tidy vectors to restore orthonormality
    public void Tidy();

    // Transform a local vertex to world space
    public Vector3 TransformLocalToWorld(Vertex3 local);
}
```

#### `FlightController` (new file: `src/EliteRetro.Core/Systems/FlightController.cs`)

```csharp
public class FlightController
{
    // Player input state
    public float PitchInput { get; private set; }   // -1 to +1
    public float RollInput { get; private set; }    // -1 to +1
    public float YawInput { get; private set; }     // -1 to +1 (if supported)

    // Scaled angles for Minsky rotation
    public float Alpha => RollInput / 256f;  // Roll angle (0-0.125 rad)
    public float Beta => PitchInput / 256f;  // Pitch angle (0-0.03125 rad)

    public const float FixedRotationAngle = 1f / 16f;        // 3.6 degrees
    public const float FixedRotationSin = 1f / 16f;          // sin(1/16)
    public const float FixedRotationCos = 1f - 1f / 512f;    // cos(1/16)

    public void Update();  // Read keyboard input
}
```

### 7.2 Modified Types

#### `EntityInstance` (modify existing or create new)

Add orientation and position:

```csharp
public class EntityInstance
{
    public ShipModel Model { get; }
    public Vector3 Position { get; set; }           // World coordinates
    public Vector3 Velocity { get; set; }           // Coordinates per iteration
    public OrientationMatrix Orientation { get; set; }
    public float Speed { get; set; }
    public float Energy { get; set; }
    public int SlotIndex { get; set; }
}
```

#### `SpaceScene` (modify existing)

Replace Euler angle rotation with Minsky universe rotation:

```csharp
// Remove: private Vector3 _rotation = Vector3.Zero;
// Add:
private FlightController _flightController = new();
private OrientationMatrix _playerOrientation = OrientationMatrix.Identity;

// In Update():
_flightController.Update();

// Rotate entire universe around player
foreach (var entity in _entities)
{
    // Phase 5: Rotate position by player pitch/roll
    entity.Position = OrientationMatrix.RotatePosition(
        entity.Position,
        _flightController.Alpha,
        _flightController.Beta);

    // Phase 7: Rotate orientation vectors
    entity.Orientation.ApplyUniverseRotation(
        _flightController.Alpha,
        _flightController.Beta);

    // Phase 8: Entity's own rotation (AI or damping)
    entity.Orientation.ApplyOwnRotation(entity.PitchAI, entity.RollAI);

    // Phase 3: Forward movement along nosev
    entity.Position += entity.Orientation.Nosev * entity.Speed;
}

// Phase 1: Periodic tidying (round-robin)
_tidyIndex = (_tidyIndex + 1) % _entities.Count;
_entities[_tidyIndex].Orientation.Tidy();
```

### 7.3 View System

Add space view switching (front, rear, left, right):

```csharp
public enum SpaceView { Front, Rear, Left, Right }

public static class ViewTransform
{
    public static void ApplyViewTransform(EntityInstance entity, SpaceView view)
    {
        // Creates a transformed copy for rendering only
        // Original data unchanged
        switch (view)
        {
            case SpaceView.Rear:
                // Negate x and z
                break;
            case SpaceView.Left:
                // Swap x<->z, negate new z
                break;
            case SpaceView.Right:
                // Swap x<->z, negate new x
                break;
        }
    }
}
```

## 8. Files to Create

| File | Purpose |
|------|---------|
| `src/EliteRetro.Core/Entities/OrientationMatrix.cs` | 3×3 rotation matrix with Minsky rotation, tidying |
| `src/EliteRetro.Core/Systems/FlightController.cs` | Input handling, angle scaling |
| `src/EliteRetro.Core/Systems/ViewTransform.cs` | Space view axis flipping |

## 9. Files to Modify

| File | Changes |
|------|---------|
| `src/EliteRetro.Core/Scenes/SpaceScene.cs` | Replace Euler angles with Minsky universe rotation |
| `src/EliteRetro.Core/Entities/EntityInstance.cs` | Add Orientation field (or create if missing) |
| `src/EliteRetro.Core/GameConstants.cs` | Add rotation constants (from local bubble plan) |

## 10. Implementation Steps

### Phase 1: Orientation System
1. Create `OrientationMatrix` with identity construction
2. Implement `RotatePosition` (Minsky circle for positions)
3. Implement `ApplyUniverseRotation` (Minsky for orientation vectors)
4. Implement `Tidy` (Gram-Schmidt orthonormalization)
5. Implement `TransformLocalToWorld` (vertex transformation)

### Phase 2: Flight Control
6. Create `FlightController` with keyboard input mapping
7. Map arrow keys → pitch, Q/W → roll
8. Implement angle scaling (divide by 256)

### Phase 3: Entity Movement
9. Add `Orientation` field to entity instances
10. Implement forward movement along nosev
11. Implement fixed-angle own rotation for AI ships

### Phase 4: Integration
12. Modify `SpaceScene` to use Minsky rotation instead of Euler angles
13. Add view switching (front/rear/left/right) with axis transforms
14. Add periodic tidying (round-robin across entities)

### Phase 5: Polish
15. Verify orthonormality is maintained over long rotation sequences
16. Tune rotation speed constants for authentic feel

## 11. Key Formulas Reference

### Minsky Universe Rotation (player pitch/roll on entity position)
```
K2 = y - α·x
z  = z + β·K2
y  = K2 - β·z
x  = x + α·y
```

### Minsky Orientation Rotation (same formula on vector components)
```
// Roll (α) on roofv and sidev
roofv_y' = roofv_y - α·roofv_x
roofv_x' = roofv_x + α·roofv_y'

// Pitch (β) on roofv and nosev
roofv_y'' = roofv_y' - β·roofv_z
roofv_z'  = roofv_z + β·roofv_y''
```

### Fixed-Angle Entity Rotation (1/16 rad)
```
x' = x·(1 - 1/512) - y/16
y' = y·(1 - 1/512) + x/16
```

### Tidy: Recompute sidev from cross-product
```
sidev = (nosev × roofv) / 96
```

### View Transform: Rear view
```
x' = -x,  z' = -z  (all x/z components negated)
```

### View Transform: Left view
```
swap(x, z);  z' = -z;
```

### View Transform: Right view
```
swap(x, z);  x' = -x;
```
