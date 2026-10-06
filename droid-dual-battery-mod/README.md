# Salty's Droid Dual Battery

A Stationeers mod that gives **H.E.M. Droids a second battery slot**, so you can hot-swap batteries without the droid powering down.

## What it does

- **Second battery slot (droids only).** A new HUD button, **β**, sits directly under the droid's normal battery button, which is now labelled **α**. Humans and Zrilians are unchanged.
- **Drain order: α, then β.** The droid runs on battery α until it's empty, then switches to battery β automatically. If both are flat, vanilla's fallback still applies: a charged battery held in either hand.
- **Hot-swap.** While the droid runs on β, pull the empty α battery and put in a fresh one, with no power loss.
- **Droid Sleeper charges both batteries.** Both slots charge every power tick at the sleeper's normal rate, and the extra power is drawn from the sleeper's cable network, the same way vanilla already charges a suit battery.
- **Localized labels.** The labels use the game's own translated name for the droid battery slot plus **α / β** (English: "Battery α" / "Battery β"). They update when you change language and keep the game's per-language font.

## Requirements and install

- [StationeersLaunchPad](https://github.com/StationeersLaunchPad/StationeersLaunchPad) **v1.0.0 or later** (BepInEx + LaunchPadBooster).
- Put the mod folder (`About/About.xml` + `SaltysDroidDualBattery.dll`) in `Documents/My Games/Stationeers/mods/`. Developers: `build-and-install.sh` builds and deploys in one step, and refuses to run while the game is open, because the DLL is locked.

### Multiplayer

**Required on the server *and* every client.** Saves and network messages refer to an item's slot by number, so everyone must agree that droids have the extra slot. The mod sets `Networking.Required`, so LaunchPad refuses a join when the server and client don't match. *Multiplayer is not yet tested in-game.*

### Saves

- **Existing saves work.** No new save is needed: the slot is added to existing droids as the save loads.
- **Removing the mod:** take the battery out of slot β first and save. Without the mod the slot doesn't exist, and a battery saved in it won't be put back. The game logs `Slot N does not exist on thing …` and leaves it out of your inventory.

### Config

LaunchPad config → **Debug → VerboseLogging** (off by default) logs battery switches (α → β) and setup detail to `BepInEx/LogOutput.log`.

## How it works

The H.E.M. Droid's battery slot is really vanilla's **Uniform** slot, retyped to `Battery` when the game learns the character is a droid (`Human.SetSpeciesSpecificSlots`). That's also why it sits on the #5 key and why its HUD label said "Uniform". Every system that uses the droid's battery reads one property, `Human.RobotBattery`. The mod hooks those two points plus the HUD:

| File | Harmony patch | Purpose |
|---|---|---|
| `Patches/ExtraBatterySlotPatch.cs` | postfix `Human.SetSpeciesSpecificSlots` | Droids only: append a `Battery` slot to the **end** of `Slots`. Idempotent. Sets `Parent`, the `Slot{N}` interaction action and the icon, as `Thing.Awake` would. |
| `Patches/RobotBatteryPatch.cs` | postfix `Human.RobotBattery` getter | If α is missing or empty and β has charge, return β. Drain, Brain stun, HUD power readout and chargers all follow automatically. |
| `Patches/HudSlotPatch.cs` | postfix `InventoryManager.Initialize(Entity)` | Clone the #5 HUD button, link it to slot β, hide its "5" hotkey hint. Kept out of `InventoryManager.DisplaySlots` on purpose (see below). |
| `Patches/DroidSleeperPatch.cs` | prefix + postfix `DroidSleeper.ChargeRobot` | Charge both slot batteries each tick, skipping the one vanilla just charged. |
| `Patches/BatteryLabels.cs` | postfix `LocalizedText.Refresh` | Localized α/β labels; hands #5 back to vanilla for non-droids. |

Design notes:

- **Why adding the slot "late" is safe.** Species is unknown at `Human.Awake`, so the slot is added when species is applied. Both load paths apply species synchronously, straight after creating the character and before any of its saved items look for their slot:
  - save load: `XmlSaveLoad.Load` → `Create` → `DeserializeSave`
  - client join: `NetworkClient.ProcessThings` → `Create` → `DeserializeOnJoin`
- **Why it's appended last.** Items store their slot as an index (`ParentSlotId`), so existing indices must never move, and the slot has to come after vanilla's slots and any slots other mods add.
- **Why the HUD clone isn't in `DisplaySlots`.** Vanilla's per-frame hotkey loop reads `displaySlot.Slot.Occupant` for every entry, so an unlinked entry (a human after a droid) would throw every frame.
- **Species never changes on a live body in vanilla.** Every species assignment is on a freshly created body. A droid that dies keeps both batteries on its corpse for looting.

The full development log, including dead ends, test results and the reasoning behind each decision, is in [`UpdateNotes.md`](UpdateNotes.md).

## Known limitations

- No hotkey for slot β yet: click or drag batteries into it.
- Multiplayer is untested in-game.

## Sources and credits

**Game code.** All game behaviour was established by decompiling Stationeers' `Assembly-CSharp.dll` with [ILSpy](https://github.com/icsharpcode/ILSpy) (`ilspycmd`). Key types: `Human`, `Slot`, `Thing`, `DynamicThing`, `InventoryManager`, `SlotDisplay`, `SlotDisplayButton`, `HotkeyDisplay`, `LocalizedText`, `Localization`, `DroidSleeper`, `XmlSaveLoad`, `NetworkClient`.

**Frameworks.**
- [BepInEx](https://github.com/BepInEx/BepInEx): plugin loader.
- [Harmony](https://github.com/pardeike/Harmony): runtime method patching. Uses `HarmonyPatch`, `AccessTools.FieldRefAccess` and `__state`.
- [StationeersLaunchPad / LaunchPadBooster](https://github.com/StationeersLaunchPad/LaunchPadBooster): mod loading, the `OnLoaded(ConfigFile)` entry point, and `Mod.Networking.Required`.

**Ideas and prior art.**
- [Suit Enhancement Suite](https://github.com/altmank/Stationeers-SuitEnhancementSuite) by altmank. Reading its source confirmed this mod's approach to adding slots: append a `new Slot { … }` to an existing item's `Slots` list; keep it idempotent with a `StringKey` check; always add **after** the vanilla and other-mod slots so saved slot indices never shift. Its README's warning about emptying added slots before removing the mod is echoed above. No code was copied.
- [Stationeers Community Wiki: HEM Droid](https://stationeers-wiki.com/HEM_Droid): the droid's Uniform slot is replaced by a battery slot, and a droid can run off a battery held in hand.
- [Steam discussion: "H.E.M. Droids and Batteries"](https://steamcommunity.com/app/544550/discussions/0/1754653387142881010/): players asking for "Main and Reserve" droid batteries.
- Project skeleton (csproj, plugin bootstrap, `build-and-install.sh`) is reused from this repo's own *Salty's Fabricator Overflow* mod.

**Author:** NaClie88 (Salty).
