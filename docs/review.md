# Flight Systems Code Review

**Date:** 2026-05-03  
**Reviewer:** MiniMax M2.7  
**Files Reviewed:** FlightScene.cs, FlightControlService.cs, ShipInstance.cs, OrientationMatrix.cs, CollisionSystem.cs, DockingSystem.cs, LocalBubbleManager.cs, GameConstants.cs

---

## Executive Summary

The flight systems implement authentic BBC Elite mechanics with rotating universe (Minsky algorithm), sphere-based collision, and a 5-check docking system. Several bugs and design issues exist that affect gameplay correctness and crash potential.

---

## Critical Bugs

### 1. **SUN/STATION SLOT OVERWRITE** (FlightScene.cs:189)
**Severity: HIGH**

In `InitializeBubble()`, the sun is placed at slot 1 (line 169), then the station is also placed at slot 1 (line 189), overwriting the sun reference. Only the station will exist — the sun is silently discarded.

```csharp
// Line 169: Sun placed at slot 1
_bubbleManager.SetSlot(GameConstants.SunStationSlot, sun);

// Lines 171-189: Station overwrites slot 1
_bubbleManager.SetSlot(GameConstants.SunStationSlot, station); // Sun is lost!
```

**Fix:** Use conditional slot assignment — sun OR station, not both.

---

### 2. **SUN PROXIMITY CHECK ALWAYS SKIPS** (FlightScene.cs:301-302)
**Severity: HIGH**

`CheckSunProximity()` is called each frame but the result is assigned to `sunEffect` with no branch to apply heat damage, fuel scooping, or fatal consequences. The TODO comment confirms this is dead code:

```csharp
var sunEffect = _bubbleManager.CheckSunProximity();
// TODO: Apply heat damage, fuel scooping, etc. based on sunEffect
```

---

### 3. **NULL REFERENCE ON SCENE RE-ENTRY** (FlightScene.cs:110-125)
**Severity: HIGH**

`EnsureInitialized()` sets `_initialized = true` after calling `InitializeBubble()`. If FlightScene is revisited, `_initialized` is true but `_bubbleManager` may not have been set via the constructor (when `game` is not `GameInstance`):

```csharp
if (_initialized) return;  // Returns early
// _bubbleManager could be null if constructed with null game
```

**Fix:** Guard `_bubbleManager` access or check for null in all methods.

---

### 4. **FACE TARGET PRODUCES NON-ORTHONORMAL BASIS** (ShipInstance.cs:155-163)
**Severity: MEDIUM**

`FaceTarget()` uses a simple cross product sequence that can fail when `direction` is parallel to `UnitY`:

```csharp
Orientation.Sidev = Vector3.Normalize(Vector3.Cross(direction, up));  // Fails if direction ≈ UnitY
Orientation.Roofv = Vector3.Cross(Orientation.Sidev, direction);
```

If `direction` is near `(0, ±1, 0)`, the cross product with `UnitY` yields near-zero, causing NaN in normalization.

---

## Design Issues

### 5. **INCONSISTENT ROTATION FORMULAS** (OrientationMatrix.cs:40-51 vs 125-130)
**Severity: MEDIUM**

Two different rotation algorithms exist for two different use cases:
- `ApplyUniverseRotation()` (line 40) — body-axis rotation, uses updated intermediate values (Minsky-style)
- `ApplyMinskyRotation()` (line 125) — per-component rotation, also Minsky-style

`ShipInstance.ApplyUniverseRotation()` calls `ApplyMinskyRotation()` on orientations while doing per-component rotation on positions — this is correct for Elite's MVS4, but the dual-algorithm design is confusing and error-prone.

---

### 6. **FLIGHTCONTLSERVICE IGNORES SPEED DELTA CORRECTLY** (FlightControlService.cs:71-74)
**Severity: MEDIUM**

Speed control returns `SpeedDelta = ±1f` regardless of key held duration. FlightScene then applies `dt * 60` scaling:

```csharp
// FlightScene line 283
_playerSpeed = Math.Clamp(_playerSpeed + _lastControl.SpeedDelta * (float)gameTime.ElapsedGameTime.TotalSeconds * 60, 0f, 40f);
```

This produces non-linear acceleration (dt varies per frame) and no maximum acceleration cap. Also, W/S are toggle-style, not continuous — holding W jumps speed instantly by ~60 units/sec on a 60fps machine.

---

### 7. **LASER HIT DETECTION ONLY CHECKS Z SIGN** (FlightScene.cs:954)
**Severity: MEDIUM**

```csharp
if (entity.Position.Z >= 0) continue;  // Only checks if in front, not crosshair alignment
```

The actual crosshair cone check (`hitConeCos = 0.96f`) is bypassed for very close entities (`dist < 5.0`). This means anything within 5 units of the player is always hit regardless of aim.

---

### 8. **BASECOLLISIONRADIUS = 200 IS ENORMOUS** (CollisionSystem.cs:17)
**Severity: MEDIUM**

Collision radius starts at 200 units base, scaled by vertex count. Given that entities spawn 100-300 units away, most entities are always colliding. The debug log (line 65-68) even shows "close approach" triggers at `combinedRadius * 3` — meaning 1200+ units, which is basically everything.

---

### 9. **FIRE LASER AUDIO CALLED BEFORE TARGET CHECK** (FlightScene.cs:204-206)
**Severity: LOW**

Audio `PlayLaser()` fires before the hit detection determines if there's a target. A "pew" sound plays even on missed shots.

---

### 10. **SAVE GAME DOESN'T USE ACTIVE SCENE DATA** (FlightScene.cs:908-924)
**Severity: LOW**

`SaveGame()` hardcodes galaxy 0, system 0 regardless of actual player position. When full galaxy travel is implemented, this will cause save corruption or incorrect positioning.

---

## Potential Crashes

### 11. **DIVISION BY ZERO IN PROJECTTOSCREEN** (FlightScene.cs:871)
```csharp
if (projected.W == 0) return new Vector2(512, 240);  // Only checks W=0, not near-zero
```

`projected.W` can be very small (near-zero) causing huge `ndcX/ndcY` values.

### 12. **ROTATEVECTOR REUSES INTERMEDIATE VALUES INCORRECTLY** (OrientationMatrix.cs:132-141)
```csharp
float k2 = v.Y - alpha * v.X;
float z = v.Z + beta * k2;
float y = k2 - beta * z;   // Uses updated z — correct Minsky
float x = v.X + alpha * y; // Uses updated y — correct Minsky
```

Actually correct. The comment at line 101-102 in ShipInstance.cs claims `ApplyMinskyRotation` follows different logic, but it doesn't — the implementation is consistent.

---

## Missing Functionality

- **No hyperspace/jump system** — `JumpOffset` constant exists but no implementation
- **Docking computer** (`DockingSystem.UpdateDockingComputer()`) is defined but not wired to any scene
- **Front/rear shield distinction** — `CollisionSystem.ApplyPlayerDamage()` checks hit direction but HUD uses single Energy value
- **Missiles** — referenced in HUD but no firing/locking logic
- **Cargo canisters don't move** — spawned with `Speed = 0` so they just drift in place

---

## Recommendations

1. **Fix sun/station overwrite immediately** — blocks sun/station coexistence
2. **Wire sun proximity effects** — remove dead code or implement the TODO
3. **Add null checks in FlightScene methods** — prevent crashes on scene re-entry
4. **Reduce collision radius** — 200 base is too large, try 20-40
5. **Fix laser audio timing** — only play on confirmed hit
6. **Implement hyperspace** — currently only stub constant exists
7. **Wire docking computer to FlightScene** — currently orphaned code

---

## Test Scenarios

| Scenario | Expected | Actual |
|----------|----------|--------|
| Start FlightScene twice | Should initialize correctly | Null ref crash likely |
| Fly into sun | Fatal damage applied | No effect (TODO) |
| Fire lasers at empty space | Pew sound + no damage | Pew + no damage (OK) |
| Get within 5 units of ship | Automatic hit | Works as designed (bug) |
| Spawn station while sun exists | Both visible | Sun disappears (bug) |
| Hold W for 1 second | Gradual acceleration | ~60 units/sec instant (design issue) |

---

## Second Review Pass - Additional Findings (2026-05-03)

A second independent review confirmed all findings above and discovered additional issues:

---

### 13. **RAM MODE ORIENTATION USES UNINITIALIZED ROOFV** (FlightScene.cs:575-577)
**Severity: MEDIUM**

When spawning a ship in ram mode, the orientation is computed incorrectly:

```csharp
orientation = new OrientationMatrix
{
    Nosev = toPlayer,
    Roofv = Vector3.Normalize(Vector3.Cross(toPlayer, Vector3.UnitY)),
    Sidev = Vector3.Normalize(Vector3.Cross(orientation.Roofv, toPlayer))  // BUG!
};
```

The `Sidev` calculation references `orientation.Roofv` before the new `Roofv` is assigned. This uses the default `OrientationMatrix.Identity` value from line 557, not the newly computed Roofv.

**Fix:**
```csharp
var newRoofv = Vector3.Normalize(Vector3.Cross(toPlayer, Vector3.UnitY));
orientation = new OrientationMatrix
{
    Nosev = toPlayer,
    Roofv = newRoofv,
    Sidev = Vector3.Normalize(Vector3.Cross(newRoofv, toPlayer))
};
```

---

### 14. **DOUBLE PAUSE STATE** (FlightScene.cs:37 + FlightControlService.cs:34,92)
**Severity: LOW**

Pause state is tracked in two places independently:
- `FlightScene._paused` (line 37)
- `FlightControlService._isPaused` (line 34)

Both are toggled by the P key. FlightScene checks `!_lastControl.IsPaused` before toggling its own `_paused` (line 317), but the dual tracking is confusing and could desync.

---

### 15. **SHIELD VALUES NOT SYNCED WITH PLAYER SHIP** (LocalBubbleManager.cs:54-60 vs 97-105)
**Severity: MEDIUM**

LocalBubbleManager has separate `PlayerEnergy`, `PlayerShieldFront`, `PlayerShieldAft`, `PlayerHull` properties (lines 54-66), but also creates a `PlayerShip` instance with its own `Energy` and `Hull` fields. These are not synchronized:

- `CollisionSystem.ApplyPlayerDamage()` modifies `playerShip.Energy` and `playerShip.Hull`
- `HudRenderer` uses `bubble.PlayerShip?.Energy` (via HUDState)
- But some code may read `bubble.PlayerEnergy` expecting it to be current

**Fix:** Either use only `PlayerShip.Energy/Hull` or sync the properties on each frame.

---

### 16. **STALE LASERS DATA IN SAVES** (SaveGameManager.cs:87-90)
**Severity: LOW**

Laser types are hardcoded to 0 (none) in SaveGameManager despite the player being able to fire lasers in FlightScene:

```csharp
data[OffLaser0] = 0;
data[OffLaser1] = 0;
data[OffLaser2] = 0;
data[OffLaser3] = 0;
```

When the equipment system is implemented, this will silently lose player's laser upgrades on save.

---

### 17. **UNUSED _circleRenderer FIELD** (FlightScene.cs:20, 87)
**Severity: LOW (Code Smell)**

`_circleRenderer` is created in `LoadContent()` but never used anywhere in FlightScene. Dead allocation.

---

### 18. **TARGET PRACTICE CLEARS SUN/STATION** (FlightScene.cs:622-633)
**Severity: LOW**

`SpawnTargetPracticeShip()` calls `_bubbleManager.Clear()` which removes sun/station (slot 1) but only restores the planet. The sun is permanently lost when toggling target practice mode on.

---

### 19. **POTENTIAL INTEGER OVERFLOW IN COLLISION RADIUS** (CollisionSystem.cs:80-81)
**Severity: LOW**

```csharp
int vertexCount = ship.Blueprint.Model.Vertices.Count;
float sizeFactor = 1f + (vertexCount / 20f);
return BaseCollisionRadius * sizeFactor;
```

If a model has many vertices (e.g., 200), the collision radius becomes `200 * (1 + 10) = 2200` units — larger than spawn distance (100-300 units). New entities would immediately collide.

---

### 20. **EVENTS NOT UNSUBSCRIBED (MEMORY LEAK)** (FlightScene.cs:77-79)
**Severity: MEDIUM**

Events are subscribed in the constructor:
```csharp
_bubbleManager.EntityEvent += OnEntityEvent;
_bubbleManager.CollisionEvent += OnCollision;
```

But `UnloadContent()` is empty — events are never unsubscribed. If FlightScene is created/destroyed multiple times, handlers accumulate and fire multiple times per event.

**Fix:**
```csharp
public override void UnloadContent()
{
    if (_bubbleManager != null)
    {
        _bubbleManager.EntityEvent -= OnEntityEvent;
        _bubbleManager.CollisionEvent -= OnCollision;
    }
    _whitePixel?.Dispose();
}
```

---

## Updated Summary

| Category | Original | New | Total |
|----------|----------|-----|-------|
| Critical Bugs | 4 | 0 | 4 |
| Design Issues | 6 | 3 | 9 |
| Potential Crashes | 2 | 0 | 2 |
| Code Smells | 0 | 5 | 5 |
| Missing Features | 5 | 0 | 5 |

### Priority Fixes (Updated Order):
1. Sun/station slot overwrite (#1) — CRITICAL
2. Event handler leak (#20) — HIGH (memory leak)
3. Shield sync issue (#15) — MEDIUM
4. Ram mode orientation bug (#13) — MEDIUM
5. Reduce collision radius (#8) — MEDIUM

---

## Verified Correct Implementations

The following were verified as working correctly:

- ✅ Minsky per-component rotation in `ShipInstance.ApplyUniverseRotation()`
- ✅ TIDY orthonormalization in `OrientationMatrix.Tidy()`
- ✅ View matrix construction for 4 camera directions
- ✅ Laser damage calculation (shields → hull)
- ✅ Cargo drop spawning on ship destruction
- ✅ Save file checksum computation (CHECK and CHK2)

---

# Repo-wide Code Review (EliteRetro.Core)

**Date:** 2026-05-04  
**Reviewer:** GPT-5.2 (Cursor agent)  
**Scope:** `src/EliteRetro.Core` (entry/runtime loop, scenes, scheduling, local bubble, galaxy, save/load, rendering touchpoints)

## Executive Summary

The project has a clear “spine” (`Program` → `GameInstance` → `SceneManager` → active `GameScene`) and a convincing Elite-style simulation model (local bubble slots + MCNT frame-spread scheduling + rotating-universe math). Most issues are **correctness/robustness** (scene transitions and state ownership), plus a few **latent bugs** that will bite when features like variable timestep or more frequent scene creation are exercised.

## Confirmed High-Impact Issues

### A) **`GameInstance.ChangeScene()` does not change scenes (it pushes)**
**Severity: HIGH (behavior + memory/state growth)**

`GameInstance.ChangeScene(GameScene scene)` currently calls `SceneManager.PushScene(scene)` instead of `SceneManager.ChangeScene(...)`:
- This means menu selections like “START NEW GAME” / “SPACE VIEW” / “GALAXY MAP” *stack scenes* rather than replacing them.
- The name strongly suggests “replace stack”, but the implementation is “push”.
- Symptoms: unexpected back-stack behavior, accumulated scene instances, potential lingering event subscriptions/resources if `UnloadContent()` is incomplete.

**Evidence:** `src/EliteRetro.Core/GameInstance.cs` (lines ~203-211 in current file contents).

### B) **`MainLoopCounter.DecrementTimeBased()` cannot work as written**
**Severity: HIGH (bug; dead/incorrect utility)**

`MainLoopCounter` declares `_tickAccumulator` as `readonly` and never stores the updated accumulator back to a field. As a result:
- `DecrementTimeBased()` always starts from the default accumulator value, so it can only ever compute ticks for the current call and cannot accumulate remainder time.
- If you ever switch to variable timestep, MCNT-driven scheduling will drift or stall.

**Evidence:** `src/EliteRetro.Core/Systems/MainLoopCounter.cs` (fields + `DecrementTimeBased`).

### C) **Collision radii are scaled in a way that makes collisions “always on”**
**Severity: HIGH for gameplay feel, MEDIUM for correctness**

`CollisionSystem` uses:
- `BaseCollisionRadius = 200f`
- `sizeFactor = 1f + (vertexCount / 20f)`

For many models, this creates *kilounit* collision spheres (e.g., 200 vertices → \(200 * (1 + 10) = 2200\)). Given spawn distances in `FlightScene` (100–300), entities will collide immediately or constantly trip “close approach” logs.

**Evidence:** `src/EliteRetro.Core/Systems/CollisionSystem.cs` (radius functions + debug threshold).

### D) **`ShipInstance.FaceTarget()` can produce NaNs**
**Severity: MEDIUM (rare, but catastrophic when hit)**

`FaceTarget()` computes:

- `Sidev = Normalize(Cross(direction, UnitY))`

If `direction` is parallel/near-parallel to `UnitY`, the cross product approaches zero and normalization yields NaNs. This matches the prior review’s finding and remains present.

**Evidence:** `src/EliteRetro.Core/Entities/ShipInstance.cs` (`FaceTarget`).

## Confirmed “State Ownership” Smells

### E) Player energy/hull duplicated across `LocalBubbleManager` and `PlayerShip`
**Severity: MEDIUM (desync risk)**

`LocalBubbleManager` exposes `PlayerEnergy`, `PlayerShieldFront`, `PlayerShieldAft`, `PlayerHull`, but also creates a `PlayerShip` in a reserved slot with its own `Energy` and `Hull`. Current gameplay code (e.g., collision + HUD in `FlightScene`) mainly reads/writes `PlayerShip.Energy/Hull`, while `SaveGameManager` writes `bubble.PlayerEnergy` and `bubble.PlayerMissiles`.

This is a classic “two sources of truth” problem.

**Evidence:** `src/EliteRetro.Core/Managers/LocalBubbleManager.cs` (player fields + player ship creation), `src/EliteRetro.Core/Systems/SaveGameManager.cs`.

## Strengths Worth Keeping

- **Scene stack architecture is simple and effective**: `SceneManager` owns push/pop, Escape-pop behavior is centralized, and `GameScene` is a tight interface.
- **Galaxy generation is clean and deterministic**: `GalaxyGenerator` uses a seed that advances per system with explicit twist steps; the token-table approach is readable and faithful.
- **Wireframe rendering is thoughtfully engineered**: back-face culling + hidden-line dashing + outline/silhouette handling shows strong intent and good internal documentation linkage.

## Recommended Priority Order (repo-wide)

1. **Fix/rename scene transition API**: make `GameInstance.ChangeScene()` actually clear/replace, or rename it to `PushScene()` and update call sites to use stack semantics intentionally.
2. **Fix MCNT time-based decrement**: either remove `DecrementTimeBased()` until needed, or make it functional (store accumulator remainder).
3. **Rework collision radius calculation**: use model extents or a per-blueprint radius, and lower the default base radius dramatically.
4. **Resolve “player stats” single source of truth**: choose `PlayerShip` as authoritative (recommended) and have save/load/hud read from it, or keep manager fields but keep them synchronized in one place.
5. **Harden vector-basis construction** (`FaceTarget`, station orientation): add fallback “up” vector selection when direction ~ `UnitY`.

## Notes on the Existing Flight Review

The existing “Flight Systems Code Review” is broadly accurate and already calls out many key issues. The repo-wide additions above mainly cover:
- scene stack semantics (a systemic behavior issue),
- time-based MCNT bug (cross-cutting scheduling),
- collision radius scaling (systemic gameplay effect),
- and state ownership duplication (source-of-truth risk).
