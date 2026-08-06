# Sources — Device IC10 Scripts

Every script in this folder traces to a source here, same discipline
as the root `SOURCES.md` covers for the airlock design. If a script's
header comment names a source without a matching entry below, it
hasn't been properly logged yet.

## Air Conditioner

- GitHub, jhillacre/stationeers-scripts, `air-conditioner-controller.ic10`
  — https://github.com/jhillacre/stationeers-scripts/blob/main/air-conditioner-controller.ic10
  (already cited elsewhere in this repo's airlock `SOURCES.md`, a
  known-good source used before). Base for `air-conditioner/
  ac_thermostat.ic10` — PID-based temperature control via the AC's
  onboard `db` self-reference. **Bug found and fixed here**: the
  original's tolerance-band check compared current temperature
  against the lower bound in both directions instead of against the
  lower and upper bounds separately, so `Mode` was active almost every
  tick regardless of the tolerance setting. See
  `air-conditioner/ac_thermostat_notes.md` for the full trace.
  **Feature added here**: optional external target-temperature source
  via `d0`, graceful-degrades to the hardcoded default if unwired.

## Filtration

- GitHub, jhillacre/stationeers-scripts, `onboard-filtration.ic10` —
  https://github.com/jhillacre/stationeers-scripts/blob/main/onboard-filtration.ic10
  (same known-good repo used for Air Conditioner). Base for
  `filtration/onboard_filtration.ic10` — dual filter-slot management
  via the unit's onboard `db` self-reference, checked against every
  filter type/size across 7 gas categories. **Unchanged from the
  original** — traced the full slot-status logic (see
  `filtration/filtration_notes.md`) and found no bug; a behavior that
  initially looked suspicious (filtering stops entirely if either
  occupied slot is bad, even if the other is fine) turned out to be a
  deliberate, defensible design choice, documented rather than
  changed. **Feature considered, not added**: an externally-adjustable
  `PRESSURE_TARGET` (mirroring the AC's dial pattern) — confirmed
  not possible as designed, not just held back on suspicion: this
  device's onboard slot exposes exactly two pins, `d0`/`d1`, both
  already used for the Light/Alarm outputs. No room for a third
  input the way the AC had a free `d0`. See the notes file.
- CowsAreEvil is a credible source for this project generally (see the
  root `SOURCES.md`'s Custom Airlock V2 citation) — a "Cow's Internal
  Filtration Controller" is referenced secondhand in a Steam Community
  discussion, but their own Workshop profile page 403's on fetch, so
  it hasn't been located/confirmed directly. Worth checking again if
  the `jhillacre` script above turns out to need more than the design
  notes already cover.
- Steam Community discussion, "The Ultimate Filtration IC10" —
  https://steamcommunity.com/app/544550/discussions/0/797838226728518655/
  — a two-program solution (`FiltrationUnitConfig.ic10` +
  `FiltrationUnitProcess.ic10`), also onboard-chip-slot targeted.
  **Not retrieved** — 403's on fetch, same Cloudflare block as the
  CowsAreEvil profile above. Not currently needed since the
  `jhillacre` script above covers this device already, but left here
  in case it's worth a second implementation to compare against later.
- Steam Workshop, "Filtration Pilot (OnBoard)" —
  https://steamcommunity.com/sharedfiles/filedetails/?id=2978782048
  — same fetch block, same "not currently needed" status.

## Phase Change Separator

**Original design for this project** — the automation framework
(`phase-change-separator/phase_separator.ic10`) is not adapted from
any external source, so it has no `original/` file.

- **Project owner's own Stationpedia screenshots, 11 gases, provided
  2026-08-06** — the primary source for
  `condensation_reference.md`'s full reference table (Hydrazine,
  Sodium Chloride, Carbon Dioxide, Hydrochloric Acid, Hydrogen,
  Methane, Water, Silanol, Nitrogen, Oxygen, Pollutant). Real
  in-game data, read visually off each chart's gridlines — see that
  file's own header for the precision caveat (accurate to the nearest
  gridline, not the decimal). This superseded the search-aggregated
  Nitrogen-only figure the script originally shipped with; that
  figure turned out to match the real chart almost exactly, a good
  independent confirmation, but is no longer the operative source —
  the screenshots are.
- Community Wiki, "Phase Change Mechanics" —
  https://stationeers-wiki.com/Phase_Change_Mechanics — still **not
  retrieved**, 403's on fetch (Cloudflare bot-protection). No longer
  blocking anything now that the screenshots cover the actual data
  needed, but left here in case its explanatory text (not just the
  numbers) is useful later.
- Stationeering (Substack), "Thermal Conditioning & Gas Separation
  Guide" — https://stationeering.substack.com/p/atmos-2-thermal-separation
  — same status, not retrieved, not currently blocking anything.

## Fabrication automation (researched, not yet built)

Devices confirmed to expose Stack-based instructions for external
IC10 automation (Community Wiki, aggregated search result, not yet
independently confirmed per-device): Logic Sorter, Autolathe,
Electronics Printer, Hydraulic Pipe Bender, Tool Manufactory, Security
Printer, Rocket Manufactory. No specific script sourced yet — flagged
here so the lead isn't lost before this category gets its own pass.
