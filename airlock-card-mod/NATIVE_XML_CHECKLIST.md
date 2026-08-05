# Milestone 0 Checklist — Native XML Mod Investigation

No downloads, no installs — everything here uses files already on the
machine you play Stationeers on. Point of this pass: find out whether
Stationeers' built-in XML mod format can define a genuinely *new* item,
or only reconfigure an existing one. That answer decides how much of
this whole mod can skip C# entirely.

## 1. Find the real data files

- Go to `<Stationeers install>/rocketstation_Data/StreamingAssets/Data/`
  (find the install folder the same way as `GETTING_STARTED.md` step
  1: Steam → Library → Stationeers → right-click → Manage → Browse
  Local Files).
- There should be roughly 27 `.xml` files here — recipes, traders,
  start conditions, and similar. Open a few in any text editor (even
  Notepad) and look for one that mentions Circuitboards or
  `ElectronicsPrinterRecipes` — recipe data for player-craftable items
  is confirmed to live in files structured like this project's sourced
  example (see `README.md`'s "Sources" section for the exact snippet).

## 2. Find the Advanced Airlock Circuitboard's entry specifically

- Search across those files (most text editors have a
  find-in-files/search-folder feature) for `Airlock` — you're looking
  for whatever `PrefabName` corresponds to `Circuitboard (Advanced
  Airlock)`. Write down the exact `PrefabName` string you find — this
  is a real, confirmed fact this project doesn't have yet.
- **The key question:** does that entry look like it's *defining* the
  item (anything resembling a model reference, icon path, or full item
  description), or does it look like *only* a recipe/cost table for an
  item defined somewhere else entirely (a compiled prefab, likely
  outside any XML file)? If every field you can find is just
  ingredient costs and craft time, that's a strong signal item
  *definitions* live outside XML reach, and only their *costs* are
  moddable this way.

## 3. Check the two example mods that ship with the game

- Still in `StreamingAssets/`, find `ExampleMod.zip` and
  `AttributesExampleMod`. Extract/open them and look at their
  `About/About.xml` — this is the real, current schema, better than
  anything paraphrased in this project's docs. Compare its structure
  to what's described in `README.md`'s "Sources" section; note any
  differences.
- If either example mod happens to add a genuinely new item (not just
  modify an existing one's numbers), that directly answers the open
  question — check its `GameData/` folder for how it does it.

## 4. Report back

Whatever you find — the real `PrefabName`, whether new items look
possible via XML alone, anything the example mods reveal — is what
turns Milestone 0/1.5 from "unconfirmed, needs your own check" into a
real fact this project can build on. Doesn't matter if the answer is
"no, XML can't create new items" — that's just as useful to know, since
it settles the question and we move straight to the BepInEx path with
no more guessing.

## What this doesn't answer

Even a fully positive answer here (XML can define new items) only
solves the *card's existence* — its name, recipe, and which model it
uses. The actual fail-safe *behavior* (Tier monitoring, staged
power-failure response, Propped-Open) is logic, not data, and needs
real code no matter what this milestone finds — see Milestone 2+ in
`README.md`.
