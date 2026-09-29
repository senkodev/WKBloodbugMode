using HarmonyLib;
using UnityEngine;

namespace BloodbugMode
{
    [HarmonyPatch(typeof(M_Level), "Awake")]
    internal static class LevelAwakePatch
    {
        private static void Postfix(M_Level __instance)
        {
            MotherKin.ScanLevel(__instance);
            if (BloodbugController.Exists)
            {
                BodyOutline.TrackDead(__instance.GetComponentsInChildren<Denizen>(true));
            }
            if (NearHearing.On)
            {
                NearHearing.Adjust(__instance.GetComponentsInChildren<AudioSource>(true));
            }
        }
    }

    [HarmonyPatch(typeof(SSM_PerkChecker), nameof(SSM_PerkChecker.Check))]
    internal static class MotherPerkCheckPatch
    {
        // https://harmony.pardeike.net/articles/patching-postfix.html
        private static void Postfix(SSM_PerkChecker __instance, ref float __result)
        {
            if (MotherKin.IsMotherCheck(__instance) && MotherKin.AppliesTo(ENT_Player.GetPlayer()))
            {
                __result = __instance.trueValue;
            }
        }
    }

    [HarmonyPatch(typeof(CL_LocalizationManager.Localization), nameof(CL_LocalizationManager.Localization.GetLine))]
    internal static class MotherLinePatch
    {
        private static void Postfix(string group, string key, ref string __result)
        {
            if (MotherKin.TryGetLine(group, key, out string line) && MotherKin.AppliesToRun)
            {
                __result = line;
            }
        }
    }

    [HarmonyPatch(typeof(CL_AchievementManager), nameof(CL_AchievementManager.SetAchievementValue), typeof(string), typeof(bool))]
    internal static class RoachAchievementPatch
    {
        private static bool Prefix(string name)
        {
            return name != MotherKin.GameRoachAchievement || !MotherKin.AppliesToRun;
        }
    }

    [HarmonyPatch(typeof(UT_LocationDataCollector), nameof(UT_LocationDataCollector.LogData))]
    internal static class EndingVisitPatch
    {
        private static void Prefix(UT_LocationDataCollector __instance, out bool __state)
        {
            __state = __instance.locationName == MotherKin.GameRoachEndingLocation && MotherKin.AppliesToRun;
            if (__state)
            {
                __instance.locationName = MotherKin.EndingLocation;
            }
        }

        private static void Finalizer(UT_LocationDataCollector __instance, bool __state)
        {
            if (__state)
            {
                __instance.locationName = MotherKin.GameRoachEndingLocation;
            }
        }
    }

    [HarmonyPatch(typeof(UT_GameStateController), nameof(UT_GameStateController.SetGamemode))]
    internal static class EndingSetGamemodePatch
    {
        private static void Prefix(ref M_Gamemode gamemode)
        {
            if (MotherKin.IsGameRoachEnding(gamemode) && MotherKin.AppliesToRun)
            {
                gamemode = MotherKin.EndingFor(gamemode);
            }
        }
    }

    [HarmonyPatch(typeof(UT_GameStateController), nameof(UT_GameStateController.LoadGamemode))]
    internal static class EndingLoadGamemodePatch
    {
        private static void Prefix(ref M_Gamemode gm)
        {
            if (MotherKin.IsGameRoachEnding(gm) && MotherKin.AppliesToRun)
            {
                gm = MotherKin.EndingFor(gm);
            }
        }
    }
}
