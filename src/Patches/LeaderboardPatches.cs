using HarmonyLib;

namespace BloodbugMode
{
    [HarmonyPatch(typeof(M_Gamemode), nameof(M_Gamemode.Finish))]
    internal static class GamemodeFinishPatch
    {
        private static void Prefix(M_Gamemode __instance, out bool? __state)
        {
            __state = null;
            if (Leaderboards.IsBloodbugRun && !AlreadySkipsScoring(__instance))
            {
                __state = CL_Leaderboard.WK_Leaderboard_Core.disableLeaderboards;
                CL_Leaderboard.WK_Leaderboard_Core.disableLeaderboards = true;
                Plugin.Log.LogInfo("bloodbug run, not submitting to leaderboards");
            }
        }

        private static void Postfix(M_Gamemode __instance, bool hasFinished, bool? __state)
        {
            bool payBonus = __state.HasValue
                && hasFinished
                && __instance.winCredits > 0
                && CL_GameManager.GetRoaches(global: true) < __instance.winCreditMaximum
                && !__instance.IsCompetitive();
            if (payBonus)
            {
                CL_ProgressionManager.ShowUnlockPopup(__instance.roachEndSprite, "WIN BONUS",
                    $"{__instance.winCredits} Facility Credits Added", DarkMachineCrayons.gold * 0.5f, null, addToSession: false);
                CL_GameManager.AddRoaches(__instance.winCredits, global: true);
            }
        }

        // https://harmony.pardeike.net/articles/patching-finalizer.html
        private static void Finalizer(bool? __state)
        {
            if (__state.HasValue)
            {
                CL_Leaderboard.WK_Leaderboard_Core.disableLeaderboards = __state.Value;
            }
        }

        private static bool AlreadySkipsScoring(M_Gamemode gamemode)
        {
            return (!gamemode.allowCheatedScores && CommandConsole.hasCheated)
                || (!gamemode.allowCheatedScores && !CL_GameManager.AreAchievementsAllowed())
                || WorldLoader.customSeed
                || !CL_GameManager.gMan.allowScores
                || !gamemode.allowLeaderboardScoring;
        }
    }

    [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.UploadGamemodeResults))]
    internal static class SteamUploadPatch
    {
        private static bool Prefix()
        {
            return !Leaderboards.IsBloodbugRun;
        }
    }

    internal static class Leaderboards
    {
        private static bool flown;

        public static bool IsBloodbugRun
        {
            get
            {
                ENT_Player player = ENT_Player.playerObject;
                return flown || (player != null && player.HasPerk(BloodbugContent.PerkId));
            }
        }

        public static void MarkRun()
        {
            flown = true;
        }

        public static void NewRun()
        {
            flown = false;
        }
    }
}
