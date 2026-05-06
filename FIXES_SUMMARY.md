# EliteRetro Critical Fixes Summary

## Overview
This document summarizes all critical bugs fixed in the EliteRetro codebase based on the comprehensive review in `.kilo/plans/1778094541448-sunny-lagoon.md`.

## Fixes Implemented

### 1. MarketSystem Exploits (NE-1, NE-2, NE-3)
**File:** `src/EliteRetro.Core/Systems/MarketSystem.cs`

**NE-1: Negative Price Exploit**  
- Prices could go negative for commodities with negative `EconomyPriceFactor` (Food, Textiles, Narcotics, Luxuries) at high economy levels
- Fix: Clamp price to minimum 1 before calculation (line 130-131)

**NE-2: Availability Wrap-Around**  
- Used bitwise AND (`& 63`) on signed intermediate values; negative values wrap to high availability (e.g., `-1 & 63 = 63`)
- Fix: Use proper unsigned modulo: `(qty % 64 + 64) % 64` (line 146)

**NE-3: Buy/Sell No Price Validation**  
- No validation of prices at runtime; negative prices allowed trading exploits
- Fix: Added price validation in both `Buy()` and `Sell()` methods (lines 160-161, 179-180)

### 2. TaskScheduler Exception Handling (NE-4)
**File:** `src/EliteRetro.Core/Systems/TaskScheduler.cs`

- No try/catch around registered task actions; one failing task stops all subsequent tasks for the frame
- Fix: Wrap each task in try/catch with debug logging, continue processing remaining tasks (lines 52-62, 68-78)

### 3. CollisionSystem Planet Crash Scaling (NE-5)
**File:** `src/EliteRetro.Core/Systems/CollisionSystem.cs`

- Used hardcoded `0.0001f` scale on `PlanetRadius` (24576), yielding ~2.45 unit crash radius vs spawn distance of 100-300 units
- Fix: Use consistent Elite internal coordinate system; remove ad-hoc scaling factor (lines 296-300, 310-314)

### 4. DockingSystem Approach Angle Inverted (NE-10)
**File:** `src/EliteRetro.Core/Systems/DockingSystem.cs`

- `CheckApproachAngle` returned true when `fixedDot <= 214` (angle > 33°), backwards logic
- Fix: Changed to `fixedDot >= ApproachAngleThreshold` requiring ship to face station (line 86-89)
- Also added `GetPersonalityFlags()` helper for consistency with ShipAISystem
- Updated `CheckFriendliness()` to use proper personality mapping

### 5. ShipAISystem ShipClass→NewbFlags Type Confusion (NE-12)
**File:** `src/EliteRetro.Core/Systems/ShipAISystem.cs`

- Cast `ShipClass` (byte: 0=Innocent,1=BountyHunter,2=Pirate,3=Cop) directly to `NewbFlags` enum (Pirate=8, Cop=64)
- Fix: Added `GetPersonalityFlags()` method to properly map ShipClass values to correct NewbFlags
- Updated `FindTarget()`, `IsHostileToward()`, `CalculateMovementDirection()` to use new mapping

### 6. StardustRenderer Screen Bounds (NE-20)
**File:** `src/EliteRetro.Core/Rendering/StardustRenderer.cs`

- Checks `screenX > center.X + 1024 + 10` but center is at 512 (for 1024-wide screen)
- Fix: Changed to `screenX > center.X + 512 + 10` and `screenY > center.Y + 384 + 10` (lines 158-159)

### 7. WireframeRenderer O(E×V×F) Hot Path Optimization
**File:** `src/EliteRetro.Core/Rendering/WireframeRenderer.cs`

- Allocated `edgeFaces` and `faceVertexSets` arrays every frame
- Edge-face matching used nested loops O(E×V×F) with LINQ allocations
- Fix: 
  - Added `GetEdgeFaces()` and `GetFaceVertexSets()` caching methods to `ShipModel` (compile once per model)
  - Replaced LINQ `Any()` and `Count()` with manual loops to avoid enumerator allocations
  - Use cached adjacency lists instead of rebuilding each frame (lines 199-200, 228-237, 252-265)

### 8. Renderer Resource Leak Fix (IDisposable Implementation)
**Files:** All renderer classes

- Every renderer created `Texture2D` in constructor, never disposed
- None implemented `IDisposable`; scene transitions leaked GPU resources
- Fix: Added `IDisposable` pattern to all renderers:
  - `WireframeRenderer` (with `_lineTexture` disposal)
  - `CircleRenderer` 
  - `EllipseRenderer`
  - `SunRenderer`
  - `StardustRenderer`
  - `RingRenderer`
  - `ScannerRenderer`
  - `ExplosionRenderer`
  - `HudRenderer`
- Each now has proper dispose pattern with `_isDisposed` flag, `Dispose()` method, and finalizer
- Texture disposal in `Dispose(bool disposing)` method

## Additional Improvements

### ShipModel Caching
- Added `_edgeFacesCache` and `_faceVertexSetsCache` fields to `ShipModel`
- Lazy-initialized on first access, reused for all instances of same model
- Eliminates per-frame allocations for static model topology

## Verification

All fixes have been applied and the solution builds successfully:
- `EliteRetro.Core` project: Builds without errors (only pre-existing CA1416 warnings)
- `EliteRetro.DesktopGL` project: Builds without errors
- Full solution: Build succeeded

## Priority Order (As Implemented)

1. **Critical Exploits** — MarketSystem (NE-1, NE-2, NE-3)
2. **TaskScheduler** — Exception handling (NE-4)
3. **Gameplay Bugs** — Planet crash (NE-5), Docking angle (NE-10), ShipAISystem flags (NE-12)
4. **Resource Leaks** — IDisposable on all renderers, StardustRenderer bounds (NE-20)
5. **Performance** — WireframeRenderer caching (NE-12 optimization)

## Lines Changed Per File

- `MarketSystem.cs`: ~5 lines modified (price clamp, modulo fix, buy/sell validation)
- `TaskScheduler.cs`: ~16 lines modified (try/catch wrappers)
- `CollisionSystem.cs`: ~6 lines modified (removed scaling factors)
- `DockingSystem.cs`: ~20 lines modified (angle fix, GetPersonalityFlags, CheckFriendliness)
- `ShipAISystem.cs`: ~25 lines modified (GetPersonalityFlags, 3 usage sites)
- `StardustRenderer.cs`: ~4 lines modified (screen bounds) + IDisposable (~20 lines)
- `WireframeRenderer.cs`: ~15 lines modified (caching, manual loops) + IDisposable (~20 lines)
- `ShipModel.cs`: ~40 lines added (caching properties)
- Renderer classes: ~15-20 lines each for IDisposable pattern

## Testing Recommendations

1. Test market trading with various economy levels (verify no negative prices)
2. Visit edge-case systems (high economy with low base price commodities)
3. Test crash landing scenarios at various distances from planet
4. Verify docking works with proper approach angles
5. Observe AI ship behavior (should now correctly identify friendlies/hostiles)
6. Monitor memory usage over multiple scene transitions (should not grow)
7. Profile frame time in combat scene (reduced GC from WireframeRenderer)
