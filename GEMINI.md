# Project Instructions: EliteRetro

## Architectural Principles
- **Authenticity First:** The project prioritizes bit-level and visual fidelity to the original BBC Elite. Mathematical complexity in rendering or simulation must never be simplified for "cleanliness" unless specifically requested.
- **Surgical Refactoring:** Structural changes must be isolated and minimal. Do not combine unrelated architectural patterns (e.g., Strategy Pattern + Property Encapsulation) in a single turn.

## Mandatory Refactoring Workflow
1. **Research & Map:** Identify the target logic and its side effects.
2. **Implementation Plan:** Present a concise plan describing:
    - Exactly which lines will change.
    - Which simulation/visual logic MUST remain untouched.
    - How correctness will be verified.
3. **Approval Gate:** **Wait for explicit user approval** before modifying any file.
4. **Surgical Act:** Apply only the approved changes.
5. **Verify:** Run a full build and perform a logic audit immediately after the edit.

## Simulation Integrity Rules
- **No Simplified Math:** Renderers (Sun, Planet, Scanner, Rings) contain complex, tuned logic. Never refactor these into generic "Circle/Ellipse" calls without preserving the original specialized algorithms.
- **C# Property Safety:** When converting fields to properties, always use the pattern: `Vector3 pos = entity.Position; pos.X = ...; entity.Position = pos;`. Never assume direct mutation of struct properties works.
- **Source of Truth:** Player vitals (Energy, Hull) must have a single authoritative source. Ensure HUD, Collision, and Save systems are audited for desync after any state change.

## Verification Requirements
- All structural changes require a full project build (`dotnet build`).
- Any change to simulation math requires a line-by-line logic comparison against the `origin/master` state.
