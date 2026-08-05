# Re-Volt Parts Delta — Fail-Safe Airlock

Every device this build uses (from the vanilla setup guide's hardware
shopping list), checked against Re-Volt's actual feature set — as of
2026-08-05, confirmed against the mod's raw README and commit history,
not just the general repo link (see "Sources" below). This is still a
paper pass, not an in-game test — "confirmed" below means confirmed in
the mod's own source, not confirmed working in this build.

Status legend: 🟢 unaffected · 🟡 optional upgrade available · 🔵
simplification candidate, worth doing · 🔴 needs verification before you
trust the vanilla build under this mod.

| Vanilla part | Alias / pin | Status | Why |
|---|---|---|---|
| IC Housing ×2(+1) | — | 🟢 | Not a power-distribution device; outside Re-Volt's feature scope. |
| IC10 chip ×2(+1) | — | 🟢 | Same. |
| Portal (Airlock door) ×2 | `DoorExt`/`DoorInt` | 🟢 | Doors aren't touched by the power-sim overhaul. |
| Active Vent ×1 | `Vent` | 🟢 | Atmospherics device, not electrical. |
| LED (`StructureDiode`) ×1 | `LED` | 🟢 | Data-network device, not electrical distribution. |
| Logic Switch ("Button") ×3 | read via `lbn` | 🟢 | Same. |
| Gas Sensor ×1–3 | `ChamberSensor`/`SensExt`/`SensInt` | 🟢 | Atmospherics/data, not electrical. |
| **Logic Transmitter ×2** | `Transmitter`/`Receiver` | 🔵 | The Active/Passive pair exists purely as a workaround for sharing data across two differently-powered circuits without merging them. Re-Volt's **Data Diode** does exactly that job natively — see "Simplification candidate" below. |
| **Power Controller — zone gate** | `Gate` (d2, Watcher) | 🟡 | Re-Volt doesn't replace Power Controller outright, so vanilla wiring keeps working unmodified. **Load Center could replace it, but see "Load Center reconsidered" below — it's not a clean 1-for-1 swap for this specific build**, unlike the Data Diode. |
| **Power Controller — dedicated battery** | `Battery` (d0, Watcher) | 🟡 | Downgraded from 🔴 — see "Modular Battery" below. Not an active risk *right now*, but will need re-checking once that ships. |

## Simplification candidate: Data Diode replaces the Transmitter pair

**Confirmed via the mod's own commit** (`c03324d`, "Add optoisolator and
data diode", 2026-07-05). Two devices were added:

- **Optoisolator** (`StructureDataBridge`) — "Using a tiny parasitic
  draw to transfer data signals between up to four networks,
  bidirectionally." Cannot be chained through a second optoisolator.
- **Data Diode** (`StructureDataDiode`) — one-way variant: "the output
  network to see all data devices on the input network, but not
  vice-versa."

That second description is the important one. Watcher → Cycle is
already strictly one-directional in this design — Cycle never sends
anything back to Watcher, since Watcher owns the Gate write itself based
on its own button reads. A Data Diode is a tighter fit than the
bidirectional Optoisolator, and — critically — the wording ("see all
data devices on the input network") implies it **bridges visibility**,
not just relays one value. If that holds up in-game, wiring a Data
Diode between Watcher's always-on network (input side) and Cycle's
gated network (output side) would let Cycle:

- Drop both Logic Transmitters and the manual dial-pairing step entirely
  — currently the single most-forgotten setup step in the vanilla
  build (see the setup guide's own troubleshooting section: "check the
  Passive Transmitter's dial is actually tuned").
- Potentially read the Buttons directly via `lbn`, the same way Watcher
  does now, instead of Watcher packing Tier + 3 button states into one
  `Setting` value for Cycle to unpack (`watcher.ic10` lines 80–87,
  `cycle.ic10`'s corresponding unpack). Whether this actually
  eliminates the packing scheme, or Cycle still needs some shared value
  for Tier specifically, is the one thing left to verify in-game.

**Wiring difference to note:** unlike the wireless Transmitter pair,
a Data Diode needs a physical data cable run between the two housings.
Pair this with a Cable Tray run (below) so it doesn't add its own
separate cable path across the build.

**Forked, as a hypothesis** — `watcher.ic10` and `cycle.ic10` in this
folder drop the Transmitter pair on the assumption above. Cycle reads
Watcher's Buttons directly by name (same `BtnHash`/name constants
Watcher itself uses) and reads Watcher's LED `Color` directly for Tier
instead of unpacking a relayed value. **`LEDHash` in `cycle.ic10` is a
fresh, unconfirmed type-hash** — same standard as `BtnHash`, verify
against Stationpedia before trusting it; if it's wrong, Cycle silently
sees nothing (batch-read semantics, no error) and Tier defaults to
Critical, which would leave the airlock permanently locked down — check
this one early, not last.

Dropping the relay didn't meaningfully shrink the code, for what it's
worth: Watcher goes from 88 to 79 lines (loses the Transmitter alias,
`Mode` write, and packing block), but Cycle actually grows from 104 to
112 (reconstructing Tier from a color comparison costs more lines than
the old div/mod unpack). Both are still well inside the 128-line limit
either way — the actual payoff of this swap is fewer physical parts and
no manual pairing step, not simpler code.

**Do not build against these forked scripts until the Data Diode's
network-bridging is confirmed in-game** — if it turns out to only relay
a single value rather than bridging visibility, these scripts don't
work and the vanilla `ic10-airlock/watcher.ic10` +
`ic10-airlock/cycle.ic10` + two Logic Transmitters remains the correct
build even under Re-Volt.

## Cable Tray: smaller footprint, not just fewer cable objects

**Confirmed implemented** — actively developed as recently as
2026-07-29 (commit `474861f`). Primary-source description (English
localization string, from the junction-box commit): *"A Cable Tray
junction box, connecting up to 6 directions. When placed as part of a
larger cable tray run, connected cables with a matching capacity and
colour will be tied together."*

This project's read on that: a tray run lets you bundle multiple
separate cable networks — power and data alike — through one shared
physical path instead of routing each one individually. For this build
specifically, that's directly relevant to the setup guide's own
footprint note (hardware list, step 2: *"at least 1 more grid of
spillover... for the Vent's pressure tanks, the zone-gate Power
Controller, and the two IC Housings"*). Right now that spillover volume
has to carry, as separate physical runs: the zone-gate power feed, the
Button/`lbn` data wiring, and (if you adopt the Data Diode above) a new
data-cable run between Watcher and Cycle. A Cable Tray run through that
same spillover space could carry all of it, meaningfully shrinking the
footprint and making the build easier to lay out correctly the first
time — fewer independent cable paths to route around each other.

**One caveat:** the fetched description confirms cables are tied
together by "matching capacity and colour," but doesn't explicitly
state whether a single tray run can carry a power network and a data
network side by side, or only same-domain cables. Worth confirming
in-game before assuming you can route the zone's power feed and the
Data Diode's data cable through the exact same tray.

## Modular Battery — not currently a risk, but watch this

**Corrected from the earlier "needs verification" write-up.** The
mod's raw README (`main` branch, fetched 2026-08-05) lists the Modular
Battery split (Charger/Battery Bank/Inverter) and per-battery
charge/discharge-rate limits under **"Future Content,"** not
"Features" — and a targeted search of the commit history for
`battery`/`modular`/`charger`/`bank`/`inverter` returned **zero
matching commits**. As of today, the dedicated battery this design's
Watcher chip reads (`Battery Charge`/`Battery Maximum`, `watcher.ic10`
lines 32–33) is still the plain vanilla Battery/Power Controller
mechanic — the crowbar-and-swap failsafe backstop should work
unmodified right now.

This is a moving target, not a closed question — the mod is under
active development (Circuit Breakers moved from "Future Content" to
shipped between the README's writing and now, confirmed via commit
`5ab21b8`, 2026-07-11, "smart breaker" bug fix). Re-check the README
and commit log before relying on this if time has passed since
2026-08-05, and re-open the two questions from the original
verification note once Modular Battery actually ships:

1. Does the split remove the "single portable item you can crowbar
   out" mechanic entirely?
2. Does `Charge`/`Maximum` move off whichever device ends up wired to
   `Battery` (d0)?

## Load Center reconsidered — not recommended for this build

**Walking back the earlier "purpose-built alternative" framing.**
Digging into the actual commits (`18d5044`, `eb4398c`, both
2026-07-04) turned up more detail than the first pass had, and it
changes the recommendation:

- Load Center exposes `LogicSlotType.On` (writable) and
  `LogicSlotType.Quantity` (readable) — **per slot**, not one flat
  `On` field for the whole group. The commits show five power-class
  slots: **Lights, Doors, Atmospherics, Equipment, Logic** (referred to
  as `Button1`–`Button5` in the source).
- That means gating "the whole Cycle zone" through a Load Center isn't
  one write like the current `s Gate On 1` — this build's zone spans
  three of those categories at once (Portals → Doors, Active Vent →
  Atmospherics, the Cycle IC Housing itself → Logic), so Watcher would
  need to write **three separate slot values** to bring the zone up or
  down together, not one.
- The actual value of a Load Center is managing *multiple, mixed-category
  circuits* from a single dashboard point. This build already gates
  everything through one dedicated, already-isolated Power Controller —
  there's no second circuit here for a Load Center to consolidate
  against. **For this specific build, swapping in a Load Center adds
  script complexity (3 slot writes instead of 1) without removing any
  hardware or wiring**, unlike the Data Diode swap.

**Recommendation: skip this for the airlock build.** Load Center is
still a real, confirmed-implemented device — just a better fit for a
whole-base breaker room (see `database/mods.json` → `revolt.enabled_goals`,
"Breaker Room / Central Power Management") than for this single
isolated 3-device circuit. Exact slot-addressing IC10 syntax (`ls`/`ss`
by slot index, vs. a plain named field) wasn't confirmed in this pass —
not worth chasing further unless the recommendation above changes.

## Circuit Breaker — additive, no script changes needed

**Confirmed implemented**, commit `5ab21b8` (2026-07-11) fixes a Smart
Breaker data-port bug, confirming Smart Breakers expose logic data (not
just Small/Large's plain trip-and-reset behavior) as `database/mods.json`
already claimed. Unlike the two devices above, this one doesn't touch
Watcher or Cycle's code at all — it's a physical addition in series
with the existing zone-gate circuit, not a device either script talks
to.

**Where it goes:** in series with the zone-gate Power Controller's
output, between `Gate` and the rest of the Cycle zone (both Portals,
the Vent, the Cycle IC Housing). Vanilla has no resettable protection
on this circuit at all — an overload burns out a fuse that must be
rebuilt from scratch. A Circuit Breaker here means a trip is a reset,
not a rebuild.

**Which variant:** Small or Large cover the trip-and-reset behavior;
neither needs any script awareness since Watcher and Cycle never read
from it. **Smart** is the only variant worth a second look, purely as
an optional stretch: since it has a confirmed data port, Watcher could
in principle read the breaker's trip state as bonus diagnostic info (a
fourth condition alongside Tier for the LED, say). **Not built into
either the vanilla or the Data Diode scripts** — this project's own
rule against unrequested scope: don't add speculative complexity a
requirement didn't ask for. If you want it, it's a small, isolable
addition on top of whichever base script you're already running, not a
reason to change the core design.

## Sources

- `database/mods.json` → `revolt` entry — feature list and device
  registry for this mod.
- Raw README, `https://raw.githubusercontent.com/Sukasa/ReVolt/main/README.md`
  (fetched 2026-08-05) — Features vs. Future Content split; note the
  README itself is stale relative to the commit log (Cable Tray, Load
  Center, Optoisolator, and Data Diode are shipped but appear in
  neither list).
- Commit `c03324d` ("Add optoisolator and data diode", 2026-07-05) —
  Optoisolator/Data Diode descriptions quoted above.
- Commit `474861f` ("...cable tray junctions...", 2026-07-29) — Cable
  Tray junction-box description quoted above.
- Commit `5ab21b8` ("Fix bug with two data ports on smart breaker...",
  2026-07-11) — Circuit Breaker implementation confirmation.
- Commits `18d5044`, `eb4398c` (both 2026-07-04) — Load Center
  implementation confirmation.
- Commit history search for `battery`/`modular`/`charger`/`bank`/
  `inverter` (2026-08-05, no matches) — Modular Battery not-yet-shipped
  finding.
- `ic10-airlock/ic10_failsafe_airlock_requirements.md` — "Power
  Controller physical placement" section, for why the swap mechanic
  matters this much.
- `ic10-airlock/watcher.ic10` — the script whose `Battery Charge`/
  `Battery Maximum` reads, and Transmitter/Setting packing scheme, are
  discussed above.

**Note on how these were fetched:** commit contents above came through
an AI-summarizing fetch tool, not a direct diff read — the quoted
description strings are as reported by that tool, not independently
re-verified byte-for-byte against the raw commit. Treat them the same
way this project treats any other single-pass research result: high
confidence, not certainty. Worth a direct look at the commit diffs
yourself before this becomes load-bearing for a real build.
