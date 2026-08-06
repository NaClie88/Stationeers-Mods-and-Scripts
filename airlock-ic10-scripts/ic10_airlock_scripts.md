# IC10 Airlock — Scripts

Each chip's code lives in its own file, ready to open and copy straight
into that chip's IC10 editor window in-game:

- **`watcher.ic10`** — always powered. Power Tier monitor, Button
  reader, Cycle-zone gate control.
- **`cycle.ic10`** — powered only when Watcher's zone gate is on.
  Doors, Vent, chamber Gas Sensor, the full evacuate/pressurize state
  machine.
- **`gas_sensor.ic10`** — optional. Propped-Open match/mismatch
  monitor for the two exterior/interior-facing Gas Sensors.

For why any of this looks the way it does — design rationale,
corrections, dry-run verification — see `ic10_airlock_code_notes.md`.
For hardware, wiring, and first-time setup, see
`ic10_airlock_setup_guide.md`.
