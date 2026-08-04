## **TIER 0: SURVIVAL** 
*Foundation systems needed to stay alive. Can work on these in parallel.*

### **Project 0.1: Hydration System**
**Success Criteria:**
- [ ] Pressurized room established (helmet-off zone)
- [ ] Water dispenser or vending machine accessible
- [ ] Player can sustain 24+ hours without thirst mechanic killing them
- [ ] Water supply is continuous (not one-time)

**Checkpoints:**
- 0.1.1: Create airtight room
- 0.1.2: Install water dispenser/vending
- 0.1.3: Test helmet removal and hydration
- 0.1.4: Confirm water doesn't run out

---

### **Project 0.2: Pressurized Base & Airlock System (TIER 0-1)**

#### **0.2a: BASIC AIRLOCK** (Tier 0)
**Success Criteria:**
- [ ] Main base is airtight (no pressure loss)
- [ ] Functional double-door airlock for entry/exit
- [ ] Pressure gauge shows stable levels
- [ ] No decompression deaths during operations
- [ ] Interior pressure >= 50 kPa
- [ ] Manual door controls accessible

**Checkpoints:**
- 0.2a.1: Build frame structure with walls
- 0.2a.2: Seal all openings (add extra iron sheets to frames)
- 0.2a.3: Create double-door airlock (manual operation)
- 0.2a.4: Monitor pressure over time (6+ hours stable)
- 0.2a.5: Test manual door cycle (no pressure loss)

#### **0.2b: CONSOLE-CONTROLLED AIRLOCK** (Tier 1)
**Success Criteria:**
- [ ] Airlock console (IC chip + display) installed and powered
- [ ] 2 doors (interior & exterior) controllable from console
- [ ] 2 active vents (interior & exterior) controllable from console
- [ ] Console cycles gas transfer between interior/exterior
- [ ] No manual door toggling needed (all via console)
- [ ] Airlock cycles complete in <30 seconds

**Checkpoints:**
- 0.2b.1: Insert Airlock circuitboard into console
- 0.2b.2: Place glass sheet on console
- 0.2b.3: Install data disk
- 0.2b.4: Wire both doors to console (power + data)
- 0.2b.5: Wire both active vents to console (power + data)
- 0.2b.6: Configure console: select exterior door, exterior vent, interior door, interior vent
- 0.2b.7: Test full cycle: press cycle on console, monitor door states and vent operation
- 0.2b.8: Verify both active vents draw from correct pipe networks (interior + exterior isolated)

**Reference:** See Steam Workshop "Airlock Control" (ID: 1524868713) or "Super Simple Autocycling Airlock" (ID: 1232888907) for setup guidance.

#### **0.2c: IC-BASED ADVANCED AIRLOCK** (Tier 2)
**Success Criteria:**
- [ ] IC10 circuit controls door and vent logic
- [ ] Gas sensors detect composition/pressure on both sides
- [ ] Doors remain locked if pressure/composition mismatch
- [ ] Auto-props doors open if both sides match (pressure, temp, gas %)
- [ ] Pressure equalization automatic
- [ ] System runs autonomously without player input

**Checkpoints:**
- 0.2c.1: Build IC10 housing with data network
- 0.2c.2: Deploy pressure + gas composition sensors (interior airlock)
- 0.2c.3: Wire sensors to IC10
- 0.2c.4: Wire doors + active vents to IC10
- 0.2c.5: Program IC10: compare interior/exterior sensors, hold doors until match
- 0.2c.6: Test auto-cycle with mismatched atmosphere (should NOT open)
- 0.2c.7: Test auto-cycle with matched atmosphere (should open)

**Reference:** Steam Workshop "Emergency Bulkhead Lockdown" (ID: 2258102536) uses similar logic (sensor + IC + lights).

#### **0.2d: ADVANCED AIRLOCK + FAILSAFE** (Tier 3)
**Success Criteria:**
- [ ] All 0.2c features working
- [ ] **Failsafe Mode:** If base power lost, doors default to closed (exterior side)
- [ ] **Manual Backup Controls** in every room for emergency egress
- [ ] Low-power circuit powers door locks (battery-backed, <100W draw)
- [ ] Manual override cranks/levers allow door opening without power
- [ ] Posted emergency procedures at each airlock

**Checkpoints:**
- 0.2d.1: Install isolated backup battery circuit (dedicated power line)
- 0.2d.2: Wire door locks to backup battery (NOT main power grid)
- 0.2d.3: Test: cut main power, verify doors default to closed
- 0.2d.4: Install manual crank mechanism on exterior door (no power needed)
- 0.2d.5: Install manual crank mechanism on each room interior door
- 0.2d.6: Test manual override: crank door open with main power OFF
- 0.2d.7: Post laminated emergency procedures at each airlock

---

### **Project 0.3: Emergency Power**
**Success Criteria:**
- [ ] At least one power source online (solar or fuel gen)
- [ ] Battery backup exists
- [ ] Critical systems (lights, vending) powered for 8+ hours minimum
- [ ] No power-related deaths

**Checkpoints:**
- 0.3.1: Deploy solar panels OR fuel generator
- 0.3.2: Install basic battery
- 0.3.3: Wire conduits to lights/vending
- 0.3.4: Verify 8-hour continuous power test

---

### **Project 0.4: Basic Atmosphere Control**
**Success Criteria:**
- [ ] Pressurized room has breathable O₂ (>= 19%)
- [ ] Pressure stable (not fluctuating wildly)
- [ ] CO₂ levels manageable (not suffocating)
- [ ] Player can breathe in base for extended periods

**Checkpoints:**
- 0.4.1: Identify current atmospheric composition
- 0.4.2: Deploy O₂ source (manual or temporary)
- 0.4.3: Set up basic CO₂ scrubber/vent
- 0.4.4: Monitor atmosphere for 12+ hours stable

---

## **TIER 1: FOUNDATION**
*Core infrastructure systems. These enable all future progression.*

### **Project 1.1: Energy Infrastructure**
**Success Criteria:**
- [ ] Multiple power sources operational (2+ generators or solar arrays)
- [ ] Total capacity >= peak load consumption
- [ ] Battery storage >= 6 hours of base consumption
- [ ] Distributed conduit network (no power bottlenecks)
- [ ] Redundancy: base survives loss of one power source

**Checkpoints:**
- 1.1.1: Map current power consumption
- 1.1.2: Build second power source (diversify fuel/solar)
- 1.1.3: Install sufficient battery capacity
- 1.1.4: Create separate power circuits (primary/backup)
- 1.1.5: Stress test during high load

---

### **Project 1.2: Fabrication Setup**
**Success Criteria:**
- [ ] Fabricator online and functional
- [ ] Can produce basic metal components
- [ ] Supply chain for fabricator inputs established
- [ ] Sufficient power allocated for fabrication cycles
- [ ] Queue system prevents bottlenecks

**Checkpoints:**
- 1.2.1: Acquire/build fabricator
- 1.2.2: Power and wire fabricator
- 1.2.3: Test basic component production (frames, doors)
- 1.2.4: Establish input material source
- 1.2.5: Verify queue doesn't jam

---

### **Project 1.3: Storage & Inventory Management**
**Success Criteria:**
- [ ] Organized storage for: ores, ingots, components, consumables
- [ ] Each storage category labeled/accessible
- [ ] Inventory doesn't overflow (items lost)
- [ ] Easy retrieval of frequently-used items
- [ ] Sufficient capacity for 30+ days production

**Checkpoints:**
- 1.3.1: Build storage containers/lockers
- 1.3.2: Assign storage purposes (ore vs ingot vs component)
- 1.3.3: Test retrieval speed (can find items quickly)
- 1.3.4: Monitor overflow incidents (should be zero)

---

### **Project 1.4: Basic Mining Operations**
**Success Criteria:**
- [ ] Mining producing ore consistently
- [ ] Ore collected and stored
- [ ] Mining doesn't deplete nearby resources too quickly
- [ ] Supply rate >= demand rate for fabrication

**Checkpoints:**
- 1.4.1: Identify mineable ore deposits
- 1.4.2: Set up manual or simple automated mining
- 1.4.3: Create ore collection/storage point
- 1.4.4: Measure production rate vs consumption

---

### **Project 1.5: Storm & Weather Protection**
**Success Criteria:**
- [ ] Weather Station installed (advance storm warning)
- [ ] All exposed items secured or moved indoors before storms
- [ ] Structural repair stock maintained (duct tape, glass sheets)
- [ ] Solar panels protected or storm-rated
- [ ] Base survives 3+ consecutive storms with minimal damage
- [ ] No lost items due to storm winds

**Checkpoints:**
- 1.5.1: Build/acquire Weather Station for advance warning
- 1.5.2: Secure all loose crates/items (mount or store in lockers)
- 1.5.3: Stock duct tape and glass sheets for rapid repair
- 1.5.4: Identify planet-specific storm risk (dust/solar/cold/heat — varies by body)
- 1.5.5: Reinforce or relocate exposed solar panels
- 1.5.6: Survive first storm, log damage
- 1.5.7: Repair and adjust base layout based on damage pattern
- 1.5.8: Survive 3 consecutive storms with no item loss

**Note:** Storm behavior is planet-specific — Moon has solar storms (heat spike, faster suit O₂ burn), Mars has dust storms (visibility loss, item damage), Europa has cold buffeting winds, Venus/Vulcan have heat storms that can destroy low pressure-threshold structures like iron walls. Rooms are generally storm-safe, but rooms larger than 1200 cubes can still experience storm effects inside.

---

### **Project 1.6: EVA Suit & Spare Equipment Stock**
**Success Criteria:**
- [ ] Spare suits stocked (minimum 2 full sets: suit, helmet, backpack)
- [ ] Spare tools stocked (wrench, welder, drill, duct tape, wire cutters — 2x each)
- [ ] Spare O₂ tanks and canisters filled and racked
- [ ] Spare batteries for suits/tools
- [ ] Jetpack + fuel available if low-gravity body
- [ ] Equipment locker organized and labeled near airlock

**Checkpoints:**
- 1.6.1: Fabricate/acquire 2+ spare suits and helmets
- 1.6.2: Fabricate/acquire spare set of core tools
- 1.6.3: Build filling station for O₂ tanks/canisters
- 1.6.4: Stock spare suit batteries (charged)
- 1.6.5: Build equipment locker near primary airlock
- 1.6.6: Label locker slots (suits, tools, tanks, batteries)
- 1.6.7: Test full EVA kit-up in under 60 seconds

---

### **Project 1.7: Vehicle & Rover Operations**
**Success Criteria:**
- [ ] Rover or AIMEe mining bot operational
- [ ] Vehicle fuel/charging station established
- [ ] Vehicle garage/storm shelter for parking (prevents storm damage)
- [ ] Mining range extended beyond walking distance
- [ ] Ore transport rate improved vs manual carrying

**Checkpoints:**
- 1.7.1: Fabricate Rover or AIMEe bot
- 1.7.2: Build fuel/charge station for vehicle
- 1.7.3: Build sheltered garage (storm protection)
- 1.7.4: Test round-trip mining run
- 1.7.5: Automate return-to-base on low fuel/battery (if AIMEe)
- 1.7.6: Measure ore/hour improvement vs manual mining

**Note:** Vehicles and AIMEe left outside during storms can take damage, same as loose items — garage/shelter is not optional for long-term use.

---

## **TIER 2: CYCLES**
*Advanced systems that create closed loops. Independence required for each.*

### **Project 2.1: Thermal Regulation**
**Success Criteria:**
- [ ] Base temperature maintained 18-25°C consistently
- [ ] No freezing or overheating deaths
- [ ] Temperature fluctuation < 5°C over 12 hours
- [ ] Heating/cooling system responds to temperature changes
- [ ] All rooms monitored and stable

**Checkpoints:**
- 2.1.1: Install temperature sensors
- 2.1.2: Build heating system (incinerator or radiator)
- 2.1.3: Build cooling system (active cooler or water loop)
- 2.1.4: Automate thermostat (turn on/off by temperature)
- 2.1.5: Extended stability test (24+ hours)

---

### **Project 2.2: Thermal/Pressure Distillation**
**Success Criteria:**
- [ ] Distillation unit separating gas mixtures
- [ ] Can extract specific gases (O₂, N₂, CO₂) reliably
- [ ] Output gases pure (>95% target gas)
- [ ] Pressure maintained through distillation
- [ ] System runs autonomously

**Checkpoints:**
- 2.2.1: Build distillation apparatus
- 2.2.2: Understand distillation input requirements
- 2.2.3: Test separation of known gas mixture
- 2.2.4: Route output gases to storage/use
- 2.2.5: Verify output purity via sensors

---

### **Project 2.3: Food Production (O₂ Gen + CO₂ Scrubbing)**
**Success Criteria:**
- [ ] Greenhouse producing crops
- [ ] Measurable O₂ generation (atmospheric sensors show increase)
- [ ] CO₂ consumption visible (levels drop when plants active)
- [ ] Crops growing continuously (reproducible seed supply)
- [ ] Atmospheric O₂ maintained >= 19% via plants alone

**Checkpoints:**
- 2.3.1: Build greenhouse with proper pressure
- 2.3.2: Acquire seeds and planting medium
- 2.3.3: Establish light source (natural or artificial)
- 2.3.4: Measure O₂/CO₂ change over 12 hours
- 2.3.5: Verify harvest produces seeds for next cycle

---

### **Project 2.4: Waste Management**
**Success Criteria:**
- [ ] Excess CO₂ routed away from habitable areas
- [ ] Heat from generators dissipated safely
- [ ] No waste gas buildup in base
- [ ] Exhaust vented or recycled systematically
- [ ] System scales with production increases

**Checkpoints:**
- 2.4.1: Identify waste gas sources
- 2.4.2: Build exhaust routing (vents/pipes)
- 2.4.3: Route waste away from living areas
- 2.4.4: Monitor atmospheric purity (no contamination)
- 2.4.5: Test during high-load periods

---

### **Project 2.5: Cloning/Cryo Vat System**
**Success Criteria:**
- [ ] Cryotube(s) installed and operational
- [ ] Powered continuously (dedicated circuit, no brownouts)
- [ ] Housed in pressurized, breathable atmosphere room
- [ ] Respawn point configured at cryotube
- [ ] Passive healing boost confirmed functional (up to 75% faster)
- [ ] Death recovery tested (respawn successfully with gear/location intact)

**Checkpoints:**
- 2.5.1: Unlock/fabricate Cryotube recipe (Autolathe MK2)
- 2.5.2: Build dedicated cryo room (pressurized, breathable)
- 2.5.3: Wire cryotube to stable power circuit
- 2.5.4: Place Kit (Respawn) to set spawn point at cryotube
- 2.5.5: Test passive healing (enter tube, verify accelerated recovery)
- 2.5.6: Test death/respawn cycle (verify spawn at cryotube, not random location)
- 2.5.7: Stock spare gear near cryotube for post-respawn kit-up

**Note:** Respawning applies a temporary debuff affecting food/water consumption speed, tool usage speed, and trader prices — plan spare consumables near the cryotube for recovery. On death, the body remains recoverable for a period before decaying — a nearby corpse-retrieval plan (see Mortuary, Project 2.6) helps recover lost gear.

---

### **Project 2.6: Mortuary (Early Stage — Storage Lockers)**
**Success Criteria:**
- [ ] Designated storage locker(s) for recovered remains/skulls
- [ ] Located near cryo room or base entrance
- [ ] Labeled and separate from general storage
- [ ] Simple record of losses kept (for later pedestal hall)

**Checkpoints:**
- 2.6.1: Designate locker(s) specifically for remains
- 2.6.2: Label locker clearly (separate from tool/ore storage)
- 2.6.3: Establish retrieval routine after each death (recover body/items before decay)
- 2.6.4: Track names/dates informally for future memorial hall

---

## **TIER 3: OPTIMIZATION**
*Advanced automation and integration. Full economic system.*

### **Project 3.1: Power Automation & Cycling**
**Success Criteria:**
- [ ] Non-critical circuits identified and automatable
- [ ] Motion sensors deployed in work areas
- [ ] Lights cycle on/off based on occupancy
- [ ] Standby power consumption minimized
- [ ] Peak load reduced by >= 20% vs constant run
- [ ] Generator backup auto-activates when battery drops below threshold
- [ ] Generator auto-deactivates when battery charges above threshold (with hysteresis)

**Checkpoints:**
- 3.1.1: Audit all circuits (identify which can cycle)
- 3.1.2: Install motion sensors in each room
- 3.1.3: Wire motion sensors to light circuits via batch logic (reader → writer)
- 3.1.4: Test sensor responsiveness (lights trigger/stop correctly)
- 3.1.5: Measure power savings over 24 hours
- 3.1.6: Build IC10 circuit for battery monitoring
- 3.1.7: Wire backup generator to IC10 + battery (data + power)
- 3.1.8: Program IC10: turn ON generator if battery < 25%, turn OFF if battery > 90%
- 3.1.9: Test failover: simulate battery drain, verify generator starts
- 3.1.10: Test shutdown: simulate battery charging, verify generator stops

**Reference:** Steam Workshop "[F&S] Emergency Power System" (ID: 1696723430) or "Power controller V2" (ID: 2362230182) for backup generator automation. Also see "Stationeers IC10 Solar Tracker & Power Automation" guide for battery-aware power management with hysteresis.

---

### **Project 3.2: Trade Infrastructure**
**Success Criteria:**
- [ ] Trade platform functional and accessible
- [ ] Storage for trade goods (gases + solids/ores)
- [ ] Profitable trade cycle established (input < output value)
- [ ] Trader visits reliably
- [ ] No loss of goods during trade
- [ ] All tradable gases stored in large tanks with proper temp/pressure controls
- [ ] Understand trade runs on in-game currency — sell goods for credits, then spend credits to buy

**Checkpoints:**
- 3.2.1: Build/acquire trade platform
- 3.2.2: Create dedicated storage (gas tanks + ore containers)
- 3.2.3: Establish connection between production and trade storage
- 3.2.4: Execute first SELL transaction (goods → credits)
- 3.2.5: Execute first BUY transaction (credits → goods/blueprints)
- 3.2.6: Monitor multiple trade cycles (consistency)

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
- 3.2.2a: Build large tank for O₂ (safe, primary trade good)
- 3.2.2b: Build large tank for N₂ (inert, common trade good)
- 3.2.2c: Build large tank for CO₂ (inert, exportable)
- 3.2.2d: Build large tank for Ar (specialty gas, optional)
- 3.2.2e: Build cooler/heater circuit for each tank (maintain temp range)
- 3.2.2f: Install pressure gauges on all tanks (monitor capacity)
- 3.2.2g: **DO NOT build tanks for H₂, CH₄, or Volatiles** (too dangerous; not storing)
- 3.2.2h: Optional: Small tank for NH₃ (only if willing to manage hazard)

**Solid Trade Storage Criteria:**
- [ ] Ore containers organized by type (Iron, Copper, Silicon, Gold, etc.)
- [ ] Easy access for loading onto trade platform
- [ ] Inventory tracking to prevent overfill
- [ ] Separate from other storage (ore dust contamination)

---

### **Project 3.3: Automated Crafting System**
**Success Criteria:**
- [ ] Inventory tracking system in place (manual or automated logging)
- [ ] Vending machines pull items and register consumption
- [ ] Items pulled from vending trigger auto-queue in fabricator
- [ ] Crafted items automatically returned to stock
- [ ] System maintains minimum stock levels (configurable)
- [ ] No lost items or inventory desync
- [ ] Reduces player manual crafting time by 80%+

**Checkpoints:**
- 3.3.1: Map all vending machines and their primary items
- 3.3.2: Create inventory log sheet (manual or digital tracking)
- 3.3.3: Establish baseline stock levels for each common item
- 3.3.4: Build material input buffer near fabricator
- 3.3.5: Set up automated routing: vending → item removed → fabricator signal
- 3.3.6: Wire fabricator to auto-queue when stock drops below threshold
- 3.3.7: Route fabricated items back to vending storage
- 3.3.8: Test full cycle (pull item → craft → restock) 5+ times
- 3.3.9: Monitor for 24+ hours (verify no bottlenecks or desync)

**Items to Automate (Priority Order):**
1. **Tier 0-1:** Water (from vending) → Minimum stock: 50+
2. **Tier 1:** Frames, doors, pumps, batteries → Stock: 20+ each
3. **Tier 1-2:** Regulators, valves, sensors → Stock: 10+ each
4. **Tier 2-3:** Advanced components (distillers, coolers) → Stock: 5+ each

**System Requirements:**
- Fabricator must have dedicated power circuit (no brownouts)
- Input material storage must not jam (regular clearing)
- Vending machine pull sensors or logic readers (detect item removal)
- Conveyors/chutes for material routing (fabricator → vending storage)
- IC10 circuit for inventory monitoring and queue management
- Logic writers to trigger fabricator queue based on stock levels

**Reference:** Steam Workshop contains ore sorting systems and IC10 crafting controllers. Community practice uses: Vending Machines (100 slots inventory) + Logic Readers (detect stock level) + Logic Writers (trigger fabricator) + Conveyors (route output). Example: Cows Are Evil on YouTube has documented full automation on Venus playthrough with IC10 code available on workshop.

---

### **Project 3.3b: Goal Board System**
**Success Criteria:**
- [ ] Centralized task/priority board visible to player
- [ ] Daily/session priorities posted
- [ ] Track completion status of major systems
- [ ] Identifies next critical project
- [ ] Difficulty-appropriate goals (Standard: helmet-off for hydration)
- [ ] Updates based on current base state

**Checkpoints:**
- 3.3b.1: Create physical goal board (whiteboard, sign, or monitor)
- 3.3b.2: Post current Tier completion status
- 3.3b.3: List next 3-5 priority projects
- 3.3b.4: Track daily resource production targets
- 3.3b.5: Log maintenance schedules (sensor checks, power audits)
- 3.3b.6: Update weekly (or after major system completion)

**Sample Goal Board Layout (Difficulty: Standard):**

```
═══════════════════════════════════════════════════════════════
                     STATION STATUS BOARD
═══════════════════════════════════════════════════════════════

TIER -1 - LANDING DAY:
  [✓] -1.1 Day 1 Mining Run (150g+ Fe, Cu, Au, ice collected)
  [✓] -1.2 Night 1 Fabrication (Autolathe, Furnace, Electronics Printer online)

TIER 0 - SURVIVAL:
  [✓] 0.1 Hydration (remove helmet to drink)
  [✓] 0.2a Basic Airlock (manual double-door)
  [✓] 0.3 Emergency Power (solar online)
  [✓] 0.4 Atmosphere (O₂ >= 19%)

TIER 1 - FOUNDATION:
  [✓] 1.1 Energy Infrastructure (2x power sources)
  [✓] 1.2 Fabrication (fabricator online)
  [IP] 1.3 Storage (85% complete - sort ores by type)
  [ ] 1.4 Mining Operations (set production target: 100 Fe/hour)
  [ ] 1.5 Storm & Weather Protection (weather station not built)
  [ ] 1.6 EVA/Spare Equipment Stock (0/2 spare suits)
  [ ] 1.7 Vehicle/Rover Operations (not started)

TIER 2 - CYCLES:
  [IP] 2.1 Thermal Regulation (heating working, cooling next)
  [ ] 2.2 Distillation (not started)
  [ ] 2.3 Food Production (seeds acquired, greenhouse frame built)
  [ ] 2.4 Waste Management (exhaust venting planned)
  [ ] 2.5 Cloning/Cryo Vat (recipe unlocked, room not built)
  [ ] 2.6 Mortuary - Lockers (not started)

TIER 3 - OPTIMIZATION:
  [ ] 3.1 Power Automation (motion sensors ordered)
  [ ] 3.2 Trade Infrastructure (gas tanks planned)
  [ ] 3.3 Automated Crafting (in progress - 30% wired)
  [ ] 3.3b Goal Board (THIS ONE)
  [ ] 3.4b Mortuary - Pedestals (long-term)
  [ ] 3.5 Rocket Fuel Chain (long-term)
  [ ] 3.6 Rocket Mining (long-term goal)
  [ ] 3.7 Redundancy (power backup planned)

═══════════════════════════════════════════════════════════════
TODAY'S PRIORITIES:
  1. Complete 1.3 Storage (30 min estimated)
  2. Build cooling system for 2.1 (60 min)
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

### **Project 3.4b: Mortuary Hall (Advanced — Pedestal Memorial)**
**Success Criteria:**
- [ ] Dedicated memorial room built (separate from general storage)
- [ ] Pedestal per fallen crew member/clone cycle
- [ ] Remains transferred from early lockers (2.6) to pedestals
- [ ] Room lit, pressurized, and storm-safe
- [ ] Record/plaque system identifying each memorial

**Checkpoints:**
- 3.4b.1: Design and build dedicated mortuary hall room
- 3.4b.2: Fabricate/place pedestals (one per recorded loss)
- 3.4b.3: Transfer remains from early lockers (2.6) to pedestals
- 3.4b.4: Add lighting and life-support to the hall
- 3.4b.5: Label each pedestal (name/date/cause, informal record)
- 3.4b.6: Retire the early locker system once hall is operational

---

### **Project 3.5: Rocket Fuel Production Chain**
**Success Criteria:**
- [ ] Volatiles mined/extracted reliably
- [ ] Fuel refinement chain operational (Volatiles → refined rocket fuel)
- [ ] Fuel storage tank sized for full rocket launch + reserve
- [ ] Production rate exceeds single-launch consumption
- [ ] Fuel tank isolated from habitation (flammable, high hazard)

**Checkpoints:**
- 3.5.1: Identify Volatiles source (mining or gas extraction)
- 3.5.2: Build refinement chain (furnace/distillation to rocket fuel)
- 3.5.3: Build isolated, ventilated fuel storage tank
- 3.5.4: Test single full-tank production cycle
- 3.5.5: Verify storage safety (isolation from crew areas, temp control)
- 3.5.6: Confirm reserve fuel for return trip or second launch

---

### **Project 3.6: Rocket Mining Operations**
**Success Criteria:**
- [ ] Rocket assembled and tested
- [ ] Successful launch and landing on target body
- [ ] Mining equipment deployed on secondary location
- [ ] Resources extracted and returned (or colony established)
- [ ] Return trip feasible with gathered resources

**Checkpoints:**
- 3.6.1: Gather rocket components via fabrication/trade
- 3.6.2: Build rocket frame and pressurize
- 3.6.3: Install fuel (from 3.5 Fuel Production), guidance systems, cargo hold
- 3.6.4: Perform launch test (successful orbit)
- 3.6.5: Establish mining on secondary body
- 3.6.6: Execute return trip (or verify colony stability)

---

### **Project 3.7: System Redundancy & Failsafes**
**Success Criteria:**
- [ ] Backup systems for critical functions (power, water, O₂)
- [ ] Automatic shutdown sequences when systems fail
- [ ] Manual overrides accessible (no system is fully automatic)
- [ ] Emergency protocols tested (system survives single point failure)
- [ ] Player can recover from any single system failure

**Checkpoints:**
- 3.7.1: Identify single points of failure in each Tier 2 system
- 3.7.2: Build redundant path for each critical system
- 3.7.3: Test failover (switch to backup mid-operation)
- 3.7.4: Create manual override controls
- 3.7.5: Stress test (disable one component, verify survival)

---

## **Cross-Tier Dependencies**

| Project | Depends On | Enables |
|---------|-----------|---------|
| -1.1 (Day 1 Mining) | None (true starting point) | -1.2 |
| -1.2 (Night 1 Fabrication) | -1.1 (need ore/ice first) | ALL Tier 0+ projects |
| 0.1 (Hydration) | -1.2, 0.2a (Basic Airlock) | All Tier 1+ |
| 0.2a (Basic Airlock) | -1.2 (need frames/walls) | 0.1, 0.3, 0.4 |
| 0.2b (Console Airlock) | 0.2a, 1.1 (Basic+Energy) | 0.2c, Tier 2+ |
| 0.2c (IC Advanced Airlock) | 0.2b, 1.2 (Console+Fabrication) | 0.2d |
| 0.2d (Failsafe + Backups) | 0.2c, 1.1 (IC+Energy) | Emergency safety & egress |
| 0.3 (Emergency Power) | -1.2 (Solar panel deployed Night 1) | 1.1, 1.2, 1.4 |
| 0.4 (Atmosphere) | 0.2a (Basic Airlock) | 2.3 (Food), 2.2 (Distill) |
| 1.1 (Energy Infra) | 0.3 (Emergency) | 1.2, 1.4, 2.x, 3.x |
| 1.2 (Fabrication) | 1.1 (Energy) | 1.3, 1.4, 1.6, 1.7, 2.x, 3.x |
| 1.3 (Storage) | None (parallel) | 1.4, 2.x, 3.2, 3.3 |
| 1.4 (Mining) | 1.2 (Fabrication) | 1.3, 1.7, 3.2, 3.3 |
| 1.5 (Storm Protection) | None (parallel, do early) | Protects all Tier 1+ systems |
| 1.6 (EVA/Spare Equipment) | 1.2 (Fabrication) | 2.5 (Cryo recovery), 1.7 |
| 1.7 (Vehicle/Rover) | 1.2, 1.4 (Fab+Mining) | Faster 1.4, 3.5 (Fuel gathering) |
| 2.1 (Thermal) | 1.1 (Energy) | 3.7 |
| 2.2 (Distillation) | 1.1, 1.2 (Energy+Fab) | 2.3, 3.2 |
| 2.3 (Food) | 0.4, 1.1 (Atmosphere+Energy) | 3.7 |
| 2.4 (Waste) | 1.1 (Energy) | 3.7 |
| 2.5 (Cloning/Cryo) | 1.1, 1.2, 0.4 (Energy+Fab+Atmosphere) | 2.6, survival continuity |
| 2.6 (Mortuary - Lockers) | 2.5 (Cryo/death cycle exists) | 3.4b (Advanced Mortuary) |
| 3.1 (Power Automation) | 1.1 (Energy Infra built) | Optional optimization |
| 3.2 (Trade) | 1.1, 1.4 (Energy+Mining) | 3.6 (Rocket) |
| 3.3 (Auto Crafting) | 1.2, 1.3 (Fab+Storage) | System efficiency |
| 3.3b (Goal Board) | None (informational) | Player organization |
| 3.4b (Mortuary - Pedestals) | 2.6 (Early lockers established) | Aesthetic/roleplay milestone |
| 3.5 (Rocket Fuel Chain) | 1.4, 1.7 (Mining+Vehicle for Volatiles) | 3.6 (Rocket) |
| 3.6 (Rocket Mining) | 1.2, 3.2, 3.5 (Fabrication+Trade+Fuel) | End-game option |
| 3.7 (Redundancy) | All Tier 2 systems | Survival stability |

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
| **0.2b Console Airlock** | Super Simple Autocycling Airlock | ID: 1232888907 - Setup guide for console-based airlock |
| **0.2b Console Airlock** | Airlock Control | ID: 1524868713 - IC-based airlock using door buttons (compatibility note) |
| **0.2c IC Advanced Airlock** | Emergency Bulkhead Lockdown | ID: 2258102536 - IC10 + gas sensors, auto-open on pressure match |
| **3.1 Power Automation** | [F&S] Emergency Power System | ID: 1696723430 - Auto backup generator (turn on <5%, off >50%) |
| **3.1 Power Automation** | Power controller V2 | ID: 2362230182 - Solar tracking + battery readout + generator control |
| **3.1 Power Automation** | Stationeers IC10 Solar Tracker & Power Automation | XGamingServer guide - Battery-aware power manager with hysteresis |
| **3.3 Automated Crafting** | Ore Sorting System (multiple) | Workshop - Sorts ores for ingot recipes & retrieval |
| **3.3 Automated Crafting** | Cows Are Evil Venus Playthrough | YouTube - Full automated base with IC10 inventory management (code available on workshop) |
| **General IC10 Learning** | How to Program Anything with IC10 for the Novice | Steam guide - Core MIPS concepts, stack iteration, logic readers/writers |

