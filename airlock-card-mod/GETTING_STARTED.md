# Getting Started — Milestone 1 Checklist

Goal: get the stock `ExamplePatchMod` template building and showing a
log line in-game. Nothing airlock-related yet — this only proves the
pipeline works. Check each item; most of these you may already have.

## 1. Confirm Stationeers itself

- Open Steam → Library → find Stationeers → right-click → Manage →
  Browse Local Files. Note this folder's full path — you'll need it
  in step 5. (Typically something like
  `C:\Program Files (x86)\Steam\steamapps\common\Stationeers`, but
  Steam library locations vary — use whatever your Browse Local Files
  actually opens.)

## 2. Install BepInEx

- Download **BepInEx 5.4.21** (the x64 pack) for Stationeers. The
  Stationeers Community Wiki's modding pages and the StationeersLaunchPad
  docs both link the correct release — grab it from there rather than
  a generic BepInEx download, since version matters.
- Extract the zip's contents directly into the Stationeers folder from
  step 1 (so `winhttp.dll`, `doorstop_config.ini`, and a `BepInEx/`
  folder end up alongside the game's own files).
- **Launch the game once, then close it.** This first launch is what
  generates `BepInEx/core/BepInEx.dll` and `BepInEx/core/0Harmony.dll`
  — you need both to exist before step 5.
- Verify: `<Stationeers folder>/BepInEx/core/BepInEx.dll` and
  `.../0Harmony.dll` both exist, and
  `<Stationeers folder>/rocketstation_Data/Managed/Assembly-CSharp.dll`
  exists (this one ships with the game itself, should already be
  there regardless of BepInEx).

## 3. Install Visual Studio

- Visual Studio Community (free) is enough. During install, check the
  **".NET desktop development"** workload — the template targets
  .NET Framework 4.7.2, and that workload is what provides it.

## 4. Get the template

- Download the ZIP or clone `StationeersModding/ExamplePatchMod`.
- Rename the folder to something like `AirlockCardMod`.
- Open the `.sln` in Visual Studio.
- In Solution Explorer, Ctrl+H (Find and Replace) — replace every
  occurrence of `ExamplePatchMod` with your project's name (e.g.
  `AirlockCardMod`). This renames the namespace, class, and plugin GUID
  string together.
- Project menu → `<YourName>` Properties → update both **Assembly
  Name** and **Default Namespace** from the placeholder to your actual
  name, if Find/Replace didn't already catch them.

## 5. Fix the DLL references

The template's `.csproj` ships with the original author's own path
hardcoded (`D:\SteamLibrary\steamapps\common\Stationeers\...`) —
**this will not match your machine.** In Visual Studio:

- Solution Explorer → References (or Dependencies) → for each of
  `0Harmony`, `Assembly-CSharp`, `BepInEx`, `UnityEngine`,
  `UnityEngine.CoreModule` → Properties → update the **Path** to point
  at your own Stationeers folder from step 1:
  - `0Harmony` → `<your folder>\BepInEx\core\0Harmony.dll`
  - `BepInEx` → `<your folder>\BepInEx\core\BepInEx.dll`
  - `Assembly-CSharp`, `UnityEngine`, `UnityEngine.CoreModule` →
    `<your folder>\rocketstation_Data\Managed\<name>.dll`

## 6. Build and install

- Build (Debug is fine). This produces a DLL in `bin\Debug\`.
- Create a folder for it under BepInEx's plugins directory:
  `<Stationeers folder>/BepInEx/plugins/AirlockCardMod/`
- Copy the built DLL into that folder.

## 7. Launch and verify

- Start Stationeers. Watch for a BepInEx console window (a separate
  window that opens alongside the game, showing log output) — if it's
  not appearing, check `doorstop_config.ini` for an `enabled=true`
  console setting, or check `BepInEx/LogOutput.log` after the game
  closes instead.
- Look for a line like `[AirlockCardMod]: Patch succeeded`. That's the
  whole test — if it's there, the entire pipeline works: Visual Studio
  built it correctly, the references were right, BepInEx found and
  loaded it, and Harmony initialized without error.
- If instead you see `Patch Failed` with an exception, or don't see
  the plugin at all in the log, stop and report exactly what you see
  before moving on — don't guess past a failed step.

## What's next

Once this works: Milestone 1.5 in `README.md` — open
`Assembly-CSharp.dll` in a decompiler (dnSpy or ILSpy, both free) and
find the classes behind the Advanced Airlock Circuitboard and the
Console device. That's the point where this stops being "follow a
generic checklist" and starts being specific to this mod.
