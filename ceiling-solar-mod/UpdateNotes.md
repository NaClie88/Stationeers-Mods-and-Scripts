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
