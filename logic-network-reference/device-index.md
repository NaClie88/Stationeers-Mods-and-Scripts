# Device Index — every vanilla device's LogicType names (unverified skeleton)

**Status: broad framework, community-sourced, NOT independently verified
against decompiled ground truth.** This is the "what LogicTypes does
each device expose, by name" skeleton the project owner asked for —
useful for knowing what to look up, not yet trustworthy for anything
load-bearing. See [`README.md`](README.md) and
[`base-behavior.md`](base-behavior.md) for the verified, decompiled-
ground-truth layer this project is building on top of this skeleton
over time, and `devices/power-controller.md` for a concrete example of
this exact source getting a device's behavior subtly wrong (`Charge`
described as "Current charge of battery in slot" — no mention that it
also includes live input-network power — the same imprecision that
misled `ic10-airlock/watcher.ic10`'s original design).

## Source and license

Extracted from
[`FlorpyDorpinator/StationpediaAscended`](https://github.com/FlorpyDorpinator/StationpediaAscended)'s
`mod/descriptions.json` (2026-08-06 snapshot, 499 devices), a
community-authored Stationeers mod project. That repo's `README.md`
states an MIT license, though **the repository did not actually
contain a `LICENSE` file at the time of this extraction** — as a
result, only the plain facts (device names and the LogicType field
names each one exposes) are reproduced here, not that project's own
prose descriptions or file structure, to stay clearly on the safe side
of that ambiguity. If you maintain that repo and want to clarify the
license, or if a `LICENSE` file gets added later, this note should be
revisited.

**Names marked with `*`** didn't have a `displayName` in the source
data — the name shown is derived from the class key instead (prefix
stripped, camelCase split into words) and may not exactly match the
in-game name. Cross-check these against Stationpedia in-game before
relying on the display name specifically; the `deviceKey` (real class
name) is accurate regardless.

## How to use this table

1. Find a device by name or `deviceKey` below to see what LogicType
   *names* it exposes — a starting point for "does this device even
   have a Charge field," not a description of what each one means.
2. For anything actually load-bearing (a script, a mod, a wiki edit),
   check [`ground-truth-database.md`](ground-truth-database.md) first
   (120 classes, decompiled, covers most devices with any non-trivial
   LogicType behavior) or `devices/*.md` if that device has a
   hand-written entry there (deeper explanation, not just raw
   expressions), or decompile it yourself following `README.md`'s
   methodology section — don't trust this table's presence/absence of
   a LogicType as final, and never trust what a LogicType *means* from
   this table alone (only `ground-truth-database.md`, `devices/*.md`,
   or your own decompilation should be treated as sourcing a
   *meaning*, not just a name).
3. `(none listed)` means the source had no `logicDescriptions` entries
   for that device — could mean genuinely no LogicTypes, or could mean
   the source data just didn't cover it. Not yet distinguished.

| Display name | Class key (`deviceKey`) | LogicTypes |
|---|---|---|
| Access Bridge * | `ThingStructureAccessBridge` | Activate, Lock, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower |
| Active Vent * | `ThingStructureActiveVent` | CombustionOutput, Error, Lock, Maximum, Mode, NameHash, On, Open, Power, PrefabHash, PressureExternal, PressureInternal, PressureOutput, Ratio, RatioCarbonDioxideOutput, RatioNitrogenOutput, RatioNitrousOxideOutput, RatioOxygenOutput, RatioPollutantOutput, RatioVolatilesOutput, RatioWaterOutput, ReferenceId, RequiredPower, Setting, TemperatureOutput, TotalMolesOutput |
| Advanced Composter | `ThingStructureAdvancedComposter` | Activate, ClearMemory, Error, ExportCount, ImportCount, Lock, Maximum, Mode, NameHash, On, Open, Power, PrefabHash, Quantity, Ratio, ReferenceId, RequiredPower, Setting |
| Advanced Furnace | `ThingStructureAdvancedFurnace` | Activate, ClearMemory, Combustion, Error, ExportCount, ImportCount, Lock, Maximum, Mode, NameHash, On, Open, Power, PrefabHash, Pressure, Ratio, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, Reagents, RecipeHash, ReferenceId, RequiredPower, Setting, SettingInput, SettingOutput, Temperature, TotalMoles |
| Advanced Packaging Machine * | `ThingStructureAdvancedPackagingMachine` | Activate, ClearMemory, CompletionRatio, Error, ExportCount, ImportCount, Lock, NameHash, On, Open, Power, PrefabHash, Reagents, RecipeHash, ReferenceId, RequiredPower, StackSize |
| Advanced Tablet | `ThingItemAdvancedTablet` | Activate, Error, Mode, On, Power, ReferenceId, SoundAlert, Volume |
| Air Conditioner | `ThingStructureAirConditioner` | CombustionInput, CombustionOutput, CombustionOutput2, Error, Lock, Maximum, Mode, NameHash, On, Open, OperationalTemperatureEfficiency, Power, PrefabHash, PressureEfficiency, PressureInput, PressureOutput, PressureOutput2, Ratio, RatioCarbonDioxideInput, RatioCarbonDioxideOutput, RatioCarbonDioxideOutput2, RatioLiquidCarbonDioxideInput, RatioLiquidCarbonDioxideOutput, RatioLiquidCarbonDioxideOutput2, RatioLiquidNitrogenInput, RatioLiquidNitrogenOutput, RatioLiquidNitrogenOutput2, RatioLiquidNitrousOxideInput, RatioLiquidNitrousOxideOutput, RatioLiquidNitrousOxideOutput2, RatioLiquidOxygenInput, RatioLiquidOxygenOutput, RatioLiquidOxygenOutput2, RatioLiquidPollutantInput, RatioLiquidPollutantOutput, RatioLiquidPollutantOutput2, RatioLiquidVolatilesInput, RatioLiquidVolatilesOutput, RatioLiquidVolatilesOutput2, RatioNitrogenInput, RatioNitrogenOutput, RatioNitrogenOutput2, RatioNitrousOxideInput, RatioNitrousOxideOutput, RatioNitrousOxideOutput2, RatioOxygenInput, RatioOxygenOutput, RatioOxygenOutput2, RatioPollutantInput, RatioPollutantOutput, RatioPollutantOutput2, RatioSteamInput, RatioSteamOutput, RatioSteamOutput2, RatioVolatilesInput, RatioVolatilesOutput, RatioVolatilesOutput2, RatioWaterInput, RatioWaterOutput, RatioWaterOutput2, ReferenceId, RequiredPower, Setting, TemperatureDifferentialEfficiency, TemperatureInput, TemperatureOutput, TemperatureOutput2, TotalMolesInput, TotalMolesOutput, TotalMolesOutput2 |
| Airlock | `ThingStructureAirlock` | Idle, Lock, Mode, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Airlock Gate * | `ThingStructureAirlockGate` | Idle, Lock, Mode, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Airlock Wide | `ThingStructureAirlockWide` | Idle, Lock, Mode, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Angle Grinder | `ThingItemAngleGrinder` | Activate, Power, ReferenceId |
| Arc Furnace | `ThingStructureArcFurnace` | (none listed) |
| Arc Welder | `ThingItemArcWelder` | Activate, Power, ReferenceId |
| Area Power Control | `ThingStructureAreaPowerControl` | Charge, Error, Lock, Maximum, Mode, NameHash, On, Open, Power, PowerActual, PowerPotential, PrefabHash, Ratio, ReferenceId, RequiredPower |
| Area Power Control (Reversed) | `ThingStructureAreaPowerControlReversed` | Charge, Error, Lock, Maximum, Mode, NameHash, On, Open, Power, PowerActual, PowerPotential, PrefabHash, Ratio, ReferenceId, RequiredPower |
| Autolathe | `ThingStructureAutolathe` | Activate, ClearMemory, CompletionRatio, Error, ExportCount, ImportCount, Lock, NameHash, On, Open, Power, PrefabHash, Reagents, RecipeHash, ReferenceId, RequiredPower, StackSize |
| Automated Oven | `ThingStructureAutomatedOven` | (none listed) |
| Autominer (Small) | `ThingStructureAutoMinerSmall` | Activate, ClearMemory, Error, ExportCount, ImportCount, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower |
| Back Pressure Regulator | `ThingStructureBackPressureRegulator` | Error, Lock, Maximum, NameHash, On, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Basket Hoop | `ThingStructureBasketHoop` | Lock, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Batch Slot Reader | `ThingStructureLogicBatchSlotReader` | Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Battery (Medium) | `ThingStructureBatteryMedium` | Charge, Maximum, Mode, NameHash, On, Power, PowerActual, PowerPotential, PrefabHash, Ratio, ReferenceId |
| Battery Cell (Large) | `ThingItemBatteryCellLarge` | Mode, ReferenceId |
| Battery Cell (Nuclear) | `ThingItemBatteryCellNuclear` | Mode, ReferenceId |
| Battery Cell (Small) | `ThingItemBatteryCell` | Mode, ReferenceId |
| Battery Cell Charger | `ThingStructureBatteryCharger` | Activate, Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Battery Charger Small | `ThingStructureBatteryChargerSmall` | Activate, Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Battery Large * | `ThingStructureBatteryLarge` | Charge, Error, Lock, Maximum, Mode, NameHash, On, Power, PowerActual, PowerPotential, PrefabHash, Ratio, ReferenceId |
| Battery Small | `ThingStructureBatterySmall` | Charge, ChargeRatio, Error, Maximum, Mode, NameHash, On, Power, PrefabHash, ReferenceId, Setting |
| Battery Wireless Cell | `ThingBattery_Wireless_cell` | Mode, ReferenceId |
| Battery Wireless Cell (Big) | `ThingBattery_Wireless_cell_Big` | Mode, ReferenceId |
| Beacon | `ThingStructureBeacon` | Color, Error, Lock, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Bench (Angled) | `ThingStructureAngledBench` | NameHash, PrefabHash, ReferenceId |
| Bench (Counter Style) | `ThingStructureBench1` | Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Bench (Flat) | `ThingStructureFlatBench` | NameHash, PrefabHash, ReferenceId |
| Bench (Frame Style) | `ThingStructureBench3` | Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Bench (High Tech Style) | `ThingStructureBench2` | Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Bench (Workbench Style) | `ThingStructureBench4` | Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Blast Door | `ThingStructureBlastDoor` | Idle, Lock, Mode, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Block Bed | `ThingStructureBlockBed` | Activate, Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Button | `ThingStructureLogicButton` | (none listed) |
| Cable Analyzer | `ThingStructureCableAnalysizer` | NameHash, PowerActual, PowerPotential, PowerRequired, PrefabHash, ReferenceId |
| Cable Fuse (500kW) | `ThingStructureCableFuse500k` | NameHash, PrefabHash, ReferenceId |
| Camera | `ThingStructureCamera` | Mode, NameHash, On, PrefabHash, ReferenceId |
| Cargo Storage (Medium) | `ThingStructureCargoStorageMedium` | ClearMemory, Error, ExportCount, ImportCount, Lock, NameHash, On, Open, Power, PrefabHash, Quantity, Ratio, ReferenceId, RequiredPower |
| Cargo Storage (Small) | `ThingStructureCargoStorageSmall` | ClearMemory, Error, ExportCount, ImportCount, Lock, NameHash, On, Open, Power, PrefabHash, Quantity, Ratio, ReferenceId, RequiredPower |
| Cartridge Debug Analyzer * | `ThingCartridgeDebugAnalyzer` | ReferenceId |
| CCTV Camera (Fish-Eye) | `ThingStructureSecurityCameraFishEye` | Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| CCTV Camera (Left) | `ThingStructureSecurityCameraLeft` | Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| CCTV Camera (Panning) | `ThingStructureSecurityCameraPanning` | Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| CCTV Camera (Right) | `ThingStructureSecurityCameraRight` | Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| CCTV Camera (Straight) | `ThingStructureSecurityCameraStraight` | Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Centrifuge * | `ThingStructureCentrifuge` | ClearMemory, Error, ExportCount, ImportCount, NameHash, On, Open, Power, PrefabHash, Reagents, ReferenceId, RequiredPower |
| Chair | `ThingStructureChair` | NameHash, PrefabHash, ReferenceId |
| Chair (Backless Double) | `ThingStructureChairBacklessDouble` | NameHash, PrefabHash, ReferenceId |
| Chair (Backless Single) | `ThingStructureChairBacklessSingle` | NameHash, PrefabHash, ReferenceId |
| Chair (Booth Corner Left) | `ThingStructureChairBoothCornerLeft` | NameHash, PrefabHash, ReferenceId |
| Chair (Booth Middle) | `ThingStructureChairBoothMiddle` | NameHash, PrefabHash, ReferenceId |
| Chair (Rectangle Double) | `ThingStructureChairRectangleDouble` | NameHash, PrefabHash, ReferenceId |
| Chair (Rectangle Single) | `ThingStructureChairRectangleSingle` | NameHash, PrefabHash, ReferenceId |
| Chair (Thick Double) | `ThingStructureChairThickDouble` | NameHash, PrefabHash, ReferenceId |
| Chair (Thick Single) | `ThingStructureChairThickSingle` | NameHash, PrefabHash, ReferenceId |
| Chute Digital Flip Flop Splitter Left | `ThingStructureChuteDigitalFlipFlopSplitterLeft` | Mode, NameHash, On, Power, PrefabHash, Quantity, ReferenceId, RequiredPower, Setting, SettingOutput |
| Chute Digital Flip Flop Splitter Right | `ThingStructureChuteDigitalFlipFlopSplitterRight` | Mode, NameHash, On, Power, PrefabHash, Quantity, ReferenceId, RequiredPower, Setting, SettingOutput |
| Chute Digital Valve Left | `ThingStructureChuteDigitalValveLeft` | Lock, NameHash, On, Open, Power, PrefabHash, Quantity, ReferenceId, RequiredPower, Setting |
| Chute Digital Valve Right | `ThingStructureChuteDigitalValveRight` | Lock, NameHash, On, Open, Power, PrefabHash, Quantity, ReferenceId, RequiredPower, Setting |
| Chute Export Bin | `ThingStructureChuteExportBin` | Error, Lock, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower |
| Chute Flip Flop Splitter | `ThingStructureChuteFlipFlopSplitter` | Mode, ReferenceId |
| Chute Import Bin | `ThingStructureChuteBin` | Error, Lock, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower |
| Chute Inlet | `ThingStructureChuteInlet` | ClearMemory, ImportCount, Lock, NameHash, PrefabHash, ReferenceId |
| Chute Outlet | `ThingStructureChuteOutlet` | ClearMemory, ExportCount, ImportCount, Lock, NameHash, PrefabHash, ReferenceId |
| Circuit Housing * | `ThingStructureCircuitHousing` | Error, LineNumber, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting, StackSize |
| Combustion Centrifuge | `ThingStructureCombustionCentrifuge` | ClearMemory, Combustion, CombustionInput, CombustionLimiter, CombustionOutput, Error, ExportCount, ImportCount, Lock, On, Open, Power, PrefabHash, Pressure, PressureInput, PressureOutput, RatioCarbonDioxide, RatioCarbonDioxideInput, RatioCarbonDioxideOutput, RatioLiquidCarbonDioxide, RatioLiquidCarbonDioxideInput, RatioLiquidCarbonDioxideOutput, RatioLiquidNitrogen, RatioLiquidNitrogenInput, RatioLiquidNitrogenOutput, RatioLiquidNitrousOxide, RatioLiquidNitrousOxideInput, RatioLiquidNitrousOxideOutput, RatioLiquidOxygen, RatioLiquidOxygenInput, RatioLiquidOxygenOutput, RatioLiquidPollutant, RatioLiquidPollutantInput, RatioLiquidPollutantOutput, RatioLiquidVolatiles, RatioLiquidVolatilesInput, RatioLiquidVolatilesOutput, RatioNitrogen, RatioNitrogenInput, RatioNitrogenOutput, RatioNitrousOxide, RatioNitrousOxideInput, RatioNitrousOxideOutput, RatioOxygen, RatioOxygenInput, RatioOxygenOutput, RatioPollutant, RatioPollutantInput, RatioPollutantOutput, RatioSteam, RatioSteamInput, RatioSteamOutput, RatioVolatiles, RatioVolatilesInput, RatioVolatilesOutput, RatioWater, RatioWaterInput, RatioWaterOutput, Reagents, RequiredPower, Rpm, Stress, Temperature, TemperatureInput, TemperatureOutput, Throttle, TotalMoles, TotalMolesInput, TotalMolesOutput |
| Composite Door | `ThingStructureCompositeDoor` | Idle, Lock, Mode, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Composite Roll Cover | `ThingCompositeRollCover` | Idle, Lock, Mode, NameHash, On, Open, PrefabHash, ReferenceId, Setting |
| Composite Window Shutter Controller | `ThingStructureCompositeWindowShutterController` | Error, Lock, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower |
| Computer | `ThingModularDeviceComputer` | Error, NameHash, On, Open, Power, PrefabHash, ReferenceId |
| Computer (Big Screen Wall Mounted) | `ThingStructureComputerBigScreenWallMounted` | Error, Lock, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower |
| Computer (Big Screen) | `ThingStructureComputerBigScreen` | Error, Lock, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower |
| Computer (Modern) | `ThingStructureComputer` | Error, Lock, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower |
| Computer (Retro) | `ThingStructureComputerUpright` | Error, Lock, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower |
| Condensation Chamber | `ThingStructureCondensationChamber` | Combustion, Error, Lock, Maximum, NameHash, On, Open, Power, PrefabHash, Pressure, Ratio, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, RequiredPower, Setting, Temperature, TotalMoles |
| Condensation Valve | `ThingStructureCondensationValve` | Maximum, NameHash, On, PrefabHash, Ratio, ReferenceId, Setting |
| Console | `ThingStructureConsole` | (none listed) |
| Console 2x2 | `ThingStructureConsole2x2` | Error, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Console Base 1 | `ThingStructureConsoleBase1` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Base 1 Double | `ThingStructureConsoleBase1Double` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Base 2 | `ThingStructureConsoleBase2` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Base 2 Double | `ThingStructureConsoleBase2Double` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Base 3 | `ThingStructureConsoleBase3` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Base 3 Double | `ThingStructureConsoleBase3Double` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Base 4 | `ThingStructureConsoleBase4` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Base 4 Double | `ThingStructureConsoleBase4Double` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Base 5 | `ThingStructureConsoleBase5` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Base 5 Double | `ThingStructureConsoleBase5Double` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Base 6 | `ThingStructureConsoleBase6` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Base 6 Double | `ThingStructureConsoleBase6Double` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Base Corner 1 | `ThingStructureConsoleBaseCorner1` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Base Corner 2 | `ThingStructureConsoleBaseCorner2` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Base Corner 3 | `ThingStructureConsoleBaseCorner3` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Base Corner 4 | `ThingStructureConsoleBaseCorner4` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Base Corner 5 | `ThingStructureConsoleBaseCorner5` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Base Corner 6 | `ThingStructureConsoleBaseCorner6` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Corner Inner 1 | `ThingStructureConsoleCornerInner1` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Corner Inner 1 Center | `ThingStructureConsoleCornerInner1Center` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Corner Inner 1 Double | `ThingStructureConsoleCornerInner1Double` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Corner Inner 2 | `ThingStructureConsoleCornerInner2` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Corner Inner 2 Center | `ThingStructureConsoleCornerInner2Center` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Corner Inner 2 Double | `ThingStructureConsoleCornerInner2Double` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Dual | `ThingStructureConsoleDual` | Activate, Error, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Console Flat 1 | `ThingStructureConsoleFlat1` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Flat 1 Corner | `ThingStructureConsoleFlat1Corner` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Flat 1 Corner 2 | `ThingStructureConsoleFlat1Corner2` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Flat 1 Corner 3 | `ThingStructureConsoleFlat1Corner3` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Flat 1 Double | `ThingStructureConsoleFlat1Double` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Flat 2 | `ThingStructureConsoleFlat2` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Flat 2 Double | `ThingStructureConsoleFlat2Double` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Flat 3 | `ThingStructureConsoleFlat3` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Flat 3 Double | `ThingStructureConsoleFlat3Double` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Flat 3x2 | `ThingStructureConsoleFlat3x2` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Flat 3x2 Double | `ThingStructureConsoleFlat3x2Double` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Flat Corner Inner | `ThingStructureConsoleFlatCornerInner` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Flat Corner Inner Center | `ThingStructureConsoleFlatCornerInnerDoubleCenter` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Flat Corner Inner Double | `ThingStructureConsoleFlatCornerInnerDouble` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console LED1x2 * | `ThingStructureConsoleLED1x2` | Color, Error, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Console LED1x3 * | `ThingStructureConsoleLED1x3` | Color, Error, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Console LED5 * | `ThingStructureConsoleLED5` | Color, Error, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Console Monitor | `ThingStructureConsoleMonitor` | Activate, Error, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Console Terminal | `ThingStructureConsoleTerminal` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Terminal 2 | `ThingStructureConsoleTerminal2` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Wall Mount 1 | `ThingStructureConsoleWallMount1` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Console Wall Mount 2 | `ThingStructureConsoleWallMount2` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Corner Locker | `ThingStructureCornerLocker` | Lock, NameHash, Open, PrefabHash, ReferenceId |
| CounterFlow Heat Exchanger - Gas + Gas | `ThingStructurePassthroughHeatExchangerGasToGas` | Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| CounterFlow Heat Exchanger - Gas + Liquid | `ThingStructurePassthroughHeatExchangerGasToLiquid` | (none listed) |
| CounterFlow Heat Exchanger - Liquid + Liquid | `ThingStructurePassthroughHeatExchangerLiquidToLiquid` | Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| Cryo Tube Horizontal | `ThingStructureCryoTubeHorizontal` | Activate, EntityState, Error, Lock, Maximum, Mode, NameHash, On, Open, Power, PrefabHash, Pressure, Ratio, ReferenceId, RequiredPower, Setting, Temperature |
| Cryo Tube Vertical | `ThingStructureCryoTubeVertical` | Activate, EntityState, Error, Lock, Maximum, Mode, NameHash, On, Open, Power, PrefabHash, Pressure, Ratio, ReferenceId, RequiredPower, Setting, Temperature |
| Daylight Sensor * | `ThingStructureDaylightSensor` | Activate, Horizontal, Mode, NameHash, On, PrefabHash, ReferenceId, SolarAngle, SolarIrradiance, Vertical |
| Deep Miner | `ThingStructureDeepMiner` | ClearMemory, Error, ExportCount, ImportCount, Maximum, NameHash, On, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Digital Valve * | `ThingStructureDigitalValve` | Error, Lock, Maximum, NameHash, On, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Diode | `ThingStructureDiode` | NameHash, PrefabHash, ReferenceId |
| Diode Slide | `ThingStructureDiodeSlide` | Lock, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Diode Slide 1 | `ThingModularDeviceSliderDiode1` | Color, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Diode Slide 2 | `ThingModularDeviceSliderDiode2` | Color, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Drill * | `ThingItemDrill` | Activate, Power, ReferenceId |
| Drinking Fountain | `ThingStructureDrinkingFountain2x1` | Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Droid Sleeper Vertical | `ThingStructureSleeperVerticalDroid` | Activate, Error, Lock, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower |
| Dynamic GPR | `ThingDynamicGPR` | Activate, On, Power, ReferenceId |
| Electrolyzer | `ThingStructureElectrolyzer` | Activate, Combustion, CombustionInput, CombustionOutput, Error, Lock, Maximum, Mode, NameHash, On, Open, Power, PrefabHash, Pressure, PressureInput, PressureOutput, Ratio, RatioCarbonDioxide, RatioCarbonDioxideInput, RatioCarbonDioxideOutput, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidCarbonDioxideInput, RatioLiquidCarbonDioxideOutput, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrogenInput, RatioLiquidNitrogenOutput, RatioLiquidNitrousOxide, RatioLiquidNitrousOxideInput, RatioLiquidNitrousOxideOutput, RatioLiquidOxygen, RatioLiquidOxygenInput, RatioLiquidOxygenOutput, RatioLiquidPollutant, RatioLiquidPollutantInput, RatioLiquidPollutantOutput, RatioLiquidVolatiles, RatioLiquidVolatilesInput, RatioLiquidVolatilesOutput, RatioNitrogen, RatioNitrogenInput, RatioNitrogenOutput, RatioNitrousOxide, RatioNitrousOxideInput, RatioNitrousOxideOutput, RatioOxygen, RatioOxygenInput, RatioOxygenOutput, RatioPollutant, RatioPollutantInput, RatioPollutantOutput, RatioPollutedWater, RatioSteam, RatioSteamInput, RatioSteamOutput, RatioVolatiles, RatioVolatilesInput, RatioVolatilesOutput, RatioWater, RatioWaterInput, RatioWaterOutput, ReferenceId, RequiredPower, Setting, Temperature, TemperatureInput, TemperatureOutput, TotalMoles, TotalMolesInput, TotalMolesOutput |
| Electronics Printer | `ThingStructureElectronicsPrinter` | Activate, ClearMemory, CompletionRatio, Error, ExportCount, ImportCount, Lock, NameHash, On, Open, Power, PrefabHash, Reagents, RecipeHash, ReferenceId, RequiredPower, StackSize |
| Elevator Level | `ThingStructureElevatorLevelIndustrial` | Activate, ElevatorLevel, ElevatorSpeed, Error, Lock, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower |
| Elevator Level (Cabled) | `ThingStructureElevatorLevelFront` | Activate, ElevatorLevel, ElevatorSpeed, Error, Lock, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower |
| Elevator Shaft | `ThingStructureElevatorShaftIndustrial` | ElevatorLevel, ElevatorSpeed, NameHash, PrefabHash, ReferenceId |
| Elevator Shaft (Cabled) | `ThingStructureElevatorShaft` | ElevatorLevel, ElevatorSpeed, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Emergency Angle Grinder | `ThingItemEmergencyAngleGrinder` | Activate, Power, ReferenceId |
| Emergency Arc Welder | `ThingItemEmergencyArcWelder` | Activate, Power, ReferenceId |
| Emergency Button | `ThingModularDeviceEmergencyButton3x3` | Activate, NameHash, PrefabHash, ReferenceId, Setting |
| Emergency Button * | `ThingStructureEmergencyButton` | Activate, Error, Lock, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Emergency Drill | `ThingItemEmergencyDrill` | Activate, Power, ReferenceId |
| Emergency Space Helmet | `ThingItemEmergencySpaceHelmet` | Combustion, Flush, Lock, On, Open, Power, Pressure, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, SoundAlert, Temperature, TotalMoles, Volume |
| Evaporation Chamber | `ThingStructureEvaporationChamber` | Combustion, Error, Lock, Maximum, NameHash, On, Open, Power, PrefabHash, Pressure, Ratio, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, RequiredPower, Setting, Temperature, TotalMoles |
| Expansion Valve | `ThingStructureExpansionValve` | Maximum, NameHash, On, PrefabHash, Ratio, ReferenceId, Setting |
| Filtration | `ThingStructureFiltration` | (none listed) |
| Filtration Liquid | `ThingStructureFiltrationLiquid` | CombustionInput, CombustionOutput, CombustionOutput2, Error, Lock, Maximum, Mode, NameHash, On, Open, Power, PrefabHash, PressureInput, PressureOutput, PressureOutput2, Ratio, RatioCarbonDioxideInput, RatioCarbonDioxideOutput, RatioCarbonDioxideOutput2, RatioLiquidCarbonDioxideInput, RatioLiquidCarbonDioxideOutput, RatioLiquidCarbonDioxideOutput2, RatioLiquidNitrogenInput, RatioLiquidNitrogenOutput, RatioLiquidNitrogenOutput2, RatioLiquidNitrousOxideInput, RatioLiquidNitrousOxideOutput, RatioLiquidNitrousOxideOutput2, RatioLiquidOxygenInput, RatioLiquidOxygenOutput, RatioLiquidOxygenOutput2, RatioLiquidPollutantInput, RatioLiquidPollutantOutput, RatioLiquidPollutantOutput2, RatioLiquidVolatilesInput, RatioLiquidVolatilesOutput, RatioLiquidVolatilesOutput2, RatioNitrogenInput, RatioNitrogenOutput, RatioNitrogenOutput2, RatioNitrousOxideInput, RatioNitrousOxideOutput, RatioNitrousOxideOutput2, RatioOxygenInput, RatioOxygenOutput, RatioOxygenOutput2, RatioPollutantInput, RatioPollutantOutput, RatioPollutantOutput2, RatioSteamInput, RatioSteamOutput, RatioSteamOutput2, RatioVolatilesInput, RatioVolatilesOutput, RatioVolatilesOutput2, RatioWaterInput, RatioWaterOutput, RatioWaterOutput2, ReferenceId, RequiredPower, Setting, TemperatureInput, TemperatureOutput, TemperatureOutput2, TotalMolesInput, TotalMolesOutput, TotalMolesOutput2 |
| Flashing Light * | `ThingStructureFlashingLight` | Lock, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Flashlight | `ThingItemFlashlight` | Mode, On, Power, ReferenceId |
| Flip Cover Switch | `ThingModularDeviceFlipCoverSwitch` | NameHash, On, Open, PrefabHash, ReferenceId, Setting |
| Flip Switch | `ThingModularDeviceFlipSwitch` | (none listed) |
| Flood Light (Large) | `ThingStructureGlowLight2` | Activate, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Flood Light (Small) | `ThingStructureGlowLightSmall` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Fridge Big * | `ThingStructureFridgeBig` | Combustion, CombustionOutput, Error, Maximum, NameHash, On, Open, Power, PrefabHash, Pressure, PressureOutput, Ratio, RatioCarbonDioxide, RatioCarbonDioxideOutput, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrogenOutput, RatioNitrousOxide, RatioNitrousOxideOutput, RatioOxygen, RatioOxygenOutput, RatioPollutant, RatioPollutantOutput, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioVolatilesOutput, RatioWater, RatioWaterOutput, ReferenceId, RequiredPower, Setting, Temperature, TemperatureOutput, TotalMoles, TotalMolesOutput |
| Fridge Small | `ThingStructureFridgeSmall` | (none listed) |
| Furnace | `ThingStructureFurnace` | Activate, ClearMemory, Combustion, ExportCount, ImportCount, Lock, Maximum, Mode, NameHash, Open, PrefabHash, Pressure, Ratio, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, Reagents, RecipeHash, ReferenceId, Setting, Temperature, TotalMoles |
| Fuse (100kW) | `ThingStructureCableFuse100k` | NameHash, PrefabHash, ReferenceId |
| Fuse (1kW) | `ThingStructureCableFuse1k` | NameHash, PrefabHash, ReferenceId |
| Fuse (50kW) | `ThingStructureCableFuse50k` | NameHash, PrefabHash, ReferenceId |
| Fuse (5kW) | `ThingStructureCableFuse5k` | NameHash, PrefabHash, ReferenceId |
| Gas Canister (Smart) | `ThingItemGasCanisterSmart` | Mode, Pressure, RatioCarbonDioxide, RatioNitrogen, RatioOxygen, RatioVolatiles, ReferenceId, Temperature, TotalMoles |
| Gas Capsule Tank Small | `ThingStructureCapsuleTankGas` | Combustion, CombustionOutput, Maximum, NameHash, PrefabHash, Pressure, PressureOutput, Ratio, RatioCarbonDioxide, RatioCarbonDioxideOutput, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrogenOutput, RatioNitrousOxide, RatioNitrousOxideOutput, RatioOxygen, RatioOxygenOutput, RatioPollutant, RatioPollutantOutput, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioVolatilesOutput, RatioWater, RatioWaterOutput, ReferenceId, Setting, Temperature, TemperatureOutput, TotalMoles, TotalMolesOutput, Volume, VolumeOfLiquid |
| Gas Fuel Generator | `ThingStructureGasGenerator` | Combustion, Error, Maximum, NameHash, On, Power, PowerGeneration, PrefabHash, Pressure, Ratio, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, RequiredPower, Setting, Temperature, TotalMoles |
| Gas Mask * | `ThingItemGasMask` | Combustion, Flush, Pressure, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, SoundAlert, Temperature, TotalMoles, Volume |
| Gas Mixer | `ThingStructureGasMixer` | Error, Lock, Maximum, NameHash, On, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Gas Sensor * | `ThingStructureGasSensor` | Combustion, NameHash, PrefabHash, Pressure, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, Temperature, TotalMoles, VolumeOfLiquid |
| Gas Tank Mk II | `ThingDynamicGasTankAdvanced` | Mode, Pressure, RatioCarbonDioxide, RatioNitrogen, RatioOxygen, RatioVolatiles, ReferenceId, Temperature, TotalMoles |
| Gas Tank Storage | `ThingStructureGasTankStorage` | NameHash, PrefabHash, Pressure, Quantity, RatioCarbonDioxide, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioVolatiles, RatioWater, ReferenceId, Temperature |
| Gas Umbilical Female * | `ThingStructureGasUmbilicalFemale` | Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| Gas Umbilical Female Side * | `ThingStructureGasUmbilicalFemaleSide` | Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| Gauge 2x2 | `ThingModularDeviceGauge2x2` | (none listed) |
| Glass Door | `ThingStructureGlassDoor` | Idle, Lock, Mode, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Glow Light | `ThingStructureGlowLight` | Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Grow Light | `ThingStructureGrowLight` | (none listed) |
| Growlight Large * | `ThingStructureGrowlightLarge` | Lock, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| H2 Combustor | `ThingH2Combustor` | Activate, Combustion, CombustionInput, CombustionOutput, Error, Lock, Maximum, Mode, NameHash, On, Open, Power, PrefabHash, Pressure, PressureInput, PressureOutput, Ratio, RatioCarbonDioxide, RatioCarbonDioxideInput, RatioCarbonDioxideOutput, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidCarbonDioxideInput, RatioLiquidCarbonDioxideOutput, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrogenInput, RatioLiquidNitrogenOutput, RatioLiquidNitrousOxide, RatioLiquidNitrousOxideInput, RatioLiquidNitrousOxideOutput, RatioLiquidOxygen, RatioLiquidOxygenInput, RatioLiquidOxygenOutput, RatioLiquidPollutant, RatioLiquidPollutantInput, RatioLiquidPollutantOutput, RatioLiquidVolatiles, RatioLiquidVolatilesInput, RatioLiquidVolatilesOutput, RatioNitrogen, RatioNitrogenInput, RatioNitrogenOutput, RatioNitrousOxide, RatioNitrousOxideInput, RatioNitrousOxideOutput, RatioOxygen, RatioOxygenInput, RatioOxygenOutput, RatioPollutant, RatioPollutantInput, RatioPollutantOutput, RatioPollutedWater, RatioSteam, RatioSteamInput, RatioSteamOutput, RatioVolatiles, RatioVolatilesInput, RatioVolatilesOutput, RatioWater, RatioWaterInput, RatioWaterOutput, ReferenceId, RequiredPower, Setting, Temperature, TemperatureInput, TemperatureOutput, TotalMoles, TotalMolesInput, TotalMolesOutput |
| Hard Backpack * | `ThingItemHardBackpack` | ReferenceId |
| Hard Hat * | `ThingItemHardHat` | On, Power, ReferenceId |
| Hard Jetpack * | `ThingItemHardJetpack` | Activate, On, ReferenceId |
| Hard Suit * | `ThingItemHardSuit` | Activate, AirRelease, Combustion, EntityState, Error, Filtration, ForwardX, ForwardY, ForwardZ, Lock, On, Orientation, PositionX, PositionY, PositionZ, Power, Pressure, PressureExternal, PressureSetting, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, Setting, SoundAlert, Temperature, TemperatureExternal, TemperatureSetting, TotalMoles, VelocityMagnitude, VelocityRelativeX, VelocityRelativeY, VelocityRelativeZ, VelocityX, VelocityY, VelocityZ, Volume |
| Hardsuit Helmet * | `ThingItemHardsuitHelmet` | Combustion, Flush, Lock, On, Open, Power, Pressure, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, SoundAlert, Temperature, TotalMoles, Volume |
| Harvie | `ThingStructureHarvie` | NameHash, On, Open, PrefabHash, ReferenceId |
| Hash Display | `ThingCircuitboardHashDisplay` | Mode, ReferenceId, Setting |
| Hydraulic Pipe Bender * | `ThingStructureHydraulicPipeBender` | Activate, ClearMemory, CompletionRatio, Error, ExportCount, ImportCount, Lock, NameHash, On, Open, Power, PrefabHash, Reagents, RecipeHash, ReferenceId, RequiredPower, StackSize |
| Hydroponics Station | `ThingStructureHydroponicsStation` | Combustion, CombustionOutput, Error, Maximum, NameHash, On, Power, PrefabHash, Pressure, PressureOutput, Ratio, RatioCarbonDioxide, RatioCarbonDioxideOutput, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrogenOutput, RatioNitrousOxide, RatioNitrousOxideOutput, RatioOxygen, RatioOxygenOutput, RatioPollutant, RatioPollutantOutput, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioVolatilesOutput, RatioWater, RatioWaterOutput, ReferenceId, RequiredPower, Setting, Temperature, TemperatureOutput, TotalMoles, TotalMolesOutput |
| Hydroponics Tray Data * | `ThingStructureHydroponicsTrayData` | Combustion, NameHash, PrefabHash, Pressure, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, Temperature, TotalMoles |
| IC Housing (Compact) | `ThingStructureCircuitHousingCompact` | Error, LineNumber, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting, StackSize |
| Icarus Helmet * | `ThingItemIcarusHelmet` | Combustion, Flush, Lock, On, Open, Power, Pressure, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, SoundAlert, Temperature, TotalMoles, Volume |
| Ice Crusher * | `ThingStructureIceCrusher` | Activate, ClearMemory, Error, ImportCount, Lock, Maximum, NameHash, On, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Igniter * | `ThingStructureIgniter` | NameHash, On, PrefabHash, ReferenceId |
| Integrated Circuit10 * | `ThingItemIntegratedCircuit10` | LineNumber, ReferenceId |
| Interior Door Glass * | `ThingStructureInteriorDoorGlass` | Idle, Lock, Mode, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Interior Door Padded | `ThingStructureInteriorDoorPadded` | Idle, Lock, Mode, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Interior Door Padded Thin * | `ThingStructureInteriorDoorPaddedThin` | Idle, Lock, Mode, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Interior Door Triangle | `ThingStructureInteriorDoorTriangle` | Idle, Lock, Mode, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Jetpack Basic * | `ThingItemJetpackBasic` | Activate, On, ReferenceId |
| Klaxon Speaker | `ThingStructureKlaxon` | Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, SoundAlert, Volume |
| Label Diode 2 | `ThingModularDeviceLabelDiode2` | Color, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Label Diode 3 | `ThingModularDeviceLabelDiode3` | Color, Mode, On, Power, ReferenceId |
| Labeller * | `ThingItemLabeller` | Error, On, Power, ReferenceId |
| Landingpad Center | `ThingLandingpad_CenterPiece01` | Mode, ReferenceId |
| Landingpad Tank Connector (Liquid) | `ThingLandingpad_LiquidTankConnectorPiece` | Error, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Landingpad_Data Connection Piece * | `ThingLandingpad_DataConnectionPiece` | Activate, Combustion, ContactTypeId, Error, Mode, NameHash, On, Power, PrefabHash, Pressure, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, RequiredPower, Temperature, TotalMoles, Vertical |
| Landingpad_Gas Connector Inward Piece * | `ThingLandingpad_GasConnectorInwardPiece` | Combustion, Error, Maximum, NameHash, On, Power, PrefabHash, Pressure, Ratio, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, RequiredPower, Setting, Temperature, TotalMoles |
| Landingpad_Gas Connector Outward Piece * | `ThingLandingpad_GasConnectorOutwardPiece` | Combustion, Error, Maximum, NameHash, On, Power, PrefabHash, Pressure, Ratio, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, RequiredPower, Setting, Temperature, TotalMoles |
| Landingpad_Gas Tank Connector Piece * | `ThingLandingpad_GasTankConnectorPiece` | Error, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Landingpad_Liquid Connector Inward Piece * | `ThingLandingpad_LiquidConnectorInwardPiece` | Combustion, Error, Maximum, NameHash, On, Power, PrefabHash, Pressure, Ratio, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, RequiredPower, Setting, Temperature, TotalMoles |
| Landingpad_Liquid Connector Outward Piece * | `ThingLandingpad_LiquidConnectorOutwardPiece` | Combustion, Error, Maximum, NameHash, On, Power, PrefabHash, Pressure, Ratio, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, RequiredPower, Setting, Temperature, TotalMoles |
| Landingpad_Threshhold Piece * | `ThingLandingpad_ThreshholdPiece` | NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Laptop | `ThingItemLaptop` | Error, On, PositionX, PositionY, PositionZ, Power, PressureExternal, ReferenceId, TemperatureExternal |
| Large Direct Heat Exchange - Liquid + Liquid | `ThingStructureLargeDirectHeatExchangeLiquidtoLiquid` | Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| Large Direct Heat Exchanger - Gas + Liquid | `ThingStructureLargeDirectHeatExchangeGastoLiquid` | Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| Large Direct Heat Exchanger (Gas-Gas) | `ThingStructureLargeDirectHeatExchangeGastoGas` | (none listed) |
| Large Extendable Radiator | `ThingStructureLargeExtendableRadiator` | NameHash, Open, PrefabHash, ReferenceId |
| Large Hanger Door * | `ThingStructureLargeHangerDoor` | Idle, Lock, Mode, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Large Rocket Gas Fuel Tank | `ThingStructureLargeRocketGasFuelTank` | NameHash, PrefabHash, Pressure, RatioCarbonDioxide, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioVolatiles, RatioWater, ReferenceId, Temperature, TotalMoles |
| Large Rocket Liquid Fuel Tank * | `ThingStructureLargeRocketLiquidFuelTank` | Combustion, CombustionOutput, Maximum, NameHash, PrefabHash, Pressure, PressureOutput, Ratio, RatioCarbonDioxide, RatioCarbonDioxideOutput, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrogenOutput, RatioNitrousOxide, RatioNitrousOxideOutput, RatioOxygen, RatioOxygenOutput, RatioPollutant, RatioPollutantOutput, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioVolatilesOutput, RatioWater, RatioWaterOutput, ReferenceId, Setting, Temperature, TemperatureOutput, TotalMoles, TotalMolesOutput, Volume, VolumeOfLiquid |
| Large Satellite Dish | `ThingStructureLargeSatelliteDish` | Horizontal, NameHash, PrefabHash, ReferenceId, Vertical |
| LArRE Dock | `ThingStructureRoboticArmDock` | Activate, Error, Extended, Idle, NameHash, On, PositionX, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| LArRE Dock (Collector) | `ThingStructureLarreDockCollector` | Activate, Error, Extended, Idle, Mode, NameHash, On, Open, PositionX, Power, PrefabHash, Quantity, Ratio, ReferenceId, RequiredPower, Setting |
| Larre Dock Atmos | `ThingStructureLarreDockAtmos` | NameHash, PrefabHash, ReferenceId |
| Larre Dock Bypass | `ThingStructureLarreDockBypass` | NameHash, PrefabHash, ReferenceId |
| Larre Dock Cargo * | `ThingStructureLarreDockCargo` | Activate, Error, Extended, Idle, NameHash, On, Open, PositionX, Power, PrefabHash, ReferenceId, RequiredPower, Setting, TargetPrefabHash, TargetSlotIndex |
| Larre Dock Hydroponics * | `ThingStructureLarreDockHydroponics` | Activate, Error, Extended, Idle, NameHash, On, Open, PositionX, Power, PrefabHash, ReferenceId, RequiredPower, Setting, TargetPrefabHash, TargetSlotIndex |
| Launch Silo * | `ThingLaunchSilo` | Idle, Lock, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| LED Display 2 | `ThingModularDeviceLEDdisplay2` | Color, Error, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Lever | `ThingStructureLogicSwitch` | Lock, NameHash, Open, PrefabHash, ReferenceId, Setting |
| Lfo Volume * | `ThingDeviceLfoVolume` | Activate, Bpm, Error, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Time |
| Light * | `ThingDynamicLight` | Lock, On, Open, Power, ReferenceId |
| Light Long * | `ThingStructureLightLong` | Lock, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Light Round | `ThingStructureLightRound` | Lock, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Light Round Angled | `ThingStructureLightRoundAngled` | Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Light Round Small * | `ThingStructureLightRoundSmall` | Lock, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Linear Rail Door | `ThingStructureRobotArmDoor` | NameHash, Open, Power, PrefabHash, ReferenceId, RequiredPower |
| Liquid Back Volume Regulator | `ThingStructureBackLiquidPressureRegulator` | Error, Lock, Maximum, NameHash, On, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Liquid Canister (Smart) | `ThingItemLiquidCanisterSmart` | Mode, Pressure, ReferenceId, Temperature, TotalMoles |
| Liquid Capsule Tank Small | `ThingStructureCapsuleTankLiquid` | Maximum, PrefabHash, Pressure, Ratio, RatioCarbonDioxide, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioVolatiles, RatioWater, ReferenceId, Setting, Temperature, TotalMoles, Volume |
| Liquid Drain * | `ThingStructureLiquidDrain` | CombustionOutput, Error, Lock, Maximum, NameHash, On, Power, PrefabHash, PressureOutput, Ratio, RatioCarbonDioxideOutput, RatioNitrogenOutput, RatioNitrousOxideOutput, RatioOxygenOutput, RatioPollutantOutput, RatioVolatilesOutput, RatioWaterOutput, ReferenceId, RequiredPower, Setting, TemperatureOutput, TotalMolesOutput |
| Liquid Pipe Analyzer * | `ThingStructureLiquidPipeAnalyzer` | Combustion, Error, Lock, NameHash, NetworkFault, On, Power, PrefabHash, Pressure, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, RequiredPower, Temperature, TotalMoles, Volume, VolumeOfLiquid |
| Liquid Pipe Heater * | `ThingStructureLiquidPipeHeater` | Error, Lock, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Liquid Pipe Radiator | `ThingStructureLiquidPipeRadiator` | (none listed) |
| Liquid Pressure Regulator * | `ThingStructureLiquidPressureRegulator` | Error, Lock, Maximum, NameHash, On, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Liquid Tank Big * | `ThingStructureLiquidTankBig` | Combustion, CombustionOutput, Maximum, NameHash, PrefabHash, Pressure, PressureOutput, Ratio, RatioCarbonDioxide, RatioCarbonDioxideOutput, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrogenOutput, RatioNitrousOxide, RatioNitrousOxideOutput, RatioOxygen, RatioOxygenOutput, RatioPollutant, RatioPollutantOutput, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioVolatilesOutput, RatioWater, RatioWaterOutput, ReferenceId, Setting, Temperature, TemperatureOutput, TotalMoles, TotalMolesOutput, Volume, VolumeOfLiquid |
| Liquid Tank Big Insulated * | `ThingStructureLiquidTankBigInsulated` | Combustion, CombustionOutput, Maximum, NameHash, PrefabHash, Pressure, PressureOutput, Ratio, RatioCarbonDioxide, RatioCarbonDioxideOutput, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrogenOutput, RatioNitrousOxide, RatioNitrousOxideOutput, RatioOxygen, RatioOxygenOutput, RatioPollutant, RatioPollutantOutput, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioVolatilesOutput, RatioWater, RatioWaterOutput, ReferenceId, Setting, Temperature, TemperatureOutput, TotalMoles, TotalMolesOutput, Volume, VolumeOfLiquid |
| Liquid Tank Small | `ThingStructureLiquidTankSmall` | Combustion, CombustionOutput, Maximum, NameHash, PrefabHash, Pressure, PressureOutput, Ratio, RatioCarbonDioxide, RatioCarbonDioxideOutput, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrogenOutput, RatioNitrousOxide, RatioNitrousOxideOutput, RatioOxygen, RatioOxygenOutput, RatioPollutant, RatioPollutantOutput, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioVolatilesOutput, RatioWater, RatioWaterOutput, ReferenceId, Setting, Temperature, TemperatureOutput, TotalMoles, TotalMolesOutput, Volume, VolumeOfLiquid |
| Liquid Tank Small (Insulated) | `ThingStructureLiquidTankSmallInsulated` | Combustion, CombustionOutput, Maximum, NameHash, PrefabHash, Pressure, PressureOutput, Ratio, RatioCarbonDioxide, RatioCarbonDioxideOutput, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrogenOutput, RatioNitrousOxide, RatioNitrousOxideOutput, RatioOxygen, RatioOxygenOutput, RatioPollutant, RatioPollutantOutput, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioVolatilesOutput, RatioWater, RatioWaterOutput, ReferenceId, Setting, Temperature, TemperatureOutput, TotalMoles, TotalMolesOutput, Volume, VolumeOfLiquid |
| Liquid Tank Storage | `ThingStructureLiquidTankStorage` | (none listed) |
| Liquid Vacuum | `ThingItemLiquidVacuum` | Activate, Error, Mode, On, Power, ReferenceId |
| Liquid Volume Pump * | `ThingStructureLiquidVolumePump` | Error, Lock, Maximum, NameHash, On, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Liquid Wall Cooler | `ThingStructureWaterWallCooler` | CombustionOutput, Error, Lock, Maximum, NameHash, On, Power, PrefabHash, PressureOutput, Ratio, RatioCarbonDioxideOutput, RatioNitrogenOutput, RatioNitrousOxideOutput, RatioOxygenOutput, RatioPollutantOutput, RatioVolatilesOutput, RatioWaterOutput, ReferenceId, RequiredPower, Setting, TemperatureOutput, TotalMolesOutput |
| Loader | `ThingStructurePacker` | Error, Lock, Mode, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower |
| Locker (Small) | `ThingStructureLockerSmall` | Lock, NameHash, Open, PrefabHash, ReferenceId |
| Logic Alarm | `ThingModularDeviceAlarm` | Color, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Logic Batch Reader * | `ThingStructureLogicBatchReader` | Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Logic Batch Writer * | `ThingStructureLogicBatchWriter` | Error, ForceWrite, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Logic Compare | `ThingStructureLogicCompare` | Error, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Logic Dial * | `ThingStructureLogicDial` | Mode, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| Logic Gate | `ThingStructureLogicGate` | Error, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Logic Hash Gen * | `ThingStructureLogicHashGen` | NameHash, PrefabHash, ReferenceId, Setting |
| Logic Math | `ThingStructureLogicMath` | Error, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Logic Math Unary * | `ThingStructureLogicMathUnary` | Error, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Logic Memory | `ThingStructureLogicMemory` | NameHash, PrefabHash, ReferenceId, Setting |
| Logic Min/Max | `ThingStructureLogicMinMax` | Error, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Logic Num Pad | `ThingModularDeviceNumpad` | Color, Mode, NameHash, On, Power, PrefabHash, ReferenceId, Setting |
| Logic Pid Controller * | `ThingStructureLogicPidController` | DerivativeGain, Error, IntegralGain, Maximum, Minimum, NameHash, On, Power, PrefabHash, ProportionalGain, ReferenceId, RequiredPower, Reset, Setpoint, Setting |
| Logic Reader | `ThingStructureLogicReader` | Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Logic Rocket Downlink * | `ThingStructureLogicRocketDownlink` | NameHash, Power, PrefabHash, ReferenceId, RequiredPower |
| Logic Select | `ThingStructureLogicSelect` | Error, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Logic Slider | `ThingModularDeviceSlider` | NameHash, PrefabHash, ReferenceId, Setting |
| Logic Sorter * | `ThingStructureLogicSorter` | ClearMemory, Error, ExportCount, ImportCount, Lock, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, StackSize |
| Logic Step Sequencer | `ThingLogicStepSequencer8` | Activate, Bpm, Error, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Time |
| Logic Switch | `ThingModularDeviceSwitch` | Color, NameHash, On, PrefabHash, ReferenceId, Setting |
| Logic Transmitter | `ThingStructureLogicTransmitter` | Channel, Mode, On, Power, ReferenceId, Setting |
| Logic Uplink | `ThingStructureLogicRocketUplink` | Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Logic Writer | `ThingStructureLogicWriter` | Error, ForceWrite, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Logic Writer Switch | `ThingStructureLogicWriterSwitch` | Activate, Error, ForceWrite, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Manual Floor Hatch | `ThingStructureManualFloorHatch` | Idle, Lock, NameHash, Open, PrefabHash, ReferenceId, Setting |
| Manual Hatch | `ThingStructureManualHatch` | Idle, Lock, Mode, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Medium Convection Radiator | `ThingStructureMediumConvectionRadiator` | Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| Medium Convection Radiator | `ThingStructurePassiveLargeRadiatorGas` | Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| Medium Convection Radiator Liquid * | `ThingStructureMediumConvectionRadiatorLiquid` | Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| Medium Hanger Door * | `ThingStructureMediumHangerDoor` | Idle, Lock, Mode, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Medium Radiator (Gas) | `ThingStructureMediumRadiator` | (none listed) |
| Medium Radiator Liquid | `ThingStructureMediumRadiatorLiquid` | NameHash, PrefabHash, ReferenceId |
| Medium Rocket Gas Fuel Tank * | `ThingStructureMediumRocketGasFuelTank` | Combustion, CombustionOutput, Maximum, NameHash, PrefabHash, Pressure, PressureOutput, Ratio, RatioCarbonDioxide, RatioCarbonDioxideOutput, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrogenOutput, RatioNitrousOxide, RatioNitrousOxideOutput, RatioOxygen, RatioOxygenOutput, RatioPollutant, RatioPollutantOutput, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioVolatilesOutput, RatioWater, RatioWaterOutput, ReferenceId, Setting, Temperature, TemperatureOutput, TotalMoles, TotalMolesOutput, Volume, VolumeOfLiquid |
| Medium Rocket Liquid Fuel Tank * | `ThingStructureMediumRocketLiquidFuelTank` | Combustion, CombustionOutput, Maximum, NameHash, PrefabHash, Pressure, PressureOutput, Ratio, RatioCarbonDioxide, RatioCarbonDioxideOutput, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrogenOutput, RatioNitrousOxide, RatioNitrousOxideOutput, RatioOxygen, RatioOxygenOutput, RatioPollutant, RatioPollutantOutput, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioVolatilesOutput, RatioWater, RatioWaterOutput, ReferenceId, Setting, Temperature, TemperatureOutput, TotalMoles, TotalMolesOutput, Volume, VolumeOfLiquid |
| Medium Satellite Dish | `ThingStructureSatelliteDish` | Activate, BestContactFilter, ContactTypeId, Error, Horizontal, Idle, InterrogationProgress, MinimumWattsToContact, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting, SignalID, SignalStrength, SizeX, SizeZ, StackSize, TargetPadIndex, Vertical, WattsReachingContact |
| Microwave Power Receiver | `ThingStructurePowerTransmitterReceiver` | Charge, Error, Horizontal, Mode, NameHash, On, PositionX, PositionY, PositionZ, Power, PowerActual, PowerPotential, PrefabHash, ReferenceId, RequiredPower, Vertical |
| Microwave Power Transmitter | `ThingStructurePowerTransmitter` | Charge, Error, Horizontal, Mode, NameHash, On, PositionX, PositionY, PositionZ, Power, PowerActual, PowerPotential, PrefabHash, ReferenceId, RequiredPower, Vertical |
| Mining Belt MK II | `ThingItemMiningBeltMKII` | ReferenceId |
| Mining Charge | `ThingItemMiningCharge` | Mode, ReferenceId |
| Mining Drill | `ThingItemMiningDrill` | Activate, Error, Mode, On, Power, ReferenceId |
| Mining Drill (Heavy) | `ThingItemMiningDrillHeavy` | Activate, Error, Mode, On, Power, ReferenceId |
| Mk II Angle Grinder | `ThingItemMKIIAngleGrinder` | Activate, Power, ReferenceId |
| Mk II Arc Welder | `ThingItemMKIIArcWelder` | Activate, Power, ReferenceId |
| Mk II Drill | `ThingItemMKIIDrill` | Activate, Power, ReferenceId |
| Mk II Mining Drill | `ThingItemMKIIMiningDrill` | Activate, Error, Mode, On, Power, ReferenceId |
| Modular Device Big Lever * | `ThingModularDeviceBigLever` | NameHash, Open, PrefabHash, ReferenceId, Setting |
| Modular Device Card Reader * | `ThingModularDeviceCardReader` | Color, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Modular Device Console * | `ThingModularDeviceConsole` | Activate, Color, Error, NameHash, On, Open, Power, PrefabHash, ReferenceId, Setting |
| Modular Device Dial * | `ThingModularDeviceDial` | Mode, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| Modular Device Dial Small * | `ThingModularDeviceDialSmall` | Mode, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| Modular Device Gauge3x3 * | `ThingModularDeviceGauge3x3` | Color, NameHash, PrefabHash, ReferenceId, Setting |
| Modular Device LE Ddisplay3 * | `ThingModularDeviceLEDdisplay3` | Color, Error, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Modular Device Light * | `ThingModularDeviceLight` | Color, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Modular Device Light Large * | `ThingModularDeviceLightLarge` | Color, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Modular Device Meter3x3 * | `ThingModularDeviceMeter3x3` | Color, Maximum, Mode, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| Modular Device Round Button * | `ThingModularDeviceRoundButton` | Activate, Color, NameHash, PrefabHash, ReferenceId, Setting |
| Modular Device Square Button * | `ThingModularDeviceSquareButton` | Activate, Color, NameHash, PrefabHash, ReferenceId, Setting |
| Modular Device Throttle3x2 * | `ThingModularDeviceThrottle3x2` | NameHash, PrefabHash, ReferenceId, Setting |
| Modular Light Small | `ThingModularDeviceLightSmall` | Color, NameHash, On, Power, PrefabHash, ReferenceId |
| Motherboard (IC10 Debugger) | `ThingMotherboardDebugAnalyzer` | ReferenceId |
| Motion Sensor | `ThingStructureMotionSensor` | Activate, NameHash, On, PrefabHash, Quantity, ReferenceId |
| Night Vision Goggles | `ThingItemNVG` | Lock, On, Power, ReferenceId |
| Nitrolyzer | `ThingStructureNitrolyzer` | Activate, Combustion, CombustionInput, CombustionInput2, CombustionOutput, Error, Maximum, Mode, NameHash, On, Open, Power, PrefabHash, Pressure, PressureInput, PressureInput2, PressureOutput, Ratio, RatioCarbonDioxide, RatioCarbonDioxideInput, RatioCarbonDioxideInput2, RatioCarbonDioxideOutput, RatioLiquidNitrogen, RatioLiquidNitrogenInput, RatioLiquidNitrogenInput2, RatioLiquidNitrogenOutput, RatioNitrogen, RatioNitrogenInput, RatioNitrogenInput2, RatioNitrogenOutput, RatioNitrousOxide, RatioNitrousOxideInput, RatioNitrousOxideInput2, RatioNitrousOxideOutput, RatioOxygen, RatioOxygenInput, RatioOxygenInput2, RatioOxygenOutput, RatioPollutant, RatioPollutantInput, RatioPollutantInput2, RatioPollutantOutput, RatioVolatiles, RatioVolatilesInput, RatioVolatilesInput2, RatioVolatilesOutput, RatioWater, RatioWaterInput, RatioWaterInput2, RatioWaterOutput, ReferenceId, RequiredPower, Setting, Temperature, TemperatureInput, TemperatureInput2, TemperatureOutput, TotalMoles, TotalMolesInput, TotalMolesInput2, TotalMolesOutput |
| Occupancy Sensor * | `ThingStructureOccupancySensor` | Activate, NameHash, PrefabHash, Quantity, ReferenceId, StackSize |
| OGRE | `ThingStructureHorizontalAutoMiner` | Activate, ClearMemory, Error, ExportCount, ImportCount, Mode, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower |
| One-Way Valve Lever (Gas) | `ThingStructurePipeOneWayValveLever` | (none listed) |
| Overhead Corner Locker | `ThingStructureOverheadShortCornerLocker` | Lock, NameHash, Open, PrefabHash, ReferenceId |
| Overhead Locker | `ThingStructureOverheadShortLocker` | Lock, NameHash, Open, PrefabHash, ReferenceId |
| Passive Large Radiator Liquid * | `ThingStructurePassiveLargeRadiatorLiquid` | Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| Passive Liquid Drain * | `ThingStructurePassiveLiquidDrain` | NameHash, PrefabHash, ReferenceId |
| Passive Speaker * | `ThingPassiveSpeaker` | NameHash, PrefabHash, ReferenceId, SoundAlert, Volume |
| Pipe Analyzer | `ThingStructurePipeAnalysizer` | Combustion, Error, Lock, NameHash, NetworkFault, On, Power, PrefabHash, Pressure, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, RequiredPower, Temperature, TotalMoles, Volume, VolumeOfLiquid |
| Pipe Convection Radiator | `ThingStructurePipeRadiator` | NameHash, PrefabHash, ReferenceId |
| Pipe Heater * | `ThingStructurePipeHeater` | Error, Lock, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Pipe Igniter * | `ThingStructurePipeIgniter` | Activate, Error, NameHash, Power, PrefabHash, ReferenceId, RequiredPower |
| Pipe Label * | `ThingStructurePipeLabel` | NameHash, PrefabHash, ReferenceId |
| Pipe Liquid One Way Valve Lever * | `ThingStructurePipeLiquidOneWayValveLever` | Maximum, NameHash, On, PrefabHash, Ratio, ReferenceId, Setting |
| Pipe Meter | `ThingStructurePipeMeter` | NameHash, PrefabHash, ReferenceId |
| Pipe Organ | `ThingStructurePipeOrgan` | Mode, On, ReferenceId |
| Pipe Radiator | `ThingStructurePipeRadiatorFlat` | NameHash, PrefabHash, ReferenceId |
| Pipe Radiator Liquid | `ThingStructurePipeRadiatorFlatLiquid` | NameHash, PrefabHash, ReferenceId |
| Plant Genetic Stabilizer | `ThingAppliancePlantGeneticStabilizer` | Activate, Mode, On, Power, ReferenceId |
| Plant Sampler | `ThingItemPlantSampler` | Activate, Mode, On, Power, ReferenceId |
| Pneumatic Mining Drill | `ThingItemMiningDrillPneumatic` | Activate, Error, Mode, On, Power, ReferenceId |
| Portable Air Conditioner | `ThingDynamicAirConditioner` | Mode, On, Power, ReferenceId, Temperature |
| Portable Composter | `ThingPortableComposter` | Activate, Mode, On, Power, ReferenceId |
| Portable Solar Panel | `ThingPortableSolarPanel` | (none listed) |
| Portables Connector | `ThingStructurePortablesConnector` | Maximum, NameHash, Open, PrefabHash, Ratio, ReferenceId, Setting |
| Power Connector | `ThingStructurePowerConnector` | NameHash, Open, PrefabHash, ReferenceId |
| Power Transmitter Omni | `ThingStructurePowerTransmitterOmni` | Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Powered Bench | `ThingStructureBench` | Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Powered Vent * | `ThingStructurePoweredVent` | CombustionOutput, Error, Lock, Mode, NameHash, On, Power, PrefabHash, PressureExternal, PressureOutput, RatioCarbonDioxideOutput, RatioNitrogenOutput, RatioNitrousOxideOutput, RatioOxygenOutput, RatioPollutantOutput, RatioVolatilesOutput, RatioWaterOutput, ReferenceId, RequiredPower, TemperatureOutput, TotalMolesOutput |
| Powered Vent Large | `ThingStructurePoweredVentLarge` | CombustionOutput, Error, Lock, Mode, NameHash, On, Power, PrefabHash, PressureExternal, PressureOutput, RatioCarbonDioxideOutput, RatioNitrogenOutput, RatioNitrousOxideOutput, RatioOxygenOutput, RatioPollutantOutput, RatioVolatilesOutput, RatioWaterOutput, ReferenceId, RequiredPower, TemperatureOutput, TotalMolesOutput |
| Pressurant Valve | `ThingStructurePressurantValve` | Error, Lock, Maximum, NameHash, On, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Pressure Fed Gas Engine | `ThingStructurePressureFedGasEngine` | Combustion, Error, NameHash, On, PassedMoles, Power, PrefabHash, Pressure, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, RequiredPower, Temperature, Throttle, TotalMoles |
| Pressure Fed Gas Engine Heavy * | `ThingStructurePressureFedGasEngineHeavy` | Combustion, Error, NameHash, On, PassedMoles, Power, PrefabHash, Pressure, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, RequiredPower, Temperature, Throttle, TotalMoles |
| Pressure Fed Liquid Engine | `ThingStructurePressureFedLiquidEngine` | Combustion, Error, Maximum, NameHash, On, PassedMoles, Power, PrefabHash, Pressure, Ratio, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, RequiredPower, Setting, Temperature, Throttle, TotalMoles |
| Pressure Fed Liquid Engine Heavy * | `ThingStructurePressureFedLiquidEngineHeavy` | Combustion, Error, Maximum, NameHash, On, PassedMoles, Power, PrefabHash, Pressure, Ratio, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, RequiredPower, Setting, Temperature, Throttle, TotalMoles |
| Pressure Plate Small | `ThingStructurePressurePlateSmall` | Activate, NameHash, On, PrefabHash, Quantity, ReferenceId |
| Pressure Regulator | `ThingStructurePressureRegulator` | CombustionOutput, Error, Lock, Mode, NameHash, On, Power, PrefabHash, PressureOutput, RatioCarbonDioxideOutput, RatioNitrogenOutput, RatioNitrousOxideOutput, RatioOxygenOutput, RatioPollutantOutput, RatioVolatilesOutput, RatioWaterOutput, ReferenceId, RequiredPower, Setting, TemperatureOutput, TotalMolesOutput |
| Proximity Sensor * | `ThingStructureProximitySensor` | Activate, NameHash, PrefabHash, Quantity, ReferenceId, Setting |
| Pumped Gas Engine | `ThingStructureGovernedGasEngine` | Combustion, Error, NameHash, On, PassedMoles, Power, PrefabHash, Pressure, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, RequiredPower, Temperature, Throttle, TotalMoles |
| Pumped Liquid Engine | `ThingStructurePumpedLiquidEngine` | Combustion, Error, Maximum, NameHash, On, PassedMoles, Power, PrefabHash, Pressure, Ratio, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, RequiredPower, Setting, Temperature, Throttle, TotalMoles |
| Purge Valve | `ThingStructurePurgeValve` | Error, Lock, Maximum, NameHash, On, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Reagent Reader | `ThingStructureLogicReagentReader` | (none listed) |
| Recycler | `ThingStructureRecycler` | Activate, ClearMemory, Error, ExportCount, ImportCount, NameHash, On, Power, PrefabHash, Reagents, ReferenceId, RequiredPower |
| Refrigerated Vending Machine | `ThingStructureRefrigeratedVendingMachine` | Activate, ClearMemory, Combustion, Error, ExportCount, ImportCount, Lock, NameHash, On, Power, PrefabHash, Pressure, Quantity, Ratio, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, RequestHash, RequiredPower, Setting, TargetPrefabHash, TargetSlotIndex, Temperature, TotalMoles |
| Remote Detonator | `ThingItemRemoteDetonator` | Activate, Error, Lock, Mode, On, Power, ReferenceId |
| Robot * | `ThingRobot` | Error, ForwardX, ForwardY, ForwardZ, MineablesInQueue, MineablesInVicinity, Mode, On, Orientation, PositionX, PositionY, PositionZ, Power, PressureExternal, ReferenceId, TargetX, TargetY, TargetZ, TemperatureExternal, VelocityMagnitude, VelocityRelativeX, VelocityRelativeY, VelocityRelativeZ, VelocityX, VelocityY, VelocityZ |
| Rocket Avionics | `ThingStructureRocketAvionics` | Acceleration, Altitude, Apex, AutoLand, AutoShutOff, BurnTimeRemaining, Chart, ChartedNavPoints, CurrentCode, Density, DestinationCode, Discover, DryMass, Error, FlightControlRule, Mass, MinedQuantity, Mode, NameHash, NavPoints, On, Power, PrefabHash, Progress, Quantity, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReEntryAltitude, Reagents, ReferenceId, RequiredPower, Richness, Sites, Size, StackSize, Survey, Temperature, Thrust, ThrustToWeight, TimeToDestination, TotalMoles, TotalQuantity, VelocityRelativeY, Weight |
| Rocket Celestial Tracker | `ThingStructureRocketCelestialTracker` | CelestialHash, Error, Horizontal, Index, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, StackSize, Vertical |
| Rocket Circuit Housing | `ThingStructureRocketCircuitHousing` | Error, LineNumber, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting, StackSize |
| Rocket Engine (Tiny) | `ThingStructureRocketEngineTiny` | CombustionOutput, Error, Lock, Maximum, NameHash, On, Power, PrefabHash, PressureOutput, Ratio, RatioCarbonDioxideOutput, RatioNitrogenOutput, RatioNitrousOxideOutput, RatioOxygenOutput, RatioPollutantOutput, RatioVolatilesOutput, RatioWaterOutput, ReferenceId, RequiredPower, Setting, TemperatureOutput, TotalMolesOutput |
| Rocket Gas Collector | `ThingStructureRocketGasCollector` | Combustion, Lock, NameHash, On, Power, PrefabHash, Pressure, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, RequiredPower, Temperature, TotalMoles |
| Rocket Gas Filtration | `ThingStructureRocketFiltrationGas` | CombustionInput, CombustionOutput, CombustionOutput2, Error, Lock, Maximum, Mode, NameHash, On, Power, PrefabHash, PressureInput, PressureOutput, PressureOutput2, Ratio, RatioCarbonDioxideInput, RatioCarbonDioxideOutput, RatioCarbonDioxideOutput2, RatioLiquidCarbonDioxideInput, RatioLiquidCarbonDioxideOutput, RatioLiquidCarbonDioxideOutput2, RatioLiquidNitrogenInput, RatioLiquidNitrogenOutput, RatioLiquidNitrogenOutput2, RatioLiquidNitrousOxideInput, RatioLiquidNitrousOxideOutput, RatioLiquidNitrousOxideOutput2, RatioLiquidOxygenInput, RatioLiquidOxygenOutput, RatioLiquidOxygenOutput2, RatioLiquidPollutantInput, RatioLiquidPollutantOutput, RatioLiquidPollutantOutput2, RatioLiquidVolatilesInput, RatioLiquidVolatilesOutput, RatioLiquidVolatilesOutput2, RatioNitrogenInput, RatioNitrogenOutput, RatioNitrogenOutput2, RatioNitrousOxideInput, RatioNitrousOxideOutput, RatioNitrousOxideOutput2, RatioOxygenInput, RatioOxygenOutput, RatioOxygenOutput2, RatioPollutantInput, RatioPollutantOutput, RatioPollutantOutput2, RatioSteamInput, RatioSteamOutput, RatioSteamOutput2, RatioVolatilesInput, RatioVolatilesOutput, RatioVolatilesOutput2, RatioWaterInput, RatioWaterOutput, RatioWaterOutput2, ReferenceId, RequiredPower, Setting, TemperatureInput, TemperatureOutput, TemperatureOutput2, TotalMolesInput, TotalMolesOutput, TotalMolesOutput2 |
| Rocket Manufactory | `ThingStructureRocketManufactory` | Activate, ClearMemory, CompletionRatio, Error, ExportCount, ImportCount, Lock, NameHash, On, Open, Power, PrefabHash, Reagents, RecipeHash, ReferenceId, RequiredPower, StackSize |
| Rocket Miner | `ThingStructureRocketMiner` | ClearMemory, DrillCondition, Error, ExportCount, ImportCount, Lock, NameHash, On, Power, PrefabHash, Quantity, ReferenceId, RequiredPower |
| Rocket Scanner | `ThingStructureRocketScanner` | Error, Lock, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Rover_Mk I * | `ThingRover_MkI` | On, Power, ReferenceId |
| SDB Hopper | `ThingStructureSDBHopper` | ClearMemory, ImportCount, NameHash, Open, PrefabHash, ReferenceId |
| SDB Hopper Advanced | `ThingStructureSDBHopperAdvanced` | ClearMemory, ImportCount, Lock, NameHash, Open, PrefabHash, ReferenceId |
| SDB Silo | `ThingStructureSDBSilo` | Activate, ClearMemory, Error, ExportCount, ImportCount, Lock, Mode, NameHash, On, Open, Power, PrefabHash, Quantity, ReferenceId, RequiredPower |
| Security Printer | `ThingStructureSecurityPrinter` | Activate, ClearMemory, CompletionRatio, Error, ExportCount, ImportCount, Lock, NameHash, On, Open, Power, PrefabHash, Reagents, RecipeHash, ReferenceId, RequiredPower, StackSize |
| Sensor Lenses | `ThingItemSensorLenses` | On, Power, ReferenceId |
| Shelf Medium | `ThingStructureShelfMedium` | NameHash, Open, PrefabHash, ReferenceId |
| Short Corner Locker | `ThingStructureShortCornerLocker` | Lock, NameHash, Open, PrefabHash, ReferenceId |
| Short Locker | `ThingStructureShortLocker` | Lock, NameHash, Open, PrefabHash, ReferenceId |
| Shower | `ThingStructureShower` | Activate, Maximum, NameHash, Open, PrefabHash, Ratio, ReferenceId, Setting |
| Shower (Powered) | `ThingStructureShowerPowered` | Error, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower |
| Sign 1x1 | `ThingStructureSign1x1` | NameHash, PrefabHash, ReferenceId |
| Sign 2x1 | `ThingStructureSign2x1` | NameHash, PrefabHash, ReferenceId |
| Single Bed | `ThingStructureSingleBed` | NameHash, PrefabHash, ReferenceId |
| Sleeper | `ThingStructureSleeper` | Activate, EntityState, Error, Lock, Maximum, NameHash, On, Open, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Sleeper Left | `ThingStructureSleeperLeft` | Activate, EntityState, Error, Lock, Maximum, Mode, NameHash, On, Open, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Sleeper Right | `ThingStructureSleeperRight` | Activate, EntityState, Error, Lock, Maximum, Mode, NameHash, On, Open, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Sleeper Vertical | `ThingStructureSleeperVertical` | Activate, Error, Lock, Maximum, Mode, Open, Power, Ratio, Setting |
| Slot Reader | `ThingStructureLogicSlotReader` | Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Small Direct Heat Exchanger - Gas + Gas | `ThingStructureSmallDirectHeatExchangeGastoGas` | Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| Small Direct Heat Exchanger - Liquid + Gas | `ThingStructureSmallDirectHeatExchangeLiquidtoGas` | Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| Small Direct Heat Exchanger - Liquid + Liquid | `ThingStructureSmallDirectHeatExchangeLiquidtoLiquid` | Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| Small Satellite Dish | `ThingStructureSmallSatelliteDish` | Activate, BestContactFilter, ContactTypeId, Error, Horizontal, Idle, InterrogationProgress, MinimumWattsToContact, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Setting, SignalID, SignalStrength, SizeX, SizeZ, TargetPadIndex, Vertical, WattsReachingContact |
| Small Tank | `ThingStructureTankSmall` | Combustion, CombustionOutput, Maximum, NameHash, Open, PrefabHash, Pressure, PressureOutput, Ratio, RatioCarbonDioxide, RatioCarbonDioxideOutput, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrogenOutput, RatioNitrousOxide, RatioNitrousOxideOutput, RatioOxygen, RatioOxygenOutput, RatioPollutant, RatioPollutantOutput, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioVolatilesOutput, RatioWater, RatioWaterOutput, ReferenceId, Setting, Temperature, TemperatureOutput, TotalMoles, TotalMolesOutput, Volume, VolumeOfLiquid |
| Solar Panel (Angled) | `ThingStructureSolarPanel45` | Charge, Horizontal, Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Vertical |
| Solar Panel (Flat) | `ThingStructureSolarPanelFlat` | (none listed) |
| Solar Panel (Heavy) | `ThingStructureSolarPanelReinforced` | Charge, Horizontal, Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Vertical |
| Solar Panel * | `ThingStructureSolarPanel` | Charge, Horizontal, Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Vertical |
| Solar Panel Dual * | `ThingStructureSolarPanelDual` | Charge, Horizontal, Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Vertical |
| Solar Panel Dual Reinforced * | `ThingStructureSolarPanelDualReinforced` | Charge, Horizontal, Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Vertical |
| Solar Panel Flat Reinforced * | `ThingStructureSolarPanelFlatReinforced` | Charge, Horizontal, Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Vertical |
| Solar Panel45 Reinforced * | `ThingStructureSolarPanel45Reinforced` | Charge, Horizontal, Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Vertical |
| Solid Fuel Generator | `ThingStructureSolidFuelGenerator` | (none listed) |
| Sorter | `ThingStructureSorter` | ClearMemory, Error, ExportCount, ImportCount, Lock, Mode, NameHash, On, Output, Power, PrefabHash, ReferenceId, RequiredPower |
| Space Helmet | `ThingItemSpaceHelmet` | Lock, Open, Power, Pressure, RatioCarbonDioxide, RatioNitrogen, RatioOxygen, RatioPollutant, RatioVolatiles, RatioWater, Temperature |
| Spacepack | `ThingItemSpacepack` | Activate, On, ReferenceId |
| Spotlight | `ThingStructureSpotlight` | Error, Horizontal, Mode, NameHash, On, PositionX, PositionY, PositionZ, Power, PrefabHash, ReferenceId, RequiredPower, Vertical |
| Stacker | `ThingStructureStacker` | Activate, ClearMemory, Error, ExportCount, ImportCount, Lock, Mode, NameHash, On, Output, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Stacker Reverse * | `ThingStructureStackerReverse` | Activate, ClearMemory, Error, ExportCount, ImportCount, Lock, Mode, NameHash, On, Output, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Station Battery | `ThingStructureBattery` | Charge, Error, Lock, Maximum, Mode, NameHash, On, Power, PowerActual, PowerPotential, PrefabHash, Ratio, ReferenceId |
| Step Unit * | `ThingDeviceStepUnit` | Activate, Error, Mode, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Volume |
| Stirling Engine | `ThingStructureStirlingEngine` | Combustion, EnvironmentEfficiency, Error, Maximum, NameHash, On, Power, PowerGeneration, PrefabHash, Pressure, Quantity, Ratio, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, RequiredPower, Setting, Temperature, TotalMoles, Volume, WorkingGasEfficiency |
| Stop Watch | `ThingStopWatch` | Activate, Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower, Time |
| Storage Locker | `ThingStructureStorageLocker` | NameHash, Open, PrefabHash, ReferenceId |
| StructureConsole 3x3 | `ThingStructureConsole3x3` | Error, On, Open, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Suit HARM * | `ThingItemSuitHARM` | Activate, AirRelease, Combustion, EntityState, Error, Filtration, ForwardX, ForwardY, ForwardZ, Lock, On, Orientation, PositionX, PositionY, PositionZ, Power, Pressure, PressureExternal, PressureSetting, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, Setting, SoundAlert, Temperature, TemperatureExternal, TemperatureSetting, TotalMoles, VelocityMagnitude, VelocityRelativeX, VelocityRelativeY, VelocityRelativeZ, VelocityX, VelocityY, VelocityZ, Volume |
| Suit Helmet HARM * | `ThingItemSuitHelmetHARM` | Combustion, Flush, Lock, On, Open, Power, Pressure, RatioCarbonDioxide, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrousOxide, RatioOxygen, RatioPollutant, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioWater, ReferenceId, SoundAlert, Temperature, TotalMoles, Volume |
| Suit Storage | `ThingStructureSuitStorage` | NameHash, PrefabHash, ReferenceId |
| Suit Storage Frame * | `ThingStructureSuitStorageFrame` | Error, Maximum, NameHash, On, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Suit Storage Locker | `ThingStructureSuitStorageLocker` | (none listed) |
| Super Large Direct Heat Exchange Gas to Gas | `ThingStructureSuperLargeDirectHeatExchangeGastoGas` | NameHash, PrefabHash, ReferenceId |
| Super Large Direct Heat Exchange Gasto Liquid * | `ThingStructureSuperLargeDirectHeatExchangeGastoLiquid` | Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| Super Large Direct Heat Exchange Liquid To Liquid * | `ThingStructureSuperLargeDirectHeatExchangeLiquidToLiquid` | Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| Switch | `ThingStructureLogicSwitch2` | Lock, NameHash, Open, PrefabHash, ReferenceId, Setting |
| Tablet * | `ThingItemTablet` | Error, On, Power, ReferenceId |
| Tank Big * | `ThingStructureTankBig` | Combustion, CombustionOutput, Maximum, NameHash, Open, PrefabHash, Pressure, PressureOutput, Ratio, RatioCarbonDioxide, RatioCarbonDioxideOutput, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrogenOutput, RatioNitrousOxide, RatioNitrousOxideOutput, RatioOxygen, RatioOxygenOutput, RatioPollutant, RatioPollutantOutput, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioVolatilesOutput, RatioWater, RatioWaterOutput, ReferenceId, Setting, Temperature, TemperatureOutput, TotalMoles, TotalMolesOutput, Volume, VolumeOfLiquid |
| Tank Big Insulated * | `ThingStructureTankBigInsulated` | Combustion, CombustionOutput, Maximum, NameHash, Open, PrefabHash, Pressure, PressureOutput, Ratio, RatioCarbonDioxide, RatioCarbonDioxideOutput, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrogenOutput, RatioNitrousOxide, RatioNitrousOxideOutput, RatioOxygen, RatioOxygenOutput, RatioPollutant, RatioPollutantOutput, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioVolatilesOutput, RatioWater, RatioWaterOutput, ReferenceId, Setting, Temperature, TemperatureOutput, TotalMoles, TotalMolesOutput, Volume, VolumeOfLiquid |
| Tank Small Air * | `ThingStructureTankSmallAir` | Combustion, CombustionOutput, Maximum, NameHash, Open, PrefabHash, Pressure, PressureOutput, Ratio, RatioCarbonDioxide, RatioCarbonDioxideOutput, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrogenOutput, RatioNitrousOxide, RatioNitrousOxideOutput, RatioOxygen, RatioOxygenOutput, RatioPollutant, RatioPollutantOutput, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioVolatilesOutput, RatioWater, RatioWaterOutput, ReferenceId, Setting, Temperature, TemperatureOutput, TotalMoles, TotalMolesOutput, Volume, VolumeOfLiquid |
| Tank Small Fuel * | `ThingStructureTankSmallFuel` | Combustion, CombustionOutput, Maximum, NameHash, Open, PrefabHash, Pressure, PressureOutput, Ratio, RatioCarbonDioxide, RatioCarbonDioxideOutput, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrogenOutput, RatioNitrousOxide, RatioNitrousOxideOutput, RatioOxygen, RatioOxygenOutput, RatioPollutant, RatioPollutantOutput, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioVolatilesOutput, RatioWater, RatioWaterOutput, ReferenceId, Setting, Temperature, TemperatureOutput, TotalMoles, TotalMolesOutput, Volume, VolumeOfLiquid |
| Tank Small Insulated * | `ThingStructureTankSmallInsulated` | Combustion, CombustionOutput, Maximum, NameHash, Open, PrefabHash, Pressure, PressureOutput, Ratio, RatioCarbonDioxide, RatioCarbonDioxideOutput, RatioHydrogen, RatioLiquidCarbonDioxide, RatioLiquidHydrogen, RatioLiquidNitrogen, RatioLiquidNitrousOxide, RatioLiquidOxygen, RatioLiquidPollutant, RatioLiquidVolatiles, RatioNitrogen, RatioNitrogenOutput, RatioNitrousOxide, RatioNitrousOxideOutput, RatioOxygen, RatioOxygenOutput, RatioPollutant, RatioPollutantOutput, RatioPollutedWater, RatioSteam, RatioVolatiles, RatioVolatilesOutput, RatioWater, RatioWaterOutput, ReferenceId, Setting, Temperature, TemperatureOutput, TotalMoles, TotalMolesOutput, Volume, VolumeOfLiquid |
| Telescope | `ThingStructureGroundBasedTelescope` | Activate, AlignmentError, CelestialHash, CelestialParentHash, DistanceAu, DistanceKm, Eccentricity, Error, Horizontal, HorizontalRatio, Inclination, Lock, NameHash, On, Open, OrbitPeriod, Power, PrefabHash, ReferenceId, RequiredPower, SemiMajorAxis, TrueAnomaly, Vertical, VerticalRatio |
| Terrain Manipulator | `ThingItemTerrainManipulator` | Activate, Error, Mode, On, Power, ReferenceId |
| ThingStructureCrewUmbilical | `ThingStructureCrewUmbilical` | Error, Lock, Mode, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower |
| Tool Belt MK II | `ThingItemMkIIToolbelt` | ReferenceId |
| Tool Manufactory | `ThingStructureToolManufactory` | Activate, ClearMemory, CompletionRatio, Error, ExportCount, ImportCount, Lock, NameHash, On, Open, Power, PrefabHash, Reagents, RecipeHash, ReferenceId, RequiredPower, StackSize |
| Tracking Beacon | `ThingItemBeacon` | Error, On, Power, ReferenceId |
| Trader Waypoint | `ThingStructureTraderWaypoint` | Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Transformer (Large) | `ThingStructureTransformer` | Error, Lock, Maximum, NameHash, On, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Transformer (Medium) | `ThingStructureTransformerMedium` | Error, Lock, Maximum, NameHash, On, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Transformer Reversed (Small) | `ThingStructureTransformerSmallReversed` | (none listed) |
| Transformer Small (Rocket) | `ThingStructureRocketTransformerSmall` | Error, Lock, Maximum, NameHash, On, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Transformer Small * | `ThingStructureTransformerSmall` | Error, Lock, Maximum, NameHash, On, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Trigger Plate (Large) | `ThingStructurePressurePlateLarge` | NameHash, PrefabHash, ReferenceId, Setting |
| Trigger Plate (Medium) | `ThingStructurePressurePlateMedium` | NameHash, PrefabHash, ReferenceId, Setting |
| Turbo Volume Pump (Liquid) | `ThingStructureLiquidTurboVolumePump` | Error, Lock, Maximum, Mode, NameHash, On, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Turbo Volume Pump * | `ThingStructureTurboVolumePump` | Error, Lock, Maximum, Mode, NameHash, On, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Umbilical (Chute) | `ThingStructureChuteUmbilicalMale` | Error, Lock, Mode, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower |
| Umbilical (Gas) | `ThingStructureGasUmbilical Male` | Error, Lock, Maximum, Mode, NameHash, On, Open, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Umbilical (Gas) | `ThingStructureGasUmbilicalMale` | Error, Lock, Maximum, Mode, NameHash, On, Open, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Umbilical (Liquid) | `ThingStructureLiquidUmbilicalMale` | Error, Lock, Maximum, Mode, NameHash, On, Open, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Umbilical (Power) | `ThingStructurePowerUmbilicalMale` | Error, Lock, Mode, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower |
| Umbilical Socket (Chute) | `ThingStructureChuteUmbilicalFemale` | NameHash, PrefabHash, ReferenceId |
| Umbilical Socket (Liquid) | `ThingStructureLiquidUmbilicalFemale` | Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| Umbilical Socket (Power) | `ThingStructurePowerUmbilicalFemale` | NameHash, PrefabHash, ReferenceId |
| Umbilical Socket Angle (Chute) | `ThingStructureChuteUmbilicalFemaleSide` | NameHash, PrefabHash, ReferenceId |
| Umbilical Socket Angle (Liquid) | `ThingStructureLiquidUmbilicalFemaleSide` | Maximum, NameHash, PrefabHash, Ratio, ReferenceId, Setting |
| Umbilical Socket Angle (Power) | `ThingStructurePowerUmbilicalFemaleSide` | NameHash, PrefabHash, ReferenceId |
| Unloader | `ThingStructureUnloader` | ClearMemory, Error, ExportCount, ImportCount, Lock, Mode, NameHash, On, Output, Power, PrefabHash, ReferenceId, RequiredPower |
| Upright Wind Turbine * | `ThingStructureUprightWindTurbine` | NameHash, PowerGeneration, PrefabHash, ReferenceId |
| Utility Button | `ThingModularDeviceUtilityButton2x2` | Activate, NameHash, PrefabHash, ReferenceId, Setting |
| Valve (Gas) | `ThingStructureValve` | Maximum, NameHash, On, PrefabHash, Ratio, ReferenceId, Setting |
| Valve (Liquid) | `ThingStructureLiquidValve` | Maximum, NameHash, On, PrefabHash, Ratio, ReferenceId, Setting |
| Vending Machine * | `ThingStructureVendingMachine` | Activate, ClearMemory, Error, ExportCount, ImportCount, Lock, NameHash, On, Power, PrefabHash, Quantity, Ratio, ReferenceId, RequestHash, RequiredPower, TargetPrefabHash, TargetSlotIndex |
| Vending Machine Small * | `ThingStructureVendingMachineSmall` | Activate, ClearMemory, Error, ExportCount, ImportCount, Lock, NameHash, On, Power, PrefabHash, Quantity, Ratio, ReferenceId, RequestHash, RequiredPower, TargetPrefabHash, TargetSlotIndex |
| Very Important Button | `ThingStructureVeryImportantButton` | Activate, Error, Lock, NameHash, On, Open, Power, PrefabHash, ReferenceId, RequiredPower, Setting |
| Volume Pump * | `ThingStructureVolumePump` | Error, Lock, Maximum, NameHash, On, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Wall Cooler | `ThingStructureWallCooler` | CombustionOutput, Error, Lock, Maximum, NameHash, On, Power, PrefabHash, PressureOutput, Ratio, RatioCarbonDioxideOutput, RatioNitrogenOutput, RatioNitrousOxideOutput, RatioOxygenOutput, RatioPollutantOutput, RatioVolatilesOutput, RatioWaterOutput, ReferenceId, RequiredPower, Setting, TemperatureOutput, TotalMolesOutput |
| Wall Heater | `ThingStructureWallHeater` | Error, Lock, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Wall Light | `ThingStructureWallLight` | Lock, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Wall Light (Battery) | `ThingStructureWallLightBattery` | Lock, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Wall Light (Long Angled) | `ThingStructureLightLongAngled` | Lock, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Wall Light (Long Wide) | `ThingStructureLightLongWide` | Lock, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Water Bottle Filler Powered * | `ThingStructureWaterBottleFillerPowered` | Activate, Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Water Bottle Filler Powered Bottom * | `ThingStructureWaterBottleFillerPoweredBottom` | Activate, Error, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Water Digital Valve * | `ThingStructureWaterDigitalValve` | Error, Lock, Maximum, NameHash, On, Power, PrefabHash, Ratio, ReferenceId, RequiredPower, Setting |
| Water Pipe Meter * | `ThingStructureWaterPipeMeter` | NameHash, PrefabHash, ReferenceId |
| Water Purifier | `ThingStructureWaterPurifier` | ClearMemory, Error, ImportCount, Lock, NameHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Wear Lamp * | `ThingItemWearLamp` | On, Power, ReferenceId |
| Weather Station * | `ThingStructureWeatherStation` | Activate, Error, Lock, Mode, NameHash, NextWeatherEventTime, NextWeatherHash, On, Power, PrefabHash, ReferenceId, RequiredPower |
| Wind Turbine | `ThingStructureWindTurbine` | NameHash, PowerGeneration, PrefabHash, ReferenceId |
| Wireless Battery Cell Extra Large | `ThingItemWirelessBatteryCellExtraLarge` | Mode, ReferenceId |
