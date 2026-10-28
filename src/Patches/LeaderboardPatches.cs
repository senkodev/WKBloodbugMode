using System.Reflection;
using System.Threading.Tasks;
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

    [HarmonyPatch]
    internal static class PlaceScorePatch
    {
        private static MethodBase TargetMethod()
        {
            return typeof(SteamManager).GetNestedType("SteamLeaderboardApi", BindingFlags.NonPublic).GetMethod("PlaceScore");
        }

        private static bool Prefix(ref Task<(bool scoreChanged, Task detailsUploadFinished)> __result)
        {
            if (!Leaderboards.IsBloodbugRun) return true;
            Plugin.Log.LogInfo("Bloodbug mode run, leaderboard upload was skipped");
            __result = Task.FromResult((false, Task.CompletedTask));
            return false;
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
