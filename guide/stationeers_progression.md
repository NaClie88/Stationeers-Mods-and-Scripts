## **DOCUMENT ASSUMPTIONS**
*Read this first. The framework below makes specific choices about starting conditions and environment — change these and the priority order changes with them.*

1. **Two starting conditions are covered:** the **Standard/Normal start** (full two-crate lander, covered in Tier -1 Projects A1/A2) and the **Brutal start** (single minimal crate, covered in Projects A3/A4). "Brutal" is a *starting condition* selected at world creation, independent of the Easy/Normal/Stationeer *difficulty* slider — you can pair Brutal with any difficulty, though most hardcore players pair it with Stationeer difficulty.
2. **Default reference world is Mars** unless you configure otherwise — it's the game's own recommended first world (thin atmosphere, moderate temperature swings, no extreme hazards) and every priority order above this note assumes Mars-like conditions.
3. **Solo player** is assumed throughout. Multiplayer changes crate math (each player brings their own emergency supplies) but not the underlying priority logic.
4. **See "Environment Configuration by World"** (below the Build Priority Order) to adjust the priority sequence for your actual landing site — hot worlds, cold worlds, vacuum worlds, and thin-atmosphere worlds each demand a different early ordering, and that section tells you exactly what to move up or down.
5. **Vanilla is the default supported build, not a mandatory sequence.** Every project in this guide works standalone on pure vanilla Stationeers — no step assumes a mod is present. This isn't about requiring anyone to "finish vanilla, then start mods"; it's compatibility: most people this gets shared with won't have every mod installed, so the vanilla path has to be complete and correct on its own. Mods (like Re-Volt) are a separate, optional configuration layered on top for those who do have them, tracked in `mods.json` — never silently substituted into the base build.

---


## **VERIFIED STARTING LANDER MANIFEST**
*Confirmed on Normal difficulty (non-Vulcan/Venus start). Difficulty settings change resource consumption rates, respawn conditions, and whether you must open your helmet to eat/drink — they do NOT change what's in the crates. Source: Stationeers Community Wiki, "Starting Gear."*

**On your person:** Helmet, Glasses, EVA Suit (Air canister 5700 kPa, empty Waste canister, charged Large Battery, 3 CO₂ Filters), Jetpack+Backpack (Propellant canister, medical pill, Tablet w/ Tracker + Network Analyzer + Atmos Analyzer cartridges, Mining Belt w/ Drill + Pickaxe + 7 ore slots, 20 Road Flares, Portable Solar Panel), Orange Uniform (Tomato Soup, Water Bottle, Credit Card €0), Toolbelt (Wrench, Crowbar, Hand Drill, Wire Cutters, Welder, Screwdriver, Angle Grinder, 50x Cable Coil), Duct Tape.

**Construction Supplies 1:** Kit(Arc Furnace), Kit(Autolathe), 30x Iron Frames, 50x Iron Sheets, 50x Plastic Sheets, 2x Power Controller, Kit(Solid Generator), 30x Kit(Iron Wall), Kit(Solar Panel), 50x Glass Sheets.

**Construction Supplies 2:** Kit(Active Vent), Kit(Console), 2x Kit(Door) [airtight], Circuitboard(Airlock), 2x Pipe Valve, charged Large Battery, Data Disk, Kit(Sensors), Battery Charger, 20x Construction Kit(Pipe).

**Consumable Supplies:** 20x Road Flare, Circuitboard(Advanced Airlock), 10x Kit(Small Flag), 5x Spray Paint, Labeller, Tracking Beacon.

**Organic Supplies 1:** Egg Carton (6 Fertilized Eggs), 3x each Potato/Wheat/Corn/Fern/Tomato/Pumpkin/Soybean/Mushroom/Rice.

**Residential Supplies:** 3x Water Bottle (filled), Tomato Soup, Corn Soup, 2x Kit(Tables), 2x Kit(Locker), Microwave, Ore Scanner cartridge, Ground Penetrating Radar.

**Portable Appliance Kits:** Kit(Portable A/C), Kit(Portable Scrubber), Kit(Portable Hydroponics), Kit(Portable Generator), Liquid Canister (Water, 3807 kPa), 2x Small Battery, 2x Duct Tape, Wrench.

**Also on the Lander itself:** Portable Tank with 7576 kPa Oxygen.

**Critical correction to earlier version of this document:** The starting crates already contain 30 Iron Frames, 50 Iron Sheets, 30 Iron Wall kits, a Solar Panel kit, a Solid Generator kit, 2 Doors, an Airlock circuit board, Sensors, and 20 Pipes. **You do not need to mine or fabricate anything to build your first sealed shelter** — that material is handed to you. Mining on Day 1 exists to fund what ISN'T in the crate: the Electronics Printer, Hydraulic Pipe Bender, and steel reserves for expansion beyond the starter kit.

---


## **FABRICATION TIER REFERENCE**
*Critical context: every project below assumes fabrication tools that don't exist on landing. This table maps what unlocks what.*

| Tier | Machine | How You Get It | Unlocks |
|------|---------|----------------|---------|
| **T1 (Free)** | Autolathe | Included in starting crates, just place + power it | Basic frames, tools, Electronics Printer kit, Hydraulic Pipe Bender kit |
| **T1 (Free)** | Arc Furnace | Included in starting crates | Smelts raw ore → ingots (Iron, Copper, Gold, Steel via Iron+Coal) |
| **T1 (Built)** | Electronics Printer | Built via Autolathe — confirmed cost includes 2g Gold ingot, plus Iron + Copper (exact iron/copper amount not wiki-confirmed at time of writing — carry extra of both) | Cables, batteries, solar panels, circuit boards, Mod Kits |
| **T1 (Built)** | Hydraulic Pipe Bender | Built via Autolathe — confirmed cost includes 2g Gold ingot, plus Iron + Copper | Pipes, atmospherics parts, water/gas handling components |
| **T2 (Upgrade)** | Autolathe Mk2 / Electronics Printer Mk2 | Apply Mod Kit (from T2 Electronics Printer) + Screwdriver | Faster printing, advanced recipes (Advanced Furnace, etc.) |
| **T2 (Built)** | Fabricator | Built via T2 Electronics Printer (needs Iron/Gold/Copper) | Advanced components, needs separate Computer + Motherboard to run queues |
| **T2+ (Built)** | Advanced Furnace | Recipe unlocked via T2 Electronics Printer | Alloys (Steel, advanced materials) at higher volume/purity |
| **T3 (Built)** | Rocket Manufactory, Security Printer, etc. | Built from Fabricator chain | Rocket parts, advanced defense/automation items |

**Practical implication:** Projects C1, C2, F1, F2 (Energy, Fabrication, Storage, Mining) all assume the Autolathe → Electronics Printer → Fabricator chain is already built. That chain itself is the true Tier 0 starting task and is covered below.

---


## **PRIORITY INDEX — Recommended Order**
*This is the reading/execution order. Categories are lettered by real-world urgency, not by theme — Category A is what kills you fastest if skipped, Category P is the least time-pressured. Multi-project categories list their internal order too. **Milestone 1** below marks the point where your base becomes self-sufficient — it pulls specific projects forward out of their normal category rank; see that section for the full breakdown.*

| # | Category | Projects | Why This Rank |
|---|----------|----------|----------------|
| 1 | **A** — Landing Day | A1→A2 (Normal) or A3→A4 (Brutal) | The literal starting point — nothing else is possible first |
| 2 | **B** — Immediate Survival | B1, B2, B3, B4 + conditional B5–B9 | Shelter, water, air, power — die without these in hours. B5–B9 are placeholder slots that only activate for hot/cold/zero-g/weak-solar/high-pressure worlds — check the Environment Configuration table |
| 3 | **C** — Core Infrastructure | C1, C2 | Energy + Fabrication gate almost everything downstream |
| 4 | **D** — Sustained Survival | D1→D1b, D2 | D1 (minimum hydroponics) is part of Milestone 1; D1b (full food/O₂ system) comes later. Storms hit within 7 days |
| 5 | **E** — Continuity Safeguards | E1, E2 | Set your respawn point before a bad death scatters your gear |
| 6 | **F** — Resource Scaling | F1, F2 | Grow mining/storage past Day 1 baseline |
| 7 | **G** — Climate Control | G1 (Milestone 1) → G2 → G3 → G4 | G1 is the minimum-viable temperature band for Milestone 1; G2/G3/G4 build the full automated system afterward |
| 8 | **H** — Airlock Automation | H1 (Milestone 1) → H2 → H3 | H1 (gas-conserving console airlock) is pulled into Milestone 1 — recurring gas loss is too costly to defer. H2/H3 remain later upgrades |
| 9 | **I** — Field Readiness | I1, I2 | Spares and mobility — protects against losses, not day-to-day survival |
| 10 | **J** — Atmospheric Refinement | J1, J2 | Gas purity and waste handling — quality-of-life at this point |
| 11 | **K** — Organization | K1 | Zero cost, zero dependencies — genuinely could move earlier if you want it sooner |
| 12 | **L** — Automation & Efficiency | L1, L2 | Optimization, not necessity |
| 13 | **M** — Economy | M1 | Trade — unlocks convenience purchases, not survival |
| 14 | **N** — Rocketry | N1→N2 | Long-horizon end-game goal |
| 15 | **O** — Legacy & Memorial | O1 | Purely aesthetic/roleplay milestone |
| 16 | **P** — Resilience | P1 | Hardening what you've already built — last because there's nothing to harden until the rest exists |

**Reading this table:** work top-to-bottom. Within a category, follow the listed project order (e.g., Category H is a strict chain: H1 before H2 before H3). Categories without arrows (like B or D's first pair) can have their projects done in either order or in parallel — check each project's own dependencies if unsure. **Milestone 1 is reached once B1–B4, H1, G1, D1, and D2's baseline are all complete** (plus any conditional B5–B9 for your world) — at that point you have a genuinely self-sufficient base, even though most categories are still incomplete.

---

## **MILESTONE 1: MINIMUM VIABLE BASE**
*The first real target. This is not "finish every category up to here" — it's a specific cross-cutting checklist that pulls individual projects out of their categories, some earlier than their category's normal priority rank, because together they define the point where your base stops being a campsite and starts being self-sufficient. Starting rations (food/water/O₂ from the landing crate) cover you until this point — Milestone 1 is what replaces them with something renewable.*

**The checklist:**

| Requirement | Project | Why this one, not the full category |
|---|---|---|
| **Automated airlock, not losing gas** | **H1** (Console-Controlled Airlock) | B1's manual double-door works but vents on every cycle. H1 adds console-driven vent control so cycling doesn't dump your whole atmosphere each time — pulled forward from Category H because gas loss is a recurring cost, not a one-time inconvenience. Full IC-based automation (H2/H3) is NOT required yet. |
| **Temperature-stable base** | **G1** (Thermal — Minimum Viable) | Interior held 18–25°C for a full day/night cycle, even manually toggled. Full automated climate control (G2–G4) is NOT required yet. |
| **Water established** | **B2** (Hydration System) | Continuous source (dispenser, or the closet+bottle technique), not just the starting Water Bottles. |
| **Hydroponics minimum built** | **D1** (Hydroponics — Minimum Viable) | At least one working tray with a confirmed grow cycle. Full multi-tray O₂/CO₂-contributing greenhouse (D1b) is NOT required yet. |
| **Continuous power** | **B4** (Emergency Power) | At least one stable source with battery backup — this is the floor; C1's multi-source redundancy is a later category, not required for Milestone 1. |
| **Breathable atmosphere maintained** | **B3** (Basic Atmosphere Control) | O₂ ≥19%, CO₂ managed, sustained — not just the Day 1 patch job. |
| **Structure sealed against the first storm** | **D2** (Storm & Weather Protection) — baseline only | Storms are documented to begin within 7 days of landing. Milestone 1 requires your shelter survive one, not the full storm-protection project. |
| **[Conditional] Environment adaptation** | **B5/B6/B7/B8/B9** (whichever applies) | If you're on a hot, cold, zero-g, weak-solar, or high-pressure world, that placeholder project is part of Milestone 1 too — check the Environment Configuration table for your world. B9 (pressure) is easy to miss since it's not obviously fatal like heat or cold — it fails quietly, as a wall bursting mid-storm. |

**What's deliberately NOT in Milestone 1:** Full Category C (multi-source energy redundancy), Category E (cloning/mortuary), Category F (scaled mining/storage), full Category G (automated climate), full Category H (IC-based advanced airlock), and everything from Category I onward. Those matter, but a base can be genuinely self-sufficient without them — Milestone 1 is deliberately the *minimum*, not the *complete* early game.

**Sanitation note:** waste handling (CO₂ filters, waste canister) is covered by the starting EVA Suit kit and isn't a separate build requirement — nothing to add here unless your waste canister capacity becomes a bottleneck later (that's a Category F/J concern, not Milestone 1).

---

## **ROOM STANDARDS: What Makes a Valid Room**
*Milestone 1 and the Category system measure your BASE overall. This measures a single ROOM — useful because not every room needs to hit the same tier. Your greenhouse might sit at R1 forever while your core habitat reaches R3. Confirmed against current game mechanics (Community Wiki "Room" page and related mechanics threads).*

**A note on efficient evacuation (relevant at every tier below):** pulling a room down to vacuum is not linear. The first 90% of the air comes out quickly, but the last 10% can take 200–400% longer (confirmed via official patch notes) as the active vent's pull rate tapers off near-empty. Confirmed fix: run a second extraction point in a different spot in the room (a second Active Vent, or toss a Portable Air Scrubber in as a mobile second point) rather than waiting on one vent to finish — this is faster than it looks like it should be, and worth doing by default rather than only when impatient. Powered Vent / Large Powered Vent (2×/4× the pull strength of a standard Active Vent) are worth building once available for the same reason.

---

### **Tier R0 — Base Game Requirement**
*The literal minimum for the game engine to recognize the space as a Room at all. This is not livable — it's the floor.*

- [ ] **Enclosed on all 6 sides.** Confirmed: a single cube enclosed by 6 Frames counts as a Room for detection purposes — but detection ≠ airtight (see below).
- [ ] **Airtight boundary, not just enclosed.** Bare Frames do NOT hold pressure — only once Sheets are welded onto every boundary face (finished Walls/Windows/Doors) does the room actually hold gas. A Room the game "sees" and a Room that holds pressure are two different bars.
- [ ] **Under the size cap.** Confirmed room size limit is ~1200 cubes — beyond that, the game stops checking enclosure and treats it as outside (storms occur inside it even if you've built a custom atmosphere in there).
- [ ] **Doors/portals count as boundaries regardless of build stage** — even an unfinished or open door separates rooms for detection purposes, which matters if you're troubleshooting why two spaces aren't merging into one atmosphere.

**This tier is a checkpoint, not a destination.** A room that only meets R0 will register as a room but won't reliably hold breathable air for long — move to R1 before relying on it.

---

### **Tier R1 — Minimum Livable Room**
*Maps to Milestone 1's per-room requirements. This is "safe to occupy," not "comfortable."*

- [ ] All R0 requirements met, verified by an actual pressure hold test (fill it, walk away, come back — did it hold?)
- [ ] At least a manual airlock (B1) — two doors with a buffer, so entering doesn't vent the room
- [ ] Breathable atmosphere established (B3) — O₂ ≥19%, confirmed ~16 kPa O₂ partial pressure minimum to safely pull your helmet
- [ ] Temperature within survivable band, even if only passively (G1 minimum-viable)
- [ ] Basic lighting

**CO₂ note:** fixed CO₂ scrubbing is now an R2 requirement (see below), not R1 — but CO₂ buildup is still a real risk at this tier since R1 has no formal scrubbing yet. Stopgap: keep a Portable Air Scrubber with a CO₂ filter on hand and run it manually until you reach R2's fixed installation — don't go without CO₂ handling entirely just because it's not on the R1 checklist.

---

### **Tier R2 — Practical / Sustained Room**
*The room stops needing constant manual attention.*

- [ ] **CO₂ scrubbing, fixed installation** (a filtration unit with a CO₂ filter, wired in — not the portable stopgap from R1) — unscrubbed CO₂ buildup is a documented "silent killer" in sealed rooms, and this is the first thing R2 should add
- [ ] Console-controlled airlock (H1) — gas-conserving cycling, not manual double-door venting every time
- [ ] Automated thermostat (G2/G3 as applicable to your world) — heating/cooling responds to sensor readings, not manual toggling
- [ ] Gas and pressure sensors actively placed and monitored, not just "set once and forget"
- [ ] Storm-sealed (D2 baseline) — confirmed to survive a storm without breach
- [ ] Waste gas routed to a proper destination, not just vented indiscriminately

---

### **Tier R3 — Fully Implemented Room**
*What you asked for explicitly. This is the ceiling, not a requirement for most rooms — reserve it for your core habitat, not every closet.*

- [ ] **Automated lighting** — motion-sensor driven (L1 Power Automation), not switches
- [ ] **Full automated atmospheric control** (G4) — heating and cooling unified under one thermostat loop, unattended, holding <5°C fluctuation over 12 hours
- [ ] **IC-based advanced airlock** (H2) — auto-compares pressure/composition on both sides before cycling, not just console-driven
- [ ] **Emergency airlock / hull-breach failsafe** (H3) — this is the specific piece you called out. Confirmed mechanic: once powered, an airlock circuit locks its doors in whatever state they're in, and that lock persists through power loss — it does not passively force doors closed on its own. The actual failsafe is active, not passive: gas/pressure sensors plus IC10 logic that detects a mismatch across a doorway and re-locks/closes it in response before a breach can propagate. See Project H3 for the corrected build (including the dedicated-power and true-manual-bypass requirements), and the Steam Workshop "Emergency Bulkhead Lockdown" reference (ID: 2258102536) already cited in this document's Workshop Resources table — that script is built exactly for this active-detection role.
- [ ] **Manual override present** even with full automation — per H3's confirmed mechanic, this means a second door that's never wired into the airlock circuit at all (so it's never locked), not a "manual override" on the automated doors themselves — a locked door stays locked through power loss, Crowbar included, so the only real override is a genuinely separate route
- [ ] **Redundant sensor coverage** — pressure, gas composition, and temperature all monitored with alerting (not just readable on request), so a slow leak or drift gets caught before it's an emergency
- [ ] **Structural rating verified against your world's actual pressure differential** (B9, high-pressure worlds only) — a fully automated room is still only as safe as the walls holding it together

**Reference:** Room detection/size/airtightness mechanics confirmed via Community Wiki "Room" page and Steam Community discussion threads on room size limits and wall airtightness behavior; active vent taper-off and multi-point evacuation fix confirmed via official patch notes (Update v0.2.4294.19984) and Steam Community "Pulling a vacuum" thread; Portable Air Scrubber specs (250L tank, 20.5W/tick, dual-filter speed doubling) from Community Wiki "Portable Air Scrubber" page.

---


## **CATEGORY A: LANDING DAY — Day 1 / Night 1 Bootstrap**
*The true starting point. Everything else depends on this.*


### **Project A1: Day 1 Shelter Placement & Mining Run**
**Success Criteria:**
- [ ] Both starting Construction Supply crates unloaded and inventoried
- [ ] Sealed shelter placed using ONLY crate-provided kits (no mining required for this part)
- [ ] Minimum viable ore stockpile gathered before nightfall (for Night 1 fabrication, not shelter)
- [ ] Return path to lander marked/known (Tracking Beacon deployed at base)
- [ ] Ice types identified and separated (Oxite, Volatiles, Water Ice, Nitrice are NOT interchangeable)

**Mining List (Priority Order) — Justification Cited Below:**
1. **Iron Ore** — 150g minimum (matches documented first furnace batch), more is always useful — cited: Community Wiki Guide (Mining), "Steel age"
2. **Coal** — 50g minimum (3:1 iron:coal ratio for the first furnace run) — same source
3. **Ice (Oxite)** — 1–2 chunks (or 2 if cold, e.g., Europa) — furnace fuel component
4. **Ice (Volatiles)** — 2–4 chunks (or 4 if cold) — furnace fuel component, burns hot
5. **Copper Ore** — "a stack or two" (50–100g) — cited: "Iron age" priority tier, used heavily downstream for cable/electronics
6. **Silicon** — "a stack or two" — cited: "Iron age" priority tier, needed for glass sheets and later water filtration
7. **Gold Ore** — opportunistic, 1 stack if found — cited: "Steel age" tier, "if you stumble at gold, silver, or lead — have a stack or two" (NOT worth a dedicated detour)
8. **Ice (Water)** — per Wiki: each player consumes ~4 Water Ice pieces per game day, and ~1 Oxite-equivalent for oxygen per day — mine accordingly for your play session length, on top of the 3 starting Water Bottles

**Checkpoints:**
- A1.1: Unbolt/wrench both yellow Construction Supply crates from lander
- A1.2: Place Iron Frames + Iron Walls + Door kits from crate to form a sealed room (zero mining needed)
- A1.3: Place Kit(Solar Panel) with a Glass Sheet (from crate stock, not mined) oriented for morning sun
- A1.4: Deploy Tracking Beacon at shelter before heading out to mine
- A1.5: Equip Mining Belt, head out, mine Iron/Coal/Oxite/Volatiles per list above (minimum viable), then Copper/Silicon/Gold opportunistically
- A1.6: Collect ice chunks in mining belt's dedicated ore slots (not loose inventory) to slow melting
- A1.7: Return to shelter before sunset with haul

**Note:** Ice melts above 0°C or in direct sunlight even on airless worlds — this is why smelting happens at night/in shade, not during the mining trip.

**Hydration warning:** Thirst can fully deplete in as little as 13–35 real-time minutes depending on body temperature (see Project B2 for the exact table) — potentially faster than your Day 1 mining loop takes. Carry your starting Water Bottle and drink from it mid-trip if needed; don't assume you can wait until you're back at the lander.

---


### **Project A2: Night 1 Fabrication Sequence**
**Success Criteria:**
- [ ] Autolathe placed, powered, and producing basic items
- [ ] Arc Furnace placed and first smelting batch complete (Steel)
- [ ] Electronics Printer built (unlocks power/cable/solar production beyond starter kit)
- [ ] Starter shelter sealed and holding pressure (using crate materials, not mined ones)
- [ ] Solar panel wired to a battery for morning charge

**Fabrication List (Priority Order):**
1. Place **Autolathe** on a frame, power via starting Large Battery, power it on
2. Place **Arc Furnace** nearby (accessible top, output won't roll away)
3. Smelt first batch: **150g Iron + 50g Coal + 1–2 Oxite + 2–4 Volatiles** → Steel (documented minimum to reach ~2000K, the temperature needed for steel)
4. Pull "Open Mold" lever → collect Steel ingots
5. Wire Solar Panel (already placed Day 1) → Power Controller (in crate) → Battery
6. Build **Electronics Printer** via Autolathe using smelted Iron/Copper/Gold (this is the actual resource-gated task — everything else tonight was crate-supplied)
7. If ore surplus remains, build **Hydraulic Pipe Bender** the same way

**Checkpoints:**
- A2.1: Place and power Autolathe
- A2.2: Place Arc Furnace, load first smelting batch, pull mold lever
- A2.3: Confirm shelter is sealed (crate walls/doors — verify pressure holds)
- A2.4: Wire Solar Panel → Power Controller → Battery for morning charge
- A2.5: Build Electronics Printer via Autolathe (Iron+Copper+Gold ingots)
- A2.6: IF surplus ore remains: build Hydraulic Pipe Bender
- A2.7: IF ore was insufficient tonight: note shortfall on Goal Board, prioritize that ore type on Day 2

**Reference:** Community Wiki "Guide (Mining)" and "Beginner's Guide" confirm the Autolathe → Arc Furnace → Electronics Printer → Hydraulic Pipe Bender sequence and the 150 Iron / 50 Coal / 1-2 Oxite / 2-4 Volatiles furnace figures cited above.

**Feeds into:** Completing A1 and A2 satisfies the prerequisites assumed by Projects B4 (Emergency Power) and C1–C2 (Energy Infrastructure, Fabrication Setup) below.

---


## **BRUTAL START VARIANT**
*"Brutal" is a single-crate minimal starting condition, confirmed via official patch notes as "the bare minimum needed for survival" — no premade shelter, and (confirmed via community reports) no starting Door kits. Exact itemized crate contents are not fully published by the developers at time of writing; hover over your crate in-game to confirm your exact loadout. The list below is assembled from confirmed patch-note facts and verified community reports, flagged where uncertain.*

**Confirmed Brutal crate facts:**
- Single crate (vs. Normal's 6+ crates) — "bare minimum needed for survival"
- Iron Frames ARE included (confirmed: community guide describes constructing a foundation from starting-crate frames)
- Door kits are NOT included by default (confirmed via community forum report)
- Active Vent was removed and replaced with a Power Controller in a later patch (confirmed patch note)
- World-specific supplements exist: Europa Brutal includes a Wind Turbine (solar is unreliable there); Vulcan/Venus Brutal include an Oxygen tank + 12L Water canister (no ice available on those worlds)
- Autolathe and Arc Furnace are still present — every Brutal playthrough confirms building these first


### **Project A3: Brutal Day 1 — Immediate Mining Run**
**Success Criteria:**
- [ ] Crate unpacked and inventoried (expect far less than Normal start)
- [ ] Minimum smeltable ore gathered before dark — there is no premade shelter buffer to fall back on
- [ ] Temporary shelter identified (natural terrain/cave) if base isn't sealed by nightfall

**Mining List (Priority Order) — same core targets as Normal, sourced from community "Surviving Every Planet, Brutally" guide, which confirms this list holds roughly constant across all worlds:**
1. **Iron Ore** — 3 stacks (150g) — frames, steel, foundation (you have far fewer premade frames than Normal start, possibly zero spare)
2. **Copper Ore** — 2 stacks (100g) — cable/electronics, needed sooner since you can't lean on crate spares
3. **Coal** — 50g — furnace fuel
4. **Gold** — "some," opportunistic — Electronics Printer gating resource
5. **Silicon** — "some" — glass sheets (you likely do NOT have 50 pre-made Glass Sheets like Normal start)
6. **Ice (Oxite + Volatiles)** — "some" of each — furnace fuel, breathable-mix fallback

**Checkpoints:**
- A3.1: Unpack the single Brutal crate, inventory contents against confirmed list above
- A3.2: If no Door kits present, plan a wrench-hatch or temporary sealed-frame entry instead
- A3.3: Mine iron/copper/coal/ice immediately — do not delay, there's no fallback shelter buffer
- A3.4: Watch hydration/hunger far more closely than Normal start — Brutal is nearly always paired with Stationeer difficulty's faster depletion rates
- A3.5: Return before dark with enough for at least a partial furnace batch


### **Project A4: Brutal Night 1 — Fabrication Under Scarcity**
**Success Criteria:**
- [ ] Autolathe + Arc Furnace placed and powered
- [ ] First furnace batch complete, even if partial
- [ ] Minimum sealed room achieved (may require improvised sealing without Door kits)
- [ ] Solar Panel or equivalent power source online

**Fabrication List (Priority Order):**
1. Place Autolathe + Arc Furnace (crate-supplied, free)
2. Smelt whatever ore was gathered — do not wait for the "ideal" 150/50/2/4 ratio (see Poor-RNG Contingency Rule below, it applies doubly here)
3. Fabricate Iron Frames + Sheets from smelted iron (you likely have few or none pre-made, unlike Normal start)
4. Fabricate or improvise a sealed entry (if no Door kit: a removable wall panel or airlock built entirely from fabricated parts)
5. Power setup (Solar Panel if included, or whatever the world-specific Brutal supplement provides — e.g., Wind Turbine on Europa)
6. Electronics Printer only once basic shelter and power are stable — this is a later priority on Brutal than on Normal, since Normal's premade shelter frees up Night 1 for it

**Checkpoints:**
- A4.1: Place and power Autolathe + Arc Furnace
- A4.2: Smelt first batch (partial is fine)
- A4.3: Fabricate frames/sheets for a minimum 2x2 sealed shelter
- A4.4: Seal the entry point by whatever means available (kit door if present, improvised panel if not)
- A4.5: Get any power source online before the battery in your suit runs low
- A4.6: Defer Electronics Printer to Night 2 if Night 1 resources were insufficient — do not sacrifice shelter integrity to rush it

**Reference:** Steam Community guide "Surviving Every Planet, Brutally" (ID: 3394899504) and official dev patch notes (v0.E1025.22811, "Brutal Starts" update) confirm the single-crate minimal loadout and the universal Day 1 mining targets above.

---


## **RESOURCE-RESILIENT BUILD PRIORITY ORDER**
*Not every map generation is good. Some worlds have thin, scattered, or distant ore veins. This order guarantees a productive cycle even with a poor mining haul — it's sequenced by resource-independence first, so a bad night still ends with real progress instead of a stalled queue.*

| Priority | Task | Resource Requirement | Why This Order |
|----------|------|----------------------|-----------------|
| **P0** | Place shelter from crate kits (frames/walls/doors) | **None mined** — crate-supplied | Always completable regardless of map RNG. Do this first, every time. |
| **P1** | Place Solar Panel + wire to battery | **None mined** — crate-supplied | Free power setup; do this before any mining trip so you return to charge already flowing. |
| **P2** | Place Autolathe + Arc Furnace | **None mined** — crate-supplied | Machines are free; only their FUEL (ore) is mined. Set up the machines even with zero ore in hand. |
| **P3** | First furnace batch (whatever Iron/Coal/Ice you actually found) | **Partial ore OK** | Smelt whatever you have — a half batch of Steel is still progress. Don't wait for the "ideal" 150g/50g ratio if the map didn't give it. |
| **P4** | Electronics Printer | **Iron + Copper + Gold ingots (any amount smelted)** | If gold is scarce, this is your bottleneck — prioritize scouting for gold veins over repeat iron runs once iron/copper are sufficient. |
| **P5** | Hydraulic Pipe Bender | **Iron + Copper + Gold ingots (surplus after P4)** | Lower priority than Electronics Printer — atmospherics can wait a cycle; power/cables can't. |
| **P6** | Expand steel stock beyond crate supply | **Ongoing Iron+Coal mining** | Only relevant once P0–P5 are done; this is a background task for every subsequent mining trip, not a blocker. |

**Poor-RNG contingency rule:** If a mining cycle yields less than the minimum furnace batch (150g Iron / 50g Coal), do NOT wait for a "complete" batch before smelting — partial batches still produce usable ingots, just less Steel per pull. Banking ore for a "perfect" batch wastes a night's fabrication window. Smelt what you have, fabricate what that allows, and let the next cycle top up the deficit. This keeps every single night productive even on a bad map.

---


## **ENVIRONMENT CONFIGURATION BY WORLD**
*The P0–P6 order above assumes Mars-like conditions (per Document Assumptions). Every other world has a hazard that must be inserted into the priority order, usually before P3. Find your world below and apply its modification.*

| World | Hazard Profile | Confirmed Conditions | Priority Modification |
|-------|----------------|----------------------|------------------------|
| **Mars** | Thin atmosphere, moderate temp swings, no extremes | Day ~20°C, night -53°C, thin CO₂/N₂/Ar atmosphere usable for pressurization, occasional dust storms damage exposed items/solar panels | **Baseline — no modification.** This is the reference world the P0–P6 order was written for. |
| **Moon** | Vacuum + partial gravity | No atmosphere at all (full vacuum), reduced gravity, ice deposits present, solar storms spike heat and suit O₂ burn rate | Insert **"Seal & Pressurize" at P0.5** — with zero ambient atmosphere, your very first sealed room is more urgent than on Mars, since there's no thin atmosphere to buy you any margin. Reduced gravity also means falls/jumps behave differently — budget extra time for mobility, not danger. |
| **Europa** | Extreme cold | Substantial, breathable, but *reliably freezing* atmosphere; batteries lose charge over time just from exposure to the cold | Insert **"Power + Heating" at P0.5, ahead of P1** — you need continuous power before anything else, because cold alone drains your batteries even when idle. **Reuse furnace waste heat**: route the Arc Furnace's smelting heat into your shelter's early heating loop instead of venting it — this is a documented technique for cold worlds and turns a byproduct into your first heat source. Grow lights and heaters both compete for this same early power budget, so oversize P1 (power) accordingly. |
| **Venus** | Heavy, hot, elevated-pressure atmosphere | **Corrected figure:** in-game Venus pressure is 239 kPa (2.36× Earth standard) — mostly CO₂ and Hydrochloric Acid with some Nitrogen and trace Pollutants, no surface ice. (An earlier version of this table cited ~92 bar / 96% CO₂ — that's real-world Venus data from Wikipedia, not this game's figure; corrected here.) | Insert **"Cooling System" at P0.5** — heat alone can destroy tanks/canisters within minutes if left exposed. Also see **Project B9 (High-Pressure Structural Accommodation)** — 239 kPa ambient sits close to Iron Wall's 150 kPa burst threshold once you account for interior/exterior differential, so material choice matters here. No ice means your Water/Oxite/Volatiles mining steps (Project A1, items 3-4, 8) must be replaced with an imported or synthesized alternative — Venus starts typically include a Water canister and Oxygen tank specifically because ice mining isn't an option here. |
| **Vulcan** | Extreme heat, autoignition risk | Very hot, no ice deposits, volatiles-related fuels are especially autoignition-prone in this heat | Insert **"Cooling System" at P0.5** — same urgency as Venus. Additionally, treat **fuel/volatile storage as a standing hazard** — Vulcan's ambient heat pushes stored fuel closer to its autoignition point than on any other world, so isolate fuel storage from your shelter earlier than the framework's default Project N1 timeline. |
| **Space / Orbit / Asteroid Belt** | Vacuum, no gravity, no ground | No atmosphere, no gravity, no solid "ground" to build on in the traditional sense — official Asteroid start condition provides an Ingots Crate, Rocket Crate, and pre-filled Liquid Oxygen + Liquid Volatiles tanks instead of raw ore | Entirely different **P0**: there is no "mine ore from ground" step — replace Projects A1/A3 with **asteroid-surface mining or reliance on the pre-filled ingot/liquid crates** the Asteroid start condition actually provides. Structures must be anchored (no gravity to hold loose items in place) — treat **anchoring/mounting** as a new P0 step before any other placement. No day/night solar cycle in the way Mars has one — prioritize **non-solar power** (nuclear, wind if available, or imported fuel) over Solar Panel placement in P1. |
| **Mimas** | Extremely low gravity, weak solar, no water ice | Tiny moon, very low gravity, distant from the sun (weak solar output), water ice removed from spawn resources (patch-confirmed), reduced coal | Insert **"Non-Solar Power" priority into P1** — solar is documented as unreliable this far out; the Brutal start here is confirmed to include a Wind Turbine for this reason. Since water ice isn't available, Hydration (Project B2) must route through a different source — synthesized/imported water, not mined ice. |

**How to use this table:** Find your world, note the "Priority Modification" column, and mentally insert that step into the P0–P6 order from the previous section at the position specified (usually "P0.5," meaning after shelter placement but before the first furnace batch). Everything else in the framework — Tiers 0 through 3 — proceeds unchanged; only the *early* sequencing shifts per world.

---


## **CATEGORY B: IMMEDIATE SURVIVAL — Shelter, Water, Air, Power**

**Read this before building anything in this category:** Hydration depletes far faster than most players expect, and every airlock cycle costs real resources (pressurized air vented to vacuum/exterior on most worlds). These two facts together are why seasoned players do NOT build a full pressurized base before their first drink — they build the smallest possible sealed volume, use it minimally, and expand later. See B2 below for the exact numbers and the technique.

---

#### **B1: BASIC AIRLOCK**
**Success Criteria:**
- [ ] Main base is airtight (no pressure loss)
- [ ] Functional double-door airlock for entry/exit
- [ ] Pressure gauge shows stable levels
- [ ] No decompression deaths during operations
- [ ] Interior pressure >= 50 kPa
- [ ] Manual door controls accessible

**Checkpoints:**
- B1.1: Build frame structure with walls
- B1.2: Seal all openings (add extra iron sheets to frames)
- B1.3: Create double-door airlock (manual operation)
- B1.4: Monitor pressure over time (6+ hours stable)
- B1.5: Test manual door cycle (no pressure loss)

**Veteran shortcut — the 1x1x1 closet:** Rather than sealing your whole starter platform on Night 1, many experienced players build a single 1x1x1 sealed frame near their work area, drop a few chunks of Oxite ice on the floor inside, and melt it with a Welding Torch. Ambient heat does the rest — this creates a minimally breathable pocket just large enough to pull your helmet off, drink, and reseal, for a fraction of the material and power cost of a full pressurized room. This is especially standard practice on vacuum worlds (Moon, Space, Mimas), where every full-base airlock cycle vents your entire internal atmosphere to nothing. Confirmed technique — see Community Wiki "Beginner's Guide" and "Guide (Mining)."

---

### **Project B2: Hydration System**
**Success Criteria:**
- [ ] Pressurized room OR minimal sealed closet established (helmet-off zone)
- [ ] Water dispenser, vending machine, or starting Water Bottles accessible
- [ ] Player understands the real depletion window (see below) and plans drink breaks accordingly
- [ ] Water supply is continuous (not one-time) once base is established

**The real deadline — confirmed depletion rates (Normal difficulty, hunger/thirst rate = 1.0):**

| Body Temperature | Time to fully deplete (100%→0%) |
|---|---|
| 0°C | 34.7 minutes |
| 10°C | 23.1 minutes |
| 20°C (typical) | 17.4 minutes |
| 30°C | 13.9 minutes |

**This is real-time minutes, not in-game hours.** At the default day-length setting (20 real minutes = 1 in-game day), hydration can fully deplete **before a single in-game day cycle completes** — meaning on your Day 1 mining run, you may need a drink break *during* the excursion, not just when you return to base at night. Health only begins falling once hydration hits 0%, so there's a small buffer past empty, but treat the numbers above as your working deadline, not spare time. For comparison, Hunger depletes much slower — base rate 100%/hour on Normal, so roughly 60 real minutes to fully empty — confirming thirst is the more urgent of the two.

**Checkpoints:**
- B2.1: Create airtight room OR minimal 1x1x1 closet (see B1 veteran shortcut)
- B2.2: Drink from a starting Water Bottle during the Day 1 mining trip if it runs past ~15-20 real minutes — don't wait for Night 1
- B2.3: Install water dispenser/vending once base is established (upgrade from closet-and-bottle approach)
- B2.4: Confirm water doesn't run out on a continuous basis

**Why seasoned players push to the limit instead of over-building:** Every airlock cycle on a pressurized room vents that room's atmosphere to the exterior (full loss on vacuum worlds, partial loss into a thin atmosphere on worlds like Mars) — re-pressurizing costs stored gas and power every single time. Building an elaborate multi-room facility just to drink water means paying that cost repeatedly, early, when power is scarcest. The common veteran pattern is to let hydration/hunger run down close to the danger line before addressing it — minimizing total cycle count — rather than building the infrastructure to service needs comfortably from Day 1. This isn't reckless: it's a deliberate trade of comfort for resource efficiency during the exact window (Category A/B) when both are scarcest.

---

### **Project B3: Basic Atmosphere Control**
**Success Criteria:**
- [ ] Pressurized room has breathable O₂ (>= 19%)
- [ ] Pressure stable (not fluctuating wildly)
- [ ] CO₂ levels manageable (not suffocating)
- [ ] Player can breathe in base for extended periods

**Checkpoints:**
- B3.1: Identify current atmospheric composition
- B3.2: Deploy O₂ source (manual or temporary)
- B3.3: Set up basic CO₂ scrubber/vent
- B3.4: Monitor atmosphere for 12+ hours stable

---


### **Project B4: Emergency Power**
**Success Criteria:**
- [ ] At least one power source online (solar or fuel gen)
- [ ] Battery backup exists
- [ ] Critical systems (lights, vending) powered for 8+ hours minimum
- [ ] No power-related deaths

**Checkpoints:**
- B4.1: Deploy solar panels OR fuel generator
- B4.2: Install basic battery
- B4.3: Wire conduits to lights/vending
- B4.4: Verify 8-hour continuous power test

---

## **ENVIRONMENT PLACEHOLDER SLOTS (Conditional — Category B window)**
*These are not universal projects — they exist only for worlds where the hazard applies. Check your world against the Environment Configuration by World table above; if none of B5–B9 apply, skip straight to Category C. Position: these slot in here, after B4 and before C1, because they're urgent enough to precede Core Infrastructure but depend on nothing beyond B1's basic shelter.*

### **Project B5: Cooling System — [CONDITIONAL: Venus, Vulcan only]**
**Skip this project entirely unless you landed on a hot world.**
**Success Criteria:**
- [ ] Active cooling loop operational before first extended EVA
- [ ] Interior temperature held below the point where stored canisters/tanks are at risk
- [ ] Steel or reinforced structure used in place of plain Iron Walls (Venus specifically — documented pressure failure)

**Checkpoints:**
- B5.1: Identify heat-vulnerable stored items (canisters, tanks, batteries) and shield/relocate them first
- B5.2: Deploy a wall cooler or equivalent active cooling device inside the sealed shelter
- B5.3: On Venus: substitute Steel Walls for Iron Walls in any exterior-facing structure
- B5.4: Verify interior temperature stays in survivable band through one full day/night cycle

### **Project B6: Heating + Power Reserve — [CONDITIONAL: Europa only]**
**Skip this project entirely unless you landed on a cold world.**
**Success Criteria:**
- [ ] Continuous power reserve sized for heating load (cold drains batteries even at idle)
- [ ] Furnace waste heat routed into shelter heating loop
- [ ] Interior temperature held above freezing

**Checkpoints:**
- B6.1: Oversize battery/power capacity beyond the Category C default — cold worlds burn power just standing still
- B6.2: Route Arc Furnace exhaust/waste heat into the shelter instead of venting it
- B6.3: Verify battery charge doesn't net-drain overnight even with heating running

### **Project B7: Structural Anchoring — [CONDITIONAL: Space/Orbit/Asteroid Belt only]**
**Skip this project entirely unless you landed in zero gravity.**
**Success Criteria:**
- [ ] All placed structures confirmed anchored (nothing drifts on bump/impact)
- [ ] Mining/resource-gathering plan adjusted for asteroid-surface or pre-filled-crate reality (no ground to dig)

**Checkpoints:**
- B7.1: Verify every placed frame/structure is mounted, not floating loose
- B7.2: Replace the standard Day 1 mining plan (Project A1) with asteroid-surface or Asteroid-start-crate resource gathering

### **Project B8: Non-Solar Power Priority — [CONDITIONAL: Mimas, Space/Asteroid Belt]**
**Skip this project entirely unless solar is documented as unreliable on your world.**
**Success Criteria:**
- [ ] A non-solar power source (wind, gas generator, nuclear) online before relying on Solar Panel placement
- [ ] B4 (Emergency Power) re-sequenced to prioritize this source over solar

**Checkpoints:**
- B8.1: Identify available non-solar source for your world (Wind Turbine on Europa/Mimas Brutal starts, Gas Generator elsewhere)
- B8.2: Deploy and verify output before treating B4 as complete

### **Project B9: High-Pressure Structural Accommodation — [CONDITIONAL: Venus, or any world with ambient pressure approaching structural burst thresholds]**
**Skip this project entirely if your world's ambient pressure stays well under 150 kPa (check the Environment Configuration table). Required for Venus (confirmed 239 kPa ambient); apply the same caution on Vulcan or any other elevated-pressure world even where the exact figure isn't documented here — verify in-game before committing to standard materials.**

**Success Criteria:**
- [ ] Structural materials rated for your world's actual pressure differential, not just Iron Walls by default
- [ ] Airlock staged so no single door faces the full interior-to-exterior pressure jump
- [ ] Active Vent pressure settings reprogrammed if ambient exceeds the 100 kPa default the airlock console assumes

**Confirmed structural burst pressures (differential, not absolute):**

| Structure | Burst Pressure |
|---|---|
| Iron Wall | 150 kPa |
| Composite Window | 200 kPa |
| Glass Door | 200 kPa |
| Composite Wall | 300 kPa |
| Composite Door | 300 kPa |
| Airlock (portal) | 1 MPa (1000 kPa) |
| Gas Canister | ~10 MPa |
| Furnace / connected pipe | ~60 MPa |
| Frames, Blast Doors, **Foundation blocks** | No documented pressure limit |

**Foundation blocks** belong in the same "unlimited" tier as Frames and Blast Doors — they're the heavy structural/terrain element used to seat tanks, silos, and other heavy structures directly, and like Frames they don't model a rupture threshold the way thin-plate elements (Walls, Windows, Doors) do. On a high-pressure world, a shell built from Foundation blocks or reinforced Frames — not standard Walls — is the safest default for anything facing the full exterior differential.

**Checkpoints:**
- B9.1: Confirm your world's actual ambient pressure in-game (don't assume — check your suit's external readout)
- B9.2: If ambient pressure minus your planned interior pressure exceeds ~150 kPa, do NOT rely on plain Iron Walls for exterior-facing structure — use Composite Wall/Door (300 kPa), Blast Doors, or Foundation blocks (no documented limit) instead
- B9.3: Stage your airlock across two or more chambers rather than one big jump, so each individual door only faces a fraction of the total differential — this is standard practice on high-pressure worlds, not just a Venus-specific trick
- B9.4: Reprogram the Active Vent's PressureExternal value via IC10 if ambient exceeds 100 kPa — the default airlock console logic assumes a 100 kPa exterior and will not function correctly above that without adjustment
- B9.5: Consider running interior base pressure elevated (closer to ambient) rather than the default ~100 kPa, to reduce the differential any single wall/door has to hold — this trades suit/atmosphere cost for structural safety margin
- B9.6: On Venus specifically: the documented player technique is also to reduce Hardsuit target pressure to 14-30 kPa during EVA (reduces convective heat transfer, not a structural fix, but pairs with the above)
- B9.7: When expanding an existing sealed base outward, use the mark-before-you-build technique below — this is the single most common way players accidentally blow out a wall

**Expansion technique — mark before you build (also doubles as an airtightness safeguard):**

**The underlying mechanic (confirmed, still standard as of the most recent updates):** Stationeers atmospherics run on a large-grid, 2m-cube-cell model — each cell defaults to 8000L and pressure is calculated directly from the ideal gas law (`pressure = moles × 8.3144 × temperature / volume`). A bare Frame doesn't block gas flow at all; only once Sheets are welded onto it does it become an airtight boundary. A finished/sealed wall doesn't hold its own separate gas pocket — it's a boundary, not a container. This means when you deconstruct one:
- If it borders the **exterior/world atmosphere**, the freed cell just merges with that already-vast outside reservoir — no meaningful change, gas doesn't "rush" anywhere because the outside is effectively infinite (or already vacuum).
- If it borders your **sealed interior room**, the freed cell's volume gets added to your room's total. Same total moles of gas, now spread across more volume — pressure drops immediately per the ideal gas law, and gas visibly moves to fill the new space.

This is why a careless expansion blows a wall out: deconstructing an *exterior-facing* wall before its replacement is sealed vents your interior gas straight to the true outside atmosphere/vacuum — uncontrolled loss, potentially explosive. But deconstructing an *interior-facing* wall (one with an already-sealed new shell on its far side) just dilutes your room into the extra volume — a pressure dip you can immediately correct by topping up gas, not a disaster.

**The tested technique (confirmed workflow, works because of the mechanic above):**
1. From **inside** your current sealed room, use Spray Paint (5 cans included in the starting Consumable Supplies crate) to flag every wall/frame segment you intend to expand past — pick one color meaning "expansion boundary."
2. From **outside**, build entirely new frames (and seal them) around all the flagged segments, fully enclosing the intended expanded volume. The flagged wall is still doing its job as your seal throughout this phase — nothing about your interior pressure is at risk while this shell goes up, since it isn't connected to anything yet.
3. Once the new outer shell is fully sealed, go back **inside** and deconstruct the flagged frames. Because the new shell is already airtight, this doesn't vent to true exterior — it just merges your interior gas into the new enclosed volume, which is the safe, self-correcting pressure dip described above rather than an explosive vent.
4. Top up gas afterward to restore your target pressure across the now-larger combined volume.
5. **Same technique, different use:** mark ANY wall you're unsure about the same way — a suspected leak, a boundary a teammate built whose pressure state you don't know. A simple color convention (e.g., red = live/exterior-facing, green = confirmed interior-safe) turns "is this wall holding pressure against the outside?" from a guess into something visible at a glance — useful for airtightness maintenance generally, not just expansions, and especially valuable in multiplayer where a teammate might not know which wall is load-bearing on pressure.

**Reference:** Confirmed burst pressures from community-compiled reference spreadsheet (via Steam Community discussion); Venus ambient pressure and Hardsuit pressure-reduction technique from Community Wiki "Venus" page; Active Vent PressureExternal default behavior from Community Wiki "Active Vent" page; grid-cell/mole/pressure model confirmed current via Community Wiki "Atmosphere" page and stationeering.com's data reference (8000L cell volume, ideal gas law formula), cross-checked against the March 2026 "Gases Update" patch notes, which added new gas types without changing this underlying volume/pressure mechanic. The expansion-blowout failure mode is a documented recurring player pitfall (Steam Community "Base pressurizing - Do's & Don'ts" and similar threads); the spray-paint marking method itself is practical guidance built on that confirmed problem and mechanic, not an officially named game feature.

---

## **CATEGORY C: CORE INFRASTRUCTURE — Energy & Fabrication Backbone**


### **Project C1: Energy Infrastructure**
**Success Criteria:**
- [ ] Multiple power sources operational (2+ generators or solar arrays)
- [ ] Total capacity >= peak load consumption
- [ ] Battery storage >= 6 hours of base consumption
- [ ] Distributed conduit network (no power bottlenecks)
- [ ] Redundancy: base survives loss of one power source

**Checkpoints:**
- C1.1: Map current power consumption
- C1.2: Build second power source (diversify fuel/solar)
- C1.3: Install sufficient battery capacity
- C1.4: Create separate power circuits (primary/backup)
- C1.5: Stress test during high load

---


### **Project C2: Fabrication Setup**
**Success Criteria:**
- [ ] Fabricator online and functional
- [ ] Can produce basic metal components
- [ ] Supply chain for fabricator inputs established
- [ ] Sufficient power allocated for fabrication cycles
- [ ] Queue system prevents bottlenecks

**Checkpoints:**
- C2.1: Acquire/build fabricator
- C2.2: Power and wire fabricator
- C2.3: Test basic component production (frames, doors)
- C2.4: Establish input material source
- C2.5: Verify queue doesn't jam

---


## **CATEGORY D: SUSTAINED SURVIVAL — Food & Storm Resilience**


### **Project D1: Hydroponics — Minimum Viable**
**This is the Milestone 1 bar. D1b below is the full self-sustaining food/O₂/CO₂ system; D1 is just enough to stop relying purely on starting rations.**

**Success Criteria:**
- [ ] At least one Hydroponics Tray or Portable Hydroponics unit operational
- [ ] At least one crop type growing (from starting Organic Supplies seeds — Potato, Wheat, Corn, etc.)
- [ ] Confirms the growing loop works (light, water, nutrients) before scaling up

**Checkpoints:**
- D1.1: Place Hydroponics Tray or Portable Hydroponics kit (from starting crate) inside sealed, lit space
- D1.2: Plant at least one starting seed type
- D1.3: Verify growth over one cycle (confirms light/water/pressure are all adequate)
- D1.4: Harvest and confirm seed reproduction for a second planting

---

### **Project D1b: Food Production (Full — O₂ Gen + CO₂ Scrubbing)**
**Success Criteria:**
- [ ] Greenhouse producing crops at scale (multiple trays/plots, not just one)
- [ ] Measurable O₂ generation (atmospheric sensors show increase)
- [ ] CO₂ consumption visible (levels drop when plants active)
- [ ] Crops growing continuously (reproducible seed supply)
- [ ] Atmospheric O₂ maintained >= 19% via plants alone

**Checkpoints:**
- D1b.1: Expand from D1's single tray to a full greenhouse with proper pressure
- D1b.2: Diversify seed stock beyond the first crop type
- D1b.3: Establish reliable light source (natural or artificial — grow lights if light-starved world)
- D1b.4: Measure O₂/CO₂ change over 12 hours to confirm the greenhouse is a net atmospheric contributor
- D1b.5: Verify harvest sustains both food supply and seed stock indefinitely

---


### **Project D2: Storm & Weather Protection**
**Success Criteria:**
- [ ] Weather Station installed (advance storm warning)
- [ ] All exposed items secured or moved indoors before storms
- [ ] Structural repair stock maintained (duct tape, glass sheets)
- [ ] Solar panels protected or storm-rated
- [ ] Room properly recognized as storm-safe (see the room-recognition note below — this is the most common reason "sealed" rooms still take damage)
- [ ] Base survives 3+ consecutive storms with minimal damage
- [ ] No lost items due to storm winds
- [ ] Cabling upsized to capture, not just survive, a solar storm's power spike (see below)

**What's actually confirmed destructible (correcting an earlier assumption in this document):**

A genuinely *recognized* sealed Room is confirmed storm-safe per the Community Wiki — a properly-built, properly-detected room's walls and doors do not take direct storm damage. What does:
- **Solar panels** — the most consistently reported target, even indoors, due to a widely-reported room-recognition gotcha (see below)
- **Exposed/loose items** not stored in a locker or sealed room — confirmed to blow away and be unrecoverable, not just damaged
- **Unfinished/build-state structures** (frames without sheets, partial walls) — anything not fully finished doesn't count as sealed regardless of intent
- **Rovers and AIMEe left outside** — confirmed as the explicit exception to the normal repair process (duct tape/glass sheets/rebuild does NOT fix these two — they need dedicated garage/shelter, see Project I2)
- **Exposed ore and ingot stacks** left outside storage
- Structures on high-pressure worlds can still fail from the pressure/heat spike a storm causes hitting an already-marginal wall (this is the B9 mechanism, not the storm "attacking" the wall directly)

**Your instinct was right that walls and doors themselves aren't the normal target** — but there's a well-documented gotcha explaining why players report them getting hit anyway:

**The room-recognition gotcha:** simply enclosing a space with finished walls is sometimes NOT enough for the storm system to recognize it as a protected Room — this is a long-standing, widely-reported issue. The confirmed community workaround is to make sure the room's interior atmosphere is **distinctly different from the exterior world atmosphere** (the reliable trick: vent the room to a hard vacuum, or fill it with a gas mix that doesn't match outside) — this forces the game to recognize the enclosure as a separate, protected Room. An enclosed space with interior atmosphere too similar to outside is the most commonly reported cause of "sealed" solar panels/rooms still taking storm damage.

**Storm as an opportunity, not just a threat:** solar storms confirmed to spike panel output significantly — up to 1.5kW per panel when sun-facing, versus their normal rate. That's enough that ~4 panels during a solar storm can exceed a standard cable's 5kW capacity and burn it out. Rather than just enduring storms defensively, size your cabling (heavy cable, or separate the network with an APC/Power Controller or Transformer) to actually capture that spike instead of losing it to a blown cable — a well-prepared base can treat the first storm as a power windfall, not just a threat to survive.

**Checkpoints:**
- D2.1: Build/acquire Weather Station for advance warning
- D2.2: Secure all loose crates/items (mount or store in lockers) — confirmed to blow away permanently, not just take damage
- D2.3: Stock duct tape and glass sheets for rapid repair
- D2.4: Identify planet-specific storm risk (dust/solar/cold/heat — varies by body)
- D2.5: Reinforce or relocate exposed solar panels
- D2.6: Verify your room's interior atmosphere is distinctly different from the exterior (vacuum trick or distinct gas mix) — confirms proper Room recognition before you trust it as storm-safe
- D2.7: Upsize cabling (heavy cable or APC/Transformer separation) on any circuit with solar panels, so a storm's power spike doesn't just burn out a standard cable
- D2.8: Survive first storm, log damage
- D2.9: Repair and adjust base layout based on damage pattern
- D2.10: Survive 3 consecutive storms with no item loss

**Note:** Storm behavior is planet-specific — Moon has solar storms (heat spike, faster suit O₂ burn, and the power-spike opportunity above), Mars has dust storms (visibility loss, item damage), Europa has cold buffeting winds, Venus/Vulcan have heat storms that can push already-marginal structures (see B9) past their pressure threshold. Rooms are generally storm-safe once properly recognized (see the gotcha above), but rooms larger than 1200 cubes can still experience storm effects inside regardless.

**Reference:** Confirmed via Community Wiki "Storm" page (destructible items list, 1.5kW/panel solar storm output, 1200-cube room size cap, repair methods) and multiple Steam Community discussion threads confirming the room-recognition/distinct-atmosphere workaround as a long-standing, widely-reproduced community fix.

---


## **CATEGORY E: CONTINUITY SAFEGUARDS — Death Recovery**


### **Project E1: Cloning/Cryo Vat System**
**Success Criteria:**
- [ ] Cryotube(s) installed and operational
- [ ] Powered continuously (dedicated circuit, no brownouts)
- [ ] Housed in pressurized, breathable atmosphere room
- [ ] Respawn point configured at cryotube
- [ ] Passive healing boost confirmed functional (up to 75% faster)
- [ ] Death recovery tested (respawn successfully with gear/location intact)

**Checkpoints:**
- E1.1: Unlock/fabricate Cryotube recipe (Autolathe MK2)
- E1.2: Build dedicated cryo room (pressurized, breathable)
- E1.3: Wire cryotube to stable power circuit
- E1.4: Place Kit (Respawn) to set spawn point at cryotube
- E1.5: Test passive healing (enter tube, verify accelerated recovery)
- E1.6: Test death/respawn cycle (verify spawn at cryotube, not random location)
- E1.7: Stock spare gear near cryotube for post-respawn kit-up

**Note:** Respawning applies a temporary debuff affecting food/water consumption speed, tool usage speed, and trader prices — plan spare consumables near the cryotube for recovery. On death, the body remains recoverable for a period before decaying — a nearby corpse-retrieval plan (see Mortuary, Project E2) helps recover lost gear.

---


### **Project E2: Mortuary (Early Stage — Storage Lockers)**
**Success Criteria:**
- [ ] Designated storage locker(s) for recovered remains/skulls
- [ ] Located near cryo room or base entrance
- [ ] Labeled and separate from general storage
- [ ] Simple record of losses kept (for later pedestal hall)

**Checkpoints:**
- E2.1: Designate locker(s) specifically for remains
- E2.2: Label locker clearly (separate from tool/ore storage)
- E2.3: Establish retrieval routine after each death (recover body/items before decay)
- E2.4: Track names/dates informally for future memorial hall

---


## **CATEGORY F: RESOURCE SCALING — Mining & Storage Growth**


### **Project F1: Basic Mining Operations**
**Success Criteria:**
- [ ] Mining producing ore consistently
- [ ] Ore collected and stored
- [ ] Mining doesn't deplete nearby resources too quickly
- [ ] Supply rate >= demand rate for fabrication

**Checkpoints:**
- F1.1: Identify mineable ore deposits
- F1.2: Set up manual or simple automated mining
- F1.3: Create ore collection/storage point
- F1.4: Measure production rate vs consumption

---


### **Project F2: Storage & Inventory Management**
**Success Criteria:**
- [ ] Organized storage for: ores, ingots, components, consumables
- [ ] Each storage category labeled/accessible
- [ ] Inventory doesn't overflow (items lost)
- [ ] Easy retrieval of frequently-used items
- [ ] Sufficient capacity for 30+ days production

**Checkpoints:**
- F2.1: Build storage containers/lockers
- F2.2: Assign storage purposes (ore vs ingot vs component)
- F2.3: Test retrieval speed (can find items quickly)
- F2.4: Monitor overflow incidents (should be zero)

---


## **CATEGORY G: CLIMATE CONTROL — Thermal Regulation**


### **Project G1: Thermal Regulation — Minimum Viable**
**This is the Milestone 1 bar — enough to call your base "temperature stable." G2 and G3 below are the full active systems; G1 is the floor you need before either is complete.**

**Success Criteria:**
- [ ] Interior temperature stays within 18–25°C for at least one full day/night cycle
- [ ] At least one passive or active method holding that band (doesn't have to be automated yet)
- [ ] Temperature sensor installed and readable

**Checkpoints:**
- G1.1: Install at least one temperature sensor inside the sealed shelter
- G1.2: Confirm insulation baseline — sealed frames/walls alone provide some buffering; verify actual drift rate over a few hours
- G1.3: Deploy ONE active device (wall heater or wall cooler, whichever direction you're fighting) — doesn't need automation yet, manual toggling is acceptable for Milestone 1
- G1.4: Verify 18-25°C held through one full day/night cycle with the manual system

---

### **Project G2: Heating Subsystem (Full)**
**Success Criteria:**
- [ ] Dedicated heating device(s) sized for worst-case cold (night temps, or constant cold on Europa)
- [ ] Furnace waste heat routed in where applicable (see B6 for cold-world worlds)
- [ ] Automated on/off by sensor reading, not manual toggling

**Checkpoints:**
- G2.1: Size heating capacity against your world's documented night/cold temperature (see Environment Configuration table)
- G2.2: Install heating device(s) — incinerator, wall heater, or radiator loop
- G2.3: Wire to temperature sensor for automatic on/off
- G2.4: Test through coldest expected period (night on Mars/Moon, constant on Europa)

---

### **Project G3: Cooling Subsystem (Full)**
**Success Criteria:**
- [ ] Dedicated cooling device(s) sized for worst-case heat (day temps, or constant heat on Venus/Vulcan)
- [ ] Correct radiator type for your atmosphere (Convection if atmosphere present, Radiation if vacuum — see Environment Configuration table)
- [ ] Automated on/off by sensor reading

**Checkpoints:**
- G3.1: Size cooling capacity against your world's documented day/hot temperature
- G3.2: Confirm correct radiator type for your world (Radiation Radiators are mandatory in vacuum — Convection Radiators do not function there)
- G3.3: Install cooling device(s) and wire to temperature sensor
- G3.4: Test through hottest expected period

---

### **Project G4: Full Automated Climate Control**
**Success Criteria:**
- [ ] G2 and G3 both complete and wired to the same control logic
- [ ] Temperature fluctuation < 5°C over 12 hours, fully unattended
- [ ] All rooms (not just the original shelter) monitored and stable

**Checkpoints:**
- G4.1: Unify heating (G2) and cooling (G3) under one thermostat control loop
- G4.2: Extend sensor coverage to every room, not just the first shelter
- G4.3: Extended stability test — 24+ hours fully unattended, no manual intervention

---


## **CATEGORY H: AIRLOCK AUTOMATION — Console to Failsafe**


#### **H1: CONSOLE-CONTROLLED AIRLOCK**
**Success Criteria:**
- [ ] Airlock console (IC chip + display) installed and powered
- [ ] 2 doors (interior & exterior) controllable from console
- [ ] 2 active vents (interior & exterior) controllable from console
- [ ] Console cycles gas transfer between interior/exterior
- [ ] No manual door toggling needed (all via console)
- [ ] Airlock cycles complete in <30 seconds

**Checkpoints:**
- H1.1: Insert Airlock circuitboard into console
- H1.2: Place glass sheet on console
- H1.3: Install data disk
- H1.4: Wire both doors to console (power + data)
- H1.5: Wire both active vents to console (power + data)
- H1.6: Configure console: select exterior door, exterior vent, interior door, interior vent
- H1.7: Test full cycle: press cycle on console, monitor door states and vent operation
- H1.8: Verify both active vents draw from correct pipe networks (interior + exterior isolated)

**Reference:** See Steam Workshop "Airlock Control" (ID: 1524868713) or "Super Simple Autocycling Airlock" (ID: 1232888907) for setup guidance.


#### **H2: IC-BASED ADVANCED AIRLOCK**
**Success Criteria:**
- [ ] IC10 circuit controls door and vent logic
- [ ] Gas sensors detect composition/pressure on both sides
- [ ] Doors remain locked if pressure/composition mismatch
- [ ] Auto-props doors open if both sides match (pressure, temp, gas %)
- [ ] Pressure equalization automatic
- [ ] System runs autonomously without player input

**Checkpoints:**
- H2.1: Build IC10 housing with data network
- H2.2: Deploy pressure + gas composition sensors (interior airlock)
- H2.3: Wire sensors to IC10
- H2.4: Wire doors + active vents to IC10
- H2.5: Program IC10: compare interior/exterior sensors, hold doors until match
- H2.6: Test auto-cycle with mismatched atmosphere (should NOT open)
- H2.7: Test auto-cycle with matched atmosphere (should open)

**Reference:** Steam Workshop "Emergency Bulkhead Lockdown" (ID: 2258102536) uses similar logic (sensor + IC + lights).


#### **H3: ADVANCED AIRLOCK + FAILSAFE**
**Correction to an earlier version of this project:** the "manual crank mechanism" described previously doesn't correspond to a real game feature — Stationeers has no such device. This section is rewritten against confirmed mechanics below.

**The critical mechanic (confirmed via Community Wiki "Circuitboard (Advanced Airlock)" and "Blast Doors" pages):** once a console/IC-driven airlock circuit powers on, it **locks** every door and vent it controls — and that lock does NOT release when power is subsequently lost. A locked door stays locked, powered or not. The Crowbar (your starting manual tool) only opens a door that is **both unlocked AND unpowered** — it cannot force a locked door regardless of power state. This means a naive automated airlock is a real trap risk: lose power mid-cycle, and you can be stuck between two bolted-shut doors with no manual recourse, confirmed by multiple player reports of exactly this happening.

**Success Criteria:**
- [ ] All H2 features working
- [ ] Airlock's console/IC circuit powered from a **dedicated, isolated Power Controller** — not the main base grid — so a main-grid outage doesn't cut the airlock's own power and trigger the lock-with-no-escape scenario
- [ ] That dedicated Power Controller has a battery cell installed, and that battery is confirmed swappable with any hand-tool battery (small battery) for emergency top-up
- [ ] A **second, genuinely separate manual door or hatch** exists alongside the automated airlock — one that is never wired into any airlock circuit, so it is never locked, and the Crowbar always works on it as a true bypass
- [ ] IC10 monitoring confirms when the dedicated Power Controller is drawing down its battery (running on backup) rather than external power, so a depleting emergency supply gets caught before it runs out
- [ ] Posted emergency procedures at each airlock, including the location of its true manual bypass door

**Checkpoints:**
- H3.1: Install a Power Controller dedicated solely to the airlock's console/IC circuit — confirmed community best practice, keeps it isolated from main-grid outages
- H3.2: Confirm the dedicated Power Controller's battery cell can be swapped with a spare hand-tool battery (small battery) as an emergency top-up — this is a documented, intentional design, not a workaround
- H3.3: Build a second physical route in/out (a plain Door or Blast Door) that is NOT wired to the airlock circuit at all — this is what actually stays crowbar-operable if the automated airlock's dedicated power runs out
- H3.4: Test the failure case deliberately: cut the dedicated Power Controller's external feed, confirm the airlock doors stay locked (expected — this is why H3.3 exists, not a bug to fix)
- H3.5: Confirm the true manual bypass door opens with a Crowbar at all times, unpowered by design
- H3.6: Wire an IC10 (or Logic Reader) to read the dedicated Power Controller's Charge value over time — a steadily falling Charge with no corresponding rise means it's running on battery alone, not receiving external power; alert on a falling trend rather than waiting for total depletion
- H3.6b: Implement the staged response per `ic10_failsafe_airlock_requirements.md` — Charge ≤90%: dim non-essential lights (warning only). Charge ≤10%: close open doors, evacuate chamber atmosphere, then explicitly unlock doors so a full drain afterward leaves them Crowbar-operable, not locked-and-dead
- H3.6c: **Test first, build conditionally** — attempt a direct IC10 power write to a Portal while its data connection stays live. If the Portal can't de-energize independently of the circuit powering the IC10/Buttons/Light, add a Transformer (or equivalent switch) per Portal, wired on a separate circuit from the control chips — this is what makes Deep Idle Mode's power-cut-without-killing-the-monitor-loop actually work. Skip this checkpoint entirely if the test shows pure logic suffices.
- H3.7: Post laminated emergency procedures at each airlock, including where its true manual bypass is located

**On the IC10 detection question specifically:** I could not confirm a single dedicated boolean logic type literally meaning "running on backup power" for the Power Controller/APC in available documentation. What IS confirmed is that the Power Controller exposes readable Data Network properties (via Logic Reader, Batch Reader, or IC10) including a battery **Charge** value. The reliable, practical method is to monitor that Charge over successive reads: if it's falling instead of holding steady or rising, net power in is less than net power out — i.e., it's running on stored charge, not external supply. This is a trend-based proxy, not a single flag, but it's the confirmed and commonly-used approach. Check your specific game version's Logic Reader "VAR" cycle-through list on the actual device in-game — a more direct field may exist that isn't documented in the sources available here.

**Custom IC10 script required:** neither the Basic nor Advanced Airlock circuitboard is programmable — they're hardcoded. The IC10 **replaces** the circuitboard entirely for this design — Portals and Active Vents wire directly to the IC Housing, not to a Circuitboard (Airlock/Advanced Airlock), so the IC10 owns door locking outright with no other controller to conflict with. The staged power-failure behavior (dim lights at reduced charge, then close/evacuate/unlock at critical charge — so a fully-drained battery leaves an unpowered-but-*unlocked* door rather than an unpowered-and-locked one) requires this custom IC10 chip. If the full state machine doesn't fit in one chip's 128-line limit, multiple IC10s can coordinate by reading/writing a shared device's LogicType or a Logic Transmitter/Receiver pair — confirmed standard practice, not a workaround. Full requirements spec: see `ic10_failsafe_airlock_requirements.md` in the resource database. IC10 is programmed in a MIPS-inspired assembly language, not Lua.

**Reference:** Community Wiki "Circuitboard (Advanced Airlock)" page (lock-persists-through-power-loss behavior, confirmed directly), Community Wiki "Blast Doors" page (same lockout behavior confirmed for Blast Doors specifically), Community Wiki "Crowbar" page (unlocked+unpowered requirement), Community Wiki "Power Controller" / "Area Power Controller" pages (Data Network properties, battery-buffering/UPS-like behavior), and multiple Steam Community threads (including the original "Unlock (not open) airlock door when power is cut" suggestion thread) confirming both the trap risk and the dedicated-Power-Controller-with-swappable-battery workaround as established community practice.

---


## **CATEGORY I: FIELD READINESS — EVA Spares & Vehicles**


### **Project I1: EVA Suit & Spare Equipment Stock**
**Success Criteria:**
- [ ] Spare suits stocked (minimum 2 full sets: suit, helmet, backpack)
- [ ] Spare tools stocked (wrench, welder, drill, duct tape, wire cutters — 2x each)
- [ ] Spare O₂ tanks and canisters filled and racked
- [ ] Spare batteries for suits/tools
- [ ] Jetpack + fuel available if low-gravity body
- [ ] Equipment locker organized and labeled near airlock

**Checkpoints:**
- I1.1: Fabricate/acquire 2+ spare suits and helmets
- I1.2: Fabricate/acquire spare set of core tools
- I1.3: Build filling station for O₂ tanks/canisters
- I1.4: Stock spare suit batteries (charged)
- I1.5: Build equipment locker near primary airlock
- I1.6: Label locker slots (suits, tools, tanks, batteries)
- I1.7: Test full EVA kit-up in under 60 seconds

---


### **Project I2: Vehicle & Rover Operations**
**Success Criteria:**
- [ ] Rover or AIMEe mining bot operational
- [ ] Vehicle fuel/charging station established
- [ ] Vehicle garage/storm shelter for parking (prevents storm damage)
- [ ] Mining range extended beyond walking distance
- [ ] Ore transport rate improved vs manual carrying

**Checkpoints:**
- I2.1: Fabricate Rover or AIMEe bot
- I2.2: Build fuel/charge station for vehicle
- I2.3: Build sheltered garage (storm protection)
- I2.4: Test round-trip mining run
- I2.5: Automate return-to-base on low fuel/battery (if AIMEe)
- I2.6: Measure ore/hour improvement vs manual mining

**Note:** Vehicles and AIMEe left outside during storms can take damage, same as loose items — garage/shelter is not optional for long-term use.

---


## **CATEGORY J: ATMOSPHERIC REFINEMENT — Distillation & Waste**


### **Project J1: Thermal/Pressure Distillation**
**Success Criteria:**
- [ ] Distillation unit separating gas mixtures
- [ ] Can extract specific gases (O₂, N₂, CO₂) reliably
- [ ] Output gases pure (>95% target gas)
- [ ] Pressure maintained through distillation
- [ ] System runs autonomously

**Checkpoints:**
- J1.1: Build distillation apparatus
- J1.2: Understand distillation input requirements
- J1.3: Test separation of known gas mixture
- J1.4: Route output gases to storage/use
- J1.5: Verify output purity via sensors

---


### **Project J2: Waste Management**
**Success Criteria:**
- [ ] Excess CO₂ routed away from habitable areas
- [ ] Heat from generators dissipated safely
- [ ] No waste gas buildup in base
- [ ] Exhaust vented or recycled systematically
- [ ] System scales with production increases

**Checkpoints:**
- J2.1: Identify waste gas sources
- J2.2: Build exhaust routing (vents/pipes)
- J2.3: Route waste away from living areas
- J2.4: Monitor atmospheric purity (no contamination)
- J2.5: Test during high-load periods

---


## **CATEGORY K: ORGANIZATION — Goal Board**


### **Project K1: Goal Board System**
**Success Criteria:**
- [ ] Centralized task/priority board visible to player
- [ ] Daily/session priorities posted
- [ ] Track completion status of major systems
- [ ] Identifies next critical project
- [ ] Difficulty-appropriate goals (Standard: helmet-off for hydration)
- [ ] Updates based on current base state

**Checkpoints:**
- K1.1: Create physical goal board (whiteboard, sign, or monitor)
- K1.2: Post current Tier completion status
- K1.3: List next 3-5 priority projects
- K1.4: Track daily resource production targets
- K1.5: Log maintenance schedules (sensor checks, power audits)
- K1.6: Update weekly (or after major system completion)

**Sample Goal Board Layout (Difficulty: Standard):**

```
═══════════════════════════════════════════════════════════════
                     STATION STATUS BOARD
═══════════════════════════════════════════════════════════════

TIER -1 - LANDING DAY:
  [✓] A1 Day 1 Mining Run (150g+ Fe, Cu, Au, ice collected)
  [✓] A2 Night 1 Fabrication (Autolathe, Furnace, Electronics Printer online)

TIER 0 - SURVIVAL:
  [✓] B2 Hydration (remove helmet to drink)
  [✓] B1 Basic Airlock (manual double-door)
  [✓] B4 Emergency Power (solar online)
  [✓] B3 Atmosphere (O₂ >= 19%)

TIER 1 - FOUNDATION:
  [✓] C1 Energy Infrastructure (2x power sources)
  [✓] C2 Fabrication (fabricator online)
  [IP] F2 Storage (85% complete - sort ores by type)
  [ ] F1 Mining Operations (set production target: 100 Fe/hour)
  [ ] D2 Storm & Weather Protection (weather station not built)
  [ ] I1 EVA/Spare Equipment Stock (0/2 spare suits)
  [ ] I2 Vehicle/Rover Operations (not started)

TIER 2 - CYCLES:
  [IP] G1 Thermal Regulation (heating working, cooling next)
  [ ] J1 Distillation (not started)
  [ ] D1 Food Production (seeds acquired, greenhouse frame built)
  [ ] J2 Waste Management (exhaust venting planned)
  [ ] E1 Cloning/Cryo Vat (recipe unlocked, room not built)
  [ ] E2 Mortuary - Lockers (not started)

TIER 3 - OPTIMIZATION:
  [ ] L1 Power Automation (motion sensors ordered)
  [ ] M1 Trade Infrastructure (gas tanks planned)
  [ ] L2 Automated Crafting (in progress - 30% wired)
  [ ] K1 Goal Board (THIS ONE)
  [ ] O1 Mortuary - Pedestals (long-term)
  [ ] N1 Rocket Fuel Chain (long-term)
  [ ] N2 Rocket Mining (long-term goal)
  [ ] P1 Redundancy (power backup planned)

═══════════════════════════════════════════════════════════════
TODAY'S PRIORITIES:
  1. Complete F2 Storage (30 min estimated)
  2. Build cooling system for G1 (60 min)
  3. Plant seeds in greenhouse (15 min)

RESOURCE TARGETS (Daily):
  • Iron ingots: 50+ in stock
  • O₂: Distillation running
  • Water: Dispenser filled

NEXT SYSTEM FAILURE RISK:
  • Thermal: Running warm (36°C) - add cooling TODAY
  • Power: Acceptable (45% draw at peak)
  • Atmosphere: Good (22% O₂)

═══════════════════════════════════════════════════════════════
Legend: [✓] Complete  [IP] In Progress  [ ] Not Started
```

---


## **CATEGORY L: AUTOMATION & EFFICIENCY — Power & Crafting**


### **Project L1: Power Automation & Cycling**
**Success Criteria:**
- [ ] Non-critical circuits identified and automatable
- [ ] Motion sensors deployed in work areas
- [ ] Lights cycle on/off based on occupancy
- [ ] Standby power consumption minimized
- [ ] Peak load reduced by >= 20% vs constant run
- [ ] Generator backup auto-activates when battery drops below threshold
- [ ] Generator auto-deactivates when battery charges above threshold (with hysteresis)

**Checkpoints:**
- L1.1: Audit all circuits (identify which can cycle)
- L1.2: Install motion sensors in each room
- L1.3: Wire motion sensors to light circuits via batch logic (reader → writer)
- L1.4: Test sensor responsiveness (lights trigger/stop correctly)
- L1.5: Measure power savings over 24 hours
- L1.6: Build IC10 circuit for battery monitoring
- L1.7: Wire backup generator to IC10 + battery (data + power)
- L1.8: Program IC10: turn ON generator if battery < 25%, turn OFF if battery > 90%
- L1.9: Test failover: simulate battery drain, verify generator starts
- L1.10: Test shutdown: simulate battery charging, verify generator stops

**Reference:** Steam Workshop "[F&S] Emergency Power System" (ID: 1696723430) or "Power controller V2" (ID: 2362230182) for backup generator automation. Also see "Stationeers IC10 Solar Tracker & Power Automation" guide for battery-aware power management with hysteresis.

---


### **Project L2: Automated Crafting System**
**Success Criteria:**
- [ ] Inventory tracking system in place (manual or automated logging)
- [ ] Vending machines pull items and register consumption
- [ ] Items pulled from vending trigger auto-queue in fabricator
- [ ] Crafted items automatically returned to stock
- [ ] System maintains minimum stock levels (configurable)
- [ ] No lost items or inventory desync
- [ ] Reduces player manual crafting time by 80%+

**Checkpoints:**
- L2.1: Map all vending machines and their primary items
- L2.2: Create inventory log sheet (manual or digital tracking)
- L2.3: Establish baseline stock levels for each common item
- L2.4: Build material input buffer near fabricator
- L2.5: Set up automated routing: vending → item removed → fabricator signal
- L2.6: Wire fabricator to auto-queue when stock drops below threshold
- L2.7: Route fabricated items back to vending storage
- L2.8: Test full cycle (pull item → craft → restock) 5+ times
- L2.9: Monitor for 24+ hours (verify no bottlenecks or desync)

**Items to Automate (Priority Order):**
1. **Immediate needs (Category A-B):** Water (from vending) → Minimum stock: 50+
2. **Core infrastructure (Category C):** Frames, doors, pumps, batteries → Stock: 20+ each
3. **Scaling (Category F-G):** Regulators, valves, sensors → Stock: 10+ each
4. **Refinement (Category J+):** Advanced components (distillers, coolers) → Stock: 5+ each

**System Requirements:**
- Fabricator must have dedicated power circuit (no brownouts)
- Input material storage must not jam (regular clearing)
- Vending machine pull sensors or logic readers (detect item removal)
- Conveyors/chutes for material routing (fabricator → vending storage)
- IC10 circuit for inventory monitoring and queue management
- Logic writers to trigger fabricator queue based on stock levels

**Reference:** Steam Workshop contains ore sorting systems and IC10 crafting controllers. Community practice uses: Vending Machines (100 slots inventory) + Logic Readers (detect stock level) + Logic Writers (trigger fabricator) + Conveyors (route output). Example: Cows Are Evil on YouTube has documented full automation on Venus playthrough with IC10 code available on workshop.

---


## **CATEGORY M: ECONOMY — Trade Infrastructure**


### **Project M1: Trade Infrastructure**
**Success Criteria:**
- [ ] Trade platform functional and accessible
- [ ] Storage for trade goods (gases + solids/ores)
- [ ] Profitable trade cycle established (input < output value)
- [ ] Trader visits reliably
- [ ] No loss of goods during trade
- [ ] All tradable gases stored in large tanks with proper temp/pressure controls
- [ ] Understand trade runs on in-game currency — sell goods for credits, then spend credits to buy

**Checkpoints:**
- M1.1: Build/acquire trade platform
- M1.2: Create dedicated storage (gas tanks + ore containers)
- M1.3: Establish connection between production and trade storage
- M1.4: Execute first SELL transaction (goods → credits)
- M1.5: Execute first BUY transaction (credits → goods/blueprints)
- M1.6: Monitor multiple trade cycles (consistency)

**Gas Trade Storage Specification:**

| Gas | Tank Type | Storage Temp Range | Pressure Range | Special Handling | Risk | Tradeable? |
|-----|-----------|-------------------|-----------------|------------------|------|-----------|
| **Oxygen (O₂)** | Large Pressure Tank | -10 to 30°C | 0-50 MPa | Inert, stable | Non-flammable support | ✓ Yes |
| **Nitrogen (N₂)** | Large Pressure Tank | -10 to 30°C | 0-50 MPa | Inert, stable | Non-flammable diluent | ✓ Yes |
| **Carbon Dioxide (CO₂)** | Large Pressure Tank | -10 to 25°C | 0-50 MPa | Inert, stable. Liquefies at high pressure | Non-toxic in trading | ✓ Yes |
| **Hydrogen (H₂)** | Large Pressure Tank | -10 to 20°C | 0-50 MPa | **HIGHLY FLAMMABLE** - Keep away from ignition sources | Auto-ignition: 500°C | ✗ NO (DANGEROUS) |
| **Methane (CH₄)** | Large Pressure Tank | -10 to 20°C | 0-50 MPa | **HIGHLY FLAMMABLE** - Keep isolated | Auto-ignition: 595°C | ✗ NO (DANGEROUS) |
| **Argon (Ar)** | Large Pressure Tank | -10 to 30°C | 0-50 MPa | Inert, stable | Non-reactive | ✓ Yes |
| **Ammonia (NH₃)** | Large Pressure Tank | -5 to 25°C | 0-50 MPa | Toxic, corrosive at high concentrations | Noxious | ✓ Yes (with caution) |
| **Volatile (Mixed Organics)** | Large Pressure Tank | -20 to 10°C | 0-50 MPa | **FLAMMABLE** - Temperature sensitive | Auto-ignition: 200-400°C range | ✗ NO (DANGEROUS) |
| **Pollutant (Waste)** | Large Pressure Tank | -10 to 30°C | 0-50 MPa | Mixed contaminants | Low value, takes space | ✓ Yes (minimal profit) |

**Gas Storage Setup Checkpoints:**
- M1.2a: Build large tank for O₂ (safe, primary trade good)
- M1.2b: Build large tank for N₂ (inert, common trade good)
- M1.2c: Build large tank for CO₂ (inert, exportable)
- M1.2d: Build large tank for Ar (specialty gas, optional)
- M1.2e: Build cooler/heater circuit for each tank (maintain temp range)
- M1.2f: Install pressure gauges on all tanks (monitor capacity)
- M1.2g: **DO NOT build tanks for H₂, CH₄, or Volatiles** (too dangerous; not storing)
- M1.2h: Optional: Small tank for NH₃ (only if willing to manage hazard)

**Solid Trade Storage Criteria:**
- [ ] Ore containers organized by type (Iron, Copper, Silicon, Gold, etc.)
- [ ] Easy access for loading onto trade platform
- [ ] Inventory tracking to prevent overfill
- [ ] Separate from other storage (ore dust contamination)

---


## **CATEGORY N: ROCKETRY — Fuel & Rocket Mining**


### **Project N1: Rocket Fuel Production Chain**
**Success Criteria:**
- [ ] Volatiles mined/extracted reliably
- [ ] Fuel refinement chain operational (Volatiles → refined rocket fuel)
- [ ] Fuel storage tank sized for full rocket launch + reserve
- [ ] Production rate exceeds single-launch consumption
- [ ] Fuel tank isolated from habitation (flammable, high hazard)

**Checkpoints:**
- N1.1: Identify Volatiles source (mining or gas extraction)
- N1.2: Build refinement chain (furnace/distillation to rocket fuel)
- N1.3: Build isolated, ventilated fuel storage tank
- N1.4: Test single full-tank production cycle
- N1.5: Verify storage safety (isolation from crew areas, temp control)
- N1.6: Confirm reserve fuel for return trip or second launch

---


### **Project N2: Rocket Mining Operations**
**Success Criteria:**
- [ ] Rocket assembled and tested
- [ ] Successful launch and landing on target body
- [ ] Mining equipment deployed on secondary location
- [ ] Resources extracted and returned (or colony established)
- [ ] Return trip feasible with gathered resources

**Checkpoints:**
- N2.1: Gather rocket components via fabrication/trade
- N2.2: Build rocket frame and pressurize
- N2.3: Install fuel (from N1 Fuel Production), guidance systems, cargo hold
- N2.4: Perform launch test (successful orbit)
- N2.5: Establish mining on secondary body
- N2.6: Execute return trip (or verify colony stability)

---


## **CATEGORY O: LEGACY & MEMORIAL — Advanced Mortuary**


### **Project O1: Mortuary Hall (Advanced — Pedestal Memorial)**
**Success Criteria:**
- [ ] Dedicated memorial room built (separate from general storage)
- [ ] Pedestal per fallen crew member/clone cycle
- [ ] Remains transferred from early lockers (E2) to pedestals
- [ ] Room lit, pressurized, and storm-safe
- [ ] Record/plaque system identifying each memorial

**Checkpoints:**
- O1.1: Design and build dedicated mortuary hall room
- O1.2: Fabricate/place pedestals (one per recorded loss)
- O1.3: Transfer remains from early lockers (E2) to pedestals
- O1.4: Add lighting and life-support to the hall
- O1.5: Label each pedestal (name/date/cause, informal record)
- O1.6: Retire the early locker system once hall is operational

---


## **CATEGORY P: RESILIENCE — System Redundancy**


### **Project P1: System Redundancy & Failsafes**
**Success Criteria:**
- [ ] Backup systems for critical functions (power, water, O₂)
- [ ] Automatic shutdown sequences when systems fail
- [ ] Manual overrides accessible (no system is fully automatic)
- [ ] Emergency protocols tested (system survives single point failure)
- [ ] Player can recover from any single system failure

**Checkpoints:**
- P1.1: Identify single points of failure in each Category D–J system
- P1.2: Build redundant path for each critical system
- P1.3: Test failover (switch to backup mid-operation)
- P1.4: Create manual override controls
- P1.5: Stress test (disable one component, verify survival)

---


## **Cross-Category Dependencies**

| Project | Depends On | Enables |
|---------|-----------|---------|
| A1 (Day 1 Mining) | None — true starting point | A2 |
| A2 (Night 1 Fabrication) | A1 (need ore/ice first) | ALL Category B+ projects |
| A3 (Brutal Day 1) | None — alternate starting point | A4 |
| A4 (Brutal Night 1) | A3 | ALL Category B+ projects |
| B1 (Basic Airlock) | A2 or A4 (need frames/walls) | B2, B3, B4, H1 |
| B2 (Hydration) | A2/A4, B1 | Milestone 1 |
| B3 (Basic Atmosphere) | B1 | D1, D1b, J1, Milestone 1 |
| B4 (Emergency Power) | A2/A4 (Solar panel from Night 1) | C1, C2, F1, Milestone 1 |
| B5 (Cooling — conditional) | B1 | Milestone 1 on hot worlds only |
| B6 (Heating — conditional) | B1, B4 | Milestone 1 on cold worlds only |
| B7 (Anchoring — conditional) | B1 | Milestone 1 on zero-g worlds only |
| B8 (Non-Solar Power — conditional) | B1 | Feeds into B4 on weak-solar worlds |
| B9 (High-Pressure Structural — conditional) | B1 | Protects B1 and all later structures on high-pressure worlds |
| C1 (Energy Infrastructure) | B4 | C2, F1, G2, G3, J2, L1, category D–P |
| C2 (Fabrication Setup) | C1 | F1, F2, I1, I2, H1, category D–P |
| D1 (Hydroponics — Minimum) | B3 | D1b, Milestone 1 |
| D1b (Food Production — Full) | D1, C1 | P1 |
| D2 (Storm Protection) | None — parallel, do early | Milestone 1 (baseline), protects all later categories |
| E1 (Cloning/Cryo) | C1, C2, B3 | E2, survival continuity |
| E2 (Mortuary — Lockers) | E1 | O1 (Advanced Mortuary) |
| F1 (Mining Operations) | C2 | F2, I2, M1, L2 |
| F2 (Storage & Inventory) | None — parallel | F1, M1, L2 |
| G1 (Thermal — Minimum) | B1 | Milestone 1, G2, G3 |
| G2 (Heating — Full) | G1, C1 | G4 |
| G3 (Cooling — Full) | G1, C1 | G4 |
| G4 (Full Automated Climate) | G2, G3 | P1 |
| H1 (Console Airlock) | B1, C1 | H2, Milestone 1 |
| H2 (IC Advanced Airlock) | H1, C2 | H3 |
| H3 (Failsafe + Backups) | H2, C1 | Emergency safety & egress |
| I1 (EVA/Spare Equipment) | C2 | E1 (Cryo recovery), I2 |
| I2 (Vehicle/Rover) | C2, F1 | Faster F1, N1 (Fuel gathering) |
| J1 (Distillation) | C1, C2 | D1b, M1 |
| J2 (Waste Management) | C1 | P1 |
| K1 (Goal Board) | None — informational | Player organization |
| L1 (Power Automation) | C1 | Optional optimization |
| L2 (Automated Crafting) | C2, F2 | System efficiency |
| M1 (Trade Infrastructure) | C1, F1 | N2 (Rocket) |
| N1 (Rocket Fuel Chain) | F1, I2 | N2 (Rocket) |
| N2 (Rocket Mining) | C2, M1, N1 | End-game option |
| O1 (Mortuary — Pedestals) | E2 | Aesthetic/roleplay milestone |
| P1 (Redundancy) | G4, D1b, J2 systems established | Survival stability |

---


## **Measurement & Success**
Each project is **independently measurable**:
- Checkpoints are **binary** (done/not done)
- Success criteria are **quantifiable** (temperature ranges, pressure levels, power output, etc.)
- Can track progress across all Tiers simultaneously
- Failure in one project doesn't block others (except dependencies listed above)

---


## **Steam Workshop Resources** 
*(Useful blueprints and IC10 scripts by project)*

| Project | Workshop Reference | URL / Note |
|---------|-------------------|-----------|
| **H1 Console Airlock** | Super Simple Autocycling Airlock | ID: 1232888907 - Setup guide for console-based airlock |
| **H1 Console Airlock** | Airlock Control | ID: 1524868713 - IC-based airlock using door buttons (compatibility note) |
| **H2 IC Advanced Airlock** | Emergency Bulkhead Lockdown | ID: 2258102536 - IC10 + gas sensors, auto-open on pressure match |
| **L1 Power Automation** | [F&S] Emergency Power System | ID: 1696723430 - Auto backup generator (turn on <5%, off >50%) |
| **L1 Power Automation** | Power controller V2 | ID: 2362230182 - Solar tracking + battery readout + generator control |
| **L1 Power Automation** | Stationeers IC10 Solar Tracker & Power Automation | XGamingServer guide - Battery-aware power manager with hysteresis |
| **L2 Automated Crafting** | Ore Sorting System (multiple) | Workshop - Sorts ores for ingot recipes & retrieval |
| **L2 Automated Crafting** | Cows Are Evil Venus Playthrough | YouTube - Full automated base with IC10 inventory management (code available on workshop) |
| **General IC10 Learning** | How to Program Anything with IC10 for the Novice | Steam guide - Core MIPS concepts, stack iteration, logic readers/writers |

