using System;
using System.Linq;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using HarmonyLib;
using UnityEngine;

namespace SaltysDroidStandby.Patches
{
    // The standby key lives in vanilla's Settings -> Controls -> Inventory list as "Droid
    // Standby" (row label = KeyItem.Name.ToProper()). Vanilla saves every KeyManager.AllKeys
    // entry by name, so a rebind persists like any built-in key.
    //
    // Normal path: postfix on KeyManager.SetupKeyBindings. Settings.LoadSettings calls it and
    // THEN applies the saved bindings by name, and Settings.SetupValues builds one Controls row
    // per AllKeys entry afterwards -- so both happen for free.
    // Late path (mod loaded after that already ran): register now, apply the saved binding
    // from Settings.CurrentData.KeyList ourselves, and build the row with vanilla's
    // ControlItemPrefab. The row matters beyond looks: KeyManager.AssignDefaultKeys ("reset
    // to defaults") calls Display on every key and would throw without one.
    public static class KeyBinding
    {
        public const string Name = "DroidStandby";
        public const string AnchorKey = "UniformSlot"; // join the Inventory group with #5
        private const KeyCode DefaultKey = KeyCode.Z;

        public static KeyCode Key
        {
            get
            {
                KeyItem item = KeyManager.GetKeyitem(Name);
                return item != null ? item.Key : DefaultKey;
            }
        }

        public static void EnsureRegistered(bool latePath)
        {
            try
            {
                if (KeyManager.GetKeyitem(Name) != null) return;
                ControlsGroup group = KeyManager.GetControlsGroup(AnchorKey);
                if (group == null) return; // vanilla setup hasn't run yet; the postfix will do it

                AccessTools.Method(typeof(KeyManager), "AddKey")
                    .Invoke(null, new object[] { Name, DefaultKey, group, false });
                KeyItem item = KeyManager.GetKeyitem(Name);
                if (item == null) return;

                if (latePath)
                {
                    KeyItem saved = Settings.CurrentData?.KeyList?.FirstOrDefault(k => k != null && k.Name == Name);
                    if (saved != null) item.Key = saved.Key;

                    Settings settings = Settings.Instance;
                    if (settings != null && group.Transform != null && group.Transform.Find("Control" + Name) == null)
                    {
                        ControlsAssignment row = UnityEngine.Object.Instantiate(settings.ControlItemPrefab, group.Transform);
                        row.Created(item, KeyManager.AllKeys.IndexOf(item));
                        row.name = "Control" + Name;
                    }
                }
                SaltysDroidStandby.Log("Standby key registered in Settings > Controls > Inventory (" +
                                       (latePath ? "late path" : "with vanilla setup") + "), bound to " + item.Key);
            }
            catch (Exception e)
            {
                SaltysDroidStandby.Log("Standby key registration failed; using " + DefaultKey);
                SaltysDroidStandby.Log(e.ToString());
            }
        }
    }

    [HarmonyPatch(typeof(KeyManager), nameof(KeyManager.SetupKeyBindings))]
    public static class KeyBindingPatch
    {
        public static void Postfix() => KeyBinding.EnsureRegistered(latePath: false);
    }
}
