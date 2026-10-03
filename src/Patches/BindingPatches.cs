using HarmonyLib;
using System.Collections.Generic;

namespace BloodbugMode
{
    // Runs after other mods prefixes
    // https://harmony.pardeike.net/articles/priorities.html
    [HarmonyPatch(typeof(UI_TrinketPicker), nameof(UI_TrinketPicker.ReloadTrinkets))]
    [HarmonyPriority(Priority.Low)]
    internal static class TrinketPickerReloadPatch
    {
        private static void Prefix(M_Gamemode ___currentGamemode)
        {
            TrinketList offered = ___currentGamemode.availableTrinkets;
            if (offered == null || offered.bindings == null || BloodbugContent.Binding == null) return;
            if (!offered.bindings.Contains(BloodbugContent.Binding))
            {
                offered.bindings.Add(BloodbugContent.Binding);
            }
        }
    }

    [HarmonyPatch(typeof(UI_TrinketPicker), nameof(UI_TrinketPicker.SelectTrinket))]
    internal static class TrinketPickerSelectPatch
    {
        private static void Prefix(UI_TrinketPicker __instance, Trinket t)
        {
            if (BloodbugContent.Binding == null) return;
            List<Trinket> selected = __instance.selectedTrinkets;
            if (selected.Contains(t)) return;
            if (BindingRules.IsBloodbug(t))
            {
                selected.RemoveAll(BindingRules.IsRoach);
            }
            else if (BindingRules.IsRoach(t))
            {
                selected.RemoveAll(BindingRules.IsBloodbug);
            }
        }
    }
}
