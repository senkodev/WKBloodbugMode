using DarkMachine.AI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BloodbugMode
{
    [HarmonyPatch(typeof(DEN_BasicDenizen), nameof(DEN_BasicDenizen.Start))]
    internal static class DenizenStartPatch
    {
        private static void Postfix(DEN_BasicDenizen __instance)
        {
            if (BloodbugKin.IsBloodbug(__instance))
            {
                BloodbugKin.IgnorePlayer(__instance.targetComponent);
            }
            if (NearHearing.On)
            {
                NearHearing.Adjust(__instance.GetComponentsInChildren<AudioSource>(true));
            }
        }
    }

    [HarmonyPatch]
    internal static class DenizenDeathPatch
    {
        // https://harmony.pardeike.net/articles/patching-auxiliary.html
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (Type type in AccessTools.GetTypesFromAssembly(typeof(Denizen).Assembly))
            {
                if (!typeof(Denizen).IsAssignableFrom(type)) continue;
                foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
                {
                    bool dies = method.Name == "Damage" || (method.Name == "Kill" && method.GetParameters().Length == 2);
                    if (dies && !method.IsAbstract && !method.IsStatic)
                    {
                        yield return method;
                    }
                }
            }
        }

        private static void Postfix(Denizen __instance)
        {
            if (__instance.dead && BloodbugController.Exists)
            {
                BodyOutline.Track(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(DEN_Roach), "AI")]
    internal static class RoachCrushPatch
    {
        private static void Prefix(DEN_Roach __instance, out bool __state)
        {
            __state = __instance.canSquish && BloodbugController.IsBloodbug(ENT_Player.playerObject);
            if (__state)
            {
                __instance.canSquish = false;
            }
        }

        private static void Finalizer(DEN_Roach __instance, bool __state)
        {
            if (__state)
            {
                __instance.canSquish = true;
            }
        }
    }

    internal static class BloodbugKin
    {
        public const string Tag = "Senkodev_BloodbugKin";

        public static bool IsBloodbug(GameEntity entity)
        {
            return entity.name.StartsWith("Denizen_Bloodbug", StringComparison.OrdinalIgnoreCase)
                || (entity.objectType != null && entity.objectType.IndexOf("bloodbug", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static void IgnorePlayer(AITargetComponent targeting)
        {
            var tags = new List<string>(targeting.targetIgnoreTags);
            if (!tags.Contains(Tag))
            {
                tags.Add(Tag);
                targeting.targetIgnoreTags = tags.ToArray();
            }
        }
    }
}
