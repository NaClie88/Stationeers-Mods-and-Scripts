# Power Generation Strategy by World

Compiled 2026-08-17. Third in this repo's cross-referenced guide set —
`elemental_lifecycle.md` covers where fuel/materials come from,
`trade_economy.md` covers where to sell/buy them, this one covers turning
them (or sunlight, or wind) into power, and how to choose the right mix
per world. Extends `database/worlds.json`'s existing hazard-profile data
rather than duplicating it — read that file's `radiator_note`/
`priority_mod`/`mining_note` fields alongside this doc, not instead of it.
Same tag scheme as the other two guides:

| Tag | Meaning |
|---|---|
| 🔒 LOCKED IN | Confirmed mechanism/behavior, low risk of being wrong |
| ❓ UNCONFIRMED | Couldn't verify from public sources at the research effort spent |
| ⚠️ CONTRADICTION | Two sources (including this project's own prior docs) disagree — flagged, not silently resolved |
| 🔮 SPECULATION | My own inference from known mechanics, explicitly not sourced directly |

---

## 1. Generator roster (world-agnostic reference)

| Generator | Mechanism | Output | Requirements / notes | Confidence |
|---|---|---|---|---|
| Solar Panel (tracking) | sunlight | world-dependent (§2); storm spike up to ~1.5 kW/panel | 150° tracking sweep limit, degrades near horizon; needs clear line to sun | 🔒 mechanism, per-world numbers in §2 |
| Portable Solar Panel | sunlight | low, early-game | starter-kit item (already in this repo's EVA loadout) | 🔒 |
| Wind Turbine (tall) | atmosphere/wind | up to 500 W in low-density atmosphere, better in dense atmosphere; storm spike up to 20,000 W | **requires atmosphere — confirmed zero output in true vacuum** | 🔒 |
| Upright Wind Turbine | atmosphere/wind | smaller scale; storm output up to ~1 kW | same vacuum requirement as above | 🔒 |
| Solid Fuel Generator | burns Coal/Charcoal/Biomass | 1 unit charcoal ≈ 120 kJ total energy (33.3 Wh); peak kW figure ❓ imprecise in what surfaced, verify in-game | works anywhere, including vacuum (self-contained chemical reaction) | 🔒 mechanism |
| Gas Fuel Generator | burns combustible gas + O₂ (Methane/Hydrogen) | very high — one source cites ~38.3 kW per 1 kPa of fuel (❓ precision unconfirmed) | **must be throttled**, needs Heavy Cable (§1 cable sizing); works anywhere incl. vacuum | 🔒 mechanism, ⚠️ sizing needs care |
| Stirling Engine | temperature **differential** (not fuel) | up to 8 kW peak, ~1.8 kW typical | needs a genuine hot/cold side; ~44.4 kW cooling capacity required to shed waste heat at max output | 🔒 mechanism |
| Portable Generator | small fuel-based | low, early-game | starter-kit item, exempt from the "needs Heavy Cable" rule (only fueled generator under 5 kW) | 🔒 |
| Turbine Generator | pressure differential between two sides | max ~90 W per unit — tiny | ❓ **possibly removed** — one community report (Oct 2025) says this device no longer exists; verify before planning around it | ❓ |
| RTG | radioisotope, no fuel input | ❓ **contradictory sources** — wiki phrasing implies a 4 kW "limitless" variant is creative-mode-only, but separately names an `ItemRTGSurvival` at 800 W, implying a survival-mode version exists. Could not resolve which is true. | check Stationpedia/build menu directly before assuming either figure | ❓ |
| "Nuclear" / Uranium | — | **not a power source in vanilla** | No nuclear reactor exists in-game (devs have discussed it, not implemented). Uranium's real use is off-gassing Pollutant for coolant, not fuel. **This corrects `elemental_lifecycle.md`**, which listed Uranium as "reactor fuel" — already fixed there, noted here so the two docs don't disagree. | 🔒 (correction, two independent sources) |

**Cable sizing (applies on every world):** Normal/Light Cable caps at
**5 kW** actual power; Heavy Cable caps at **100 kW**. A Fuse on the
network blows predictably instead of a random cable segment when capacity
is exceeded — cheap insurance on anything over 5 kW, which per existing
community guidance is every fueled generator except the Portable
Generator. ❓ a "Super Heavy" cable tier turned up in exactly one source
and wasn't corroborated elsewhere — don't assume it exists without an
in-game check.

---

## 2. World-by-world recommendations

Builds directly on `database/worlds.json`'s existing entries — this
section doesn't repeat `hazard_profile`/`radiator_note`/etc., it adds the
power-specific layer on top.

### Mars (baseline)

Solar is the standard primary source — normal day/night cycle, no
extremes. Dust storms damage exposed panels (this repo's progression
guide already documents redeploying panels after a storm to restore
integrity/efficiency). Thin atmosphere means the Wind Turbine works but
sits nearer its low-density floor (~500 W-ish) — somewhat
counterintuitively, the dense-atmosphere hot worlds (Venus/Vulcan, below)
outperform Mars on wind.

**Recommended stack:** Solar (primary) + battery bank sized for full
night coverage + Solid Fuel Generator (Coal/Charcoal) as fallback.
Standard Light Cable is fine unless a Gas Fuel Generator is added, in
which case switch that circuit to Heavy Cable.

### Moon

**Wind Turbine: confirmed zero output.** True vacuum — no atmosphere
means no wind, full stop. Don't build one here.

Solar works fine (nothing to attenuate sunlight), but the Moon's solar
angle is 0°, meaning day and night are **exactly half the day-cycle
length each** — size the battery bank for the *entire* night unassisted,
not a partial-coverage buffer. Solar storms here spike both heat and
panel output (existing repo note already flags the O₂-burn-rate risk;
the same storm event is also a power windfall if cabling is sized to
capture it, same 1.5 kW/panel dynamic as Mars).

Solid/Gas Fuel Generators work fine regardless of vacuum (self-contained
reactions) and are the natural night/storm backup. Remember: Radiation
Radiators only here (no convection in vacuum) — relevant for shedding any
generator's waste heat, not just habitat cooling.

**Recommended stack:** Solar (primary, sized for full-night battery
coverage) + Solid Fuel Generator (Coal/Charcoal — ice is present here per
`worlds.json`, so Gas Fuel Generator on Methane/Hydrogen is also viable
as the night/storm backup, not just Solid Fuel).

### Europa

Atmosphere is present, so the Wind Turbine actually functions here — and
this repo's own `worlds.json` already confirms the Europa Brutal start
ships a Wind Turbine specifically *because* solar is documented as
unreliable on this world (❓ the specific reason solar is unreliable here
— persistent cloud, low light angle, something else — wasn't found in
research; only the recommendation itself is corroborated).

🔮 **Speculation:** Europa's extreme, constant cold is a naturally
available cold-side heat sink for a Stirling Engine — in principle easier
to satisfy its ~44.4 kW cooling requirement here than on a temperate or
hot world, since ambient temperature does much of the rejection work for
free. Not sourced directly — an inference from the Stirling Engine's
known mechanism plus Europa's known thermal profile. Worth testing
in-game, not assumed.

**Recommended stack:** Wind Turbine (primary — atmosphere confirmed
present) + Solar (secondary) + Solid/Gas Fuel Generator as calm-weather
backup (ice is present per `worlds.json`, so Methane/Hydrogen fuel is
locally producible, not just Coal) + reuse Arc Furnace waste heat for
base heating (already documented in `worlds.json`), which frees
generator capacity that would otherwise go to space heaters.

### Venus

Solar is confirmed strong here — **max output 1.15 kW**, despite the
dense atmosphere, because Venus orbits closer to the sun than the
baseline; the thick atmosphere doesn't block light as much as intuition
suggests. Wind Turbine is confirmed effective too — the dense atmosphere
"grants strong winds," genuinely good wind power despite Venus's hostile
reputation. One source states Solar/Wind/Solid Fuel Generator are
"equally effective" on Venus — treat as a rough guide, not a precise
ranking (❓ single source, no Solid-Fuel-specific number given).

No ice here (confirmed in `worlds.json` and `elemental_lifecycle.md`) →
no local Methane/Hydrogen for a Gas Fuel Generator. Either import fuel via
trade (see `trade_economy.md`) or lean on Solar+Wind+Solid Fuel instead.

🔮 **Speculation, opposite conclusion from Europa:** Venus's extreme
ambient heat makes *rejecting* waste heat (the Stirling Engine's cold
side) actively hard — consistent with this repo's existing note that
convection radiators here are "fighting a losing battle" cooling into an
already-hot environment. A Stirling Engine likely fits worse here than on
cold/vacuum worlds, not better, despite "temperature differential"
sounding like it should favor an extreme-temperature world generally —
the differential needs a genuinely *cold* sink, not just a hot source.

**Recommended stack:** Solar + Wind Turbine as co-primary sources (both
independently confirmed strong here). Solid Fuel Generator as backup if
Coal is available (❓ not explicitly confirmed present on Venus — verify
before relying on it); Gas Fuel Generator is not locally fuelable
(no ice) unless fuel is imported via `trade_economy.md`. Skip the
Stirling Engine here per the heat-rejection caveat above unless testing
it specifically.

### Vulcan

Solar is present but orbit-dependent — confirmed range **500 W to 1.2 kW**
tied to Vulcan's elliptical orbit (best near perigee). Less predictable
than Mars/Venus — size the battery buffer for the *low* end of that
range, not the peak.

Same no-ice constraint as Venus — same import-or-substitute fuel logic.
Extra hazard specific to this world (already documented in this repo):
autoignition risk for stored volatile fuels in Vulcan's heat, meaning a
Gas Fuel Generator's fuel storage is a standing Day-1 hazard here, not a
later-game concern. Same Stirling Engine caveat as Venus applies (🔮
speculation, not confirmed for Vulcan specifically).

❓ **Wind Turbine viability on Vulcan is unconfirmed.** Venus's
dense-atmosphere-implies-strong-wind logic doesn't automatically carry
over — Vulcan's atmosphere is only described as "hot, planet-specific
composition" in this repo's own `worlds.json`, with no density figure.
Verify in-game before relying on wind as a primary source here.

**Recommended stack:** Solar (primary, battery sized for the *low* end of
the 500 W–1.2 kW orbital swing) + Solid Fuel Generator (Coal, if
confirmed present — same ❓ as Venus) as the dependable backup, since Gas
Fuel Generator needs imported fuel (no ice) and carries the extra
autoignition hazard noted above. Treat Wind Turbine as a bonus to test
for, not something to plan the base around, until confirmed.

### Space / Orbit / Asteroid Belt

**Wind Turbine: confirmed zero output**, same as Moon — full vacuum, no
atmosphere at all.

⚠️ **Contradiction with this repo's own progression guide, flagged for
follow-up rather than silently overridden:** `guide/stationeers_progression.md`
currently recommends prioritizing "non-solar power (nuclear, wind if
available, or imported fuel)" over Solar Panels here. Two of those three
alternatives don't actually exist as options: nuclear isn't a real
vanilla power source (§1), and wind is definitionally impossible in
vacuum (confirmed above). Whether Solar itself is actually *good* or
*bad* here depends on something research couldn't confirm either way —
does this "world" simulate a day/night cycle at all (rotation, orbital
eclipsing by a body), or is exposure closer to constant? If it's closer
to constant, Solar could plausibly be the *best* option here precisely
because there's no night to plan around — the opposite of the existing
guidance. **Not resolved — needs an in-game check before either doc's
claim is trusted.**

What *is* confirmed regardless: Solid/Gas Fuel Generators work
independent of vacuum, and the Asteroid start's pre-filled Liquid Oxygen
+ Liquid Volatiles tanks (already documented in `worlds.json`) are
literally generator fuel sitting in the starting inventory, not just
rocket fuel — a Gas Fuel Generator could run on those tanks directly
before any mining happens. Radiation Radiators only, same as Moon.

**Recommended stack:** Gas Fuel Generator (primary — run directly off the
Asteroid start's pre-filled Liquid Oxygen + Liquid Volatiles tanks before
anything else exists) + Solar as a to-be-tested supplement once the
day/night question above is resolved. Don't build a Wind Turbine here
under any circumstances (confirmed zero output).

### Mimas

⚠️ **Contradiction found in this project's own data, flagged rather than
resolved:** `database/worlds.json` lists Mimas's atmosphere as
`"vacuum"` and separately states the Mimas Brutal start "is confirmed to
include a Wind Turbine ... because solar is unreliable here." But this
doc's §1 research directly confirms Wind Turbines produce **zero output
in true vacuum**. Those two claims can't both be true as currently
written. Possible explanations, none confirmed:
- Mimas might have some non-zero trace atmosphere despite the "vacuum"
  label (a documentation imprecision in `worlds.json`)
- the Brutal-start "Wind Turbine" kit might be included for a different
  reason (e.g. forward-compatibility, or usable if the player relocates)
  rather than as a claim it'll produce power in place
- the vacuum-means-zero-output research finding has an exception this
  research didn't surface

**Needs an in-game check** — place the starting Wind Turbine kit on
Mimas and read its logic output — before either doc's claim is trusted
at face value. Logged to README Planned.

What's solid regardless of that open question: no water ice (confirmed),
so hydration and local fuel-gas production both need substitution
anyway. Solid/Gas Fuel Generators work independent of atmosphere and are
the safe default here until the Wind Turbine question resolves.
Extremely low gravity and weak/distant solar (already in `worlds.json`)
mean even a working Solar Panel should be budgeted as a low-output
supplement, not a primary source, regardless of how the wind question
shakes out.

**Recommended stack:** Solid/Gas Fuel Generator (primary — no water ice
means substituting an imported/synthesized water source anyway, so plan
fuel logistics around that same supply run) + Solar as a low-output
supplement only. Hold off on the Wind Turbine as anything more than an
experiment until the vacuum contradiction above is resolved in-game.

---

## 3. Cross-references

- **Fuel sourcing** (Coal, Methane, Hydrogen, and the corrected Uranium
  finding) — `guide/elemental_lifecycle.md`
- **Importing fuel where local production is impossible** (Venus/Vulcan
  no-ice worlds) — `guide/trade_economy.md`
- **Existing power-relevant projects** (0.3 Emergency Power, 1.1 Energy
  Infrastructure, 3.1 Power Automation with battery-threshold generator
  cycling) — `guide/stationeers_progression.md`

---

## Sources

- [Power Supply — Stationeers Community Wiki](https://stationeers-wiki.com/Power_Supply)
- [Solar Panel — Stationeers Community Wiki](https://stationeers-wiki.com/Solar_Panel)
- [Solar Logic Circuits Guide — Stationeers Community Wiki](https://stationeers-wiki.com/Solar_Logic_Circuits_Guide)
- ["Estimating Solar Panel Efficiency" — Stationeering Systems (Summer)](https://stationeering.substack.com/p/estimating-solar-panel-efficiency)
- [Wind Turbine — Stationeers Community Wiki](https://stationeers-wiki.com/Wind_Turbine)
- [Upright Wind Turbine — Stationeers Community Wiki](https://stationeers-wiki.com/Upright_Wind_Turbine)
- [Solid Fuel Generator — Stationeers Community Wiki](https://stationeers-wiki.com/Solid_Fuel_Generator)
- [Gas Fuel Generator — Stationeers Community Wiki](https://stationeers-wiki.com/Gas_Fuel_Generator)
- [Stirling Engine — Stationeers Community Wiki](https://stationeers-wiki.com/Stirling_Engine)
- [Turbine Generator — Stationeers Community Wiki](https://stationeers-wiki.com/Turbine_Generator)
- [RTG — Stationeers Community Wiki](https://stationeers-wiki.com/RTG)
- [Ore (Uranium) — Stationeers Community Wiki](https://stationeers-wiki.com/Ore_(Uranium))
- [Cable / Cables — Stationeers Wiki (Fandom) / Community Wiki](https://stationeers-wiki.com/Cables)
- [Venus — Stationeers Community Wiki](https://stationeers-wiki.com/Venus)
- Steam Community discussions (Wind Turbine vacuum behavior, Uranium's
  actual use, Turbine Generator removal report, RTG creative/survival
  question) — individual thread URLs not preserved per-claim; re-derive
  via the search terms in this doc's research history if a specific
  thread needs re-checking
- `database/worlds.json` and `guide/stationeers_progression.md` in this
  repo (existing hazard-profile and progression data this doc extends)
