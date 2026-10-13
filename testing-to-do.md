# Testing to-do: Salty's Stationeers mods

Everything still waiting on an in-game check, across all four mods. Status as of **2026-10-06**. All four mods are built from their latest commits and installed.

**How to use this:** tick boxes as you go. When something fails, note what you saw next to it, quit the game, and send the log (see *Reporting back* at the bottom). Sections are ordered by priority. **★ = high priority**: a recent bug fix or something never tested.

| Mod | Branch | Installed to |
|---|---|---|
| Salty's Droid Dual Battery | `droid-dual-battery-mod` | `mods/SaltysDroidDualBattery/` |
| Salty's Advanced Airlock | `airlock-mod-card` | `mods/AirlockCardMod/` (**moved** from `BepInEx/plugins/`) |
| Salty's Yield Multiplier | `yield-multiplier-mod` | `mods/SaltysYieldMultiplier/` |
| Salty's Fabricator Overflow | `fabricator-overflow-mod` | `mods/SaltysFabricatorOverflow/` |

---

## 0. Pre-flight (do first, takes 2 minutes)

- [ ] Launch Stationeers. If LaunchPad asks to restart, restart.
- [ ] All four mods are listed and enabled in LaunchPad's mod list.
- [ ] Optional but helpful: turn on **Debug → VerboseLogging** for every mod you plan to test in LaunchPad's config.
- [ ] Load a save, then quit, and check `BepInEx/LogOutput.log` for these lines. Each patch line should appear **once**:
  - **Droid:** `Droid second battery slot patch succeeded`, `Robot battery selection patch succeeded`, `HUD slot button patch succeeded`, `Droid Sleeper slot-2 charging patch succeeded`, `Battery label localization patch succeeded`, `Disposable charger targeting patch succeeded`
  - ★ **Airlock:** `[Salty's Advanced Airlock]: Patch succeeded` exactly **once**, followed by `Already patched by an earlier Awake() call -- skipping`. Two "Patch succeeded" lines would mean it's loading twice, from the old and new locations.
  - **Yield:** all eight patch lines plus `PureIce freeze/melt pair succeeded`
  - **Fabricator:** `Insert tracking patch succeeded`, `Overflow eject patch succeeded`
  - **No line containing `failed`** from any Salty mod.
- [ ] ★ Confirm `Stationeers/BepInEx/plugins/` contains **no** `AirlockCardMod` folder. The install script removed it.

Known noise you can ignore: red `Could not cast to InstructionData : /instruction.xml` (vanilla IC10 script library), and `MissingMethodException … filtercleanermod` (the Filter Cleaner mod, not ours).

---

## 1. Droid Dual Battery

Already confirmed (2026-10-05): second slot on an existing save · switchover α→β · hot-swap · save/reload · loot both from a corpse · sleeper charges both in every combination · "Battery 1 / Battery 2" labels.

### New since the last test
- [ ] ★ **Localized labels.** As a droid, the buttons read **"Battery α"** (#5) and **"Battery β"**.
- [ ] **Language switch.** Change the game language in settings while in-game. Both labels should update to that language's word for "Battery", still with α/β, and the text should render properly with no missing-glyph boxes (try a CJK language if you can). Switch back to English.
- [ ] **Human character.** A human or Zrilian character sees no β button, and #5 reads "Uniform" again.
- [ ] ★ **Handheld charger (bug fix).** Put a **dead** battery in α and a **charged** one in β, then use a Disposable Battery Charger on yourself. **α should charge**, not β.
  - [ ] Then with α full and β low, the charger should charge β.
  - [ ] Then with both full and a suit on, it should charge the suit battery (vanilla behaviour).

### Regression (quick re-check after this round of changes)
- [ ] Drain α and confirm the droid keeps running on β.
- [ ] Sleeper charges both slots.

---

## 2. Advanced Airlock

### ★ New install location and bug fixes (do these first)
- [ ] ★ Pre-flight airlock log lines above are correct: patched once, no double load.
- [ ] ★ An existing airlock still works: the log shows `Failsafe layer attached, Tier=…`, and a normal cycle works from both buttons.
- [ ] ★ **Multi-airlock fix.** Build or use **3 Advanced Airlocks** on the same save. This is the case that was broken; 2 or 4 airlocks wouldn't have shown it.
  - [ ] Each airlock independently responds to power state. For example, drop the Station Battery into the Low or Critical range and confirm **all three** react, not just one.
  - [ ] Opening a door on **each** airlock is noticed. The `OnDoorOpened` behaviour applies on all three; with VerboseLogging the log shows it per airlock.
  - [ ] Deconstruct one of the three mid-session. No errors, and the other two keep working.

### Fixes from 2026-08-08, committed but never re-tested
- [ ] ★ **Critical-tier escape no longer traps you.** Enter Critical tier. The chamber evacuates, then the doors unlock **once**, and you can walk out. Before the fix, they re-locked every tick.
  - [ ] First button press after escaping re-locks and opens the requested side.
  - [ ] Second press re-closes, re-locks, and runs a normal vanilla cycle.
- [ ] ★ **No stale vents (gas-mixing fix).** After a Critical-tier stay ends, both Active Vents are **off**. On the next normal cycle, no gas mixes between sides.
- [ ] Check the log for the `UnlockDoors()` / `LockDoors()` / `CloseDoor(...)` diagnostic lines. They print lock and open state, so paste them if anything looks off.

### Door and vent primitives never verified in-game
These are marked `NOT YET VERIFIED IN-GAME` in `AdvancedAirlockControlHost.cs`:
- [ ] `ForceEvacuate()`: chamber pumped to vacuum, both doors closed and locked.
- [ ] `UnlockDoors()`: both doors unlocked; a tool-less player can crowbar them.
- [ ] `LockDoors()`: both doors locked; vanilla cycling (`IsOperable`) works again afterwards.
- [ ] `HoldBothDoorsOpen()`: propped-open mode holds both doors open.
- [ ] `OpenDoor(side)`: opens only the requested side.

### Power tiers (Station Battery design)
Setup: a Station Battery with Data IO on the airlock Console's network.
- [ ] **Normal:** downstream power on, buttons pass straight through to vanilla cycling.
- [ ] **Low (idle-saving):** downstream circuit powers off while idle.
  - [ ] Press a button. It wakes, opens **only** that side's door, and powers down again after the chamber was entered and then vacated.
- [ ] **Critical:** see the escape test above.
- [ ] Hysteresis: hovering around a tier boundary doesn't flicker between tiers.
- [ ] ★ **"Sub APC won't power on" report** (diagnostic logging added in commit `8830afb`). Reproduce it and send the `SetDownstreamPower` log lines.

### Atmosphere safety (from the latest commits)
- [ ] Propped-open handling only activates on genuinely safe, matched atmospheres, and doesn't hijack a cycle already in progress.
- [ ] Pressure-decline fail-safe: a leaking chamber is detected.
- [ ] Vent relief is target-driven: it stops at the 10 MPa cap and doesn't run as a blind pulse.
- [ ] LED idle-flash shows as expected.

Not wired yet, so nothing to test: Button C hold, the vanilla-cycle-requested signal, presence sensors and maintenance mode. These are stubbed to `false` in the host.

---

## 3. Yield Multiplier

The full procedure is in `ore-yield-multiplier-mod/TEST_PLAN.md` (sections A–J). Already ticked there: startup, basic Furnace iron, Arc Furnace copper, Steel.

### ★ New bug fixes
- [ ] ★ **Arc Furnace large batch (clamp fix).** At a high multiplier (try **20×**), feed the Arc Furnace a big batch of one ore, enough that the multiplied output would pass 500. You should get the **full multiplied total** across several output stacks, not one capped stack followed by nothing. With VerboseLogging, look for `ArcFurnaceIngotPoolPatch: refunded N unclamped units`.
- [ ] **Regression at 5×:** a normal small Arc Furnace batch still gives 5×.
- [ ] **Ice melt still correct after the restore change.** Melt Volatile Ice and confirm 5× gas, **and** melt a second piece to confirm it's still 5×, not compounding to 25×.

### Still open in TEST_PLAN.md
- [ ] A3: drag a slider to an odd value (e.g. 3.7). The log shows it snapped (4).
- [ ] A5: startup log lines.
- [ ] B7: Iron in the **Advanced Furnace** gives 5×.
- [ ] C10: **Electrum** (two-ingredient alloy) is correct.
- [ ] D11: stack-size clamp edge case in the regular furnaces.
- [ ] E12–14: Volatile Ice in-world, Volatile Ice in a furnace, Oxite. Before and after amounts in the log.
- [ ] F15: trace gas from a plain ore is multiplied too (expected side effect).
- [ ] ★ G16–17: **freeze/thaw nets to zero.** Freeze a known amount of gas into PureIce, melt it, and the amount matches. Do the small-amount case too.
- [ ] ★ H19–20: **anti-exploit.** The smelt → dirty ore → Centrifuge cycle gives no net gain.
- [ ] I21–22: a live config change takes effect without a restart. Setting both to 1 gives vanilla yields.
- [ ] J23: Droid/Zrilian respawn kits are unaffected.
- [ ] **Re-melt is 1:1:** melt 10 ingots back in a furnace and get 10 ingots out, not 50.
- [ ] **Furnace tooltip:** the contents list matches the "Produce N ingots" line.

---

## 4. Fabricator Overflow

Already confirmed: the cap at 500 works. No code changes in this round, so this is a quick regression only.
- [ ] Put about 800 of one ingot into an Autolathe. About 5 s after the last insert, 300 are ejected as a stack.
- [ ] Keep topping up quickly. Nothing ejects until you stop for 5 s, then it all comes out together (≤500 per stack).
- [ ] With an export chute attached, the overflow goes down the chute.
- [ ] Mixed ingots: 800 iron + 200 copper ejects only iron.
- [ ] Config: change `MaxPerIngotType` to 300 live. The next eject respects it.
- [ ] Printing still works normally, and the eject never interrupts a print in progress.

---

## 5. Multiplayer (all mods, when a second player or a dedicated server is available)

Nothing here has been tested in multiplayer yet.
- [ ] **Droid** (required on server and client): a client without the mod is refused at join, with LaunchPad's mismatch message.
- [ ] **Droid:** a client joins a server where a droid **already has a battery in β**. The battery shows in β for the client, with no errors in either log.
- [ ] **Droid:** a new droid character spawned mid-session on the server gets the β slot on the client, and moving a battery into β syncs.
- [ ] **Droid:** the sleeper and charger behave the same for a client-controlled droid.
- [ ] **Airlock:** the failsafe runs on the host; a client sees the same door and vent behaviour.
- [ ] **Yield:** client-side smelting yields match the host's config.
- [ ] **Fabricator:** overflow ejects for a client feeding a printer, as server-side logic.

---

## Reporting back

After a session, quit the game and either tell me which boxes passed or failed, or say "check the log". I'll read these myself:
- `C:\Program Files (x86)\Steam\steamapps\common\Stationeers\BepInEx\LogOutput.log`
- `%USERPROFILE%\AppData\LocalLow\Rocketwerkz\rocketstation\Player.log` (full Unity log, including exceptions)

Screenshots help for anything visual: HUD buttons, labels, tooltips.

## Future mod ideas (to-do)

- [ ] **Droid clock HUD**: a new mod that gives HEM Droids a clock readout in the lower right of the HUD, directly above the jetpack HUD item. (Requested 2026-10-07.)
