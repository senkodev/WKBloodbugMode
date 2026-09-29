using HarmonyLib;
using UnityEngine;

namespace BloodbugMode
{
    internal static class PlayerAccess
    {
        public const float VelocityToMetres = 175f;

        public const float InputAcceleration = 0.15f;

        // https://harmony.pardeike.net/articles/utilities.html
        private static readonly AccessTools.FieldRef<ENT_Player, Vector3> vel =
            AccessTools.FieldRefAccess<ENT_Player, Vector3>("vel");

        private static readonly AccessTools.FieldRef<ENT_Player, float> camSway =
            AccessTools.FieldRefAccess<ENT_Player, float>("camSway");

        public static ref Vector3 Vel(ENT_Player player)
        {
            return ref vel(player);
        }

        public static ref float CamSway(ENT_Player player)
        {
            return ref camSway(player);
        }
    }
}
