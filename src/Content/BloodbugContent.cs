using System.Collections.Generic;
using TrinketAndBindingFramework;
using UnityEngine;

namespace BloodbugMode
{
    internal static class BloodbugContent
    {
        public const string PerkId = "Perk_Senkodev_BloodbugForm";
        public const string LightBodyId = "Perk_Senkodev_BloodbugLightBody";
        public const string WetWingsId = "Perk_Senkodev_BloodbugWetWings";
        public const string WingInjuryId = "Perk_Senkodev_BloodbugWingInjury";
        public const string BindingId = "Senkodev_Bloodbug";
        public const string TrinketName = "Trinket_" + BindingId;
        public const string DatabaseId = "senkodev-bloodbugmode";
        public const string FormBuffId = "senkodev-bloodbugform";

        public static Perk Perk { get; private set; }
        public static Sprite Icon { get; private set; }
        public static Sprite Pixel { get; private set; }

        public static Perk LightBody { get; private set; }
        public static Perk WetWings { get; private set; }
        public static Perk WingInjury { get; private set; }

        private static WKAssetDatabase database;

        public static void RegisterBinding()
        {
            Create();
            TrinketRegistry.RegisterBinding(
                BindingId,
                "Bloodbug Mode",
                "You are a <color=red>Bloodbug</color>.\nFly on limited stamina and land on walls to rest. Don't hit them too fast.",
                scoreMultiplierBonus: Balance.ScoreMultiplierBonus,
                icon: Icon,
                perksToGrantFactory: () => new List<Perk> { Perk });
            TrinketRegistry.RegisterMutexHub(BindingId, BindingRules.GameRoachBinding);

            NoHammerBindingRegistrar.AddExternalSource(() => Leaderboards.IsBloodbugRun);
        }

        public static void Register()
        {
            if (CL_AssetManager.baseDatabase == null) return;
            CL_AssetManager.AddNewDatabase(DatabaseId, database);
            if (GameAssets.Load())
            {
                UseGameCardArt();
            }
        }

        private static void Create()
        {
            Icon = ModFiles.LoadSprite("binding_icon.png", "Binding_Icon_Bloodbug");
            Pixel = ModFiles.WhitePixel();

            Perk = NewPerk(PerkId, "Bloodbug Mode", Perk.PerkType.binding, Icon);
            Perk.description = "(+) You fly, for as long as your wings hold out.\n(+) You can land on a wall or ceiling to rest.\n(+) V to charge at a denizen or to feed on what is in reach.\n(+) Bloodbugs are friendly.\n(-) You are short-sighted and blind to red (protanopia).\n(-) You are small and slow on foot. Hitting anything at speed hurts.\n(-) You can't use the hammer, climb on handholds, throw rebar and crush grubs.\n(-) You can't place pitons or use computers.";
            Perk.flavorText = "bzzzzzz";
            Perk.tags = new List<string> { "binding" };
            Perk.playerTag = new List<string> { BloodbugKin.Tag };
            Perk.modules = new List<PerkModule> { new BloodbugModule() };
            Perk.useBuff = true;
            Perk.buff = new BuffContainer
            {
                id = FormBuffId,
                desc = "",
                loseOverTime = false,
                loseRate = 0f,
                buffs = new List<BuffContainer.Buff>
                {
                    new BuffContainer.Buff { id = "addPlayerScale", maxAmount = Balance.Scale - 1f }
                }
            };

            LightBody = NewPerk(LightBodyId, "Light Body", Perk.PerkType.red, ModFiles.LoadSprite("debuff_light_body.png", "Debuff_Icon_LightBody"));
            WetWings = NewPerk(WetWingsId, "Wet Wings", Perk.PerkType.red, ModFiles.LoadSprite("debuff_wet_wings.png", "Debuff_Icon_WetWings"));
            WingInjury = NewPerk(WingInjuryId, "Wing Injury", Perk.PerkType.red, ModFiles.LoadSprite("debuff_wing_injury.png", "Debuff_Icon_WingInjury"));
            WingInjury.canStack = true;

            database = ModFiles.Keep(ScriptableObject.CreateInstance<WKAssetDatabase>());
            database.name = DatabaseId;
            database.id = DatabaseId;
            database.perkAssets.AddRange(new[] { Perk, LightBody, WetWings, WingInjury });

            LightBody.description = $"(-) You weigh next to nothing. Blows, blasts, fans and steam throw you {Percent(Balance.LightBodyKnockback - 1f)}% further.";
            WetWings.description = $"(-) Your wings are soaked. Flying costs {Percent(Balance.WetDrainMultiplier - 1f)}% more stamina until they dry out.";
            WingInjury.description = $"(-) A hard crash has torn your wings. Each injury takes {Percent(Balance.InjuryStaminaLoss)}% off your flight stamina until it heals.";
            WingInjury.stackMax = Balance.MaxInjuries;
        }

        private static Perk NewPerk(string id, string title, Perk.PerkType type, Sprite icon)
        {
            Perk perk = ModFiles.Keep(ScriptableObject.CreateInstance<Perk>());
            perk.name = id;
            perk.id = id;
            perk.title = title;
            perk.description = "";
            perk.flavorText = "";
            perk.icon = icon;
            perk.perkType = type;
            perk.sortingCategory = -10;
            perk.spawnPool = Perk.PerkPool.never;
            perk.spawnInEndless = false;
            perk.competitive = false;
            perk.removeOnCleanse = false;
            perk.canStack = false;
            perk.stackMax = 1;
            perk.multiplierCurve = AnimationCurve.Constant(0f, 1f, 1f);
            perk.useBuff = false;
            perk.buff = new BuffContainer { id = "", desc = "", buffs = new List<BuffContainer.Buff>() };
            perk.useBaseBuff = false;
            perk.baseBuff = new BuffContainer { id = "", desc = "", buffs = new List<BuffContainer.Buff>() };
            perk.tags = new List<string>();
            perk.flags = new List<string>();
            perk.playerTag = new List<string>();
            perk.modules = new List<PerkModule>();
            perk.unlockProgressionID = "";
            return perk;
        }

        private static void UseGameCardArt()
        {
            Perk source = GameAssets.HalfInventory;
            if (source == null) return;
            foreach (Perk perk in new[] { Perk, LightBody, WetWings, WingInjury })
            {
                perk.perkCard = source.perkCard;
                perk.perkFrame = source.perkFrame;
                perk.iconMat = source.iconMat;
            }
        }

        private static string Percent(float fraction)
        {
            return Mathf.RoundToInt(fraction * 100f).ToString();
        }
    }
}
