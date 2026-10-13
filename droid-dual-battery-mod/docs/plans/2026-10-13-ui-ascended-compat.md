# UI Ascended Compatibility (battery β) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** When Stationeers UI Ascended is loaded, a droid sees battery β as a small box attached to #5 on UI Ascended's HUD (with charge %, accepting drops) and as an entry in UI Ascended's #5 wheel.

**Architecture:** Three new files in `Patches/`. `UiAscendedCompat` detects UI Ascended by type name on the first HUD build in a world, then applies the patches from `UiAscendedBetaBox` (HUD box + drop target on `EquipmentColumnWidget`) and `UiAscendedBetaWheel` (`EquipmentKeyRadialFeature.BuildRoot`/`CanOpen`). Every UI Ascended member is reached by reflection: no compile-time reference. Each hook disables itself on its first exception.

**Tech Stack:** C# 9 / .NET Framework 4.7.2, HarmonyLib, BepInEx + StationeersLaunchPad, Unity UI + TextMeshPro. Build with VS 2022 Build Tools MSBuild.

**Spec:** `droid-dual-battery-mod/docs/specs/2026-10-13-ui-ascended-compat-design.md`

## Global Constraints

- No compile-time reference to UI Ascended (`StationeersUIMod.dll`). Resolve everything with `AccessTools` by name.
- UI Ascended identifiers (v1.0.3): `StationeersUIMod.UI.Hud.Widgets.EquipmentColumnWidget`, `StationeersUIMod.Features.EquipmentKeyRadialFeature`, `StationeersUIMod.Features.ItemMenuBuilder`, `StationeersUIMod.UI.Hud.HudDropZone`, `StationeersUIMod.UI.Hud.HudPanel`.
- Patch UI Ascended once, on the first `HudSlotPatch.Postfix`, never at `Awake` (load order isn't guaranteed).
- One log line per hook (`UI Ascended compat: <hook> patched` / `... skipped`). If UI Ascended is absent: `UI Ascended not loaded -- compat skipped`, once.
- Each postfix catches everything; on first exception it logs once and disables itself for the session.
- Droids only (`SpeciesClass.Robot`). Humans and Zrilians see no change.
- β box size = 0.6 × #5 box. Labels: #5 = `<BATTERY> α`, β box = `β NN%` or `β`.
- β charge comes from `BatteryCell.CurrentPowerPercentage` (networked; `PowerStored` is not).
- Decompiled UI Ascended / game source is never committed. It lives only in the scratchpad.
- Shell scripts stay LF; don't use `sed -i` on CRLF files.
- Commit messages end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

1. **HUD rebuilt mid-session** (F10 theme switch, F9 edits, scene reload): the β box must reappear on the new widget instance, with no stale box or exception. Covered by the `ConditionalWeakTable` + `PanelGo == null` rebuild in Task 2; checked in Task 4 step 6.
2. **Theme hides #5** (F9 "First slot"/"Slot count" excludes index 4): the β box must hide too. Checked in Task 4 step 7.
3. **Respawn as human after being a droid**: β box hidden, and #5 label back to UI Ascended's own text. Checked in Task 4 step 9.
4. **Horizontal equipment row** (a theme with "Horizontal row" on): β goes above or below #5, not on top of #4/#6. Checked in Task 4 step 8.
5. **HUD built before our patches applied** (UI Ascended builds its HUD before `InventoryManager.Initialize`): the β box must still appear without waiting for a resize. Covered by Update building on demand in Task 2; checked in Task 4 step 4.

## Testing approach

This repo has no automated test harness for game mods: they only run inside Stationeers. Each code task is verified by a **clean MSBuild compile (0 errors, 0 warnings)**. Behaviour is verified **in-game in Task 4**, using the `LogOutput.log` line per hook (repo rule) and a manual checklist.

Build command (from `droid-dual-battery-mod/`, Git Bash):
```bash
"/c/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/MSBuild.exe" SaltysDroidDualBattery/SaltysDroidDualBattery.csproj //p:Configuration=Debug //nologo //v:minimal
```
Expected: ends with `SaltysDroidDualBattery -> ...\bin\Debug\SaltysDroidDualBattery.dll` and no `warning`/`error` lines.

---

### Task 1: Compat entry point, hook guard, deferred patching

**Files:**
- Create: `droid-dual-battery-mod/SaltysDroidDualBattery/Patches/UiAscendedCompat.cs`
- Modify: `droid-dual-battery-mod/SaltysDroidDualBattery/SaltysDroidDualBattery.cs` (store the Harmony instance; version 0.3)
- Modify: `droid-dual-battery-mod/SaltysDroidDualBattery/Patches/HudSlotPatch.cs` (call `TryApply`)
- Modify: `droid-dual-battery-mod/SaltysDroidDualBattery/Patches/BatteryLabels.cs` (`BatteryName` → `internal`)
- Modify: `droid-dual-battery-mod/SaltysDroidDualBattery/SaltysDroidDualBattery.csproj` (Compile entries for all three new files; the Task 2/3 files are created as stubs here so the project builds)

**Interfaces:**
- Produces: `SaltysDroidDualBattery.HarmonyInstance` (`internal static Harmony`); `UiAscendedCompat.TryApply()`; `UiAscendedCompat.Patch(Harmony, MethodBase, Type owner, string postfixName, string label) : bool`; `HookGuard(string name)` with `bool Disabled` and `void Fail(Exception)`; `BatteryLabels.BatteryName()` (internal); stubs `UiAscendedBetaBox.Apply(Harmony, Type columnType)` and `UiAscendedBetaWheel.Apply(Harmony)`.

- [ ] **Step 1: Store the Harmony instance and bump the version**

In `SaltysDroidDualBattery.cs`, change `pluginVersion` to `"0.3"` and add the field below `MOD`:
```csharp
        // Kept so patches can be applied after Awake (UI Ascended compat is patched on the
        // first HUD build, once every LaunchPad mod has loaded).
        internal static Harmony HarmonyInstance;
```
In `Awake()`, replace `var harmony = new Harmony(pluginGuid);` with:
```csharp
                var harmony = HarmonyInstance = new Harmony(pluginGuid);
```

- [ ] **Step 2: Make `BatteryName` internal**

In `BatteryLabels.cs`, change `private static string BatteryName()` to `internal static string BatteryName()`.

- [ ] **Step 3: Create `UiAscendedCompat.cs`**

```csharp
using System;
using System.Reflection;
using HarmonyLib;

namespace SaltysDroidDualBattery.Patches
{
    // Soft-dependency support for Stationeers UI Ascended (Workshop 3776545141). Its HUD hides
    // the vanilla clothing panel that holds our cloned β button, and its own equipment column
    // and #5 wheel only know the six vanilla slots. When it's loaded we patch both by name: no
    // compile-time reference, so this mod builds and runs without it.
    //
    // Patched on the first HUD build in a world rather than in Awake: LaunchPad doesn't
    // guarantee UI Ascended's assembly is loaded before ours. Harmony patches still reach
    // widgets that already exist.
    public static class UiAscendedCompat
    {
        private const string ColumnTypeName = "StationeersUIMod.UI.Hud.Widgets.EquipmentColumnWidget";

        private static bool _attempted;

        public static void TryApply()
        {
            if (_attempted)
            {
                return;
            }
            _attempted = true;
            try
            {
                Type column = AccessTools.TypeByName(ColumnTypeName);
                if (column == null)
                {
                    SaltysDroidDualBattery.Log("UI Ascended not loaded -- compat skipped");
                    return;
                }
                Harmony harmony = SaltysDroidDualBattery.HarmonyInstance;
                if (harmony == null)
                {
                    SaltysDroidDualBattery.Log("UI Ascended compat: no Harmony instance -- skipped");
                    return;
                }
                SaltysDroidDualBattery.Log("UI Ascended detected -- applying compat patches");
                UiAscendedBetaBox.Apply(harmony, column);
                UiAscendedBetaWheel.Apply(harmony);
            }
            catch (Exception e)
            {
                SaltysDroidDualBattery.Log("UI Ascended compat setup failed");
                SaltysDroidDualBattery.Log(e.ToString());
            }
        }

        // One log line per hook either way, so every patch can be confirmed in-game.
        internal static bool Patch(Harmony harmony, MethodBase target, Type owner, string postfixName, string label)
        {
            if (target == null)
            {
                SaltysDroidDualBattery.Log("UI Ascended compat: " + label + " target not found -- skipped");
                return false;
            }
            try
            {
                harmony.Patch(target, postfix: new HarmonyMethod(owner, postfixName));
                SaltysDroidDualBattery.Log("UI Ascended compat: " + label + " patched");
                return true;
            }
            catch (Exception e)
            {
                SaltysDroidDualBattery.Log("UI Ascended compat: " + label + " patch failed");
                SaltysDroidDualBattery.Log(e.ToString());
                return false;
            }
        }
    }

    // A UI Ascended update can rename what we reach by reflection. The first exception in a
    // hook is logged once and switches that hook off, so it can't spam the log every frame.
    internal sealed class HookGuard
    {
        private readonly string _name;

        public HookGuard(string name)
        {
            _name = name;
        }

        public bool Disabled { get; private set; }

        public void Fail(Exception e)
        {
            Disabled = true;
            SaltysDroidDualBattery.Log("UI Ascended compat: " + _name + " threw -- disabled for this session");
            SaltysDroidDualBattery.Log(e.ToString());
        }
    }
}
```

- [ ] **Step 4: Create the Task 2/3 stubs so the project builds**

`Patches/UiAscendedBetaBox.cs`:
```csharp
using System;
using HarmonyLib;

namespace SaltysDroidDualBattery.Patches
{
    public static class UiAscendedBetaBox
    {
        public static void Apply(Harmony harmony, Type column)
        {
        }
    }
}
```
`Patches/UiAscendedBetaWheel.cs`:
```csharp
using HarmonyLib;

namespace SaltysDroidDualBattery.Patches
{
    public static class UiAscendedBetaWheel
    {
        public static void Apply(Harmony harmony)
        {
        }
    }
}
```

- [ ] **Step 5: Call `TryApply` from the HUD build**

In `HudSlotPatch.Postfix`, make the first line:
```csharp
            UiAscendedCompat.TryApply();
```

- [ ] **Step 6: Register the files in the csproj**

After `<Compile Include="Patches\BatteryChargerPatch.cs" />` add:
```xml
    <Compile Include="Patches\UiAscendedCompat.cs" />
    <Compile Include="Patches\UiAscendedBetaBox.cs" />
    <Compile Include="Patches\UiAscendedBetaWheel.cs" />
```

- [ ] **Step 7: Build.** Run the build command. Expected: clean, 0 warnings.

- [ ] **Step 8: Commit**
```bash
git add droid-dual-battery-mod/SaltysDroidDualBattery
git commit -m "UI Ascended compat: detection, hook guard, deferred patching (v0.3)"
```

---

### Task 2: β box on UI Ascended's equipment column (+ drop target)

**Files:**
- Modify (replace stub): `droid-dual-battery-mod/SaltysDroidDualBattery/Patches/UiAscendedBetaBox.cs`

**Interfaces:**
- Consumes: `UiAscendedCompat.Patch(...)`, `HookGuard`, `ExtraBatterySlot.Get(Human) : Slot`, `BatteryLabels.BatteryName() : string`.
- Produces: `UiAscendedBetaBox.Apply(Harmony harmony, Type column)`.

UI Ascended facts this relies on (v1.0.3):
- The column's boxes are a private `List<Box> _boxes`. Nested `Box` has `PanelGraphic Panel`, `Image Icon`, `TextMeshProUGUI Label`. Index 4 = #5 (`UniformSlot`).
- `PanelGraphic` sizes are private `_w`/`_h`, set only by `SetShape(w,h,rTL,rTR,rBR,rBL,topInset,bottomInset)`. **`Instantiate` wouldn't copy them**, so new panels are made with `HudPanel.MakePanel(Transform, string)` (protected static) and styled every frame like UI Ascended's Meson tag: `FillColor()`, `BorderColor()`, `BorderWidthFor()`, `GlassSheenFor()`, `GlassEdgeFor()`, `FeatherFor()`, `ApplyMeshFx(IGlassSurface)`, `RadiusTL()` (protected instance on `HudElementView`).
- `Layout(float)` positions boxes (anchoredPosition, relative to screen centre). `UpdatePanel(HudSnapshot, float)` runs every frame and sets #5's label to "BATTERY" for robots. `CollectDropZones(List<HudDropZone>, HudSnapshot, float)` adds `{CanvasRect, Slot, Label}` per box, in the same coordinate frame as the box positions.

- [ ] **Step 1: Replace `UiAscendedBetaBox.cs` with the full implementation**

```csharp
using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using CharacterCustomisation;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SaltysDroidDualBattery.Patches
{
    // Battery β as a small box attached to UI Ascended's #5 box, the way UI Ascended attaches
    // its Meson Scanner tag to the glasses box. It is styled through the widget's own helpers
    // every frame, so it follows the theme, glass and scale. It's also a drop target.
    public static class UiAscendedBetaBox
    {
        private const int BatteryBoxIndex = 4;
        private const float SizeRatio = 0.6f;
        private const string HudPanelTypeName = "StationeersUIMod.UI.Hud.HudPanel";
        private const string DropZoneTypeName = "StationeersUIMod.UI.Hud.HudDropZone";

        private sealed class Beta
        {
            public GameObject PanelGo;
            public Graphic Panel;
            public Image Icon;
            public TextMeshProUGUI Label;
            public bool Laid;
            public Vector2 Offset; // β centre minus #5 centre, in the column's coordinates
            public int LastPct = int.MinValue;
        }

        private static readonly ConditionalWeakTable<object, Beta> _betas = new ConditionalWeakTable<object, Beta>();
        private static readonly object[] NoArgs = new object[0];

        private static readonly HookGuard _layoutGuard = new HookGuard("β box layout");
        private static readonly HookGuard _updateGuard = new HookGuard("β box update");
        private static readonly HookGuard _zoneGuard = new HookGuard("β box drop target");

        private static FieldInfo _boxes, _boxPanel, _boxIcon, _boxLabel, _panelW;
        private static MethodInfo _makePanel, _makeIcon, _setShape;
        private static MethodInfo _fillColor, _borderColor, _borderWidthFor, _sheenFor, _edgeFor, _featherFor, _applyMeshFx, _radiusTL;
        private static PropertyInfo _root, _pBorderColor, _pBorderWidth, _pSheen, _pSpec, _pFeather;
        private static Type _zoneType;
        private static FieldInfo _zoneRect, _zoneSlot, _zoneLabel;

        public static void Apply(Harmony harmony, Type column)
        {
            Type box = AccessTools.Inner(column, "Box");
            Type hudPanel = AccessTools.TypeByName(HudPanelTypeName);
            _zoneType = AccessTools.TypeByName(DropZoneTypeName);
            _boxes = AccessTools.Field(column, "_boxes");
            _boxPanel = box != null ? AccessTools.Field(box, "Panel") : null;
            _boxIcon = box != null ? AccessTools.Field(box, "Icon") : null;
            _boxLabel = box != null ? AccessTools.Field(box, "Label") : null;
            Type panelType = _boxPanel?.FieldType;
            _panelW = panelType != null ? AccessTools.Field(panelType, "_w") : null;
            _setShape = panelType != null ? AccessTools.Method(panelType, "SetShape",
                new[] { typeof(float), typeof(float), typeof(float), typeof(float), typeof(float), typeof(float), typeof(float), typeof(float) }) : null;
            _pBorderColor = panelType != null ? AccessTools.Property(panelType, "BorderColor") : null;
            _pBorderWidth = panelType != null ? AccessTools.Property(panelType, "BorderWidth") : null;
            _pSheen = panelType != null ? AccessTools.Property(panelType, "Sheen") : null;
            _pSpec = panelType != null ? AccessTools.Property(panelType, "Spec") : null;
            _pFeather = panelType != null ? AccessTools.Property(panelType, "FeatherOverride") : null;
            _makePanel = hudPanel != null ? AccessTools.Method(hudPanel, "MakePanel", new[] { typeof(Transform), typeof(string) }) : null;
            _makeIcon = hudPanel != null ? AccessTools.Method(hudPanel, "MakeIcon", new[] { typeof(Transform), typeof(string) }) : null;
            _root = hudPanel != null ? AccessTools.Property(hudPanel, "Root") : null;
            _fillColor = AccessTools.Method(column, "FillColor", Type.EmptyTypes);
            _borderColor = AccessTools.Method(column, "BorderColor", Type.EmptyTypes);
            _borderWidthFor = AccessTools.Method(column, "BorderWidthFor", Type.EmptyTypes);
            _sheenFor = AccessTools.Method(column, "GlassSheenFor", Type.EmptyTypes);
            _edgeFor = AccessTools.Method(column, "GlassEdgeFor", Type.EmptyTypes);
            _featherFor = AccessTools.Method(column, "FeatherFor", Type.EmptyTypes);
            _radiusTL = AccessTools.Method(column, "RadiusTL", Type.EmptyTypes);
            _applyMeshFx = AccessTools.Method(column, "ApplyMeshFx");
            _zoneRect = _zoneType != null ? AccessTools.Field(_zoneType, "CanvasRect") : null;
            _zoneSlot = _zoneType != null ? AccessTools.Field(_zoneType, "Slot") : null;
            _zoneLabel = _zoneType != null ? AccessTools.Field(_zoneType, "Label") : null;

            object[] required =
            {
                _boxes, _boxPanel, _boxIcon, _boxLabel, _panelW, _setShape, _pBorderColor, _pBorderWidth, _pSheen,
                _pSpec, _pFeather, _makePanel, _makeIcon, _root, _fillColor, _borderColor, _borderWidthFor,
                _sheenFor, _edgeFor, _featherFor, _radiusTL, _applyMeshFx,
            };
            if (Array.IndexOf(required, null) >= 0)
            {
                SaltysDroidDualBattery.Log("UI Ascended compat: equipment column members not found (index " +
                                           Array.IndexOf(required, null) + ") -- β box skipped");
                return;
            }

            UiAscendedCompat.Patch(harmony, AccessTools.Method(column, "Layout", new[] { typeof(float) }),
                typeof(UiAscendedBetaBox), nameof(LayoutPostfix), "β box layout");
            UiAscendedCompat.Patch(harmony, AccessTools.Method(column, "UpdatePanel"),
                typeof(UiAscendedBetaBox), nameof(UpdatePostfix), "β box update");
            if (_zoneRect != null && _zoneSlot != null && _zoneLabel != null)
            {
                UiAscendedCompat.Patch(harmony, AccessTools.Method(column, "CollectDropZones"),
                    typeof(UiAscendedBetaBox), nameof(DropZonesPostfix), "β box drop target");
            }
            else
            {
                SaltysDroidDualBattery.Log("UI Ascended compat: drop zone members not found -- β drop target skipped");
            }
        }

        private static void LayoutPostfix(object __instance)
        {
            if (_layoutGuard.Disabled)
            {
                return;
            }
            try
            {
                Layout(__instance);
            }
            catch (Exception e)
            {
                _layoutGuard.Fail(e);
            }
        }

        private static void UpdatePostfix(object __instance)
        {
            if (_updateGuard.Disabled)
            {
                return;
            }
            try
            {
                Update(__instance);
            }
            catch (Exception e)
            {
                _updateGuard.Fail(e);
            }
        }

        private static void DropZonesPostfix(object __instance, object into)
        {
            if (_zoneGuard.Disabled)
            {
                return;
            }
            try
            {
                AddDropZone(__instance, into as IList);
            }
            catch (Exception e)
            {
                _zoneGuard.Fail(e);
            }
        }

        // The droid's β slot, or null for non-droids.
        private static Slot BetaSlot(out Human human)
        {
            human = InventoryManager.ParentHuman;
            return human != null && human.SpeciesClass == SpeciesClass.Robot ? ExtraBatterySlot.Get(human) : null;
        }

        private static object BatteryBox(object widget)
        {
            var boxes = _boxes.GetValue(widget) as IList;
            return boxes != null && boxes.Count > BatteryBoxIndex ? boxes[BatteryBoxIndex] : null;
        }

        // Rebuilt when UI Ascended rebuilds its HUD (theme switch, F9 edit, scene reload).
        private static Beta GetOrBuild(object widget)
        {
            if (_betas.TryGetValue(widget, out Beta beta))
            {
                if (beta.PanelGo != null)
                {
                    return beta;
                }
                _betas.Remove(widget);
            }
            var root = _root.GetValue(widget) as RectTransform;
            if (root == null)
            {
                return null;
            }
            var panel = (Graphic)_makePanel.Invoke(null, new object[] { root, "BatteryBeta_Box" });
            var icon = (Image)_makeIcon.Invoke(null, new object[] { root, "BatteryBeta_Icon" });
            var source = (TextMeshProUGUI)_boxLabel.GetValue(BatteryBox(widget));
            TextMeshProUGUI label = UnityEngine.Object.Instantiate(source, root, false);
            label.name = "BatteryBeta_Label";
            beta = new Beta { PanelGo = panel.gameObject, Panel = panel, Icon = icon, Label = label };
            _betas.Add(widget, beta);
            SaltysDroidDualBattery.Log("UI Ascended: added battery β box beside #5");
            return beta;
        }

        private static void SetActive(Beta beta, bool active)
        {
            if (beta.PanelGo.activeSelf != active)
            {
                beta.PanelGo.SetActive(active);
                beta.Icon.gameObject.SetActive(active);
                beta.Label.gameObject.SetActive(active);
            }
        }

        private static void Layout(object widget)
        {
            object box5 = BatteryBox(widget);
            if (box5 == null)
            {
                return;
            }
            Beta beta = GetOrBuild(widget);
            if (beta == null)
            {
                return;
            }
            var panel5 = (Component)_boxPanel.GetValue(box5);
            if (!panel5.gameObject.activeSelf)
            {
                // The theme's slot slice leaves #5 out.
                SetActive(beta, false);
                beta.Laid = false;
                return;
            }

            float size5 = (float)_panelW.GetValue(panel5);
            float size = size5 * SizeRatio;
            Vector2 c5 = ((RectTransform)panel5.transform).anchoredPosition;

            // Away from the screen centre, as LayoutMesonTag does: beside a column, above/below a row.
            Vector2 dir = IsHorizontal(widget, c5)
                ? new Vector2(0f, c5.y <= 0f ? 1f : -1f)
                : new Vector2(c5.x <= 0f ? 1f : -1f, 0f);
            beta.Offset = dir * (size5 * 0.5f + size5 * 0.12f + size * 0.5f);
            Vector2 c = c5 + beta.Offset;

            ((RectTransform)beta.Panel.transform).anchoredPosition = c;
            float radius = Mathf.Min((float)_radiusTL.Invoke(widget, NoArgs), size * 0.25f);
            _setShape.Invoke(beta.Panel, new object[] { size, size, radius, radius, radius, radius, 0f, 0f });
            beta.Icon.rectTransform.sizeDelta = new Vector2(size * 0.58f, size * 0.58f);
            beta.Icon.rectTransform.anchoredPosition = c + new Vector2(0f, size * 0.04f);
            beta.Label.rectTransform.sizeDelta = new Vector2(size * 1.6f, size * 0.3f);
            beta.Label.rectTransform.anchoredPosition = c + new Vector2(0f, -size * 0.5f + size * 0.13f);
            beta.Laid = true;
        }

        // Row or column, read from where another visible box sits relative to #5.
        private static bool IsHorizontal(object widget, Vector2 c5)
        {
            var boxes = (IList)_boxes.GetValue(widget);
            for (int i = 0; i < boxes.Count; i++)
            {
                if (i == BatteryBoxIndex)
                {
                    continue;
                }
                var panel = (Component)_boxPanel.GetValue(boxes[i]);
                if (panel != null && panel.gameObject.activeSelf)
                {
                    Vector2 d = ((RectTransform)panel.transform).anchoredPosition - c5;
                    return Mathf.Abs(d.x) > Mathf.Abs(d.y);
                }
            }
            return false;
        }

        private static void Update(object widget)
        {
            object box5 = BatteryBox(widget);
            if (box5 == null)
            {
                return;
            }
            Slot betaSlot = BetaSlot(out _);
            if (!_betas.TryGetValue(widget, out Beta beta) || beta.PanelGo == null || !beta.Laid)
            {
                if (betaSlot == null)
                {
                    return;
                }
                // The HUD may have been built (and laid out) before our patches were applied.
                Layout(widget);
                if (!_betas.TryGetValue(widget, out beta) || beta.PanelGo == null)
                {
                    return;
                }
            }

            var panel5 = (Component)_boxPanel.GetValue(box5);
            bool show = betaSlot != null && beta.Laid && panel5.gameObject.activeSelf;
            SetActive(beta, show);
            if (!show)
            {
                return;
            }

            var label5 = (TextMeshProUGUI)_boxLabel.GetValue(box5);
            string alpha = BatteryLabels.BatteryName().ToUpperInvariant() + " α";
            if (label5.text != alpha)
            {
                label5.text = alpha;
            }

            BatteryCell cell = betaSlot.Get<BatteryCell>();
            bool occupied = cell != null;

            Color fill = (Color)_fillColor.Invoke(widget, NoArgs);
            Color border = (Color)_borderColor.Invoke(widget, NoArgs);
            if (!occupied)
            {
                fill.a *= 0.55f;
                border.a *= 0.45f;
            }
            beta.Panel.color = fill;
            _pBorderColor.SetValue(beta.Panel, border);
            _pBorderWidth.SetValue(beta.Panel, _borderWidthFor.Invoke(widget, NoArgs));
            _pSheen.SetValue(beta.Panel, _sheenFor.Invoke(widget, NoArgs));
            _pSpec.SetValue(beta.Panel, occupied ? _edgeFor.Invoke(widget, NoArgs) : 0f);
            _pFeather.SetValue(beta.Panel, _featherFor.Invoke(widget, NoArgs));
            _applyMeshFx.Invoke(widget, new object[] { beta.Panel });

            Sprite sprite = occupied ? cell.GetThumbnail() : null;
            beta.Icon.sprite = sprite;
            beta.Icon.enabled = sprite != null;
            beta.Icon.color = new Color(1f, 1f, 1f, 0.95f);

            if (beta.Label.font != label5.font)
            {
                beta.Label.font = label5.font;
            }
            beta.Label.fontSize = label5.fontSize * 0.85f;
            Color text = label5.color;
            if (!occupied)
            {
                text.a *= 0.7f;
            }
            beta.Label.color = text;
            int pct = occupied ? cell.CurrentPowerPercentage : -1;
            if (pct != beta.LastPct)
            {
                beta.LastPct = pct;
                beta.Label.text = pct >= 0 ? "β " + pct + "%" : "β";
            }
        }

        private static void AddDropZone(object widget, IList zones)
        {
            Slot betaSlot = BetaSlot(out Human human);
            if (zones == null || betaSlot == null || !_betas.TryGetValue(widget, out Beta beta) || !beta.Laid ||
                !beta.PanelGo.activeSelf)
            {
                return;
            }
            object zone5 = null;
            foreach (object zone in zones)
            {
                if (ReferenceEquals(_zoneSlot.GetValue(zone), human.UniformSlot))
                {
                    zone5 = zone;
                    break;
                }
            }
            if (zone5 == null)
            {
                return;
            }
            var r5 = (Rect)_zoneRect.GetValue(zone5);
            Vector2 size = r5.size * SizeRatio;
            Vector2 centre = r5.center + beta.Offset;
            object betaZone = Activator.CreateInstance(_zoneType);
            _zoneRect.SetValue(betaZone, new Rect(centre - size * 0.5f, size));
            _zoneSlot.SetValue(betaZone, betaSlot);
            _zoneLabel.SetValue(betaZone, BatteryLabels.BatteryName().ToUpperInvariant() + " β");
            zones.Add(betaZone);
        }
    }
}
```

Notes for the implementer:
- Greek letters in string literals are fine. The repo's files are UTF-8, and `BatteryLabels.cs` already uses them.
- Postfix parameters are typed `object` (Harmony accepts a base type for injected args), because UI Ascended's types can't be named at compile time.

- [ ] **Step 2: Build.** Run the build command. Expected: clean, 0 warnings.

- [ ] **Step 3: Commit**
```bash
git add droid-dual-battery-mod/SaltysDroidDualBattery/Patches/UiAscendedBetaBox.cs
git commit -m "UI Ascended compat: battery beta box attached to #5, with drop target"
```

---

### Task 3: β in UI Ascended's #5 wheel

**Files:**
- Modify (replace stub): `droid-dual-battery-mod/SaltysDroidDualBattery/Patches/UiAscendedBetaWheel.cs`

**Interfaces:**
- Consumes: `UiAscendedCompat.Patch(...)`, `HookGuard`, `ExtraBatterySlot.Get(Human) : Slot`, `BatteryLabels.BatteryName()`.
- Produces: `UiAscendedBetaWheel.Apply(Harmony harmony)`.

UI Ascended facts this relies on (v1.0.3):
- `EquipmentKeyRadialFeature` has private `string _buttonName` (`"UniformSlot"` for #5), `List<RadialEntry> BuildRoot()` (a fresh, empty list when #5 is empty) and `bool CanOpen()` (false when #5 is empty).
- `ItemMenuBuilder.BuildSlotEntry(Slot) : RadialEntry` (public static) is the entry it builds for each slot of a bag: take to hand / drop target when occupied, stow-from-hand when empty. `RadialEntry.Sublabel` is a public string field.

- [ ] **Step 1: Replace `UiAscendedBetaWheel.cs`**

```csharp
using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using CharacterCustomisation;
using HarmonyLib;

namespace SaltysDroidDualBattery.Patches
{
    // Battery β as "contents" of UI Ascended's #5 wheel: the same slot entry UI Ascended builds
    // for each slot of a bag (take to hand, drop onto, stow from hand). With α empty the wheel
    // lists both slots, and it opens as long as β holds a battery.
    public static class UiAscendedBetaWheel
    {
        private const string WheelTypeName = "StationeersUIMod.Features.EquipmentKeyRadialFeature";
        private const string MenuBuilderTypeName = "StationeersUIMod.Features.ItemMenuBuilder";
        private const string BatteryButton = "UniformSlot";

        private static readonly HookGuard _buildGuard = new HookGuard("β wheel entry");
        private static readonly HookGuard _openGuard = new HookGuard("β wheel open");

        private static FieldInfo _buttonName;
        private static MethodInfo _buildSlotEntry;
        private static FieldInfo _sublabel;

        public static void Apply(Harmony harmony)
        {
            Type wheel = AccessTools.TypeByName(WheelTypeName);
            Type builder = AccessTools.TypeByName(MenuBuilderTypeName);
            _buttonName = wheel != null ? AccessTools.Field(wheel, "_buttonName") : null;
            _buildSlotEntry = builder != null ? AccessTools.Method(builder, "BuildSlotEntry", new[] { typeof(Slot) }) : null;
            if (_buttonName == null || _buildSlotEntry == null)
            {
                SaltysDroidDualBattery.Log("UI Ascended compat: #5 wheel members not found -- β wheel entry skipped");
                return;
            }
            _sublabel = AccessTools.Field(_buildSlotEntry.ReturnType, "Sublabel");

            UiAscendedCompat.Patch(harmony, AccessTools.Method(wheel, "BuildRoot"),
                typeof(UiAscendedBetaWheel), nameof(BuildRootPostfix), "β wheel entry");
            UiAscendedCompat.Patch(harmony, AccessTools.Method(wheel, "CanOpen"),
                typeof(UiAscendedBetaWheel), nameof(CanOpenPostfix), "β wheel open");
        }

        // The #5 feature for a droid with a β slot; everything else is left alone.
        private static Slot BetaSlotFor(object feature, out Human human)
        {
            human = null;
            if ((string)_buttonName.GetValue(feature) != BatteryButton)
            {
                return null;
            }
            human = InventoryManager.ParentHuman;
            return human != null && human.SpeciesClass == SpeciesClass.Robot ? ExtraBatterySlot.Get(human) : null;
        }

        private static void BuildRootPostfix(object __instance, object __result)
        {
            if (_buildGuard.Disabled)
            {
                return;
            }
            try
            {
                var entries = __result as IList;
                if (entries == null)
                {
                    return;
                }
                Slot beta = BetaSlotFor(__instance, out Human human);
                if (beta == null)
                {
                    return;
                }
                string name = BatteryLabels.BatteryName();
                if (human.UniformSlot != null && human.UniformSlot.Get() == null)
                {
                    // Vanilla built nothing for an empty #5: offer it as a slot too.
                    Add(entries, human.UniformSlot, name + " α");
                }
                Add(entries, beta, name + " β");
            }
            catch (Exception e)
            {
                _buildGuard.Fail(e);
            }
        }

        private static void Add(IList entries, Slot slot, string sublabel)
        {
            object entry = _buildSlotEntry.Invoke(null, new object[] { slot });
            if (entry == null)
            {
                return;
            }
            // Both slots are named "Battery"; the letter tells them apart.
            _sublabel?.SetValue(entry, sublabel);
            entries.Add(entry);
        }

        private static void CanOpenPostfix(object __instance, ref bool __result)
        {
            if (__result || _openGuard.Disabled)
            {
                return;
            }
            try
            {
                Slot beta = BetaSlotFor(__instance, out _);
                if (beta != null && beta.Get() != null)
                {
                    __result = true;
                }
            }
            catch (Exception e)
            {
                _openGuard.Fail(e);
            }
        }
    }
}
```

- [ ] **Step 2: Build.** Run the build command. Expected: clean, 0 warnings.

- [ ] **Step 3: Commit**
```bash
git add droid-dual-battery-mod/SaltysDroidDualBattery/Patches/UiAscendedBetaWheel.cs
git commit -m "UI Ascended compat: battery beta entry in the #5 wheel"
```

---

### Task 4: Docs, install, in-game verification

**Files:**
- Modify: `droid-dual-battery-mod/About/About.xml` (Version 0.3; mention UI Ascended support in Description)
- Modify: `droid-dual-battery-mod/README.md` (a "Stationeers UI Ascended" section: what shows where; credit FlorpyDorp + JXSN)
- Modify: `droid-dual-battery-mod/UpdateNotes.md` (new dated section: why, what, findings, test results)
- External (user's machine, game closed, backup first): `Documents/My Games/Stationeers/profiles/my mods.xml`

- [ ] **Step 1: Docs.** Update About.xml (`<Version>0.3</Version>`, and append to Description: " With Stationeers UI Ascended, battery 2 shows as a small β box beside #5 and in the 5 wheel."). Add the README section. Add an `UpdateNotes.md` entry `## 2026-10-13: Stationeers UI Ascended compatibility (v0.3)` summarising the spec's "What breaks today" and "Design", and linking the spec and plan.

- [ ] **Step 2: Enable UI Ascended in LaunchPad.** Ask the user first (it edits their game profile). Then, with the game closed: back up `my mods.xml` to `my mods.xml.bak-2026-10-13`. Insert, in alphabetical order by `Name` and keeping CRLF (edit with a `.py` script, not `sed`):
`  <Mod Name="Stationeers UI Ascended" Source="Workshop" DirectoryPath="C:\Program Files (x86)\Steam\steamapps\workshop\content\544550\3776545141" WorkshopHandle="3776545141" ModID="StationeersUIAscended" />`
Then set its `modconfig.xml` entry to `Enabled="true"`.

- [ ] **Step 3: Install.** Game closed: `bash build-and-install.sh`. Expected: `Installed OK (<md5>)`. Record the MD5 in UpdateNotes.

- [ ] **Step 4: Log check (user launches the game, loads a droid save).** In `Stationeers/BepInEx/LogOutput.log` expect, in order:
  `UI Ascended detected -- applying compat patches`, `UI Ascended compat: β box build patched`, `... β box layout patched`, `... β box update patched`, `... β box drop target patched`, `... β wheel entry patched`, `... β wheel open patched`, `... β wheel title patched`, `UI Ascended: added battery β box beside #5`. No `threw -- disabled` lines.

- [ ] **Step 5: Droid HUD checklist (user).** β box beside #5, #5 reads `BATTERY α`, β shows icon + `β NN%` that drops while running on β; empty β is dimmed and reads `β`. Drag a battery from hand or Universal Inventory onto the β box: it installs. Tap 5: wheel shows α (take to hand) and `Battery β`. Take β to hand, then install it back via the β entry. Empty α, tap 5: the wheel still opens with both slots.

- [ ] **Step 6: HUD rebuild (Review Focus 1).** Switch theme in F10 and back, and open/close F9: the β box is still there, and there's no `threw` line in the log.

- [ ] **Step 7: #5 hidden (Review Focus 2).** In F9 set the equipment column's "Slot count" to 4: the β box disappears. Restore it.

- [ ] **Step 8: Horizontal row (Review Focus 4).** In F9 turn on "Horizontal row" for the equipment column: the β box sits above or below #5 and doesn't cover #4/#6. Turn it back off.

- [ ] **Step 8b: Curvature / bloom (final-review finding).** Turn HUD Bloom on (this forces Dome curvature), then try the CurvedRt and Curved world canvas curvature modes: in each, the β box's panel, icon and label all render. Also turn "Show labels" off and on: the `β NN%` label follows. With α empty and β holding a battery, tap 5: the wheel's title reads "Battery", not "UniformSlot".

- [ ] **Step 9: Non-droid (Review Focus 3).** Respawn or load as a human: no β box, no extra wheel entry, #5 label is UI Ascended's own.

- [ ] **Step 9b: Wireless top-off (added 2026-10-13, separate feature).** The log shows `Wireless battery top-up patch succeeded`. Turn on Debug → VerboseLogging for start/stop lines.
  1. Wireless battery at 50% or more in β, partly drained normal battery in α: α's % rises while β's falls by the same energy.
  2. It stops once β reaches 50%.
  3. Swap the two (wireless in α): it tops off β the same way.
  4. Two wireless batteries: no transfer.
  5. Near a powered Omni transmitter, the wireless battery keeps refilling while it feeds the other one.
  6. No `Wireless top-up threw` line in the log.

- [ ] **Step 10: Without UI Ascended.** Optional, at the user's choice: remove it from the profile again and check the log shows `UI Ascended not loaded -- compat skipped` and the vanilla β button works as in v0.2.

- [ ] **Step 11: Record results in UpdateNotes and commit**
```bash
git add droid-dual-battery-mod/About/About.xml droid-dual-battery-mod/README.md droid-dual-battery-mod/UpdateNotes.md
git commit -m "Dual Battery v0.3: UI Ascended compatibility docs and test results"
```
