using HarmonyLib;
using UnityEngine;

namespace BloodbugMode
{
    [HarmonyPatch(typeof(ENT_Player), nameof(ENT_Player.Start))]
    internal static class PlayerStartPatch
    {
        private static void Postfix()
        {
            Leaderboards.NewRun();
        }
    }

    [HarmonyPatch(typeof(ENT_Player), "Movement")]
    internal static class PlayerMovementPatch
    {
        private static void Prefix(ENT_Player __instance)
        {
            BloodbugController.For(__instance)?.BeforeMovement();
        }

        private static void Postfix(ENT_Player __instance)
        {
            BloodbugController.For(__instance)?.AfterMovement();
        }
    }

    [HarmonyPatch(typeof(ENT_Player), "OnControllerColliderHit")]
    internal static class PlayerColliderHitPatch
    {
        private static void Prefix(ENT_Player __instance, ControllerColliderHit hit)
        {
            BloodbugController.For(__instance)?.OnSurfaceHit(hit);
        }
    }

    [HarmonyPatch(typeof(ENT_Player), nameof(ENT_Player.AddForce), typeof(Vector3), typeof(string))]
    internal static class PlayerAddForcePatch
    {
        private static void Prefix(ENT_Player __instance, ref Vector3 v, string source)
        {
            v *= BloodbugController.KnockbackMultiplier(__instance, source);
        }
    }

    [HarmonyPatch(typeof(UT_ForceZone), "FixedUpdate")]
    internal static class ForceZonePatch
    {
        private static void Prefix()
        {
            BloodbugController.activeForceZones++;
        }

        private static void Finalizer()
        {
            BloodbugController.activeForceZones--;
        }
    }

    // the game carries props by their mass, so half the strength is the same as twice the weight
    [HarmonyPatch(typeof(ENT_Player), nameof(ENT_Player.GrabPropUpdate))]
    internal static class PropCarryPatch
    {
        private static void Prefix(ENT_Player __instance, int hand, out (Rigidbody body, float mass) __state)
        {
            __state = default;
            CL_Prop prop = __instance.hands[hand].grabTarget;
            if (prop == null || BloodbugItems.IsPlank(prop) || !BloodbugController.IsBloodbug(__instance)) return;

            Rigidbody body = prop.GetRigidbody();
            __state = (body, body.mass);
            body.mass /= Balance.CarryStrength;
        }

        private static void Finalizer((Rigidbody body, float mass) __state)
        {
            if (__state.body != null)
            {
                __state.body.mass = __state.mass;
            }
        }
    }

    [HarmonyPatch(typeof(ENT_Player), "SetCameraFov")]
    internal static class CameraFovPatch
    {
        private static void Postfix(ENT_Player __instance)
        {
            BloodbugController bug = BloodbugController.For(__instance);
            if (bug == null) return;

            float widened = bug.WidenView(__instance.cam.fieldOfView);
            __instance.cam.fieldOfView = widened;
            __instance.inventoryCam.fieldOfView = widened;
        }
    }
}
