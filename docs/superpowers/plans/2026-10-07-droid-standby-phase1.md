# Salty's Droid Standby — Phase 1 (Standby Core) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship Power Save Mode and Deep Standby for H.E.M. Droids — cognition-floor visuals/slowdown, scaled battery drain, reduced jump, player-picked wake conditions with an ImGui panel, and the AFK safety net — working in single-player and multiplayer, with no Time Skip (phases 2–3).

**Architecture:** Pure, Unity-free logic (`src/`: key gestures, level transitions, wake evaluation, safety-net rule, battery math) is test-driven in a net9.0 xunit project, exactly like the airlock mod's `FailsafeController`. A thin BepInEx/Harmony layer (`SaltysDroidStandby/`) reads the game into snapshots, applies effects server-side (stun floor on `Brain.OnLifeTick`, drain refund around `Human.OnLifeTick`) and client-side (jump on `MovementController.HandleJump`, input/wake/safety net in the plugin's `Update`, panel on `ImGuiWindowManager.Draw`). Clients tell the server their level with one LaunchPadBooster message.

**Tech Stack:** C# 9 / .NET Framework 4.7.2 (mod), .NET 9 + xunit (tests), BepInEx 5, HarmonyX, StationeersLaunchPad 1.0 / LaunchPadBooster, RG.ImGui (`ImGuiNET`), MSBuild 2022 Build Tools.

**Spec:** `docs/superpowers/specs/2026-10-07-droid-standby-design.md` (same branch). Read it first; section numbers below (§) refer to it.

## Global Constraints

- Branch `droid-standby-mod`; mod folder `droid-standby-mod/`; plugin GUID `com.naclie88.SaltysDroidStandby`; display name `Salty's Droid Standby`; version `0.1`.
- Mod targets `net472`, `LangVersion 9.0` — **no file-scoped namespaces, no records, no `init`**, block-scoped namespaces only (files in `src/` are compiled by both projects).
- `src/` files must not reference Unity, BepInEx, Harmony or game assemblies.
- Droids only for Power Save / Deep Standby (`human.IsArtificial`).
- Levels: tap = Power Save, long press (default 0.6 s) = Deep Standby; any press in Deep wakes to Normal (§5.1). Default key `Z`.
- Defaults: Power Save stun floor 40, drain ×0.5; Deep stun floor 85, drain ×0.25; jump factor = speed factor `1 − 0.9 × floor/100`.
- Never modify the static `Human.PowerDrainedPerTick` (§4.2). Never change which battery is drained.
- Standby state is never saved; load always starts at Normal.
- Safety net: battery ≤ 10 % and no input ≥ 60 s → Deep Standby; **effectively solo** (true single-player, or a host who is the only connected player) also pauses (`WorldManager.SetGamePause(true)`), and a join ends that pause; otherwise multiplayer never pauses (§9).
- No Time Skip code in this phase (§16).
- `MOD.Networking.Required = true` (§10).
- Every commit message ends with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Git on this machine: always `git -c core.longpaths=true …` inside the scratchpad worktree (MAX_PATH on deep repo files).
- Build: `"/c/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/MSBuild.exe" <csproj> //p:Configuration=Debug //nologo //v:minimal`. Install only via `build-and-install.sh` and only with Stationeers closed.

## Review Focus

1. **Entering standby during the day / with a full battery** — a reasonable player expects "wake at sunrise" not to fire instantly at noon. Threshold conditions are *armed* only after being seen on the far side of the threshold. Pinned in Task 3 (`Light_alreadyBright_atEntry_doesNotWakeUntilItDipsAndRecovers`, `BatteryCharged_alreadyFull_atEntry_doesNotWake`).
2. **Automatic wake at low battery looping with the safety net** — after a "battery dropped to 5 %" wake, an AFK player must not be thrown straight back into standby every second. Safety net is suppressed after any automatic wake until real input. Pinned in Task 4 (`SuppressedAfterAutoWake_untilInput`).
3. **Real stun on top of the floor** — damage/empty battery stun above the floor must still show and never be lowered by the mod; entering a sleeper/bed or dying clears standby. Pinned in Task 7 (code: floor only raises; clear on death / life suspender) and the Task 12 in-game checklist.
4. **Standby key while typing in chat/console or paused** — must do nothing. Pinned in Task 9 (`KeyManager.InputState == KeyInputState.Game` gate) and Task 12 checklist.
5. **No battery fitted / zero-capacity battery** — readings and safety net must not divide by zero or throw. Pinned in Task 2 (`NoBatteries_returnsZero`, `ZeroCapacityCell_isIgnored`).

---

## File Structure

```
droid-standby-mod/
  About/About.xml                         LaunchPad manifest
  build-and-install.sh                    build + deploy to mods/SaltysDroidStandby + hash verify
  .gitattributes                          *.sh LF
  README.md / UpdateNotes.md              user docs / running dev log
  src/                                    PURE logic (no Unity) — compiled into mod AND tests
    StandbyTypes.cs                       StandbyLevel, Gesture, WakeCondition enums
    PressDetector.cs                      tap vs long press from key up/down + time
    LevelTransitions.cs                   (level, gesture) -> next level
    BatteryMath.cs                        summed battery ratio
    WakeConditions.cs                     WorldSnapshot, WakeThresholds, WakeEvaluator
    SafetyNetLogic.cs                     should the safety net fire?
  SaltysDroidStandby/
    SaltysDroidStandby.csproj / .sln / .gitignore / .gitattributes / Properties/AssemblyInfo.cs
    SaltysDroidStandby.cs                 plugin: Mod, config, patching, per-frame driver
    StandbyConfig.cs                      ConfigEntries + null-safe accessors + per-level factors
    StandbyRegistry.cs                    per-Human level (server truth / client mirror)
    StandbyRequestMessage.cs              client -> host level request; StandbyNetwork.Request
    Patches/CognitionFloorPatch.cs        Brain.OnLifeTick postfix: hold stun floor, auto-clear
    Patches/DrainPatch.cs                 Human.OnLifeTick prefix/postfix: refund unspent drain
    Patches/JumpPatch.cs                  MovementController.HandleJump: scale jumpForce
    Game/WorldReadings.cs                 game state -> WorldSnapshot
    Game/LocalController.cs               input, wake checks, safety net, mirror reset (client)
    UI/WakePanel.cs                       ImGui panel + status line + ImGuiWindowManager.Draw postfix
  tests/SaltysDroidStandby.Tests/
    SaltysDroidStandby.Tests.csproj       net9.0 xunit, links ../../src/*.cs
    PressDetectorTests.cs, LevelTransitionsTests.cs, BatteryMathTests.cs,
    WakeConditionsTests.cs, SafetyNetLogicTests.cs
```

Work in the worktree `…\scratchpad\wt-standby` on branch `droid-standby-mod` (it already holds the spec). Paths below are relative to that worktree root.

---

### Task 1: Scaffold, pure types, gesture detection and level transitions

**Files:**
- Create: `droid-standby-mod/src/StandbyTypes.cs`, `droid-standby-mod/src/PressDetector.cs`, `droid-standby-mod/src/LevelTransitions.cs`
- Create: `droid-standby-mod/tests/SaltysDroidStandby.Tests/SaltysDroidStandby.Tests.csproj`, `PressDetectorTests.cs`, `LevelTransitionsTests.cs`
- Create: `droid-standby-mod/.gitattributes`

**Interfaces:**
- Produces: `enum StandbyLevel : byte { Normal = 0, PowerSave = 1, Deep = 2 }`, `enum Gesture { None, Tap, LongPress }`, `[Flags] enum WakeCondition { None=0, Light=1, Wind=2, Storm=4, Battery=8, Danger=16 }` (namespace `SaltysDroidStandby`); `PressDetector(float longPressSeconds)` with `float LongPressSeconds { get; set; }` and `Gesture Update(bool isDown, float now)`; `static StandbyLevel LevelTransitions.Next(StandbyLevel current, Gesture gesture)`.

- [ ] **Step 1: Create the test project and `.gitattributes`**

`droid-standby-mod/.gitattributes`:
```
# Bash refuses CRLF scripts; keep shell scripts LF on every checkout.
*.sh text eol=lf
```

`droid-standby-mod/tests/SaltysDroidStandby.Tests/SaltysDroidStandby.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <!-- Logic-only tests. src/ has zero Unity/BepInEx/game dependencies, so it compiles
       and runs identically here. Not part of the shipped mod build. -->
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <IsPackable>false</IsPackable>
    <RootNamespace>SaltysDroidStandby.Tests</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include="..\..\src\*.cs" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Write the failing tests**

`PressDetectorTests.cs`:
```csharp
using Xunit;

namespace SaltysDroidStandby.Tests
{
    public class PressDetectorTests
    {
        [Fact]
        public void QuickPressAndRelease_isTap()
        {
            var d = new PressDetector(0.6f);
            Assert.Equal(Gesture.None, d.Update(true, 0f));
            Assert.Equal(Gesture.None, d.Update(true, 0.2f));
            Assert.Equal(Gesture.Tap, d.Update(false, 0.3f));
        }

        [Fact]
        public void Hold_firesLongPressOnce_whileStillHeld_andNoTapOnRelease()
        {
            var d = new PressDetector(0.6f);
            d.Update(true, 0f);
            Assert.Equal(Gesture.LongPress, d.Update(true, 0.6f));
            Assert.Equal(Gesture.None, d.Update(true, 1.5f));
            Assert.Equal(Gesture.None, d.Update(false, 2f));
        }

        [Fact]
        public void NoInput_isNone()
        {
            var d = new PressDetector(0.6f);
            Assert.Equal(Gesture.None, d.Update(false, 0f));
            Assert.Equal(Gesture.None, d.Update(false, 5f));
        }

        [Fact]
        public void ThresholdChange_appliesToNextPress()
        {
            var d = new PressDetector(0.6f) { LongPressSeconds = 1.0f };
            d.Update(true, 0f);
            Assert.Equal(Gesture.None, d.Update(true, 0.8f));
            Assert.Equal(Gesture.LongPress, d.Update(true, 1.0f));
        }
    }
}
```

`LevelTransitionsTests.cs`:
```csharp
using Xunit;

namespace SaltysDroidStandby.Tests
{
    public class LevelTransitionsTests
    {
        [Theory]
        [InlineData(StandbyLevel.Normal, Gesture.Tap, StandbyLevel.PowerSave)]
        [InlineData(StandbyLevel.Normal, Gesture.LongPress, StandbyLevel.Deep)]
        [InlineData(StandbyLevel.PowerSave, Gesture.Tap, StandbyLevel.Normal)]
        [InlineData(StandbyLevel.PowerSave, Gesture.LongPress, StandbyLevel.Deep)]
        [InlineData(StandbyLevel.Deep, Gesture.Tap, StandbyLevel.Normal)]
        [InlineData(StandbyLevel.Deep, Gesture.LongPress, StandbyLevel.Normal)]
        [InlineData(StandbyLevel.PowerSave, Gesture.None, StandbyLevel.PowerSave)]
        [InlineData(StandbyLevel.Deep, Gesture.None, StandbyLevel.Deep)]
        public void Table_matchesSpec(StandbyLevel from, Gesture g, StandbyLevel expected)
        {
            Assert.Equal(expected, LevelTransitions.Next(from, g));
        }
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `cd droid-standby-mod/tests/SaltysDroidStandby.Tests && dotnet test --nologo`
Expected: build FAILS — `PressDetector`, `Gesture`, `StandbyLevel`, `LevelTransitions` not found.

- [ ] **Step 4: Write the implementation**

`droid-standby-mod/src/StandbyTypes.cs`:
```csharp
using System;

namespace SaltysDroidStandby
{
    // Wire value is the byte; never reorder (sent in StandbyRequestMessage).
    public enum StandbyLevel : byte
    {
        Normal = 0,
        PowerSave = 1,
        Deep = 2,
    }

    public enum Gesture
    {
        None,
        Tap,
        LongPress,
    }

    [Flags]
    public enum WakeCondition
    {
        None = 0,
        Light = 1,
        Wind = 2,
        Storm = 4,
        Battery = 8,
        Danger = 16,
    }
}
```

`droid-standby-mod/src/PressDetector.cs`:
```csharp
namespace SaltysDroidStandby
{
    // One key, two gestures (spec §5.1): released before LongPressSeconds = Tap; held to
    // LongPressSeconds = LongPress, fired once while still held (no Tap on that release).
    public sealed class PressDetector
    {
        private bool _wasDown;
        private float _downAt;
        private bool _longFired;

        public PressDetector(float longPressSeconds)
        {
            LongPressSeconds = longPressSeconds;
        }

        public float LongPressSeconds { get; set; }

        public Gesture Update(bool isDown, float now)
        {
            if (isDown && !_wasDown)
            {
                _wasDown = true;
                _downAt = now;
                _longFired = false;
                return Gesture.None;
            }
            if (isDown)
            {
                if (!_longFired && now - _downAt >= LongPressSeconds)
                {
                    _longFired = true;
                    return Gesture.LongPress;
                }
                return Gesture.None;
            }
            if (_wasDown)
            {
                _wasDown = false;
                return _longFired ? Gesture.None : Gesture.Tap;
            }
            return Gesture.None;
        }
    }
}
```

`droid-standby-mod/src/LevelTransitions.cs`:
```csharp
namespace SaltysDroidStandby
{
    // Spec §5.1 table. Any gesture in Deep Standby wakes to Normal.
    public static class LevelTransitions
    {
        public static StandbyLevel Next(StandbyLevel current, Gesture gesture)
        {
            if (gesture == Gesture.None)
            {
                return current;
            }
            switch (current)
            {
                case StandbyLevel.Normal:
                    return gesture == Gesture.Tap ? StandbyLevel.PowerSave : StandbyLevel.Deep;
                case StandbyLevel.PowerSave:
                    return gesture == Gesture.Tap ? StandbyLevel.Normal : StandbyLevel.Deep;
                default:
                    return StandbyLevel.Normal;
            }
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --nologo`
Expected: `Passed! - Failed: 0, Passed: 12`

- [ ] **Step 6: Commit**

```bash
git -c core.longpaths=true add droid-standby-mod
git -c core.longpaths=true commit -m "Add standby types, tap/long-press detector and level transitions (TDD)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: Summed battery ratio

**Files:**
- Create: `droid-standby-mod/src/BatteryMath.cs`
- Test: `droid-standby-mod/tests/SaltysDroidStandby.Tests/BatteryMathTests.cs`

**Interfaces:**
- Produces: `static float BatteryMath.TotalRatio(IEnumerable<KeyValuePair<float, float>> cells)` — each pair is (stored, maximum); returns 0..1; 0 when there is no usable capacity.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Collections.Generic;
using Xunit;

namespace SaltysDroidStandby.Tests
{
    public class BatteryMathTests
    {
        private static KeyValuePair<float, float> Cell(float stored, float max) =>
            new KeyValuePair<float, float>(stored, max);

        [Fact]
        public void SingleCell_isItsRatio()
        {
            Assert.Equal(0.25f, BatteryMath.TotalRatio(new[] { Cell(250f, 1000f) }), 3);
        }

        [Fact]
        public void TwoCells_areSummedByEnergy_notAveragedByRatio()
        {
            // 0 of 1000 + 3000 of 3000 = 3000 / 4000
            Assert.Equal(0.75f, BatteryMath.TotalRatio(new[] { Cell(0f, 1000f), Cell(3000f, 3000f) }), 3);
        }

        [Fact]
        public void NoBatteries_returnsZero()
        {
            Assert.Equal(0f, BatteryMath.TotalRatio(new KeyValuePair<float, float>[0]));
        }

        [Fact]
        public void ZeroCapacityCell_isIgnored()
        {
            Assert.Equal(0.5f, BatteryMath.TotalRatio(new[] { Cell(5f, 0f), Cell(50f, 100f) }), 3);
        }

        [Fact]
        public void Result_isClampedToOne()
        {
            Assert.Equal(1f, BatteryMath.TotalRatio(new[] { Cell(120f, 100f) }));
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --nologo`
Expected: FAIL — `BatteryMath` not found.

- [ ] **Step 3: Write the implementation**

```csharp
using System.Collections.Generic;

namespace SaltysDroidStandby
{
    // Spec §4.4: "battery" = energy summed over every battery-type slot (works with or
    // without the Dual Battery mod's second slot, no reference to it).
    public static class BatteryMath
    {
        public static float TotalRatio(IEnumerable<KeyValuePair<float, float>> cells)
        {
            float stored = 0f;
            float max = 0f;
            foreach (var cell in cells)
            {
                if (cell.Value <= 0f)
                {
                    continue;
                }
                stored += cell.Key < 0f ? 0f : cell.Key;
                max += cell.Value;
            }
            if (max <= 0f)
            {
                return 0f;
            }
            float ratio = stored / max;
            return ratio > 1f ? 1f : ratio;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --nologo` — Expected: `Failed: 0`, 17 passed.

- [ ] **Step 5: Commit**

```bash
git -c core.longpaths=true add droid-standby-mod
git -c core.longpaths=true commit -m "Add summed battery ratio (TDD)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Wake conditions

**Files:**
- Create: `droid-standby-mod/src/WakeConditions.cs`
- Test: `droid-standby-mod/tests/SaltysDroidStandby.Tests/WakeConditionsTests.cs`

**Interfaces:**
- Produces:
  - `struct WorldSnapshot { float LightPercent; float WindPercent; bool StormActive; bool SolarStormActive; bool Outdoors; float BatteryRatio; float BruteBurn; float PressureKpa; float TemperatureK; }` (public fields)
  - `class WakeThresholds` with public fields (defaults): `LightPercent = 20f`, `WindPercent = 40f`, `BatteryChargedRatio = 0.9f`, `BatteryLowRatio = 0.05f`, `DamageDelta = 1f`, `PressureDeltaKpa = 20f`, `SafeTempMinK = 223.15f`, `SafeTempMaxK = 323.15f`, `ConsecutiveChecks = 3`
  - `class WakeEvaluator(WakeCondition selected, WakeThresholds thresholds, WorldSnapshot atEntry)` with `WakeCondition Selected { get; }` and `string Check(WorldSnapshot now)` → human-readable reason, or `null` to keep sleeping.

Semantics (spec §6 + Review Focus 1):
- Light / Wind / Battery-charged: fire when ≥ threshold for `ConsecutiveChecks` checks in a row, but only once **armed** — armed when seen below the threshold (at entry or any later check).
- Battery-low: fire when ≤ `BatteryLowRatio` for `ConsecutiveChecks`, armed when seen above it.
- Storm: only while `Outdoors`; fires when `(StormActive, SolarStormActive)` differs from the entry state for `ConsecutiveChecks` checks; indoors resets the counter.
- Danger fires on the first check: `BruteBurn` rose by ≥ `DamageDelta` since the previous check, or `|PressureKpa − previous| ≥ PressureDeltaKpa`; temperature outside `[SafeTempMinK, SafeTempMaxK]` for `ConsecutiveChecks`, armed when seen inside the band.

- [ ] **Step 1: Write the failing tests**

```csharp
using Xunit;

namespace SaltysDroidStandby.Tests
{
    public class WakeConditionsTests
    {
        private static WorldSnapshot Night() => new WorldSnapshot
        {
            LightPercent = 0f, WindPercent = 10f, StormActive = false, SolarStormActive = false,
            Outdoors = true, BatteryRatio = 0.5f, BruteBurn = 0f, PressureKpa = 100f, TemperatureK = 290f,
        };

        private static string Run(WakeEvaluator e, WorldSnapshot s, int times)
        {
            string r = null;
            for (int i = 0; i < times; i++) r = e.Check(s);
            return r;
        }

        [Fact]
        public void Light_wakesAfterThreeConsecutiveBrightChecks()
        {
            var e = new WakeEvaluator(WakeCondition.Light, new WakeThresholds(), Night());
            var dawn = Night(); dawn.LightPercent = 25f;
            Assert.Null(e.Check(dawn));
            Assert.Null(e.Check(dawn));
            Assert.Contains("light", e.Check(dawn));
        }

        [Fact]
        public void Light_flicker_resetsTheCount()
        {
            var e = new WakeEvaluator(WakeCondition.Light, new WakeThresholds(), Night());
            var dawn = Night(); dawn.LightPercent = 25f;
            e.Check(dawn); e.Check(dawn);
            Assert.Null(e.Check(Night()));
            Assert.Null(e.Check(dawn));
        }

        [Fact]
        public void Light_alreadyBright_atEntry_doesNotWakeUntilItDipsAndRecovers()
        {
            var noon = Night(); noon.LightPercent = 90f;
            var e = new WakeEvaluator(WakeCondition.Light, new WakeThresholds(), noon);
            Assert.Null(Run(e, noon, 10));
            e.Check(Night());
            Assert.Contains("light", Run(e, noon, 3));
        }

        [Fact]
        public void Unselected_conditions_neverFire()
        {
            var e = new WakeEvaluator(WakeCondition.Battery, new WakeThresholds(), Night());
            var bright = Night(); bright.LightPercent = 100f; bright.WindPercent = 100f;
            Assert.Null(Run(e, bright, 10));
        }

        [Fact]
        public void Wind_wakesWhenAboveThreshold()
        {
            var e = new WakeEvaluator(WakeCondition.Wind, new WakeThresholds(), Night());
            var gusty = Night(); gusty.WindPercent = 60f;
            Assert.Contains("wind", Run(e, gusty, 3));
        }

        [Fact]
        public void BatteryCharged_wakesAt90Percent()
        {
            var e = new WakeEvaluator(WakeCondition.Battery, new WakeThresholds(), Night());
            var full = Night(); full.BatteryRatio = 0.92f;
            Assert.Contains("charged", Run(e, full, 3));
        }

        [Fact]
        public void BatteryCharged_alreadyFull_atEntry_doesNotWake()
        {
            var full = Night(); full.BatteryRatio = 1f;
            var e = new WakeEvaluator(WakeCondition.Battery, new WakeThresholds(), full);
            Assert.Null(Run(e, full, 10));
        }

        [Fact]
        public void BatteryLow_wakesAt5Percent()
        {
            var e = new WakeEvaluator(WakeCondition.Battery, new WakeThresholds(), Night());
            var low = Night(); low.BatteryRatio = 0.04f;
            Assert.Contains("low", Run(e, low, 3));
        }

        [Fact]
        public void Storm_startOutdoors_wakes()
        {
            var e = new WakeEvaluator(WakeCondition.Storm, new WakeThresholds(), Night());
            var storm = Night(); storm.StormActive = true;
            Assert.Contains("storm", Run(e, storm, 3));
        }

        [Fact]
        public void Storm_end_wakes()
        {
            var stormy = Night(); stormy.StormActive = true;
            var e = new WakeEvaluator(WakeCondition.Storm, new WakeThresholds(), stormy);
            Assert.Contains("storm", Run(e, Night(), 3));
        }

        [Fact]
        public void SolarStorm_start_wakes()
        {
            var e = new WakeEvaluator(WakeCondition.Storm, new WakeThresholds(), Night());
            var solar = Night(); solar.StormActive = true; solar.SolarStormActive = true;
            Assert.Contains("solar", Run(e, solar, 3));
        }

        [Fact]
        public void Storm_indoors_isIgnored()
        {
            var e = new WakeEvaluator(WakeCondition.Storm, new WakeThresholds(), Night());
            var inside = Night(); inside.StormActive = true; inside.Outdoors = false;
            Assert.Null(Run(e, inside, 10));
        }

        [Fact]
        public void Danger_damage_wakesImmediately()
        {
            var e = new WakeEvaluator(WakeCondition.Danger, new WakeThresholds(), Night());
            var hurt = Night(); hurt.BruteBurn = 5f;
            Assert.Contains("damage", e.Check(hurt));
        }

        [Fact]
        public void Danger_pressureSwing_wakesImmediately()
        {
            var e = new WakeEvaluator(WakeCondition.Danger, new WakeThresholds(), Night());
            var breach = Night(); breach.PressureKpa = 40f;
            Assert.Contains("pressure", e.Check(breach));
        }

        [Fact]
        public void Danger_temperatureOutOfBand_wakesAfterThreeChecks()
        {
            var e = new WakeEvaluator(WakeCondition.Danger, new WakeThresholds(), Night());
            var cold = Night(); cold.TemperatureK = 150f;
            Assert.Null(e.Check(cold));
            Assert.Null(e.Check(cold));
            Assert.Contains("temperature", e.Check(cold));
        }

        [Fact]
        public void Danger_temperature_alreadyOutOfBand_atEntry_doesNotWake()
        {
            var cold = Night(); cold.TemperatureK = 150f;
            var e = new WakeEvaluator(WakeCondition.Danger, new WakeThresholds(), cold);
            Assert.Null(Run(e, cold, 10));
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --nologo` — Expected: build FAILS, `WakeEvaluator` / `WorldSnapshot` / `WakeThresholds` not found.

- [ ] **Step 3: Write the implementation**

`droid-standby-mod/src/WakeConditions.cs`:
```csharp
using System;

namespace SaltysDroidStandby
{
    // Plain snapshot of everything the wake conditions read (spec §6). Filled from the game
    // by Game/WorldReadings.cs; constructed by hand in tests.
    public struct WorldSnapshot
    {
        public float LightPercent;     // 0..100, sun height x storm dimming
        public float WindPercent;      // 0..100
        public bool StormActive;       // a weather event currently affects this altitude
        public bool SolarStormActive;  // ...and it is a solar storm
        public bool Outdoors;          // not inside a Room
        public float BatteryRatio;     // 0..1, summed over battery slots
        public float BruteBurn;        // brute + burn damage (stun excluded on purpose)
        public float PressureKpa;      // surrounding atmosphere
        public float TemperatureK;     // surrounding atmosphere
    }

    public sealed class WakeThresholds
    {
        public float LightPercent = 20f;
        public float WindPercent = 40f;
        public float BatteryChargedRatio = 0.9f;
        public float BatteryLowRatio = 0.05f;
        public float DamageDelta = 1f;
        public float PressureDeltaKpa = 20f;
        public float SafeTempMinK = 223.15f;
        public float SafeTempMaxK = 323.15f;
        public int ConsecutiveChecks = 3;
    }

    // One evaluator per Deep Standby stay. Check() is called about once per second; it
    // returns a reason string when a selected condition fires, else null.
    public sealed class WakeEvaluator
    {
        private readonly WakeThresholds _t;
        private readonly bool _entryStorm;
        private readonly bool _entrySolar;

        private bool _lightArmed, _windArmed, _chargedArmed, _lowArmed, _tempArmed;
        private int _lightCount, _windCount, _chargedCount, _lowCount, _stormCount, _tempCount;
        private float _lastBruteBurn, _lastPressure;

        public WakeEvaluator(WakeCondition selected, WakeThresholds thresholds, WorldSnapshot atEntry)
        {
            Selected = selected;
            _t = thresholds;
            _entryStorm = atEntry.StormActive;
            _entrySolar = atEntry.SolarStormActive;
            _lightArmed = atEntry.LightPercent < _t.LightPercent;
            _windArmed = atEntry.WindPercent < _t.WindPercent;
            _chargedArmed = atEntry.BatteryRatio < _t.BatteryChargedRatio;
            _lowArmed = atEntry.BatteryRatio > _t.BatteryLowRatio;
            _tempArmed = InBand(atEntry.TemperatureK);
            _lastBruteBurn = atEntry.BruteBurn;
            _lastPressure = atEntry.PressureKpa;
        }

        public WakeCondition Selected { get; }

        public string Check(WorldSnapshot now)
        {
            string reason = null;

            if (Has(WakeCondition.Light) &&
                Rising(now.LightPercent, _t.LightPercent, ref _lightArmed, ref _lightCount))
            {
                reason = $"sunrise (light {now.LightPercent:F0}%)";
            }
            if (reason == null && Has(WakeCondition.Wind) &&
                Rising(now.WindPercent, _t.WindPercent, ref _windArmed, ref _windCount))
            {
                reason = $"wind (wind {now.WindPercent:F0}%)";
            }
            if (reason == null && Has(WakeCondition.Battery))
            {
                if (Rising(now.BatteryRatio, _t.BatteryChargedRatio, ref _chargedArmed, ref _chargedCount))
                {
                    reason = $"battery charged ({now.BatteryRatio * 100f:F0}%)";
                }
                else if (Falling(now.BatteryRatio, _t.BatteryLowRatio, ref _lowArmed, ref _lowCount))
                {
                    reason = $"battery low ({now.BatteryRatio * 100f:F0}%)";
                }
            }
            if (reason == null && Has(WakeCondition.Storm))
            {
                reason = CheckStorm(now);
            }
            if (reason == null && Has(WakeCondition.Danger))
            {
                reason = CheckDanger(now);
            }

            _lastBruteBurn = now.BruteBurn;
            _lastPressure = now.PressureKpa;
            return reason;
        }

        private bool Has(WakeCondition c) => (Selected & c) != 0;

        private bool InBand(float k) => k >= _t.SafeTempMinK && k <= _t.SafeTempMaxK;

        // >= threshold for N checks, but only after having been seen below it.
        private bool Rising(float value, float threshold, ref bool armed, ref int count)
        {
            if (value < threshold)
            {
                armed = true;
                count = 0;
                return false;
            }
            if (!armed)
            {
                return false;
            }
            return ++count >= _t.ConsecutiveChecks;
        }

        // <= threshold for N checks, but only after having been seen above it.
        private bool Falling(float value, float threshold, ref bool armed, ref int count)
        {
            if (value > threshold)
            {
                armed = true;
                count = 0;
                return false;
            }
            if (!armed)
            {
                return false;
            }
            return ++count >= _t.ConsecutiveChecks;
        }

        private string CheckStorm(WorldSnapshot now)
        {
            if (!now.Outdoors)
            {
                _stormCount = 0;
                return null;
            }
            bool changed = now.StormActive != _entryStorm || now.SolarStormActive != _entrySolar;
            if (!changed)
            {
                _stormCount = 0;
                return null;
            }
            if (++_stormCount < _t.ConsecutiveChecks)
            {
                return null;
            }
            string kind = (now.SolarStormActive || _entrySolar) ? "solar storm" : "storm";
            return now.StormActive ? kind + " started" : kind + " ended";
        }

        private string CheckDanger(WorldSnapshot now)
        {
            if (now.BruteBurn - _lastBruteBurn >= _t.DamageDelta)
            {
                return "damage taken";
            }
            if (Math.Abs(now.PressureKpa - _lastPressure) >= _t.PressureDeltaKpa)
            {
                return $"pressure change ({now.PressureKpa:F0} kPa)";
            }
            if (InBand(now.TemperatureK))
            {
                _tempArmed = true;
                _tempCount = 0;
                return null;
            }
            if (_tempArmed && ++_tempCount >= _t.ConsecutiveChecks)
            {
                return $"temperature ({now.TemperatureK - 273.15f:F0} C)";
            }
            return null;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --nologo` — Expected: `Failed: 0`, 33 passed.

- [ ] **Step 5: Commit**

```bash
git -c core.longpaths=true add droid-standby-mod
git -c core.longpaths=true commit -m "Add wake condition evaluator: armed thresholds, hysteresis, storm edges (TDD)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: Safety-net rule and the "effectively solo" rule

**Files:**
- Create: `droid-standby-mod/src/SafetyNetLogic.cs`
- Test: `droid-standby-mod/tests/SaltysDroidStandby.Tests/SafetyNetLogicTests.cs`

**Interfaces:**
- Produces: `static bool SafetyNetLogic.ShouldTrigger(StandbyLevel level, float batteryRatio, float idleSeconds, float batteryThreshold, float idleThresholdSeconds, bool suppressedUntilInput)`; `static bool SafetyNetLogic.IsEffectivelySolo(bool networkActive, bool isServer, IEnumerable<bool> connectedClientIsHost)`.

- [ ] **Step 1: Write the failing test**

```csharp
using Xunit;

namespace SaltysDroidStandby.Tests
{
    public class SafetyNetLogicTests
    {
        [Fact]
        public void LowAndIdle_triggers()
        {
            Assert.True(SafetyNetLogic.ShouldTrigger(StandbyLevel.Normal, 0.09f, 61f, 0.10f, 60f, false));
        }

        [Fact]
        public void LowAndIdle_inPowerSave_triggers()
        {
            Assert.True(SafetyNetLogic.ShouldTrigger(StandbyLevel.PowerSave, 0.09f, 61f, 0.10f, 60f, false));
        }

        [Fact]
        public void AlreadyDeep_doesNotTrigger()
        {
            Assert.False(SafetyNetLogic.ShouldTrigger(StandbyLevel.Deep, 0.01f, 600f, 0.10f, 60f, false));
        }

        [Fact]
        public void NotIdleLongEnough_doesNotTrigger()
        {
            Assert.False(SafetyNetLogic.ShouldTrigger(StandbyLevel.Normal, 0.05f, 30f, 0.10f, 60f, false));
        }

        [Fact]
        public void BatteryAboveThreshold_doesNotTrigger()
        {
            Assert.False(SafetyNetLogic.ShouldTrigger(StandbyLevel.Normal, 0.5f, 600f, 0.10f, 60f, false));
        }

        [Fact]
        public void SuppressedAfterAutoWake_untilInput()
        {
            Assert.False(SafetyNetLogic.ShouldTrigger(StandbyLevel.Normal, 0.04f, 600f, 0.10f, 60f, true));
        }

        [Fact]
        public void Solo_singlePlayer()
        {
            Assert.True(SafetyNetLogic.IsEffectivelySolo(false, false, new bool[0]));
        }

        [Fact]
        public void Solo_hostAlone_withOwnEntry()
        {
            Assert.True(SafetyNetLogic.IsEffectivelySolo(true, true, new[] { true }));
        }

        [Fact]
        public void Solo_hostAlone_emptyList()
        {
            Assert.True(SafetyNetLogic.IsEffectivelySolo(true, true, new bool[0]));
        }

        [Fact]
        public void NotSolo_hostWithAGuest()
        {
            Assert.False(SafetyNetLogic.IsEffectivelySolo(true, true, new[] { true, false }));
        }

        [Fact]
        public void NotSolo_clientOnDedicatedServer()
        {
            Assert.False(SafetyNetLogic.IsEffectivelySolo(true, false, new[] { false }));
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --nologo` — Expected: FAIL, `SafetyNetLogic` not found.

- [ ] **Step 3: Write the implementation**

```csharp
using System.Collections.Generic;

namespace SaltysDroidStandby
{
    // Spec §9. `suppressedUntilInput` is set after any automatic wake (e.g. "battery low")
    // and cleared on real player input, so an AFK droid isn't bounced in and out of standby.
    public static class SafetyNetLogic
    {
        public static bool ShouldTrigger(StandbyLevel level, float batteryRatio, float idleSeconds,
                                         float batteryThreshold, float idleThresholdSeconds,
                                         bool suppressedUntilInput)
        {
            return level != StandbyLevel.Deep
                && !suppressedUntilInput
                && batteryRatio <= batteryThreshold
                && idleSeconds >= idleThresholdSeconds;
        }

        // Spec §9 "effectively solo": true single-player, or a multiplayer host whose only
        // connection is itself (an empty client list counts). A client is never solo here --
        // a lone player on a dedicated server is phase 3's job.
        public static bool IsEffectivelySolo(bool networkActive, bool isServer, IEnumerable<bool> connectedClientIsHost)
        {
            if (!networkActive) return true;
            if (!isServer) return false;
            foreach (bool isHost in connectedClientIsHost)
            {
                if (!isHost) return false;
            }
            return true;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --nologo` — Expected: `Failed: 0`, 44 passed.

- [ ] **Step 5: Commit**

```bash
git -c core.longpaths=true add droid-standby-mod
git -c core.longpaths=true commit -m "Add safety-net trigger and effectively-solo rules (TDD)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: Mod project, plugin bootstrap and config

**Files:**
- Create: `droid-standby-mod/SaltysDroidStandby/SaltysDroidStandby.csproj`, `.gitignore`, `.gitattributes`, `Properties/AssemblyInfo.cs`, `SaltysDroidStandby.cs`, `StandbyConfig.cs`
- Create: `droid-standby-mod/About/About.xml`, `droid-standby-mod/build-and-install.sh`

**Interfaces:**
- Consumes: `StandbyLevel` (Task 1), `WakeThresholds`, `WakeCondition` (Tasks 1, 3).
- Produces: `SaltysDroidStandby.Log(string)`, `SaltysDroidStandby.LogVerbose(string)`, `SaltysDroidStandby.MOD` (LaunchPadBooster `Mod`); `StandbyConfig` static members: `KeyCode StandbyKey`, `float LongPressSeconds`, `float StunFloor(StandbyLevel)`, `float DrainFactor(StandbyLevel)`, `float JumpFactor(StandbyLevel)`, `WakeThresholds Thresholds()`, `WakeCondition DefaultWake`, `float SafetyBattery`, `float SafetyIdleSeconds`, `bool SafetyPause`, `float CheckIntervalSeconds` (1f). All accessors return the spec defaults when config is unbound (null).

- [ ] **Step 1: Copy the project skeleton from the Dual Battery mod**

From the worktree root (the droid mod lives on branch `droid-dual-battery-mod`; read its files with `git show`):
```bash
mkdir -p droid-standby-mod/SaltysDroidStandby/Properties droid-standby-mod/About
git show droid-dual-battery-mod:droid-dual-battery-mod/SaltysDroidDualBattery/.gitignore > droid-standby-mod/SaltysDroidStandby/.gitignore
git show droid-dual-battery-mod:droid-dual-battery-mod/SaltysDroidDualBattery/.gitattributes > droid-standby-mod/SaltysDroidStandby/.gitattributes
git show droid-dual-battery-mod:droid-dual-battery-mod/build-and-install.sh | sed 's/SaltysDroidDualBattery/SaltysDroidStandby/g' > droid-standby-mod/build-and-install.sh
git show droid-dual-battery-mod:droid-dual-battery-mod/SaltysDroidDualBattery/Properties/AssemblyInfo.cs \
  | sed -e "s/Salty's Droid Dual Battery/Salty's Droid Standby/g" -e 's/c41b8e27-5d93-4a6f-b1e8-7f02d6a93c15/5e7d2c91-8a3b-4f60-b2d4-1c9e7a5f3b08/' \
  > droid-standby-mod/SaltysDroidStandby/Properties/AssemblyInfo.cs
```

- [ ] **Step 2: Write `SaltysDroidStandby.csproj`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />
  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
    <ProjectGuid>{5E7D2C91-8A3B-4F60-B2D4-1C9E7A5F3B08}</ProjectGuid>
    <OutputType>Library</OutputType>
    <RootNamespace>SaltysDroidStandby</RootNamespace>
    <AssemblyName>SaltysDroidStandby</AssemblyName>
    <LangVersion>9.0</LangVersion>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <Deterministic>true</Deterministic>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
  </PropertyGroup>
  <PropertyGroup>
    <GameDir>C:\Program Files (x86)\Steam\steamapps\common\Stationeers</GameDir>
    <Managed>$(GameDir)\rocketstation_Data\Managed</Managed>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="0Harmony"><HintPath>$(GameDir)\BepInEx\core\0Harmony.dll</HintPath></Reference>
    <Reference Include="BepInEx"><HintPath>$(GameDir)\BepInEx\core\BepInEx.dll</HintPath></Reference>
    <Reference Include="LaunchPadBooster"><HintPath>$(GameDir)\BepInEx\plugins\StationeersLaunchPad\LaunchPadBooster.dll</HintPath></Reference>
    <Reference Include="Assembly-CSharp"><HintPath>$(Managed)\Assembly-CSharp.dll</HintPath></Reference>
    <Reference Include="RG.ImGui"><HintPath>$(Managed)\RG.ImGui.dll</HintPath></Reference>
    <Reference Include="UnityEngine"><HintPath>$(Managed)\UnityEngine.dll</HintPath></Reference>
    <Reference Include="UnityEngine.CoreModule"><HintPath>$(Managed)\UnityEngine.CoreModule.dll</HintPath></Reference>
    <Reference Include="UnityEngine.InputLegacyModule"><HintPath>$(Managed)\UnityEngine.InputLegacyModule.dll</HintPath></Reference>
    <Reference Include="UnityEngine.PhysicsModule"><HintPath>$(Managed)\UnityEngine.PhysicsModule.dll</HintPath></Reference>
    <Reference Include="UnityEngine.AudioModule"><HintPath>$(Managed)\UnityEngine.AudioModule.dll</HintPath></Reference>
    <Reference Include="System" />
    <Reference Include="System.Core" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include="..\src\*.cs" />
    <Compile Include="Properties\AssemblyInfo.cs" />
    <Compile Include="SaltysDroidStandby.cs" />
    <Compile Include="StandbyConfig.cs" />
  </ItemGroup>
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>
```

Later tasks add their own `<Compile Include=…>` lines.

- [ ] **Step 3: Write `About/About.xml`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<ModMetadata xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Name>Salty's Droid Standby</Name>
  <ModID>com.naclie88.SaltysDroidStandby</ModID>
  <Author>Joshua</Author>
  <Version>0.1</Version>
  <Description>H.E.M. Droids can trade cognition for battery life. Tap Z for Power Save Mode (half drain, slower); hold Z for Deep Standby (quarter drain, barely conscious) with wake conditions you choose: sunrise, wind, storm start/end, battery, danger. An AFK safety net drops a droid into standby before it runs flat. Required on server and clients in multiplayer.</Description>
</ModMetadata>
```

- [ ] **Step 4: Write `StandbyConfig.cs`**

```csharp
using BepInEx.Configuration;
using UnityEngine;

namespace SaltysDroidStandby
{
    // All tunables (spec §13). Every accessor falls back to the spec default when the
    // ConfigEntry is unbound (null) -- e.g. if LaunchPad never called OnLoaded.
    public static class StandbyConfig
    {
        public static ConfigEntry<KeyCode> Key;
        public static ConfigEntry<float> LongPress;
        public static ConfigEntry<float> PowerSaveFloor, PowerSaveDrain;
        public static ConfigEntry<float> DeepFloor, DeepDrain;
        public static ConfigEntry<int> Consecutive;
        public static ConfigEntry<float> LightThreshold, WindThreshold;
        public static ConfigEntry<float> BatteryCharged, BatteryLow;
        public static ConfigEntry<float> DamageDelta, PressureDelta, TempMinC, TempMaxC;
        public static ConfigEntry<bool> WakeLight, WakeWind, WakeStorm, WakeBattery, WakeDanger;
        public static ConfigEntry<float> SafetyBatteryEntry, SafetyIdleEntry;
        public static ConfigEntry<bool> SafetyPauseEntry;
        public static ConfigEntry<bool> Verbose;

        public static void Bind(ConfigFile c)
        {
            Key = c.Bind("Controls", "StandbyKey", KeyCode.Z, "Tap = Power Save Mode, hold = Deep Standby, any press in Deep Standby = wake.");
            LongPress = c.Bind("Controls", "LongPressSeconds", 0.6f, new ConfigDescription("How long to hold the key for Deep Standby.", new AcceptableValueRange<float>(0.2f, 2f)));

            PowerSaveFloor = c.Bind("PowerSave", "CognitionLossFloor", 40f, new ConfigDescription("Minimum cognition loss (stun) held in Power Save Mode. Speed = 1 - 0.9 x floor/100.", new AcceptableValueRange<float>(0f, 85f)));
            PowerSaveDrain = c.Bind("PowerSave", "DrainFactor", 0.5f, new ConfigDescription("Battery drain multiplier.", new AcceptableValueRange<float>(0.05f, 1f)));
            DeepFloor = c.Bind("DeepStandby", "CognitionLossFloor", 85f, new ConfigDescription("Minimum cognition loss held in Deep Standby. Keep below 90 (vanilla falls unconscious at 90 in a bed, 100 anywhere).", new AcceptableValueRange<float>(0f, 89f)));
            DeepDrain = c.Bind("DeepStandby", "DrainFactor", 0.25f, new ConfigDescription("Battery drain multiplier.", new AcceptableValueRange<float>(0.05f, 1f)));

            Consecutive = c.Bind("Wake", "ConsecutiveChecks", 3, new ConfigDescription("Checks (about 1 s apart) a threshold must hold before waking.", new AcceptableValueRange<int>(1, 10)));
            LightThreshold = c.Bind("Wake", "LightPercent", 20f, new ConfigDescription("Wake when light (sun height x storm dimming) rises above this %.", new AcceptableValueRange<float>(1f, 100f)));
            WindThreshold = c.Bind("Wake", "WindPercent", 40f, new ConfigDescription("Wake when wind rises above this %.", new AcceptableValueRange<float>(1f, 100f)));
            BatteryCharged = c.Bind("Wake", "BatteryChargedPercent", 90f, new ConfigDescription("Wake when total battery charges to this %.", new AcceptableValueRange<float>(10f, 100f)));
            BatteryLow = c.Bind("Wake", "BatteryLowPercent", 5f, new ConfigDescription("Wake when total battery drops to this %.", new AcceptableValueRange<float>(0f, 50f)));
            DamageDelta = c.Bind("Wake", "DamagePoints", 1f, new ConfigDescription("Danger: wake on this much brute+burn damage between checks.", new AcceptableValueRange<float>(0.1f, 50f)));
            PressureDelta = c.Bind("Wake", "PressureChangeKpa", 20f, new ConfigDescription("Danger: wake on a pressure swing of this many kPa between checks.", new AcceptableValueRange<float>(1f, 500f)));
            TempMinC = c.Bind("Wake", "SafeTempMinC", -50f, "Danger: wake when surrounding temperature stays below this (C).");
            TempMaxC = c.Bind("Wake", "SafeTempMaxC", 50f, "Danger: wake when surrounding temperature stays above this (C).");
            WakeLight = c.Bind("WakeDefaults", "Light", true, "Pre-ticked in the wake panel.");
            WakeWind = c.Bind("WakeDefaults", "Wind", false, "Pre-ticked in the wake panel.");
            WakeStorm = c.Bind("WakeDefaults", "Storm", false, "Pre-ticked in the wake panel.");
            WakeBattery = c.Bind("WakeDefaults", "Battery", true, "Pre-ticked in the wake panel.");
            WakeDanger = c.Bind("WakeDefaults", "Danger", true, "Pre-ticked in the wake panel.");

            SafetyBatteryEntry = c.Bind("SafetyNet", "BatteryPercent", 10f, new ConfigDescription("Auto Deep Standby at or below this total battery %, when idle.", new AcceptableValueRange<float>(0f, 50f)));
            SafetyIdleEntry = c.Bind("SafetyNet", "IdleSeconds", 60f, new ConfigDescription("Seconds without input before the safety net may act.", new AcceptableValueRange<float>(10f, 600f)));
            SafetyPauseEntry = c.Bind("SafetyNet", "PauseInSinglePlayer", true, "Also pause the game in single-player when the safety net acts.");

            Verbose = c.Bind("Debug", "VerboseLogging", false, "Log level changes, wake checks and refunds.");
        }

        public static KeyCode StandbyKey => Key != null ? Key.Value : KeyCode.Z;
        public static float LongPressSeconds => LongPress != null ? LongPress.Value : 0.6f;
        public static float CheckIntervalSeconds => 1f;

        public static float StunFloor(StandbyLevel level)
        {
            switch (level)
            {
                case StandbyLevel.PowerSave: return PowerSaveFloor != null ? PowerSaveFloor.Value : 40f;
                case StandbyLevel.Deep: return DeepFloor != null ? DeepFloor.Value : 85f;
                default: return 0f;
            }
        }

        public static float DrainFactor(StandbyLevel level)
        {
            switch (level)
            {
                case StandbyLevel.PowerSave: return PowerSaveDrain != null ? PowerSaveDrain.Value : 0.5f;
                case StandbyLevel.Deep: return DeepDrain != null ? DeepDrain.Value : 0.25f;
                default: return 1f;
            }
        }

        // Same factor vanilla applies to walking speed for this much stun (MovementController).
        public static float JumpFactor(StandbyLevel level) => Mathf.Clamp01(1f - 0.9f * StunFloor(level) / 100f);

        public static WakeThresholds Thresholds() => new WakeThresholds
        {
            LightPercent = LightThreshold != null ? LightThreshold.Value : 20f,
            WindPercent = WindThreshold != null ? WindThreshold.Value : 40f,
            BatteryChargedRatio = (BatteryCharged != null ? BatteryCharged.Value : 90f) / 100f,
            BatteryLowRatio = (BatteryLow != null ? BatteryLow.Value : 5f) / 100f,
            DamageDelta = DamageDelta != null ? DamageDelta.Value : 1f,
            PressureDeltaKpa = PressureDelta != null ? PressureDelta.Value : 20f,
            SafeTempMinK = (TempMinC != null ? TempMinC.Value : -50f) + 273.15f,
            SafeTempMaxK = (TempMaxC != null ? TempMaxC.Value : 50f) + 273.15f,
            ConsecutiveChecks = Consecutive != null ? Consecutive.Value : 3,
        };

        public static WakeCondition DefaultWake
        {
            get
            {
                WakeCondition w = WakeCondition.None;
                if (WakeLight == null || WakeLight.Value) w |= WakeCondition.Light;
                if (WakeWind != null && WakeWind.Value) w |= WakeCondition.Wind;
                if (WakeStorm != null && WakeStorm.Value) w |= WakeCondition.Storm;
                if (WakeBattery == null || WakeBattery.Value) w |= WakeCondition.Battery;
                if (WakeDanger == null || WakeDanger.Value) w |= WakeCondition.Danger;
                return w;
            }
        }

        public static float SafetyBattery => (SafetyBatteryEntry != null ? SafetyBatteryEntry.Value : 10f) / 100f;
        public static float SafetyIdleSeconds => SafetyIdleEntry != null ? SafetyIdleEntry.Value : 60f;
        public static bool SafetyPause => SafetyPauseEntry == null || SafetyPauseEntry.Value;
        public static bool IsVerbose => Verbose != null && Verbose.Value;
    }
}
```

- [ ] **Step 5: Write `SaltysDroidStandby.cs` (plugin)**

```csharp
using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LaunchPadBooster;

namespace SaltysDroidStandby
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class SaltysDroidStandby : BaseUnityPlugin
    {
        public const string pluginGuid = "com.naclie88.SaltysDroidStandby";
        public const string pluginName = "Salty's Droid Standby";
        public const string pluginVersion = "0.1";

        public static new readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(pluginName);
        public static Mod MOD;

        // LaunchPad creates this component more than once; only the instance that patched
        // drives the per-frame client logic (Update), so it never runs twice per frame.
        private static SaltysDroidStandby _driver;
        private static bool _patched;

        public static void Log(string line) => Logger.LogInfo(line);

        public static void LogVerbose(string line)
        {
            if (StandbyConfig.IsVerbose) Log(line);
        }

        static SaltysDroidStandby()
        {
            try
            {
                MOD = new Mod(pluginName, pluginVersion);
                MOD.Networking.Required = true;
                Log("LaunchPadBooster.Mod created (multiplayer required)");
            }
            catch (Exception e)
            {
                Log("LaunchPadBooster.Mod creation FAILED");
                Log(e.ToString());
            }
        }

        public void OnLoaded(ConfigFile config)
        {
            try
            {
                StandbyConfig.Bind(config);
                Log("Config bound: key=" + StandbyConfig.StandbyKey + ", Deep floor=" + StandbyConfig.StunFloor(StandbyLevel.Deep));
            }
            catch (Exception e)
            {
                Log("Config binding FAILED");
                Log(e.ToString());
            }
        }

        private void Awake()
        {
            if (_patched)
            {
                Log("Already patched by an earlier Awake() call -- skipping");
                return;
            }
            try
            {
                var harmony = new Harmony(pluginGuid);
                RegisterPatches(harmony);
                _patched = true;
                _driver = this;
                Log("Awake() completed");
            }
            catch (Exception e)
            {
                Log("Awake() threw unexpectedly");
                Log(e.ToString());
            }
        }

        // Filled in by later tasks: one PatchSafely line per patch class, plus network
        // message registration.
        private static void RegisterPatches(Harmony harmony)
        {
        }

        internal static bool PatchSafely(Harmony harmony, Type patchClass, string label)
        {
            try
            {
                harmony.CreateClassProcessor(patchClass).Patch();
                Log(label + " succeeded");
                return true;
            }
            catch (Exception e)
            {
                Log(label + " failed");
                Log(e.ToString());
                return false;
            }
        }
    }
}
```

- [ ] **Step 6: Build**

Run: `"/c/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/MSBuild.exe" droid-standby-mod/SaltysDroidStandby/SaltysDroidStandby.csproj //p:Configuration=Debug //nologo //v:minimal`
Expected: `SaltysDroidStandby -> …\bin\Debug\SaltysDroidStandby.dll`, 0 errors. (If `UnityEngine.InputLegacyModule.dll` or `UnityEngine.AudioModule.dll` is absent from `Managed`, remove that `<Reference>`; Input/AudioSource then resolve from `UnityEngine.CoreModule` in this Unity version.)

- [ ] **Step 7: Re-run tests (src/ unchanged, still passes)**

Run: `cd droid-standby-mod/tests/SaltysDroidStandby.Tests && dotnet test --nologo` — Expected: `Failed: 0`.

- [ ] **Step 8: Commit**

```bash
git -c core.longpaths=true add droid-standby-mod
git -c core.longpaths=true commit -m "Add mod project, plugin bootstrap and config

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: Standby registry and network request

**Files:**
- Create: `droid-standby-mod/SaltysDroidStandby/StandbyRegistry.cs`, `droid-standby-mod/SaltysDroidStandby/StandbyRequestMessage.cs`
- Modify: `SaltysDroidStandby.csproj` (Compile lines), `SaltysDroidStandby.cs` (`RegisterPatches`)

**Interfaces:**
- Consumes: `StandbyLevel`, `SaltysDroidStandby.MOD`, `SaltysDroidStandby.LogVerbose`.
- Produces: `static StandbyLevel StandbyRegistry.Get(Human h)`, `static void StandbyRegistry.Set(Human h, StandbyLevel level)`; `static void StandbyNetwork.Request(Human h, StandbyLevel level)` (applies locally; also sends to host when this process is a client); `class StandbyRequestMessage : INetworkMessage`.

- [ ] **Step 1: Write `StandbyRegistry.cs`**

```csharp
using System.Runtime.CompilerServices;
using Assets.Scripts.Objects.Entities;

namespace SaltysDroidStandby
{
    // Per-Human standby level. On the server this is the truth that drives the stun floor
    // and drain refund; on a client it mirrors the local player's own level (jump, UI, wake
    // checks). Weak keys: entries die with the Human. Never saved -- load = Normal (spec §11).
    public static class StandbyRegistry
    {
        private static readonly ConditionalWeakTable<Human, StrongBox<StandbyLevel>> Levels =
            new ConditionalWeakTable<Human, StrongBox<StandbyLevel>>();

        public static StandbyLevel Get(Human h)
        {
            return h != null && Levels.TryGetValue(h, out var box) ? box.Value : StandbyLevel.Normal;
        }

        public static void Set(Human h, StandbyLevel level)
        {
            if (h == null) return;
            var box = Levels.GetOrCreateValue(h);
            if (box.Value == level) return;
            SaltysDroidStandby.LogVerbose("Standby " + h.name + ": " + box.Value + " -> " + level);
            box.Value = level;
        }
    }
}
```

- [ ] **Step 2: Write `StandbyRequestMessage.cs`**

```csharp
using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using LaunchPadBooster.Networking;

namespace SaltysDroidStandby
{
    // Client -> host: "set my droid's level". The host only accepts it for the droid owned by
    // the sending connection.
    public class StandbyRequestMessage : INetworkMessage
    {
        public long HumanId;
        public byte Level;

        public StandbyRequestMessage() { }

        public void Serialize(RocketBinaryWriter writer)
        {
            writer.WriteInt64(HumanId);
            writer.WriteByte(Level);
        }

        public void Deserialize(RocketBinaryReader reader)
        {
            HumanId = reader.ReadInt64();
            Level = reader.ReadByte();
        }

        public void Process(long clientId)
        {
            if (!NetworkManager.IsServer) return;
            if (!(Thing.Find(HumanId) is Human human) || human.OrganBrain == null) return;
            Client owner = Client.Find(human.OrganBrain.ClientId);
            if (owner == null || owner.connectionId != clientId)
            {
                SaltysDroidStandby.Log("Rejected standby request for " + human.name + " from connection " + clientId);
                return;
            }
            if (Level > (byte)StandbyLevel.Deep) return;
            StandbyRegistry.Set(human, (StandbyLevel)Level);
        }
    }

    public static class StandbyNetwork
    {
        // Single entry point for every level change made on this machine. In single-player
        // and on the host it is applied directly; a client also tells the host.
        public static void Request(Human human, StandbyLevel level)
        {
            if (human == null) return;
            StandbyRegistry.Set(human, level);
            if (NetworkManager.IsClient)
            {
                ModNetworkingExtensions.SendToHost(new StandbyRequestMessage
                {
                    HumanId = human.ReferenceId,
                    Level = (byte)level,
                });
            }
        }
    }
}
```

- [ ] **Step 3: Register the message**

In `SaltysDroidStandby.cs`, replace the empty `RegisterPatches` body:
```csharp
        private static void RegisterPatches(Harmony harmony)
        {
            MOD?.Networking.RegisterMessage<StandbyRequestMessage>();
        }
```
Add to the csproj `<ItemGroup>` of Compile items:
```xml
    <Compile Include="StandbyRegistry.cs" />
    <Compile Include="StandbyRequestMessage.cs" />
```

- [ ] **Step 4: Build**

Run the MSBuild command from Global Constraints. Expected: 0 errors.
If the compiler reports `ModNetworkingExtensions.SendToHost` is not found (LaunchPadBooster uses C# 14 extension blocks), replace that call with the instance-syntax form `new StandbyRequestMessage { … }.SendToHost();` and rebuild — one of the two forms resolves; keep whichever compiles.

- [ ] **Step 5: Commit**

```bash
git -c core.longpaths=true add droid-standby-mod
git -c core.longpaths=true commit -m "Add per-droid standby registry and client-to-host level request

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 7: Cognition floor (server)

**Files:**
- Create: `droid-standby-mod/SaltysDroidStandby/Patches/CognitionFloorPatch.cs`
- Modify: csproj, `SaltysDroidStandby.cs` (`RegisterPatches`)

**Interfaces:**
- Consumes: `StandbyRegistry.Get/Set`, `StandbyConfig.StunFloor(StandbyLevel)`.
- Produces: `static bool CognitionFloorPatch.IsBlocked(Human h)` — true when standby must not apply (dead, not a droid, inside a life suspender, sleeping). Used again by Task 9's client mirror reset.

- [ ] **Step 1: Write the patch**

```csharp
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using HarmonyLib;

namespace SaltysDroidStandby.Patches
{
    // Spec §4.1. Entity.DamageState.Stun *is* the brain's stun (EntityDamageState.Stun returns
    // OrganBrain.DamageState.Stun), so the brain is the one place to hold the floor. A powered
    // droid's brain recovers 3 stun per Brain.OnLifeTick; this postfix re-raises it to the
    // floor right after. It only ever RAISES stun, so real stun (damage, empty battery) still
    // shows on top. 85 stays under vanilla's unconscious thresholds (90 in a life suspender,
    // 100 anywhere -- Human.OnLifeTick).
    [HarmonyPatch(typeof(Brain), nameof(Brain.OnLifeTick))]
    public static class CognitionFloorPatch
    {
        public static void Postfix(Brain __instance)
        {
            if (!GameManager.RunSimulation) return;
            Human human = __instance.ParentHuman;
            if (human == null) return;

            StandbyLevel level = StandbyRegistry.Get(human);
            if (level == StandbyLevel.Normal) return;

            if (IsBlocked(human))
            {
                StandbyRegistry.Set(human, StandbyLevel.Normal);
                return;
            }

            float floor = StandbyConfig.StunFloor(level);
            if (__instance.DamageState.Stun < floor)
            {
                __instance.DamageState.Damage(ChangeDamageType.Set, floor, DamageUpdateType.Stun);
            }
        }

        public static bool IsBlocked(Human human)
        {
            return human == null
                || !human.IsArtificial
                || human.State == EntityState.Dead
                || human.IsSleeping
                || (human.RootParent is ILifeSuspender suspender && suspender.IsSuspendingLife);
        }
    }
}
```

- [ ] **Step 2: Register and add to csproj**

`RegisterPatches` gains (after the message line):
```csharp
            if (!PatchSafely(harmony, typeof(Patches.CognitionFloorPatch), "Cognition floor patch"))
            {
                StandbyDisabled = true;
            }
```
Add to the plugin class:
```csharp
        // Spec §14: if the floor or drain patch can't apply, standby turns itself off so a
        // player can never end up in a half-working state.
        public static bool StandbyDisabled;
```
csproj: `<Compile Include="Patches\CognitionFloorPatch.cs" />`

- [ ] **Step 3: Build**

Run the MSBuild command. Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git -c core.longpaths=true add droid-standby-mod
git -c core.longpaths=true commit -m "Hold the cognition (stun) floor per standby level on the server

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 8: Drain scaling and jump power

**Files:**
- Create: `droid-standby-mod/SaltysDroidStandby/Patches/DrainPatch.cs`, `droid-standby-mod/SaltysDroidStandby/Patches/JumpPatch.cs`
- Modify: csproj, `SaltysDroidStandby.cs` (`RegisterPatches`)

**Interfaces:**
- Consumes: `StandbyRegistry.Get`, `StandbyConfig.DrainFactor`, `StandbyConfig.JumpFactor`, `SaltysDroidStandby.StandbyDisabled`.
- Produces: nothing new.

- [ ] **Step 1: Write `DrainPatch.cs`**

```csharp
using Assets.Scripts;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using HarmonyLib;

namespace SaltysDroidStandby.Patches
{
    // Spec §4.2. Human.OnLifeTick drains `RobotBattery.PowerStored -= k x PowerDrainedPerTick`.
    // PowerDrainedPerTick is STATIC (shared by every human, also set by the headlamp), so it
    // must not be scaled per droid. Instead: remember the battery and its charge before the
    // tick, and refund the unspent share afterwards. Which battery drains is never changed,
    // so Dual Battery's alpha -> beta order, chargers and the sleeper are untouched.
    [HarmonyPatch(typeof(Human), nameof(Human.OnLifeTick))]
    public static class DrainPatch
    {
        public struct Before
        {
            public BatteryCell Battery;
            public float Stored;
            public float Factor;
        }

        public static void Prefix(Human __instance, out Before __state)
        {
            __state = default;
            if (!GameManager.RunSimulation || !__instance.IsArtificial) return;
            float factor = StandbyConfig.DrainFactor(StandbyRegistry.Get(__instance));
            if (factor >= 1f) return;
            BatteryCell battery = __instance.RobotBattery;
            if (battery == null) return;
            __state = new Before { Battery = battery, Stored = battery.PowerStored, Factor = factor };
        }

        public static void Postfix(Before __state)
        {
            if (__state.Battery == null) return;
            float spent = __state.Stored - __state.Battery.PowerStored;
            if (spent <= 0f) return;
            __state.Battery.PowerStored += spent * (1f - __state.Factor);
        }
    }
}
```

- [ ] **Step 2: Write `JumpPatch.cs`**

```csharp
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using HarmonyLib;

namespace SaltysDroidStandby.Patches
{
    // Spec §4.3. jumpForce is a per-MovementController instance field, so scaling it for the
    // duration of HandleJump and restoring it in a Finalizer affects only this player's jump.
    // Jetpack thrust is a separate system and is not touched.
    [HarmonyPatch(typeof(MovementController), "HandleJump")]
    public static class JumpPatch
    {
        public static void Prefix(MovementController __instance, out float __state)
        {
            __state = -1f;
            Human local = InventoryManager.ParentHuman;
            if (local == null || __instance.gameObject != local.gameObject) return;
            float factor = StandbyConfig.JumpFactor(StandbyRegistry.Get(local));
            if (factor >= 1f) return;
            __state = __instance.jumpForce;
            __instance.jumpForce *= factor;
        }

        public static void Finalizer(MovementController __instance, float __state)
        {
            if (__state >= 0f) __instance.jumpForce = __state;
        }
    }
}
```

- [ ] **Step 3: Register and add to csproj**

`RegisterPatches` gains:
```csharp
            if (!PatchSafely(harmony, typeof(Patches.DrainPatch), "Drain scaling patch"))
            {
                StandbyDisabled = true;
            }
            PatchSafely(harmony, typeof(Patches.JumpPatch), "Jump power patch");
```
csproj:
```xml
    <Compile Include="Patches\DrainPatch.cs" />
    <Compile Include="Patches\JumpPatch.cs" />
```

- [ ] **Step 4: Build**

Run the MSBuild command. Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git -c core.longpaths=true add droid-standby-mod
git -c core.longpaths=true commit -m "Scale battery drain per droid (refund) and reduce jump power in standby

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 9: World readings and the local controller (input, wake, safety net)

**Files:**
- Create: `droid-standby-mod/SaltysDroidStandby/Game/WorldReadings.cs`, `droid-standby-mod/SaltysDroidStandby/Game/LocalController.cs`
- Modify: csproj, `SaltysDroidStandby.cs` (add `Update`)

**Interfaces:**
- Consumes: `PressDetector`, `LevelTransitions`, `WakeEvaluator`, `WorldSnapshot`, `SafetyNetLogic`, `BatteryMath`, `StandbyNetwork.Request`, `StandbyRegistry.Get`, `CognitionFloorPatch.IsBlocked`, `StandbyConfig.*`.
- Produces: `static WorldSnapshot WorldReadings.Read(Human h)`; `static float WorldReadings.TotalBattery(Human h)`; `LocalController` static API used by the panel (Task 10): `StandbyLevel Level`, `WakeCondition Selected { get; set; }`, `bool PanelOpen`, `bool PausedBySafetyNet`, `string LastWakeReason`, `WorldSnapshot LastReading`, `void ConfirmPanel()`, `void ResumeFromSafetyPause()`, `void Tick()`.

- [ ] **Step 1: Write `WorldReadings.cs`**

```csharp
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Objects;
using UnityEngine;
using Weather;

namespace SaltysDroidStandby.Game
{
    // Game state -> plain WorldSnapshot (spec §3/§6). Sources verified in the decompile:
    // light = sun height (OrbitalSimulation.WorldSunVector, the vector vanilla's day curve
    // uses) x storm dimming (WeatherManager.GetSolarRatioAt); wind = the 0..1 noise wind
    // turbines use; storm = WeatherManager running + affecting this altitude; solar storm =
    // that event has a DirectionalLight (vanilla's own solar-storm camera gate); outdoors =
    // not in a Room (as MovementController's jump code checks).
    public static class WorldReadings
    {
        public static WorldSnapshot Read(Human h)
        {
            float height = h.Position.y;
            bool storm = WeatherManager.CurrentEventAffects(height);
            var atmosphere = h.BreathingAtmosphere;
            return new WorldSnapshot
            {
                LightPercent = Mathf.Clamp01(Vector3.Dot(Vector3.up, OrbitalSimulation.WorldSunVector.normalized))
                               * WeatherManager.GetSolarRatioAt(height) * 100f,
                WindPercent = Mathf.Clamp01(WindTurbineGenerator.WindStrength) * 100f,
                StormActive = storm,
                SolarStormActive = storm && WeatherManager.CurrentWeatherEvent?.DirectionalLight != null,
                Outdoors = h.Room == null,
                BatteryRatio = TotalBattery(h),
                BruteBurn = h.DamageState.Brute + h.DamageState.Burn,
                PressureKpa = atmosphere != null ? atmosphere.PressureGassesAndLiquids.ToFloat() : 0f,
                TemperatureK = atmosphere != null ? atmosphere.Temperature.ToFloat() : 293.15f,
            };
        }

        // Spec §4.4: every battery-type slot, so the Dual Battery mod's beta slot is included
        // without referencing that mod.
        public static float TotalBattery(Human h)
        {
            var cells = new List<KeyValuePair<float, float>>();
            foreach (Slot slot in h.Slots)
            {
                if (slot.Type == Slot.Class.Battery && slot.Get() is BatteryCell cell)
                {
                    cells.Add(new KeyValuePair<float, float>(cell.PowerStored, cell.PowerMaximum));
                }
            }
            return BatteryMath.TotalRatio(cells);
        }
    }
}
```

- [ ] **Step 2: Write `LocalController.cs`**

```csharp
using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using SaltysDroidStandby.Patches;
using UnityEngine;

namespace SaltysDroidStandby.Game
{
    // Everything that belongs to the local player, run once per frame by the plugin's
    // Update: the key, wake checks, the safety net, and resetting the local mirror when the
    // server-side rules would have (death, bed/sleeper). Server effects live in the patches.
    public static class LocalController
    {
        private static readonly PressDetector Press = new PressDetector(0.6f);
        private static WakeEvaluator _evaluator;
        private static float _nextCheckAt;
        private static float _lastInputAt;
        private static Vector3 _lastMouse;
        private static bool _suppressedUntilInput;

        public static StandbyLevel Level => StandbyRegistry.Get(InventoryManager.ParentHuman);
        public static WakeCondition Selected { get; set; } = WakeCondition.None;
        public static bool PanelOpen { get; set; }
        public static bool PausedBySafetyNet { get; private set; }
        public static string LastWakeReason { get; private set; }
        public static WorldSnapshot LastReading { get; private set; }

        public static void Tick()
        {
            Human me = InventoryManager.ParentHuman;
            if (me == null || !me.IsArtificial || SaltysDroidStandby.StandbyDisabled)
            {
                return;
            }

            if (Level != StandbyLevel.Normal && CognitionFloorPatch.IsBlocked(me))
            {
                SetLevel(me, StandbyLevel.Normal);
            }

            // "Until another logs on": a join ends a solo safety pause; the droid stays in Deep.
            if (PausedBySafetyNet && !IsEffectivelySolo())
            {
                ResumeFromSafetyPause();
                SaltysDroidStandby.Log("Safety pause ended: another player joined");
            }

            TrackInput();
            HandleKey(me);

            if (Level == StandbyLevel.Deep && _evaluator != null && Time.time >= _nextCheckAt)
            {
                _nextCheckAt = Time.time + StandbyConfig.CheckIntervalSeconds;
                LastReading = WorldReadings.Read(me);
                string reason = _evaluator.Check(LastReading);
                if (reason != null)
                {
                    Wake(me, reason, automatic: true);
                }
            }

            float battery = WorldReadings.TotalBattery(me);
            if (SafetyNetLogic.ShouldTrigger(Level, battery, Time.unscaledTime - _lastInputAt,
                    StandbyConfig.SafetyBattery, StandbyConfig.SafetyIdleSeconds, _suppressedUntilInput))
            {
                EnterDeep(me);
                PanelOpen = false;
                LastWakeReason = null;
                if (IsEffectivelySolo() && StandbyConfig.SafetyPause)
                {
                    WorldManager.SetGamePause(true);
                    PausedBySafetyNet = true;
                }
                SaltysDroidStandby.Log($"Safety net: Deep Standby at {battery * 100f:F0}% battery");
            }
        }

        // Spec §9: single-player, or a host who is the only one connected.
        private static bool IsEffectivelySolo()
        {
            var hosts = new System.Collections.Generic.List<bool>();
            foreach (var client in NetworkBase.Clients) hosts.Add(client.IsHost);
            return SafetyNetLogic.IsEffectivelySolo(NetworkManager.IsActive, NetworkManager.IsServer, hosts);
        }

        private static void TrackInput()
        {
            Vector3 mouse = Input.mousePosition;
            if (Input.anyKey || (mouse - _lastMouse).sqrMagnitude > 1f)
            {
                _lastInputAt = Time.unscaledTime;
                _suppressedUntilInput = false;
            }
            _lastMouse = mouse;
        }

        private static void HandleKey(Human me)
        {
            Press.LongPressSeconds = StandbyConfig.LongPressSeconds;
            // Ignore the key while typing in chat/console, in menus, or paused (Review Focus 4).
            bool down = KeyManager.InputState == KeyInputState.Game && Input.GetKey(StandbyConfig.StandbyKey);
            Gesture g = Press.Update(down, Time.unscaledTime);
            if (g == Gesture.None) return;

            StandbyLevel next = LevelTransitions.Next(Level, g);
            if (next == StandbyLevel.Deep)
            {
                EnterDeep(me);
            }
            else if (Level == StandbyLevel.Deep)
            {
                Wake(me, "manual", automatic: false);
            }
            else
            {
                SetLevel(me, next);
            }
        }

        private static void EnterDeep(Human me)
        {
            SetLevel(me, StandbyLevel.Deep);
            Selected = Selected == WakeCondition.None ? StandbyConfig.DefaultWake : Selected;
            LastReading = WorldReadings.Read(me);
            _evaluator = new WakeEvaluator(Selected, StandbyConfig.Thresholds(), LastReading);
            _nextCheckAt = Time.time + StandbyConfig.CheckIntervalSeconds;
            PanelOpen = true;
        }

        private static void Wake(Human me, string reason, bool automatic)
        {
            SetLevel(me, StandbyLevel.Normal);
            _evaluator = null;
            PanelOpen = false;
            LastWakeReason = automatic ? "Woke: " + reason : null;
            _suppressedUntilInput = automatic;
            if (automatic) SaltysDroidStandby.Log("Woke: " + reason);
        }

        private static void SetLevel(Human me, StandbyLevel level)
        {
            if (level != StandbyLevel.Deep)
            {
                _evaluator = null;
                PanelOpen = false;
            }
            StandbyNetwork.Request(me, level);
        }

        // Called by the panel when the player presses Confirm: rebuild the evaluator with the
        // conditions they ticked, baselined on the current readings.
        public static void ConfirmPanel()
        {
            Human me = InventoryManager.ParentHuman;
            if (me == null || Level != StandbyLevel.Deep) return;
            LastReading = WorldReadings.Read(me);
            _evaluator = new WakeEvaluator(Selected, StandbyConfig.Thresholds(), LastReading);
            PanelOpen = false;
        }

        public static void ResumeFromSafetyPause()
        {
            if (PausedBySafetyNet)
            {
                WorldManager.SetGamePause(false);
                PausedBySafetyNet = false;
            }
        }

        public static void DismissWakeMessage() => LastWakeReason = null;
    }
}
```

- [ ] **Step 3: Drive it from the plugin**

Add to `SaltysDroidStandby` class:
```csharp
        private void Update()
        {
            if (_driver != this) return;
            try
            {
                Game.LocalController.Tick();
            }
            catch (Exception e)
            {
                Log("LocalController.Tick threw");
                Log(e.ToString());
            }
        }
```
csproj:
```xml
    <Compile Include="Game\WorldReadings.cs" />
    <Compile Include="Game\LocalController.cs" />
```

- [ ] **Step 4: Build**

Run the MSBuild command. Expected: 0 errors.
Known name check: `PressurekPa.ToFloat()` and `TemperatureKelvin.ToFloat()` exist (used by vanilla, e.g. `SolarPanel`/`Atmosphere`); `OrbitalSimulation.WorldSunVector` is a public static `Vector3`; `WindTurbineGenerator.WindStrength` is public static. If any member is reported missing, open it with `ilspycmd -t <Type> <Assembly-CSharp.dll>` and use the decompiled public name — do not guess.

- [ ] **Step 5: Re-run unit tests**

Run: `cd droid-standby-mod/tests/SaltysDroidStandby.Tests && dotnet test --nologo` — Expected: `Failed: 0`.

- [ ] **Step 6: Commit**

```bash
git -c core.longpaths=true add droid-standby-mod
git -c core.longpaths=true commit -m "Add world readings and local controller: key, wake checks, safety net

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 10: Wake panel and status line (ImGui)

**Files:**
- Create: `droid-standby-mod/SaltysDroidStandby/UI/WakePanel.cs`
- Modify: csproj, `SaltysDroidStandby.cs` (`RegisterPatches`)

**Interfaces:**
- Consumes: `LocalController` (Task 9), `WakeCondition`, `StandbyConfig.Thresholds()`.
- Produces: `WakePanelPatch` (Harmony postfix on `UI.ImGuiUi.ImGuiWindows.ImGuiWindowManager.Draw`).

- [ ] **Step 1: Write `WakePanel.cs`**

```csharp
using Assets.Scripts.Inventory;
using HarmonyLib;
using ImGuiNET;
using SaltysDroidStandby.Game;
using UI.ImGuiUi.ImGuiWindows;
using UnityEngine;

namespace SaltysDroidStandby.UI
{
    // ImGuiManager.RenderOverlay calls ImGuiWindowManager.Draw() every frame between
    // ImGui.NewFrame and ImGui.Render (and never on loading screens), so a postfix there is a
    // safe place to draw. While the panel is open the cursor is freed and game input blocked,
    // the same save/unlock/restore pattern vanilla's CinematicCamera uses.
    [HarmonyPatch(typeof(ImGuiWindowManager), nameof(ImGuiWindowManager.Draw))]
    public static class WakePanelPatch
    {
        private const string InputStateKey = "SaltysDroidStandby";
        private static bool _cursorFreed;
        private static CursorLockMode _savedLock;
        private static bool _savedVisible;

        private static readonly Vector4 Amber = new Vector4(1f, 0.75f, 0.3f, 1f);

        public static void Postfix()
        {
            if (InventoryManager.ParentHuman == null) return;

            bool interactive = LocalController.PanelOpen || LocalController.PausedBySafetyNet;
            SetCursorFree(interactive);

            if (LocalController.PausedBySafetyNet) { DrawSafetyNet(); return; }
            if (LocalController.Level == StandbyLevel.Deep)
            {
                if (LocalController.PanelOpen) DrawPanel(); else DrawStatus();
                return;
            }
            if (LocalController.Level == StandbyLevel.PowerSave) DrawLine("Power Save Mode");
            if (LocalController.LastWakeReason != null) DrawWakeMessage();
        }

        private static void DrawPanel()
        {
            WorldSnapshot r = LocalController.LastReading;
            WakeThresholds t = StandbyConfig.Thresholds();
            ImGui.SetNextWindowBgAlpha(0.92f);
            ImGui.SetNextWindowPos(new Vector2(Screen.width * 0.5f - 210f, Screen.height * 0.3f), ImGuiCond.Always);
            ImGui.Begin("Deep Standby##SaltysDroidStandby", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse);
            ImGui.TextColored(Amber, "Deep Standby - wake me when:");
            ImGui.Separator();
            Toggle(WakeCondition.Light, $"Light above {t.LightPercent:F0}%  (now {r.LightPercent:F0}%)");
            Toggle(WakeCondition.Wind, $"Wind above {t.WindPercent:F0}%  (now {r.WindPercent:F0}%)");
            Toggle(WakeCondition.Storm, "Storm starts or ends, incl. solar  (" + (r.Outdoors ? "outdoors" : "indoors - paused") + ")");
            Toggle(WakeCondition.Battery, $"Battery charged to {t.BatteryChargedRatio * 100f:F0}% or down to {t.BatteryLowRatio * 100f:F0}%  (now {r.BatteryRatio * 100f:F0}%)");
            Toggle(WakeCondition.Danger, "Danger: damage, pressure swing, temperature");
            ImGui.Separator();
            if (ImGui.Button("Confirm")) LocalController.ConfirmPanel();
            ImGui.Text("Press the standby key to wake at any time.");
            ImGui.End();
        }

        private static void Toggle(WakeCondition c, string label)
        {
            bool on = (LocalController.Selected & c) != 0;
            if (ImGui.Checkbox(label, ref on))
            {
                LocalController.Selected = on ? LocalController.Selected | c : LocalController.Selected & ~c;
            }
        }

        private static void DrawStatus()
        {
            ImGui.SetNextWindowBgAlpha(0.75f);
            ImGui.SetNextWindowPos(new Vector2(20f, Screen.height * 0.5f), ImGuiCond.Always);
            ImGui.Begin("##SaltysDroidStandbyStatus", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize);
            ImGui.TextColored(Amber, "Deep Standby - waking on: " + Describe(LocalController.Selected));
            if (ImGui.IsWindowHovered() && Input.GetMouseButtonDown(0)) LocalController.PanelOpen = true;
            ImGui.End();
        }

        private static void DrawSafetyNet()
        {
            ImGui.SetNextWindowBgAlpha(0.95f);
            ImGui.SetNextWindowPos(new Vector2(Screen.width * 0.5f - 180f, Screen.height * 0.35f), ImGuiCond.Always);
            ImGui.Begin("Saved you##SaltysDroidStandbySafety", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse);
            ImGui.TextColored(Amber, $"Saved you at {LocalController.LastReading.BatteryRatio * 100f:F0}% battery.");
            ImGui.Text("You were idle with a low battery, so your droid entered Deep Standby.");
            if (ImGui.Button("Resume in standby")) LocalController.ResumeFromSafetyPause();
            ImGui.End();
        }

        private static void DrawLine(string text)
        {
            ImGui.SetNextWindowBgAlpha(0.6f);
            ImGui.SetNextWindowPos(new Vector2(20f, Screen.height * 0.5f), ImGuiCond.Always);
            ImGui.Begin("##SaltysDroidStandbyLine", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoInputs);
            ImGui.TextColored(Amber, text);
            ImGui.End();
        }

        private static float _wakeShownAt = -1f;

        private static void DrawWakeMessage()
        {
            if (_wakeShownAt < 0f) _wakeShownAt = Time.unscaledTime;
            DrawLine(LocalController.LastWakeReason);
            if (Time.unscaledTime - _wakeShownAt > 6f)
            {
                LocalController.DismissWakeMessage();
                _wakeShownAt = -1f;
            }
        }

        private static string Describe(WakeCondition w)
        {
            if (w == WakeCondition.None) return "nothing (key only)";
            return w.ToString().ToLowerInvariant();
        }

        private static void SetCursorFree(bool free)
        {
            if (free == _cursorFreed) return;
            if (free)
            {
                _savedLock = Cursor.lockState;
                _savedVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                KeyManager.SetInputState(InputStateKey, KeyInputState.Typing);
            }
            else
            {
                Cursor.lockState = _savedLock;
                Cursor.visible = _savedVisible;
                KeyManager.RemoveInputState(InputStateKey);
            }
            _cursorFreed = free;
        }
    }
}
```

Note: while the panel is open the input state is `Typing`, so `LocalController.HandleKey` ignores the standby key (Review Focus 4) — waking from the open panel is via Confirm then the key, or the key after confirming. This is intended: the panel is a modal choice.

- [ ] **Step 2: Register and add to csproj**

`RegisterPatches` gains:
```csharp
            PatchSafely(harmony, typeof(UI.WakePanelPatch), "Wake panel patch");
```
csproj: `<Compile Include="UI\WakePanel.cs" />`

- [ ] **Step 3: Build**

Run the MSBuild command. Expected: 0 errors. (`ImGuiWindowFlags.AlwaysAutoResize`, `NoCollapse`, `NoTitleBar`, `NoInputs` and `ImGuiCond.Always` are standard ImGuiNET enum members; `RG.ImGui.dll` exposes `ImGuiNET.ImGuiWindowFlags` and `ImGuiNET.ImGuiCond`.)

- [ ] **Step 4: Commit**

```bash
git -c core.longpaths=true add droid-standby-mod
git -c core.longpaths=true commit -m "Add ImGui wake panel, status line and safety-net prompt

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 11: Docs, install and testing checklist

**Files:**
- Create: `droid-standby-mod/README.md`, `droid-standby-mod/UpdateNotes.md`
- Modify: `testing-to-do.md` is NOT on this branch — instead add `droid-standby-mod/TESTING.md`

**Interfaces:** none.

- [ ] **Step 1: Write `README.md`**

````markdown
# Salty's Droid Standby

H.E.M. Droids trade cognition for battery life — ride out a night or a storm on unreliable renewables.

## Controls (default key `Z`, rebindable)

| | Tap | Hold (0.6 s) |
|---|---|---|
| Normal | Power Save Mode | Deep Standby |
| Power Save | back to Normal | Deep Standby |
| Deep Standby | wake | wake |

- **Power Save Mode** — battery drain ×0.5, slower (cognition loss held at 40 → ~64 % speed), weaker jumps. Jetpack unaffected.
- **Deep Standby** — battery drain ×0.25, barely conscious (cognition loss 85 → ~24 % speed, near-black vision). Opens a panel to choose what wakes you: **light** (sunrise), **wind**, **storm start/end** incl. solar storms (outdoors only), **battery** charged/low, **danger** (damage, pressure swing, temperature).
- **AFK safety net** — total battery ≤ 10 % and no input for 60 s → Deep Standby; single-player also pauses the game.

The visuals and slowdown are vanilla's own cognition (stun) effects — the mod only holds a minimum. Beds, cryo tubes and the Droid Sleeper keep their vanilla behaviour (zero drain, charging). Standby is never saved: loading always starts at Normal.

## Install / requirements

- StationeersLaunchPad 1.0+. Mod folder in `Documents/My Games/Stationeers/mods/`. Developers: `build-and-install.sh` (game closed).
- **Multiplayer: required on server and every client.**
- Compatible with vanilla and Salty's Droid Dual Battery (no dependency; drain is scaled on whichever battery is in use, and battery readings sum every battery slot).

## How it works

| File | Patch | Purpose |
|---|---|---|
| `Patches/CognitionFloorPatch.cs` | postfix `Brain.OnLifeTick` (server) | hold the stun floor; clear on death / bed / sleeper |
| `Patches/DrainPatch.cs` | prefix+postfix `Human.OnLifeTick` (server) | refund the unspent share of the tick's drain |
| `Patches/JumpPatch.cs` | prefix+finalizer `MovementController.HandleJump` | scale jump force for the local player |
| `Game/LocalController.cs` | plugin `Update` | key gestures, wake checks, safety net |
| `UI/WakePanel.cs` | postfix `ImGuiWindowManager.Draw` | wake panel, status line, safety prompt |
| `src/*.cs` | — | pure logic, unit-tested in `tests/` |

Design: `docs/superpowers/specs/2026-10-07-droid-standby-design.md`. Dev log: `UpdateNotes.md`.

## Sources and credits

- Game behaviour from decompiling `Assembly-CSharp.dll` with ILSpy (`ilspycmd`): `Human.OnLifeTick` drain, `Brain.OnLifeTick` stun, `Entity.OnCameraUpdate` / `MovementController` stun effects, `WeatherManager`, `OrbitalSimulation`, `WindTurbineGenerator`, `ImGuiManager`, `KeyManager`, `WorldManager.SetGamePause`.
- BepInEx, Harmony, StationeersLaunchPad/LaunchPadBooster (config + networking), RG.ImGui (the game's own ImGui).
- Built in collaboration with Claude (Anthropic). Author: NaClie88 (Salty).
````

- [ ] **Step 2: Write `UpdateNotes.md`**

```markdown
# Salty's Droid Standby — running dev log

Newest entries at the bottom. Design: `docs/superpowers/specs/2026-10-07-droid-standby-design.md`; plan: `docs/superpowers/plans/2026-10-07-droid-standby-phase1.md`.

## 2026-10-07 — Phase 1 (standby core) built

Implemented per the plan: pure logic in `src/` with xunit tests, server-side stun floor and drain refund, client jump/input/wake/safety net, ImGui panel, one client->host message. Time Skip (phases 2-3) not started.
```

- [ ] **Step 3: Write `TESTING.md` (in-game checklist for phase 1)**

```markdown
# Droid Standby — Phase 1 in-game checks

Log lines expected once each: `Cognition floor patch succeeded`, `Drain scaling patch succeeded`, `Jump power patch succeeded`, `Wake panel patch succeeded`.

- [ ] Tap Z: "Power Save Mode" shows; cognition ~40 %, walking ~64 %, jumps lower, jetpack unchanged.
- [ ] Battery drains at about half the normal rate (time 60 s normal vs Power Save; VerboseLogging on).
- [ ] Hold Z: Deep Standby, near-black, ~24 % speed, panel opens with live readings; still conscious.
- [ ] Battery drains at about a quarter of normal.
- [ ] Confirm collapses to status line; clicking it reopens the panel.
- [ ] Tap or hold Z in Deep Standby wakes; vision recovers over a few seconds.
- [ ] Z does nothing while typing in chat / console or in the pause menu.
- [ ] Light: enter at night with Light ticked — wakes at sunrise with "Woke: sunrise". Entering at noon does NOT wake instantly.
- [ ] Storm: outdoors, storm start and end each wake; indoors they don't. Solar storm reported as "solar storm".
- [ ] Battery: with Battery ticked, charge past 90 % with a handheld charger → wakes "battery charged". (Entering a Droid Sleeper clears standby by design — vanilla's sleeper already has zero drain.)
- [ ] Danger: take damage → wakes immediately; depressurise the room → wakes "pressure change".
- [ ] Safety net (single-player): battery ≤ 10 %, idle 60 s → Deep Standby + game paused + "Saved you" prompt; Resume works.
- [ ] Safety net as the only player on a hosted multiplayer game: pauses like single-player; when a second player joins, the pause ends (droid stays in Deep Standby).
- [ ] Safety net with two players connected: Deep Standby only, no pause.
- [ ] Safety net does not loop after a "battery low" auto-wake while still AFK.
- [ ] Entering a bed / Droid Sleeper / dying clears standby.
- [ ] With Salty's Droid Dual Battery: drain still alpha → beta; battery % reading includes both slots.
- [ ] Save while in standby, reload: starts in Normal.
- [ ] Multiplayer: client's standby applies (host sees the client droid slow + low drain); client without the mod is refused.
```

- [ ] **Step 4: Install (Stationeers closed)**

Run: `bash droid-standby-mod/build-and-install.sh`
Expected: `Installed OK (<md5>) to /c/Users/Joshua/Documents/My Games/Stationeers/mods/SaltysDroidStandby.`

- [ ] **Step 5: Commit and push**

```bash
git -c core.longpaths=true add droid-standby-mod docs
git -c core.longpaths=true commit -m "Add README, dev log and phase 1 in-game checklist

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
git -c core.longpaths=true push -u origin droid-standby-mod
```

---

### Task 12: In-game verification with the project owner

**Files:** `droid-standby-mod/UpdateNotes.md` (results), `droid-standby-mod/TESTING.md` (ticks)

- [ ] **Step 1:** Ask the project owner to run `TESTING.md` (game launched after install). Do not modify code while the game is running.
- [ ] **Step 2:** After they close the game, read `BepInEx/LogOutput.log` and `%USERPROFILE%/AppData/LocalLow/Rocketwerkz/rocketstation/Player.log` for every `Salty's Droid Standby` line and any exception mentioning `SaltysDroidStandby`.
- [ ] **Step 3:** For each failure: reproduce from the log, fix with a test in `tests/` when the cause is in `src/`, rebuild, reinstall (game closed), and record cause + fix in `UpdateNotes.md`.
- [ ] **Step 4:** Commit each fix separately and push.

---

## Self-review notes (completed)

- **Spec coverage:** §2 levels (Tasks 1, 7, 8, 9) · §4.1 floor (Task 7) · §4.2 drain (Task 8) · §4.3 jump (Task 8) · §4.4 battery (Tasks 2, 9) · §5 controls/panel (Tasks 1, 9, 10) · §6 wake (Tasks 3, 9) · §9 safety net incl. effectively solo (Tasks 4, 9, 10) · §10 networking (Task 6) · §11 compatibility (Tasks 8, 9 design; checklist) · §13 config (Task 5) · §14 errors (Tasks 5, 7, 8: `PatchSafely`, `StandbyDisabled`) · §15 tests (Tasks 1–4, 11–12). §7–8 Time Skip are phases 2–3, out of this plan by design.
- **Type consistency:** `StandbyLevel`, `Gesture`, `WakeCondition`, `WorldSnapshot`, `WakeThresholds`, `WakeEvaluator.Check(WorldSnapshot) : string`, `SafetyNetLogic.ShouldTrigger(...)`, `BatteryMath.TotalRatio(IEnumerable<KeyValuePair<float,float>>)`, `StandbyRegistry.Get/Set`, `StandbyNetwork.Request`, `CognitionFloorPatch.IsBlocked`, `LocalController.*` are used with the same names/signatures everywhere.
- **Placeholders:** none. Two compile-time alternatives are spelled out explicitly (extension-method call form in Task 6, optional Unity module references in Task 5) with the exact fallback to use.
