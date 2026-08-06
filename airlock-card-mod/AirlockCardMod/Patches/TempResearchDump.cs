using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using HarmonyLib;

namespace AirlockCardMod.Patches
{
    // TEMPORARY, 2026-08-06 -- not part of the mod's real design.
    // One-shot research dump for the IC10 loose-ends pass (see
    // airlock-ic10-scripts/ic10_airlock_code_notes.md's "still open" list on
    // main): BtnHash (the Logic Switch/Button PrefabHash) and the LED
    // Color enum values are both Unity Inspector-serialized runtime
    // data, not present in the compiled IL (same category as
    // OnThreadUpdate's TickSpeed, see PATCH_PLAN.md) -- so they can't
    // be resolved via static decompilation alone. This dumps the real
    // runtime values once, piggybacking on the same OnThreadUpdate
    // attachment point AdvancedAirlockFailsafePatch already uses
    // (guarantees the game is fully loaded, GameManager and the
    // prefab registry both populated). Remove this file once the
    // dump has been captured and the IC10 docs updated.
    [HarmonyPatch(typeof(AirlockControlBase), nameof(AirlockControlBase.OnThreadUpdate))]
    internal static class TempResearchDump
    {
        private static bool dumped;

        private static void Postfix()
        {
            if (dumped) return;
            dumped = true;

            DumpCustomColors();
            DumpPrefabsMatching("switch", "button", "lever");
        }

        private static void DumpCustomColors()
        {
            var sb = new StringBuilder();
            sb.Append("[Salty's Advanced Airlock]: RESEARCH -- CustomColors (index: name):\n");
            var colors = Assets.Scripts.Util.Singleton<Assets.Scripts.GameManager>.Instance.CustomColors;
            for (int i = 0; i < colors.Count; i++)
            {
                sb.Append(i).Append(": ").Append(colors[i].Name).Append('\n');
            }
            UnityEngine.Debug.Log(sb.ToString());
        }

        private static void DumpPrefabsMatching(params string[] needles)
        {
            var sb = new StringBuilder();
            sb.Append("[Salty's Advanced Airlock]: RESEARCH -- prefabs matching switch/button/lever (hash: PrefabName):\n");
            foreach (var thing in Prefab.AllPrefabs)
            {
                if (thing == null) continue;
                string name = thing.PrefabName ?? "";
                string lower = name.ToLowerInvariant();
                bool match = false;
                foreach (var needle in needles)
                {
                    if (lower.Contains(needle)) { match = true; break; }
                }
                if (match)
                {
                    sb.Append(thing.PrefabHash).Append(": ").Append(name).Append('\n');
                }
            }
            UnityEngine.Debug.Log(sb.ToString());
        }
    }
}
