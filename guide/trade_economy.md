# Trade Economy & Profit Maximization Guide

Compiled 2026-08-17. Companion to `guide/elemental_lifecycle.md` — that doc
covers where every element/substance comes from and goes to; this one
covers where to *sell* it and how to sequence a base's economy for
maximum profit. Same confidence-tag scheme as that doc, reused here for
consistency:

| Tag | Meaning |
|---|---|
| 🔒 LOCKED IN | Long-standing mechanism, stable across versions |
| 🆕 NEW (Mar 2026) | Added in the Gases Update — young, more likely to shift |
| ❓ UNCONFIRMED | Couldn't verify from public sources at the research effort spent — check in-game or decompile before relying on it |
| 🔮 SPECULATION | My own forward-looking guess, explicitly not sourced |

---

## 1. Core trading mechanics

🔒 **Setup:** a Landing Pad (Landing Pad Center kit + surrounding tiles,
3×3 minimum, expandable to 5×5/7×7/9×9) plus a Satellite Dish and Computer
to summon traders. A Credit Card must be on your character (uniform or
hand) to transact. Trading is essential on worlds with no ice (Venus,
Vulcan) and useful everywhere else as a second resource pipeline.

🔒 **Schedule:** 3 trader vessels arrive per day — one each for Close,
Medium, and Far distance tiers. Which of the ~10 trader *types* shows up
for a given tier appears to be randomized per visit (❓ exact
randomization weighting unconfirmed).

🔒 **Prices are fixed per visit** — there is no in-session demand-decay
mechanic that punishes you for selling a lot to one trader in one visit.
The "market" changes by which trader type happens to show up on the next
scheduled visit, not by you moving volume. Practical implication: don't
hold back volume to "protect the price" within a single trader visit —
that's not how this game's trade works, unlike games with live demand
curves.

🔒 **Bulk scaling is the direct profit lever tied to landing pad size.**
Stock/required quantities for bulk goods are base values at a ×1.0
multiplier, scaled by which ship size lands: a good with base stock 800
delivers **400** from a Basic/Utility ship, **800** from a Medium ship,
**1,600** from a Large ship. Landing pad size gates which ship tier can
land (3×3 → Small, 5×5 → Medium, up to 9×9 → Large) — **building the
biggest pad you can support directly doubles-then-quadruples the volume
you can move per visit**, independent of anything else in this doc.

🔒 **Weather:** regular shuttle traders (Small/Medium/Large, including gas
variants) can't land during storms; Plane traders are unaffected. Relevant
if you're automating sales and care about uptime.

❓ **Unconfirmed:** whether Far-tier trades pay meaningfully better than
Close-tier, or whether any reputation/unlock system exists that improves
prices over time. Nothing in public sources confirmed or denied this —
don't plan a strategy around it without an in-game check. Satellite Dish
size (Small/default-Medium/Large) affects **scan speed** for resolving
farther tiers, not confirmed to affect price.

---

## 2. The 10 trader types — buy vs. sell, and a structural trap to avoid

**Traders generally don't buy back what they sell.** Per the game's own
Trading Update IV notes: *"the alloy trader sells alloys but wants to buy
ores."* This is the single most important thing to internalize before
optimizing anything else — matching a good to the trader that actually
**wants** it is not the same as matching it to the similarly-named trader.

| Trader | Sells (to you) | Buys (from you) | Confidence |
|---|---|---|---|
| Alloy Trader | processed ingots, advanced superalloys (larger ships) | raw ores, specialty fuel mixes | 🔒 confirmed |
| Construction Trader | building materials, cable | **all alloy types** (diversified revenue across every alloy you can make) | 🔒 confirmed |
| Ore Trader | ore (incl. Salt 🆕) | ❓ unconfirmed — verify in-game | ❓ |
| Gas Trader | various gases | various gases; expanded Gases Update to include Helium, Hydrogen, Silanol, Ozone (Medium/Large ships) 🆕 | 🔒 sell-list, ❓ buy specifics |
| Liquid Trader | cryogenic/industrial liquids in bulk + canisters | expanded Gases Update to include Liquid Hydrogen, Liquid Hydrazine, Liquid Ozone, Liquid Hydrochloric Acid 🆕 | 🔒 sell-list, ❓ buy specifics |
| Food Trader | — | — | ❓ unconfirmed |
| Hydroponic/Seed Trader | — | — | ❓ unconfirmed |
| Hardware Trader | — | — | ❓ unconfirmed |
| Consumable Trader | — | — | ❓ unconfirmed |
| Appliance Trader | — | — | ❓ unconfirmed |

**Concrete takeaway from the confirmed rows:** raw ore's best buyer is the
**Alloy Trader**, not (necessarily) the Ore Trader — verify the Ore
Trader's buy side in-game before assuming it's a better sink. Processed
alloy's best buyer is the **Construction Trader**, and it buys *every*
alloy type, so diversifying what you smelt doesn't cost you a sink.

---

## 3. What's actually most profitable — a progression framework

The best quantitative source found for this is a community deep-dive,
**"Building a Trade Empire in Stationeers" (Summer, Stationeering
Systems, stationeering.substack.com/p/lunar-trade-economics)** — a
lunar/moon-base case study combining demand modeling, transaction-cycle
analysis, and infrastructure cost accounting. Treat its literal €/hour and
% figures as **one world's specific numbers**, not universal constants —
but its ranking and reasoning are the transferable part:

1. **Fuel production first.** Cheapest infrastructure (ice mining +
   melting, no smelting/heat management needed) and, in that analysis, the
   highest margin-per-complexity entry point: **91% margin, ~€3,632.50/hour
   profit** on trade-sourced inputs. This is the "prove viability, fund
   the next phase" business, not the endgame.
2. **Mining second**, funded by fuel profits — builds the ore-extraction
   infrastructure that phase 3 depends on.
3. **Refining last (target market, not starting point).** Alloys/ingots
   concentrate roughly **45% of total market value** in that analysis —
   the single largest value pool in the game's economy — but demand the
   most infrastructure (sustained high-temperature smelting, active heat
   management), which is exactly why it's sequenced last, not first,
   despite being the biggest prize.
4. **Automation isn't optional at scale.** In that same analysis, fuel
   trade opportunities averaged **about once every 2 minutes** — too fast
   for a human to babysit indefinitely. A companion piece by the same
   author, *"Automated Trade in Stationeers"*
   (stationeering.substack.com/p/automated-terminal-system), covers
   building an automated IC10 selling system — not yet absorbed into this
   repo (see Planned in root `README.md`), but a natural next build given
   this project's existing `logic-network-reference/` and
   `device-ic10-scripts/` IC10 expertise.

🔮 **Speculation, not from that source:** the analysis above almost
certainly predates or doesn't account for the Gases Update (nothing in
what surfaced mentions Helium/Hydrogen/Silanol/Ozone/Hydrazine as trade
goods) — its "fuel production" numbers are built on Methane/Oxite-era
economics. Whether Hydrogen's newly-liquefiable, clean-combustion profile
(see `elemental_lifecycle.md` §4–5) changes the fuel-tier math is untested
here — worth benchmarking against the classic Methane route rather than
assumed better.

---

## 4. New-since-the-Gases-Update opportunities (cross-ref: `elemental_lifecycle.md`)

These sell-list additions are five months old at time of writing —
newer than most community trade guides (including the deep-dive cited
above), so likely under-discussed/under-optimized relative to their
actual value:

- **Salt** → Ore Trader (new sell item)
- **Helium, Hydrogen, Silanol, Ozone** → sellable gas, Medium/Large Gas
  Trader
- **Liquid Hydrogen, Liquid Hydrazine, Liquid Ozone, Liquid Hydrochloric
  Acid** → Liquid Trader

Per `elemental_lifecycle.md` §9, Silanol and Ozone's *acquisition* method
(mined vs. player-synthesized) is still an open follow-up — worth
resolving before building a trade loop around either, since "sell it"
strategy depends entirely on how cheaply you can make/collect it in the
first place.

---

## 5. Practical checklist to maximize profit

1. **Build the biggest landing pad you can support.** Bulk multiplier
   (§1) scales revenue per visit directly — this is the single highest-
   leverage, lowest-complexity lever in this whole doc.
2. **Sequence infrastructure fuel → mining → refining**, not the reverse
   (§3) — refining is the biggest prize but the worst starting point.
3. **Automate selling with IC10** once volume exceeds what's manually
   sustainable (§3) — this repo already has the scripting foundation for
   it, building the actual trade terminal script is just not done yet.
4. **Sell to the trader that *wants* the good, not the same-named one**
   (§2) — Alloy Trader for raw ore, Construction Trader for alloys.
5. **Check the post-Gases-Update sell lists** (§4) before assuming an
   older guide's "best fuel/gas to sell" list is still complete.
6. **Don't ration volume within one trader visit** — prices are fixed per
   visit (§1), so there's no benefit to holding stock back from a single
   sale the way you might in a game with live demand curves.
7. **Watch weather for automation uptime** — Plane traders keep working
   during storms when shuttle traders can't land (§1).

---

## Aside: relationship to `saltys-yield-multiplier` (kept separate, per this repo's vanilla-first convention)

`saltys-yield-multiplier` (its own repo) multiplies Furnace/Advanced
Furnace/Arc Furnace ingot yield and ice-melt gas yield, live-adjustable,
both default 5×, touching neither mining nor difficulty. Fuel production
(ice-melt gas) and refining (ingot yield) are exactly the two categories
§3 identifies as the cheap-entry and highest-total-value legs of this
strategy — so that mod, if run, would directly amplify both without
changing the sequencing logic above. Noted as a cross-reference only: the
analysis in this doc stands entirely on vanilla mechanics, consistent with
this repo's "vanilla is the default" stance in the root `README.md`.

---

## Sources

- [Trading Guide — Stationeers Wiki (Fandom)](https://stationeers.fandom.com/wiki/Trading_Guide)
- [Guide (Trading) — Stationeers Community Wiki](https://stationeers-wiki.com/Guide_(Trading))
- [Trader — Stationeers Community Wiki](https://stationeers-wiki.com/Trader)
- [Kit (Landing Pad) — Stationeers Community Wiki](https://stationeers-wiki.com/Kit_(Landing_Pad))
- [Satellite Tracking — Stationeers Community Wiki](https://stationeers-wiki.com/Satellite_Tracking)
- [Steam — The Trading Update IV (patch notes)](https://steamcommunity.com/games/544550/announcements/detail/3675535022079397997)
- [Steam — The Trading Update V (SteamDB patch notes, 2023-03-05)](https://steamdb.info/patchnotes/10695982/)
- [GitHub — Intergalactic-Carpet/StationeersTraders (auto-generated trader data dump)](https://github.com/Intergalactic-Carpet/StationeersTraders)
- ["Building a Trade Empire in Stationeers" — Summer, Stationeering Systems](https://stationeering.substack.com/p/lunar-trade-economics)
- ["Automated Trade in Stationeers" — Summer, Stationeering Systems (companion piece, not yet absorbed into this repo)](https://stationeering.substack.com/p/automated-terminal-system)
- `guide/elemental_lifecycle.md` in this repo (source material this doc cross-references for acquisition costs)
