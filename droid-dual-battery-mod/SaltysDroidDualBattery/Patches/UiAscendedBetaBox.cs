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

            // Built inside UI Ascended's own BuildContent, like its Meson tag, so the HUD's
            // one-time setup that follows (layer, world materials, fx list) includes β.
            UiAscendedCompat.Patch(harmony, AccessTools.Method(column, "BuildContent", new[] { typeof(RectTransform) }),
                typeof(UiAscendedBetaBox), nameof(BuildContentPostfix), "β box build");
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

        private static void BuildContentPostfix(object __instance)
        {
            if (_layoutGuard.Disabled)
            {
                return;
            }
            try
            {
                GetOrBuild(__instance);
            }
            catch (Exception e)
            {
                _layoutGuard.Fail(e);
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
                Hide(__instance);
                return;
            }
            try
            {
                Update(__instance);
            }
            catch (Exception e)
            {
                _updateGuard.Fail(e);
                Hide(__instance);
            }
        }

        // With the update hook off, β would otherwise freeze on screen with stale data.
        private static void Hide(object widget)
        {
            try
            {
                if (_betas.TryGetValue(widget, out Beta beta) && beta.PanelGo != null)
                {
                    SetActive(beta, false);
                }
            }
            catch
            {
                // Nothing more we can do; the guard has already logged.
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
            // Built late (HUD existed before our patches): match the HUD's render layer, which
            // UI Ascended's camera-rendered curvature modes cull by.
            panel.gameObject.layer = icon.gameObject.layer = label.gameObject.layer = root.gameObject.layer;
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
            // Follow the theme's "Show labels" toggle like the other boxes.
            beta.Label.enabled = label5.enabled;
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
