# Ship Data Implementation Plan

> **Source:** BBC Elite ship blueprint and data block specifications
>
> **Implementation:** C# (.NET 9.0), MonoGame (EliteRetro.Core)

---

## Overview

Elite's ships are defined by two complementary data systems:

1. **Ship Blueprints (XX21)** — Static model data: vertices, edges, faces, and characteristics stored in read-only data tables.
2. **Ship Data Blocks (K%)** — Runtime instance data: position, orientation, speed, energy, AI state stored in 36-byte blocks in the local bubble.

This document covers both systems and the ship specification data (speed, shields, weapons, etc.).

### Key Sources
- [Ship Blueprints](https://elite.bbcelite.com/deep_dives/ship_blueprints.html)
- [Comparing Ship Specifications](https://elite.bbcelite.com/deep_dives/comparing_ship_specifications.html)
- [Ship Data Blocks](https://elite.bbcelite.com/deep_dives/ship_data_blocks.html)
- [The Elusive Cougar](https://elite.bbcelite.com/deep_dives/the_elusive_cougar.html)

---

## 1. Ship Blueprint Structure (XX21)

### 1.1 Blueprint Lookup Table

XX21 contains addresses of all ship blueprints. Ship type 1 (Sidewinder) is first, up to type 13 (escape pod). Python is stored separately at SHIP_PYTHON above screen memory.

### 1.2 Ship Characteristics (First 20 Bytes)

| Byte Offset | Field | Description |
|-------------|-------|-------------|
| #0 | `max_cargo` | Max cargo canisters released when destroyed |
| #1-2 | `targetable_area` | 16-bit little-endian (e.g. 95×95 for Cobra Mk3) |
| #3 | `edges_offset_lo` | Edges data offset low byte (from byte #0) |
| #4 | `faces_offset_lo` | Faces data offset low byte (from byte #0) |
| #5 | `max_heap_size` | 1 + 4 × max visible edges |
| #6 | `laser_vertex` | Vertex number × 4 for laser position |
| #7 | `explosion_count` | 4n + 6 (n = explosion origin vertices) |
| #8 | `vertex_data_size` | Number of vertices × 6 |
| #9 | `edge_count` | Number of edges |
| #10-11 | `bounty` | Bounty in Cr × 10 (16-bit little-endian) |
| #12 | `face_data_size` | Number of faces × 4 |
| #13 | `visibility_distance` | Shows as dot beyond this distance |
| #14 | `max_energy` | Maximum energy/shields |
| #15 | `max_speed` | Maximum speed (1-44) |
| #16 | `edges_offset_hi` | Edges data offset high byte (can be negative offset) |
| #17 | `faces_offset_hi` | Faces data offset high byte |
| #18 | `normal_scale` | Face normals scaled down by 2^this value |
| #19 | `weapons_flags` | `%00lllmmm` — bits 0-2 = missiles, bits 3-5 = laser power |

### 1.3 Vertex Definitions (6 bytes each)

| Byte | Field | Description |
|------|-------|-------------|
| #0 | `x` | Magnitude of x-coordinate (origin in middle) |
| #1 | `y` | Magnitude of y-coordinate |
| #2 | `z` | Magnitude of z-coordinate |
| #3 | `signs_visibility` | `%xyzvvvvv` — bits 7-5 = sign of x,y,z; bits 0-4 = visibility distance |
| #4 | `faces_1_2` | `%ffffffff` — bits 0-3 = face 1 index, bits 4-7 = face 2 index |
| #5 | `faces_3_4` | `%ffffffff` — bits 0-3 = face 3 index, bits 4-7 = face 4 index |

Coordinates bounded to ±255 (8-bit magnitude + sign bit).

### 1.4 Edge Definitions (4 bytes each)

| Byte | Field | Description |
|------|-------|-------------|
| #0 | `visibility` | Visibility distance |
| #1 | `faces` | `%ffffffff` — bits 0-3 = face 1, bits 4-7 = face 2 |
| #2 | `start_vertex` | Vertex number at start |
| #3 | `end_vertex` | Vertex number at end |

### 1.5 Face Definitions (4 bytes each)

| Byte | Field | Description |
|------|-------|-------------|
| #0 | `visibility_flags` | `%xyzvvvvv` — bits 0-4 = visibility distance; face shown *beyond* this distance |
| #1 | `normal_x` | Magnitude of face normal x-component |
| #2 | `normal_y` | Magnitude of face normal y-component |
| #3 | `normal_z` | Magnitude of face normal z-component |

Face normals are scaled down by 2^`normal_scale` (byte #18 of characteristics).

---

## 2. Ship Data Blocks (K% / INWK)

Each ship instance in the local bubble occupies a 36-byte data block. Before processing, the block is copied into the INWK zero-page workspace for faster manipulation.

### 2.1 Byte Layout

| Byte Offset | Field | Size | Description |
|-------------|-------|------|-------------|
| #0-2 | `x_coord` | 3 | x-coordinate (x_sign x_hi x_lo), 24-bit sign-magnitude |
| #3-5 | `y_coord` | 3 | y-coordinate (y_sign y_hi y_lo) |
| #6-8 | `z_coord` | 3 | z-coordinate (z_sign z_hi z_lo) |
| #9-14 | `nosev` | 6 | Nose vector (x, y, z), 16-bit sign-magnitude each |
| #15-20 | `roofv` | 6 | Roof vector (x, y, z), 16-bit sign-magnitude each |
| #21-26 | `sidev` | 6 | Side vector (x, y, z), 16-bit sign-magnitude each |
| #27 | `speed` | 1 | Speed (range 1-40) |
| #28 | `acceleration` | 1 | Added to speed once in MVEIT, then zeroed |
| #29 | `roll_counter` | 1 | Bits 0-6: counter value; Bit 7: direction |
| #30 | `pitch_counter` | 1 | Bits 0-6: counter value; Bit 7: direction |
| #31 | `flags` | 1 | See below |
| #32 | `ai_status` | 1 | See below |
| #33-34 | `line_heap_ptr` | 2 | Ship line heap address pointer (INWK(34 33)) |
| #35 | `energy` | 1 | Energy level (0-255) |

**NES version:** Bytes #33-34 repurposed for scanner number and explosion cloud counter; 4 extra bytes (#37-40) for explosion cloud random seeds (40 bytes total).

### 2.2 Flags Byte (#31)

| Bit | Name | Description |
|-----|------|-------------|
| 0-2 | `missile_count` | Number of missiles carried |
| 3 | `draw_flag` | Set if ship should be drawn |
| 4 | `scanner_flag` | Set if ship appears on scanner |
| 5 | `exploding` | Set if ship is in explosion animation |
| 6 | `laser_drawn` | Set if laser beam or explosion cloud has been drawn |
| 7 | `killed` | Set if ship has been destroyed/removed |

### 2.3 AI/Status Byte (#32)

| Bit | Name | Description |
|-----|------|-------------|
| 0 | `ecm_active` | Set if E.C.M. system is active |
| 1-6 | `aggression` | Aggression level (0-63) |
| 7 | `ai_enabled` | Set if AI is active (clear for cargo/junk) |

### 2.4 Number Formats

**24-bit sign-magnitude (coordinates):**
- Sign stored in bit 7 of the sign byte (#0, #3, #6)
- Remaining 23 bits contain magnitude
- Example: x_sign=#0, x_hi=#1, x_lo=#2

**16-bit sign-magnitude (orientation vectors):**
- Sign stored in bit 7 of the high byte
- Unit length represented as 96 (0x6000)
- Example: nosev_x stored as two bytes (hi, lo) at #9-10

---

## 3. Ship Specifications

### 3.1 Ship Hardware (BBC Master Version)

| Ship | Laser | Missiles | Shields | Speed | Bounty | Target Area | Cargo | Explosion Count |
|------|-------|----------|---------|-------|--------|-------------|-------|----------------|
| Sidewinder | 2 | 0 | 70 | 37 | 5 | 65×65 | 0 | 6 |
| Viper | 2 | 1 | 140 | 32 | 0 | 75×75 | 0 | 9 |
| Cobra Mk III | 2 | 3 | 150 | 28 | 0 | 95×95 | 3 | 9 |
| Cobra Mk III (pirate) | 2 | 2 | 150 | 28 | 17.5 | 95×95 | 1 | 9 |
| Python | 3 | 3 | 250 | 20 | 0 | 80×80 | 5 | 9 |
| Python (pirate) | 3 | 3 | 250 | 20 | 20 | 80×80 | 2 | 9 |
| Anaconda | 7 | 7 | 252 | 14 | 0 | 100×100 | 7 | 10 |
| Adder | 2 | 0 | 85 | 24 | 4 | 50×50 | 0 | 4 |
| Asp Mk II | 5 | 1 | 150 | 40 | 20 | 60×60 | 0 | 5 |
| Fer-de-Lance | 2 | 2 | 160 | 30 | 0 | 40×40 | 0 | 5 |
| Mamba | 2 | 2 | 90 | 30 | 15 | 70×70 | 1 | 7 |
| Krait | 2 | 0 | 80 | 30 | 10 | 60×60 | 1 | 3 |
| Gecko | 2 | 0 | 70 | 30 | 5.5 | 99×99 | 0 | 5 |
| Boa | 3 | 4 | 250 | 24 | 0 | 70×70 | 5 | 8 |
| Cobra Mk I | 2 | 2 | 90 | 26 | 7.5 | 99×99 | 3 | 5 |
| Constrictor | 6 | 4 | 252 | 36 | 0 | 65×65 | 3 | 10 |
| Thargoid | 2 | 6 | 240 | 39 | 50 | 99×99 | 0 | 8 |
| Thargon | 2 | 0 | 20 | 30 | 5 | 40×40 | 0 | 3 |
| Missile | 0 | 0 | 2 | 44 | 0 | 40×40 | 0 | 1 |
| Escape Pod | 0 | 0 | 17 | 8 | 0 | 16×16 | 0 | 4 |
| Coriolis Station | 0 | 6 | 240 | 0 | 0 | 160×160 | 0 | 12 |
| Asteroid | 0 | 0 | 60 | 30 | 0.5 | 80×80 | 0 | 7 |
| Cargo Canister | 0 | 0 | 17 | 15 | 0 | 20×20 | 0 | 3 |

### 3.2 Ship Wireframes

| Ship | Max Edges | Vertices | Edges | Faces | Visibility Dist | Normal Scale | Kill Points |
|------|-----------|----------|-------|-------|-----------------|--------------|-------------|
| Sidewinder | 15 | 10 | 15 | 7 | 20 | 4 | 0.33 |
| Viper | 19 | 15 | 20 | 7 | 23 | 2 | 0.10 |
| Cobra Mk III | 38 | 28 | 38 | 13 | 50 | 2 | 0.91 |
| Python | 21 | 11 | 26 | 13 | 40 | 1 | 0.66 |
| Anaconda | 22 | 15 | 25 | 12 | 36 | 2 | 1.00 |
| Adder | 24 | 18 | 29 | 15 | 20 | 4 | 0.35 |
| Asp Mk II | 25 | 19 | 28 | 12 | 40 | 2 | 1.08 |
| Fer-de-Lance | 26 | 19 | 27 | 10 | 40 | 2 | 1.25 |
| Mamba | 23 | 25 | 28 | 5 | 25 | 4 | 0.50 |
| Escape Pod | 6 | 4 | 6 | 4 | 8 | 16 | 0.06 |
| Missile | 20 | 17 | 24 | 9 | 14 | 4 | 0.58 |
| Coriolis Station | 21 | 16 | 28 | 14 | 120 | 1 | — |
| Asteroid | 16 | 9 | 21 | 14 | 50 | 2 | 0.03 |
| Thargoid | 25 | 20 | 26 | 10 | 55 | 4 | 2.66 |

### 3.3 Spawning Behavior

| Ship | Junk | Pack Hunter | Bounty Hunter | Trader | Cop | Personality |
|------|------|-------------|---------------|--------|-----|-------------|
| Sidewinder | No | Yes | No | No | No | Hostile, Pirate |
| Viper | No | No | No | No | Yes | Bounty hunter, Cop |
| Cobra Mk III | No | No | No | Yes | No | Innocent |
| Cobra Mk III (pirate) | No | Yes | Yes | No | No | Hostile, Pirate |
| Python | No | No | No | Yes | No | Innocent |
| Anaconda | No | No | No | Yes | No | Trader, Innocent |
| Asp Mk II | No | No | Yes | No | No | Hostile, Pirate |
| Fer-de-Lance | No | No | Yes | No | No | Bounty hunter |
| Mamba | No | Yes | No | No | No | Hostile, Pirate |
| Thargoid | No | No | No | No | No | Hostile, Pirate |
| Asteroid | Yes | No | No | No | No | — |
| Cargo Canister | Yes | No | No | No | No | — |

---

## 4. Complete Ship Blueprint Data

Complete vertex, edge, and face data for all ships. Coordinates use sign-magnitude format: vertex signs encoded in byte bits 7-5, magnitudes are absolute values.

### 4.1 Sidewinder

**Characteristics:** Target area 65×65, Bounty 50, Max speed 37, Max energy 70, Laser 2, Missiles 0, Visibility 20, Explosion count 6, Normal scale 4

**Vertices (10):**
| # | x | y | z | Face1 | Face2 | Face3 | Face4 | Visibility |
|---|----|----|----|-------|-------|-------|-------|------------|
| 0 | 64 | -16 | -28 | 2 | 1 | 4 | 4 | 31 |
| 1 | -64 | -16 | -28 | 3 | 1 | 4 | 4 | 31 |
| 2 | -64 | 16 | -28 | 3 | 0 | 4 | 4 | 31 |
| 3 | 64 | 16 | -28 | 2 | 0 | 4 | 4 | 31 |
| 4 | 0 | 0 | 36 | 0 | 1 | 2 | 3 | 31 |
| 5 | 64 | 0 | -28 | 4 | 4 | 4 | 4 | 17 |
| 6 | -64 | 0 | -28 | 4 | 4 | 4 | 4 | 17 |
| 7 | 0 | 16 | -28 | 4 | 4 | 4 | 4 | 16 |
| 8 | 0 | -16 | -28 | 4 | 4 | 4 | 4 | 16 |
| 9 | 0 | 0 | -28 | 4 | 4 | 4 | 4 | 12 |

**Edges (15):**
| # | V1 | V2 | F1 | F2 | Vis |
|---|----|----|----|----|-----|
| 0 | 0 | 4 | 2 | 1 | 31 |
| 1 | 1 | 4 | 3 | 1 | 31 |
| 2 | 2 | 4 | 3 | 0 | 31 |
| 3 | 3 | 4 | 2 | 0 | 31 |
| 4 | 0 | 3 | 2 | 4 | 31 |
| 5 | 1 | 2 | 3 | 4 | 31 |
| 6 | 0 | 5 | 2 | 4 | 17 |
| 7 | 1 | 6 | 3 | 4 | 17 |
| 8 | 2 | 6 | 3 | 4 | 16 |
| 9 | 3 | 5 | 2 | 4 | 16 |
| 10 | 5 | 9 | 4 | 4 | 12 |
| 11 | 6 | 9 | 4 | 4 | 12 |
| 12 | 7 | 5 | 4 | 4 | 14 |
| 13 | 7 | 6 | 4 | 4 | 14 |
| 14 | 8 | 5 | 4 | 4 | 14 |

**Faces (7):**
| # | Nx | Ny | Nz | Vis |
|---|----|----|----|-----|
| 0 | 0 | 32 | 8 | 31 |
| 1 | 0 | -32 | 8 | 31 |
| 2 | 44 | 16 | 8 | 31 |
| 3 | -44 | 16 | 8 | 31 |
| 4 | 0 | 0 | -112 | 31 |
| 5 | 0 | 0 | -112 | 17 |
| 6 | 0 | 0 | -112 | 16 |

### 4.2 Viper

**Characteristics:** Target area 75×75, Bounty 0, Max speed 32, Max energy 140, Laser 2, Missiles 1, Visibility 23, Explosion count 9, Normal scale 2

**Vertices (15):**
| # | x | y | z | Face1 | Face2 | Face3 | Face4 | Visibility |
|---|----|----|----|-------|-------|-------|-------|------------|
| 0 | -10 | -4 | 47 | 3 | 0 | 5 | 4 | 31 |
| 1 | 10 | -4 | 47 | 1 | 0 | 3 | 2 | 31 |
| 2 | -16 | 8 | -23 | 5 | 0 | 7 | 6 | 31 |
| 3 | 16 | 8 | -23 | 1 | 0 | 8 | 7 | 31 |
| 4 | -66 | 0 | -3 | 5 | 4 | 6 | 6 | 31 |
| 5 | 66 | 0 | -3 | 2 | 1 | 8 | 8 | 31 |
| 6 | -20 | -14 | -23 | 4 | 3 | 7 | 6 | 31 |
| 7 | 20 | -14 | -23 | 3 | 2 | 8 | 7 | 31 |
| 8 | -8 | -6 | 33 | 3 | 3 | 3 | 3 | 16 |
| 9 | 8 | -6 | 33 | 3 | 3 | 3 | 3 | 17 |
| 10 | -8 | -13 | -16 | 3 | 3 | 3 | 3 | 16 |
| 11 | 8 | -13 | -16 | 3 | 3 | 3 | 3 | 17 |
| 12 | -16 | 0 | -23 | 6 | 6 | 6 | 6 | 16 |
| 13 | 16 | 0 | -23 | 6 | 6 | 6 | 6 | 16 |
| 14 | 0 | -14 | -23 | 6 | 6 | 6 | 6 | 12 |

**Edges (20):**
| # | V1 | V2 | F1 | F2 | Vis |
|---|----|----|----|----|-----|
| 0 | 0 | 1 | 3 | 0 | 31 |
| 1 | 1 | 5 | 2 | 1 | 31 |
| 2 | 5 | 3 | 8 | 1 | 31 |
| 3 | 3 | 2 | 7 | 0 | 31 |
| 4 | 2 | 4 | 6 | 5 | 31 |
| 5 | 4 | 0 | 5 | 4 | 31 |
| 6 | 5 | 7 | 8 | 2 | 31 |
| 7 | 7 | 6 | 7 | 3 | 31 |
| 8 | 6 | 4 | 6 | 4 | 31 |
| 9 | 0 | 2 | 5 | 0 | 29 |
| 10 | 1 | 3 | 1 | 0 | 30 |
| 11 | 0 | 6 | 4 | 3 | 29 |
| 12 | 1 | 7 | 3 | 2 | 30 |
| 13 | 2 | 6 | 7 | 6 | 20 |
| 14 | 3 | 7 | 8 | 7 | 20 |
| 15 | 8 | 10 | 3 | 3 | 16 |
| 16 | 9 | 11 | 3 | 3 | 17 |
| 17 | 12 | 14 | 6 | 6 | 12 |
| 18 | 13 | 14 | 6 | 6 | 12 |
| 19 | 12 | 13 | 6 | 6 | 16 |

**Faces (9):**
| # | Nx | Ny | Nz | Vis |
|---|----|----|----|-----|
| 0 | 0 | 31 | 5 | 31 |
| 1 | 4 | 45 | 8 | 31 |
| 2 | 25 | -108 | 19 | 31 |
| 3 | 0 | -84 | 12 | 31 |
| 4 | -25 | -108 | 19 | 31 |
| 5 | -4 | 45 | 8 | 31 |
| 6 | -88 | 16 | -214 | 31 |
| 7 | 0 | 0 | -187 | 31 |
| 8 | 88 | 16 | -214 | 31 |

### 4.3 Cobra Mk III

**Characteristics:** Target area 95×95, Bounty 0, Max speed 28, Max energy 150, Laser 2, Missiles 3, Visibility 50, Explosion count 9, Normal scale 2, Gun vertex 21

**Vertices (28):**
| # | x | y | z | Face1 | Face2 | Face3 | Face4 | Visibility |
|---|----|----|----|-------|-------|-------|-------|------------|
| 0 | 0 | 0 | 72 | 1 | 2 | 3 | 4 | 31 |
| 1 | 0 | 16 | 24 | 0 | 1 | 2 | 2 | 30 |
| 2 | 0 | -16 | 24 | 3 | 4 | 5 | 5 | 30 |
| 3 | 48 | 0 | -24 | 2 | 4 | 6 | 6 | 31 |
| 4 | -48 | 0 | -24 | 1 | 3 | 6 | 6 | 31 |
| 5 | 24 | -16 | -24 | 4 | 5 | 6 | 6 | 30 |
| 6 | -24 | -16 | -24 | 5 | 3 | 6 | 6 | 30 |
| 7 | 24 | 16 | -24 | 0 | 2 | 6 | 6 | 31 |
| 8 | -24 | 16 | -24 | 0 | 1 | 6 | 6 | 31 |
| 9 | -32 | 0 | -24 | 6 | 6 | 6 | 6 | 19 |
| 10 | 32 | 0 | -24 | 6 | 6 | 6 | 6 | 19 |
| 11 | 8 | 8 | -24 | 6 | 6 | 6 | 6 | 19 |
| 12 | -8 | 8 | -24 | 6 | 6 | 6 | 6 | 19 |
| 13 | -8 | -8 | -24 | 6 | 6 | 6 | 6 | 18 |
| 14 | 8 | -8 | -24 | 6 | 6 | 6 | 6 | 18 |
| 15 | -128 | 0 | -40 | 8 | 8 | 8 | 8 | 16 |
| 16 | 128 | 0 | -40 | 8 | 8 | 8 | 8 | 16 |
| 17 | -128 | 24 | -40 | 8 | 8 | 8 | 8 | 14 |
| 18 | 128 | 24 | -40 | 8 | 8 | 8 | 8 | 14 |
| 19 | -128 | -24 | -40 | 8 | 8 | 8 | 8 | 14 |
| 20 | 128 | -24 | -40 | 8 | 8 | 8 | 8 | 14 |
| 21 | 0 | 26 | -40 | 8 | 8 | 8 | 8 | 12 |
| 22 | 0 | -26 | -40 | 8 | 8 | 8 | 8 | 12 |
| 23 | -64 | 26 | -40 | 8 | 8 | 8 | 8 | 10 |
| 24 | 64 | 26 | -40 | 8 | 8 | 8 | 8 | 10 |
| 25 | -64 | -26 | -40 | 8 | 8 | 8 | 8 | 10 |
| 26 | 64 | -26 | -40 | 8 | 8 | 8 | 8 | 10 |
| 27 | 0 | 0 | -40 | 8 | 8 | 8 | 8 | 8 |

**Edges (38) and Faces (13):** Data available from BBC Elite archives.

### 4.4 Python

**Characteristics:** Target area 80×80, Bounty 0, Max speed 20, Max energy 250, Laser 3, Missiles 3, Visibility 40, Explosion count 9, Normal scale 1

**Vertices (11):**
| # | x | y | z | Face1 | Face2 | Face3 | Face4 | Visibility |
|---|----|----|----|-------|-------|-------|-------|------------|
| 0 | -32 | 0 | 36 | 0 | 1 | 4 | 5 | 31 |
| 1 | 32 | 0 | 36 | 0 | 2 | 5 | 6 | 31 |
| 2 | 64 | 0 | -28 | 2 | 3 | 6 | 6 | 31 |
| 3 | -64 | 0 | -28 | 1 | 3 | 4 | 4 | 31 |
| 4 | 0 | 16 | -28 | 0 | 1 | 2 | 3 | 31 |
| 5 | 0 | -16 | -28 | 3 | 4 | 5 | 6 | 31 |
| 6 | -12 | 6 | -28 | 3 | 3 | 3 | 3 | 15 |
| 7 | 12 | 6 | -28 | 3 | 3 | 3 | 3 | 15 |
| 8 | 12 | -6 | -28 | 3 | 3 | 3 | 3 | 12 |
| 9 | -12 | -6 | -28 | 3 | 3 | 3 | 3 | 12 |
| 10 | 0 | 0 | -28 | 3 | 3 | 3 | 3 | 8 |

**Edges (26) and Faces (13):** Data available from BBC Elite archives.

### 4.5 Anaconda

**Characteristics:** Target area 100×100, Bounty 0, Max speed 14, Max energy 252, Laser 7, Missiles 7, Visibility 36, Explosion count 10, Normal scale 2, Gun vertex 12

**Vertices (15):**
| # | x | y | z | Face1 | Face2 | Face3 | Face4 | Visibility |
|---|----|----|----|-------|-------|-------|-------|------------|
| 0 | 0 | 0 | 224 | 0 | 1 | 2 | 3 | 31 |
| 1 | 0 | 48 | 48 | 0 | 1 | 4 | 5 | 30 |
| 2 | 96 | 0 | -16 | 15 | 15 | 15 | 15 | 31 |
| 3 | -96 | 0 | -16 | 15 | 15 | 15 | 15 | 31 |
| 4 | 0 | 48 | -32 | 4 | 5 | 8 | 9 | 30 |
| 5 | 0 | 24 | -112 | 9 | 8 | 12 | 12 | 31 |
| 6 | -48 | 0 | -112 | 8 | 11 | 12 | 12 | 31 |
| 7 | 48 | 0 | -112 | 9 | 10 | 12 | 12 | 31 |
| 8 | 0 | -48 | 48 | 2 | 3 | 6 | 7 | 30 |
| 9 | 0 | -48 | -32 | 6 | 7 | 10 | 11 | 30 |
| 10 | 0 | -24 | -112 | 10 | 11 | 12 | 12 | 30 |
| 11 | 0 | 0 | -112 | 12 | 12 | 12 | 12 | 16 |
| 12 | 48 | 48 | -32 | 15 | 15 | 15 | 15 | 30 |
| 13 | -48 | 48 | -32 | 15 | 15 | 15 | 15 | 30 |
| 14 | 0 | 48 | -112 | 15 | 15 | 15 | 15 | 30 |

**Edges (25) and Faces (12):** Data available from BBC Elite archives.

### 4.6 Escape Pod

**Characteristics:** Target area 16×16, Bounty 0, Max speed 8, Max energy 17, Laser 0, Missiles 0, Visibility 8, Explosion count 4, Normal scale 16

**Vertices (4):**
| # | x | y | z | Face1 | Face2 | Face3 | Face4 | Visibility |
|---|----|----|----|-------|-------|-------|-------|------------|
| 0 | 24 | 16 | 0 | 0 | 1 | 5 | 5 | 31 |
| 1 | 24 | 5 | 15 | 0 | 1 | 2 | 2 | 31 |
| 2 | 24 | -13 | 9 | 0 | 2 | 3 | 3 | 31 |
| 3 | 24 | -13 | -9 | 0 | 3 | 4 | 4 | 31 |
| 4 | 24 | 5 | -15 | 0 | 4 | 5 | 5 | 31 |
| 5 | -24 | 16 | 0 | 1 | 5 | 6 | 6 | 31 |
| 6 | -24 | 5 | 15 | 1 | 2 | 6 | 6 | 31 |
| 7 | -24 | -13 | 9 | 2 | 3 | 6 | 6 | 31 |
| 8 | -24 | -13 | -9 | 3 | 4 | 6 | 6 | 31 |
| 9 | -24 | 5 | -15 | 4 | 5 | 6 | 6 | 31 |

**Edges (15):**
| # | V1 | V2 | F1 | F2 | Vis |
|---|----|----|----|----|-----|
| 0 | 0 | 1 | 0 | 1 | 31 |
| 1 | 1 | 2 | 0 | 2 | 31 |
| 2 | 2 | 3 | 0 | 3 | 31 |
| 3 | 3 | 4 | 0 | 4 | 31 |
| 4 | 0 | 4 | 0 | 5 | 31 |
| 5 | 0 | 5 | 1 | 5 | 31 |
| 6 | 1 | 6 | 1 | 2 | 31 |
| 7 | 2 | 7 | 2 | 3 | 31 |
| 8 | 3 | 8 | 3 | 4 | 31 |
| 9 | 4 | 9 | 4 | 5 | 31 |
| 10 | 5 | 6 | 1 | 6 | 31 |
| 11 | 6 | 7 | 2 | 6 | 31 |
| 12 | 7 | 8 | 3 | 6 | 31 |
| 13 | 8 | 9 | 4 | 6 | 31 |
| 14 | 9 | 5 | 5 | 6 | 31 |

**Faces (7):**
| # | Nx | Ny | Nz | Vis |
|---|----|----|----|-----|
| 0 | 96 | 0 | 0 | 31 |
| 1 | 0 | 41 | 30 | 31 |
| 2 | 0 | -18 | 48 | 31 |
| 3 | 0 | -51 | 0 | 31 |
| 4 | 0 | -18 | -48 | 31 |
| 5 | 0 | 41 | -30 | 31 |
| 6 | -96 | 0 | 0 | 31 |

### 4.7 Missile

**Characteristics:** Target area 40×40, Bounty 0, Max speed 44, Max energy 2, Laser 0, Missiles 0, Visibility 14, Explosion count 1, Normal scale 4

**Vertices (17):**
| # | x | y | z | Face1 | Face2 | Face3 | Face4 | Visibility |
|---|----|----|----|-------|-------|-------|-------|------------|
| 0 | 0 | 0 | 68 | 0 | 1 | 2 | 3 | 31 |
| 1 | 8 | -8 | 36 | 1 | 2 | 4 | 5 | 31 |
| 2 | 8 | 8 | 36 | 2 | 3 | 4 | 7 | 31 |
| 3 | -8 | 8 | 36 | 0 | 3 | 6 | 7 | 31 |
| 4 | -8 | -8 | 36 | 0 | 1 | 5 | 6 | 31 |
| 5 | 8 | 8 | -44 | 4 | 7 | 8 | 8 | 31 |
| 6 | 8 | -8 | -44 | 4 | 5 | 8 | 8 | 31 |
| 7 | -8 | -8 | -44 | 5 | 6 | 8 | 8 | 31 |
| 8 | -8 | 8 | -44 | 6 | 7 | 8 | 8 | 31 |
| 9 | 12 | 12 | -44 | 4 | 7 | 8 | 8 | 8 |
| 10 | 12 | -12 | -44 | 4 | 5 | 8 | 8 | 8 |
| 11 | -12 | -12 | -44 | 5 | 6 | 8 | 8 | 8 |
| 12 | -12 | 12 | -44 | 6 | 7 | 8 | 8 | 8 |
| 13 | -8 | 8 | -12 | 6 | 7 | 7 | 7 | 8 |
| 14 | -8 | -8 | -12 | 5 | 6 | 6 | 6 | 8 |
| 15 | 8 | 8 | -12 | 4 | 7 | 7 | 7 | 8 |
| 16 | 8 | -8 | -12 | 4 | 5 | 5 | 5 | 8 |

**Edges (24) and Faces (9):** Data available from BBC Elite archives.

### 4.8 Coriolis Station

**Characteristics:** Target area 160×160, Bounty 0, Max speed 0, Max energy 240, Laser 0, Missiles 6, Visibility 120, Explosion count 12, Normal scale 1

**Vertices (16):**
| # | x | y | z | Face1 | Face2 | Face3 | Face4 | Visibility |
|---|----|----|----|-------|-------|-------|-------|------------|
| 0 | 160 | 0 | 160 | 0 | 1 | 2 | 6 | 31 |
| 1 | 0 | 160 | 160 | 0 | 2 | 3 | 8 | 31 |
| 2 | -160 | 0 | 160 | 0 | 3 | 4 | 7 | 31 |
| 3 | 0 | -160 | 160 | 0 | 1 | 4 | 5 | 31 |
| 4 | 160 | -160 | 0 | 1 | 5 | 6 | 10 | 31 |
| 5 | 160 | 160 | 0 | 2 | 6 | 8 | 11 | 31 |
| 6 | -160 | 160 | 0 | 3 | 7 | 8 | 12 | 31 |
| 7 | -160 | -160 | 0 | 4 | 5 | 7 | 9 | 31 |
| 8 | 160 | 0 | -160 | 6 | 10 | 11 | 13 | 31 |
| 9 | 0 | 160 | -160 | 8 | 11 | 12 | 13 | 31 |
| 10 | -160 | 0 | -160 | 7 | 9 | 12 | 13 | 31 |
| 11 | 0 | -160 | -160 | 5 | 9 | 10 | 13 | 31 |
| 12 | 10 | -30 | 160 | 0 | 0 | 0 | 0 | 30 |
| 13 | 10 | 30 | 160 | 0 | 0 | 0 | 0 | 30 |
| 14 | -10 | 30 | 160 | 0 | 0 | 0 | 0 | 30 |
| 15 | -10 | -30 | 160 | 0 | 0 | 0 | 0 | 30 |

**Edges (28) and Faces (14):** Data available from BBC Elite archives.

### 4.9 Asteroid

**Characteristics:** Target area 80×80, Bounty 5, Max speed 30, Max energy 60, Laser 0, Missiles 0, Visibility 50, Explosion count 7, Normal scale 2

**Vertices (9):**
| # | x | y | z | Face1 | Face2 | Face3 | Face4 | Visibility |
|---|----|----|----|-------|-------|-------|-------|------------|
| 0 | 0 | 80 | 0 | 15 | 15 | 15 | 15 | 31 |
| 1 | -80 | -10 | 0 | 15 | 15 | 15 | 15 | 31 |
| 2 | 0 | -80 | 0 | 15 | 15 | 15 | 15 | 31 |
| 3 | 70 | -40 | 0 | 15 | 15 | 15 | 15 | 31 |
| 4 | 60 | 50 | 0 | 5 | 6 | 12 | 13 | 31 |
| 5 | 50 | 0 | 60 | 15 | 15 | 15 | 15 | 31 |
| 6 | -40 | 0 | 70 | 0 | 1 | 2 | 3 | 31 |
| 7 | 0 | 30 | -75 | 15 | 15 | 15 | 15 | 31 |
| 8 | 0 | -50 | -60 | 8 | 9 | 10 | 11 | 31 |

**Edges (21) and Faces (14):** Data available from BBC Elite archives.

---

## 5. The Cougar

The Cougar is an extremely rare easter egg ship:

- **Spawn rate:** ~0.011% (1 in 9,000 spawnings)
- **Scanner visibility:** Version-dependent — visible in 6502 Second Processor (cyan), cloaked in BBC Master and C64
- **Cloaking mechanism:** Result of ship type renumbering. When the Elite logo (type 32) was removed, the Cougar inherited type 32 but the `scacol` scanner color table was not updated, causing it to inherit the logo's "hidden" scanner behavior
- **Appearance:** Featured on BBC Master title screen, rotating above the prompt
- **Stats:** Laser 6, Missiles 4, Shields 252, Speed 40, Target Area 70×70, Cargo 3
- **Blueprint:** 19 vertices, 25 edges, 6 faces

---

## 6. Data Structures

### 6.1 ShipBlueprint (static data)

```csharp
public class ShipBlueprint
{
    public string Name { get; init; } = "";
    public int MaxCargo { get; init; }
    public int TargetableArea { get; init; }
    public int LaserVertex { get; init; }
    public int ExplosionCount { get; init; }
    public int Bounty { get; init; }          // Cr × 10
    public int VisibilityDistance { get; init; }
    public int MaxEnergy { get; init; }
    public int MaxSpeed { get; init; }
    public int NormalScale { get; init; }     // 2^N divisor for face normals
    public int MissileCount { get; init; }
    public int LaserPower { get; init; }

    public List<VertexDef> Vertices { get; init; } = new();
    public List<EdgeDef> Edges { get; init; } = new();
    public List<FaceDef> Faces { get; init; } = new();
}

public record struct VertexDef(
    byte X, byte Y, byte Z,     // Magnitudes
    bool SignX, bool SignY, bool SignZ,
    byte Visibility,
    byte Face1, byte Face2, byte Face3, byte Face4  // Face indices (0 = none)
);

public record struct EdgeDef(
    byte Visibility,
    byte Face1, byte Face2,
    byte StartVertex, byte EndVertex
);

public record struct FaceDef(
    byte Visibility,
    sbyte NormalX, sbyte NormalY, sbyte NormalZ
);
```

### 6.2 ShipInstance (runtime data)

```csharp
public class ShipInstance
{
    public ShipBlueprint Blueprint { get; }
    public int SlotIndex { get; set; }

    // Position (24-bit sign-magnitude → float for modern use)
    public Vector3 Position { get; set; }

    // Orientation vectors (16-bit sign-magnitude, unit = 96)
    public Vector3 Nosev { get; set; }
    public Vector3 Roofv { get; set; }
    public Vector3 Sidev { get; set; }

    // Movement
    public int Speed { get; set; }           // 1-40
    public int Acceleration { get; set; }    // One-shot, zeroed after use

    // Rotation counters
    public int RollCounter { get; set; }     // Bits 0-6: value, Bit 7: direction
    public int PitchCounter { get; set; }

    // State
    public int Energy { get; set; }
    public ShipFlags Flags { get; set; }
    public ShipAI AI { get; set; }
}

[Flags]
public enum ShipFlags : byte
{
    None = 0,
    ShouldDraw = 1 << 3,
    OnScanner = 1 << 4,
    Exploding = 1 << 5,
    LaserDrawn = 1 << 6,
    Killed = 1 << 7,
}

public struct ShipAI
{
    public bool ECMActive;
    public int Aggression;    // 0-63
    public bool Enabled;
}
```

### 6.3 Ship Type Enumeration

```csharp
public enum ShipType
{
    None = 0,
    Sidewinder = 1,
    Viper = 2,
    CobraMk3 = 3,
    Python = 4,
    Anaconda = 5,
    Adder = 6,
    AspMk2 = 7,
    FerDeLance = 8,
    Mamba = 9,
    Krait = 10,
    Gecko = 11,
    // ... extended types for enhanced versions
    EscapePod = 13,
    Missile = 14,
    Asteroid = 15,
    CargoCanister = 16,
    CoriolisStation = 17,
    Thargoid = 18,
    Thargon = 19,
    // Enhanced-only ships
    CobraMk1 = 20,
    Boa = 21,
    Constrictor = 22,
    Transporter = 23,
    Shuttle = 24,
    // Special
    Cougar = 32,  // Easter egg
}
```

---

## 7. Implementation Steps

### Phase 1: Blueprint Data
1. Create `ShipBlueprint`, `VertexDef`, `EdgeDef`, `FaceDef` types
2. Port all ship blueprints from BBC Elite data (vertices, edges, faces for each ship type)
3. Create `ShipBlueprintLoader` to load blueprints from embedded data
4. Verify against existing model classes (CobraMk3Model, SidewinderModel, etc.)

### Phase 2: Ship Instance Data
5. Create `ShipInstance` with 36-byte-equivalent fields
6. Implement 24-bit sign-magnitude coordinate conversion
7. Implement 16-bit sign-magnitude orientation vector conversion (unit = 96)
8. Implement flags and AI byte packing/unpacking

### Phase 3: Ship Specifications
9. Create `ShipSpecifications` record with all hardware stats (speed, shields, weapons, etc.)
10. Create specification data for all ship types from the comparison tables
11. Add spawning behavior data (personality flags, trader/pirate/cop tendencies)

### Phase 4: Integration
12. Connect blueprints to `LocalBubbleManager` slot system
13. Connect specifications to combat/trading AI
14. Add Cougar easter egg spawning (1 in 9,000 chance)
15. Implement scanner color mapping per ship type

---

## 8. Files to Create

| File | Purpose |
|------|---------|
| `src/EliteRetro.Core/Entities/ShipBlueprint.cs` | Blueprint data structures + all ship definitions |
| `src/EliteRetro.Core/Entities/ShipInstance.cs` | Runtime ship instance (36-byte equivalent) |
| `src/EliteRetro.Core/Entities/ShipSpecifications.cs` | Ship stats (speed, shields, weapons, spawning) |
| `src/EliteRetro.Core/Entities/ShipType.cs` | Ship type enumeration + metadata |

## 9. Files to Modify

| File | Changes |
|------|---------|
| `src/EliteRetro.Core/Entities/ShipModel.cs` | Merge with or reference new blueprint system |
| `src/EliteRetro.Core/Managers/LocalBubbleManager.cs` | Use ShipInstance for entity data |
| `src/EliteRetro.Core/GameConstants.cs` | Add ship-related constants |

---

## 10. Open Questions

1. **Complete edge/face data** — Some ships have partial edge/face data listed above. Full data for all 34 ships needs to be extracted from the individual ship pages on BBC Elite.
2. **Face normal vectors** — The original blueprints include pre-computed face normals. These need to be extracted or recomputed from vertex data.
3. **Enhanced version ships** — Many ships (Cobra Mk I, Boa, Constrictor, etc.) only appear in enhanced versions. Need to decide which versions to support.
4. **Cougar blueprint** — Full vertex/edge/face data needs extraction from the individual ship page.
5. **Color data** — Ship colors and scanner colors vary by platform version. Need to decide which palette to use as canonical.
