using System.Collections.Generic;

namespace BloodbugMode
{
    internal class HandCosmeticSwap
    {
        private readonly List<Cosmetic_HandItem>[] mods = new List<Cosmetic_HandItem>[2];
        private readonly List<Cosmetic_HandItem>[] originals = new List<Cosmetic_HandItem>[2];

        public bool Active { get; private set; }

        public void Apply(ENT_Player player, bool wanted)
        {
            Cosmetic_HandItem bugHands = wanted ? BugHands.Cosmetic : null;
            if (bugHands == null)
            {
                Restore(player);
                return;
            }

            for (int i = 0; i < player.hands.Length; i++)
            {
                ENT_Player.Hand hand = player.hands[i];
                if (mods[i] == null)
                {
                    mods[i] = new List<Cosmetic_HandItem> { bugHands };
                }

                if (hand.currentCosmetics != mods[i])
                {
                    originals[i] = hand.currentCosmetics;
                    hand.currentCosmetics = mods[i];
                }
                else if (mods[i].Count != 1 || mods[i][0] != bugHands)
                {
                    // something else added to the list like the player changing thei cosmetics midrun
                    mods[i].Remove(bugHands);
                    originals[i] = new List<Cosmetic_HandItem>(mods[i]);
                    mods[i].Clear();
                    mods[i].Add(bugHands);
                }
            }
            Active = true;
        }

        public void Restore(ENT_Player player)
        {
            if (!Active) return;
            Active = false;

            for (int i = 0; i < player.hands.Length; i++)
            {
                ENT_Player.Hand hand = player.hands[i];
                if (hand.currentCosmetics == mods[i])
                {
                    hand.currentCosmetics = originals[i];
                }
                originals[i] = null;
            }
        }
    }
}
