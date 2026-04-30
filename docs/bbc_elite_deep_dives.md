---
name: BBC Elite Deep Dive References
description: Collection of BBC Elite technical deep dive articles used for implementation planning
type: reference
---

## Task Scheduling & Main Loop

- [Main loop counter task scheduling](https://elite.bbcelite.com/deep_dives/scheduling_tasks_with_the_main_loop_counter.html) — MCNT counter (0-255), modulo via AND masking, scheduled tasks (energy regen every 8, tactics every 8, TIDY every 16, spawn every 256)

## Docking

- [Docking checks](https://elite.bbcelite.com/deep_dives/docking_checks.html) — 5 geometric checks: friendliness, approach angle (nosev_z≤214), heading (z>0), safe cone (z≥89), slot horizontal (|roofv_x|≥80)
- [Docking computer](https://elite.bbcelite.com/deep_dives/the_docking_computer.html) — automated approach via fake keypress injection, 3 stages (approach, align, accelerate), intentionally imperfect

## Visual Effects

- [Explosion clouds](https://elite.bbcelite.com/deep_dives/drawing_explosion_clouds.html) — vertex-based particles, counter starts at 18 (+4/frame to 128 then shrinks), 4 stored random seeds for reproducible redraws
- [Stardust front view](https://elite.bbcelite.com/deep_dives/stardust_in_the_front_view.html) — 16-bit sign-magnitude coords, motion: q=64*speed/z_hi, roll/pitch transforms
- [Stardust side views](https://elite.bbcelite.com/deep_dives/stardust_in_the_side_views.html) — different transformation stages for side/rear views

## AI & Combat

- [Combat rank](https://elite.bbcelite.com/deep_dives/combat_rank.html) — 9 ranks from Harmless (0-7) to Elite (6400+), based on TALLY kill count
- [Crosshairs/targeting](https://elite.bbcelite.com/deep_dives/in_the_crosshairs.html) — HITCH routine: z_sign positive, x_hi=y_hi=0, distance² vs targetable area
- [Tactics routine flow](https://elite.bbcelite.com/deep_dives/program_flow_of_the_tactics_routine.html) — 7-part flow: energy recharge, targeting dot product, energy check, missile decision, laser firing, vector-based movement
- [NEWB flags](https://elite.bbcelite.com/deep_dives/advanced_tactics_with_the_newb_flags.html) — 8 personality bits (byte #37): trader, bounty hunter, hostile, pirate, docking, innocent, cop, scooped
- [Aggression & hostility](https://elite.bbcelite.com/deep_dives/aggression_and_hostility_in_ship_tactics.html) — aggression (0-63) controls turn probability, separate from hostility flag

## HUD & Save

- [3D scanner](https://elite.bbcelite.com/deep_dives/the_3d_scanner.html) — 138×36 ellipse at (124,220), ±63 range, dot+stick projection, IFF coloring
- [Dashboard indicators](https://elite.bbcelite.com/deep_dives/the_dashboard_indicators.html) — 11 bar indicators (16px each), DILX routine, shields/fuel/temp/altitude/speed/energy
- [Commander save files](https://elite.bbcelite.com/deep_dives/commander_save_files.html) — 256 bytes (75 used), binary layout with checksums (CHK, CHK2)
- [Competition code](https://elite.bbcelite.com/deep_dives/the_competition_code.html) — 4-byte encoded value: credit+rank+platform+tamper detection via XOR chain
