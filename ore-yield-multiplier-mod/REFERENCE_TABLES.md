# Reference Tables — Vanilla Gas Baselines

Ground truth for verifying the multiplier during testing. **Values here
come from this mod's own `VerboseLogging` output against the actual
installed game version** (log's `before=` field is the pre-multiply,
vanilla amount), not from guessing or an external wiki — that's the most
reliable source available, since it's this exact game build's real data,
not a possibly-outdated community writeup.

**How to add a row:** with `VerboseLogging` on, smelt/melt the item once,
then check `BepInEx/LogOutput.log` for lines like:
```
IceMeltYieldPatch fired: ItemIronOre, gas=Pollutant, before=2mol, after=10mol (x5)
```
`before` is the vanilla baseline — add it below. If an item releases more
than one gas, it'll log one line per gas; list all of them.

To verify a test result: `observed_amount ÷ current_multiplier` should
equal the vanilla baseline below (or `vanilla × multiplier` should equal
what you observed).

## Ores (via `IceMeltYieldPatch`, `Ore.Smelt`)

| Ore | Gas | Vanilla (per unit) | Source |
|---|---|---|---|
| Iron (`ItemIronOre`) | CarbonDioxide | 0.5 mol | mod log |
| Iron (`ItemIronOre`) | Pollutant | 2 mol | mod log |
| Copper (`ItemCopperOre`) | Nitrogen | 0.5 mol | mod log |
| Copper (`ItemCopperOre`) | CarbonDioxide | 1 mol | mod log |
| Copper (`ItemCopperOre`) | Pollutant | 1 mol | mod log |
| Coal (`ItemCoalOre`) | CarbonDioxide | 10 mol | mod log |
| Coal (`ItemCoalOre`) | Pollutant | 3 mol | mod log |
| Gold (`ItemGoldOre`) | ? | ? | **needs re-test** — released *some* gas per an earlier pre-`VerboseLogging` test, exact composition not yet captured in this format |
| Silver (`ItemSilverOre`) | ? | ? | not yet tested |
| Uranium (`ItemUraniumOre`) | ? | ? | not yet tested |
| Nickel (`ItemNickelOre`) | ? | ? | not yet tested |
| Lead (`ItemLeadOre`) | ? | ? | not yet tested |
| Cobalt (`ItemCobaltOre`) | ? | ? | not yet tested |
| Silicon (`ItemSiliconOre`) | ? | ? | not yet tested |

## Ice (via `IceMeltYieldPatch` for regular `Ice`, or the
## `PureIceFreezeYieldPatch`/`PureIceMeltYieldPatch` pair for `PureIce`)

| Ice type | Gas | Vanilla (per unit) | Source |
|---|---|---|---|
| Volatile Ice | Methane | 20 mol | Stationeers Community Wiki — **not yet confirmed against this game build's log, treat as unconfirmed until re-tested** |
| Volatile Ice | Hydrogen | 2 mol | Stationeers Community Wiki — unconfirmed against this build |
| Oxite | Oxygen | 22.5 mol | Stationeers Community Wiki — unconfirmed against this build |
| Oxite | Nitrogen | 2.5 mol | Stationeers Community Wiki — unconfirmed against this build |
| Nitrice | ? | ? | not yet tested — do you have this ice type accessible? |
| PureIce (condensed atmosphere) | *(varies — see note)* | *(varies)* | n/a — `PureIce`'s composition is whatever gas you personally vented and froze, not a fixed table value; verify against the exact quantity you vented, logged by `PureIceFreezeYieldPatch`/`PureIceMeltYieldPatch` at the time |

## Notes

- Regular ore trace-gas release and ice-melt gas release go through the
  *same* patch (`IceMeltYieldPatch`, on `Ore.Smelt`) — see
  `UpdateNotes.md` for why. Both tables above are really one mechanism.
- `PureIce` doesn't have a fixed baseline like the other rows — its
  composition is set at the moment of freezing, from whatever you vented.
  Use the log's own before/after pair from that specific freeze/melt
  event to verify it, not a table lookup.
- If a gas you observe isn't a "yield" in the intuitive sense (e.g.
  Pollutant from smelting) — that's expected and confirmed intentional
  behavior, not a bug; see `UpdateNotes.md`'s note on `IceMeltYieldPatch`
  affecting regular-ore trace gas too.
