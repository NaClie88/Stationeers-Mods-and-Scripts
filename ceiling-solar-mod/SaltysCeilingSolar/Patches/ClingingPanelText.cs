using Assets.Scripts;
using HarmonyLib;

namespace SaltysCeilingSolar.Patches
{
    // Thing.DisplayName and the Stationpedia resolve names through Localization.GetThingName /
    // GetThingDescription keyed by PrefabName. A postfix answers for the clinging panel and
    // survives language changes, which rebuild the game's own tables.
    [HarmonyPatch(typeof(Localization), nameof(Localization.GetThingName))]
    public static class ClingingPanelNamePatch
    {
        public const string Name = "Solar Panel (Clinging)";

        public static void Postfix(string thingPrefabName, ref string __result)
        {
            if (thingPrefabName == ClingingPanelPrefabPatch.ClingingName)
            {
                __result = Name;
            }
        }
    }

    [HarmonyPatch(typeof(Localization), nameof(Localization.GetThingDescription))]
    public static class ClingingPanelDescriptionPatch
    {
        public const string Description =
            "A basic solar panel built to cling to the inside of a wall or ceiling with its cells facing the " +
            "surface it is mounted on. Fit it against glass and it collects sunlight from outside while staying " +
            "sheltered from storms inside a sealed room.";

        public static void Postfix(string thingPrefabName, ref string __result)
        {
            if (thingPrefabName == ClingingPanelPrefabPatch.ClingingName)
            {
                __result = Description;
            }
        }
    }
}
