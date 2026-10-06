using HarmonyLib;
using System;
using UnityEngine;

namespace AirlockCardMod
{
    #region BepInEx
    [BepInEx.BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class AirlockCardMod : BepInEx.BaseUnityPlugin
    {
        public const string pluginGuid = "com.username.AirlockCardMod";
        public const string pluginName = "Salty's Advanced Airlock";
        public const string pluginVersion = "1.0";
        public static void Log(string line)
        {
            Debug.Log("[" + pluginName + "]: " + line);
        }
        // StationeersLaunchPad calls Awake() more than once (config-UI load + session load).
        // An unguarded PatchAll() would stack every postfix twice, running each failsafe tick
        // twice. Plain BepInEx (DLL dropped in BepInEx/plugins) calls it once; the guard is
        // harmless there.
        private static bool _patched;

        void Awake()
        {
            if (_patched)
            {
                Log("Already patched by an earlier Awake() call -- skipping");
                return;
            }
            try
            {
                var harmony = new Harmony(pluginGuid);
                harmony.PatchAll();
                _patched = true;
                Log("Patch succeeded");

            }
            catch (Exception e)
            {

                Log("Patch Failed");
                Log(e.ToString());
            }
        }
    }
    #endregion
}
