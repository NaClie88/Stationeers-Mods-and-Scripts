# Salty's Droid Standby: Phase 1b Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rework the mod to the three Revision 2 states. Power Save is a single tap, Standby a double tap, and Deep Standby a hidden menu opened by a 3 s hold. Each state gets its new movement, look and drain numbers and its new input limits, only the standby key wakes, and the overlay is raised above the hand slots.

**Architecture:** All the decision logic stays pure and unit-tested in `src/`: gestures, the key → action table, per-level factors and blocks, and the safety-net trigger. The Harmony layer only *reads* `LevelProfile` and applies the result. `LocalController` is the single owner of local state and the menu. Time acceleration is **not** in this phase: Deep Standby "Start" freezes controls and drain at normal speed.

**Tech Stack:** C# 9 / net472 (BepInEx 5, HarmonyX, StationeersLaunchPad 1.0 / LaunchPadBooster, RG.ImGui), xunit on net9 for `src/`.

**Spec:** `docs/superpowers/specs/2026-10-07-droid-standby-design.md`. The **Revision 2** section at the top is binding and supersedes §2, §4.3, §5 and the entry rules in §7/§9.

## Global Constraints

- Gestures: single tap = Power Save (fires after the double-tap window, default **0.35 s**, configurable); double tap = Standby; hold **3 s** (configurable) = Deep Standby menu.
- Power Save: movement (top speed and jump) **12.5 %**, mouse look **12.5 %**, drain **×0.5**, cognition floor **40**.
- Standby: movement **0 %** (no walk, jump or jetpack), mouse look **6.25 %**, drain **×0.25**, floor **85**. World interaction is blocked. Inventory management (hotkeys and Ctrl/Alt + mouse) is allowed.
- Deep Standby after Start: movement 0, look 0, drain **×0** (frozen), floor 85. World interaction and inventory are blocked. Time runs at normal speed in phase 1b.
- **Only a tap of the standby key wakes** (plus a firing wake condition). No other key or mouse input wakes.
- Standby auto-wakes **silently** (status line only, no sound) on the **config-default** wake conditions.
- Closing the Deep Standby menu without Start returns to the previous state.
- The safety net (≤10 % battery, 60 s idle) enters **Standby**, never Deep. It pauses when effectively solo.
- Overlay: centred horizontally, its bottom just above the hand-slot panel. Hidden while `InventoryManager.ShowMenu` or `!InventoryManager.ShowUi`.
- Repo rules (CLAUDE.md and memory):
  - never commit decompiled game source;
  - install only with the game closed, and only via `droid-standby-mod/build-and-install.sh`;
  - keep `droid-standby-mod/UpdateNotes.md` as a running what/why log;
  - every commit ends with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`;
  - `git -c core.longpaths=true` for adds in this worktree.
- Worker-thread rule (unchanged): code reached from `Brain.OnLifeTick` / `Human.OnLifeTick` uses no Unity APIs, uses `ReferenceEquals`, and catches everything.

## Review Focus

1. **Stale config value:** the existing `LongPressSeconds = 0.6` in the user's `.cfg` would make the "3 s hold" fire at 0.6 s. The setting is renamed to `HoldSeconds` (Task 4) so the 3 s default applies; the test asserts the new key name and default.
2. **Ctrl/Alt mouse mode in Standby** must still move items between slots. The interaction block patches only the `InventoryManager` mode methods, which vanilla already skips while `Cursor.visible`; `SlotDisplayButton` handlers are blocked **only** in Deep (`LevelProfile.BlocksInventory`). Pinned by the `LevelProfile` tests (Task 2).
3. **Wake responsiveness:** in Standby and Deep a tap must wake at once, not after the 0.35 s double-tap window. Pinned by `PressDetector` `ImmediateTap` tests (Task 1).
4. **Holding the key in Deep Standby** must wake, not reopen the menu (every gesture in Deep = Wake). Pinned in the `KeyActions` tests (Task 2).
5. **Network byte range:** the level enum grows to 4 values; the host must still reject bytes above `DeepStandby`. Pinned by the `LevelProfile.IsValidWire` test (Task 2), and used in `StandbyRequestMessage` (Task 4).

---

## File Structure

| File | Change | Responsibility |
|---|---|---|
| `src/StandbyTypes.cs` | modify | `StandbyLevel` (4 values), `Gesture` (None/SingleTap/DoubleTap/LongPress), `KeyAction`, `WakeCondition` |
| `src/PressDetector.cs` | rewrite | single / double / long-press detection, with an immediate-tap mode |
| `src/LevelTransitions.cs` | rewrite → `KeyActions.Decide` | (level, gesture) → `KeyAction` |
| `src/LevelProfile.cs` | create | per-level movement/look factors, blocks, wire validation |
| `src/SafetyNetLogic.cs` | modify | trigger only from Normal/PowerSave |
| `tests/...` | modify/create | `PressDetectorTests`, `KeyActionsTests` (replaces `LevelTransitionsTests`), `LevelProfileTests`, `SafetyNetLogicTests`, `ReviewFixTests` |
| `SaltysDroidStandby/StandbyConfig.cs` | modify | new config keys and sections, 4-level floor/drain |
| `SaltysDroidStandby/StandbyRequestMessage.cs` | modify | range check via `LevelProfile.IsValidWire` |
| `SaltysDroidStandby/Patches/SpeedPatch.cs`, `JumpPatch.cs`, `LookPatch.cs` | modify | read `LevelProfile` factors |
| `SaltysDroidStandby/Patches/JetpackPatch.cs` | create | no jetpack thrust while movement is blocked |
| `SaltysDroidStandby/Patches/InteractionPatch.cs` | create | block world interaction (Standby, Deep) and inventory (Deep) |
| `SaltysDroidStandby/Game/LocalController.cs` | rewrite | gestures, menu, wakes, safety net → Standby |
| `SaltysDroidStandby/UI/WakePanel.cs` | rewrite | menu with Start/Cancel, status lines, raised position |
| `SaltysDroidStandby/SaltysDroidStandby.cs` | modify | register the new patches |
| `SaltysDroidStandby/SaltysDroidStandby.csproj` | modify | reference `UnityEngine.UIModule` (RectTransformUtility) |
| `README.md`, `UpdateNotes.md`, `TESTING.md` | modify | docs |

All commands run from `droid-standby-mod/` in the worktree unless noted. Test command: `dotnet test tests/SaltysDroidStandby.Tests -v q`.

---

### Task 1: Types and the new PressDetector

**Files:**
- Modify: `src/StandbyTypes.cs`
- Rewrite: `src/PressDetector.cs`
- Rewrite: `tests/SaltysDroidStandby.Tests/PressDetectorTests.cs`

**Interfaces:**
- Produces:
  - `enum StandbyLevel : byte { Normal=0, PowerSave=1, Standby=2, DeepStandby=3 }`
  - `enum Gesture { None, SingleTap, DoubleTap, LongPress }`
  - `enum KeyAction { None, EnterPowerSave, EnterStandby, Wake, OpenMenu }` (used by Task 2; declared here so all enums share one file)
  - `PressDetector(float doubleTapSeconds, float longPressSeconds)`, with properties `DoubleTapSeconds`, `LongPressSeconds`, `bool ImmediateTap`, and `Gesture Update(bool isDown, float now)`

- [ ] **Step 1: Replace the enums in `src/StandbyTypes.cs`** (keep `WakeCondition` unchanged):

```csharp
    // Wire value is the byte; never reorder (sent in StandbyRequestMessage).
    public enum StandbyLevel : byte
    {
        Normal = 0,
        PowerSave = 1,
        Standby = 2,
        DeepStandby = 3,
    }

    public enum Gesture
    {
        None,
        SingleTap,
        DoubleTap,
        LongPress,
    }

    // What the standby key asks for (KeyActions.Decide).
    public enum KeyAction
    {
        None,
        EnterPowerSave,
        EnterStandby,
        Wake,
        OpenMenu,
    }
```

- [ ] **Step 2: Write the failing tests.** Replace `PressDetectorTests.cs`:

```csharp
using Xunit;

namespace SaltysDroidStandby.Tests
{
    public class PressDetectorTests
    {
        private static PressDetector Make() => new PressDetector(0.35f, 3f);

        [Fact]
        public void SingleTap_firesOnlyAfterWindowExpires()
        {
            var d = Make();
            Assert.Equal(Gesture.None, d.Update(true, 0f));
            Assert.Equal(Gesture.None, d.Update(false, 0.1f));
            Assert.Equal(Gesture.None, d.Update(false, 0.3f));   // still inside window
            Assert.Equal(Gesture.SingleTap, d.Update(false, 0.46f));
            Assert.Equal(Gesture.None, d.Update(false, 1f));     // fires once
        }

        [Fact]
        public void SecondPressInsideWindow_isDoubleTap_onPress()
        {
            var d = Make();
            d.Update(true, 0f);
            d.Update(false, 0.1f);
            Assert.Equal(Gesture.DoubleTap, d.Update(true, 0.3f));
            Assert.Equal(Gesture.None, d.Update(false, 0.4f));   // release swallowed
            Assert.Equal(Gesture.None, d.Update(false, 2f));     // no trailing SingleTap
        }

        [Fact]
        public void Hold_firesLongPressOnce_andNothingOnRelease()
        {
            var d = Make();
            d.Update(true, 0f);
            Assert.Equal(Gesture.None, d.Update(true, 2.9f));
            Assert.Equal(Gesture.LongPress, d.Update(true, 3f));
            Assert.Equal(Gesture.None, d.Update(true, 5f));
            Assert.Equal(Gesture.None, d.Update(false, 5.1f));
            Assert.Equal(Gesture.None, d.Update(false, 9f));
        }

        [Fact]
        public void ImmediateTap_firesSingleTapOnRelease()
        {
            var d = Make();
            d.ImmediateTap = true;
            d.Update(true, 0f);
            Assert.Equal(Gesture.SingleTap, d.Update(false, 0.1f));
            Assert.Equal(Gesture.None, d.Update(false, 1f));
        }

        [Fact]
        public void ImmediateTap_holdStillLongPresses()
        {
            var d = Make();
            d.ImmediateTap = true;
            d.Update(true, 0f);
            Assert.Equal(Gesture.LongPress, d.Update(true, 3f));
            Assert.Equal(Gesture.None, d.Update(false, 3.2f));
        }

        [Fact]
        public void SlowSecondPress_isTwoSingleTaps()
        {
            var d = Make();
            d.Update(true, 0f);
            d.Update(false, 0.1f);
            Assert.Equal(Gesture.SingleTap, d.Update(false, 0.5f));
            d.Update(true, 1f);
            d.Update(false, 1.1f);
            Assert.Equal(Gesture.SingleTap, d.Update(false, 1.5f));
        }

        [Fact]
        public void NoInput_isNone()
        {
            var d = Make();
            Assert.Equal(Gesture.None, d.Update(false, 0f));
            Assert.Equal(Gesture.None, d.Update(false, 5f));
        }
    }
}
```

- [ ] **Step 3: Run the tests and watch them fail.**
Run: `dotnet test tests/SaltysDroidStandby.Tests -v q`
Expected: compile FAIL. The `PressDetector` constructor arity, `ImmediateTap`, and `Gesture.Tap` are referenced from `LevelTransitions.cs` and its tests. Delete `src/LevelTransitions.cs` and `tests/.../LevelTransitionsTests.cs` now (Task 2 replaces them), then re-run. Expected: FAIL on `PressDetector` members only.

- [ ] **Step 4: Rewrite `src/PressDetector.cs`:**

```csharp
namespace SaltysDroidStandby
{
    // One key, three gestures (spec Revision 2):
    //  - SingleTap: press+release, then no second press within DoubleTapSeconds (fires when the
    //    window expires), or immediately on release when ImmediateTap is set (used in Standby
    //    and Deep Standby so waking never waits on the window).
    //  - DoubleTap: a second press inside the window; fires on that press, its release is swallowed.
    //  - LongPress: held for LongPressSeconds; fires once while held, nothing on release.
    public sealed class PressDetector
    {
        private bool _wasDown;
        private float _downAt;
        private bool _swallowRelease;
        private bool _pendingTap;
        private float _releasedAt;

        public PressDetector(float doubleTapSeconds, float longPressSeconds)
        {
            DoubleTapSeconds = doubleTapSeconds;
            LongPressSeconds = longPressSeconds;
        }

        public float DoubleTapSeconds { get; set; }
        public float LongPressSeconds { get; set; }
        public bool ImmediateTap { get; set; }

        public Gesture Update(bool isDown, float now)
        {
            if (isDown && !_wasDown)
            {
                _wasDown = true;
                _downAt = now;
                _swallowRelease = false;
                if (_pendingTap && !ImmediateTap && now - _releasedAt <= DoubleTapSeconds)
                {
                    _pendingTap = false;
                    _swallowRelease = true;
                    return Gesture.DoubleTap;
                }
                _pendingTap = false;
                return Gesture.None;
            }
            if (isDown)
            {
                if (!_swallowRelease && now - _downAt >= LongPressSeconds)
                {
                    _swallowRelease = true;
                    return Gesture.LongPress;
                }
                return Gesture.None;
            }
            if (_wasDown)
            {
                _wasDown = false;
                if (_swallowRelease) return Gesture.None;
                if (ImmediateTap) return Gesture.SingleTap;
                _pendingTap = true;
                _releasedAt = now;
                return Gesture.None;
            }
            if (_pendingTap && now - _releasedAt > DoubleTapSeconds)
            {
                _pendingTap = false;
                return Gesture.SingleTap;
            }
            return Gesture.None;
        }
    }
}
```

- [ ] **Step 5: Run the PressDetector tests.**
Run: `dotnet test tests/SaltysDroidStandby.Tests -v q --filter PressDetectorTests`
Expected: compile errors remain in other files that use `StandbyLevel.Deep` / `Gesture.Tap` (`SafetyNetLogic.cs`, its tests, `ReviewFixTests.cs`). In `src/SafetyNetLogic.cs` change `level != StandbyLevel.Deep` to `level != StandbyLevel.DeepStandby` (Task 3 changes the rule properly). In `SafetyNetLogicTests.AlreadyDeep_doesNotTrigger` change `StandbyLevel.Deep` to `StandbyLevel.DeepStandby`. Then re-run. Expected: PASS 7/7 in PressDetectorTests; full suite green.

- [ ] **Step 6: Commit.**

```bash
git -c core.longpaths=true add -A src tests
git commit -m "Standby phase 1b: four levels and single/double/long-press detection

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: KeyActions and LevelProfile

**Files:**
- Create: `src/KeyActions.cs`, `src/LevelProfile.cs`
- Create: `tests/SaltysDroidStandby.Tests/KeyActionsTests.cs`, `tests/SaltysDroidStandby.Tests/LevelProfileTests.cs`

**Interfaces:**
- Consumes: `StandbyLevel`, `Gesture`, `KeyAction` (Task 1)
- Produces:
  - `static KeyAction KeyActions.Decide(StandbyLevel level, Gesture g)`
  - `static bool KeyActions.WantsImmediateTap(StandbyLevel level)`
  - `static float LevelProfile.Movement(StandbyLevel)`, `static float LevelProfile.Look(StandbyLevel)`
  - `static bool LevelProfile.BlocksJetpack / BlocksWorldInteraction / BlocksInventory(StandbyLevel)`
  - `static bool LevelProfile.IsValidWire(byte)`

- [ ] **Step 1: Write the failing tests.** `KeyActionsTests.cs`:

```csharp
using Xunit;

namespace SaltysDroidStandby.Tests
{
    public class KeyActionsTests
    {
        [Theory]
        [InlineData(StandbyLevel.Normal, Gesture.SingleTap, KeyAction.EnterPowerSave)]
        [InlineData(StandbyLevel.Normal, Gesture.DoubleTap, KeyAction.EnterStandby)]
        [InlineData(StandbyLevel.Normal, Gesture.LongPress, KeyAction.OpenMenu)]
        [InlineData(StandbyLevel.PowerSave, Gesture.SingleTap, KeyAction.Wake)]
        [InlineData(StandbyLevel.PowerSave, Gesture.DoubleTap, KeyAction.EnterStandby)]
        [InlineData(StandbyLevel.PowerSave, Gesture.LongPress, KeyAction.OpenMenu)]
        [InlineData(StandbyLevel.Standby, Gesture.SingleTap, KeyAction.Wake)]
        [InlineData(StandbyLevel.Standby, Gesture.DoubleTap, KeyAction.Wake)]
        [InlineData(StandbyLevel.Standby, Gesture.LongPress, KeyAction.OpenMenu)]
        [InlineData(StandbyLevel.DeepStandby, Gesture.SingleTap, KeyAction.Wake)]
        [InlineData(StandbyLevel.DeepStandby, Gesture.DoubleTap, KeyAction.Wake)]
        [InlineData(StandbyLevel.DeepStandby, Gesture.LongPress, KeyAction.Wake)]
        [InlineData(StandbyLevel.Normal, Gesture.None, KeyAction.None)]
        [InlineData(StandbyLevel.DeepStandby, Gesture.None, KeyAction.None)]
        public void Table_matchesRevision2(StandbyLevel level, Gesture g, KeyAction expected)
        {
            Assert.Equal(expected, KeyActions.Decide(level, g));
        }

        [Theory]
        [InlineData(StandbyLevel.Normal, false)]
        [InlineData(StandbyLevel.PowerSave, false)]
        [InlineData(StandbyLevel.Standby, true)]
        [InlineData(StandbyLevel.DeepStandby, true)]
        public void ImmediateTap_onlyWhereATapCanOnlyMeanWake(StandbyLevel level, bool expected)
        {
            Assert.Equal(expected, KeyActions.WantsImmediateTap(level));
        }
    }
}
```

`LevelProfileTests.cs`:

```csharp
using Xunit;

namespace SaltysDroidStandby.Tests
{
    public class LevelProfileTests
    {
        [Theory]
        [InlineData(StandbyLevel.Normal, 1f, 1f)]
        [InlineData(StandbyLevel.PowerSave, 0.125f, 0.125f)]
        [InlineData(StandbyLevel.Standby, 0f, 0.0625f)]
        [InlineData(StandbyLevel.DeepStandby, 0f, 0f)]
        public void Factors_matchRevision2(StandbyLevel level, float move, float look)
        {
            Assert.Equal(move, LevelProfile.Movement(level));
            Assert.Equal(look, LevelProfile.Look(level));
        }

        [Theory]
        [InlineData(StandbyLevel.Normal, false, false, false)]
        [InlineData(StandbyLevel.PowerSave, false, false, false)]
        [InlineData(StandbyLevel.Standby, true, true, false)]   // inventory stays usable (Ctrl/Alt mouse)
        [InlineData(StandbyLevel.DeepStandby, true, true, true)]
        public void Blocks_matchRevision2(StandbyLevel level, bool jetpack, bool world, bool inventory)
        {
            Assert.Equal(jetpack, LevelProfile.BlocksJetpack(level));
            Assert.Equal(world, LevelProfile.BlocksWorldInteraction(level));
            Assert.Equal(inventory, LevelProfile.BlocksInventory(level));
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(3, true)]
        [InlineData(4, false)]
        [InlineData(255, false)]
        public void IsValidWire_rejectsUnknownLevels(byte b, bool expected)
        {
            Assert.Equal(expected, LevelProfile.IsValidWire(b));
        }
    }
}
```

- [ ] **Step 2: Run the tests and watch them fail.**
Run: `dotnet test tests/SaltysDroidStandby.Tests -v q`
Expected: compile FAIL: `KeyActions` and `LevelProfile` don't exist.

- [ ] **Step 3: Implement.** `src/KeyActions.cs`:

```csharp
namespace SaltysDroidStandby
{
    // Spec Revision 2 gesture table. Every gesture in Deep Standby wakes (holding the key
    // there must not reopen the menu). In Standby a tap can only mean "wake", so taps are
    // read immediately (no double-tap wait).
    public static class KeyActions
    {
        public static KeyAction Decide(StandbyLevel level, Gesture g)
        {
            if (g == Gesture.None) return KeyAction.None;
            switch (level)
            {
                case StandbyLevel.Normal:
                    if (g == Gesture.SingleTap) return KeyAction.EnterPowerSave;
                    return g == Gesture.DoubleTap ? KeyAction.EnterStandby : KeyAction.OpenMenu;
                case StandbyLevel.PowerSave:
                    if (g == Gesture.SingleTap) return KeyAction.Wake;
                    return g == Gesture.DoubleTap ? KeyAction.EnterStandby : KeyAction.OpenMenu;
                case StandbyLevel.Standby:
                    return g == Gesture.LongPress ? KeyAction.OpenMenu : KeyAction.Wake;
                default:
                    return KeyAction.Wake;
            }
        }

        public static bool WantsImmediateTap(StandbyLevel level) =>
            level == StandbyLevel.Standby || level == StandbyLevel.DeepStandby;
    }
}
```

`src/LevelProfile.cs`:

```csharp
namespace SaltysDroidStandby
{
    // Spec Revision 2 numbers. Movement = top speed and jump; Look = mouse sensitivity.
    // Floors and drains stay in StandbyConfig (user-tunable); these are fixed by the spec.
    public static class LevelProfile
    {
        public static float Movement(StandbyLevel level)
        {
            switch (level)
            {
                case StandbyLevel.PowerSave: return 0.125f;
                case StandbyLevel.Standby:
                case StandbyLevel.DeepStandby: return 0f;
                default: return 1f;
            }
        }

        public static float Look(StandbyLevel level)
        {
            switch (level)
            {
                case StandbyLevel.PowerSave: return 0.125f;
                case StandbyLevel.Standby: return 0.0625f;
                case StandbyLevel.DeepStandby: return 0f;
                default: return 1f;
            }
        }

        public static bool BlocksJetpack(StandbyLevel level) => level >= StandbyLevel.Standby;
        public static bool BlocksWorldInteraction(StandbyLevel level) => level >= StandbyLevel.Standby;
        public static bool BlocksInventory(StandbyLevel level) => level == StandbyLevel.DeepStandby;

        public static bool IsValidWire(byte b) => b <= (byte)StandbyLevel.DeepStandby;
    }
}
```

- [ ] **Step 4: Run the tests.**
Run: `dotnet test tests/SaltysDroidStandby.Tests -v q`
Expected: PASS (all tests).

- [ ] **Step 5: Commit** (message: `Standby phase 1b: gesture table and per-level profile`, plus the Co-Authored-By line).

---

### Task 3: Safety net enters Standby

**Files:**
- Modify: `src/SafetyNetLogic.cs`
- Modify: `tests/SaltysDroidStandby.Tests/SafetyNetLogicTests.cs`

**Interfaces:**
- Produces: `ShouldTrigger` with an unchanged signature that returns false when `level >= StandbyLevel.Standby`.

- [ ] **Step 1: Add the failing test** to `SafetyNetLogicTests`:

```csharp
        [Fact]
        public void AlreadyStandby_doesNotTrigger()
        {
            Assert.False(SafetyNetLogic.ShouldTrigger(StandbyLevel.Standby, 0.01f, 600f, 0.10f, 60f, false, true));
        }
```

- [ ] **Step 2: Run it.** `dotnet test tests/SaltysDroidStandby.Tests -v q --filter SafetyNetLogicTests`. Expected: FAIL (`AlreadyStandby_doesNotTrigger`).
- [ ] **Step 3: Implement.** In `ShouldTrigger`, replace `&& level != StandbyLevel.DeepStandby` with `&& level < StandbyLevel.Standby`. Update the class comment: "Spec Revision 2: the safety net enters Standby (never Deep)."
- [ ] **Step 4: Run the full suite.** Expected: PASS.
- [ ] **Step 5: Commit** (`Standby phase 1b: safety net targets Standby`).

---

### Task 4: Config, network message, life-tick patches

**Files:**
- Modify: `SaltysDroidStandby/StandbyConfig.cs`, `SaltysDroidStandby/StandbyRequestMessage.cs`, `SaltysDroidStandby/SaltysDroidStandby.csproj`

**Interfaces:**
- Consumes: `LevelProfile.IsValidWire`
- Produces:
  - `StandbyConfig.DoubleTapSeconds` (float, default 0.35) and `StandbyConfig.HoldSeconds` (float, default 3)
  - `StandbyConfig.StunFloor(level)` → 0 / 40 / 85 / 85
  - `StandbyConfig.DrainFactor(level)` → 1 / 0.5 / 0.25 / 0
  - `StandbyConfig.MovementFactor` is **removed**; the patches use `LevelProfile`.

This task has no pure code; its gate is a clean `dotnet build` of the plugin plus the green unit suite.

- [ ] **Step 1: Edit `StandbyConfig.cs`.**
  - Replace the `LongPress` entry with two entries under a **new key name**, so a stale 0.6 s value in an existing `.cfg` is not reused (Review Focus 1):

```csharp
        public static ConfigEntry<float> DoubleTap, Hold;
        public static ConfigEntry<float> StandbyFloorEntry, StandbyDrainEntry, DeepFloorEntry;
...
            DoubleTap = c.Bind("Controls", "DoubleTapSeconds", 0.35f, new ConfigDescription("Window for a double tap of the standby key (Standby). A single tap (Power Save) takes effect after this window.", new AcceptableValueRange<float>(0.15f, 1f)));
            Hold = c.Bind("Controls", "HoldSeconds", 3f, new ConfigDescription("Hold the standby key this long to open the Deep Standby (Time Skip) menu.", new AcceptableValueRange<float>(1f, 6f)));
```

  - Section changes:
    - Keep `[PowerSave]` CognitionLossFloor 40 and DrainFactor 0.5.
    - Add `[Standby]` CognitionLossFloor 85 (range 0–89) and DrainFactor 0.25 (range 0.05–1).
    - Change `[DeepStandby]` to have CognitionLossFloor 85 only. Delete the DeepDrain entry: drain is frozen (×0) per spec.
    - Rename the old `PowerSaveFloor` description text "Speed = 1 - 0.9 x floor/100" to "Vision only; speed is fixed by the level."
  - Replace the accessors:

```csharp
        public static float DoubleTapSeconds => DoubleTap != null ? DoubleTap.Value : 0.35f;
        public static float HoldSeconds => Hold != null ? Hold.Value : 3f;

        public static float StunFloor(StandbyLevel level)
        {
            switch (level)
            {
                case StandbyLevel.PowerSave: return PowerSaveFloor != null ? PowerSaveFloor.Value : 40f;
                case StandbyLevel.Standby: return StandbyFloorEntry != null ? StandbyFloorEntry.Value : 85f;
                case StandbyLevel.DeepStandby: return DeepFloorEntry != null ? DeepFloorEntry.Value : 85f;
                default: return 0f;
            }
        }

        public static float DrainFactor(StandbyLevel level)
        {
            switch (level)
            {
                case StandbyLevel.PowerSave: return PowerSaveDrain != null ? PowerSaveDrain.Value : 0.5f;
                case StandbyLevel.Standby: return StandbyDrainEntry != null ? StandbyDrainEntry.Value : 0.25f;
                case StandbyLevel.DeepStandby: return 0f; // frozen (spec Revision 2)
                default: return 1f;
            }
        }
```

  - Delete `MovementFactor` and `LongPressSeconds`. Update the SafetyNet description: "Auto Standby at or below this total battery %, when idle."
  - Update the WakeDefaults descriptions: "Pre-ticked in the Deep Standby menu, and what Standby wakes on."

- [ ] **Step 2: `StandbyRequestMessage.cs` line 40:** replace `if (Level > (byte)StandbyLevel.Deep) return;` with `if (!LevelProfile.IsValidWire(Level)) return;`.
- [ ] **Step 3:** `DrainPatch` already refunds `spent * (1 - factor)`, so factor 0 refunds everything. No change is needed; add one comment line above `__state.Battery.PowerStored += ...`: `// factor 0 (Deep Standby) refunds the whole tick: drain frozen.`
- [ ] **Step 4: Add the csproj reference** after the AudioModule line:

```xml
    <Reference Include="UnityEngine.UIModule"><HintPath>$(Managed)\UnityEngine.UIModule.dll</HintPath></Reference>
```

- [ ] **Step 5:** Build is expected to fail until Task 5/6 update the patches and controller. Run the unit suite only: `dotnet test tests/SaltysDroidStandby.Tests -v q`. Expected: PASS. Commit (`Standby phase 1b: config for four levels, renamed hold key`).

---

### Task 5: Movement, look, jetpack and interaction patches

**Files:**
- Modify: `SaltysDroidStandby/Patches/SpeedPatch.cs`, `JumpPatch.cs`, `LookPatch.cs`
- Create: `SaltysDroidStandby/Patches/JetpackPatch.cs`, `SaltysDroidStandby/Patches/InteractionPatch.cs`
- Modify: `SaltysDroidStandby/SaltysDroidStandby.cs` (`RegisterPatches`)

**Interfaces:**
- Consumes: `LevelProfile.Movement / Look / BlocksJetpack / BlocksWorldInteraction / BlocksInventory`, `StandbyRegistry.Get(Human)`
- Produces: patch classes `JetpackPatch`, `WorldInteractionPatch`, `InventoryFreezePatch`, `SlotButtonFreezePatch`

Decompile facts this task relies on (verified in `Assembly-CSharp`; do not commit the decompile):
- `MovementController.HandleJetpack(float force, bool haveGravity)` is private. It applies all thrust from the WASD, ascend and descend axes, then calls private `StabilizeJetpack()` when the public `Stabilizer` field is set.
- `InventoryManager.ManagerUpdate` returns **before** any hotkey or world handling while `Cursor.visible` (Ctrl/Alt mouse mode). It then calls `CheckDisplaySlotInput()` (slot hotkeys and scroll item; private), and later dispatches `NormalMode()` / `PlacementMode()` / `PrecisionPlacementMode()` (all private). The else branch calls the public `ClearCursor()`.
- `SlotDisplayButton` (namespace `Assets.Scripts.UI`) has public `OnBeginDrag`, `OnPointerClick` and `OnPointerDown`. These are the mouse-mode inventory handlers.

- [ ] **Step 1: Speed/Jump/Look read `LevelProfile`.**
  - In `SpeedPatch` and `JumpPatch`, replace `StandbyConfig.MovementFactor(StandbyRegistry.Get(local))` with `LevelProfile.Movement(StandbyRegistry.Get(local))`.
  - In `LookPatch`, replace it with `LevelProfile.Look(StandbyRegistry.Get(local))`.
  - Update the header comments: Revision 2 numbers, 12.5 % / 0 %, and look 12.5 % / 6.25 % / 0.
  - In `JumpPatch`, change "Jetpack thrust is a separate system and is not touched." to "Jetpack thrust: see JetpackPatch."

- [ ] **Step 2: Create `JetpackPatch.cs`:**

```csharp
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using HarmonyLib;

namespace SaltysDroidStandby.Patches
{
    // Spec Revision 2: no jetpack in Standby or Deep Standby. HandleJetpack applies every
    // thrust force from the movement axes; skipping it removes thrust. The stabilizer still
    // runs, so a droid that parks mid-air in zero-g is held steady instead of tumbling.
    [HarmonyPatch(typeof(MovementController), "HandleJetpack")]
    public static class JetpackPatch
    {
        private static readonly AccessTools.FieldRef<MovementController, bool> Stabilizer =
            AccessTools.FieldRefAccess<MovementController, bool>("Stabilizer");
        private static readonly System.Action<MovementController> Stabilize =
            AccessTools.MethodDelegate<System.Action<MovementController>>(AccessTools.Method(typeof(MovementController), "StabilizeJetpack"));

        public static bool Prefix(MovementController __instance)
        {
            Human local = InventoryManager.ParentHuman;
            if (local == null || __instance.parentEntity != local) return true;
            if (!LevelProfile.BlocksJetpack(StandbyRegistry.Get(local))) return true;
            if (Stabilizer(__instance)) Stabilize(__instance);
            return false;
        }
    }
}
```

- [ ] **Step 3: Create `InteractionPatch.cs`:**

```csharp
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using HarmonyLib;
using UnityEngine.EventSystems;

namespace SaltysDroidStandby.Patches
{
    // Spec Revision 2 input limits, local player only.
    //  - Standby and Deep: no world interaction. These InventoryManager mode methods handle
    //    the cursor thing, use, mining and placement. Vanilla skips them entirely while the
    //    cursor is visible (Ctrl/Alt mouse mode), so blocking them never touches inventory.
    //  - Deep only: no inventory either -- slot hotkeys/scroll (CheckDisplaySlotInput) and the
    //    slot buttons' mouse handlers.
    internal static class StandbyInput
    {
        public static StandbyLevel LocalLevel()
        {
            Human local = InventoryManager.ParentHuman;
            return local == null ? StandbyLevel.Normal : StandbyRegistry.Get(local);
        }
    }

    [HarmonyPatch]
    public static class WorldInteractionPatch
    {
        [HarmonyTargetMethods]
        public static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> Targets()
        {
            yield return AccessTools.Method(typeof(InventoryManager), "NormalMode");
            yield return AccessTools.Method(typeof(InventoryManager), "PlacementMode");
            yield return AccessTools.Method(typeof(InventoryManager), "PrecisionPlacementMode");
        }

        public static bool Prefix(InventoryManager __instance)
        {
            if (!LevelProfile.BlocksWorldInteraction(StandbyInput.LocalLevel())) return true;
            __instance.ClearCursor();
            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryManager), "CheckDisplaySlotInput")]
    public static class InventoryFreezePatch
    {
        public static bool Prefix() => !LevelProfile.BlocksInventory(StandbyInput.LocalLevel());
    }

    [HarmonyPatch]
    public static class SlotButtonFreezePatch
    {
        [HarmonyTargetMethods]
        public static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> Targets()
        {
            yield return AccessTools.Method(typeof(SlotDisplayButton), nameof(SlotDisplayButton.OnBeginDrag), new[] { typeof(PointerEventData) });
            yield return AccessTools.Method(typeof(SlotDisplayButton), nameof(SlotDisplayButton.OnPointerClick), new[] { typeof(PointerEventData) });
            yield return AccessTools.Method(typeof(SlotDisplayButton), nameof(SlotDisplayButton.OnPointerDown), new[] { typeof(PointerEventData) });
        }

        public static bool Prefix() => !LevelProfile.BlocksInventory(StandbyInput.LocalLevel());
    }
}
```

If `PointerEventData` doesn't resolve, add `<Reference Include="UnityEngine.UI"><HintPath>$(Managed)\UnityEngine.UI.dll</HintPath></Reference>` to the csproj. Record a `Ruling:` line in the ledger if you do.

- [ ] **Step 4: Register.** In `SaltysDroidStandby.RegisterPatches`, add `JetpackPatch`, `WorldInteractionPatch`, `InventoryFreezePatch` and `SlotButtonFreezePatch` using the same `PatchSafely(...)` calls as the existing `SpeedPatch` line. A failure in any of them must set `StandbyDisabled` exactly as the others do.

- [ ] **Step 5:** The plugin build still fails on `LocalController` / `WakePanel` (Task 6). Run the unit suite (expected: PASS). Commit (`Standby phase 1b: Standby input limits, jetpack block, Deep freeze`).

---

### Task 6: LocalController and WakePanel (menu, wakes, overlay)

**Files:**
- Rewrite: `SaltysDroidStandby/Game/LocalController.cs`
- Rewrite: `SaltysDroidStandby/UI/WakePanel.cs`

**Interfaces:**
- Consumes: `PressDetector(float,float)` + `ImmediateTap`, `KeyActions.Decide/WantsImmediateTap`, `StandbyConfig.DoubleTapSeconds/HoldSeconds/DefaultWake/Thresholds()`, `SafetyNetLogic`, `WorldReadings`, `StandbyNetwork.Request`
- Produces (for WakePanel):
  - `LocalController.Level`, `Selected`, `MenuOpen`, `PausedBySafetyNet`, `LastWakeReason`, `LastReading`
  - `LocalController.StartDeep()`, `LocalController.CancelMenu()`, `LocalController.ResumeFromSafetyPause()`, `LocalController.DismissWakeMessage()`

- [ ] **Step 1: Rewrite `LocalController.cs`.** Keep `ResetSession`, `IsEffectivelySolo`, `TrackInput`, the blocked-reset and the join-ends-safety-pause blocks exactly as they are now. Replace the rest with:

```csharp
        private static readonly PressDetector Press = new PressDetector(0.35f, 3f);
        private static WakeEvaluator _evaluator;
        private static float _nextCheckAt;
        private static float _lastInputAt;
        private static Vector3 _lastMouse;
        private static bool _suppressedUntilInput;
        private static Human _lastHuman;

        public static StandbyLevel Level => StandbyRegistry.Get(InventoryManager.ParentHuman);
        public static WakeCondition Selected { get; set; } = WakeCondition.None;
        public static bool MenuOpen { get; private set; }
        public static bool PausedBySafetyNet { get; private set; }
        public static string LastWakeReason { get; private set; }
        public static WorldSnapshot LastReading { get; private set; }
```

In `Tick()`, after `HandleKey(me)`:

```csharp
            if (MenuOpen && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                StartDeep();
            }

            // Standby (config-default conditions, silent) and Deep Standby (menu conditions).
            bool sleeping = Level == StandbyLevel.Standby || Level == StandbyLevel.DeepStandby;
            if (sleeping && _evaluator != null && Time.time >= _nextCheckAt)
            {
                _nextCheckAt = Time.time + StandbyConfig.CheckIntervalSeconds;
                LastReading = WorldReadings.Read(me);
                string reason = _evaluator.Check(LastReading);
                if (reason != null)
                {
                    Wake(me, reason, automatic: true, sound: Level == StandbyLevel.DeepStandby);
                }
            }
```

Safety-net block: same condition, but the action is:

```csharp
                EnterStandby(me);
                LastWakeReason = null;
                if (IsEffectivelySolo() && StandbyConfig.SafetyPause)
                {
                    WorldManager.SetGamePause(true);
                    PausedBySafetyNet = true;
                }
                SaltysDroidStandby.Log($"Safety net: Standby at {battery * 100f:F0}% battery");
```

Key and transitions:

```csharp
        private static void HandleKey(Human me)
        {
            Press.DoubleTapSeconds = StandbyConfig.DoubleTapSeconds;
            Press.LongPressSeconds = StandbyConfig.HoldSeconds;
            // While the menu is open a tap cancels it, so read taps immediately there too.
            Press.ImmediateTap = MenuOpen || KeyActions.WantsImmediateTap(Level);
            // Ignore the key while typing in chat/console, in menus, or paused (Review Focus 4, phase 1).
            bool down = KeyManager.InputState == KeyInputState.Game && Input.GetKey(Patches.KeyBinding.Key);
            Gesture g = Press.Update(down, Time.unscaledTime);
            if (g == Gesture.None) return;

            if (MenuOpen)
            {
                CancelMenu();   // any gesture while the menu is up closes it, back to the previous state
                return;
            }

            switch (KeyActions.Decide(Level, g))
            {
                case KeyAction.EnterPowerSave: SetLevel(me, StandbyLevel.PowerSave); break;
                case KeyAction.EnterStandby: EnterStandby(me); break;
                case KeyAction.Wake: Wake(me, "manual", automatic: false, sound: false); break;
                case KeyAction.OpenMenu: OpenMenu(me); break;
            }
        }

        private static void EnterStandby(Human me)
        {
            SetLevel(me, StandbyLevel.Standby);
            Arm(me, StandbyConfig.DefaultWake);
        }

        // Opening the menu does not change level: the droid keeps its current state until Start.
        private static void OpenMenu(Human me)
        {
            Selected = StandbyConfig.DefaultWake;   // spec §5.2: pre-ticked from config on every open
            LastReading = WorldReadings.Read(me);
            MenuOpen = true;
        }

        public static void StartDeep()
        {
            Human me = InventoryManager.ParentHuman;
            if (me == null || !MenuOpen) return;
            MenuOpen = false;
            SetLevel(me, StandbyLevel.DeepStandby);
            Arm(me, Selected);
            SaltysDroidStandby.Log("Deep Standby started, waking on: " + Selected);
        }

        public static void CancelMenu()
        {
            if (!MenuOpen) return;
            // The level never changes while the menu is open (OpenMenu doesn't touch it), so closing
            // it is all "return to the previous state" needs; a Standby droid stays armed throughout.
            MenuOpen = false;
        }

        private static void Arm(Human me, WakeCondition conditions)
        {
            LastReading = WorldReadings.Read(me);
            _evaluator = new WakeEvaluator(conditions, StandbyConfig.Thresholds(), LastReading);
            _nextCheckAt = Time.time + StandbyConfig.CheckIntervalSeconds;
        }

        private static void Wake(Human me, string reason, bool automatic, bool sound)
        {
            // Final-review I4: only a "battery low" wake stands the safety net down until input.
            bool batteryLow = _evaluator != null && _evaluator.LastWakeWasBatteryLow;
            SetLevel(me, StandbyLevel.Normal);
            LastWakeReason = automatic ? "Woke: " + reason : null;
            _suppressedUntilInput = automatic && batteryLow;
            if (automatic)
            {
                if (sound) UIAudioManager.Play(UIAudioManager.NarrationPanelHash); // Deep only; Standby wakes silently
                SaltysDroidStandby.Log("Woke: " + reason);
            }
        }

        private static void SetLevel(Human me, StandbyLevel level)
        {
            if (level != StandbyLevel.Standby && level != StandbyLevel.DeepStandby) _evaluator = null;
            StandbyNetwork.Request(me, level);
        }
```

  - `ResetSession` sets `MenuOpen = false` (replacing `PanelOpen = false`).
  - The blocked-reset block (`if (Level != StandbyLevel.Normal && blocked)`) also does `MenuOpen = false`.
  - Delete `EnterDeep` and `ConfirmPanel`.
  - Note: with Task 5's patches, a blocked level is read from the registry, which the host updates from `StandbyNetwork.Request`. In single-player and for the host, that's immediate.

- [ ] **Step 2: Rewrite `WakePanel.cs`.** Keep the class header, `PanelModal`, `SetCursorFree`, `ResetSession`, the menu/HUD hide block, the disabled-notice block, `Toggle`, `Describe` and `DrawWakeMessage`. Change the following.

Positioning: raise the overlay so its bottom sits just above the hand-slot panel (the "card" the user saw it cover). Use the panel's real screen rect, with a fixed fallback:

```csharp
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 1f);
        private static readonly Vector3[] Corners = new Vector3[4];

        // Bottom-centre anchor just above the hand-slot panel (user: "raised by the height of the
        // notification card ... it is overlaying the hands item displays"). ImGui y is top-down,
        // Unity screen y bottom-up. Fallback when the panel isn't available: 80 % down the screen.
        private static Vector2 Anchor()
        {
            float y = Screen.height * 0.8f;
            try
            {
                GameObject hands = InventoryManager.Instance != null ? InventoryManager.Instance.PanelHandsGameObject : null;
                if (hands != null && hands.activeInHierarchy && hands.transform is RectTransform rt)
                {
                    rt.GetWorldCorners(Corners);
                    Canvas canvas = rt.GetComponentInParent<Canvas>();
                    Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                    float topUnity = RectTransformUtility.WorldToScreenPoint(cam, Corners[1]).y; // [1] = top-left
                    y = Mathf.Clamp(Screen.height - topUnity - 8f, Screen.height * 0.3f, Screen.height * 0.95f);
                }
            }
            catch (System.Exception) { /* keep fallback */ }
            return new Vector2(Screen.width * 0.5f, y);
        }
```

Every `SetNextWindowPos` uses `ImGui.SetNextWindowPos(Anchor(), ImGuiCond.Always, BottomCenter);`. Delete `Center`, `LowerFifth()` and `BottomEdge()`.

`Postfix` body after the disabled-notice block:

```csharp
            bool interactive = LocalController.MenuOpen || LocalController.PausedBySafetyNet;
            SetCursorFree(interactive);

            if (LocalController.PausedBySafetyNet) { DrawSafetyNet(); return; }
            if (LocalController.MenuOpen) { DrawMenu(); return; }
            switch (LocalController.Level)
            {
                case StandbyLevel.PowerSave: DrawLine("Power Save Mode"); break;
                case StandbyLevel.Standby: DrawLine("Standby - tap the standby key to wake"); break;
                case StandbyLevel.DeepStandby: DrawLine("Deep Standby - waking on: " + Describe(LocalController.Selected) + "  (tap the standby key to wake)"); break;
            }
            if (LocalController.LastWakeReason != null) DrawWakeMessage();
```

Menu (replaces `DrawPanel` and `DrawStatus`):

```csharp
        private static void DrawMenu()
        {
            WorldSnapshot r = LocalController.LastReading;
            WakeThresholds t = StandbyConfig.Thresholds();
            ImGui.SetNextWindowBgAlpha(0.92f);
            ImGui.SetNextWindowPos(Anchor(), ImGuiCond.Always, BottomCenter);
            ImGui.Begin("Deep Standby (Time Skip)##SaltysDroidStandby", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse);
            ImGui.TextColored(Amber, "Deep Standby - wake me when:");
            ImGui.Separator();
            Toggle(WakeCondition.Light, $"Light above {t.LightPercent:F0}%  (now {r.LightPercent:F0}%)");
            Toggle(WakeCondition.Wind, $"Wind above {t.WindPercent:F0}%  (now {r.WindPercent:F0}%)");
            Toggle(WakeCondition.Storm, "Storm starts or ends, incl. solar  (" + (r.Outdoors ? "outdoors" : "indoors - paused") + ")");
            Toggle(WakeCondition.Battery, $"Battery charged to {t.BatteryChargedRatio * 100f:F0}% or down to {t.BatteryLowRatio * 100f:F0}%  (now {r.BatteryRatio * 100f:F0}%)");
            Toggle(WakeCondition.Danger, "Danger: damage, pressure swing, temperature");
            ImGui.Separator();
            ImGui.Text("Controls and battery drain freeze until you wake.");
            if (ImGui.Button("Start")) LocalController.StartDeep();
            ImGui.SameLine();
            if (ImGui.Button("Cancel")) LocalController.CancelMenu();
            ImGui.Text("Enter = Start. Tap the standby key to cancel.");
            ImGui.End();
        }
```

`DrawSafetyNet`: text becomes `"You were idle with a low battery, so your droid entered Standby."`, and the button becomes `"Resume (stay in Standby)"`. `DrawLine` uses `Anchor()`/`BottomCenter` and keeps `NoInputs`.

The live readings in the menu refresh while it is open: in `LocalController.Tick`, before the wake check, add `if (MenuOpen && Time.time >= _nextCheckAt) { _nextCheckAt = Time.time + StandbyConfig.CheckIntervalSeconds; LastReading = WorldReadings.Read(me); }`.

- [ ] **Step 3: Build the plugin.**
Run (from `droid-standby-mod/`): the build half of `build-and-install.sh`, i.e. the `MSBuild.exe ... /p:Configuration=Release` line it contains, **not** the install. Expected: `Build succeeded`, 0 errors. Fix any compile errors (renamed members such as `PanelOpen` → `MenuOpen`, and `StandbyLevel.Deep` → `DeepStandby`) and ledger them as rulings if they deviate from this plan.
Then run `grep -rn "StandbyLevel.Deep\b\|PanelOpen\|MovementFactor\|LongPressSeconds\|ConfirmPanel\|EnterDeep" SaltysDroidStandby src`. Expected: no output.

- [ ] **Step 4: Unit suite.** `dotnet test tests/SaltysDroidStandby.Tests -v q`. Expected: PASS.
- [ ] **Step 5: Commit** (`Standby phase 1b: hidden Deep Standby menu, Standby silent wake, overlay above hand slots`).

---

### Task 7: Docs, review, install

**Files:**
- Modify: `droid-standby-mod/README.md`, `UpdateNotes.md`, `TESTING.md`

- [ ] **Step 1: README.** Replace the Controls section with the Revision 2 table (state, gesture, movement, look, drain, limits, wake), and the key line: "Settings > Controls > Inventory: Droid Standby (default Z)". Add the four new patches to the patch table:
  - `JetpackPatch`: prefix on `MovementController.HandleJetpack`.
  - `WorldInteractionPatch`: prefix on `InventoryManager.NormalMode` / `PlacementMode` / `PrecisionPlacementMode`.
  - `InventoryFreezePatch`: prefix on `InventoryManager.CheckDisplaySlotInput`.
  - `SlotButtonFreezePatch`: prefix on `SlotDisplayButton` pointer and drag handlers.

  Note that Time Skip acceleration is phase 2.
- [ ] **Step 2: UpdateNotes.md.** Add a "2026-10-07: Phase 1b (Revision 2 states)" entry covering what changed and why:
  - the user redefined the states after testing;
  - double-tap delay and immediate-tap reasoning;
  - why only the mode methods are blocked in Standby (Ctrl/Alt mouse mode bypasses them);
  - the jetpack stabilizer kept;
  - the `HoldSeconds` rename for the stale `.cfg` value;
  - overlay anchored to `PanelHandsGameObject`.
- [ ] **Step 3: TESTING.md.** Add a "Phase 1b" checklist:
  - single tap → Power Save after ~⅓ s: very slow walk, sluggish look.
  - Double tap → Standby:
    - no walk, jump or jetpack;
    - look is very slow;
    - doors and switches can't be used, and a held tool does nothing on the world;
    - Ctrl/Alt + mouse can still move a battery between slots;
    - slot hotkeys still work: slot 5 (battery) swaps a battery with the hand, including with Salty's Droid Dual Battery installed (its 2nd battery slot too).
  - Tap in Standby wakes instantly.
  - Standby auto-wakes silently at dawn (default Light), with a status line and no sound.
  - Hold 3 s → menu:
    - the droid stays in its current state;
    - Cancel, Enter, Start and a tap all behave as described;
    - after Start, no movement, look, inventory or world use, the battery % doesn't drop over a minute, and a tap wakes;
    - holding the key in Deep wakes.
  - The overlay sits above the hand-slot cards and hides under Esc.
  - The safety net enters Standby, not Deep.
  - Zero-g: entering Standby while jetpacking keeps the droid stable.
  - Check that the old `LongPressSeconds` line in the `.cfg` is ignored and the hold takes 3 s.
- [ ] **Step 4: Commit docs, then final whole-branch review** per the execution skill (review range = the commit before Task 1 .. HEAD; include this plan's Review Focus). Fix Critical/Important findings with tests where the code is pure.
- [ ] **Step 5: Push** `git push` (branch `droid-standby-mod`).
- [ ] **Step 6: Install — only after the user confirms the game is closed.** Announce first, then run `bash droid-standby-mod/build-and-install.sh` and report the installed DLL hash.
