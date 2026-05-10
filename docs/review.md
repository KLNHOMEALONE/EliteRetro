# Elite Retro - Comprehensive Code Review

**Last Updated:** 2026-05-10  
**Reviewers:** MiniMax M2.7, GPT-5.2, Gemini CLI  

---

## 1. Executive Summary

The project has undergone significant architectural hardening. Core classes have been encapsulated, memory leaks have been plugged, and the simulation math has been made more robust. Transition to an interface-driven design (SOLID) is in progress, specifically with the implementation of the Strategy Pattern for AI.

---

## 2. SOLID & Architectural Analysis

### **S - Single Responsibility Principle (SRP)**
- **God Object Decomposition:** 
  - *FlightScene:* Still manages many concerns, but renderers are now handled via disposable interfaces.
  - *LocalBubbleManager:* Successfully decoupled. Player state and environmental effects (heat, fuel scoop) moved to `PlayerManager`; combat effects (energy bomb) moved to `CombatService`. **(DONE)**.
- **AI Strategy:** NPC tactics moved from the static `ShipAISystem` to pluggable `IAIBehavior` implementations. **(DONE)**.

### **O - Open/Closed Principle (OCP)**
- **NPC Logic:** Successfully refactored using the **Strategy Pattern**. New ship behaviors can now be added by implementing `IAIBehavior` without modifying `ShipAISystem`. **(DONE)**.

### **L - Liskov Substitution Principle (LSP)**
- **Scene Hierarchy:** Improvements to `IDisposable` in renderers ensure that scenes can be swapped more safely without leaking GPU resources.

### **I - Interface Segregation Principle (ISP)**
- **Manager Bloat:** `LocalBubbleManager` split is complete. It is now a pure entity container. **(DONE)**.

### **D - Dependency Inversion Principle (DIP)**
- **Concrete Dependencies:** Formal **Dependency Injection** implemented via `Microsoft.Extensions.DependencyInjection`. Scenes and systems are decoupled from concrete instances via `IGameContext` and constructor injection. **(DONE)**.

---

## 3. Critical Bugs & Regressions (STATUS)

### **3.1 SUN/STATION SLOT OVERWRITE** (Status: **VERIFIED FIXED**)
The logic in `InitializeBubble` and `SpawnStation` now correctly handles Sun/Station exclusivity with proper cleanup.

### **3.2 EVENT HANDLER LEAK** (Status: **VERIFIED FIXED**)
`FlightScene` now unsubscribes from `EntityEvent` and `CollisionEvent` in `UnloadContent`.

### **3.3 FACE TARGET NaN POTENTIAL** (Status: **VERIFIED FIXED**)
`ShipInstance.FaceTarget()` now includes a dot-product guard to prevent NaNs when looking at parallel axes.

### **3.4 PLAYER STATS DESYNC** (Status: **VERIFIED FIXED**)
`LocalBubbleManager` properties (Energy, Shields, Hull) now forward directly to the `PlayerShip` instance, ensuring a single source of truth.

### **3.5 GPU MEMORY LEAKS** (Status: **VERIFIED FIXED**)
All renderers now implement `IDisposable` and are properly released when scenes are unloaded. problematic `new Renderer()` calls in draw loops have been eliminated.

---

## 4. Gameplay & Simulation Correctness

### **4.1 ENORMOUS COLLISION RADII** (Status: **FIXED**)
Collision radii now derive from model bounding radius.

### **4.2 LASER TARGETING MISMATCH** (Status: **FIXED**)
Targeting math is now consistent across simulation and rendering spaces.

### **4.3 SPEED CONTROL NON-LINEARITY** (Status: **FIXED**)
SpeedDelta in `FlightControlService` is now applied as a per-second rate.

---

## 5. Refactoring Roadmap (Remaining)

| Priority | Task | Target Class | Status |
|----------|------|--------------|--------|
| 1 | Decouple Player State from World Manager | `LocalBubbleManager` | **DONE** |
| 2 | Extract Input to a dedicated service | `FlightScene` | **DONE** |
| 3 | Implement formal Dependency Injection | `GameInstance` | **DONE** |
| 4 | Add Unit Tests for `OrientationMatrix` | `Tests` | **DONE** |

---

## 6. Verification Checklist

- [x] Memory: No growth in GPU memory after 10 space/menu swaps.
- [x] Physics: Sun and Station swap correctly in safe zone.
- [x] Math: `FaceTarget` does not return NaNs for vertical targets.
- [x] Encapsulation: `ShipInstance` state is protected by properties.

---

## 7. UI & Aesthetic Fidelity Review (vs HUD3.jpg)

**Status:** IN PROGRESS

### **7.1 Cross-hair Accuracy**
- **Observation:** Current gap (`inner = 16`) is too wide. Reference shows a tight, segmented cross-hair.
- **Goal:** Reduce `inner` to 4-6px and add 1-2 tick marks per bar.

### **7.2 View Layout & Framing**
- **Observation:** Original Elite uses double-line white borders for the 3D view and instrument panel.
- **Goal:** Implement double-line borders in `DrawFrame`.
- **Overlay:** "Front view" text should be centered at the top of the 3D area, not left-aligned.

### **7.3 Instrument Panel (HUD)**
- **Observation:** Compass should be a small circular "dot-in-circle" display next to the scanner.
- **Goal:** Replace/augment the grid compass with a circular variant.
- **Proportions:** Standard Elite uses 1/3 (33.3%) height for the HUD. Current is 37.5%.
- **Goal:** Re-tune `HudHeightFraction` to `0.333f`.

---

## 8. UI Fidelity Review (vs HUDFromLegend.png)

**Status:** IN PROGRESS  
**Reference Image:** `HUDFromLegend.png` (High-Fidelity)

This review is based on a new, clearer reference image, superseding the analysis from `HUD3.jpg`.

### **8.1 View Framing & Colors**
- **Observation:** The reference uses single-line borders. The 3D view is framed in **green**, while the HUD panel is framed in white. "Front View" text is also green.
- **Goal:**
    - Modify `DrawFrame` to render single-line borders.
    - Set the 3D view frame and "Front View" text color to green.

### **8.2 Cross-hair**
- **Observation:** The reference shows a circular green targeting reticle for combat. Our current cross-hair is a white, non-combat version.
- **Goal (Immediate):** Change the color of the existing cross-hair to green to match the overall aesthetic.
- **Goal (Future):** Implement a dynamic system to switch to a circular targeting reticle when an enemy is targeted.

### **8.3 HUD Proportions**
- **Observation:** The HUD panel appears to be slightly smaller than 1/3 of the screen, closer to 28-30%.
- **Goal:** Fine-tune `HudHeightFraction` to `0.28f` for better accuracy.

### **8.4 Instrument Panel Details**
- **Scanner Grid:** The internal grid of the elliptical scanner should be a **'Y' shape**, not the current 'W' shape.
- **New Indicators:**
    - A **solid green circle** (likely for active fuel scoop).
    - A **yellow ship icon** (likely for cargo).
    - A **small green arrow** next to the circular compass.
- **Goal:** Implement the 'Y' shape scanner grid and add the missing HUD indicators.
