# Re-Volt Parts Delta — Fail-Safe Airlock

Every device this build uses (from the vanilla setup guide's hardware
shopping list), checked against Re-Volt's documented feature set
(`database/mods.json` → `revolt`, sourced from
[Sukasa/ReVolt](https://github.com/Sukasa/ReVolt)). Nothing below has
been checked **in-game** yet — this is a paper pass over what Re-Volt's
own feature list claims to touch, same "flag by confidence" standard the
vanilla docs hold themselves to.

Status legend: 🟢 unaffected · 🟡 optional upgrade available · 🔴 needs
verification before you trust the vanilla build under this mod.

| Vanilla part | Alias / pin | Status | Why |
|---|---|---|---|
| IC Housing ×2(+1) | — | 🟢 | Not a power-distribution device; outside Re-Volt's feature scope. |
| IC10 chip ×2(+1) | — | 🟢 | Same. |
| Portal (Airlock door) ×2 | `DoorExt`/`DoorInt` | 🟢 | Doors aren't touched by the power-sim overhaul. |
| Active Vent ×1 | `Vent` | 🟢 | Atmospherics device, not electrical. |
| LED (`StructureDiode`) ×1 | `LED` | 🟢 | Data-network device, not electrical distribution. |
| Logic Switch ("Button") ×3 | read via `lbn` | 🟢 | Same. |
| Logic Transmitter ×2 | `Transmitter`/`Receiver` | 🟢 | Data-network device, power-isolated by design even in vanilla. |
| Gas Sensor ×1–3 | `ChamberSensor`/`SensExt`/`SensInt` | 🟢 | Atmospherics/data, not electrical. |
| **Power Controller — zone gate** | `Gate` (d2, Watcher) | 🟡 | Re-Volt doesn't list Power Controller as a replaced device, so the plain vanilla wiring should keep working as-is. But this is exactly the kind of circuit Re-Volt's new **Load Center** (group many devices under one controllable point) and resettable **Circuit Breaker** (Small/Smart) were built for — see "Optional enhancements" below. |
| **Power Controller — dedicated battery** | `Battery` (d0, Watcher) | 🔴 | **The one that matters.** See "Needs verification" below — this is the swappable-battery failsafe backstop the whole design leans on. |

## Needs verification — do this first if you're running Re-Volt

**The dedicated battery / swap mechanic.** The vanilla design's core
safety backstop is: a player trapped in a fully dead chamber can crowbar
the dedicated Power Controller open and physically swap in a fresh
battery (requirements doc, "Power Controller physical placement"
section). Re-Volt's Modular Battery split turns a single battery role
into three separate structures — **Charger**, **Battery Bank**, and
**Inverter** (`database/mods.json` → `revolt.new_devices`) — plus adds
per-battery charge/discharge-rate limits. Two open questions this
project hasn't answered yet, because they require either the mod's
source/wiki or an actual in-game test:

1. **Is the swappable item still a single portable battery you can
   crowbar out, or does the Modular Battery split remove that mechanic
   entirely** (i.e. Battery Bank is a fixed structure, not a portable
   item)? If it's no longer swappable, the crowbar-and-swap backstop
   this design depends on doesn't work the same way under Re-Volt, and
   the requirements doc's failsafe story needs a Re-Volt-specific
   alternative — not yet designed.
2. **Does whatever ends up wired to `Battery` (d0 on Watcher) still
   expose `Charge` and `Maximum` as LogicTypes**, the same two fields
   `watcher.ic10` reads every tick (see `watcher.ic10` lines 32–33)? If
   the Modular Battery split moves those fields onto the Battery Bank or
   Inverter specifically rather than a Power Controller wrapping the
   whole thing, `d0` may need to point at a different structure than in
   the vanilla wiring table.

Until both are answered, **do not assume the vanilla `watcher.ic10`
works unmodified under Re-Volt** — it's the single highest-risk part of
this build to get wrong, since it's the chip the in-chamber
trapped-player scenario depends on. Check Sukasa/ReVolt's own
documentation/source first; if that doesn't resolve it, testing in-game
with a Logic Reader against whatever's wired to `d0` will.

If verification shows a real difference, fork the affected script(s)
into this folder (`watcher.ic10`, etc.) rather than editing the vanilla
one in `ic10-airlock/` — update `README.md` in this folder once that
happens.

## Optional enhancements (not required, vanilla wiring still works)

Neither of these fixes a broken part — they're upgrades Re-Volt makes
possible that vanilla couldn't do at all:

- **Circuit Breaker (Small or Smart) in series with the zone-gate
  circuit.** Vanilla has no resettable breaker — an overload burns out
  a fuse that must be replaced (`database/mods.json` →
  `revolt._build_order_principle` context). Re-Volt's Circuit Breaker
  trips and resets instead. Purely additive; doesn't change any wiring
  already in the setup guide, just adds a component in series.
- **Load Center for the whole Cycle zone**, replacing the plain
  zone-gate Power Controller. Would let the zone-gate circuit be
  managed alongside other grouped circuits from one point instead of
  a single dedicated Power Controller. Same `Gate`-style logic write is
  assumed to still apply, but the exact LogicType on a Load Center
  hasn't been checked — verify before relying on it if you go this
  route.

## Sources

- `database/mods.json` → `revolt` entry — feature list and device
  registry for this mod.
- [Sukasa/ReVolt](https://github.com/Sukasa/ReVolt) — primary source,
  not yet mined for specific LogicType names on the new devices.
- `ic10-airlock/ic10_failsafe_airlock_requirements.md` — "Power
  Controller physical placement" section, for why the swap mechanic
  matters this much.
- `ic10-airlock/watcher.ic10` — the script whose `Battery Charge`/
  `Battery Maximum` reads are the specific risk flagged above.
