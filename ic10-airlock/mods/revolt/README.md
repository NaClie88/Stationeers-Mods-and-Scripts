# Re-Volt Variant — Fail-Safe Airlock

This folder is an **overlay**, not a replacement. The vanilla build in
`ic10-airlock/` (requirements doc, setup guide, code notes, and the three
`.ic10` scripts) is the default supported configuration and is never
edited to accommodate a mod — someone without Re-Volt installed should be
able to ignore this folder entirely and build the base airlock exactly as
documented there.

This directory exists so a player **with** Re-Volt installed
(`database/mods.json` → `revolt`) has a single place to check for parts or
wiring that behave differently, without that mod-specific detail leaking
into the vanilla docs. Same separation principle as `recipes.json`'s
`source: "mod:revolt"` tagging, applied to the airlock build instead of
the resource database.

## Files

- **`PARTS_DELTA.md`** — every vanilla part this build uses, checked
  against Re-Volt's feature set: confirmed-unaffected, optional upgrade,
  or needs in-game verification before you trust it. Start here.
- **Forked scripts** (`watcher.ic10`, `cycle.ic10`, `gas_sensor.ic10`) —
  not present yet. Added here only if in-game verification under
  Re-Volt shows the vanilla script actually breaks (e.g. a LogicType the
  script reads doesn't exist on whatever device ends up wired to that
  pin) — see the status table in `PARTS_DELTA.md`. Until then, the
  vanilla scripts in `ic10-airlock/*.ic10` are used unmodified; nothing
  is confirmed broken yet, just unconfirmed.

## Status

**Unverified.** Re-Volt overhauls power distribution (recursive
networks, per-battery charge/discharge-rate limits, delayed cable
burn-out, Load Centers, a Modular Battery split into
Charger/Battery Bank/Inverter — full list in `database/mods.json`), but
nothing in this project has been checked against it in-game yet. Treat
every line in `PARTS_DELTA.md` marked "needs verification" as a
checklist, not a confirmed fact — same standard the vanilla build holds
itself to for its own unconfirmed values (see its setup guide, section
7).

## How to add the next mod's variant

Same pattern, new sibling folder: `ic10-airlock/mods/<mod-key>/`, with
its own `PARTS_DELTA.md` and its own forked scripts only where actually
needed. `<mod-key>` should match the key used in `database/mods.json` so
the two stay cross-referenceable.
