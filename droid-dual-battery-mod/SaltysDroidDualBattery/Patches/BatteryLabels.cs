using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace SaltysDroidDualBattery.Patches
{
    // HUD labels for the two droid battery buttons: the game's own localized name for the droid
    // battery slot, plus α / β, so they read correctly in every language without new strings.
    //
    // The #5 button's label is a TextMeshPro text driven by a LocalizedText component keyed to
    // "Uniform"; vanilla never relabels it when it turns the slot into the droid battery slot.
    // LocalizedText.Refresh runs on load and on every language change, and it also swaps the font
    // for the language (CJK fonts), so it is kept and our text is applied after it (postfix)
    // rather than removing the component.
    public static class BatteryLabels
    {
        // Same StringKey vanilla gives the droid battery slot, so Localization.GetName finds the
        // translated slot name (Localization.SlotsName[StringHash]) in the current language.
        private static readonly Slot NameKey = new Slot
        {
            StringKey = ExtraBatterySlot.SlotKey,
            StringHash = Animator.StringToHash(ExtraBatterySlot.SlotKey),
        };

        private static LocalizedText _alpha;
        private static LocalizedText _beta;
        private static bool _droidMode;

        public static string AlphaText => BatteryName() + " α"; // α
        public static string BetaText => BatteryName() + " β"; // β

        // Called once per HUD build with the vanilla #5 button and our clone.
        public static void Attach(SlotDisplay source, SlotDisplay clone)
        {
            _alpha = source.SlotText != null ? source.SlotText.GetComponent<LocalizedText>() : null;
            _beta = clone.SlotText != null ? clone.SlotText.GetComponent<LocalizedText>() : null;
            if (_alpha == null || _beta == null)
            {
                SaltysDroidDualBattery.Log("HUD: battery label has no LocalizedText -- labels set once, " +
                                           "won't follow language changes");
            }
            Apply(source.SlotText, clone.SlotText);
        }

        // True while the local player is a droid. Humans and Zrilians share the #5 button, so
        // leaving droid mode hands the label back to vanilla's LocalizedText ("Uniform").
        public static void SetDroidMode(bool droid, SlotDisplay source, SlotDisplay clone)
        {
            _droidMode = droid;
            if (droid)
            {
                Apply(source?.SlotText, clone?.SlotText);
            }
            else if (_alpha != null)
            {
                _alpha.Refresh();
            }
        }

        private static void Apply(TMP_Text alpha, TMP_Text beta)
        {
            if (!_droidMode)
            {
                return;
            }
            if (alpha != null)
            {
                alpha.text = AlphaText;
            }
            if (beta != null)
            {
                beta.text = BetaText;
            }
        }

        // Missing translation -> Localization returns "<N:lang:key>"; fall back to English.
        private static string BatteryName()
        {
            string name = Localization.GetName(NameKey);
            return string.IsNullOrEmpty(name) || name.StartsWith("<") ? "Battery" : name;
        }

        public static void OnLocalizedTextRefreshed(LocalizedText text)
        {
            if (text == null || text.TextMesh == null)
            {
                return;
            }
            if (text == _beta)
            {
                // The clone is only ever shown to droids.
                text.TextMesh.text = BetaText;
            }
            else if (text == _alpha && _droidMode)
            {
                text.TextMesh.text = AlphaText;
            }
        }
    }

    // Re-applies α / β after vanilla re-localizes a label (load, language change).
    [HarmonyPatch(typeof(LocalizedText), nameof(LocalizedText.Refresh))]
    public static class BatteryLabelLocalizationPatch
    {
        public static void Postfix(LocalizedText __instance)
        {
            BatteryLabels.OnLocalizedTextRefreshed(__instance);
        }
    }
}
