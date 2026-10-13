# Salty's Pipe Splicer: running dev log

A running record of what was done and **why**, written as the work happens. Newest entries go at the bottom.

## 2026-10-13: Idea

Request (in the Rotation Cycle thread): when placing a new connection into a long run (e.g. a 10-long straight), break it and rebuild it from shorter lengths, for both electrical and pipe networks, without blowing out gas pipes. Built as its own mod because it has to run on the host, while Rotation Cycle is client-only.

## 2026-10-13: What the game code says (decompile of Update 0.2.6428.27798)

- **Long pieces are separate prefabs:** `<family>3/5/10` for StructurePipeStraight, StructurePipeLiquidStraight, StructureInsulatedPipeStraight, StructureInsulatedPipeLiquidStraight, StructureCableStraight, StructureCableStraightH, StructureCableSuperHeavyStraight (+ Burnt cable variants). The single is the bare family name. Chutes have long versions but aren't IGridMergeable. No split/shorten code exists.
- **Merge:** `MultiMergeConstructor.Construct` (server only, no re-check of the client's CanReplace) takes `GridController.GetPipe/GetCable(localPosition)`, ORs open ends, `OnServer.Destroy`s it and builds ONE single-cell piece. On a long piece that deletes the rest of the run. The client cursor refuses it via `Piping.CanReplace` (`DontAllowMergingWithWrench`) or `Cable._IsCollision` (`BlockMergeWithOtherCables`); the user confirmed the game blocks merging onto long pieces today.
- **Gas:** one `Atmosphere` per pipe network. `Pipe.OnDestroy` snapshots the whole network's gas, rebuilds each neighbour's network and queues `NetworkAtmosphereEvent.DivideNetworkAtmosphere`, which at the next atmos tick splits the moles over the surviving networks by volume. Nothing vents; removing volume raises pressure. Burst damage accrues per atmos tick while over pressure, not instantly. So adding the same number of cells back in the same frame keeps the pressure.
- **Cables:** network rebuild on destroy, no stored energy in the network, fuses/burn only evaluated in power ticks.
- **Building from code:** `Constructor.SpawnConstruct(new CreateStructureInstance(prefab, oldPiece) { LocalGrid = origin })` on the server; `Thing.Create` replicates to clients. Prefab cells via `GridBounds._gridsSmall` (filled at prefab load) and `GetLocalSmallGrid(pos, rot)`, ordered along the piece.
- **Cost:** `MultiConstructor.OnUseItem(qty)` clamps to the stack, so check `Quantity` first. Long straights are discounted (user).

## 2026-10-13: Design (v0.1)

- Prefix on `MultiMergeConstructor.Construct`: if the target is a long straight, plan the rebuild (cheapest mix of 10/5/3/1 per side by `EntryQuantity`, each piece's origin solved so its cells exactly match the run, checked with `GetLocalSmallGrid`). If anything doesn't fit, block the merge (the vanilla merge would delete the run). Charge `extra = cost(replacements) + cost(single) − cost(long)` before anything changes.
- The original method then merges the junction against the long piece. Its charge is looked up by ConnectionType Straight, i.e. against a single straight, which is why the single is in `extra`.
- Postfix spawns the replacement pieces in the same frame. Logs the pieces, the extra cost and, for pipes, network pressure/moles before and 2 s after.
- Client: `Piping.CanReplace` postfix and `Cable._IsCollision` postfix let the cursor through over a long straight of the same type with the wrench in hand; `Piping`/`Cable.CanReplace` then check the kit covers `extra` + the junction and show "needs N more" if not.
- **Open questions for the in-game test:** does `GetPipe(localPosition)` return the long piece; do the spawned pieces register correctly while the long one is being destroyed in the same frame (vanilla does destroy-then-create the same way); does the pressure line show no change.

## 2026-10-13: Review against the decompile

- Compiles (0 errors). `OnServer.Destroy` is deferred to end of frame, `SmallCell.Add` overwrites a slot and `RemoveSmallGridStructure` only clears a slot still holding that object, so the long piece's late deregistration can't wipe the new pieces. Vanilla's own merge relies on the same.
- Gas: the new pieces join the old network mid-frame (volume briefly long + new); at end of frame `Pipe.OnDestroy(long)` removes its volume, snapshots the full gas and divides it over the rebuilt network by volume. Conserved.
- The junction's shape is the OR of the new piece's and the long piece's open-end layout, and its price is looked up by the long piece's ConnectionType. Both need the long prefab to describe itself as a Straight along its length; that's prefab data, so the prefix now sets ConnectionType Straight + the matching permutation on the doomed piece if needed and logs what it found (`Merge layout for …`).
- Known risk, same as vanilla merges: a tee whose third end joins a separate pressurised network merges it mid-frame with a `CreateAdd` that may land on an atmosphere the end-of-frame rebuild orphans, so that side network's gas could be lost. Test it; the 2 s pressure line shows it.
