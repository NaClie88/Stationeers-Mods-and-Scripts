# Elemental Lifecycle Reference

Compiled 2026-08-17. Game context: post-**Gases Update** (shipped
2026-03-19), so roughly five months old at time of writing — recent enough
that community docs may still be catching up, which is exactly why this
doc tags what's solid vs. what's still shifting.

Origin: written to answer "where does every element actually come from,
and does the game ever create or destroy matter" while working on
`saltys-yield-multiplier` (the ingot/ice-melt yield mod, tracked in its own
repo) — kept here instead, since it's general game-mechanics reference
material, not specific to that one mod.

## How to read the tags

| Tag | Meaning |
|---|---|
| 🔒 LOCKED IN | Long-standing mechanism, stable across many versions, structurally unlikely to change |
| 🆕 NEW (Mar 2026) | Added or renamed in the Gases Update — mechanic exists, but is young and more likely to get rebalanced |
| ⚠️ SOFT SPOT | A place the simulation is a deliberate/known abstraction rather than strict mass balance |
| ❓ UNCONFIRMED | Couldn't verify from public sources at the research effort spent — check in-game Stationpedia or decompile before relying on it |
| 🔮 SPECULATION | My own forward-looking guess, explicitly not sourced from any patch note or dev statement |

First-party note: only the **ice-melt gas ratios** and **furnace/ArcFurnace
ingot-scale mechanics** below are things `saltys-yield-multiplier`'s own
dev process decompiled and verified directly (see that repo's
`UpdateNotes.md`). Everything else here is community wiki / patch notes /
forum research, cross-checked across multiple sources but not decompiled —
flagged "❓" or "high confidence, not first-party verified" where relevant,
same discipline this repo's other `SOURCES.md` files use.

---

## 0. The headline answer

**Structurally, yes — mass is conserved everywhere in the "hard" physical
systems**: mining → smelting, ice → gas, electrolysis, phase change,
combustion, and recycling (which explicitly *loses* mass on purpose, but
never gains it). Those are all recipe/ratio driven and, where checked
directly, backed by real decompiled code.

**Farming and composting are the one system that doesn't cleanly show its
math** (§8) — and that's not a misunderstanding on the reader's part. It's
a documented, exploitable vanilla balance issue: players have built
plant+composter loops that produce net-positive water, and a community mod
("Plants and Nutrition") exists specifically to patch it.

---

## 1. Solid chain: Ore → Ingot → Item → (scrap) → Ore again

🔒 LOCKED IN — the oldest, most stable system in the game.

| Ore | Mined as | Smelts to | Notes |
|---|---|---|---|
| Iron | solid ore | Iron Ingot (1:1) | also → Steel (3 Iron : 1 Coal) |
| Copper | solid ore | Copper Ingot (1:1) | + Nickel 1:1 → Constantan |
| Gold | solid ore | Gold Ingot (1:1) | + Silver 1:1 → Electrum |
| Silver | solid ore | Silver Ingot (1:1) | + Gold → Electrum |
| Nickel | solid ore | Nickel Ingot (1:1) | + Iron → Invar; + Copper → Constantan |
| Lead | solid ore | Lead Ingot (1:1) | + Iron → Solder |
| Silicon | solid ore | Silicon Ingot (1:1) | electronics/glass |
| Cobalt | solid ore | **no base ingot** | alloy-only ingredient (Astroloy, Hastelloy) |
| Uranium | solid ore | Uranium Ingot (1:1) | ~~reactor fuel~~ **CORRECTED 2026-08-17**: not a power source in vanilla — no nuclear reactor exists in-game (planned by devs, not implemented); Uranium's actual use is off-gassing Pollutant (used as coolant) when heated. The "reactor fuel" claim above was wrong, sourced from a single ambiguous wiki snippet; two independent community sources now confirm the correction. Kept struck through rather than deleted, per this project's correction-trail convention. |
| Coal | solid | (not smelted alone) | reagent for Steel |
| Salt 🆕 | solid ore | — | new in Gases Update; feeds Liquid Sodium Chloride chain; added to Ore Trader sell list |

**Alloys** (❓ community-sourced, not decompiled): Steel (3 Iron:1 Coal),
Electrum (1 Silver:1 Gold), Invar (1 Iron:1 Nickel), Constantan (1
Copper:1 Nickel), Solder (1 Iron:1 Lead), Astroloy (2 Iron:1 Copper:1
Cobalt), Hastelloy (2 Nickel:1 Silver:1 Cobalt), Inconel (2 Nickel:1
Gold:1 Iron), Waspaloy (2 Nickel:1 Lead:1 Silver).

**Where solids "end":** worn/scrapped items → Recycler → Reagent Mix →
Centrifuge → ore again, at a **lossy ~50% ratio**. Deliberate entropy
sink, first-party verified for `saltys-yield-multiplier` by decompile
(`Centrifuge` uses `RecyclerRecipeComparable` with a hardcoded
`_recycleRatio = 0.5f`) — 🔒 mechanism confirmed; the exact 0.5 value is a
tunable balance number and could change later. Deep Miner "Dirty Ore" and
Rocket Miner "Space Ore" also route through the Centrifuge → random single
ore, 1:1 (❓ community-sourced).

---

## 2. Ice chain: solid → liquid/gas

🔒 mechanism locked in, 🆕 the substance roster and naming just changed a lot.

| Ice | Melts/processes to | Ratio | Confidence |
|---|---|---|---|
| Ice (Water) | Water (liquid) + trace Oxygen | — | community-sourced |
| Ice (Oxite) | Oxygen + Nitrogen gas | 22.5 mol O₂ : 2.5 mol N₂ per unit | **first-party, decompile-confirmed** |
| Ice ("Volatiles") | Methane + Hydrogen gas | 20 mol CH₄ : 2 mol H₂ per unit | **first-party, decompile-confirmed** — but 🆕 the gas itself was *renamed* Volatiles → Methane in the Gases Update, so any older guide saying "Volatiles gas" means today's Methane. Naming changed, chemistry didn't. |
| Ice (Hydrazine) 🆕 | Hydrazine | — | added Gases Update; mined via Rocket Miner from Hydrazine asteroids specifically |
| Salt / sodium-chloride-bearing material 🆕 | Liquid Sodium Chloride (only exists above 300 °C) | — | ❓ exact mined form unconfirmed |

Naming note: `saltys-yield-multiplier`'s own `UpdateNotes.md` already
records the ice-melt output as "Methane" (post-rename), while the **item**
is still called "Volatile Ice" in the same note — item name may just be
lagging the gas rename, or there's an in-game inconsistency. Worth
checking Stationpedia directly rather than trusting either name blindly.

---

## 3. Phase change: gas ↔ liquid (Evaporation/Condensation Chambers)

🔒 the *framework* — condense above a gas's vapor pressure and below its
max liquid temp; evaporate the reverse; heat exchanges with the
environment either way. This framework was a major rework a few updates
*before* the Gases Update, which then piggybacked on it to give the eight
new substances their liquid states. The **framework** is maturing toward
🔒; **individual substances' thresholds** (vapor pressure, max liquid
temp) are exactly the numbers that get rebalanced patch to patch — treat
any specific threshold as a snapshot, not gospel. (See
`device-ic10-scripts/phase-change-separator/condensation_reference.md` in
this repo for the working reference this project already maintains.)

---

## 4. Electrolyzer: Water → Hydrogen + Oxygen

🔒 straightforward decomposition, locked-in mechanism, outputs mixed in
one pipe (dangerously combustible together). Always outputs at 20 °C;
runaway heat (>300 °C in the output network) auto-ignites the mix into
steam.

🆕 knock-on effect of the Gases Update: Hydrogen used to just be "half of
the flammable gas mixed with oxygen." Now that it's a first-class,
separately liquefiable substance (§6), an electrolyzer producing it feeds
a genuinely more useful output than it did five months ago.

---

## 5. Combustion — 🆕 fully reworked, March 2026

Before the Gases Update: one shared combustion model for everything.
After: **fully data-driven**, per-substance reactions. The in-game
Stationpedia now has a **Combustion Info Panel** per gas showing input
ratios, auto-ignition temperature, and output products — **that panel is
the single most authoritative source**, more so than any wiki (including
this doc), since research here is a snapshot and the game isn't.

| Fuel | + Oxidizer | → Products | Notes |
|---|---|---|---|
| Methane (ex-Volatiles) | O₂ | CO₂ + Pollutant + trace uncombusted fuel | classic Gas Fuel Generator exhaust |
| Hydrogen 🆕 | O₂ | pure Water (Steam) | cleaner than Methane's combustion — no Pollutant |
| Hydrazine 🆕 | none needed | — | monopropellant — ignites from heat/spark alone |
| Helium 🆕 | — | does not react with anything | inert by design — see §9 speculation |

Confirmed oxidizers (three total): **Oxygen, Nitrous Oxide, Ozone** — all
gases at STP. Hydrogen autoignites at 150 °C in the presence of Ozone;
Methane's combustion enthalpy doubles when Nitrous Oxide or Ozone is the
oxidizer instead of plain O₂ — rocketry favors Hydrogen + (Nitrous
Oxide/Ozone) for this reason.

---

## 6. Pollutant — ⚠️ soft spot, not a strict single compound

Community sources describe Pollutant inconsistently — "pure Chlorine" per
some, "closer to Hydrogen Sulfide" per others. Also produced by
evaporating Liquid Sodium Chloride, which is chemically odd taken
literally. Read this as: **Pollutant is a gameplay-abstracted "bad gas"
bucket**, not a substance with one settled real-world formula. Not a bug —
a spot where the simulation is intentionally loose, same category as
farming (§8), lower stakes.

---

## 7. Farming & Composting — ⚠️ the documented soft spot

**Plant inputs:** CO₂ in atmosphere (≥0.1 mol partial pressure), light
(sun or grow light), irrigation water piped to the tray, temperature 0–50
°C (optimal 20–30 °C), optional fertilizer for yield/speed.

**Plant outputs:** consumes CO₂ + water, produces O₂ + waste heat, and
over time grows harvestable biomass/food.

On its face this looks like simplified photosynthesis (6 CO₂ + 6 H₂O +
light → C₆H₁₂O₆ + 6 O₂), which would tidily explain the carbon and
hydrogen in harvested food. **But:**

- **Documented community-known issue:** plant+composter loops can be built
  to produce **net-positive water** — the accounting doesn't actually
  close in vanilla.
- A community rebalance mod, "Plants and Nutrition," patches this
  specifically by making plants **transpire 25% of consumed water back to
  the atmosphere** (Mushroom/Hades-plant exceptions), explicitly to stop
  the infinite-growth loop.
- **Takeaway:** "plants seem to create carbon and hydrogen out of
  nowhere" is a fair description of a real, known soft spot in vanilla
  Stationeers — the one place in the elemental economy visibly looser than
  ore/ice/gas, loose enough to be exploited.

❓ **Not resolved:** whether the root cause is literal mass-spawning (a
bug/oversight) vs. under-deducted consumption somewhere in plant-growth
math — both look identical from outside. Answering for certain needs
decompiling the Plant/Farming classes directly, the same treatment this
project gave `Furnace`/`Ore.Smelt` for the yield mod. Not done here —
flagged as the one place in this doc where "verify via decompile" matters
most.

**Composter:** consumes 3 organic items (any mix of biomass/food/decayed
food) → 1 Fertilizer, releasing Nitrogen and Methane gas as byproduct. No
public source documents a mass-balance relationship between specific
inputs and that byproduct composition — flag ❓, likely another simplified
output rather than a tracked reaction.

---

## 8. Element-by-element quick reference

| Element / substance | Solid form(s) | Liquid form(s) | Gas form(s) | How you get it | Sinks |
|---|---|---|---|---|---|
| **Iron** | Ore, Ingot, built items | — | — | mining | smelting → items → scrap → Recycler/Centrifuge (lossy) |
| **Oxygen** | — | — | O₂ | Ice (Oxite) melt, Electrolyzer (splits water), plant respiration (byproduct) | breathing, combustion (oxidizer), Furnace fuel |
| **Hydrogen** | — | Liquid Hydrogen 🆕 | H₂ | Electrolyzer, Ice ("Volatiles") melt (2 mol/unit) | rocket fuel, combustion → clean Water |
| **Carbon** (as CO₂ / Methane / biomass) | biomass/food items | Liquid Alcohol 🆕 (decomposes to Methane) | CO₂, Methane (ex-Volatiles) | plant CO₂ consumption/production loop, Ice ("Volatiles") melt, combustion exhaust | plant intake, Furnace fuel, Fermenter 🆕 (plants → Alcohol) |
| **Nitrogen** | — | — | N₂ | Ice (Oxite) melt (2.5 mol/unit), Composter byproduct | atmosphere filler, some reactions |
| **Water** | Ice (Water) | Water, Steam | Steam | Ice (Water) melt, Electrolyzer input, Hydrogen+O₂ clean combustion 🆕 | irrigation, Electrolyzer feedstock |
| **Helium** 🆕 | — | *none — currently gas-only* | He | Gas Trader (bought); mining/collection source ❓ unconfirmed | breathing gas gimmick (pitch shift), otherwise inert |
| **Hydrazine** 🆕 | Ice (Hydrazine) | Liquid Hydrazine | Hydrazine gas | Rocket Miner on Hydrazine asteroids, Trader | monopropellant rocket fuel |
| **Sodium Chloride / Salt** 🆕 | Salt ore | Liquid Sodium Chloride (>300 °C only) | evaporates to Pollutant (⚠️, see §6) | mining (❓ exact form), Ore Trader | — |
| **Silanol** 🆕 | — | Liquid Silanol | Silanol gas | ❓ see §9 open follow-ups | premium phase-change refrigerant/coolant — highest latent heat per mol of any reversible phase-change material; toxic above 1 kPa partial pressure |
| **Ozone** 🆕 | — | Liquid Ozone | Ozone gas | ❓ see §9 open follow-ups | one of 3 oxidizers (with O₂, Nitrous Oxide); doubles Methane combustion enthalpy vs. plain O₂; Hydrogen autoignites at 150 °C in its presence |
| **Hydrochloric Acid** 🆕 | — | Liquid HCl | HCl gas | **Venus's global atmosphere** (confirmed — see §9); also present as an impurity on traded "dirty volatiles" | toxic hazard gas; Venus base-building has to deal with it; further uses ❓ |

---

## 9. Open follow-ups (need in-game/decompile confirmation, not just wiki research)

Project owner's read going in (2026-08-17): Silanol and Ozone are
probably player-synthesized rather than mined raw; Hydrochloric Acid is
confirmed tied to Venus's atmosphere with unclear further uses. Research
below supports that read but doesn't fully close it — flagging as open.

- **Silanol — acquisition method unconfirmed.** Wiki describes its
  *properties* (premium phase-change coolant, highest latent heat/mol of
  any reversible phase-change material, critical temp 821.669 K / 548.5 °C
  — high enough to still liquify and reject heat on Venus but not Vulcan
  daytime, low liquid molar density of 6.25 mol/L capping Evaporation
  Chamber throughput at 15.625 kJ @ 0.25 L/tick, toxic above 1 kPa) but not
  where it comes from. Consistent with the "player-made, not mined"
  hypothesis — no ore/ice source turned up in research — but not
  confirmed as synthesized either; needs an in-game/Stationpedia/decompile
  check for the actual recipe or machine (Chemistry Reactor? something new
  from the Gases Update?).
  Page to save: https://stationeers-wiki.com/Silanol

- **Ozone — acquisition method unconfirmed.** No dedicated wiki page
  found; what surfaced was scattered across the Oxidizer, Fuel, and
  Hydrogen pages, all describing *use* (as one of three combustion
  oxidizers, rocketry pairing with Hydrogen) rather than *source*. Same
  "probably player-made" hypothesis as Silanol, same lack of confirmation.
  Pages to save:
  https://stationeers-wiki.com/Oxidizer ,
  https://stationeers-wiki.com/Fuel ,
  https://legacy.stationeers-wiki.com/Ozone (older wiki fork — may have a
  dedicated page the current wiki doesn't)

- **Hydrochloric Acid — source confirmed, uses still unclear.** Confirmed:
  part of **Venus's global atmosphere** (mostly CO₂ + HCl, with some N₂
  and trace Pollutant, at ~239 kPa / +460 °C), and separately obtainable
  as an impurity on traded "dirty volatiles." Matches the "new, tied to
  Venus" read exactly. What's *not* confirmed: any crafting use for it
  beyond being a hazard to manage — worth checking Stationpedia for a
  recipe that consumes it before assuming it's purely a nuisance gas.
  Pages to save:
  https://www.stationeers-wiki.com/Hydrochloric_Acid ,
  https://stationeers-wiki.com/Venus

- General index, useful for all three: https://stationeers-wiki.com/Gases_and_Liquids/Menu

---

## 10. Speculation, explicitly separated

Everything below is a guess, not sourced from a patch note, dev post, or
roadmap.

- **Helium staying gas-only is likely temporary.** Every other substance
  in the Gases Update shipped with a liquid phase, a combustion reaction,
  or both — Helium conspicuously has neither. Reads like "the
  trader/breathing gimmick gas for now," a plausible candidate for a later
  pass adding liquefaction or reactivity. No source backs this.
- **Combustion's "fully data-driven" refactor is a maturity signal.**
  Moving from one hardcoded shared model to a per-substance data table
  suggests future fuels/oxidizers can be added without another full
  system rework — the *architecture* graduated from 🆕/volatile toward
  🔒, even though individual substances inside it stay tunable.
- **The farming mass-balance soft spot (§8) is a plausible future target**
  given a community mod already patches exactly this, and the studio has
  shown a pattern of iterating on one subsystem per major update (Phase
  Change → Gases). No roadmap evidence points at farming specifically
  being next — optimistic pattern-matching, not a forecast to plan around.

---

## Sources

- [Ice (Oxite) — Stationeers Community Wiki](https://stationeers-wiki.com/Ice_(Oxite))
- [Ice (Volatiles) — Stationeers Community Wiki](https://stationeers-wiki.com/Ice_(Volatiles))
- [Ice (Water) — Stationeers Community Wiki](https://stationeers-wiki.com/Ice_(Water))
- [Ores — Stationeers Community Wiki](https://stationeers-wiki.com/Ores)
- [Farming — Stationeers Wiki (Fandom)](https://stationeers.fandom.com/wiki/Farming)
- [Guide (Farming) — Stationeers Community Wiki](https://stationeers-wiki.com/Guide_(Farming))
- [Electrolyzer — Stationeers Community Wiki](https://stationeers-wiki.com/Electrolyzer)
- [Fuel — Stationeers Wiki (Fandom)](https://stationeers.fandom.com/wiki/Fuel)
- [Gas Fuel Generator — Stationeers Community Wiki](https://stationeers-wiki.com/Gas_Fuel_Generator)
- [Biomass — Stationeers Community Wiki](https://stationeers-wiki.com/Biomass)
- [Advanced Composter — Stationeers Community Wiki](https://stationeers-wiki.com/Advanced_Composter)
- [Dirty Ore — Stationeers Community Wiki](https://stationeers-wiki.com/Dirty_Ore)
- [Stationeers Smelting Guide — XGamingServer](https://xgamingserver.com/blog/stationeers-furnace-smelting-ore-guide/)
- [Pollutant — Stationeers Community Wiki](https://stationeers-wiki.com/Pollutant)
- [Phase Change Mechanics — Stationeers Community Wiki](https://stationeers-wiki.com/Phase_Change_Mechanics)
- [The Gases Update — SteamDB patch notes (2026-03-19)](https://steamdb.info/patchnotes/22406008/)
- [The Gases Update — Steam News](https://store.steampowered.com/news/app/544550/view/491593545667313734)
- [The Gases Update — changelog.gg](https://changelog.gg/games/stationeers-544550/updates/2026-03-19-the-gases-update-7d17)
- [Hydrazine — Stationeers Community Wiki](https://stationeers-wiki.com/Hydrazine)
- [Silanol — Stationeers Community Wiki](https://stationeers-wiki.com/Silanol)
- [Oxidizer — Stationeers Community Wiki](https://stationeers-wiki.com/Oxidizer)
- [Ozone — Unofficial Stationeers Wiki (legacy fork)](https://legacy.stationeers-wiki.com/Ozone)
- [Hydrochloric Acid — Stationeers Community Wiki](https://www.stationeers-wiki.com/Hydrochloric_Acid)
- [Venus — Stationeers Community Wiki](https://stationeers-wiki.com/Venus)
- [Gases and Liquids/Menu — Stationeers Community Wiki](https://stationeers-wiki.com/Gases_and_Liquids/Menu)
- `saltys-yield-multiplier`'s own `UpdateNotes.md` / `WORKSHOP_DESCRIPTION.md` (first-party, decompile-verified ice-melt and furnace/ArcFurnace figures — separate repo, referenced not duplicated)
