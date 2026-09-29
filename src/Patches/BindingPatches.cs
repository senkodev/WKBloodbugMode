using HarmonyLib;
using System.Collections.Generic;

namespace BloodbugMode
{
    [HarmonyPatch(typeof(UI_TrinketPicker), nameof(UI_TrinketPicker.ReloadTrinkets))]
    internal static class TrinketPickerReloadPatch
    {
        // https://harmony.pardeike.net/articles/patching-injections.html
        private static void Postfix(UI_TrinketPicker __instance, M_Gamemode ___currentGamemode)
        {
            List<Trinket> selected = __instance.selectedTrinkets;
            if (!selected.Exists(BindingRules.IsBloodbug)) return;

            if (BindingRules.AddCompanions(selected, ___currentGamemode).Count > 0)
            {
                BindingRules.Save(___currentGamemode, selected);
                AccessTools.Method(typeof(UI_TrinketPicker), "UpdateTrinketActivation").Invoke(__instance, null);
            }
        }
    }

    [HarmonyPatch(typeof(UI_TrinketPicker), nameof(UI_TrinketPicker.SelectTrinket))]
    internal static class TrinketPickerSelectPatch
    {
        private static bool Prefix(UI_TrinketPicker __instance, Trinket t, M_Gamemode ___currentGamemode, out string __state)
        {
            __state = null;
            List<Trinket> selected = __instance.selectedTrinkets;
            if (selected.Contains(t))
            {
                if (BindingRules.IsLocked(t, selected))
                {
                    __instance.ShowDescription(t.GetDescription() + "\n<color=red>Comes with the Bloodbug Mode</color>");
                    return false;
                }
                return true;
            }

            if (BindingRules.IsBloodbug(t))
            {
                List<Trinket> added = BindingRules.AddCompanions(selected, ___currentGamemode);
                if (added.Count > 0)
                {
                    string names = string.Join(" and ", added.ConvertAll(companion => companion.title));
                    __state = $"{names} {(added.Count > 1 ? "come" : "comes")} with it.";
                }
            }
            return true;
        }

        private static void Postfix(UI_TrinketPicker __instance, Trinket t, string __state)
        {
            if (__state != null)
            {
                __instance.ShowDescription(t.GetDescription() + $"\n<color=red>{__state}</color>");
            }
        }
    }

    [HarmonyPatch(typeof(M_Gamemode), nameof(M_Gamemode.StartFreshGamemode))]
    internal static class GamemodeStartPatch
    {
        private static void Prefix(M_Gamemode __instance)
        {
            BindingRules.FixSavedSelection(__instance);
        }
    }
}
