using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BloodbugMode
{
    [HarmonyPatch]
    internal static class BlockedShotPatch
    {
        private static readonly string[] Methods = { "Use", "StartShoot", "Shoot" };

        // https://harmony.pardeike.net/articles/patching-auxiliary.html
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (Type type in AccessTools.GetTypesFromAssembly(typeof(HandItem_Shoot).Assembly))
            {
                if (!typeof(HandItem_Shoot).IsAssignableFrom(type)) continue;
                foreach (string name in Methods)
                {
                    MethodInfo method = AccessTools.DeclaredMethod(type, name, Type.EmptyTypes);
                    if (method != null && !method.IsAbstract)
                    {
                        yield return method;
                    }
                }
            }
        }

        private static bool Prefix(HandItem_Shoot __instance)
        {
            bool blocked = (BloodbugItems.IsRebar(__instance.item) || BloodbugItems.IsPiton(__instance.item))
                && BloodbugController.IsBloodbug(__instance.hand.GetPlayer());
            if (blocked)
            {
                BloodbugItems.Refuse(__instance);
            }
            return !blocked;
        }
    }

    [HarmonyPatch(typeof(HandItem_Piton), nameof(HandItem_Piton.Use))]
    internal static class PitonUsePatch
    {
        private static bool Prefix(HandItem_Piton __instance)
        {
            bool blocked = BloodbugController.IsBloodbug(__instance.hand.GetPlayer());
            if (blocked)
            {
                BloodbugItems.Refuse(__instance);
            }
            return !blocked;
        }
    }

    [HarmonyPatch(typeof(HandItem_Piton), nameof(HandItem_Piton.PitonHit))]
    internal static class PitonHitPatch
    {
        private static bool Prefix(HandItem_Piton __instance)
        {
            return !BloodbugController.IsBloodbug(__instance.hand.GetPlayer());
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.DropItemIntoWorld))]
    internal static class ItemDropPatch
    {
        // https://harmony.pardeike.net/articles/patching-injections.html
        private static void Prefix(Item item, out float? __state)
        {
            __state = null;
            ENT_Player player = ENT_Player.playerObject;
            if (!BloodbugController.IsBloodbug(player)) return;

            float scale = BloodbugItems.IsRebar(item) ? 0f : Balance.ThrowStrength;
            __state = item.dropVel;
            item.dropVel = BloodbugItems.ScaledDropVel(item.dropVel, scale);
        }

        private static void Finalizer(Item item, float? __state)
        {
            if (__state.HasValue)
            {
                item.dropVel = __state.Value;
            }
        }
    }

    [HarmonyPatch(typeof(HandItem_Shoot), "CreateProjectile")]
    internal static class ThrowStrengthPatch
    {
        private static void Prefix(HandItem_Shoot __instance, out float? __state)
        {
            __state = null;
            bool thrown = BloodbugItems.IsThrown(__instance)
                && BloodbugController.IsBloodbug(__instance.hand.GetPlayer());
            if (thrown)
            {
                __state = __instance.shootSpeed;
                __instance.shootSpeed *= Balance.ThrowStrength;
            }
        }

        private static void Finalizer(HandItem_Shoot __instance, float? __state)
        {
            if (__state.HasValue)
            {
                __instance.shootSpeed = __state.Value;
            }
        }
    }

    [HarmonyPatch(typeof(HandItem_Melee), nameof(HandItem_Melee.Use))]
    internal static class HammerUsePatch
    {
        private static bool Prefix(HandItem_Melee __instance)
        {
            bool blocked = BloodbugItems.BlocksSwing(__instance);
            if (blocked)
            {
                BloodbugItems.Refuse(__instance);
            }
            return !blocked;
        }
    }

    [HarmonyPatch(typeof(HandItem_Melee), nameof(HandItem_Melee.StartUse))]
    internal static class HammerSwingPatch
    {
        private static bool Prefix(HandItem_Melee __instance)
        {
            return !BloodbugItems.BlocksSwing(__instance);
        }
    }

    [HarmonyPatch(typeof(HandItem_Melee), nameof(HandItem_Melee.Hit))]
    internal static class HammerHitPatch
    {
        private static bool Prefix(HandItem_Melee __instance)
        {
            return !BloodbugItems.BlocksSwing(__instance);
        }
    }

    // prevent the player from crushing grubs
    [HarmonyPatch(typeof(HandItem_Buff), nameof(HandItem_Buff.StartBuff))]
    internal static class GrubCrushPatch
    {
        private static bool Prefix(HandItem_Buff __instance)
        {
            bool blocked = BloodbugItems.IsGrub(__instance.item)
                && BloodbugController.IsBloodbug(__instance.hand.GetPlayer());
            if (blocked)
            {
                BloodbugItems.Refuse(__instance);
            }
            return !blocked;
        }
    }

    [HarmonyPatch(typeof(HandItem_Buff), nameof(HandItem_Buff.Activate))]
    internal static class BuffItemActivatePatch
    {
        private static void Postfix(HandItem_Buff __instance)
        {
            ENT_Player player = CL_GameManager.gMan.localPlayer;
            BloodbugItems.MarkBuff(__instance, player);

            if (__instance.onEatType != BloodbugController.FoodEatType)
            {
                BloodbugController.For(player)?.Eat(BloodbugItems.Meals(__instance));
            }
        }
    }
}
