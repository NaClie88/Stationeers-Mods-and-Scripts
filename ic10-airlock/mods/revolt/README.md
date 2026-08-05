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
- **`WIRING_DELTA.md`** — the practical build guide for the Data Diode
  variant below: hardware list delta, pin-table delta, and what to
  physically do differently from the vanilla setup guide. Read
  `PARTS_DELTA.md` first for the *why*; this is the *what to build*.
- **`watcher.ic10`, `cycle.ic10`** — a **hypothesis fork**, not a
  confirmed-working replacement. Drops the vanilla Logic Transmitter
  Active/Passive relay pair in favor of a Data Diode, on the assumption
  that the diode really does bridge device visibility across the two
  networks (Cycle reads Watcher's Buttons and LED directly instead of
  receiving a relayed value) — see PARTS_DELTA.md's "Simplification
  candidate" section for the reasoning and what's still unconfirmed.
  **Do not build against these until that assumption is checked
  in-game** — if it's wrong, use the vanilla scripts + two Logic
  Transmitters instead, this pair is not a safe fallback.
  Note: dropping the relay didn't shrink the code much (Watcher: 79
  lines vs. vanilla's 88; Cycle: actually grew to 112 vs. vanilla's 104,
  since reconstructing Tier from the LED's color costs more lines than
  the old div/mod unpack did) — the real payoff is fewer physical parts
  and no manual pairing step, not simpler code.
- **`gas_sensor.ic10`** — not forked. The vanilla version in
  `ic10-airlock/` never touches the Transmitter/Receiver pair, so it's
  unaffected by any of this and used as-is.

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
