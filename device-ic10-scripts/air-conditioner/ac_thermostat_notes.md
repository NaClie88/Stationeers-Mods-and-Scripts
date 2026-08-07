# Air Conditioner Thermostat — Setup Guide & Notes

No setup guide existed for this script at the source — writing one
here since the original was a bare `.ic10` file with only inline
comments.

## What this does

Runs directly on the Air Conditioner's own onboard IC chip slot (not
a separate IC Housing — this device hosts the chip itself). Keeps the
room near a target temperature using a PID controller, only engaging
(`Mode = 1`, active) when the reading drifts outside a tolerance band
around the target, rather than constantly cycling.

## Hardware

- **No IC Housing needed.** The Air Conditioner has its own chip slot
  behind a small door on the unit's face — insert the IC10 chip
  there directly.
- **Optional: a Logic Dial (or any device exposing a numeric
  `Setting`) wired to the AC's `d0` pin**, if you want to adjust the
  target temperature in-game without re-flashing the script. Leave
  `d0` unwired to just use the hardcoded `TARGET` define (294.15 K,
  ~21°C/70°F) — the script checks for this with `brdns` and falls
  back automatically, no error either way.
- The AC's IC slot exposes `db` (aliased `Self` here) as a
  self-reference to the AC's own logic, plus external pins for wiring
  other devices — confirmed via Community Wiki, not yet independently
  decompiled. If you don't wire anything to `d0`, nothing about the
  Wiki's claim of "two pins to connect two devices" needs verifying at
  all; the script degrades cleanly either way.

## Constants to adjust before use

- `TARGET` (line 15) — target temperature in Kelvin, only matters if
  you leave `d0` unwired. 294.15 K ≈ 21°C ≈ 70°F.
- `FUDGEK` (line 16) — tolerance band half-width, in Kelvin. The AC
  only engages once the reading drifts more than this far from target
  in either direction. Widen it to reduce cycling frequency at the
  cost of a wider temperature swing.

## The bug found in the original, and the fix

Pristine copy of the source, unmodified, kept read-only:
`original/air-conditioner-controller.ic10` (`jhillacre/
stationeers-scripts`, see `../SOURCES.md`). The relevant lines:

```
sub r2 TARGET FUDGEK
add r3 TARGET FUDGEK
slt r4 r2 r0
sgt r5 r2 r0
or r6 r4 r5
s Self Mode r6
```

`r2` is the lower bound, `r3` the upper bound — but both comparisons
(`slt r4 r2 r0` and `sgt r5 r2 r0`) test the *same* pair, `r2` against
`r0` (current temperature), just in opposite directions. That's
equivalent to `r6 = (current != lower_bound)` — true on almost every
tick regardless of `FUDGEK`, since exact floating-point equality with
one specific bound essentially never happens. `r3` (the upper bound)
is computed and then never referenced again — the tolerance band
never did anything; `Mode` was active nearly continuously.

Fixed here:

```
slt r4 r0 r2   # current < lower bound (too cold)
sgt r5 r0 r3   # current > upper bound (too hot)
or r6 r4 r5    # outside the tolerance band, either direction
s Self Mode r6
```

Now `Mode` only goes active when the reading actually leaves the
`[target - FUDGEK, target + FUDGEK]` band, matching what the
original's own "fudge check" comment described as the intent.

## A second bug, found 2026-08-07: the PID controller was never actually PID

Found while reviewing `separator_ac_driver.ic10` (`../phase-change-separator/
two-chamber-system/`), which reuses this same `scaleSetting` subroutine
unchanged — the same bug exists in both files, fixed in both together.

`scaleSetting` computes a proportional term and a derivative-looking
term from the current and previous error, then adds both to the
previous setting -- a P+D update. The bug: it also added a *third*
term, `prevError * 0.1`, that isn't part of any standard PID
structure:

```
r5 = error * 0.1            # P term
r6 = prevError * 0.1        # the extra term
r7 = (error - prevError) * 0.1   # D term
r5 = r5 + r6 + r7
newSetting = prevSetting + r5
```

Expand that: `r5 + r6 + r7 = 0.1*error + 0.1*prevError + 0.1*error -
0.1*prevError`. The `prevError` terms cancel exactly, every time,
regardless of what `prevError` actually is. So despite pushing and
popping `PreviousError` across every single call, the real effective
update was always just `newSetting = prevSetting + 0.2*error` -- a
plain proportional controller with `Kp=0.2`, no derivative damping at
all. The tolerance-band bug above was already found and fixed during
the original adaptation from `jhillacre/stationeers-scripts`; this one
slipped through that same pass.

Fixed by removing the extra `r6` term:

```
r5 = error * 0.1                  # P term, Kp=0.1
r7 = (error - prevError) * 0.1    # D term, Kd=0.1
r5 = r5 + r7
newSetting = prevSetting + r5
```

Now `newSetting = prevSetting + 0.1*error + 0.1*(error - prevError)`,
a real P+D controller. Behavior change to expect in-game: less
overshoot/oscillation around the fudge band than before, since the
derivative term now actually dampens fast-changing error instead of
being silently canceled -- worth a live comparison if you were already
running this script and it seemed to work fine, since "fine" was a
pure-P controller with double the intended proportional gain, not
broken outright.

## Known limitations / not yet verified

- The Air Conditioner's onboard pin behavior (`db`, and whatever `d0`/
  external pins expose) is documented from the Community Wiki, not
  independently decompiled the way `logic-network-reference` handles
  other devices — worth a ground-truth pass if this script sees heavy
  use.
- No stall/error handling beyond what's built into the PID loop
  itself — if the AC can't reach target (insufficient power, room too
  large), this script will keep nudging `Setting` indefinitely rather
  than giving up. Same category of gap as the airlock's own stall
  handling before that was addressed — worth revisiting the same way
  if it turns out to matter in practice.
