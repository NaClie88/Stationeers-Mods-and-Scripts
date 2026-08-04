# Sources — IC10 Fail-Safe Airlock Design

Every non-obvious claim in `ic10_failsafe_airlock_requirements.md` and
`ic10_airlock_prototype_code.md` traces back to one of these. Organized
by topic so you can drop the relevant subset into a GitHub repo's
CREDITS/SOURCES file without hunting back through chat history.

## IC10 language fundamentals
- Community Wiki, "Integrated Circuit (IC10)" — https://stationeers-wiki.com/Integrated_Circuit_(IC10)
- Community Wiki, "IC10" — https://stationeers-wiki.com/IC10
- XGamingServer, "Stationeers IC10 Programming: A Beginner's Guide" — https://xgamingserver.com/blog/stationeers-ic10-programming-guide/
- GitHub, SnorreSelmer/stationeers_ic10, "mips-programming-101.md" — https://github.com/SnorreSelmer/stationeers_ic10/blob/main/mips-programming-101.md
  (128 lines / 90 chars claim — conflicts with the entry below)
- GitHub, jhillacre/stationeers-scripts — https://github.com/jhillacre/stationeers-scripts
  (128 lines / 52 chars claim — conflicts with the entry above; unresolved, see verification checklist)
- GitHub, Zappes/Stationeers — https://github.com/Zappes/Stationeers
  (notes in-game Workshop script publishing is broken, why community code lives on GitHub instead)
- GitHub, drclaw1188/stationeers_ic10 — https://github.com/drclaw1188/stationeers_ic10

## Airlock circuitboards & lock/power mechanics
- Community Wiki, "Circuitboard (Advanced Airlock)" — https://stationeers-wiki.com/Circuitboard_(Advanced_Airlock)
  (lock persists through power loss — the core mechanic this whole design responds to)
- Community Wiki, "Blast Doors" — https://stationeers-wiki.com/Blast_Doors
  (same lockout behavior confirmed on Blast Doors; 25W/tick continuous draw)
- Community Wiki, "Crowbar" — https://stationeers-wiki.com/Crowbar
  (unlocked+unpowered requirement for manual operation)
- Community Wiki, "Custom Airlock IC10" — https://stationeers-wiki.com/Custom_Airlock_IC10
  (reference working script — cycling phases, LED-as-trigger pattern)
- Community Wiki, "Guide (Airlock) Atmosphere to Atmosphere" — https://stationeers-wiki.com/Guide_(Airlock)_Atmosphere_to_Atmosphere
  (Stalled phase, "Cancel Pressurize" button — confirms Stalled is real, not hypothetical)

## Power devices
- Community Wiki, "Power Controller" / "Area Power Controller" — https://stationeers-wiki.com/Power_Controller
  (battery-buffering/UPS behavior, Data Network properties)
- Community Wiki, "Logic Switch" — https://stationeers-wiki.com/Logic_Switch
  (Button/Switch functions fully unpowered — only the indicator light needs power)
- Community Wiki, "Active Vent" — https://stationeers-wiki.com/Active_Vent
  (100W confirmed draw, PressureExternal/Internal behavior)
- Community Wiki, "Powered Vent" — https://stationeers-wiki.com/Powered_Vent
  (2x/4x pressure throughput vs standard, "slightly higher" consumption, no internal pressure limiter)
- Steam Community, official patch notes discussion, "Update v0.2.4294.19984" — https://stationeers-wiki.com/Update_v0.2.4294.19984
  (active vent nonlinear pull-rate taper-off, confirmed)

## Storms & structural
- Community Wiki, "Storm" — https://stationeers-wiki.com/Storm
  (destructible items list, 1.5kW/panel solar storm boost, 1200-cube room cap, repair methods)

## Mods
- GitHub, Sukasa/ReVolt — https://github.com/Sukasa/ReVolt
  (Re-Volt mod feature list: Circuit Breakers, Cable Tray, Load Centers, Modular Batteries)
- Steam Workshop, Re-Volt — https://steamcommunity.com/sharedfiles/filedetails/?id=3587239682

## Steam Workshop scripts referenced (for cycling logic patterns, not directly quoted)
- "Emergency Bulkhead Lockdown" — https://steamcommunity.com/sharedfiles/filedetails/?id=2258102536
- "Airlock Control" — https://steamcommunity.com/sharedfiles/filedetails/?id=1524868713
  (confirms emergency-override-switch pattern — optional d5 lever)
- "Custom Airlock V2" by CowsAreEvil — https://steamcommunity.com/sharedfiles/filedetails/?id=2978749569
  (full source inspected directly — Propped-Open behavior already implemented and working in production; confirmed `brdns` instruction, optional-button batch-defaults-to-zero pattern, real LogicType names, real match-tolerance values; author is the same "Cows Are Evil" cited elsewhere in this project for Venus playthrough automation)
- "Adaptive Airlock" — https://steamcommunity.com/sharedfiles/filedetails/?id=2194510353
  (independently confirms emergency-mode manual-override-opening pattern)

## Community troubleshooting threads (specific gotchas)
- Steam Community, "Every update breaks IC10?" — https://steamcommunity.com/app/544550/discussions/0/4751948599934992260
  (stack persistence across reloads/restarts — confirmed real bug source)
- Steam Community, "Adaptive Airlock" comments — https://steamcommunity.com/sharedfiles/filedetails/comments/2194510353
  (`dr##`-style invalid register syntax confirmed as a real error — use `alias` instead)
- Steam Community, "Unlock (not open) airlock door when the power is cut" (Suggestions) — https://steamcommunity.com/app/544550/discussions/3/1621724915791890334/
  (dedicated Power Controller + swappable battery as the community-standard workaround)
- Steam Community, "logic transmitters with ic chip tutorials?" — https://steamcommunity.com/app/544550/discussions/0/669472753009635911/
  (multi-chip coordination via shared device state / Logic Transmitter pattern)

---

**Publishing plan (for tracking, not action yet):** the finished project
ships as two things together — (1) an example creative-mode Stationeers
world demonstrating the build in-game, and (2) the code base itself
(IC10 scripts + the JSON/markdown database files in this project). Both
go up together once the build is done, vanilla and modded variants
alike.

---

**Note on citation practice going forward:** every new claim added to
either the requirements doc or the prototype code should get a URL
added here in the same turn, not just a vague "confirmed via wiki"
in-line. Anything in those two files without a matching entry here
hasn't been properly sourced yet — treat that as a gap to fix before
publishing, not an oversight to ignore.
