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
