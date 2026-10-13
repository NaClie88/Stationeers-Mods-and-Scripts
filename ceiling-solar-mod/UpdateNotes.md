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
