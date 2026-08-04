# Resource Database — Schema Notes

Three files, one job each. None of them calculate anything yet — that happens
later, at guide-generation time, once a difficulty/starting-condition/world
combination is actually selected. Right now this is just the data model.

## Files

**recipes.json** — one entry per craftable item/kit. Ingredients, the machine
that builds it, tier required, power draw, craft time. Every field can be
`null` if not yet sourced — a `null` amount means "known to need this
material, exact quantity not verified," not "needs none." `verified: true`
only once every ingredient has a real number and a citation in `notes`.

**project_requirements.json** — join table. Maps a project ID (A1, B2, C1,
etc. — same scheme as the main guide) to the recipe items it needs. This is
what a future generator walks: for project X, look up its items here, then
pull cost/power from recipes.json, sum it up.

**mods.json** — registry of known mods (currently just Re-Volt, confirmed
real via Steam Workshop and GitHub — an electrical overhaul mod). A mod is
`planned` until its items actually get added to recipes.json tagged
`source: "mod:<key>"`. Vanilla recipes always have `source: "vanilla"`.

Each mod entry has two things beyond the basics:
- **`new_devices`** — concrete new items/mechanics the mod adds. These are
  recipe.json candidates once their build costs are sourced.
- **`enabled_goals`** — the more important field. A mod isn't just item
  substitutions for existing projects; it can unlock entirely new
  project-level goals that only make sense once its devices exist. Re-Volt's
  resettable Circuit Breakers and Load Centers, for example, make a real
  centralized "Breaker Room" project possible — vanilla has no resettable
  breaker and no way to group devices under one control point without a
  full IC10 setup per group, so this isn't a reskin of an existing project,
  it's new. Each `enabled_goals` entry includes a `candidate_placement` —
  which category it would slot into and roughly what number — so promoting
  it into a real lettered project later is just "pick the next available
  number in that category and link it in project_requirements.json," the
  same process as adding any other project.

## How a future generator would use this

1. Take the selected difficulty/starting-condition/world/mod-config as input.
2. For each project in the guide, look it up in project_requirements.json.
3. Pull each linked item from recipes.json, filtering out any `source:
   "mod:X"` entries where mod X isn't active in the requested config.
4. Sum ingredient amounts and power draws across the project (or across the
   whole guide, for a shopping-list view).
5. Anything with `verified: false` or a `null` amount should be flagged in
   the output rather than silently treated as zero — the generator should
   say "cost not yet known" rather than guess.
6. If a mod is active, also surface its `enabled_goals` as suggested
   additional projects for that guide — not required, but worth showing
   since they're goals the player couldn't pursue without that mod.

## Extending later

- **New vanilla item:** add to recipes.json with `source: "vanilla"`, link
  it from whichever project(s) need it in project_requirements.json.
- **New mod:** add an entry to mods.json first (status `planned`), with
  `new_devices` for concrete items and `enabled_goals` for any new
  project-level goals it unlocks (even a rough guess at `candidate_placement`
  is useful). As its items get sourced, add them to recipes.json tagged
  `source: "mod:<key>"` and update `status` to `active` once there's enough
  coverage to matter. The join table doesn't need to change structurally —
  a mod item slots into project_requirements.json the same way a vanilla
  one does.
- **Filling in gaps:** most entries right now are seeds with partial data
  (see `_unlinked_projects_todo` in project_requirements.json for what
  hasn't been touched at all). Populate opportunistically — a half-filled
  recipes.json is still useful, since the generator is expected to report
  gaps rather than assume zero-cost.
