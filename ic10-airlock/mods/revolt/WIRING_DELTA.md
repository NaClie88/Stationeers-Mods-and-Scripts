# Re-Volt Wiring Delta — Data Diode Variant

Delta only, against `ic10-airlock/ic10_airlock_setup_guide.md`. Read
that doc first — this assumes you already have it in front of you and
only calls out what changes. Covers the Data Diode swap (`watcher.ic10`
+ `cycle.ic10` in this folder). **Unverified in-game** — see
`PARTS_DELTA.md`'s "Simplification candidate" section for the
assumption this whole delta rests on before building against it.

## 1. Hardware list delta

- **Remove: Logic Transmitter ×2.** Both the Active and Passive units
  from the vanilla list are gone.
- **Add: Data Diode ×1.** One structure, wired with a physical data
  cable — not wireless like the Transmitters were.
- **Optional, recommended: Cable Tray**, enough to run one data-cable
  segment alongside the zone's existing power feed through the
  "spillover" grid volume (vanilla setup guide, step 2). See
  `PARTS_DELTA.md`'s Cable Tray section for why this keeps the
  footprint from growing when you add the diode's cable run.
- Everything else in the vanilla hardware list is unchanged.

## 2. Wiring delta — Watcher

Vanilla step 3's `d3` row (`Transmitter`) is deleted, nothing replaces
it:

- **`d3` is now spare.** No alias, no device wired here.
- `d0` (`Battery`), `d1` (`LED`), `d2` (`Gate`) — unchanged from
  vanilla.
- Load `watcher.ic10` **from this folder**, not the vanilla one.

## 3. Wiring delta — Cycle

Vanilla step 4's `d4` row (`Receiver`) is deleted, nothing replaces it:

- **`d4` is now spare**, alongside `d0` which was already spare in
  vanilla. Two of six pins unused on this housing now.
- `d1` (`DoorExt`), `d2` (`DoorInt`), `d3` (`Vent`), `d5`
  (`ChamberSensor`) — unchanged from vanilla.
- Load `cycle.ic10` **from this folder**, not the vanilla one.

## 4. New: Data Diode placement and wiring

- **Input side → Watcher's network.** Run a data cable from Watcher's
  IC Housing (or any device already on its always-on network — e.g.
  the dedicated Power Controller) to the Data Diode's input port.
- **Output side → Cycle's network.** Run a second data cable from the
  diode's output port to Cycle's IC Housing (or any device on its
  gated network).
- This physically crosses the same always-on/gated boundary the
  zone-gate power feed already crosses — route it through the same
  Cable Tray run if you built one (see hardware delta above).
- **Unconfirmed: does the diode need its own power feed?** Its own
  commit description mentions "a tiny parasitic draw" to move data
  signals, which implies it draws *some* power, but not from where.
  Check its Stationpedia entry or a Logic Reader in-game before
  assuming it works unpowered — if it turns out to need a feed of its
  own, the always-on side (same circuit as Watcher and the dedicated
  Power Controller) is the correct one to draw from, matching the
  "Watcher is never power-gated" invariant from the vanilla design.

## 5. Removed: Transmitter pairing (was vanilla step 6, second half)

Nothing to do here — that's the point. The vanilla setup guide's
"Naming the Buttons and pairing the two Logic Transmitters" step is
now just "Naming the Buttons." The whole manual dial-tuning procedure,
and its own troubleshooting entry ("Passive Transmitter's dial is
actually tuned"), no longer applies. **Button naming is unchanged** —
still name them exactly `AirlockBtnE`/`AirlockBtnI`/`AirlockBtnC` via
Labeller, per vanilla step 6's first half. Both `watcher.ic10` and
`cycle.ic10` in this folder read them by those same names.

## 6. New: the LED needs to stay unique on the bridged network

Cycle now reads Tier by batch-reading the LED's `Color` field by type
hash (`lb r9 LEDHash Color 0` — see `cycle.ic10`), not by name. This
works without a Labeller name **only if this LED is the sole
`StructureDiode`-type device reachable across the diode** — a batch
read across multiple matching devices averages them, which would
corrupt the Tier reconstruction. The vanilla design already keeps this
Power Controller's network dedicated and isolated (requirements doc,
"why this airlock has its own isolated Power Controller"), so this
should hold automatically — just don't add a second LED, or any other
`StructureDiode`, to this same isolated network for an unrelated
purpose.

## 7. Constants to check (in addition to vanilla step 7's list)

- **`LEDHash`** (`HASH("StructureDiode")`) — Cycle, brand new, **fully
  unconfirmed.** Verify the actual prefab name against Stationpedia
  before trusting it. See `PARTS_DELTA.md` for what happens if it's
  wrong (Tier silently defaults to Critical — check this first, not
  last).
- `BtnHash`, `ColorGreen`/`ColorYellow`/`ColorRed`, `WakeHold`,
  `TargetInt`/`TargetExt`, door dwell, `PropFlagHash` — all unchanged
  from vanilla, same caveats apply, see vanilla step 7.
- No `On`-field or `Mode` constant needed for the diode itself, unless
  in-game verification (section 4 above) shows it needs to be
  logic-enabled to function.

## 8. First-power-on order delta

Follow vanilla step 8's order (Watcher alone, then Cycle, then Gas
Sensor chip if built), with one addition **before** step 8.2's button
tests: confirm the Data Diode is actually bridging visibility at all —
with Watcher powered and Cycle powered through the zone gate, check
whether Cycle can see Watcher's Buttons and LED (a Logic Reader aimed
at Cycle's network should show them, if the bridge is real). If it
can't, stop here — this whole variant doesn't work as designed, and
the vanilla `ic10-airlock/` scripts + two Logic Transmitters are the
correct build even under Re-Volt.

## Not covered here

- **Load Center** — deliberately not recommended for this build, see
  `PARTS_DELTA.md`'s "Load Center reconsidered" section for why.
- **Circuit Breaker** — purely additive, doesn't change any wiring
  above; see `PARTS_DELTA.md`'s "Circuit Breaker" section for
  placement.
- **Modular Battery** — not yet shipped in the mod as of this writing;
  nothing to build against yet, see `PARTS_DELTA.md`.
