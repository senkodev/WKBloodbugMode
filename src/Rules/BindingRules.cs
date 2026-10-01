namespace BloodbugMode
{
    internal static class BindingRules
    {
        public const string GameRoachBinding = "Binding_RoachMode";

        public static bool IsBloodbug(Trinket trinket)
        {
            return trinket.name == BloodbugContent.TrinketName;
        }

        public static bool IsRoach(Trinket trinket)
        {
            return trinket.name == GameRoachBinding;
        }

        public static void RemoveRoachPerks(ENT_Player player)
        {
            Trinket roach = CL_AssetManager.GetTrinketAsset(GameRoachBinding);
            if (roach == null || !IsRoach(roach)) return;
            foreach (Perk perk in roach.perksToGrant)
            {
                if (player.HasPerk(perk.id))
                {
                    player.RemovePerk(perk.id);
                    Plugin.Log.LogInfo($"removed {perk.title}, it can't be combined with Bloodbug Mode");
                }
            }
        }
    }
}
