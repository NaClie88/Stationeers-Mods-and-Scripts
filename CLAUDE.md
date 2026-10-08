# CLAUDE.md: repo memory for Stationeers-Mods-and-Scripts

Durable, hard-won rules for working in this repo. Read before touching any mod. The details live in each mod's `UpdateNotes.md`.

## Layout: one branch per mod, all cut from `main`

| Mod | Branch | Folder |
|---|---|---|
| Salty's Advanced Airlock | `airlock-mod-card` | `airlock-card-mod/` |
| Salty's Droid Dual Battery | `droid-dual-battery-mod` | `droid-dual-battery-mod/` |
| Salty's Droid Standby | `droid-standby-mod` | `droid-standby-mod/` |
| Salty's Fabricator Overflow | `fabricator-overflow-mod` | `fabricator-overflow-mod/` |
| Salty's Yield Multiplier | `yield-multiplier-mod` | `ore-yield-multiplier-mod/` |

`main` holds the IC10 scripts, the logic-network reference and the shared docs.

Every mod folder follows the same convention:
- `About/About.xml`
- `build-and-install.sh`: builds, deploys to `Documents/My Games/Stationeers/mods/<Mod>/` and hash-verifies the copy
- `README.md`, including sources and credits
- `UpdateNotes.md`: a running dev log of what was done and **why**, written as you go
- `.gitattributes` with `*.sh text eol=lf`

## Installing and testing

- **Stationeers must be closed** to install: the DLL is locked while the game runs. Always use `build-and-install.sh`; a bare MSBuild build doesn't deploy anything.
- **LaunchPad 1.0 profiles gate which mods load.** The active profile is `Documents/My Games/Stationeers/profiles/my mods.xml` ("My Mods"). At startup LaunchPad applies it and rewrites `modconfig.xml`, so any mod **not listed in the profile is forced disabled**. It won't show in LaunchPad's list (press **P** during startup, then **M**), and an edit to `modconfig.xml` alone gets reverted.
  - A new mod folder, or an existing mod moved to a new folder, therefore does nothing until it's added to the profile.
  - Symptom: `Player.log` shows `new mod added at …\mods\<Mod>` but no `Loading Assembly` line for it.
  - Fix, with the game closed and a backup made: add a line in alphabetical order by name, keeping the file's CRLF line endings:
    `<Mod Name="<About Name>" Source="Local" DirectoryPath="C:\Users\Joshua\Documents\My Games\Stationeers\mods\<Folder>" WorkshopHandle="0" ModID="<About ModID>" />`
    Also set the mod's `modconfig.xml` entry to `Enabled="true"`.
  - The game's own in-game "Mods" menu only lists native XML mods. It never shows LaunchPad mods.
- Logs: `Stationeers/BepInEx/LogOutput.log` and `%USERPROFILE%/AppData/LocalLow/Rocketwerkz/rocketstation/Player.log` (the full Unity log, including exceptions).
- Verify every new Harmony patch in-game, using a log line per patch. Wrong targets have compiled fine and then silently never fired.

## Game-code facts that have bitten us

- **LaunchPad calls `Awake()` more than once.** Guard Harmony patching with a static `_patched` flag, or every postfix stacks.
- **`Human.OnLifeTick` and `Brain.OnLifeTick` run on a thread-pool thread** (`AtmosphericsManager.LifeTicksTick`). Never touch UnityEngine.Object APIs there (`.name`, transforms, …). Use `ReferenceEquals` and catch everything: an escaping exception makes `GameTick` skip electricity, logic and atmosphere ticks world-wide.
- **`BatteryCell.PowerStored` is not networked.** Clients only get `CurrentPowerPercentage` (0–100).
- **Freeing the cursor:** use `MouseModeController.AddModal(IModal{UnlockCursor=true})` / `RemoveModal` + `Reset()`, as `ConsoleWindow` does. Writing `Cursor.lockState` directly is re-locked every frame.
- **Saves and network messages address slots by index.** Only ever append slots, and never reorder.
- Decompiled game source is proprietary: **never commit it**. Keep it in a scratchpad; only findings go into docs.

## Git on this machine

- `core.autocrlf` is on. Shell scripts must stay LF, hence the `.gitattributes` rule in each mod folder.
- Worktrees under the long scratchpad path hit Windows MAX_PATH on deep IC10 files: use `git -c core.longpaths=true …`. Note that `git add` fails silently if its output is piped away.
- Git Bash `sed -i` silently converts CRLF files to LF, and Python inside a bash heredoc mangles backslashes. For Windows-path edits, write the script to a `.py` file.
- Commit messages end with a `Co-Authored-By:` line for the assistant.
