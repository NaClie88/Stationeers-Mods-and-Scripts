# Salty's Droid Standby: design spec

**Date:** 2026-10-07 · **Branch:** `droid-standby-mod` (off `main`) · **Status:** approved in conversation, awaiting written-spec review

## 1. Purpose

H.E.M. Droids running on unreliable renewables (solar overnight, wind between storms) get caught without power. This mod gives a droid ways to **spend less battery at the cost of cognition**, so it can ride out a power gap safely, plus a **safety net** that saves an AFK droid before it runs flat.

**Success looks like:**
- A droid caught at dusk can drop into a low-power state and survive until sunrise without reaching a bed.
- An AFK player whose droid is about to die is automatically put into standby. In single-player, the game is also paused.
- The player stays in control: every level is chosen by the player (except the safety net), wake conditions are picked per use, and time skip is always an explicit choice.
- It works alongside vanilla and the other Salty mods, especially **Salty's Droid Dual Battery**, with no hard dependency on any of them.

## 2. Features: three layers

| Layer | How you enter it | Effect |
|---|---|---|
| **Power Save Mode** | **Tap** the standby key | Cognition floor about 40 (≈64% speed, vignette and blur), battery drain **×0.5**, reduced jump power |
| **Deep Standby** | **Long press** the standby key, or the safety net | Cognition floor about 85 (≈24% speed, near-black, barely conscious), battery drain **×0.25**, wake conditions active, wake panel shown |
| **Time Skip** | The explicit **Fast-forward** button inside Deep Standby only | World time accelerated (default 5×, max 10×); **droid battery drain frozen** while accelerated |

- Droids only for Power Save and Deep Standby. Organics only take part in the Time Skip vote, by being asleep (§7).
- Deep Standby always runs at **normal speed** unless Time Skip is explicitly chosen. Time Skip is never on by default, never started automatically, and only offered inside Deep Standby.

## 3. Key facts from the decompile (Assembly-CSharp)

These ground the design and were verified, not assumed:

- **Droid drain** (`Human.OnLifeTick`): `RobotBattery.PowerStored -= (brainOnline ? 1 : OfflineMetabolism) × PowerDrainedPerTick × RobotBatteryRate`.
  - `PowerDrainedPerTick` is a **static** field (100; 105 while the headlamp or night vision is on).
  - `OfflineMetabolism` (0.1) applies when the player's client is disconnected (`Brain.IsOnline` = client connected), not while asleep.
- **Beds and sleepers:** `OnLifeTick` returns early while `RootParent is ILifeSuspender { IsSuspendingLife: true }`, so a droid in a Droid Sleeper, bed or cryo tube already drains **0**, and the sleeper charges it.
- **Cognition = stun.** The HUD's cognition readout is `DamageState.Stun`, and stun already drives everything this mod needs visually and physically:
  - `Entity.OnCameraUpdate`: vignette intensity 0→0.5 (stun 0→80), blur 0→1 (stun 0→50), saturation 1→0 and brightness 0.98→0 (stun 0→100).
  - `MovementController`: speed × (1 − 0.9 × stun/100).
  - `Human.OnLifeTick`: brain stun ≥ 100, or ≥ 90 while in a life suspender, means **Unconscious**; it becomes Alive again below 50.
  - `Brain`: a droid with an empty or missing battery gains stun each tick and recovers once powered.
- **No vanilla time skip.** The game only ever sets `Time.timeScale = 1`. The atmosphere, power and day/night simulation appear to run on their own tick, so whether `timeScale` speeds them up is **unknown** (§8, the feasibility probe).
- **World readings** available to the mod:
  - Light: `LightManager.AirSolarIrradiance`, dimmed by `WeatherManager.GetSolarRatioAt(height)`.
  - Wind: `WindTurbineGenerator.WindStrength` (static noise), boosted by the current storm's `WindStrength`.
  - Storm: `WeatherManager.IsWeatherEventRunning` + `CurrentEventAffects(height)`.
  - Solar storm: a running event with a non-null `DirectionalLight`, which is how vanilla gates its own solar-storm camera effect.
- **UI:** the game ships **RG.ImGui** and uses it for its console and debug windows, and StationeersLaunchPad's config window is ImGui too. A mod can draw its own ImGui panel without Unity UI prefabs.
- **Key binding:** `Z` is not bound by any vanilla default (checked against `KeyManager`).

## 4. Core mechanics

### 4.1 Cognition floor
Each level holds a **minimum stun** on the droid while it's active: Power Save **40**, Deep Standby **85**.
- After vanilla's own per-tick stun update, stun is raised to the floor if it's below it. It's never lowered, so real stun from damage or an empty battery still shows on top.
- 85 keeps the player conscious: it stays below vanilla's unconscious thresholds of 90 (life suspender) and 100. The player keeps control at about 24% speed, a deliberate trade-off against a strict 10% speed.
- Leaving a level removes the floor, and vanilla recovery brings cognition back over a few seconds, which looks like coming to.
- The floor is cleared automatically on death, on entering a life suspender (sleeper, bed or cryo, since vanilla already handles those), and on leaving the droid body.
- Which `DamageState` gets the floor (the entity's, the brain's, or both) is settled in the implementation plan by checking which one drives the camera, the movement and the unconscious check. The visual and movement effects above must result, and unconsciousness must not.

### 4.2 Drain scaling
Drain is scaled by **amount**: ×0.5 in Power Save, ×0.25 in Deep Standby, ×0 during Time Skip. **Which** battery is drained is never changed, so `Human.RobotBattery`, and with it the Dual Battery mod's α → β order, chargers and the sleeper, all stay untouched.
- **Mechanism constraint:** `PowerDrainedPerTick` is static and shared by all humans, so it must **not** be modified to scale one droid; that would race with other droids and with the headlamp's `SetPowerDrain`. The plan uses a per-droid approach instead, for example refunding the unspent share to the battery that was just drained, measured around `OnLifeTick`.

### 4.3 Jump power
Reduced in Power Save and Deep Standby, configurable, defaulting to the same factor as the speed reduction. The jetpack is not affected.

### 4.4 Battery reading
For the wake conditions and the safety net, battery means **the sum over every battery-type slot on the droid** (`PowerStored / PowerMaximum` summed). That's correct with or without the Dual Battery mod's second slot, with no reference to that mod.

## 5. Controls and UI

### 5.1 Key: one key, default `Z` (rebindable in LaunchPad config)

| State | Tap (< hold threshold, default 0.6 s) | Long press (≥ threshold) |
|---|---|---|
| Normal | → Power Save | → Deep Standby |
| Power Save | → Normal | → Deep Standby |
| Deep Standby | → wake to Normal | → wake to Normal |

Movement input never wakes the droid; it can still crawl.

### 5.2 Wake panel (ImGui), opened on entering Deep Standby
- One checkbox per wake condition (§6), pre-ticked from config defaults, each showing its **current reading**, e.g. `☑ Light above 30% — now 2%`.
- A **Fast-forward** button (Time Skip, §7), offered only when time skip is possible (single-player, or the multiplayer vote can pass).
- Enter confirms and collapses the panel to a one-line status: `Deep Standby — waking on: light, battery`. Clicking the status line reopens the panel. The key can't do it, because any press wakes (§5.1).
- The styling must stay readable over the near-black Deep Standby screen.

## 6. Wake conditions

Checked about **once per in-game second** while in Deep Standby. A threshold condition must hold for **3 consecutive checks** (configurable) before it fires, so a flicker or a gust doesn't wake you.

| Condition | Fires when | Notes |
|---|---|---|
| **Light** (primary) | Solar light rises above X% (default 30%) | Includes storm dimming |
| **Wind** | Wind strength rises above X | For turbine power |
| **Storm edge** | A storm **starts or ends**, compared with the state on entering standby; **solar storms included** | **Only while outdoors** (droid in the open world atmosphere, not in a room); otherwise this check is skipped |
| **Battery** | Total battery charged to X% (default 90%), or dropped to Y% (default 5%) | Uses the summed reading (§4.4) |
| **Danger** | Damage taken, a sudden pressure change, or ambient temperature outside a safe band | Temperature lives here, not as its own condition |

**On wake:** leave Deep Standby for **Normal**, stop Time Skip, play a sound, and show the reason, e.g. `Woke: sunrise (light 34%)`.

## 7. Time Skip (phases 2 and 3)

- **Only inside Deep Standby, only by pressing Fast-forward.** Speed is configurable (default 5×, max 10×).
- **While accelerated:**
  - Battery drain is frozen for every participating droid.
  - The world keeps simulating: solar, wind and the sleeper still charge; day/night, weather and atmosphere advance.
  - The status line shows `Fast-forward 5× — waking on: …`.
  - A choppy, low-frame-rate presentation suggests the droid waking up now and then (the implementation plan picks the mechanism, e.g. throttling the camera or render rate).
- **It stops on** any wake condition from any participant, any wake key press, the safety-net threshold, or the game pausing.
- **Multiplayer vote (phase 3):** acceleration runs only while **every connected player** is either in Deep Standby or **asleep** (an organic in a bed or cryo tube, i.e. vanilla `Entity.IsSleeping`). Anyone waking, joining, or leaving that state cancels it for everyone. The server owns the decision; clients report state through small custom messages (LaunchPadBooster networking).

## 8. Phase 2 opens with a feasibility probe

Before any Time Skip code: a throwaway probe answers one question. **Does raising `Time.timeScale` speed up day/night, solar charging, the weather timer and atmosphere together and correctly?**
- **Yes:** `timeScale` is the mechanism.
- **No:** look for the game's own tick-rate control and retry the probe.
- **Neither is clean:** Time Skip falls back to **"freeze drain at 1× speed"**, keeping the safety and cost benefit without acceleration.

The findings are reported and agreed before phase 2 is built. Probe code is throwaway.

## 9. AFK safety net

- **Trigger:** total battery ≤ **10%** and **no input for 60 s**, both configurable.
- **Action:** enter Deep Standby with the default wake conditions.
  - **Single-player, or effectively solo:** also **pause the game**. On return, show `Saved you at 9%` with **Resume in standby**, plus **Fast-forward** once phase 2 exists.
  - **Multiplayer with others connected:** Deep Standby only; no player can pause a shared server.
- **Effectively solo** (added 2026-10-07 at the user's request): a multiplayer **host who is the only connected player** gets the single-player features (the safety-net pause now; Time Skip without a vote in phase 2) until someone else joins. A join ends a solo pause immediately (the droid stays in Deep Standby). Rule: `!NetworkManager.IsActive || (NetworkManager.IsServer && every NetworkBase.Clients entry has IsHost)`. A lone *client* on a **dedicated server** is not covered in phase 1: pausing a server from a client is untested. Phase 3's server-owned time control covers it, since a vote of one passes.
- The safety net never starts Time Skip by itself.

## 10. Multiplayer

- The stun floor and drain scaling run on the **server**, which owns damage and battery state.
- Clients send key presses and panel choices to the server. In phase 1, standby works for every player; Time Skip arrives with phase 3.
- From the first networked feature onward, the mod sets `Networking.Required = true`: it's needed on the server and every client, and LaunchPad refuses mismatched joins.

## 11. Compatibility

- **Vanilla:** no slots, prefabs or saved data added. Standby state is **not saved**, so loading always starts at Normal and you'll never load into a near-black screen. Sleepers, beds and cryo tubes keep their vanilla behaviour.
- **Salty's Droid Dual Battery:** drain is scaled by amount on whichever battery `RobotBattery` returns (§4.2), and battery readings sum all battery-type slots (§4.4). Both mods' sleeper and charger patches keep working. There's no compile-time or runtime reference between the two.
- **Other Salty mods:** no shared patch targets.

## 12. Structure

New mod folder `droid-standby-mod/SaltysDroidStandby/`, laid out like the other Salty mods: `About/About.xml`, `build-and-install.sh`, `README.md`, `UpdateNotes.md`, `.gitattributes` (`*.sh` LF), StationeersLaunchPad config via `OnLoaded(ConfigFile)`, per-class `PatchSafely`, and an `Awake()` guard.

| Unit | Responsibility | Depends on |
|---|---|---|
| `StandbyState` | Per-droid level (Normal / PowerSave / Deep) plus the Time Skip flag; the single source of truth | — |
| `InputHandler` | Tap vs long press; sends requests to the server in multiplayer | StandbyState |
| `CognitionFloorPatch` | Holds and clears the stun floor (§4.1) | StandbyState |
| `DrainPatch` | Per-droid drain scaling (§4.2) | StandbyState |
| `JumpPatch` | Jump power reduction (§4.3) | StandbyState |
| `WakeConditions` | **Pure logic**: readings in, wake decision out; thresholds, hysteresis, storm edges, the indoor block. No Unity calls | — |
| `WorldReadings` | Reads the game state into a plain snapshot for `WakeConditions` | game |
| `SafetyNet` | Idle timer plus battery threshold; enters Deep Standby and pauses in single-player | StandbyState, WorldReadings |
| `WakePanel` | ImGui panel and status line | StandbyState, WakeConditions |
| `TimeSkip` | Phases 2 and 3: acceleration, drain freeze, vote, network messages | StandbyState |

## 13. Configuration (LaunchPad config UI)

| Key | Default |
|---|---|
| Standby key / long-press threshold | `Z` / 0.6 s |
| Power Save stun floor / drain factor / jump factor | 40 / 0.5 / speed-matched |
| Deep Standby stun floor / drain factor / jump factor | 85 / 0.25 / speed-matched |
| Wake check consecutive count | 3 |
| Light threshold / wind threshold | 30% / to be calibrated in-game |
| Battery wake: charged-to / dropped-to | 90% / 5% |
| Danger: pressure-change and temperature band | to be calibrated in-game |
| Default-ticked wake conditions | light, battery, danger |
| Safety net: battery threshold / idle time / pause in single-player | 10% / 60 s / on |
| Time Skip: speed / max | 5× / 10× |
| VerboseLogging | off |

"To be calibrated in-game" values get a starting default in the implementation plan from the decompiled ranges (e.g. the `WindStrength` noise range) and are confirmed during testing.

## 14. Error handling

- Each patch class is applied separately. A failure is logged and doesn't stop the others.
- If the cognition-floor or drain patch fails to apply, standby **disables itself** with a log line and a one-time on-screen message. The player can never be stuck stunned without a working exit.
- Every exit path (key, wake condition, death, life suspender, disconnect, load) clears the floor and any drain scaling.

## 15. Testing

- **Unit tests** (`tests/` project, as in the airlock mod) for `WakeConditions`: each threshold, consecutive-check hysteresis, storm start and end edges, the indoor block for storm checks, solar-storm detection, and the summed battery.
- **In-game checklist** added to `testing-to-do.md`:
  - each level's speed, visuals and drain
  - tap and long press
  - every wake condition
  - the safety net in single-player (pause) and multiplayer (no pause)
  - sleeper interaction
  - **with and without the Dual Battery mod**
  - jetpack unaffected
  - loading a save always starts at Normal
- Phase 2: the feasibility probe report, then Time Skip tests. Phase 3: multiplayer vote tests with a mixed droid and organic crew.

## 16. Phasing

1. **Phase 1, standby core:** §4–6, §9, §10 (no Time Skip), §11–15. Playable and useful on its own.
2. **Phase 2, Time Skip in single-player:** feasibility probe (§8) first, then §7 without the vote.
3. **Phase 3, multiplayer vote:** §7 vote and §10 networking for Time Skip.

Each phase gets its own implementation plan and its own in-game sign-off.

## 17. Out of scope

- Saving standby state across loads.
- Standby for organics (they only take part in the Time Skip vote, by sleeping).
- Discrete time skips, i.e. jumping ahead without simulating.
- Changing the jetpack, beds, sleepers or cryo tubes.
