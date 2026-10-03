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

    [HarmonyPatch(typeof(ENV_Recycler), "Start")]
    internal static class RecyclerGrind
    {
        public const string DamageType = "recycler";

        private static void Postfix(ENV_Recycler __instance)
        {
            ENT_Player player = ENT_Player.playerObject;
            if (BloodbugController.IsBloodbug(player))
            {
                Admit(__instance, player);
            }
        }

        // Originally two box colliders cover the hopper, so the climber can't get in
        // This allows the player to enter the recycler when in Bloodbug Mode by ignoring collissions on the hopper
        public static void Admit(ENV_Recycler recycler, ENT_Player player)
        {
            recycler.mask = recycler.mask | (1 << player.gameObject.layer);
            foreach (BoxCollider plug in recycler.recycleZone.transform.parent.GetComponentsInChildren<BoxCollider>())
            {
                if (!plug.isTrigger && plug.name == "Collider")
                {
                    Physics.IgnoreCollision(player.cCon, plug);
                }
            }
        }
    }

    [HarmonyPatch(typeof(UT_PlayerForceMover), "FixedUpdate")]
    internal static class PlayerDraggedPatch
    {
        private static void Postfix(UT_PlayerForceMover __instance)
        {
            if (!__instance.active || __instance.playerPullForce <= 0f) return;
            ENT_Player player = ENT_Player.playerObject;
            if (!BloodbugController.IsBloodbug(player)) return;

            float distance = Vector3.Distance(__instance.targetPoint.position, player.transform.position);
            if (!__instance.limitByDistance || distance <= __instance.distanceLimit)
            {
                BloodbugController.draggedAt = Time.fixedTime;
            }
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
