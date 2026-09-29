using HarmonyLib;
using UnityEngine;

namespace BloodbugMode
{
    [HarmonyPatch(typeof(CL_AssetManager), nameof(CL_AssetManager.InitializeAssetManager))]
    internal static class AssetManagerReadyPatch
    {
        private static void Postfix()
        {
            BloodbugContent.Register();
        }
    }

    [HarmonyPatch(typeof(CL_Handhold), nameof(CL_Handhold.CanInteract))]
    internal static class HandholdCanInteractPatch
    {
        private static void Postfix(CL_Handhold __instance, Interaction info, ref bool __result)
        {
            if (__result && BloodbugController.BlocksClimbing(info.player, __instance))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(ENT_Player.Hand), nameof(ENT_Player.Hand.GrabHold))]
    internal static class HandGrabHoldPatch
    {
        // https://harmony.pardeike.net/articles/patching-prefix.html
        private static bool Prefix(ENT_Player.Hand __instance, Transform target)
        {
            return !BloodbugController.BlocksClimbing(__instance.GetPlayer(), target.GetComponent<CL_Handhold>());
        }
    }

    [HarmonyPatch(typeof(OS_Computer_Interface), nameof(OS_Computer_Interface.ActivateComputer))]
    internal static class ComputerUsePatch
    {
        private static bool Prefix()
        {
            return !BloodbugController.IsBloodbug(ENT_Player.playerObject);
        }
    }
}
