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
        public const string pluginName = "AirlockCardMod";
        public const string pluginVersion = "1.0";
        public static void Log(string line)
        {
            Debug.Log("[" + pluginName + "]: " + line);
        }
        void Awake()
        {
            try
            {
                var harmony = new Harmony(pluginGuid);
                harmony.PatchAll();
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
