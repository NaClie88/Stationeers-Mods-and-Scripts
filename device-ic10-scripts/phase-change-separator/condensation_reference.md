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

| Gas | Freezing point | Chart max pressure | Temperature at chart max pressure | Notes |
|---|---|---|---|---|
| Hydrazine | ~274 K (~1°C) | 6000 kPa | ~520 K | |
| Sodium Chloride | ~600 K | 500 kPa | ~2500 K (curve still rising at chart's right edge) | Note the much lower 500 kPa chart ceiling vs. the 6000 kPa used for most other gases. |
| Carbon Dioxide | ~217 K (~-56°C) | 6000 kPa | ~270 K | |
| Hydrochloric Acid | ~248 K (~-25°C) | 1000 kPa | ~430 K | Chart ceiling is 1000 kPa, not 6000. |
| Hydrogen | ~14 K | 6000 kPa | ~70 K | |
| Methane | ~83 K | 6000 kPa | ~195 K | |
| Water | ~273 K (0°C) | 6000 kPa | ~640 K | Freezing point lines up exactly with real-world 273.15 K — good sanity check that these charts read consistently. |
| Silanol | ~165 K | 6000 kPa | ~820 K | |
| Nitrogen | ~41 K | 6000 kPa | ~190 K (~-83°C) | Matches the placeholder figure the script originally shipped with (6000 kPa / -83.2°C) almost exactly — good independent confirmation that the original web-search figure happened to be right. |
| Oxygen | ~55 K | 6000 kPa | ~160 K | |
| Pollutant | ~175 K | 6000 kPa | ~430 K | **Different shape from every other gas here** — the shaded liquid region has a floor around ~1800 kPa even at its coldest; below that pressure, Pollutant apparently won't condense at any temperature on this chart. Worth double-checking in-game before relying on a low-pressure Pollutant separation stage. |

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

`phase_separator.ic10` currently only wires up a single stage
(Nitrogen, matching what's now confirmed real data rather than a
guess). See `phase_separator_notes.md` for how to point the script at
a different gas, and for the multi-stage extension path now that real
data exists to drive it.
