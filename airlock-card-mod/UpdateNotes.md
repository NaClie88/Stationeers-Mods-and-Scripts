# Salty's Advanced Airlock: running dev log

A running record of what was done and **why**, written as the work happens. Newest entries go at the bottom.

This log starts on 2026-10-05, when the mod was brought in line with the repo's other Salty mods. For the history before that, see the existing docs:

- `README.md`: project overview, build paths, milestone history
- `GAP_ANALYSIS.md`: power architecture and its design-history timeline
- `PATCH_PLAN.md`: Harmony patch targets, including the two wrong-target guesses caught in-game
- `STATE_TABLE.md`, `state-map.json`, `door-states.json`: the failsafe tier and vanilla door/vent state machines
- `GETTING_STARTED.md`, `NATIVE_XML_CHECKLIST.md`: setup and the native-XML path
- git history on the `airlock-mod-card` branch

## 2026-10-05: Pending work committed

Committed the 2026-08-08 in-game fixes that were sitting uncommitted (`7f081ea`). All 43 `FailsafeController.Tests` pass.
- **Critical-tier trap:** `ForceEvacuate()` + `UnlockDoors()` ran every tick, re-locking the doors before anyone could get through. They now run once per Critical entry (`criticalHandledThisEntry`).
- **Stale-vent gas mixing:** the vents were left in evacuate mode after a Critical stay. The new `IAirlockHost.StopForcedEvacuation()` turns them off.
- Temporary diagnostic logs in `LockDoors` / `UnlockDoors` / `CloseDoor`, plus the two state-map JSONs.

## 2026-10-05: Brought in line with the Salty mod convention

The convention is set by the other mods in this repo (Droid Dual Battery, Fabricator Overflow, Yield Multiplier): a LaunchPad mod folder with `About/About.xml`, a `build-and-install.sh` that deploys to `Documents/My Games/Stationeers/mods/<Mod>/` and hash-verifies the copy, an `UpdateNotes.md` dev log, and LF-pinned shell scripts.

**Changes:**
- **`About/About.xml`** added. Name "Salty's Advanced Airlock", ModID = the existing plugin GUID.
- **`build-and-install.sh`** now installs to `mods/AirlockCardMod/` (DLL + About.xml) instead of `BepInEx/plugins/AirlockCardMod/`. It **removes the old plugins copy** after a verified install, because LaunchPad would load the mods-folder copy *and* BepInEx would load the plugins copy, applying every patch twice.
- **`Awake()` guard (`_patched`).** LaunchPad calls a mod's `Awake()` more than once (config-UI load + session load; seen in every other Salty mod's log). The old unguarded `harmony.PatchAll()` would have stacked every postfix twice, so each failsafe tick would run twice. The guard is harmless under plain BepInEx.
- **`.gitattributes`**: `*.sh text eol=lf`. With `core.autocrlf` on this machine, Git would otherwise check the script out with CRLF, which Bash refuses.
- `README.md` and `GETTING_STARTED.md` updated for the new install location.

**Deliberately not changed:**
- **No LaunchPadBooster reference.** The README's design stance is "StationeersLaunchPad is recommended, not required": a player can drop the DLL into `BepInEx/plugins/`. The other mods use LaunchPadBooster only for `OnLoaded(ConfigFile)` config and `Networking.Required`, and the airlock uses neither. Referencing it would make LaunchPad a hard dependency for no benefit. LaunchPad already loads plain BepInEx-entry DLLs from mod folders.
- **Names stay as they are** (folder `airlock-card-mod`, project, namespace and class `AirlockCardMod`, GUID `com.username.AirlockCardMod`, branch `airlock-mod-card`). The project owner chose to keep the full rename sweep for just before the 1.0 publish. Changing the GUID would also reset the BepInEx config identity.
- The existing plain `Debug.Log("[Salty's Advanced Airlock]: …")` logging format is unchanged, so earlier docs' log-line references still match.
