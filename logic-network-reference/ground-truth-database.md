# Ground-Truth Database — decompiled LogicType behavior, every class that overrides it

**What this is**: every class in `Assembly-CSharp.dll` (2026-08-06
snapshot) that overrides `GetLogicValue`, `SetLogicValue`,
`CanLogicRead`, or `CanLogicWrite` beyond
[`base-behavior.md`](base-behavior.md)'s shared `DynamicThing`
implementation — **120 classes**, extracted programmatically (not
hand-written per class) by scanning the fully decompiled source for
these four method overrides and pulling out each `LogicType` case's
return expression. Cross-checked against this branch's two hand-
written entries (`devices/power-controller.md`'s `AreaPowerControl`,
confirmed byte-for-byte identical to what manual reading found
earlier) — the automated extraction is reliable, not a rough
approximation.

**What "ground truth" means here, precisely**: the short expression
next to each `LogicType` is the actual C# code (or a close paraphrase
of it, truncated past ~180 characters) that runs when that LogicType
is read or written for that class — not a description, not an
inference, the real decompiled logic. Kept short and functional (a
property reference, a ternary, a short calculation) rather than
reproducing full method bodies — consistent with how `devices/*.md`'s
hand-written entries already quote short illustrative fragments, not
whole files (see `README.md`'s "never commit decompiled source itself"
rule — this file follows the same principle, extracted facts and short
functional fragments, not the original source structure).

## How to use this

- **Ctrl+F / search for a class name** (e.g. `AreaPowerControl`,
  `Door`, `GasSensor`) to jump to its entry. Entries are sorted
  alphabetically by C# class name, not in-game display name — cross-
  reference against [`device-index.md`](device-index.md) if you only
  know the in-game name (that file's `deviceKey` column is the prefab
  identifier, which is usually but not always a close match to the
  actual implementing class name below).
- **A class not appearing here** means it doesn't override any of
  these four methods — it only exposes `base-behavior.md`'s shared set
  (`On`, `Open`, `Lock`, `Mode`, `Color`, `Activate`, `Power`, `Error`,
  atmosphere/gas ratios, `PrefabHash`, `ReferenceId`). Confirmed for
  `Door` specifically (see `devices/door.md`) — the absence is a real
  finding, not a gap in the scan.
- **A method listed with no table under it** (e.g. `CanLogicRead`
  showing up with nothing inside) means the override exists but this
  extraction's case-by-case parsing didn't find matches inside it —
  usually because that specific method uses `if`/range-check logic
  instead of a `switch`/`case` structure (`AreaPowerControl`'s real
  `CanLogicRead` is exactly this shape — see `devices/power-controller.md`
  for that one worked out by hand). Treat these as "known override
  exists, not yet extracted" rather than "nothing here."
- **Minor extraction noise**: some expressions retain a leading
  `return` keyword or other small syntactic leftovers from how the
  source was written — cosmetic, not a correctness issue. Read past it
  rather than treating it as part of the "real" expression.

## Relationship to the rest of this branch

This is the **bulk, automated layer** — broad, fast, machine-generated,
covers every class with a real override in one pass. `devices/*.md`
remains the **hand-verified, explained layer** — a small number of
devices this project actually cares about (Power Controller, Door,
Motherboards so far), written in prose with context, caveats, and
cross-references to where this project got burned trusting the wrong
thing. Use this file to find out *what* a class does; use `devices/*.md`
(or write a new entry there, following its own worked examples) when
you need *why it matters* or a careful explanation suitable for
eventually updating the Community Wiki.

### `ActiveVent`

**File**: `Assets.Scripts.Objects.Pipes/ActiveVent.cs` | **Extends**: `SmallDeviceOutput`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `PressureExternal` | `ExternalPressure.ToDouble()` |
| `PressureInternal` | `InternalPressure.ToDouble()` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `PressureExternal` | `ExternalPressure = new PressurekPa(value)` |
| `PressureInternal` | `InternalPressure = new PressurekPa(value)` |

---

### `AdvancedComposter`

**File**: `Objects.Electrical/AdvancedComposter.cs` | **Extends**: `DeviceInputOutputImportExport`

---

### `AdvancedFurnace`

**File**: `Assets.Scripts.Objects.Pipes/AdvancedFurnace.cs` | **Extends**: `FurnaceBase`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `SettingInput` | `OutputSetting2` |
| `SettingOutput` | `base.OutputSetting` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `SettingInput` | `OutputSetting2 = (float)value` |
| `SettingOutput` | `base.OutputSetting = (float)value` |

---

### `AdvancedSuit`

**File**: `Assets.Scripts.Objects.Clothing/AdvancedSuit.cs` | **Extends**: `Suit`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `AirRelease` | `return Importing` |
| `EntityState` | `if ((object)base.ParentSlot?.Parent == null \|\| base.ParentSlot.Type != Slot.Class.Suit) { return -1.0; } return ((int?)base.ParentEntity?.State) ?? (-1)` |
| `Filtration` | `return Exporting` |
| `ForwardX` | `return Forward.x` |
| `ForwardY` | `return Forward.y` |
| `ForwardZ` | `return Forward.z` |
| `Orientation` | `return Orientation` |
| `PositionX` | `return base.Position.x` |
| `PositionY` | `return base.Position.y` |
| `PositionZ` | `return base.Position.z` |
| `PressureExternal` | `return base.WorldAtmosphere?.PressureGassesAndLiquids.ToDouble() ?? 0.0` |
| `PressureSetting` | `return base.OutputSetting` |
| `Setting` | `return Setting` |
| `SoundAlert` | `return (int)SoundAlert` |
| `TemperatureExternal` | `return base.WorldAtmosphere?.Temperature.ToDouble() ?? 0.0` |
| `TemperatureSetting` | `return base.OutputTemperature.ToDouble()` |
| `VelocityMagnitude` | `return base.VelocityMagnitude` |
| `VelocityRelativeX` | `return RelativeVelocity.x` |
| `VelocityRelativeY` | `return RelativeVelocity.y` |
| `VelocityRelativeZ` | `return RelativeVelocity.z` |
| `VelocityX` | `return base.Velocity.x` |
| `VelocityY` | `return base.Velocity.y` |
| `VelocityZ` | `return base.Velocity.z` |
| `Volume` | `return (int)SoundVolume` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `AirRelease` | `OnServer.Interact(base.InteractImport, (int)value)` |
| `Error` | `OnServer.Interact(base.InteractError, (int)value)` |
| `Filtration` | `OnServer.Interact(base.InteractExport, (int)value)` |
| `PressureSetting` | `base.OutputSetting = (float)value` |
| `Setting` | `Setting = value` |
| `SoundAlert` | `SoundAlert = (byte)Mathf.Clamp((int)value, 0, EnumCollections.SpeakerSounds.Length - 1)` |
| `TemperatureSetting` | `base.OutputTemperature = new TemperatureKelvin(value)` |
| `Volume` | `SoundVolume = (byte)Mathf.Clamp((int)value, 1, 100)` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `AirRelease` | `return true` |
| `EntityState` | `return true` |
| `Filtration` | `return true` |
| `ForwardX` | `return true` |
| `ForwardY` | `return true` |
| `ForwardZ` | `return true` |
| `Orientation` | `return true` |
| `PositionX` | `return true` |
| `PositionY` | `return true` |
| `PositionZ` | `return true` |
| `PressureExternal` | `return true` |
| `PressureSetting` | `return true` |
| `Setting` | `return true` |
| `SoundAlert` | `return true` |
| `TemperatureExternal` | `return true` |
| `TemperatureSetting` | `return true` |
| `VelocityMagnitude` | `return true` |
| `VelocityRelativeX` | `return true` |
| `VelocityRelativeY` | `return true` |
| `VelocityRelativeZ` | `return true` |
| `VelocityX` | `return true` |
| `VelocityY` | `return true` |
| `VelocityZ` | `return true` |
| `Volume` | `return true` |

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `AirRelease` | `return true` |
| `Error` | `return true` |
| `Filtration` | `return true` |
| `PressureSetting` | `return true` |
| `Setting` | `return true` |
| `SoundAlert` | `return true` |
| `TemperatureSetting` | `return true` |
| `Volume` | `return true` |

---

### `AdvancedTablet`

**File**: `Assets.Scripts.Objects.Items/AdvancedTablet.cs` | **Extends**: `Tablet`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `SoundAlert` | `(int)SoundAlert` |
| `Volume` | `(int)SoundVolume` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `SoundAlert` | `SoundAlert = (byte)Mathf.Clamp((int)value, 0, EnumCollections.SpeakerSounds.Length - 1)` |
| `Volume` | `SoundVolume = (byte)Mathf.Clamp((int)value, 0, 100)` |

---

### `AirConditioner`

**File**: `Assets.Scripts.Objects.Electrical/AirConditioner.cs` | **Extends**: `DeviceInputOutputCircuit`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `OperationalTemperatureEfficiency` | `OperationalTemperatureLimitor` |
| `PressureEfficiency` | `OptimalPressureScalar` |
| `TemperatureDifferentialEfficiency` | `TemperatureDifferentialEfficiency` |

---

### `ArcFurnace`

**File**: `Assets.Scripts.Objects.Pipes/ArcFurnace.cs` | **Extends**: `DeviceImportExport`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Idle` | `(!base.IsDeviceActive) ? 1 : 0` |
| `RecipeHash` | `_smelterResult?.GetPrefabHash() ?? 0` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Idle` | `true` |
| `RecipeHash` | `true` |

---

### `AreaPowerControl`

**File**: `Assets.Scripts.Objects.Electrical/AreaPowerControl.cs` | **Extends**: `ElectricalInputOutput`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Charge` | `AvailablePower` |
| `Maximum` | `Battery ? Battery.PowerMaximum : 0f` |
| `PowerActual` | `base.CurrentLoad` |
| `PowerPotential` | `base.PotentialLoad` |
| `Ratio` | `Battery ? (Battery.PowerStored / Battery.PowerMaximum) : 0f` |

---

### `AudioSequencer`

**File**: `Objects.Electrical/AudioSequencer.cs` | **Extends**: `LogicUnitBase`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Bpm` | `Bpm` |
| `Time` | `Setting` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Bpm` | `Bpm = (int)Mathf.Clamp((float)value, 15f, 180f)` |
| `Time` | `Setting = value` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Bpm` | `true` |
| `Time` | `true` |

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Bpm` | `true` |
| `Time` | `true` |

---

### `BasketHoop`

**File**: `Assets.Scripts.Objects.Electrical/BasketHoop.cs` | **Extends**: `Device`

---

### `Battery`

**File**: `Assets.Scripts.Objects.Electrical/Battery.cs` | **Extends**: `ElectricalInputOutput`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Charge` | `AvailablePower` |
| `Maximum` | `PowerMaximum` |
| `PowerActual` | `base.CurrentLoad` |
| `PowerPotential` | `base.PotentialLoad` |
| `Ratio` | `PowerStored / PowerMaximum` |

---

### `CableAnalyser`

**File**: `Assets.Scripts.Objects.Electrical/CableAnalyser.cs` | **Extends**: `DeviceCableMounted`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `PowerActual` | `CurrentLoad` |
| `PowerPotential` | `PotentialLoad` |
| `PowerRequired` | `RequiredLoad` |

---

### `ChuteDigitalFlipFlopSplitter`

**File**: `Objects.Pipes/ChuteDigitalFlipFlopSplitter.cs` | **Extends**: `ChuteDevice`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Quantity` | `Quantity` |
| `Setting` | `Setting` |
| `SettingOutput` | `Setting2` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Quantity` | `Quantity = (int)value` |
| `Setting` | `Setting = (int)value` |
| `SettingOutput` | `Setting2 = (int)value` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Quantity` | `true` |
| `Setting` | `true` |
| `SettingOutput` | `true` |

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Quantity` | `true` |
| `Setting` | `true` |
| `SettingOutput` | `true` |

---

### `ChuteDigitalValve`

**File**: `Objects.Pipes/ChuteDigitalValve.cs` | **Extends**: `ChuteDevice`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Quantity` | `Quantity` |
| `Setting` | `Setting` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Quantity` | `Quantity = (int)value` |
| `Setting` | `Setting = (int)value` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Quantity` | `true` |
| `Setting` | `true` |

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Quantity` | `true` |
| `Setting` | `true` |

---

### `CircuitHousing`

**File**: `Assets.Scripts.Objects.Electrical/CircuitHousing.cs` | **Extends**: `LogicUnitBase`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `LineNumber` | `if (!(ProgrammableChip != null)) { return -1.0; } return ProgrammableChip.LineNumber` |
| `Setting` | `return Setting` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `LineNumber` | `if (ProgrammableChip != null) { ProgrammableChip.LineNumber = (uint)value; } break` |
| `Setting` | `Setting = value` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `LineNumber` | `true` |
| `Setting` | `true` |

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `LineNumber` | `true` |
| `Setting` | `true` |

---

### `CombustionCentrifuge`

**File**: `Assets.Scripts.Objects.Pipes/CombustionCentrifuge.cs` | **Extends**: `DeviceInputOutputImportExportCircuit`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `CombustionLimiter` | `_internalCombustion.CombustionLimiter` |
| `Rpm` | `_internalCombustion.Rpm` |
| `Stress` | `_internalCombustion.Stress` |
| `Throttle` | `_internalCombustion.Throttle` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `CombustionLimiter` | `_internalCombustion.CombustionLimiter = (float)value` |
| `Throttle` | `_internalCombustion.Throttle = (float)value` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `CombustionLimiter` | `return true` |
| `Maximum` | `return false` |
| `Ratio` | `return false` |
| `Rpm` | `return true` |
| `Setting` | `return false` |
| `Stress` | `return true` |
| `Throttle` | `return true` |

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `CombustionLimiter` | `true` |
| `Setting` | `false` |
| `Throttle` | `true` |

---

### `CombustionDeepMiner`

**File**: `Assets.Scripts.Objects.Pipes/CombustionDeepMiner.cs` | **Extends**: `DeepMiner`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `CombustionLimiter` | `_internalCombustion.CombustionLimiter` |
| `Rpm` | `_internalCombustion.Rpm` |
| `Stress` | `_internalCombustion.Stress` |
| `Throttle` | `_internalCombustion.Throttle` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `CombustionLimiter` | `_internalCombustion.CombustionLimiter = (float)value` |
| `Throttle` | `_internalCombustion.Throttle = (float)value` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `CombustionLimiter` | `return true` |
| `Maximum` | `return false` |
| `Ratio` | `return false` |
| `Rpm` | `return true` |
| `Setting` | `return false` |
| `Stress` | `return true` |
| `Throttle` | `return true` |

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `CombustionLimiter` | `true` |
| `Setting` | `false` |
| `Throttle` | `true` |

---

### `Console`

**File**: `Assets.Scripts.Objects.Electrical/Console.cs` | **Extends**: `Computer`

---

### `CryoTube`

**File**: `Assets.Scripts.Objects.Pipes/CryoTube.cs` | **Extends**: `OccupantAtmospherics`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Pressure` | `InternalPressure.ToDouble()` |
| `Temperature` | `InternalTemperature.ToDouble()` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Pressure` | `true` |
| `Temperature` | `true` |

---

### `DaylightSensor`

**File**: `Assets.Scripts.Objects.Electrical/DaylightSensor.cs` | **Extends**: `Sensor`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Activate` | `HasLight ? 1 : 0` |
| `Horizontal` | `_horizontal` |
| `SolarAngle` | `SolarAngle` |
| `SolarIrradiance` | `LocalSolarIrradiance` |
| `Vertical` | `_vertical` |

---

### `DeviceAtmospherics`

**File**: `Assets.Scripts.Objects.Pipes/DeviceAtmospherics.cs` | **Extends**: `Device`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Maximum` | `MaxSetting` |
| `Ratio` | `OutputSetting / MaxSetting` |
| `Setting` | `OutputSetting` |

---

### `DeviceImport`

**File**: `Assets.Scripts.Objects.Pipes/DeviceImport.cs` | **Extends**: `Device`

---

### `DeviceImportExport`

**File**: `Assets.Scripts.Objects.Pipes/DeviceImportExport.cs` | **Extends**: `DeviceImport`

---

### `DeviceInput`

**File**: `Assets.Scripts.Objects.Pipes/DeviceInput.cs` | **Extends**: `DeviceAtmospherics`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `CombustionInput` | `return ConnectedPipeNetwork.Atmosphere.Inflamed ? 1 : 0` |
| `PressureInput` | `return ConnectedPipeNetwork.Atmosphere.PressureGassesAndLiquids.ToDouble()` |
| `RatioCarbonDioxideInput` | `return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioLiquidMethaneInput` | `return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioLiquidNitrogenInput` | `return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioLiquidOxygenInput` | `return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioMethaneInput` | `return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioNitrogenInput` | `return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioNitrousOxideInput` | `return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioOxygenInput` | `return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioPollutantInput` | `return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioSteamInput` | `return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioWaterInput` | `return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `TemperatureInput` | `return ConnectedPipeNetwork.Atmosphere.Temperature.ToDouble()` |
| `TotalMolesInput` | `return ConnectedPipeNetwork.Atmosphere.TotalMoles.ToDouble()` |

---

### `DeviceInputOutput`

**File**: `Assets.Scripts.Objects.Pipes/DeviceInputOutput.cs` | **Extends**: `DeviceAtmospherics`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `CombustionInput` | `return (InputNetwork?.Atmosphere != null) ? (InputNetwork.Atmosphere.Inflamed ? 1 : 0) : 0` |
| `CombustionInput2` | `return (InputNetwork2?.Atmosphere != null) ? (InputNetwork2.Atmosphere.Inflamed ? 1 : 0) : 0` |
| `CombustionOutput` | `return (OutputNetwork?.Atmosphere != null) ? (OutputNetwork.Atmosphere.Inflamed ? 1 : 0) : 0` |
| `CombustionOutput2` | `return (OutputNetwork2?.Atmosphere != null) ? (OutputNetwork2.Atmosphere.Inflamed ? 1 : 0) : 0` |
| `PressureInput` | `return (InputNetwork?.Atmosphere?.PressureGassesAndLiquids.ToDouble()).GetValueOrDefault()` |
| `PressureInput2` | `return (InputNetwork2?.Atmosphere?.PressureGassesAndLiquids.ToDouble()).GetValueOrDefault()` |
| `PressureOutput` | `return (OutputNetwork?.Atmosphere?.PressureGassesAndLiquids.ToDouble()).GetValueOrDefault()` |
| `PressureOutput2` | `return (OutputNetwork2?.Atmosphere?.PressureGassesAndLiquids.ToDouble()).GetValueOrDefault()` |
| `RatioCarbonDioxideInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioCarbonDioxideInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioCarbonDioxideOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioCarbonDioxideOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioHeliumInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioHeliumInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioHeliumOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioHeliumOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioHydrazineInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioHydrazineInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioHydrazineOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioHydrazineOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioHydrochloricAcidInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioHydrochloricAcidInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioHydrochloricAcidOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioHydrochloricAcidOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioHydrogenInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioHydrogenInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioHydrogenOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioHydrogenOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioLiquidAlcoholInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioLiquidAlcoholInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioLiquidAlcoholOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioLiquidAlcoholOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioLiquidCarbonDioxideInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioLiquidCarbonDioxideInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioLiquidCarbonDioxideOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioLiquidCarbonDioxideOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioLiquidHydrazineInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioLiquidHydrazineInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioLiquidHydrazineOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioLiquidHydrazineOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioLiquidHydrochloricAcidInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioLiquidHydrochloricAcidInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioLiquidHydrochloricAcidOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioLiquidHydrochloricAcidOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioLiquidHydrogenInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioLiquidHydrogenInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioLiquidHydrogenOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioLiquidHydrogenOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioLiquidMethaneInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioLiquidMethaneInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioLiquidMethaneOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioLiquidMethaneOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioLiquidNitrogenInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioLiquidNitrogenInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioLiquidNitrogenOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioLiquidNitrogenOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioLiquidNitrousOxideInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioLiquidNitrousOxideInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioLiquidNitrousOxideOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioLiquidNitrousOxideOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioLiquidOxygenInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioLiquidOxygenInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioLiquidOxygenOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioLiquidOxygenOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioLiquidOzoneInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioLiquidOzoneInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioLiquidOzoneOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioLiquidOzoneOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioLiquidPollutantInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioLiquidPollutantInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioLiquidPollutantOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioLiquidPollutantOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioLiquidSilanolInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioLiquidSilanolInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioLiquidSilanolOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioLiquidSilanolOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioLiquidSodiumChlorideInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioLiquidSodiumChlorideInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioLiquidSodiumChlorideOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioLiquidSodiumChlorideOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioMethaneInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioMethaneInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioMethaneOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioMethaneOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioNitrogenInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioNitrogenInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioNitrogenOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioNitrogenOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioNitrousOxideInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioNitrousOxideInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioNitrousOxideOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioNitrousOxideOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioOxygenInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioOxygenInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioOxygenOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioOxygenOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioOzoneInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioOzoneInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioOzoneOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioOzoneOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioPollutantInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioPollutantInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioPollutantOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioPollutantOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioPollutedWaterInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioPollutedWaterInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioPollutedWaterOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioPollutedWaterOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioSilanolInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioSilanolInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioSilanolOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioSilanolOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioSteamInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioSteamInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioSteamOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioSteamOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `RatioWaterInput` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere)` |
| `RatioWaterInput2` | `return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere)` |
| `RatioWaterOutput` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere)` |
| `RatioWaterOutput2` | `return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere)` |
| `TemperatureInput` | `return (InputNetwork?.Atmosphere?.Temperature.ToDouble()).GetValueOrDefault()` |
| `TemperatureInput2` | `return (InputNetwork2?.Atmosphere?.Temperature.ToDouble()).GetValueOrDefault()` |
| `TemperatureOutput` | `return (OutputNetwork?.Atmosphere?.Temperature.ToDouble()).GetValueOrDefault()` |
| `TemperatureOutput2` | `return (OutputNetwork2?.Atmosphere?.Temperature.ToDouble()).GetValueOrDefault()` |
| `TotalMolesInput` | `return (InputNetwork?.Atmosphere?.TotalMoles.ToDouble()).GetValueOrDefault()` |
| `TotalMolesInput2` | `return (InputNetwork2?.Atmosphere?.TotalMoles.ToDouble()).GetValueOrDefault()` |
| `TotalMolesOutput` | `return (OutputNetwork?.Atmosphere?.TotalMoles.ToDouble()).GetValueOrDefault()` |
| `TotalMolesOutput2` | `return (OutputNetwork2?.Atmosphere?.TotalMoles.ToDouble()).GetValueOrDefault()` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `CombustionInput` | `return InputConnection?.IsValid ?? false` |
| `CombustionInput2` | `return InputConnection2?.IsValid ?? false` |
| `CombustionOutput` | `return OutputConnection?.IsValid ?? false` |
| `CombustionOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `PressureInput` | `return InputConnection?.IsValid ?? false` |
| `PressureInput2` | `return InputConnection2?.IsValid ?? false` |
| `PressureOutput` | `return OutputConnection?.IsValid ?? false` |
| `PressureOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioCarbonDioxideInput` | `return InputConnection?.IsValid ?? false` |
| `RatioCarbonDioxideInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioCarbonDioxideOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioCarbonDioxideOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioHeliumInput` | `return InputConnection?.IsValid ?? false` |
| `RatioHeliumInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioHeliumOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioHeliumOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioHydrazineInput` | `return InputConnection?.IsValid ?? false` |
| `RatioHydrazineInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioHydrazineOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioHydrazineOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioHydrochloricAcidInput` | `return InputConnection?.IsValid ?? false` |
| `RatioHydrochloricAcidInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioHydrochloricAcidOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioHydrochloricAcidOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioHydrogenInput` | `return InputConnection?.IsValid ?? false` |
| `RatioHydrogenInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioHydrogenOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioHydrogenOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioLiquidAlcoholInput` | `return InputConnection?.IsValid ?? false` |
| `RatioLiquidAlcoholInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioLiquidAlcoholOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioLiquidAlcoholOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioLiquidCarbonDioxideInput` | `return InputConnection?.IsValid ?? false` |
| `RatioLiquidCarbonDioxideInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioLiquidCarbonDioxideOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioLiquidCarbonDioxideOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioLiquidHydrazineInput` | `return InputConnection?.IsValid ?? false` |
| `RatioLiquidHydrazineInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioLiquidHydrazineOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioLiquidHydrazineOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioLiquidHydrochloricAcidInput` | `return InputConnection?.IsValid ?? false` |
| `RatioLiquidHydrochloricAcidInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioLiquidHydrochloricAcidOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioLiquidHydrochloricAcidOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioLiquidHydrogenInput` | `return InputConnection?.IsValid ?? false` |
| `RatioLiquidHydrogenInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioLiquidHydrogenOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioLiquidHydrogenOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioLiquidMethaneInput` | `return InputConnection?.IsValid ?? false` |
| `RatioLiquidMethaneInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioLiquidMethaneOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioLiquidMethaneOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioLiquidNitrogenInput` | `return InputConnection?.IsValid ?? false` |
| `RatioLiquidNitrogenInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioLiquidNitrogenOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioLiquidNitrogenOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioLiquidNitrousOxideInput` | `return InputConnection?.IsValid ?? false` |
| `RatioLiquidNitrousOxideInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioLiquidNitrousOxideOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioLiquidNitrousOxideOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioLiquidOxygenInput` | `return InputConnection?.IsValid ?? false` |
| `RatioLiquidOxygenInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioLiquidOxygenOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioLiquidOxygenOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioLiquidOzoneInput` | `return InputConnection?.IsValid ?? false` |
| `RatioLiquidOzoneInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioLiquidOzoneOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioLiquidOzoneOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioLiquidPollutantInput` | `return InputConnection?.IsValid ?? false` |
| `RatioLiquidPollutantInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioLiquidPollutantOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioLiquidPollutantOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioLiquidSilanolInput` | `return InputConnection?.IsValid ?? false` |
| `RatioLiquidSilanolInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioLiquidSilanolOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioLiquidSilanolOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioLiquidSodiumChlorideInput` | `return InputConnection?.IsValid ?? false` |
| `RatioLiquidSodiumChlorideInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioLiquidSodiumChlorideOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioLiquidSodiumChlorideOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioMethaneInput` | `return InputConnection?.IsValid ?? false` |
| `RatioMethaneInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioMethaneOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioMethaneOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioNitrogenInput` | `return InputConnection?.IsValid ?? false` |
| `RatioNitrogenInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioNitrogenOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioNitrogenOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioNitrousOxideInput` | `return InputConnection?.IsValid ?? false` |
| `RatioNitrousOxideInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioNitrousOxideOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioNitrousOxideOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioOxygenInput` | `return InputConnection?.IsValid ?? false` |
| `RatioOxygenInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioOxygenOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioOxygenOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioOzoneInput` | `return InputConnection?.IsValid ?? false` |
| `RatioOzoneInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioOzoneOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioOzoneOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioPollutantInput` | `return InputConnection?.IsValid ?? false` |
| `RatioPollutantInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioPollutantOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioPollutantOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioPollutedWaterInput` | `return InputConnection?.IsValid ?? false` |
| `RatioPollutedWaterInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioPollutedWaterOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioPollutedWaterOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioSilanolInput` | `return InputConnection?.IsValid ?? false` |
| `RatioSilanolInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioSilanolOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioSilanolOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioSteamInput` | `return InputConnection?.IsValid ?? false` |
| `RatioSteamInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioSteamOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioSteamOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `RatioWaterInput` | `return InputConnection?.IsValid ?? false` |
| `RatioWaterInput2` | `return InputConnection2?.IsValid ?? false` |
| `RatioWaterOutput` | `return OutputConnection?.IsValid ?? false` |
| `RatioWaterOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `TemperatureInput` | `return InputConnection?.IsValid ?? false` |
| `TemperatureInput2` | `return InputConnection2?.IsValid ?? false` |
| `TemperatureOutput` | `return OutputConnection?.IsValid ?? false` |
| `TemperatureOutput2` | `return OutputConnection2?.IsValid ?? false` |
| `TotalMolesInput` | `return InputConnection?.IsValid ?? false` |
| `TotalMolesInput2` | `return InputConnection2?.IsValid ?? false` |
| `TotalMolesOutput` | `return OutputConnection?.IsValid ?? false` |
| `TotalMolesOutput2` | `return OutputConnection2?.IsValid ?? false` |

---

### `DeviceInputOutputImport`

**File**: `Assets.Scripts.Objects.Pipes/DeviceInputOutputImport.cs` | **Extends**: `DeviceInputOutput`

---

### `DeviceInputOutputImportExport`

**File**: `Assets.Scripts.Objects.Pipes/DeviceInputOutputImportExport.cs` | **Extends**: `DeviceInputOutputImport`

---

### `DeviceOutput`

**File**: `Assets.Scripts.Objects.Pipes/DeviceOutput.cs` | **Extends**: `DeviceAtmospherics`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `CombustionOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return ConnectedPipeNetwork.Atmosphere.Inflamed ? 1 : 0` |
| `PressureOutput` | `return (ConnectedPipeNetwork?.Atmosphere?.PressureGassesAndLiquids.ToDouble()).GetValueOrDefault()` |
| `RatioCarbonDioxideOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioHeliumOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioHydrazineOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioHydrochloricAcidOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioHydrogenOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioLiquidAlcoholOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioLiquidCarbonDioxideOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioLiquidHydrazineOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioLiquidHydrochloricAcidOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioLiquidHydrogenOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioLiquidMethaneOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioLiquidNitrogenOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioLiquidNitrousOxideOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioLiquidOxygenOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioLiquidOzoneOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioLiquidPollutantOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioLiquidSilanolOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioLiquidSodiumChlorideOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioMethaneOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioNitrogenOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioNitrousOxideOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioOxygenOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioOzoneOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioPollutantOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioPollutedWaterOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioSilanolOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioSteamOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `RatioWaterOutput` | `if (ConnectedPipeNetwork?.Atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere)` |
| `TemperatureOutput` | `return (ConnectedPipeNetwork?.Atmosphere?.Temperature.ToDouble()).GetValueOrDefault()` |
| `TotalMolesOutput` | `return (ConnectedPipeNetwork?.Atmosphere?.TotalMoles.ToDouble()).GetValueOrDefault()` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `CombustionOutput` | `return true` |
| `PressureOutput` | `return true` |
| `RatioCarbonDioxideOutput` | `return true` |
| `RatioHeliumOutput` | `return true` |
| `RatioHydrazineOutput` | `return true` |
| `RatioHydrochloricAcidOutput` | `return true` |
| `RatioHydrogenOutput` | `return true` |
| `RatioLiquidAlcoholOutput` | `return true` |
| `RatioLiquidCarbonDioxideOutput` | `return true` |
| `RatioLiquidHydrazineOutput` | `return true` |
| `RatioLiquidHydrochloricAcidOutput` | `return true` |
| `RatioLiquidHydrogenOutput` | `return true` |
| `RatioLiquidMethaneOutput` | `return true` |
| `RatioLiquidNitrogenOutput` | `return true` |
| `RatioLiquidNitrousOxideOutput` | `return true` |
| `RatioLiquidOxygenOutput` | `return true` |
| `RatioLiquidOzoneOutput` | `return true` |
| `RatioLiquidPollutantOutput` | `return true` |
| `RatioLiquidSilanolOutput` | `return true` |
| `RatioLiquidSodiumChlorideOutput` | `return true` |
| `RatioMethaneOutput` | `return true` |
| `RatioNitrogenOutput` | `return true` |
| `RatioNitrousOxideOutput` | `return true` |
| `RatioOxygenOutput` | `return true` |
| `RatioOzoneOutput` | `return true` |
| `RatioPollutantOutput` | `return true` |
| `RatioPollutedWaterOutput` | `return true` |
| `RatioSilanolOutput` | `return true` |
| `RatioSteamOutput` | `return true` |
| `RatioWaterOutput` | `return true` |
| `TemperatureOutput` | `return true` |
| `TotalMolesOutput` | `return true` |

---

### `DiodeSlide`

**File**: `Assets.Scripts.Objects.Electrical/DiodeSlide.cs` | **Extends**: `Diode`

---

### `Door`

**File**: `Assets.Scripts.Objects.Structures/Door.cs` | **Extends**: `Device`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Idle` | `(!base.IsDeviceActive) ? 1 : 0` |
| `Setting` | `Setting` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Idle` | `true` |
| `Setting` | `true` |

---

### `ElevatorShaft`

**File**: `Assets.Scripts.Objects.Electrical/ElevatorShaft.cs` | **Extends**: `Device`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `ElevatorLevel` | `if (ShaftNetwork == null \|\| ShaftNetwork.Carrage == null \|\| ShaftNetwork.Carrage.CurrentShaft == null) { return -1.0; } return ShaftNetwork.Carrage.CurrentShaft.ShaftLevel` |
| `ElevatorSpeed` | `return ShaftNetwork?.Speed ?? 0f` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `ElevatorLevel` | `if (ShaftNetwork != null && ShaftNetwork.Carrage != null && (int)value != ShaftNetwork.Carrage.LevelTarget) { if (GameManager.IsThread) { SetFromThread((int)value).Forget(); } else...` |
| `ElevatorSpeed` | `if (ShaftNetwork != null && ShaftNetwork.Carrage != null) { value = Mathf.Clamp((float)value, MinSpeed, ShaftNetwork.Carrage.MaxMovementSpeed); ShaftNetwork.Speed = (float)value; }...` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `ElevatorLevel` | `true` |
| `ElevatorSpeed` | `true` |

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `ElevatorLevel` | `true` |
| `ElevatorSpeed` | `true` |

---

### `Fabricator`

**File**: `Assets.Scripts.Objects.Electrical/Fabricator.cs` | **Extends**: `FabricatorBase`

---

### `Fermenter`

**File**: `Objects.Electrical/Fermenter.cs` | **Extends**: `DeviceInputOutputImportCircuit`

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Activate` | `false` |
| `CompletionRatio` | `true` |

---

### `FixedBeacon`

**File**: `Assets.Scripts.Objects.Electrical/FixedBeacon.cs` | **Extends**: `Diode`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `PositionX` | `Mathf.RoundToInt(base.Position.x)` |
| `PositionY` | `Mathf.RoundToInt(base.Position.y)` |
| `PositionZ` | `Mathf.RoundToInt(base.Position.z)` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `PositionX` | `true` |
| `PositionY` | `true` |
| `PositionZ` | `true` |

---

### `FridgePowered`

**File**: `Objects.Electrical/FridgePowered.cs` | **Extends**: `DeviceInternal`

---

### `FurnaceBase`

**File**: `Assets.Scripts.Objects.Pipes/FurnaceBase.cs` | **Extends**: `DeviceInputOutputImportExport`

---

### `GasMask`

**File**: `Assets.Scripts.Objects.Items/GasMask.cs` | **Extends**: `AtmosphericItem`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `SoundAlert` | `(int)SoundAlert` |
| `Volume` | `(int)SoundVolume` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Flush` | `if (value > double.Epsilon) { FlushMaskFromThread().Forget(); } break` |
| `SoundAlert` | `SoundAlert = (byte)Mathf.Clamp((int)value, 0, EnumCollections.SpeakerSounds.Length - 1)` |
| `Volume` | `SoundVolume = (byte)Mathf.Clamp((int)value, 0, 100)` |

---

### `GasSensor`

**File**: `Assets.Scripts.Objects.Electrical/GasSensor.cs` | **Extends**: `SmallDevice`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Combustion` | `if (!AirIgnited) { return 0.0; } return 1.0` |
| `Pressure` | `return AirPressure.ToDouble()` |
| `RatioCarbonDioxide` | `return GasRatio(logicType)` |
| `RatioHelium` | `return GasRatio(logicType)` |
| `RatioHydrazine` | `return GasRatio(logicType)` |
| `RatioHydrochloricAcid` | `return GasRatio(logicType)` |
| `RatioHydrogen` | `return GasRatio(logicType)` |
| `RatioLiquidAlcohol` | `return GasRatio(logicType)` |
| `RatioLiquidCarbonDioxide` | `return GasRatio(logicType)` |
| `RatioLiquidHydrazine` | `return GasRatio(logicType)` |
| `RatioLiquidHydrochloricAcid` | `return GasRatio(logicType)` |
| `RatioLiquidHydrogen` | `return GasRatio(logicType)` |
| `RatioLiquidMethane` | `return GasRatio(logicType)` |
| `RatioLiquidNitrogen` | `return GasRatio(logicType)` |
| `RatioLiquidNitrousOxide` | `return GasRatio(logicType)` |
| `RatioLiquidOxygen` | `return GasRatio(logicType)` |
| `RatioLiquidOzone` | `return GasRatio(logicType)` |
| `RatioLiquidPollutant` | `return GasRatio(logicType)` |
| `RatioLiquidSilanol` | `return GasRatio(logicType)` |
| `RatioLiquidSodiumChloride` | `return GasRatio(logicType)` |
| `RatioMethane` | `return GasRatio(logicType)` |
| `RatioNitrogen` | `return GasRatio(logicType)` |
| `RatioNitrousOxide` | `return GasRatio(logicType)` |
| `RatioOxygen` | `return GasRatio(logicType)` |
| `RatioOzone` | `return GasRatio(logicType)` |
| `RatioPollutant` | `return GasRatio(logicType)` |
| `RatioPollutedWater` | `return GasRatio(logicType)` |
| `RatioSilanol` | `return GasRatio(logicType)` |
| `RatioSteam` | `return GasRatio(logicType)` |
| `RatioWater` | `return GasRatio(logicType)` |
| `Temperature` | `return AirTemperature.ToDouble()` |
| `TotalMoles` | `return TotalMoles.ToDouble()` |
| `VolumeOfLiquid` | `return VolumeOfLiquid.ToDouble()` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Combustion` | `return true` |
| `Pressure` | `return true` |
| `RatioCarbonDioxide` | `return true` |
| `RatioHelium` | `return true` |
| `RatioHydrazine` | `return true` |
| `RatioHydrochloricAcid` | `return true` |
| `RatioHydrogen` | `return true` |
| `RatioLiquidAlcohol` | `return true` |
| `RatioLiquidCarbonDioxide` | `return true` |
| `RatioLiquidHydrazine` | `return true` |
| `RatioLiquidHydrochloricAcid` | `return true` |
| `RatioLiquidHydrogen` | `return true` |
| `RatioLiquidMethane` | `return true` |
| `RatioLiquidNitrogen` | `return true` |
| `RatioLiquidNitrousOxide` | `return true` |
| `RatioLiquidOxygen` | `return true` |
| `RatioLiquidOzone` | `return true` |
| `RatioLiquidPollutant` | `return true` |
| `RatioLiquidSilanol` | `return true` |
| `RatioLiquidSodiumChloride` | `return true` |
| `RatioMethane` | `return true` |
| `RatioNitrogen` | `return true` |
| `RatioNitrousOxide` | `return true` |
| `RatioOxygen` | `return true` |
| `RatioOzone` | `return true` |
| `RatioPollutant` | `return true` |
| `RatioPollutedWater` | `return true` |
| `RatioSilanol` | `return true` |
| `RatioSteam` | `return true` |
| `RatioWater` | `return true` |
| `Temperature` | `return true` |
| `TotalMoles` | `return true` |
| `VolumeOfLiquid` | `return true` |

---

### `GasTankStorage`

**File**: `Assets.Scripts.Objects.Pipes/GasTankStorage.cs` | **Extends**: `Device`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Pressure` | `return TankPressure.ToDouble()` |
| `Quantity` | `return TankQuantity.ToDouble()` |
| `RatioCarbonDioxide` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioHelium` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioHydrazine` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioHydrochloricAcid` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioHydrogen` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioLiquidAlcohol` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioLiquidCarbonDioxide` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioLiquidHydrazine` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioLiquidHydrochloricAcid` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioLiquidHydrogen` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioLiquidMethane` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioLiquidNitrogen` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioLiquidNitrousOxide` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioLiquidOxygen` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioLiquidOzone` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioLiquidPollutant` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioLiquidSilanol` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioLiquidSodiumChloride` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioMethane` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioNitrogen` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioNitrousOxide` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioOxygen` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioOzone` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioPollutant` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioPollutedWater` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioSilanol` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioSteam` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `RatioWater` | `if (ConnectedGasCanisters.Count <= 0) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere)` |
| `Temperature` | `return TankTemperature.ToDouble()` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Pressure` | `return true` |
| `Quantity` | `return true` |
| `RatioCarbonDioxide` | `return true` |
| `RatioHelium` | `return true` |
| `RatioHydrazine` | `return true` |
| `RatioHydrochloricAcid` | `return true` |
| `RatioHydrogen` | `return true` |
| `RatioLiquidAlcohol` | `return true` |
| `RatioLiquidCarbonDioxide` | `return true` |
| `RatioLiquidHydrazine` | `return true` |
| `RatioLiquidHydrochloricAcid` | `return true` |
| `RatioLiquidHydrogen` | `return true` |
| `RatioLiquidMethane` | `return true` |
| `RatioLiquidNitrogen` | `return true` |
| `RatioLiquidNitrousOxide` | `return true` |
| `RatioLiquidOxygen` | `return true` |
| `RatioLiquidOzone` | `return true` |
| `RatioLiquidPollutant` | `return true` |
| `RatioLiquidSilanol` | `return true` |
| `RatioLiquidSodiumChloride` | `return true` |
| `RatioMethane` | `return true` |
| `RatioNitrogen` | `return true` |
| `RatioNitrousOxide` | `return true` |
| `RatioOxygen` | `return true` |
| `RatioOzone` | `return true` |
| `RatioPollutant` | `return true` |
| `RatioPollutedWater` | `return true` |
| `RatioSilanol` | `return true` |
| `RatioSteam` | `return true` |
| `RatioWater` | `return true` |
| `Temperature` | `return true` |

---

### `GovernedGasEngine`

**File**: `Assets.Scripts.Objects.Pipes/GovernedGasEngine.cs` | **Extends**: `RocketEngineBase`

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Maximum` | `false` |
| `Ratio` | `false` |
| `Setting` | `false` |

---

### `GroundTelescope`

**File**: `Assets.Scripts/GroundTelescope.cs` | **Extends**: `LargeRotatable`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `AlignmentError` | `_celestialHit.GetLogicAngle()` |
| `CelestialHash` | `CurrentCelestial?.Hash ?? 0` |
| `CelestialParentHash` | `IsAligned(2f) ? _celestialHit.GetParentHash() : 0` |
| `DistanceAu` | `IsAligned(2f) ? _celestialHit.GetDistanceAu() : double.NaN` |
| `DistanceKm` | `IsAligned(2f) ? _celestialHit.GetDistanceKm() : double.NaN` |
| `Eccentricity` | `IsAligned(2f) ? ((double)_celestialHit.GetEccentricity()) : double.NaN` |
| `Horizontal` | `Horizontal * base.MaximumHorizontal` |
| `HorizontalRatio` | `Horizontal` |
| `Inclination` | `IsAligned(2f) ? ((double)_celestialHit.GetInclination()) : double.NaN` |
| `OrbitPeriod` | `IsAligned(2f) ? _celestialHit.GetPeriodDays() : double.NaN` |
| `SemiMajorAxis` | `IsAligned(2f) ? _celestialHit.GetSemiMajorAxis() : double.NaN` |
| `TrueAnomaly` | `IsAligned(2f) ? _celestialHit.GetTrueAnomaly() : double.NaN` |
| `Vertical` | `Vertical * base.MaximumVertical` |
| `VerticalRatio` | `Vertical` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Horizontal` | `{ value = RocketMath.ModuloCorrect(value, base.MaximumHorizontal); double num = value / base.MaximumHorizontal; if (!RocketMath.Approximately(num, base.RotatableBehaviour.TargetHor...` |
| `HorizontalRatio` | `value = RocketMath.ModuloCorrect(value, 1.0)` |
| `Vertical` | `{ if (value < 0.0) { value = 0.0; } if (value > base.MaximumVertical) { value = base.MaximumVertical; } double num = value / base.MaximumVertical; if (!RocketMath.Approximately(num...` |
| `VerticalRatio` | `if (value < 0.0) { value = 0.0; } if (value > 1.0) { value = 1.0; } if (!RocketMath.Approximately(value, base.RotatableBehaviour.TargetVertical, RotationTolerance)) { base.Rotatabl...` |

---

### `Harvester`

**File**: `Assets.Scripts.Objects.Chutes/Harvester.cs` | **Extends**: `DeviceImportExport`

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Harvest` | `if (value <= 0.0) { return; } TryHarvestPlant()` |
| `Plant` | `if (value <= 0.0) { return; } TryPlantSeed()` |

---

### `HydroponicsAutomated`

**File**: `Assets.Scripts.Objects.Electrical/HydroponicsAutomated.cs` | **Extends**: `DeviceInputOutputImportExport`

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Harvest` | `if (value <= 0.0) { return; } if (Plant != null && Exporting == 0 && ExportingThing == null && Activate == 0) { OnServer.Interact(base.InteractActivate, 2); } break` |
| `Plant` | `PlantAction(0uL)` |

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Activate` | `return false` |
| `Harvest` | `return true` |
| `Plant` | `return true` |

---

### `IceCrusher`

**File**: `Assets.Scripts.Objects.Pipes/IceCrusher.cs` | **Extends**: `DeviceInputOutputImport`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Pressure` | `base.InternalAtmosphere.PressureGassesAndLiquids.ToDouble()` |
| `Temperature` | `base.InternalAtmosphere.Temperature.ToDouble()` |
| `Volume` | `base.InternalAtmosphere.Volume.ToDouble()` |
| `VolumeOfLiquid` | `base.InternalAtmosphere.TotalVolumeLiquids.ToDouble()` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Pressure` | `true` |
| `Temperature` | `true` |
| `Volume` | `true` |
| `VolumeOfLiquid` | `true` |

---

### `Injector`

**File**: `Injector.cs` | **Extends**: `Item`

---

### `LandingPadDataPowerConnection`

**File**: `Objects.Electrical/LandingPadDataPowerConnection.cs` | **Extends**: `LandingPadModularDevice`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Activate` | `if (base.LandingPadCenter != null) { return base.LandingPadCenter.GetLogicValue(logicType); } break` |
| `Combustion` | `if (!(base.LandingPadNetwork?.Atmosphere?.Sparked).GetValueOrDefault()) { return 0.0; } return 1.0` |
| `ContactTypeId` | `return (base.LandingPadCenter?.CurrentTradingContact?.DataInstance?.TraderData?.IdHash).GetValueOrDefault()` |
| `Mode` | `if (base.LandingPadCenter != null) { return base.LandingPadCenter.GetLogicValue(logicType); } return -1.0` |
| `Pressure` | `return (base.LandingPadNetwork?.Atmosphere?.PressureGassesAndLiquids.ToDouble()).GetValueOrDefault()` |
| `Temperature` | `return (base.LandingPadNetwork?.Atmosphere?.Temperature.ToDouble()).GetValueOrDefault()` |
| `TotalMoles` | `return (base.LandingPadNetwork?.Atmosphere?.TotalMoles.ToDouble()).GetValueOrDefault()` |
| `Vertical` | `if (base.LandingPadCenter != null) { return base.LandingPadCenter.GetLogicValue(logicType); } return -1.0` |

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Mode` | `false` |
| `Vertical` | `true` |

---

### `LandingPadPump`

**File**: `Objects.Electrical/LandingPadPump.cs` | **Extends**: `DeviceInputOutput`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Combustion` | `if (!(LandingPadNetwork?.Atmosphere?.Sparked).GetValueOrDefault()) { return 0.0; } return 1.0` |
| `Pressure` | `return (LandingPadNetwork?.Atmosphere?.PressureGassesAndLiquids.ToDouble()).GetValueOrDefault()` |
| `Temperature` | `return (LandingPadNetwork?.Atmosphere?.Temperature.ToDouble()).GetValueOrDefault()` |
| `TotalMoles` | `return (LandingPadNetwork?.Atmosphere?.TotalMoles.ToDouble()).GetValueOrDefault()` |

---

### `Laptop`

**File**: `Assets.Scripts.Objects.Electrical/Laptop.cs` | **Extends**: `PowerTool`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `PositionX` | `base.Position.x` |
| `PositionY` | `base.Position.y` |
| `PositionZ` | `base.Position.z` |
| `PressureExternal` | `base.WorldAtmosphere?.PressureGassesAndLiquids.ToDouble() ?? 0.0` |
| `TemperatureExternal` | `base.WorldAtmosphere?.Temperature.ToDouble() ?? 0.0` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `PositionX` | `true` |
| `PositionY` | `true` |
| `PositionZ` | `true` |
| `PressureExternal` | `true` |
| `TemperatureExternal` | `true` |

---

### `LargeExtendableRadiator`

**File**: `Assets.Scripts.Objects/LargeExtendableRadiator.cs` | **Extends**: `RadiatorRotatable`

---

### `LfoVolume`

**File**: `Assets.Scripts.Objects.Electrical/LfoVolume.cs` | **Extends**: `LogicUnitBase`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Bpm` | `Bpm` |
| `Time` | `Setting` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Bpm` | `Bpm = (int)Mathf.Clamp((float)value, 15f, 180f)` |
| `Time` | `Setting = value` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Bpm` | `true` |
| `Time` | `true` |

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Bpm` | `true` |
| `Time` | `true` |

---

### `LogicButton`

**File**: `Assets.Scripts.Objects.Electrical/LogicButton.cs` | **Extends**: `LogicInputBase`

---

### `LogicDial`

**File**: `Assets.Scripts.Objects.Electrical/LogicDial.cs` | **Extends**: `LogicInputBase`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Ratio` | `Setting / (double)Mode` |
| `Setting` | `Setting` |

---

### `LogicDisplay`

**File**: `Assets.Scripts.Objects.Electrical/LogicDisplay.cs` | **Extends**: `LogicUnitBase`

---

### `LogicHashGen`

**File**: `Assets.Scripts.Objects.Electrical/LogicHashGen.cs` | **Extends**: `LogicUnitBase`

---

### `LogicMemory`

**File**: `Assets.Scripts.Objects.Electrical/LogicMemory.cs` | **Extends**: `LogicInputBase`

---

### `LogicMirror`

**File**: `Assets.Scripts.Objects.Electrical/LogicMirror.cs` | **Extends**: `LogicInputBase`

---

### `LogicPidController`

**File**: `Assets.Scripts.Objects.Electrical/LogicPidController.cs` | **Extends**: `LogicReader`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `DerivativeGain` | `_derivativeGain` |
| `IntegralGain` | `_integralGain` |
| `Maximum` | `_outputMaximum` |
| `Minimum` | `_outputMinimum` |
| `ProportionalGain` | `_proportionalGain` |
| `Setpoint` | `_setpoint` |
| `Setting` | `Setting` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `DerivativeGain` | `_derivativeGain = (float)value` |
| `IntegralGain` | `_integralGain = (float)value` |
| `Maximum` | `_outputMaximum = (float)value` |
| `Minimum` | `_outputMinimum = (float)value` |
| `ProportionalGain` | `_proportionalGain = (float)value` |
| `Reset` | `if (value > 0.0) { _pidController?.ResetController(); } break` |
| `Setpoint` | `_setpoint = (float)value` |

---

### `LogicReaderBase`

**File**: `Assets.Scripts.Objects.Electrical/LogicReaderBase.cs` | **Extends**: `LogicUnitBase`

---

### `LogicStopWatch`

**File**: `Objects.Electrical/LogicStopWatch.cs` | **Extends**: `LogicUnitBase`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Setting` | `if (OnOff && Powered) { return _time; } return 0.0` |
| `Time` | `if (OnOff && Powered) { return _time; } return 0.0` |

---

### `LogicSwitch`

**File**: `Assets.Scripts.Objects.Electrical/LogicSwitch.cs` | **Extends**: `LogicInputBase`

---

### `LogicTransmitter`

**File**: `Assets.Scripts.Objects.Electrical/LogicTransmitter.cs` | **Extends**: `LogicInputBase`

---

### `LogicUnitProcessor`

**File**: `Assets.Scripts.Objects.Electrical/LogicUnitProcessor.cs` | **Extends**: `LogicUnitBase`

---

### `LogicWriterBase`

**File**: `Assets.Scripts.Objects.Electrical/LogicWriterBase.cs` | **Extends**: `LogicUnitBase`

---

### `MotionSensor`

**File**: `Assets.Scripts.Objects.Electrical/MotionSensor.cs` | **Extends**: `Sensor`

---

### `OccupancySensor`

**File**: `Assets.Scripts.Objects.Electrical/OccupancySensor.cs` | **Extends**: `Sensor`

---

### `OreDetector`

**File**: `Objects.Items/OreDetector.cs` | **Extends**: `PowerTool`

---

### `Packer`

**File**: `Objects.Electrical/Packer.cs` | **Extends**: `ImportExport`

---

### `PassiveSpeaker`

**File**: `Assets.Scripts.Objects.Electrical/PassiveSpeaker.cs` | **Extends**: `Device`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `SoundAlert` | `(int)SoundAlert` |
| `Volume` | `(int)SoundVolume` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `SoundAlert` | `SoundAlert = (byte)Mathf.Clamp((int)value, 0, EnumCollections.SpeakerSounds.Length - 1)` |
| `Volume` | `SoundVolume = (byte)Mathf.Clamp((int)value, 1, 100)` |

---

### `PipeAnalysizer`

**File**: `Assets.Scripts.Objects.Pipes/PipeAnalysizer.cs` | **Extends**: `DevicePipeMounted`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Combustion` | `if (!PipeIgnited) { return 0.0; } return 1.0` |
| `NetworkFault` | `if (!PipeBurst) { return 0.0; } return 1.0` |
| `Pressure` | `return PipePressure.ToDouble()` |
| `RatioCarbonDioxide` | `return GasRatio(logicType)` |
| `RatioHelium` | `return GasRatio(logicType)` |
| `RatioHydrazine` | `return GasRatio(logicType)` |
| `RatioHydrochloricAcid` | `return GasRatio(logicType)` |
| `RatioHydrogen` | `return GasRatio(logicType)` |
| `RatioLiquidAlcohol` | `return GasRatio(logicType)` |
| `RatioLiquidCarbonDioxide` | `return GasRatio(logicType)` |
| `RatioLiquidHydrazine` | `return GasRatio(logicType)` |
| `RatioLiquidHydrochloricAcid` | `return GasRatio(logicType)` |
| `RatioLiquidHydrogen` | `return GasRatio(logicType)` |
| `RatioLiquidMethane` | `return GasRatio(logicType)` |
| `RatioLiquidNitrogen` | `return GasRatio(logicType)` |
| `RatioLiquidNitrousOxide` | `return GasRatio(logicType)` |
| `RatioLiquidOxygen` | `return GasRatio(logicType)` |
| `RatioLiquidOzone` | `return GasRatio(logicType)` |
| `RatioLiquidPollutant` | `return GasRatio(logicType)` |
| `RatioLiquidSilanol` | `return GasRatio(logicType)` |
| `RatioLiquidSodiumChloride` | `return GasRatio(logicType)` |
| `RatioMethane` | `return GasRatio(logicType)` |
| `RatioNitrogen` | `return GasRatio(logicType)` |
| `RatioNitrousOxide` | `return GasRatio(logicType)` |
| `RatioOxygen` | `return GasRatio(logicType)` |
| `RatioOzone` | `return GasRatio(logicType)` |
| `RatioPollutant` | `return GasRatio(logicType)` |
| `RatioPollutedWater` | `return GasRatio(logicType)` |
| `RatioSilanol` | `return GasRatio(logicType)` |
| `RatioSteam` | `return GasRatio(logicType)` |
| `RatioWater` | `return GasRatio(logicType)` |
| `Temperature` | `return PipeTemperature.ToDouble()` |
| `TotalMoles` | `return TotalMoles.ToDouble()` |
| `Volume` | `return Volume.ToDouble()` |
| `VolumeOfLiquid` | `return VolumeOfLiquid.ToDouble()` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Combustion` | `return true` |
| `NetworkFault` | `return true` |
| `Pressure` | `return true` |
| `RatioCarbonDioxide` | `return true` |
| `RatioHelium` | `return true` |
| `RatioHydrazine` | `return true` |
| `RatioHydrochloricAcid` | `return true` |
| `RatioHydrogen` | `return true` |
| `RatioLiquidAlcohol` | `return true` |
| `RatioLiquidCarbonDioxide` | `return true` |
| `RatioLiquidHydrazine` | `return true` |
| `RatioLiquidHydrochloricAcid` | `return true` |
| `RatioLiquidHydrogen` | `return true` |
| `RatioLiquidMethane` | `return true` |
| `RatioLiquidNitrogen` | `return true` |
| `RatioLiquidNitrousOxide` | `return true` |
| `RatioLiquidOxygen` | `return true` |
| `RatioLiquidOzone` | `return true` |
| `RatioLiquidPollutant` | `return true` |
| `RatioLiquidSilanol` | `return true` |
| `RatioLiquidSodiumChloride` | `return true` |
| `RatioMethane` | `return true` |
| `RatioNitrogen` | `return true` |
| `RatioNitrousOxide` | `return true` |
| `RatioOxygen` | `return true` |
| `RatioOzone` | `return true` |
| `RatioPollutant` | `return true` |
| `RatioPollutedWater` | `return true` |
| `RatioSilanol` | `return true` |
| `RatioSteam` | `return true` |
| `RatioWater` | `return true` |
| `Temperature` | `return true` |
| `TotalMoles` | `return true` |
| `Volume` | `return true` |
| `VolumeOfLiquid` | `return true` |

---

### `PoweredVent`

**File**: `Assets.Scripts.Objects.Pipes/PoweredVent.cs` | **Extends**: `SmallDeviceOutput`

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Maximum` | `return false` |
| `PressureExternal` | `return true` |
| `Ratio` | `return false` |
| `Setting` | `return false` |

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Maximum` | `return false` |
| `PressureExternal` | `return true` |
| `Ratio` | `return false` |
| `Setting` | `return false` |

---

### `PowerGeneratorPipe`

**File**: `Assets.Scripts.Objects.Electrical/PowerGeneratorPipe.cs` | **Extends**: `DeviceInputOutput`

---

### `PressureFedGasEngine`

**File**: `Assets.Scripts.Objects.Pipes/PressureFedGasEngine.cs` | **Extends**: `PressureFedEngine`

---

### `ProgrammableChip`

**File**: `Assets.Scripts.Objects.Electrical/ProgrammableChip.cs` | **Extends**: `Item`

---

### `ProximitySensor`

**File**: `Assets.Scripts.Objects.Electrical/ProximitySensor.cs` | **Extends**: `Sensor`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Quantity` | `Activate` |
| `Setting` | `Setting` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Quantity` | `true` |
| `Setting` | `true` |

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Activate` | `false` |
| `Setting` | `true` |

---

### `Radiator`

**File**: `Assets.Scripts.Objects/Radiator.cs` | **Extends**: `DeviceInputOutput`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `EnergyConvected` | `EnergyConvected` |
| `EnergyRadiated` | `EnergyRadiated` |

---

### `RadiatorRotatable`

**File**: `Assets.Scripts.Objects/RadiatorRotatable.cs` | **Extends**: `Radiator`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Horizontal` | `Horizontal * MaximumHorizontal` |
| `HorizontalRatio` | `Horizontal` |
| `Vertical` | `Vertical * MaximumVertical` |
| `VerticalRatio` | `Vertical` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Horizontal` | `{ value = RocketMath.ModuloCorrect(value, MaximumHorizontal); double num = value / MaximumHorizontal; if (!RocketMath.Approximately(num, RotatableBehaviour.TargetHorizontal, Rotati...` |
| `HorizontalRatio` | `value = RocketMath.ModuloCorrect(value, 1.0)` |
| `Vertical` | `{ if (value < 0.0) { value = 0.0; } if (value > MaximumVertical) { value = MaximumVertical; } double num = value / MaximumVertical; if (!RocketMath.Approximately(num, RotatableBeha...` |
| `VerticalRatio` | `if (value < 0.0) { value = 0.0; } if (value > 1.0) { value = 1.0; } if (!RocketMath.Approximately(value, RotatableBehaviour.TargetVertical, RotationTolerance)) { RotatableBehaviour...` |

---

### `RoboticArmDock`

**File**: `Objects.RoboticArm/RoboticArmDock.cs` | **Extends**: `RoboticArmRailDeviceBase`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Extended` | `(ArmState == ArmState.Down) ? 1 : 0` |
| `Idle` | `IsIdle() ? 1 : 0` |
| `PositionX` | `CurrentJunctionIndex` |
| `Setting` | `TargetJunctionIndex` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Activate` | `if (IsOperable && !IsMoving) { OnServer.Interact(base.InteractActivate, (int)value); } break` |
| `Open` | `{ int openState = (int)Mathf.Clamp((float)value, 0f, 1f); TrySetOpenState(openState); break; }` |
| `Setting` | `TargetJunctionIndex = (int)value` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Extended` | `true` |
| `Idle` | `true` |
| `PositionX` | `true` |
| `Setting` | `true` |

---

### `RoboticArmDockAtmos`

**File**: `Objects.RoboticArm/RoboticArmDockAtmos.cs` | **Extends**: `RoboticArmDock`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `CombustionInput` | `return (GetInputAtmos() != null) ? (GetInputAtmos().Inflamed ? 1 : 0) : 0` |
| `PressureExternal` | `return ExternalPressure.ToDouble()` |
| `PressureInput` | `return GetInputAtmos()?.PressureGassesAndLiquids.ToDouble() ?? 0.0` |
| `PressureInternal` | `return InternalPressure.ToDouble()` |
| `RatioCarbonDioxideInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioHeliumInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioHydrazineInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioHydrochloricAcidInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioHydrogenInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioLiquidAlcoholInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioLiquidCarbonDioxideInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioLiquidHydrazineInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioLiquidHydrochloricAcidInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioLiquidHydrogenInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioLiquidMethaneInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioLiquidNitrogenInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioLiquidNitrousOxideInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioLiquidOxygenInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioLiquidOzoneInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioLiquidPollutantInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioLiquidSilanolInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioLiquidSodiumChlorideInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioMethaneInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioNitrogenInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioNitrousOxideInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioOxygenInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioOzoneInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioPollutantInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioPollutedWaterInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioSilanolInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioSteamInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `RatioWaterInput` | `return AtmosphereHelper.GasRatio(logicType, GetInputAtmos())` |
| `TemperatureInput` | `return GetInputAtmos()?.Temperature.ToDouble() ?? 0.0` |
| `TotalMolesInput` | `return GetInputAtmos()?.TotalMoles.ToDouble() ?? 0.0` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `PressureExternal` | `ExternalPressure = new PressurekPa(value)` |
| `PressureInternal` | `InternalPressure = new PressurekPa(value)` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `CombustionInput` | `true` |
| `PressureExternal` | `true` |
| `PressureInput` | `true` |
| `PressureInternal` | `true` |
| `RatioCarbonDioxideInput` | `true` |
| `RatioLiquidCarbonDioxideInput` | `true` |
| `RatioLiquidMethaneInput` | `true` |
| `RatioLiquidNitrogenInput` | `true` |
| `RatioLiquidNitrousOxideInput` | `true` |
| `RatioLiquidOxygenInput` | `true` |
| `RatioLiquidPollutantInput` | `true` |
| `RatioMethaneInput` | `true` |
| `RatioNitrogenInput` | `true` |
| `RatioNitrousOxideInput` | `true` |
| `RatioOxygenInput` | `true` |
| `RatioPollutantInput` | `true` |
| `RatioSteamInput` | `true` |
| `RatioWaterInput` | `true` |
| `TemperatureInput` | `true` |
| `TotalMolesInput` | `true` |

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `PressureExternal` | `true` |
| `PressureInternal` | `true` |

---

### `RoboticArmDockCargo`

**File**: `Objects.RoboticArm/RoboticArmDockCargo.cs` | **Extends**: `RoboticArmDock`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `TargetPrefabHash` | `((double?)TargetLogicable?.GetPrefabHash()) ?? 0.0` |
| `TargetSlotIndex` | `CurrentSlotIndex` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `TargetPrefabHash` | `true` |
| `TargetSlotIndex` | `true` |

---

### `RoboticArmDockCollector`

**File**: `Objects.RoboticArm/RoboticArmDockCollector.cs` | **Extends**: `RoboticArmDock`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Quantity` | `_stackIndex + 1` |
| `Ratio` | `(float)(_stackIndex + 1) / 20f` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Quantity` | `true` |
| `Ratio` | `true` |

---

### `RoboticArmDockHydroponics`

**File**: `Objects.RoboticArm/RoboticArmDockHydroponics.cs` | **Extends**: `RoboticArmDock`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `TargetPrefabHash` | `((double?)TargetSmallGrid?.GetPrefabHash()) ?? 0.0` |
| `TargetSlotIndex` | `TargetSlotIndex` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `TargetPrefabHash` | `true` |
| `TargetSlotIndex` | `true` |

---

### `RobotMining`

**File**: `Assets.Scripts.Objects/RobotMining.cs` | **Extends**: `WheeledBase`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `ForwardX` | `return Forward.x` |
| `ForwardY` | `return Forward.y` |
| `ForwardZ` | `return Forward.z` |
| `MineablesInQueue` | `return _minableDataQueue.Count` |
| `MineablesInVicinity` | `return VoxelTerrain.GetNumberOfMinablesNearSurface(base.ThingTransformPosition, MinableSearchArea, maxMiningDepth)` |
| `Orientation` | `return Orientation` |
| `PositionX` | `return base.Position.x` |
| `PositionY` | `return base.Position.y` |
| `PositionZ` | `return base.Position.z` |
| `PressureExternal` | `if (base.WorldAtmosphere == null) { return 0.0; } return base.WorldAtmosphere.PressureGassesAndLiquids.ToDouble()` |
| `TemperatureExternal` | `if (base.WorldAtmosphere == null) { return 0.0; } return base.WorldAtmosphere.Temperature.ToDouble()` |
| `VelocityMagnitude` | `return base.VelocityMagnitude` |
| `VelocityRelativeX` | `return RelativeVelocity.x` |
| `VelocityRelativeY` | `return RelativeVelocity.y` |
| `VelocityRelativeZ` | `return RelativeVelocity.z` |
| `VelocityX` | `return base.Velocity.x` |
| `VelocityY` | `return base.Velocity.y` |
| `VelocityZ` | `return base.Velocity.z` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `TargetX` | `TargetX = (float)value` |
| `TargetY` | `TargetY = (float)value` |
| `TargetZ` | `TargetZ = (float)value` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `ForwardX` | `return true` |
| `ForwardY` | `return true` |
| `ForwardZ` | `return true` |
| `MineablesInQueue` | `return true` |
| `MineablesInVicinity` | `return true` |
| `Orientation` | `return true` |
| `PositionX` | `return true` |
| `PositionY` | `return true` |
| `PositionZ` | `return true` |
| `PressureExternal` | `return true` |
| `TemperatureExternal` | `return true` |
| `VelocityMagnitude` | `return true` |
| `VelocityRelativeX` | `return true` |
| `VelocityRelativeY` | `return true` |
| `VelocityRelativeZ` | `return true` |
| `VelocityX` | `return true` |
| `VelocityY` | `return true` |
| `VelocityZ` | `return true` |

---

### `RocketAvionicsDevice`

**File**: `Objects.Rockets/RocketAvionicsDevice.cs` | **Extends**: `Device`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Acceleration` | `return GetAcceleration()` |
| `Altitude` | `return GetAltitude()` |
| `Apex` | `return GetApexAltitude()` |
| `AutoLand` | `return GetIsAutoLand() ? 1 : 0` |
| `AutoShutOff` | `return GetIsAutoShutOff() ? 1 : 0` |
| `BurnTimeRemaining` | `return GetFuelTime()` |
| `Chart` | `return GetTargetChartRatio()` |
| `ChartedNavPoints` | `return GetTargetChartedNavPoints()` |
| `CurrentCode` | `return GetCurrentCode()` |
| `CurrentNodeType` | `return (double)(GetCurrentNode()?.NodeType ?? NodeType.None)` |
| `Density` | `return GetTargetDensity()` |
| `DestinationCode` | `return GetTargetCode()` |
| `Discover` | `return GetTargetDiscoverRatio()` |
| `DryMass` | `return GetDryMass()` |
| `FlightControlRule` | `return (double)GetFlightControlRule()` |
| `Gravity` | `return GetGravity()` |
| `Mass` | `return GetMass()` |
| `MinedQuantity` | `return GetTargetMinedQuantity()` |
| `Mode` | `return (int)GetRocketMode()` |
| `NavPoints` | `return GetTargetNavPoints()` |
| `Open` | `{ CrewModule crewModule = RocketNetwork?.CrewModule; return ((object)crewModule == null) ? (-1) : (crewModule.IsOpen ? 1 : 0); }` |
| `Progress` | `return GetProgress()` |
| `Quantity` | `return GetTargetResourceQuantity()` |
| `RatioCarbonDioxide` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioHelium` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioHydrazine` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioHydrochloricAcid` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioHydrogen` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioLiquidAlcohol` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioLiquidCarbonDioxide` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioLiquidHydrazine` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioLiquidHydrochloricAcid` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioLiquidHydrogen` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioLiquidMethane` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioLiquidNitrogen` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioLiquidNitrousOxide` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioLiquidOxygen` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioLiquidOzone` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioLiquidPollutant` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioLiquidSilanol` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioLiquidSodiumChloride` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioMethane` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioNitrogen` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioNitrousOxide` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioOxygen` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioOzone` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioPollutant` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioPollutedWater` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioSilanol` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioSteam` | `return GetTargetGasTypeRatio(logicType)` |
| `RatioWater` | `return GetTargetGasTypeRatio(logicType)` |
| `ReEntryAltitude` | `return Rocket.ReEntryProfiles[GetRocketReEntryProfile()]` |
| `Reagents` | `return GetTotalReagents()` |
| `Richness` | `return GetTargetRichness()` |
| `Sites` | `return GetTargetDiscoveredSites()` |
| `Size` | `return GetTargetSize()` |
| `Survey` | `return GetTargetSurveyedRatioUnClamped()` |
| `TargetNodeType` | `return (double)(GetTarget()?.NodeType ?? NodeType.None)` |
| `Temperature` | `return GetTargetTemperatureKelvin().ToDouble()` |
| `Thrust` | `return GetThrust()` |
| `ThrustToWeight` | `return GetThrustToWeight()` |
| `TimeToDestination` | `return GetEta()` |
| `TotalMoles` | `return GetTargetTotalMoles().ToDouble()` |
| `TotalQuantity` | `return GetTargetEstimatedTotalQuantity()` |
| `VelocityRelativeY` | `return GetVelocity()` |
| `Weight` | `return GetWeight()` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `AutoLand` | `SetAutoLand(value > 0.0)` |
| `AutoShutOff` | `SetAutoShutOff(value > 0.0)` |
| `DestinationCode` | `{ SpaceMapNode spaceMapNode = SpaceMapCode.Get((ulong)value); if (spaceMapNode != null && spaceMapNode.IsAccessible) { SetTargetDestination(spaceMapNode); } break; }` |
| `Mode` | `SetRocketMode((RocketMode)(int)value)` |
| `Open` | `{ CrewModule crewModule = RocketNetwork?.CrewModule; if ((object)crewModule != null) { OnServer.Interact(crewModule.InteractOpen, (value > 0.0) ? 1 : 0); } break; }` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Acceleration` | `return true` |
| `Altitude` | `return true` |
| `Apex` | `return true` |
| `AutoShutOff` | `return true` |
| `BurnTimeRemaining` | `return true` |
| `Chart` | `return true` |
| `ChartedNavPoints` | `return true` |
| `CurrentCode` | `return true` |
| `CurrentNodeType` | `return true` |
| `Density` | `return true` |
| `DestinationCode` | `return true` |
| `Discover` | `return true` |
| `DryMass` | `return true` |
| `FlightControlRule` | `return true` |
| `Gravity` | `return true` |
| `Mass` | `return true` |
| `MinedQuantity` | `return true` |
| `Mode` | `return true` |
| `NavPoints` | `return true` |
| `Open` | `return true` |
| `Progress` | `return true` |
| `Quantity` | `return true` |
| `RatioCarbonDioxide` | `return true` |
| `RatioHelium` | `return true` |
| `RatioHydrazine` | `return true` |
| `RatioHydrochloricAcid` | `return true` |
| `RatioHydrogen` | `return true` |
| `RatioLiquidAlcohol` | `return true` |
| `RatioLiquidCarbonDioxide` | `return true` |
| `RatioLiquidHydrazine` | `return true` |
| `RatioLiquidHydrochloricAcid` | `return true` |
| `RatioLiquidHydrogen` | `return true` |
| `RatioLiquidMethane` | `return true` |
| `RatioLiquidNitrogen` | `return true` |
| `RatioLiquidNitrousOxide` | `return true` |
| `RatioLiquidOxygen` | `return true` |
| `RatioLiquidOzone` | `return true` |
| `RatioLiquidPollutant` | `return true` |
| `RatioLiquidSilanol` | `return true` |
| `RatioLiquidSodiumChloride` | `return true` |
| `RatioMethane` | `return true` |
| `RatioNitrogen` | `return true` |
| `RatioNitrousOxide` | `return true` |
| `RatioOxygen` | `return true` |
| `RatioOzone` | `return true` |
| `RatioPollutant` | `return true` |
| `RatioPollutedWater` | `return true` |
| `RatioSilanol` | `return true` |
| `RatioSteam` | `return true` |
| `RatioWater` | `return true` |
| `ReEntryAltitude` | `return true` |
| `Reagents` | `return true` |
| `Richness` | `return true` |
| `Sites` | `return true` |
| `Size` | `return true` |
| `Survey` | `return true` |
| `TargetNodeType` | `return true` |
| `Temperature` | `return true` |
| `Thrust` | `return true` |
| `ThrustToWeight` | `return true` |
| `TimeToDestination` | `return true` |
| `TotalMoles` | `return true` |
| `TotalQuantity` | `return true` |
| `VelocityRelativeY` | `return true` |
| `Weight` | `return true` |

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `AutoLand` | `return true` |
| `AutoShutOff` | `return true` |
| `DestinationCode` | `return true` |
| `Mode` | `return true` |
| `Open` | `return true` |

---

### `RocketCelestialTracker`

**File**: `Objects.Rockets/RocketCelestialTracker.cs` | **Extends**: `Device`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `CelestialHash` | `CurrentCelestial?.Hash ?? 0` |
| `Horizontal` | `_horizontal` |
| `Index` | `(int)Index` |
| `Vertical` | `_vertical` |

---

### `RocketChuteStorage`

**File**: `Assets.Scripts.Objects.Electrical/RocketChuteStorage.cs` | **Extends**: `DeviceImportExport`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Quantity` | `CurrentIndex - 2` |
| `Ratio` | `(float)(CurrentIndex - 2) / (float)storageSlots` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Quantity` | `true` |
| `Ratio` | `true` |

---

### `RocketChuteUmbilicalMale`

**File**: `Objects.Rockets/RocketChuteUmbilicalMale.cs` | **Extends**: `ChuteDevice`

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Activate` | `false` |
| `Mode` | `false` |

---

### `RocketCrewUmbilical`

**File**: `Objects.Rockets/RocketCrewUmbilical.cs` | **Extends**: `Device`

---

### `RocketEngineBase`

**File**: `Assets.Scripts.Objects.Pipes/RocketEngineBase.cs` | **Extends**: `DeviceInput`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `PassedMoles` | `PassedMoles.ToDouble()` |
| `Throttle` | `Throttle` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `PassedMoles` | `true` |
| `Throttle` | `true` |

---

### `RocketGasUmbilicalMale`

**File**: `Objects.Rockets/RocketGasUmbilicalMale.cs` | **Extends**: `DeviceInput`

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Activate` | `false` |
| `Mode` | `false` |

---

### `RocketMiner`

**File**: `Assets.Scripts.Objects.Pipes/RocketMiner.cs` | **Extends**: `DeviceImportExport`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `DrillCondition` | `if (!(_miningHead != null)) { return -1.0; } return (double)_miningHead.Quantity / (double)_miningHead.MaxQuantity` |
| `Quantity` | `return QuantityMined` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `DrillCondition` | `true` |
| `Quantity` | `true` |

---

### `RocketPayloadBay`

**File**: `RocketPayloadBay.cs` | **Extends**: `DeviceInputOutput`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `PositionX` | `_positionX` |
| `PositionZ` | `_positionZ` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `PositionX` | `_positionX = (float)value` |
| `PositionZ` | `_positionZ = (float)value` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Maximum` | `return false` |
| `PositionX` | `return true` |
| `PositionZ` | `return true` |
| `Ratio` | `return false` |
| `Setting` | `return false` |

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `PositionX` | `true` |
| `PositionZ` | `true` |
| `Setting` | `false` |

---

### `RocketPowerUmbilicalMale`

**File**: `Objects.Rockets/RocketPowerUmbilicalMale.cs` | **Extends**: `RocketPowerUmbilical`

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Activate` | `false` |
| `Mode` | `false` |

---

### `SatelliteDish`

**File**: `Assets.Scripts.Objects.Electrical/SatelliteDish.cs` | **Extends**: `LargeElectrical`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `BestContactFilter` | `return _bestContactFilterReferenceID` |
| `ContactSlotIndex` | `return (_strongestContact?.ContactSlot != null) ? ContactSlot.ContactSlots.IndexOf(_strongestContact.ContactSlot) : (-1)` |
| `ContactTypeId` | `return _strongestContact?.DataInstance?.TraderData?.IdHash ?? 0` |
| `Horizontal` | `return Horizontal * MaximumHorizontal` |
| `HorizontalRatio` | `return Horizontal` |
| `Idle` | `return RotatableBehaviour.IsMoving ? 0f : 1f` |
| `InterrogationProgress` | `if (_strongestContact == null) { return -1.0; } if (!_strongestContact.Contacted) { return (_strongestContact?.InterrogationRatio()).Value; } return 1.0` |
| `MinimumWattsToContact` | `if (!DishScannedContacts.TryGetData(_strongestContact, out data)) { return -1.0; } return (data.CurrentTimeTillResolve > 0f) ? (-1f) : _strongestContact.MinimumWattsToContact` |
| `Setting` | `return Setting` |
| `SignalID` | `return _strongestContact?.ReferenceId ?? (-1)` |
| `SignalStrength` | `if (!DishScannedContacts.TryGetData(_strongestContact, out data)) { return -1.0; } return (data.CurrentTimeTillResolve > 0f) ? (-1f) : data.LastScannedDegreeOffset` |
| `SizeX` | `if (!DishScannedContacts.TryGetData(_strongestContact, out data)) { return -1.0; } return (data.CurrentTimeTillResolve > 0f) ? (-1f) : _strongestContact.RequiredPadSize().x` |
| `SizeZ` | `if (!DishScannedContacts.TryGetData(_strongestContact, out data)) { return -1.0; } return (data.CurrentTimeTillResolve > 0f) ? (-1f) : _strongestContact.RequiredPadSize().y` |
| `TargetPadIndex` | `return _targetPadIndex` |
| `Vertical` | `return Vertical * MaximumVertical` |
| `VerticalRatio` | `return Vertical` |
| `WattsReachingContact` | `if (!DishScannedContacts.TryGetData(_strongestContact, out data)) { return -1.0; } return (data.CurrentTimeTillResolve > 0f) ? (-1f) : GetWattageOnContact(_strongestContact)` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `BestContactFilter` | `_bestContactFilterReferenceID = (long)value` |
| `Horizontal` | `{ value = RocketMath.ModuloCorrect(value, MaximumHorizontal); double num = value / MaximumHorizontal; if (!RocketMath.Approximately(num, RotatableBehaviour.TargetHorizontal, Rotati...` |
| `HorizontalRatio` | `value = RocketMath.ModuloCorrect(value, 1.0)` |
| `Setting` | `Setting = Mathf.Clamp((int)value, minWattage, maxWattage)` |
| `TargetPadIndex` | `if (value < 0.0) { value = 0.0; } _targetPadIndex = (int)value` |
| `Vertical` | `{ if (value < 0.0) { value = 0.0; } if (value > MaximumVertical) { value = MaximumVertical; } double num = value / MaximumVertical; if (!RocketMath.Approximately(num, RotatableBeha...` |
| `VerticalRatio` | `if (value < 0.0) { value = 0.0; } if (value > 1.0) { value = 1.0; } if (!RocketMath.Approximately(value, RotatableBehaviour.TargetVertical, RotationTolerance)) { RotatableBehaviour...` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `BestContactFilter` | `return true` |
| `ContactSlotIndex` | `return true` |
| `ContactTypeId` | `return true` |
| `Horizontal` | `return true` |
| `Idle` | `return true` |
| `InterrogationProgress` | `return true` |
| `MinimumWattsToContact` | `return true` |
| `Setting` | `return true` |
| `SignalID` | `return true` |
| `SignalStrength` | `return true` |
| `SizeX` | `return true` |
| `SizeZ` | `return true` |
| `TargetPadIndex` | `return true` |
| `Vertical` | `return true` |
| `WattsReachingContact` | `return true` |

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `BestContactFilter` | `return true` |
| `Horizontal` | `return true` |
| `Setting` | `return true` |
| `TargetPadIndex` | `return true` |
| `Vertical` | `return true` |

---

### `Silo`

**File**: `Assets.Scripts.Objects.Chutes/Silo.cs` | **Extends**: `DeviceImportExport`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Dispense` | `_dispense ? 1.0 : 0.0` |
| `DispenseSlot` | `_dispenseSlot` |
| `Quantity` | `SiloThingQuantity` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Dispense` | `_dispense = value >= 1.0` |
| `DispenseSlot` | `_dispenseSlot = (int)value` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Dispense` | `true` |
| `DispenseSlot` | `true` |
| `Quantity` | `true` |

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Dispense` | `true` |
| `DispenseSlot` | `true` |

---

### `SimpleFabricatorBase`

**File**: `Assets.Scripts.Objects.Electrical/SimpleFabricatorBase.cs` | **Extends**: `FabricatorBase`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `CompletionRatio` | `Mathf.Clamp01((float)(int)Processing / 100f)` |
| `RecipeHash` | `CurrentHash` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `CompletionRatio` | `true` |
| `RecipeHash` | `true` |

---

### `SlotHandlerBase`

**File**: `Assets.Scripts.Objects.Electrical/SlotHandlerBase.cs` | **Extends**: `DeviceImportExport`

---

### `SolarPanel`

**File**: `Assets.Scripts.Objects.Electrical/SolarPanel.cs` | **Extends**: `Electrical`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Charge` | `GenerationRate` |
| `Horizontal` | `Horizontal * MaximumHorizontal` |
| `HorizontalRatio` | `Horizontal` |
| `Maximum` | `PowerGenerated()` |
| `Ratio` | `GenerationEfficiency` |
| `Vertical` | `Mathf.Lerp((float)MinimumVertical, (float)MaximumVertical, (float)Vertical)` |
| `VerticalRatio` | `Vertical` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Horizontal` | `{ value = RocketMath.ModuloCorrect(value, MaximumHorizontal); double num = value / MaximumHorizontal; if (!RocketMath.Approximately(num, RotatableBehaviour.TargetHorizontal, Rotati...` |
| `HorizontalRatio` | `value = RocketMath.ModuloCorrect(value, 1.0)` |
| `Vertical` | `{ if (value <= MinimumVertical) { value = MinimumVertical; } if (value >= MaximumVertical) { value = MaximumVertical; } double num = RocketMath.MapToScale((float)MinimumVertical, (...` |
| `VerticalRatio` | `if (value < 0.0) { value = 0.0; } if (value > 1.0) { value = 1.0; } if (!RocketMath.Approximately(value, RotatableBehaviour.TargetVertical, RotationTolerance)) { RotatableBehaviour...` |

---

### `SolidFuelGenerator`

**File**: `Assets.Scripts.Objects.Electrical/SolidFuelGenerator.cs` | **Extends**: `PowerGeneratorSlot`

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Mode` | `false` |
| `PowerGeneration` | `true` |

---

### `Sorter`

**File**: `Assets.Scripts.Objects.Electrical/Sorter.cs` | **Extends**: `DeviceImportExport2`

---

### `SpawnPointAtmospherics`

**File**: `Assets.Scripts.Objects.Electrical/SpawnPointAtmospherics.cs` | **Extends**: `DeviceInputOutput`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `EntityState` | `SleeperSlot.Contains<Entity>(out occupant) ? ((double)(int)occupant.State) : (-1.0)` |
| `HealthDamage` | `SleeperSlot.Contains<Entity>(out occupant2) ? ((double)occupant2.DamageState.Total) : (-1.0)` |
| `StunDamage` | `SleeperSlot.Contains<Entity>(out occupant3) ? ((double)occupant3.DamageState.Stun) : (-1.0)` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `EntityState` | `true` |
| `HealthDamage` | `true` |
| `StunDamage` | `true` |

---

### `Speaker`

**File**: `Assets.Scripts.Objects.Electrical/Speaker.cs` | **Extends**: `LogicUnitBase`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `SoundAlert` | `Mode` |
| `Volume` | `Volume` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `SoundAlert` | `OnServer.Interact(base.InteractMode, Mathf.Clamp((int)value, 0, EnumCollections.SpeakerSounds.Length - 1))` |
| `Volume` | `Volume = (int)(byte)Mathf.Clamp((int)value, 1, 200)` |

---

### `Stacker`

**File**: `Assets.Scripts.Objects.Electrical/Stacker.cs` | **Extends**: `SlotHandlerBase`

---

### `StateChangeDevice`

**File**: `Assets.Scripts.Objects.Pipes/StateChangeDevice.cs` | **Extends**: `SettableAtmosDevice`

---

### `StepUnit`

**File**: `Assets.Scripts.Objects.Items/StepUnit.cs` | **Extends**: `SmallDevice`

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Activate` | `if (value > 0.0) { this.OnPlayStepManual(StepData()); ResetActivateButton().Forget(); } break` |
| `Volume` | `Volume = Mathf.Clamp((int)value, 0, MaxMidiValue)` |

---

### `StirlingEngine`

**File**: `Assets.Scripts.Objects.Electrical/StirlingEngine.cs` | **Extends**: `DeviceInputOutput`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Combustion` | `if (!atmosphere.Sparked) { return 0.0; } return 1.0` |
| `EnvironmentEfficiency` | `return _machineEnvironmentEfficiency` |
| `PowerGeneration` | `return EnergyAsPower.ToDouble()` |
| `Pressure` | `return atmosphere?.PressureGassesAndLiquids.ToDouble() ?? 0.0` |
| `Quantity` | `if (atmosphere == null) { return 0.0; } return (atmosphere.TotalMoles + _hotSideAtmosphere.TotalMoles + _coldSideAtmosphere.TotalMoles).ToDouble()` |
| `RatioCarbonDioxide` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioHelium` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioHydrazine` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioHydrochloricAcid` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioHydrogen` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioLiquidAlcohol` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioLiquidCarbonDioxide` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioLiquidHydrazine` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioLiquidHydrochloricAcid` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioLiquidHydrogen` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioLiquidMethane` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioLiquidNitrogen` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioLiquidNitrousOxide` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioLiquidOxygen` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioLiquidOzone` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioLiquidPollutant` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioLiquidSilanol` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioLiquidSodiumChloride` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioMethane` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioNitrogen` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioNitrousOxide` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioOxygen` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioOzone` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioPollutant` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioPollutedWater` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioSilanol` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioSteam` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `RatioWater` | `if (atmosphere == null) { return 0.0; } return AtmosphereHelper.GasRatio(logicType, atmosphere)` |
| `Temperature` | `return atmosphere?.Temperature.ToDouble() ?? 0.0` |
| `WorkingGasEfficiency` | `return _workingGasEfficiency` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Combustion` | `return true` |
| `EnvironmentEfficiency` | `return true` |
| `PowerGeneration` | `return true` |
| `Pressure` | `return true` |
| `Quantity` | `return true` |
| `RatioCarbonDioxide` | `return true` |
| `RatioHelium` | `return true` |
| `RatioHydrazine` | `return true` |
| `RatioHydrochloricAcid` | `return true` |
| `RatioHydrogen` | `return true` |
| `RatioLiquidAlcohol` | `return true` |
| `RatioLiquidCarbonDioxide` | `return true` |
| `RatioLiquidHydrazine` | `return true` |
| `RatioLiquidHydrochloricAcid` | `return true` |
| `RatioLiquidHydrogen` | `return true` |
| `RatioLiquidMethane` | `return true` |
| `RatioLiquidNitrogen` | `return true` |
| `RatioLiquidNitrousOxide` | `return true` |
| `RatioLiquidOxygen` | `return true` |
| `RatioLiquidOzone` | `return true` |
| `RatioLiquidPollutant` | `return true` |
| `RatioLiquidSilanol` | `return true` |
| `RatioLiquidSodiumChloride` | `return true` |
| `RatioMethane` | `return true` |
| `RatioNitrogen` | `return true` |
| `RatioNitrousOxide` | `return true` |
| `RatioOxygen` | `return true` |
| `RatioOzone` | `return true` |
| `RatioPollutant` | `return true` |
| `RatioPollutedWater` | `return true` |
| `RatioSilanol` | `return true` |
| `RatioSteam` | `return true` |
| `RatioWater` | `return true` |
| `Temperature` | `return true` |
| `Volume` | `return true` |
| `WorkingGasEfficiency` | `return true` |

---

### `StructureDrinkingFountain`

**File**: `Objects.Structures/StructureDrinkingFountain.cs` | **Extends**: `DeviceInput`

---

### `StructurePoweredShower`

**File**: `Objects.Structures/StructurePoweredShower.cs` | **Extends**: `StructureShower`

---

### `StructureToilet`

**File**: `Objects.Structures/StructureToilet.cs` | **Extends**: `WaterDevice`

---

### `SuitBase`

**File**: `Assets.Scripts.Objects.Clothing/SuitBase.cs` | **Extends**: `AtmosphericItem`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `AirRelease` | `return Importing` |
| `EntityState` | `if ((object)base.ParentSlot?.Parent == null \|\| base.ParentSlot.Type != Slot.Class.Suit) { return -1.0; } return ((int?)ParentEntity?.State) ?? (-1)` |
| `Filtration` | `return Exporting` |
| `ForwardX` | `return RootParentHuman ? RootParentHuman.EntityForward.x : Forward.x` |
| `ForwardY` | `return RootParentHuman ? RootParentHuman.EntityForward.y : Forward.y` |
| `ForwardZ` | `return RootParentHuman ? RootParentHuman.EntityForward.z : Forward.z` |
| `Orientation` | `return Orientation` |
| `PositionX` | `return RootParent.Position.x` |
| `PositionY` | `return RootParent.Position.y` |
| `PositionZ` | `return RootParent.Position.z` |
| `PressureExternal` | `return base.WorldAtmosphere?.PressureGassesAndLiquids.ToDouble() ?? 0.0` |
| `PressureSetting` | `return OutputSetting` |
| `Setting` | `return Setting` |
| `SoundAlert` | `return (int)SoundAlert` |
| `TemperatureExternal` | `return base.WorldAtmosphere?.Temperature.ToDouble() ?? 0.0` |
| `TemperatureSetting` | `return OutputTemperature.ToDouble()` |
| `VelocityMagnitude` | `return base.VelocityMagnitude` |
| `VelocityRelativeX` | `return RootParentHuman ? RootParentHuman.RelativeVelocity.x : RelativeVelocity.x` |
| `VelocityRelativeY` | `return RootParentHuman ? RootParentHuman.RelativeVelocity.y : RelativeVelocity.y` |
| `VelocityRelativeZ` | `return RootParentHuman ? RootParentHuman.RelativeVelocity.z : RelativeVelocity.z` |
| `VelocityX` | `return RootParentHuman ? RootParentHuman.Velocity.x : base.Velocity.x` |
| `VelocityY` | `return RootParentHuman ? RootParentHuman.Velocity.y : base.Velocity.y` |
| `VelocityZ` | `return RootParentHuman ? RootParentHuman.Velocity.z : base.Velocity.z` |
| `Volume` | `return (int)SoundVolume` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `AirRelease` | `OnServer.Interact(base.InteractImport, (int)value)` |
| `Error` | `OnServer.Interact(base.InteractError, (int)value)` |
| `Filtration` | `OnServer.Interact(base.InteractExport, (int)value)` |
| `PressureSetting` | `OutputSetting = (float)value` |
| `Setting` | `Setting = value` |
| `SoundAlert` | `SoundAlert = (byte)Mathf.Clamp((int)value, 0, EnumCollections.SpeakerSounds.Length - 1)` |
| `TemperatureSetting` | `OutputTemperature = new TemperatureKelvin(value)` |
| `Volume` | `SoundVolume = (byte)Mathf.Clamp((int)value, 1, 100)` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `AirRelease` | `return true` |
| `EntityState` | `return true` |
| `Filtration` | `return true` |
| `ForwardX` | `return true` |
| `ForwardY` | `return true` |
| `ForwardZ` | `return true` |
| `Orientation` | `return true` |
| `PositionX` | `return true` |
| `PositionY` | `return true` |
| `PositionZ` | `return true` |
| `PressureExternal` | `return true` |
| `PressureSetting` | `return true` |
| `Setting` | `return true` |
| `SoundAlert` | `return true` |
| `TemperatureExternal` | `return true` |
| `TemperatureSetting` | `return true` |
| `VelocityMagnitude` | `return true` |
| `VelocityRelativeX` | `return true` |
| `VelocityRelativeY` | `return true` |
| `VelocityRelativeZ` | `return true` |
| `VelocityX` | `return true` |
| `VelocityY` | `return true` |
| `VelocityZ` | `return true` |
| `Volume` | `return true` |

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `AirRelease` | `return true` |
| `Error` | `return true` |
| `Filtration` | `return true` |
| `PressureSetting` | `return true` |
| `Setting` | `return true` |
| `SoundAlert` | `return true` |
| `TemperatureSetting` | `return true` |
| `Volume` | `return true` |

---

### `SurvivalToolbelt`

**File**: `Assets.Scripts.Objects/SurvivalToolbelt.cs` | **Extends**: `ToolBelt`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `EntityState` | `if (!(base.ParentSlot?.Parent is Entity entity3)) { return -1.0; } return (int)entity3.State` |
| `HealthDamage` | `if (!(base.ParentSlot?.Parent is Entity entity2)) { return -1.0; } return (int)entity2.DamageState.Total` |
| `SoundAlert` | `return (int)SoundAlert` |
| `StunDamage` | `if (!(base.ParentSlot?.Parent is Entity entity)) { return -1.0; } return (int)entity.DamageState.Stun` |
| `Volume` | `return (int)SoundVolume` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `SoundAlert` | `SoundAlert = (byte)Mathf.Clamp((int)value, 0, EnumCollections.SpeakerSounds.Length - 1)` |
| `Volume` | `SoundVolume = (byte)Mathf.Clamp((int)value, 0, 100)` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `EntityState` | `return true` |
| `HealthDamage` | `return true` |
| `SoundAlert` | `return true` |
| `StunDamage` | `return true` |
| `Volume` | `return true` |

---

### `Tank`

**File**: `Assets.Scripts.Objects.Pipes/Tank.cs` | **Extends**: `DeviceInternal`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Combustion` | `if (!base.InternalAtmosphere.Sparked) { return 0.0; } return 1.0` |
| `Pressure` | `return base.InternalAtmosphere.PressureGassesAndLiquids.ToDouble()` |
| `Quantity` | `return base.InternalAtmosphere.TotalMoles.ToDouble()` |
| `RatioCarbonDioxide` | `return GasRatio(logicType)` |
| `RatioHelium` | `return GasRatio(logicType)` |
| `RatioHydrazine` | `return GasRatio(logicType)` |
| `RatioHydrochloricAcid` | `return GasRatio(logicType)` |
| `RatioHydrogen` | `return GasRatio(logicType)` |
| `RatioLiquidAlcohol` | `return GasRatio(logicType)` |
| `RatioLiquidCarbonDioxide` | `return GasRatio(logicType)` |
| `RatioLiquidHydrazine` | `return GasRatio(logicType)` |
| `RatioLiquidHydrochloricAcid` | `return GasRatio(logicType)` |
| `RatioLiquidHydrogen` | `return GasRatio(logicType)` |
| `RatioLiquidMethane` | `return GasRatio(logicType)` |
| `RatioLiquidNitrogen` | `return GasRatio(logicType)` |
| `RatioLiquidNitrousOxide` | `return GasRatio(logicType)` |
| `RatioLiquidOxygen` | `return GasRatio(logicType)` |
| `RatioLiquidOzone` | `return GasRatio(logicType)` |
| `RatioLiquidPollutant` | `return GasRatio(logicType)` |
| `RatioLiquidSilanol` | `return GasRatio(logicType)` |
| `RatioLiquidSodiumChloride` | `return GasRatio(logicType)` |
| `RatioMethane` | `return GasRatio(logicType)` |
| `RatioNitrogen` | `return GasRatio(logicType)` |
| `RatioNitrousOxide` | `return GasRatio(logicType)` |
| `RatioOxygen` | `return GasRatio(logicType)` |
| `RatioOzone` | `return GasRatio(logicType)` |
| `RatioPollutant` | `return GasRatio(logicType)` |
| `RatioPollutedWater` | `return GasRatio(logicType)` |
| `RatioSilanol` | `return GasRatio(logicType)` |
| `RatioSteam` | `return GasRatio(logicType)` |
| `RatioWater` | `return GasRatio(logicType)` |
| `Temperature` | `return base.InternalAtmosphere.Temperature.ToDouble()` |
| `TotalMoles` | `return base.InternalAtmosphere.TotalMoles.ToDouble()` |
| `Volume` | `return base.InternalAtmosphere.Volume.ToDouble()` |
| `VolumeOfLiquid` | `return base.InternalAtmosphere.TotalVolumeLiquids.ToDouble()` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Combustion` | `return true` |
| `Pressure` | `return true` |
| `RatioCarbonDioxide` | `return true` |
| `RatioHelium` | `return true` |
| `RatioHydrazine` | `return true` |
| `RatioHydrochloricAcid` | `return true` |
| `RatioHydrogen` | `return true` |
| `RatioLiquidAlcohol` | `return true` |
| `RatioLiquidCarbonDioxide` | `return true` |
| `RatioLiquidHydrazine` | `return true` |
| `RatioLiquidHydrochloricAcid` | `return true` |
| `RatioLiquidHydrogen` | `return true` |
| `RatioLiquidMethane` | `return true` |
| `RatioLiquidNitrogen` | `return true` |
| `RatioLiquidNitrousOxide` | `return true` |
| `RatioLiquidOxygen` | `return true` |
| `RatioLiquidOzone` | `return true` |
| `RatioLiquidPollutant` | `return true` |
| `RatioLiquidSilanol` | `return true` |
| `RatioLiquidSodiumChloride` | `return true` |
| `RatioMethane` | `return true` |
| `RatioNitrogen` | `return true` |
| `RatioNitrousOxide` | `return true` |
| `RatioOxygen` | `return true` |
| `RatioOzone` | `return true` |
| `RatioPollutant` | `return true` |
| `RatioPollutedWater` | `return true` |
| `RatioSilanol` | `return true` |
| `RatioSteam` | `return true` |
| `RatioWater` | `return true` |
| `Temperature` | `return true` |
| `TotalMoles` | `return true` |
| `Volume` | `return true` |
| `VolumeOfLiquid` | `return true` |

---

### `Transformer`

**File**: `Assets.Scripts.Objects.Electrical/Transformer.cs` | **Extends**: `ElectricalInputOutput`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Maximum` | `OutputMaximum` |
| `Ratio` | `Setting / (double)OutputMaximum` |
| `Setting` | `Setting` |

---

### `TriggerPlate`

**File**: `Assets.Scripts.Objects.Electrical/TriggerPlate.cs` | **Extends**: `LogicInputBase`

---

### `TurbineGenerator`

**File**: `Assets.Scripts.Objects.Electrical/TurbineGenerator.cs` | **Extends**: `Device`

---

### `VendingMachine`

**File**: `Assets.Scripts.Objects.Electrical/VendingMachine.cs` | **Extends**: `DeviceImportExport`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `DispenseSlot` | `return _dispenseSlot` |
| `Quantity` | `return _filledSlots` |
| `Ratio` | `return (float)_filledSlots / 100f` |
| `RequestHash` | `return RequestedHash` |
| `TargetPrefabHash` | `{ if (CurrentIndex == -1) { return 0.0; } if (CurrentIndex >= Slots.Count) { return 0.0; } DynamicThing occupant; return Slots[CurrentIndex].Contains<DynamicThing>(out occupant) ? ...` |
| `TargetSlotIndex` | `return CurrentIndex` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `DispenseSlot` | `_dispenseSlot = (int)value` |
| `RequestHash` | `if (!RequestTask.Initialized) { RequestTask.Initialize(); SetRequestFromHashTask(RequestTask.Token, (int)value).Forget(); } break` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `DispenseSlot` | `return true` |
| `Quantity` | `return true` |
| `Ratio` | `return true` |
| `RequestHash` | `return true` |
| `TargetPrefabHash` | `return true` |
| `TargetSlotIndex` | `return true` |

---

### `VendingMachineRefrigerated`

**File**: `Assets.Scripts.Objects.Electrical/VendingMachineRefrigerated.cs` | **Extends**: `VendingMachine`

---

### `WaterPurifier`

**File**: `Assets.Scripts.Objects.Pipes/WaterPurifier.cs` | **Extends**: `DeviceInputOutputImport`

---

### `WeatherStation`

**File**: `Assets.Scripts.Objects/WeatherStation.cs` | **Extends**: `Device`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `NextWeatherEventTime` | `if (!IsValid) { return 0.0; } return WeatherManager.GetSecondsWhenNextWeatherEventIsActive()` |
| `NextWeatherHash` | `if (!IsValid) { return 0.0; } return WeatherManager.CurrentWeatherEvent?.IdHash ?? 0` |

---

### `WindTurbineGenerator`

**File**: `Objects/WindTurbineGenerator.cs` | **Extends**: `Device`

---

### `WirelessPower`

**File**: `Assets.Scripts.Objects.Electrical/WirelessPower.cs` | **Extends**: `ElectricalInputOutput`

**GetLogicValue (read: what each LogicType returns)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Charge` | `AvailablePower` |
| `Horizontal` | `Horizontal * MaximumHorizontal` |
| `HorizontalRatio` | `Horizontal` |
| `PositionX` | `RayPosition.x` |
| `PositionY` | `RayPosition.y` |
| `PositionZ` | `RayPosition.z` |
| `PowerActual` | `base.CurrentLoad` |
| `PowerPotential` | `base.PotentialLoad` |
| `Vertical` | `Vertical * MaximumVertical` |
| `VerticalRatio` | `Vertical` |

**SetLogicValue (write: what each LogicType does when set)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Horizontal` | `{ value = RocketMath.ModuloCorrect(value, MaximumHorizontal); double num = value / MaximumHorizontal; if (!RocketMath.Approximately(num, RotatableBehaviour.TargetHorizontal, Rotati...` |
| `HorizontalRatio` | `value = RocketMath.ModuloCorrect(value, 1.0)` |
| `Vertical` | `{ if (value < 0.0) { value = 0.0; } if (value > MaximumVertical) { value = MaximumVertical; } double num = value / MaximumVertical; if (!RocketMath.Approximately(num, RotatableBeha...` |
| `VerticalRatio` | `if (value < 0.0) { value = 0.0; } if (value > 1.0) { value = 1.0; } if (!RocketMath.Approximately(value, RotatableBehaviour.TargetVertical, RotationTolerance)) { RotatableBehaviour...` |

**CanLogicRead (extra read-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Charge` | `return true` |
| `Horizontal` | `return true` |
| `PositionX` | `return true` |
| `PositionY` | `return true` |
| `PositionZ` | `return true` |
| `PowerActual` | `return true` |
| `PowerPotential` | `return true` |
| `Vertical` | `return true` |

**CanLogicWrite (extra write-gating logic beyond the base)**

| LogicType | Expression (decompiled, ground truth) |
|---|---|
| `Horizontal` | `return true` |
| `Mode` | `return false` |
| `Vertical` | `return true` |

---

