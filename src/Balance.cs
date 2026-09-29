namespace BloodbugMode
{
    internal static class Balance
    {
        // body
        public const float Scale = 0.4f;
        public const float WalkSpeed = 0.65f;
        public const float ThrowStrength = 0.5f;
        public const float CarryStrength = 0.5f;

        // debuffs
        public const float LightBodyKnockback = 1.6f;
        public const float WetDrainMultiplier = 2f;
        public const float WetSeconds = 20f;
        public const float InjurySpeed = 12.5f;
        public const float InjuryStaminaLoss = 0.25f;
        public const float InjurySeconds = 60f;
        public const int MaxInjuries = 3;

        // flight in meters per second
        public const float CruiseSpeed = 8f;
        public const float BurstSpeed = 14f;
        public const float Responsiveness = 0.7f;
        public const float Wobble = 0.7f;

        // hunger
        public const float HungryBelow = 0.4f;
        public const float StarvingSpeed = 0.6f;

        // stamina per second
        public const float MaxStamina = 70f;
        public const float HoverDrain = 9f;
        public const float CruiseDrain = 14f;
        public const float BurstDrain = 18f;
        public const float GroundRegen = 14f;
        public const float PerchRegen = 11f;
        public const float FoodStamina = 32f;

        // by default the game's effect runs for 100 seconds slowly fading out by the end
        // balanced out to 35 seconds not to make the pills way too OP
        public const float PillSeconds = 35f;

        // water
        public const float FloatSeconds = 5f;

        // crashing into walls and the ceilings
        public const float CrashSpeed = 10f;
        public const float CrashBaseDamage = 0.5f;
        public const float CrashDamagePerSpeed = 0.2f;

        // bite
        public const float ChargeDamage = 2f;
        public const float ChargeStamina = 20f;
        public const float ChargeCooldown = 2.5f;
        public const float FeedStaminaRate = 30f;
        public const float FeedHealRate = 0.3f;
        public const float BloodPerDenizen = 60f;

        public const float ScoreMultiplierBonus = 0.35f;
        public const float BugViewSharpTo = 3.2f;

        // fixes camera issues on higher fov
        public const float NearClip = 0.375f;

        // hearing perception in metesr
        public const float HearingNear = 4f;
        public const float HearingFar = 15f;

        // hearing cutoff and resonance
        public const float HearingCutoff = 1200f;
        public const float HearingResonance = 1.5f;
    }
}
