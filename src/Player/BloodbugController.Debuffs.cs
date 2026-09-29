using System.Collections.Generic;
using UnityEngine;

namespace BloodbugMode
{
    public partial class BloodbugController
    {
        internal static int activeForceZones;

        private readonly List<string> givenPerks = new List<string>();

        private float wetTimer;
        private float injuryTimer;
        private Perk wetWings;
        private Perk injury;

        public int Injuries { get; private set; }

        public bool HasWetWings => wetTimer > 0f;

        internal static float KnockbackMultiplier(ENT_Player player, string source)
        {
            bool external = !string.IsNullOrEmpty(source) || activeForceZones > 0;
            return external && IsBloodbug(player) ? Balance.LightBodyKnockback : 1f;
        }

        private void UpdateDebuffs()
        {
            UpdateWetWings();
            UpdateInjuries();
            Stamina = Mathf.Min(Stamina, MaxStamina);
        }

        private void UpdateWetWings()
        {
            if (player.HasEffect("wet"))
            {
                wetTimer = Balance.WetSeconds;
            }
            else
            {
                wetTimer = Mathf.Max(wetTimer - Time.deltaTime, 0f);
            }

            if (wetTimer > 0f && wetWings == null)
            {
                wetWings = player.AddPerk(BloodbugContent.WetWings);
            }
            else if (wetTimer <= 0f && wetWings != null)
            {
                player.RemovePerk(wetWings);
                wetWings = null;
            }
            if (wetWings != null)
            {
                wetWings.SetBarValue(Mathf.Clamp01(wetTimer / Balance.WetSeconds));
            }
        }

        private void UpdateInjuries()
        {
            if (injury == null) return;

            if (injuryTimer <= 0f)
            {
                injuryTimer = Balance.InjurySeconds;
            }
            injuryTimer -= Time.deltaTime;
            injury.SetBarValue(Mathf.Clamp01(injuryTimer / Balance.InjurySeconds));

            if (injuryTimer <= 0f)
            {
                PlayHealedSound();
                Injuries--;
                player.RemovePerk(injury, removeAll: false);
                if (Injuries <= 0)
                {
                    injury = null;
                }
            }
        }

        private void InjureWing()
        {
            if (Injuries < Balance.MaxInjuries)
            {
                injury = player.AddPerk(BloodbugContent.WingInjury);
                Injuries++;
            }
            injuryTimer = Balance.InjurySeconds;
            Stamina = Mathf.Min(Stamina, MaxStamina);
        }

        private void RemoveDebuffs()
        {
            string[] mods = { BloodbugContent.LightBodyId, BloodbugContent.WetWingsId, BloodbugContent.WingInjuryId };
            foreach (string id in mods)
            {
                player.RemovePerk(id);
            }
            foreach (string id in givenPerks)
            {
                player.RemovePerk(id);
            }

            givenPerks.Clear();
            wetWings = null;
            injury = null;
            hunger = null;
            wetTimer = 0f;
            injuryTimer = 0f;
            Injuries = 0;
        }

        private void GivePerks()
        {
            BindingRules.RemoveRoachPerks(player);
            if (!player.HasPerk(BloodbugContent.LightBodyId))
            {
                player.AddPerk(BloodbugContent.LightBody);
            }
            wetWings = player.GetPerk(BloodbugContent.WetWingsId);
            injury = player.GetPerk(BloodbugContent.WingInjuryId);
            Injuries = injury != null ? injury.GetStackAmount() : 0;

            // foreach (Perk perk in new[] { GameAssets.HalfInventory, GameAssets.Survival, GameAssets.CarnalBloodlust })
            foreach (Perk perk in new[] { GameAssets.HalfInventory, GameAssets.Survival })
            {
                if (perk != null && !player.HasPerk(perk.id))
                {
                    player.AddPerk(perk);
                    givenPerks.Add(perk.id);
                }
            }
            hunger = FindHunger();
        }
    }
}
