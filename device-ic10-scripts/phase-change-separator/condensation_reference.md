# Condensation Reference Table

**Source: read directly from in-game Stationpedia phase diagrams,
screenshotted and provided by the project owner, 2026-08-06.** This is
primary-source game data, not a web search guess — a real upgrade
from the single unconfirmed Nitrogen figure the script originally
shipped with. Values below are read visually off each chart's
gridlines, not extracted from exact pixel/numeric data, so treat them
as accurate to the nearest gridline division, not to the decimal —
good enough to pick a real operating point, worth a live in-game
Logic Reader check if you need tighter precision than that.

**Gases that cannot be liquefied in-game at all — do not build a stage
for these, regardless of what a future chart might suggest:**
- **Helium** — confirmed by the project owner (2026-08-06): cannot be
  liquefied in this game's simulation, full stop, no pressure/
  temperature combination works. A stage targeting Helium would wait
  forever for a phase change that structurally can't happen. If more
  gases turn out to share this property, add them to this list before
  they cause the same silent hang.

**Wiki coverage gap, separate from the above:** several gases don't
have a published phase diagram on the Community Wiki yet, independent
of whether they can actually be liquefied in-game. That's a wiki
completeness gap, not evidence a gas can't condense — don't treat "no
wiki chart found" as equivalent to "can't be liquefied" the way
Helium genuinely can't. In-game Stationpedia (or the project owner's
own screenshots, as with the 11 gases below) is the actual authority;
the wiki search hitting a dead end just means look elsewhere, the same
lesson this project has already learned repeatedly with Cloudflare-
blocked wiki fetches.

Each gas's diagram plots a **freezing point** (a fixed temperature,
independent of pressure — the hard lower bound where the gas solidifies
instead of condensing) and a **P-T curve** (the condensation boundary —
everything above/right of the freezing line and above/below the curve,
per the shaded "Liquid Phase" region, is where the gas exists as a
liquid). Two reference points per gas below: the freezing point, and
where the curve sits at the chart's own maximum plotted pressure (this
varies by gas — not every diagram uses the same 6000 kPa ceiling).

**Index column** is the gas-selector dial value used by
`two-chamber-system/`'s paired scripts (`separator_ac_driver.ic10`'s
`lookupTemp` and `separator_sequencer.ic10`'s `lookupPressure`) — set
a Logic Dial to a gas's index to target it. Keep this column in sync
with both scripts' lookup tables if this reference table ever changes.

| Index | Gas | Freezing point | Chart max pressure | Temperature at chart max pressure | Notes |
|---|---|---|---|---|---|
| 0 | Hydrazine | ~274 K (~1°C) | 6000 kPa | ~520 K | |
| 1 | Sodium Chloride | ~600 K | 500 kPa | ~2500 K (curve still rising at chart's right edge) | Note the much lower 500 kPa chart ceiling vs. the 6000 kPa used for most other gases. **Freezing point (~600K) is a dramatic outlier versus every other gas in this table** (next highest is ~274K) — it solidifies well above where the other 10 would even still be liquid, so a two-chamber setup built around their operating range is unlikely to keep it liquid long enough to matter in practice (project owner, 2026-08-07). Separately, `separator_ac_driver.ic10`'s 2500K target for this gas index also drives the AC's *controlled* side, which has a real ~999°C/1272K ceiling (see `furnace-heat-recovery/topology_notes.md` on the `furnace-heat-recovery` branch) — likely unreachable as wired, but left unfixed given how unlikely this gas is to see real use. |
| 2 | Carbon Dioxide | ~217 K (~-56°C) | 6000 kPa | ~270 K | |
| 3 | Hydrochloric Acid | ~248 K (~-25°C) | 1000 kPa | ~430 K | Chart ceiling is 1000 kPa, not 6000. |
| 4 | Hydrogen | ~14 K | 6000 kPa | ~70 K | |
| 5 | Methane | ~83 K | 6000 kPa | ~195 K | |
| 6 | Water | ~273 K (0°C) | 6000 kPa | ~640 K | Freezing point lines up exactly with real-world 273.15 K — good sanity check that these charts read consistently. |
| 7 | Silanol | ~165 K | 6000 kPa | ~820 K | |
| 8 | Nitrogen | ~41 K | 6000 kPa | ~190 K (~-83°C) | Matches the placeholder figure the script originally shipped with (6000 kPa / -83.2°C) almost exactly — good independent confirmation that the original web-search figure happened to be right. |
| 9 | Oxygen | ~55 K | 6000 kPa | ~160 K | |
| 10 | Pollutant | ~175 K | 6000 kPa | ~430 K | **Different shape from every other gas here** — the shaded liquid region has a floor around ~1800 kPa even at its coldest; below that pressure, Pollutant apparently won't condense at any temperature on this chart. Worth double-checking in-game before relying on a low-pressure Pollutant separation stage. |

**Helium has no index and no row** — see the "cannot be liquefied"
warning above. Never assign it one.

## What this doesn't give you yet

These are two-point readings (freezing point + one point on the
curve), not the full curve shape. For gases with a strongly curved
P-T line (most of them — compare how differently each chart bends),
picking a pressure partway between 0 and the chart max and assuming
the temperature scales linearly would be wrong. If you need a specific
operating pressure below the chart ceiling, read that specific point
off your own Stationpedia diagram rather than interpolating from just
these two references.

## Using this table

Two different scripts use this data, for two different hardware
setups:

- **`phase_separator.ic10`** (this folder) — a simpler, single-chamber
  design using a generic Active Vent for pressure and monitoring
  temperature only (you supply your own passive/active cooling).
  Currently hardcoded to Nitrogen; see `phase_separator_notes.md` for
  swapping to a different gas.
- **`two-chamber-system/`** — the real Condensation/Evaporation
  Chamber pair setup, with a dial to pick the active gas live rather
  than editing constants. See `two-chamber-system/two_chamber_notes.md`
  for the full system. This is the one to use if you have (or are
  building) the dedicated Chamber pair; `phase_separator.ic10` is for
  a simpler single-tank build.
