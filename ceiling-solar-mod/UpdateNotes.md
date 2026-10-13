# Salty's Ceiling Solar: running dev log

A running record of what was done and **why**, written as the work happens. Newest entries go at the bottom.

## 2026-10-13: Idea and scaffold

Request: a new orientation for the Basic Solar Panel so it can be mounted on glass ceilings and glass walls instead of only standing upright on a floor. Players already enclose solar arrays in glass to protect them from storms, and sunlight reaches panels through glass, so a panel mounted against a glass roof or wall should still collect sun.

Goal: no new model. The mod should only need two changes: allow the extra placement rotations, and make the power calculation use the panel's real facing instead of assuming it stands upright.

Project skeleton copied from *Salty's Fabricator Overflow* (csproj, `.gitignore`, `.gitattributes`) and *Salty's Droid Standby* (`build-and-install.sh`).
