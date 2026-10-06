**DO NOT SUBMIT YET.** Deeper investigation found that
`AtmosphereHelper.RemoveStackWorthOfFrozenGas` already caps each freeze
chunk at exactly 2500 mol per gas type -- precisely the boundary that
would trigger this, meaning the scenario below is likely already prevented
by design, not an actual reachable bug. See TEST_PLAN.md item 15d for the
in-game vanilla (mods disabled, creative mode) repro test. Only submit
this if that test actually reproduces mass loss -- and if it does, rewrite
the repro steps to target the exact float-precision boundary that test
finds, not "vent a lot of gas," since that alone won't trigger it.

---

Title: Large single freeze event destroys excess gas mass (PureIce stack silently clamped at 50, not the recorded per-piece value)

## What happened
When enough of a single gas type freezes into PureIce in one event to
require more than 50 pieces to represent (roughly 2500+ mol of one gas
type solidifying at once), the resulting ice stack silently caps at 50
pieces. The per-piece gas quantity was already calculated assuming the
*unclamped* piece count, so the excess mass is destroyed — not left in
the environment, not folded into the 50 pieces that do form.

## What I expected
Either the full frozen quantity is represented across the stack (even if
that means exceeding 50 pieces, or distributing the excess some other
way), or the freeze event itself is capped/limited so this situation can't
arise — but the excess should not simply disappear.

## Steps to reproduce
1. Cause a large amount (2500+ mol) of a single gas type to freeze in one
   event — e.g. vent a large tank of one gas into a very cold vacuum area
   so it all solidifies at once.
2. Inspect the resulting PureIce stack(s).
3. Melt them back and total the released gas against what was originally
   vented.

## Technical detail (from decompiling Assembly-CSharp.dll — happy to share
## exact line numbers/IL if useful)
- `AtmosphereHelper.CreateIcePrefab(Mole mole, Vector3 worldPosition)`
  calls `PureIce.AssignSpawnGasValues(ice, mole.Type, mole.Quantity)`.
- `AssignSpawnGasValues` computes `int num = Mathf.CeilToInt(quantity / 50f)`
  (piece count needed) and stores `quantity / num` as the per-piece
  `SpawnGas.Quantity`, then calls `ice.SetQuantity(num)`.
- `Stackable.SetQuantity(int newQuantity)` does
  `Quantity = Mathf.Min(newQuantity, MaxQuantity)` — `PureIce.MaxQuantity`
  is 50. If `num` exceeds 50, the stack's actual piece count silently
  clamps to 50, but the per-piece `SpawnGas.Quantity` was already computed
  using the unclamped `num`, so `50 × (quantity/num)` is less than the
  original `quantity` whenever `num > 50`.

## Version
Found while developing a Workshop mod (Assembly-CSharp.dll build referenced
during investigation: 0.2.6428.27798). Please let me know if this is
already fixed in a newer build, or already tracked — happy to close this
out if so.
