using System;
using System.Collections.Generic;
using UnityEngine;

namespace BloodbugMode
{
    internal static class MotherKin
    {
        // game's own names
        public const string GameRoachPerkId = "Perk_Binding_RoachMode";
        public const string GameRoachAchievement = "ACH_ROACHMODE";
        public const string GameRoachEndingLocation = "roachending";
        private const string GameRoachEndingPrefix = "GM_RoachMode";
        private const string MotherLevels = "Nest";

        public const string EndingLocation = "senkodev-bloodbugending";
        private const string EndingName = "GM_Senkodev_BloodbugEscape";
        private const string EndingTitle = "Bloodbug Mode - Escape (mod)";
        private const string LineGroup = "mother";

        // change up the lines from the roach ending a bit :3
        private static readonly Dictionary<string, string> Lines = new Dictionary<string, string>
        {
            { "nest-hunter-intro-02", "FLY, LITTLE ONE.. ESCAPE.." },
            { "nest-entrance-run", "{rand:fly.. fly...:do not stop.. fly}" },
            { "i4-greeting-protection-05", "Fly, child. Time.. slipping." },
            { "i4-greeting-roachmode-03", "Skies of the distant home." },
            { "i4-greeting-roachmode-06", "I can.. send you to the ancestral hives." },
            { "i4-greeting-roachmode-stay-05", "Fly well, cousin." },
            { "i4-greeting-roachmode-leave-01", "Thank you, cousin... child of the swarm." },
        };

        private static readonly HashSet<SSM_PerkChecker> motherChecks = new HashSet<SSM_PerkChecker>();
        private static M_Gamemode ending;

        public static bool Enabled => Plugin.MotherKinEnabled.Value;

        public static bool AppliesToRun => Enabled && Leaderboards.IsBloodbugRun;

        public static bool AppliesTo(ENT_Player player)
        {
            return Enabled && player != null && player.HasPerk(BloodbugContent.PerkId);
        }

        public static void Forget()
        {
            motherChecks.Clear();
        }

        public static void ScanLevel(M_Level level)
        {
            if (level.name.IndexOf(MotherLevels, StringComparison.OrdinalIgnoreCase) < 0) return;
            int before = motherChecks.Count;
            foreach (UT_SpawnChance spawn in level.GetComponentsInChildren<UT_SpawnChance>(true))
            {
                Collect(spawn.spawnSettings);
            }
            foreach (UT_EventChance chance in level.GetComponentsInChildren<UT_EventChance>(true))
            {
                Collect(chance.spawnSettings);
            }
            foreach (UT_Timer timer in level.GetComponentsInChildren<UT_Timer>(true))
            {
                foreach (UT_Timer.IntermediateEvent timed in timer.otherEvents)
                {
                    Collect(timed.spawnSettings);
                }
            }
            if (motherChecks.Count > before)
            {
                Plugin.Log.LogInfo($"{level.name}: {motherChecks.Count - before} Roach Mode checks also accept Bloodbug Mode");
            }
        }

        public static bool IsMotherCheck(SSM_PerkChecker check)
        {
            return motherChecks.Contains(check);
        }

        public static bool TryGetLine(string group, string key, out string line)
        {
            line = null;
            if (group != null && group != LineGroup) return false;
            return Lines.TryGetValue(key, out line);
        }

        public static bool IsGameRoachEnding(M_Gamemode gamemode)
        {
            return gamemode != null && gamemode.name.StartsWith(GameRoachEndingPrefix);
        }

        public static M_Gamemode EndingFor(M_Gamemode gameRoachEnding)
        {
            if (ending == null)
            {
                // https://docs.unity3d.com/ScriptReference/Object.Instantiate.html
                ending = ModFiles.Keep(UnityEngine.Object.Instantiate(gameRoachEnding));
                ending.name = EndingName;
                ending.gamemodeName = EndingTitle;
                ending.steamLeaderboardName = "";
                ending.allowLeaderboardScoring = false;
            }
            return ending;
        }

        private static void Collect(SpawnTable.SpawnSettings settings)
        {
            if (settings.settingModules == null) return;
            foreach (SpawnSettingModule module in settings.settingModules)
            {
                if (module is SSM_PerkChecker check && check.perkID == GameRoachPerkId)
                {
                    motherChecks.Add(check);
                }
            }
        }
    }
}
