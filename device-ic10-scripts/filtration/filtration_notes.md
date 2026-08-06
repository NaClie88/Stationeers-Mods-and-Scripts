# Filtration Onboard Controller — Setup Guide & Notes

No setup guide existed for this script at the source — writing one
here since the original was a bare `.ic10` file with only inline
comments.

## What this does

Runs directly on the Filtration unit's own onboard IC chip slot (no
separate IC Housing needed). Manages up to two filter slots: checks
each slot's occupancy, remaining quantity, and whether the installed
filter type actually matches a gas currently present in the input
stream. Stops filtering when the outlet pressure hits a cap, when
filters are missing or spent, or when an installed filter has nothing
left to remove. Optionally drives a status light and an alarm if
either is wired.

## Hardware

- **No IC Housing needed** — insert the IC10 chip into the Filtration
  unit's own onboard slot.
- **`d0` (optional): a status Light** — on whenever either slot needs
  attention (bad/exhausted filter, or both slots empty). Leave
  unwired to skip this; the script checks with `bdns` and degrades
  cleanly either way.
- **`d1` (optional): an alarm/siren device** — same trigger condition
  as the light, separate output in case you want a louder warning
  elsewhere. Also optional, same graceful degradation.

## Constants to adjust before use

- `PRESSURE_TARGET` (kPa) — filtering stops once outlet pressure
  (either output side) reaches this cap. Default 40000.

## How slot status is determined

Each slot resolves to one of four states, checked every tick:
- **Empty** (`-1`) — no filter installed. Doesn't stop filtering on
  its own if the *other* slot is fine (single-filter operation works).
- **Bad** (`0`) — filter present but either used up (`Quantity = 0`)
  or its item hash doesn't match any of the recognized filter
  types/sizes for any gas.
- **OK** (`1`) — filter present, has quantity remaining, and matches a
  gas that's actually present in the current input stream.
- **No gas** (`2`) — filter present and valid, but there's currently
  none of its target gas in the input to remove.

## A design choice worth knowing, not a bug

Filtering stops **entirely** if *either* occupied slot comes back
"bad" or "no gas" — even if the other slot has a perfectly good,
actively-needed filter. Traced this carefully (see the original's
boolean logic in `original/onboard-filtration.ic10`) since it looked
at first like it might be the same class of mistake found in the Air
Conditioner script — it isn't. This is consistent, deliberate logic:
any problem in either slot halts the whole unit rather than silently
continuing on one filter, which forces attention to a dead/wrong
filter instead of letting it sit unnoticed. Worth knowing before you
rely on this, since "one good filter, one dead filter" reads as a
total stop, not a partial one.

## A feature considered and *not* added — now confirmed why

An externally-adjustable `PRESSURE_TARGET` (the same pattern used for
the Air Conditioner's optional target-temperature dial on `d0`) would
be a reasonable complementary feature, but there's nowhere to wire it.
**Confirmed 2026-08-06** (Community Wiki, cross-referencing multiple
independent discussions): the Filtration unit's onboard slot exposes
exactly two pins, `d0` and `d1` — both already spoken for by the
Light/Alarm outputs above. Unlike the Air Conditioner (which had a
free `d0` to use for this), there's no room left on this device for
an external setpoint source without giving up the light or the alarm.
Not a guess anymore — genuinely not possible as designed.
