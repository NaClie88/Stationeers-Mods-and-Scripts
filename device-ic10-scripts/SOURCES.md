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

- Steam Community discussion, "The Ultimate Filtration IC10" —
  https://steamcommunity.com/app/544550/discussions/0/797838226728518655/
  — a two-program solution (`FiltrationUnitConfig.ic10` +
  `FiltrationUnitProcess.ic10`) designed to run on the Filtration
  unit's own onboard chip slot with no external IC Housing needed.
  **Not yet retrieved** — the page 403's on fetch (Cloudflare
  bot-protection, the same recurring block this project has hit on
  Steam Community/Workshop and wiki pages before). Queued for a manual
  pull (copy/paste from the page directly, or via the project owner's
  own browser session) rather than more automated-fetch attempts.
- Steam Workshop, "Filtration Pilot (OnBoard)" —
  https://steamcommunity.com/sharedfiles/filedetails/?id=2978782048
  — also onboard-chip-slot targeted. Same fetch block as above, not
  yet retrieved.

## Fabrication automation (researched, not yet built)

Devices confirmed to expose Stack-based instructions for external
IC10 automation (Community Wiki, aggregated search result, not yet
independently confirmed per-device): Logic Sorter, Autolathe,
Electronics Printer, Hydraulic Pipe Bender, Tool Manufactory, Security
Printer, Rocket Manufactory. No specific script sourced yet — flagged
here so the lead isn't lost before this category gets its own pass.
