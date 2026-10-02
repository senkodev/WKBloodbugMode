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

    // Originally the shot starts 0.4 m behind the player camera, which is fine for the climber, but not for the bloodbug due to its scale
    // This patch moves the shot origin to the camera position and scales the distance to the camera by the bloodbug scale
    [HarmonyPatch(typeof(HandItem_Shoot), "CreateProjectile")]
    internal static class ShotOriginPatch
    {
        private static void Prefix(HandItem_Shoot __instance, ref Vector3 startPos)
        {
            if (!BloodbugController.IsBloodbug(__instance.hand.GetPlayer())) return;
            Vector3 eye = Camera.main.transform.position;
            startPos = eye + (startPos - eye) * Balance.Scale;
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

    [HarmonyPatch(typeof(Item_Object), nameof(Item_Object.CanPickup))]
    internal static class HammerPickupPatch
    {
        private static void Postfix(Item_Object __instance, ref bool __result)
        {
            if (!__result || __instance.itemData == null) return;
            if (BloodbugItems.IsBanned(__instance.itemData) && BloodbugController.IsBloodbug(ENT_Player.playerObject))
            {
                __result = false;
            }
        }
    }

    // The starting hammer is removed, but a spawned one is alowed
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItemToInventoryScreen))]
    internal static class HammerBagPatch
    {
        private static bool Prefix(Item item)
        {
            if (!BloodbugItems.IsHammer(item) || !BloodbugController.IsBloodbug(ENT_Player.playerObject)) return true;
            if (ConsoleSpawnPatch.Spawning)
            {
                BloodbugItems.Allow(item);
                return true;
            }
            item.ClearDropObject();
            return false;
        }
    }

    [HarmonyPatch(typeof(CL_GameManager), "SpawnItem")]
    internal static class ConsoleSpawnPatch
    {
        internal static bool Spawning;

        private static void Prefix()
        {
            Spawning = true;
        }

        private static void Finalizer()
        {
            Spawning = false;
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

    // Every roach hand item shares the animator with the eat animation of the Lemon Roach
    [HarmonyPatch(typeof(HandItem), nameof(HandItem.Use))]
    internal static class RoachEatPatch
    {
        private static void Prefix(HandItem __instance)
        {
            if (__instance.used || !BloodbugItems.IsEdibleRoach(__instance)) return;
            ENT_Player player = __instance.hand.GetPlayer();
            if (!BloodbugController.IsBloodbug(player)) return;

            Animator animator = __instance.anim != null ? __instance.anim : __instance.GetComponent<Animator>();
            if (animator == null) return;

            __instance.used = true;
            animator.SetTrigger("Use");
            if (GameAssets.RoachEatClip != null)
            {
                AudioManager.PlaySound(GameAssets.RoachEatClip, player.transform, GameAssets.RoachEatVolume, 1f, 0f);
            }
        }
    }

    [HarmonyPatch(typeof(HandItem), nameof(HandItem.Activate))]
    internal static class RoachSwallowPatch
    {
        private static void Postfix(HandItem __instance)
        {
            if (!__instance.used || !BloodbugItems.IsEdibleRoach(__instance)) return;
            BloodbugController.For(__instance.hand.GetPlayer())?.EatRoach(__instance.item);
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
