# Furnace Alloy Reference — Temperature & Pressure Windows

**Source: `recipes.csv` from `roastduckie/Stationeers-Furnace-Plot`**
(https://github.com/roastduckie/Stationeers-Furnace-Plot/blob/master/recipes.csv),
a community furnace-calculator project built specifically to help players
avoid blowing up their furnace — structured recipe data, not a forum
summary or guess. Fetched 2026-08-07. Community-sourced, not decompiled —
treat as good-confidence, not ground truth; worth a live Stationpedia
cross-check per alloy before this drives real furnace control, same
caveat this project already applies to the phase-separator's condensation
data.

## Full table

| Alloy | MinTemp (K) | MaxTemp (K) | MinPressure (kPa) | MaxPressure (kPa) | Materials |
|---|---|---|---|---|---|
| Silicon | 900 | 99999 | 100 | 99999 | 1x Silicon Ore |
| Iron | 900 | 99999 | 100 | 99999 | 1x Iron Ore |
| Gold | 600 | 99999 | 100 | 99999 | 1x Gold Ore |
| Copper | 600 | 99999 | 100 | 99999 | 1x Copper Ore |
| Silver | 600 | 99999 | 100 | 99999 | 1x Silver Ore |
| Lead | 300 | 99999 | 100 | 99999 | 1x Lead Ore |
| Nickel | 800 | 99999 | 100 | 99999 | 1x Nickel Ore |
| Steel | 600 | 99999 | 100 | 99999 | 3:1 Iron:Coal |
| Solder | 300 | 2000 | 300 | 3500 | 1:1 Iron:Lead |
| Electrum | 700 | 10000 | 800 | 2400 | 1:1 Silver:Gold |
| Waspaloy | 875 | 1000 | 1250 | 2750 | 2:1:1 Nickel:Lead:Silver |
| Hastelloy | 950 | 1000 | 2500 | 3000 | 2:1:1 Nickel:Silver:Cobalt |
| Constantan | 1000 | 1500 | 100 | 10000 | 1:1 Copper:Nickel |
| Inconel | 1150 | 1250 | 4250 | 4750 | 2:1:1 Nickel:Gold:Iron |
| Astroloy | 1200 | 1400 | 5000 | 6000 | 2:1:1 Iron:Copper:Cobalt |
| Invar | 1200 | 2000 | 6000 | 7000 | 1:1 Iron:Nickel |
| **Stellite** | **1700** | **1900** | 4000 | 5000 | 2:1:1 Cobalt:Silver:silicon |

## What this means for the working-gas loop

**Stellite has the highest temperature floor of anything in this table
(1700K), not Constantan or Inconel** — Inconel is actually one of the
*lower*-temperature alloys here (1150-1250K), just narrow-banded and
high-pressure. If the working-gas loop needs to be capable of running
every alloy in this table, 1700K+ is the real design floor for peak
loop temperature (with margin, probably ~1800-2000K as an actual
target), not the ~1500K originally guessed.

**Several alloys have a real ceiling too, not just a floor** — Hastelloy
and Waspaloy both cap at 1000K, Inconel at 1250K. A loop hot enough for
Stellite would blow straight through those windows. This is why the
control scheme (see `topology_notes.md`) needs to modulate furnace
temperature per-alloy rather than just running the hottest gas
available at all times — confirmed as the intended design (project
owner, 2026-08-07): "i want it configurable."

**Pressure windows vary just as much as temperature** — Hastelloy needs
2500-3000 kPa, Invar needs 6000-7000 kPa, Constantan tolerates almost
anything (100-10000 kPa). The control scheme has to hit both windows
simultaneously, not just temperature.
