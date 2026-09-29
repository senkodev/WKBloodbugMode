using System;
using System.Collections.Generic;

namespace BloodbugMode
{
    internal static class BindingRules
    {
        public const string GameRoachBinding = "Binding_RoachMode";

        private static readonly string[] GameCompanions = { "Binding_HalfInventory", "Binding_Survival" };

        public static bool IsBloodbug(Trinket trinket)
        {
            return trinket.name == BloodbugContent.TrinketName;
        }

        public static bool IsRoach(Trinket trinket)
        {
            return trinket.name == GameRoachBinding;
        }

        public static bool IsCompanion(Trinket trinket)
        {
            return Array.IndexOf(GameCompanions, trinket.name) >= 0;
        }

        public static bool IsLocked(Trinket trinket, List<Trinket> selected)
        {
            return IsCompanion(trinket) && selected.Exists(IsBloodbug);
        }

        public static List<Trinket> AddCompanions(List<Trinket> selected, M_Gamemode gamemode)
        {
            var added = new List<Trinket>();
            foreach (string name in GameCompanions)
            {
                Trinket companion = FindOffered(gamemode, name);
                if (companion != null && !selected.Contains(companion))
                {
                    selected.Add(companion);
                    added.Add(companion);
                }
            }
            return added;
        }

        public static void Save(M_Gamemode gamemode, List<Trinket> selected)
        {
            var names = new List<string>();
            foreach (Trinket trinket in selected)
            {
                names.Add(trinket.name);
            }
            StatManager.saveData.SetGamemodeTrinkets(gamemode.GetGamemodeName(), names);
        }

        public static void FixSavedSelection(M_Gamemode gamemode)
        {
            string name = gamemode.GetGamemodeName();
            List<string> saved = StatManager.saveData.GetGamemodeTrinkets(name);
            if (!saved.Contains(BloodbugContent.TrinketName)) return;

            var fixedList = new List<string>(saved);
            bool changed = false;
            foreach (string companion in GameCompanions)
            {
                if (!fixedList.Contains(companion) && FindOffered(gamemode, companion) != null)
                {
                    fixedList.Add(companion);
                    changed = true;
                }
            }
            if (changed)
            {
                StatManager.saveData.SetGamemodeTrinkets(name, fixedList);
            }
        }

        public static void RemoveRoachPerks(ENT_Player player)
        {
            Trinket roach = CL_AssetManager.GetTrinketAsset(GameRoachBinding);
            if (roach == null || !IsRoach(roach)) return;
            foreach (Perk perk in roach.perksToGrant)
            {
                if (player.HasPerk(perk.id))
                {
                    player.RemovePerk(perk.id);
                    Plugin.Log.LogInfo($"removed {perk.title}, it can't be combined with Bloodbug Mode");
                }
            }
        }

        private static Trinket FindOffered(M_Gamemode gamemode, string name)
        {
            if (gamemode.availableTrinkets == null) return null;
            Trinket found = gamemode.availableTrinkets.bindings.Find(binding => binding.name == name);
            if (found == null || found.comingSoon || !found.IsUnlocked()) return null;
            foreach (string setting in found.settingBlacklist)
            {
                if (gamemode.HasActiveSetting(setting)) return null;
            }
            return found;
        }
    }
}
