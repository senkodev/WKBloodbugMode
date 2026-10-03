using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BloodbugMode
{
    internal static class BindingFramework
    {
        public const string Guid = "com.cicismods.trinketandbindingframework";

        private const string RegistryType = "TrinketAndBindingFramework.TrinketRegistry, TrinketAndBindingFramework";

        public static bool Present => Chainloader.PluginInfos.ContainsKey(Guid);

        public static bool TryRegister(string id, string title, string description, float scoreMultiplierBonus, Sprite icon, Func<List<Perk>> perks, string excludes)
        {
            if (!Present) return false;
            try
            {
                Type registry = Type.GetType(RegistryType);
                MethodInfo register = registry.GetMethod("RegisterBinding", BindingFlags.Public | BindingFlags.Static);
                MethodInfo mutex = registry.GetMethod("RegisterMutexHub", BindingFlags.Public | BindingFlags.Static);
                register.Invoke(null, new object[] { id, title, description, "", 1, scoreMultiplierBonus, 0f, icon, null, perks, 0 });
                mutex.Invoke(null, new object[] { id, new[] { excludes } });
                Plugin.Log.LogInfo("binding registered through CiCi's framework");
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"CiCi's framework is installed but the binding could not be registered through it, using the game's own list instead: {e.Message}");
                return false;
            }
        }
    }
}
