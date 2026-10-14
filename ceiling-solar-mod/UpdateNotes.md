# Salty's Ceiling Solar: running dev log

A running record of what was done and **why**, written as the work happens. Newest entries go at the bottom.

## 2026-10-13: Idea and scaffold

Request: a new orientation for the Basic Solar Panel so it can be mounted on glass ceilings and glass walls instead of only standing upright on a floor. Players already enclose solar arrays in glass to protect them from storms, and sunlight reaches panels through glass, so a panel mounted against a glass roof or wall should still collect sun.

Goal: no new model. The mod should only need two changes: allow the extra placement rotations, and make the power calculation use the panel's real facing instead of assuming it stands upright.

Project skeleton copied from *Salty's Fabricator Overflow* (csproj, `.gitignore`, `.gitattributes`) and *Salty's Droid Standby* (`build-and-install.sh`).

## 2026-10-13: What the game code says (decompile of Update 0.2.6428.27798)

- **Power is already orientation-correct.** `SolarPanel.CalculateSolarEfficiency` averages `SolarPanelArm.CalculateSolarEfficiency` over its arms. Each arm uses `Cells.forward` as the panel normal and casts its rays from `Cells.position + Cells.rotation * offset` toward `OrbitalSimulation.WorldSunVector`. Nothing uses world up, so no power patch is needed.
- **Placement is gated by three prefab fields on `Structure`:** `PlacementType` (Grid/Face/FaceMount), `RotationAxis` (which rotate keys work) and `AllowedRotations` (Wall/Ceiling/Floor). `SolarPanel` doesn't override them and their values live on the prefab assets, not in code. `InventoryManager` reads them off the construction cursor.
- So the mod sets `RotationAxis = All` and `AllowedRotations = All` in a `SolarPanel.Awake` postfix. The cursor is instantiated from the prefab, so it gets the widened values too. `SmartRotate` handles Grid with any axes, and Face+All+All (`FaceAllAll`), so autoplace shouldn't log "doesn't yet work".
- **Aiming is mount-relative:** yaw spins about the pivot's local Z and pitch is ±75° about local X. A ceiling-hung panel therefore faces down by default. Left as is for v0.1; see the README.
- **Glass:** nothing in code special-cases windows. Whether a glass roof blocks the rays depends on the prefab's `CollisionMask`, so the patch logs the mask's layer names once per prefab. Players report glass-enclosed arrays working at about 70%, which suggests glass isn't in the mask.
- **Open questions for the in-game test:** do cables still connect after a flip (open-end permutation), and do the rotate keys actually turn the cursor on every axis.

## 2026-10-13: Redesign as a separate "Clinging" style

The owner clarified two things:
- The cells must face **into** the surface the panel is mounted on, so on a glass wall the panel looks outward and under a glass roof it looks at the sky. Vanilla panels face *away* from their mount, so widening the rotations alone gives the wrong result. A ceiling-hung panel would stare at the floor.
- It must be a **third style** in the kit, not extra rotations on Flat/Angled, "so they don't mix in with the standard style" and because extra rotations "would bloat the orientation menu too much".

So the rotation-widening patch is gone and the vanilla prefabs are untouched. Instead:
- **Prefab clone in a `Prefab.LoadAll` prefix.** That hook runs with `WorldManager.SourcePrefabs` filled, before anything is registered and before `InventoryManager.Initialize` builds construction cursors, so the clone is registered, gets a cursor and joins the kit like a vanilla prefab. LaunchPadBooster's own `AddPrefabs` uses the same hook. The clone is instantiated under an inactive holder, so its `Awake` doesn't run. `PrefabName`, `PrefabHash` (`Animator.StringToHash(name)`, validated by `Prefab.RegisterExisting`) and the GameObject name are all set to `StructureSolarPanelClinging`.
- **Kit:** appended (not inserted) to `ItemKitSolarPanelBasic.Constructables` with `PrefabUtils.AddToMultiConstructorKit`. The selected style travels as an index (`CreateStructureMessage.OptionIndex`), so existing indices 0/1 must not move, and server and clients must have the same list, hence `Networking.Required = true`.
- **Flip:** each arm's `Cells.localRotation *= Euler(180, 0, 0)`. Only the clone's own transforms change, never shared meshes or materials. Whether the visible cells turn with it depends on where the mesh sits in the hierarchy; the patch logs the hierarchy so the in-game test can tell.
- **Placement:** copies `StructureSolarPanel45`'s `PlacementType`/`RotationAxis`/`AllowedRotations`. Vanilla already places that one upside down, and its combination is handled by `SmartRotate.AutomaticSetup` and the cable open-end permutation. An unhandled combination (e.g. Wall|Ceiling only) risks mis-mapped cable connections, so dropping the floor option wasn't worth it.
- **Saves** store `PrefabName`; without the mod a clinging panel is skipped with a warning. The save-data type is the vanilla `SolarPanelSaveData`.
- **Name/description:** postfixes on `Localization.GetThingName` / `GetThingDescription`, which survive language reloads (the game's own tables are cleared on a language change).
- **Glass is still the open question.** The rays now leave through the panel's back, so `VerboseLogging` reports what the centre ray hits when a sun-facing clinging panel makes nothing.

## 2026-10-14: First in-game test, two bugs fixed

The owner placed clinging panels. The third kit style appeared, but:

1. **Only the cells flipped, not the frame.** The hierarchy dump showed why. The frame is the ROOT's mesh, and solar panels are `StructureRenderMode.Batched`: `Structure.Awake` destroys the root `MeshRenderer`, and `RenderBatch` draws each build state's `InitialDrawData` mesh with `Graphics.DrawMeshInstanced` at `BatchInstance.GetTRS()`, i.e. the root transform's pose. Only `Cells` (and the destroyed-state mesh) are ordinary child renderers. The root's rotation belongs to placement, so it can't be turned.
   Fix: compute one local `FlipMatrix`, a 180° turn about an axis in the mount plane plus a shift along the mount normal by `min + max`, so the model's slab of space maps back onto itself and the cells end up against the mount. Apply it to every child transform and to the root collider centre in the `LoadAll` prefix, and multiply it into `BatchInstance.GetTRS()` for the clinging panel's root-drawn frame. It also goes into `Structure.GetBatchMatrix()` (only the meson scanner overlays use it) and into the construction cursor's blueprint ghost (`InventoryManager.HandleStructurePrefab` postfix), because the cursor shows `Thing.Blueprint`, not the batched mesh. The separate 180° turn on `Cells` is gone: the whole model now turns as one.
2. **Deconstructing demanded the kit.** LaunchPadBooster's `AddToMultiConstructorKit` also sets `BuildStates[0].Tool.ToolExit = kit`, and `ToolExit` is the tool *required* to deconstruct (`Structure.AttackWith` → `ToolUse.IsToolExit`). `BuildState`/`ToolUse` are plain `[Serializable]` classes, deep-copied by `Instantiate`, so only the clone was affected. Fix: append to `ItemKitSolarPanelBasic.Constructables` directly and keep the flat panel's own `ToolExit`. The log now names the deconstruct tool.

Also from the log: Flat and Angled are both `FaceMount / Z / All`, so borrowing the Angled settings changed nothing. The clone now simply keeps the flat panel's placement. The panel's `CollisionMask` is `Default, Player, Terrain`. `VerboseLogging` now defaults to on while testing, and no longer logs panels that merely face away from the sun.

## 2026-10-14: Second test: frame still unflipped

Deconstruct with the normal tools (drill, crowbar) is confirmed fixed. The frame (the part built first) still faced the old way, and the log never printed the "frame flipped in the batch renderer" line. So the `BatchInstance.GetTRS` postfix never ran. The likely cause is that the JIT inlined that tiny struct method into `RenderBatch.Populate`, so Harmony's patch is bypassed. (`Structure.GetBatchMatrix`, a one-line getter, may be inlined too; it only feeds the meson scanner.)

Fix: the clone gets a child pivot `~SaltysClingFlip` carrying `FlipMatrix`, and a postfix on `ThingRenderer.GetRendererTransform()` returns that pivot instead of the root for clinging panels. `RenderBatch` reaches that method through the `IBatchable` interface, so it can't be inlined. Register, the instance matrix (`GetTRS` reads the transform it's handed) and Deregister (matched by transform) all use the same answer.

Correction, same day, before anyone tested 1502a90: inlining wasn't the reason. **The batch path is dead code in this game version.** `ThingRenderer.Mode()` is hard-coded to return `MeshRenderer`, so a DrawData `ThingRenderer` reports a null `SharedMesh`, `RenderBatch.Register` bails out, and neither `GetTRS` nor `GetRendererTransform` ever sees the frame. The frame is simply the root GameObject's own `MeshRenderer`/`MeshFilter`, which the hierarchy dump already marked `[mesh]`.

Real fix: in the `LoadAll` prefix, a child pivot `~SaltysClingFlip` gets `FlipMatrix`, a `Frame` child under it gets a new `MeshFilter`/`MeshRenderer` with the root's mesh, materials and shadow settings, and the root's renderer and filter are destroyed. The clone is a scene object under an inactive holder, so `DestroyImmediate` is fine; the shared mesh/material assets are only referenced. Build-state `Visualizer` references to the old renderer, and any `ThingRenderer` cached on the prefab, are repointed. `Thing.Awake` collects renderers with `GetComponentsInChildren<MeshRenderer>`, so the moved one keeps paint and visibility handling, and `CachePrefabBounds` reads renderer bounds, so the footprint follows. The two batch patches are removed; the meson scanner `GetBatchMatrix` postfix stays.
