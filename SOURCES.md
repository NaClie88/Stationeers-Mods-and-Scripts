# Sources — IC10 Fail-Safe Airlock Design

Every non-obvious claim in `ic10_failsafe_airlock_requirements.md` and
`ic10_airlock_code_notes.md` traces back to one of these. Organized
by topic so you can drop the relevant subset into a GitHub repo's
CREDITS/SOURCES file without hunting back through chat history.

## IC10 language fundamentals
- Community Wiki, "Integrated Circuit (IC10)" — https://stationeers-wiki.com/Integrated_Circuit_(IC10)
- Community Wiki, "IC10" — https://stationeers-wiki.com/IC10
- XGamingServer, "Stationeers IC10 Programming: A Beginner's Guide" — https://xgamingserver.com/blog/stationeers-ic10-programming-guide/
- GitHub, SnorreSelmer/stationeers_ic10, "mips-programming-101.md" — https://github.com/SnorreSelmer/stationeers_ic10/blob/main/mips-programming-101.md
  (128 lines / 90 chars claim — conflicts with the entry below)
- GitHub, jhillacre/stationeers-scripts — https://github.com/jhillacre/stationeers-scripts
  (128 lines / 52 chars claim — conflicts with the entry above; the 52-char figure is phrased as "to respect the in-game editor" in this repo's own README, reads more like a self-imposed style choice than a documented hard limit — checklist item 10 now leans 90, still not 100% confirmed since the wiki's primary source is blocked by Cloudflare bot-protection)
- StationeersLua Docs, "Enumerations & Constants" — https://orbitalfoundrymodteam.github.io/StationeersLuaDocs/guide/enums-constants.html
  (community-maintained LogicType/LogicSlotType reference; used to cross-check `Charge`, `Maximum`, `Ratio` existence)
- Search-aggregated result, `sbn` instruction signature — confirms `sbn prefabHash nameHash LogicType value`, example `sbn LEDHASH HASH("L1") Setting 1`; resolved the Chip C addressing bug described in the prototype-code doc
- Search-aggregated result, `lbn` instruction signature — confirms `lbn targetRegister prefabHash nameHash LogicType batchMode`, example `lbn r0 HASH("StructureGasSensor") HASH("Sensor 1") Temperature Average`; both `lbn`/`sbn` are described as letting a build "bypass the 6-pin limit on IC housing device assignments" — the basis for moving all three airlock Buttons off pins in Chip B (2026-08-04)
- Search-aggregated result, `HASH()` macro and Logic Switch structure hash — confirms batch instructions address devices via `HASH("PrefabName")` or a hash copied from Stationpedia; one example gives `define SwitchStructure -1591419276` for a switch structure's type hash, used as `BtnHash` in Chip B — **found from a single search result, not cross-confirmed against a second source or the wiki directly (blocked by Cloudflare)**, flagged in the prototype doc as a lead to verify against your own Stationpedia rather than a certainty
- GitHub, Zappes/Stationeers — https://github.com/Zappes/Stationeers
  (notes in-game Workshop script publishing is broken, why community code lives on GitHub instead)
- GitHub, drclaw1188/stationeers_ic10 — https://github.com/drclaw1188/stationeers_ic10

## Airlock circuitboards & lock/power mechanics
- Community Wiki, "Circuitboard (Advanced Airlock)" — https://stationeers-wiki.com/Circuitboard_(Advanced_Airlock)
  (lock persists through power loss — the core mechanic this whole design responds to)
- Community Wiki, "Blast Doors" — https://stationeers-wiki.com/Blast_Doors
  (same lockout behavior confirmed on Blast Doors; 25W/tick continuous draw)
- Community Wiki, "Crowbar" — https://stationeers-wiki.com/Crowbar
  (unlocked+unpowered requirement for manual operation)
- Community Wiki, "Custom Airlock IC10" — https://stationeers-wiki.com/Custom_Airlock_IC10
  (reference working script — cycling phases, LED-as-trigger pattern)
- Community Wiki, "Guide (Airlock) Atmosphere to Atmosphere" — https://stationeers-wiki.com/Guide_(Airlock)_Atmosphere_to_Atmosphere
  (Stalled phase, "Cancel Pressurize" button — confirms Stalled is real, not hypothetical)

## Power devices
- Community Wiki, "Power Controller" / "Area Power Controller" — https://stationeers-wiki.com/Power_Controller
  (battery-buffering/UPS behavior, Data Network properties)
- Community Wiki, "Logic Switch" — https://stationeers-wiki.com/Logic_Switch
  (Button/Switch functions fully unpowered — only the indicator light needs power)
- Community Wiki, "Active Vent" — https://stationeers-wiki.com/Active_Vent
  (100W confirmed draw, PressureExternal/Internal behavior)
- Community Wiki, "Powered Vent" — https://stationeers-wiki.com/Powered_Vent
  (2x/4x pressure throughput vs standard, no internal pressure limiter, scavenging behavior below ~20kPa; live page blocked automated fetch all session, resolved 2026-08-04 when the project owner supplied a local saved copy of the page — infobox confirms **Powered Vent 250W, Powered Vent Large 500W** (not "Large Powered Vent" — word order was backwards throughout this project until this fix), and the in-game Stationpedia description explicitly frames both as multi-grid/hangar-scale tools, not for a single self-contained chamber airlock like this project's)
- Steam Community, official patch notes discussion, "Update v0.2.4294.19984" — https://stationeers-wiki.com/Update_v0.2.4294.19984
  (active vent nonlinear pull-rate taper-off, confirmed)
- Gist, Twipped/77bf1bcdaa74a9bad404f937e0f40cf0d, "Stationeers Power Controller IC10 Script" — https://gist.github.com/Twipped/77bf1bcdaa74a9bad404f937e0f40cf0
  (real working script confirming Power Controller exposes `Charge` and `Maximum`, Joules, ratio computed manually via division — not a direct `Ratio`/`ChargeRatio` field; resolves requirements-doc checklist item 2)
  **SUPERSEDED, 2026-08-05, by direct decompilation of `Assembly-CSharp.dll`
  (`AreaPowerControl.GetLogicValue`)** — this gist's script apparently
  worked (or wasn't caught) because `Charge` on Power Controller isn't
  the battery's own stored charge, it's `AvailablePower` = live input-
  network load *plus* stored battery charge combined. `Ratio` IS
  directly exposed on Power Controller after all (`Battery.PowerStored
  / Battery.PowerMaximum`, confirmed legally readable via
  `CanLogicRead`'s own range check against real enum ordinals) — this
  gist's div-by-Maximum approach is a working-but-imprecise substitute,
  not the ground truth. See `ic10-airlock/ic10_airlock_code_notes.md`'s
  Watcher section for the full trace and the likely resulting bug in
  `watcher.ic10`. Kept here for the correction trail, not deleted.
- Community Wiki (community-derived enum reference), StationeersLua Docs "Enumerations & Constants" — https://orbitalfoundrymodteam.github.io/StationeersLuaDocs/guide/enums-constants.html
  (LogicType table cross-checked: `Charge`/`Maximum` confirmed, generic `Ratio` confirmed to exist as a LogicType but not shown tied to Power Controller specifically, no `Lock` entry found on this particular page — see the Steam Community "Question about locking" entry below for that confirmation instead)
- Steam Community, "Question about locking" — https://steamcommunity.com/app/544550/discussions/0/1729828401685627356/
  (confirms `Lock` is a real LogicType — a plain bit, 0 = unlocked, 1 = locked)
- StationeersLua Docs, "Airlock Controller" example — https://orbitalfoundrymodteam.github.io/StationeersLuaDocs/examples/airlock.html
  (confirms `On` and `Mode` as the vent-control pair — Mode 0 = outward/depressurize, 1 = inward/pressurize)
- Search-aggregated result, Composite Door power draw — 10W/tick confirmed via community wiki "Composite Door" / "Power" pages (direct fetch blocked by Cloudflare bot-protection; figure surfaced consistently across independent search snippets, not independently re-verified against the raw page)

## Sensors
- Search-aggregated result, Gas Sensor LogicTypes — https://stationeers-wiki.com/Gas_Sensor
  (confirms `Pressure`, `Temperature`, and per-gas `RatioX` fields — `RatioOxygen`, `RatioCarbonDioxide`, `RatioNitrogen`, `RatioPollutant`, `RatioMethane`, `RatioNitrousOxide`, `RatioHydrogen`, `RatioWater`, `RatioPollutedWater`, `RatioHydrazine`, `RatioLiquidAlcohol`, `RatioHelium`, `RatioSilanol`, `RatioHydrochloricAcid`, `RatioOzone`, `RatioLiquidOzone` — no single generic "Ratio" field for composition, resolving requirements-doc checklist item 8 and fixing a real bug in Chip C's earlier skeleton; direct fetch blocked by Cloudflare, figure set built from consistent independent search snippets)

## Storms & structural
- Community Wiki, "Storm" — https://stationeers-wiki.com/Storm
  (destructible items list, 1.5kW/panel solar storm boost, 1200-cube room cap, repair methods)

## Mods
- GitHub, Sukasa/ReVolt — https://github.com/Sukasa/ReVolt
  (Re-Volt mod feature list: Circuit Breakers, Cable Tray, Load Centers, Modular Batteries)
- Steam Workshop, Re-Volt — https://steamcommunity.com/sharedfiles/filedetails/?id=3587239682

## Steam Workshop scripts referenced (for cycling logic patterns, not directly quoted)
- "Emergency Bulkhead Lockdown" — https://steamcommunity.com/sharedfiles/filedetails/?id=2258102536
- "Airlock Control" — https://steamcommunity.com/sharedfiles/filedetails/?id=1524868713
  (confirms emergency-override-switch pattern — optional d5 lever)
- "Custom Airlock V2" by CowsAreEvil — https://steamcommunity.com/sharedfiles/filedetails/?id=2978749569
  (full source inspected directly — Propped-Open behavior already implemented and working in production; confirmed `brdns` instruction, optional-button batch-defaults-to-zero pattern, real LogicType names, real match-tolerance values; author is the same "Cows Are Evil" cited elsewhere in this project for Venus playthrough automation)
- "Adaptive Airlock" — https://steamcommunity.com/sharedfiles/filedetails/?id=2194510353
  (independently confirms emergency-mode manual-override-opening pattern)

## Community troubleshooting threads (specific gotchas)
- Steam Community, "Every update breaks IC10?" — https://steamcommunity.com/app/544550/discussions/0/4751948599934992260
  (stack persistence across reloads/restarts — confirmed real bug source)
- Steam Community, "Adaptive Airlock" comments — https://steamcommunity.com/sharedfiles/filedetails/comments/2194510353
  (`dr##`-style invalid register syntax confirmed as a real error — use `alias` instead)
- Steam Community, "Unlock (not open) airlock door when the power is cut" (Suggestions) — https://steamcommunity.com/app/544550/discussions/3/1621724915791890334/
  (dedicated Power Controller + swappable battery as the community-standard workaround)
- Steam Community, "logic transmitters with ic chip tutorials?" — https://steamcommunity.com/app/544550/discussions/0/669472753009635911/
  (multi-chip coordination via shared device state / Logic Transmitter pattern)

## Transformer bug fix and the Watcher/Cycle chip split (2026-08-04)
- Search-aggregated result, Transformer wiki page — confirms, verbatim, "Data will not flow through a transformer" — it's a passive wattage-cap device with no data port at all, disproving the earlier design's assumption that `s XfmrExt On 1/0` would work
- Steam Community discussion, power control options — confirms there's no dedicated breaker component in vanilla Stationeers and the Power Controller/APC is what the community actually uses to gate a circuit via logic instead
- Search-aggregated result, `lbn` instruction signature — confirms `lbn targetRegister prefabHash nameHash LogicType batchMode`, example `lbn r0 HASH("StructureGasSensor") HASH("Sensor 1") Temperature Average` — used for the Watcher chip's button reads (already sourced above under `sbn`, same family)
- Search-aggregated result, Logic Transmitter/Receiver channel usage — **SUPERSEDED, see the 2026-08-05 entry below.** Originally cited to confirm `s <alias> Channel0 r0` write / `l r <alias> Channel0` read and Channel0–Channel7 as eight parallel value slots per matched pair. This turned out to be wrong for the actual "Logic Transmitter" device — kept here for the correction trail, not as a current fact.
- Search-aggregated result, Power Controller gating LogicType — **not resolved by research.** Every source pointed back to "check Stationpedia in-game" without giving the specific field name; `On` is used in the prototype code as a strong inference (matches every other powered device confirmed in this project) but is explicitly flagged unconfirmed, pending the project owner's own in-game check

## In-game confirmations by project owner (not web sources)
- **Powered Vent Large power draw: 500W** — confirmed directly in-game, 2026-08-04 (note: naming corrected from "Large Powered Vent" — see Community Wiki "Powered Vent" entry above, which independently matches this figure via a locally-saved copy of the page and additionally supplies the previously-unconfirmed 250W figure for the smaller Powered Vent tier). Resolves requirements-doc checklist item 9, which no web source had published.
- **IC10 line-length limit — both 52 and 90 are correct, for different things.** 52 characters is the in-game editor's *typing* limit (a UI constraint); 90 characters is the actual execution/storage limit — pasting a line up to 90 chars works even though typing past 52 by hand is blocked. Resolves checklist item 10 and explains why community sources split down the middle on this figure.
- **Hysteresis gap: 3% confirmed as a reasonable starting value** — matches the 90%/93% and 10%/13% bands already used in Chip A.
- **Chamber footprint: 1–2 grid volumes for the chamber, +1 grid spillover for pressure tanks/cycle-air hardware** — planning figure for checklist item 7.
- **Deep Idle cycle-latency target: under 0.25ms** from wake trigger to Transformer/Portal responsiveness — a design target for checklist item 4, not yet a measured result; still needs an in-game stopwatch check once built.
- **Neither Light variant exposes a `Setting` LogicType — confirmed via in-game Logic panel screenshots, 2026-08-04.** Standard Light: `Power`, `Lock`, `On`, `RequiredPower`, `PrefabHash`, `ReferenceId`, `NameHash`. "Battery backup" Light: same set plus `Mode`. Neither has any free-form writable field. This invalidated a project-wide assumption present since before this session's work began (writing an arbitrary value to a Light's `Setting` as an inter-chip signal flag) — see `ic10_airlock_code_notes.md` for the fix (Tier moved to the Logic Transmitter, packed alongside button state).
- **The LED (`StructureDiode`, 25W) has a `Color` LogicType (Read/Write) that neither Light variant does — confirmed via in-game Logic panel screenshot, 2026-08-04.** Full field list: `Power`, `Lock`, `On`, `RequiredPower`, `Color`, `PrefabHash`, `ReferenceId`, `NameHash`. Used to drive a real three-color (green/yellow/red) Tier indicator instead of a plain on/off, restoring the visually-distinct-per-state design intent the (nonexistent) Light `Setting` write was originally meant to provide.
- Search-aggregated result, Color LogicType enum values — **not resolved by research.** Every source pointed back to the Community Wiki's "Data Network Colors" page, which blocked every direct fetch attempt this session. Aggregated search results suggest `0=Blue, 1=Grey, 2=Green, 3=Orange, 4=Red, 5=Yellow, 6=White, 7=Black, 8=Brown, 9=Khaki, 10=Pink, 11=Purple` — used in the prototype code as `ColorGreen=2`/`ColorYellow=5`/`ColorRed=4`, explicitly flagged unconfirmed pending the project owner's own check of that page (or in-game trial).
- **Community Wiki, "Logic Transmitter" page — resolved 2026-08-05 via a locally-saved copy the live page had blocked all session.** Confirms the actual device (`StructureLogicTransmitter`, 50W, Prefab Hash `-693235651`) — not a "Logic Transmitter/Receiver" pair as this project assumed throughout. There is exactly one structure, used in Active or Passive `Mode` (0=Passive, 1=Active); a "receiver" is just a second unit in Passive mode. Data Parameters: `On` (bool), `Mode` (bool), `Setting` (type "Any" — the one value field, not eight numbered channels). Pairing is physical and in-game: the Passive unit's dial is tuned to the Active unit's name, no numeric channel setting exists. This invalidated the project's Channel0–Channel7 assumption (see the superseded entry above) — fixed by packing all four signaled values (Tier + 3 button states) into the single `Setting` field.

---

**Publishing plan (for tracking, not action yet):** the finished project
ships as two things together — (1) an example creative-mode Stationeers
world demonstrating the build in-game, and (2) the code base itself
(IC10 scripts + the JSON/markdown database files in this project). Both
go up together once the build is done, vanilla and modded variants
alike.

---

**Note on citation practice going forward:** every new claim added to
either the requirements doc or the prototype code should get a URL
added here in the same turn, not just a vague "confirmed via wiki"
in-line. Anything in those two files without a matching entry here
hasn't been properly sourced yet — treat that as a gap to fix before
publishing, not an oversight to ignore.
