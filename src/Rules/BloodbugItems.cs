using System;
using UnityEngine;

namespace BloodbugMode
{
    internal static class BloodbugItems
    {
        public const string InjectorMark = "senkodev-bloodbug:injector";
        public const string PillsMark = "senkodev-bloodbug:pills";
        public const string AllowedTag = "senkodev-bloodbug:allowed";

        private const string RemoteMarkerName = "Item_Artifact_Remote_Handhold";
        private const float FoodPerMeal = 10f;
        private const float GameTossSpeed = 2f;
        private const float RefuseInterval = 0.6f;

        private static float lastRefused;

        // public static bool IsRebar(Item item) => IsKind(item, "rebar") && !IsKind(item, "explosive");
        public static bool IsRebar(Item item) => IsKind(item, "rebar");

        public static bool IsPiton(Item item) => IsKind(item, "piton");

        public static bool IsHammer(Item item) => IsKind(item, "hammer");

        // allow spawned in hammers
        public static bool IsBanned(Item item) => IsHammer(item) && !item.HasTag(AllowedTag);

        public static void Allow(Item item)
        {
            if (!item.HasTag(AllowedTag)) item.itemTags.Add(AllowedTag);
        }

        public static float RoachStamina(Item item)
        {
            if (IsKind(item, "ruby")) return Balance.RubyRoachStamina;
            if (IsKind(item, "platinum")) return Balance.PlatinumRoachStamina;
            return Balance.RoachStamina;
        }

        public static bool IsEdibleRoach(HandItem tool)
        {
            return !(tool is HandItem_Buff) && !(tool is HandItem_Food) && IsKind(tool.item, "roach");
        }

        public static bool IsThrown(HandItem_Shoot item) => !item.useAmmo;

        public static bool IsPlank(CL_Prop prop) => Contains(prop.name, "plank") || Contains(prop.objectType, "plank");

        public static bool IsRemoteMarker(Component handhold)
        {
            return handhold.gameObject.name.StartsWith(RemoteMarkerName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool BlocksSwing(HandItem_Melee tool)
        {
            return IsBanned(tool.item) && BloodbugController.IsBloodbug(tool.hand.GetPlayer());
        }

        public static void MarkBuff(HandItem_Buff used, ENT_Player player)
        {
            string mark;
            if (IsKind(used.item, "injector") && Gives(used.buff, "roided"))
            {
                mark = InjectorMark;
            }
            else if (IsKind(used.item, "pill") && Gives(used.buff, "pilled"))
            {
                mark = PillsMark;
            }
            else
            {
                return;
            }

            BuffContainer running = player.curBuffs.currentBuffs.Contains(used.buff)
                ? used.buff
                : player.curBuffs.GetBuffContainer(used.buff.id);
            if (running != null)
            {
                running.desc = mark;
            }
        }

        public static float InjectorStrength(ENT_Player player)
        {
            float strength = 0f;
            foreach (BuffContainer buff in player.curBuffs.currentBuffs)
            {
                if (buff.desc == InjectorMark)
                {
                    strength += Mathf.Clamp01(buff.buffTime) * buff.GetMultiplier();
                }
            }
            return strength;
        }

        public static bool OnPills(ENT_Player player)
        {
            foreach (BuffContainer buff in player.curBuffs.currentBuffs)
            {
                bool fresh = 1f - buff.buffTime < Balance.PillSeconds * buff.loseRate;
                if (buff.desc == PillsMark && buff.buffTime > 0f && fresh) return true;
            }
            return false;
        }

        public static float Meals(HandItem_Buff used)
        {
            float food = 0f;
            foreach (BuffContainer.Buff buff in used.buff.buffs)
            {
                if (buff.id == "food")
                {
                    food += buff.maxAmount;
                }
            }
            return food / FoodPerMeal;
        }

        public static float ScaledDropVel(float dropVel, float scale)
        {
            return (dropVel + GameTossSpeed) * scale - GameTossSpeed;
        }

        public static void RemoveHammers(Inventory inventory)
        {
            if (inventory == null) return;
            for (int i = inventory.bagItems.Count - 1; i >= 0; i--)
            {
                Item item = inventory.bagItems[i];
                if (!IsBanned(item)) continue;
                item.ClearDropObject();
                inventory.bagItems.RemoveAt(i);
            }
            for (int hand = 0; hand < inventory.itemHands.Length; hand++)
            {
                Item held = inventory.itemHands[hand].currentItem;
                if (held != null && IsBanned(held))
                {
                    inventory.DestroyItemInHand(hand);
                }
            }
            inventory.CalculateEncumberance();
        }

        public static void Refuse(HandItem item)
        {
            if (Time.time < lastRefused + RefuseInterval) return;
            lastRefused = Time.time;

            if (item is HandItem_Shoot shoot && shoot.clipHandler != null && shoot.clipHandler.HasSound("item:noAmmo"))
            {
                shoot.clipHandler.PlaySound("item:noAmmo");
            }
            item.hand.ShakeHand(0.03f);
        }

        private static bool IsKind(Item item, string kind)
        {
            if (Contains(item.prefabName, kind) || Contains(item.itemTag, kind)) return true;
            if (item.itemTags != null)
            {
                foreach (string tag in item.itemTags)
                {
                    if (Contains(tag, kind)) return true;
                }
            }
            return false;
        }

        private static bool Contains(string text, string part)
        {
            return text != null && text.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool Gives(BuffContainer container, string stat)
        {
            foreach (BuffContainer.Buff buff in container.buffs)
            {
                if (buff.id == stat) return true;
            }
            return false;
        }
    }
}
