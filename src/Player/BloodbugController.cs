using UnityEngine;

namespace BloodbugMode
{
    public partial class BloodbugController : MonoBehaviour
    {
        public enum FlightState
        {
            Inactive,
            Grounded,
            Flying,
            Perched,
            Falling,
            Resting
        }

        private static BloodbugController current;

        private ENT_Player player;
        private bool active;
        private bool leaving;
        private int surfaceMask;

        public FlightState State { get; private set; }

        public static BloodbugController For(ENT_Player player)
        {
            return current != null && current.player == player ? current : null;
        }

        public static bool Exists => current != null;

        public static bool IsBloodbug(ENT_Player player)
        {
            BloodbugController controller = For(player);
            return controller != null && controller.active;
        }

        public static bool BlocksClimbing(ENT_Player player, CL_Handhold handhold)
        {
            return IsBloodbug(player) && !BloodbugItems.IsRemoteMarker(handhold);
        }

        internal void Leave()
        {
            leaving = true;
        }

        private void Awake()
        {
            current = this;
            player = GetComponent<ENT_Player>();
            wobbleSeed = new Vector3(Random.Range(0f, 100f), Random.Range(0f, 100f), Random.Range(0f, 100f));

            // https://docs.unity3d.com/ScriptReference/Physics.GetIgnoreLayerCollision.html
            for (int layer = 0; layer < 32; layer++)
            {
                if (layer != gameObject.layer && !Physics.GetIgnoreLayerCollision(gameObject.layer, layer))
                {
                    surfaceMask |= 1 << layer;
                }
            }
        }

        private void Update()
        {
            if (leaving)
            {
                if (active)
                {
                    Deactivate(keepDebuffs: false);
                }
                else
                {
                    RemoveDebuffs();
                }
                Destroy(this);
                return;
            }

            if (!player.dead && !active)
            {
                Activate();
            }
            else if (player.dead && active)
            {
                Deactivate(keepDebuffs: true);
            }
            if (!active) return;

            if (InputManager.GetButton("Jump").Down)
            {
                jumpPressed = true;
            }
            ReadBiteKey();

            UpdateDebuffs();
            UpdateEffects();
        }

        private void OnDestroy()
        {
            Plugin.BiteKey.ConfigFile.SettingChanged -= OnSettingChanged;
            DestroyEffects();
            NearHearing.Forget();
            BodyOutline.Forget();
            MotherKin.Forget();
        }

        private void Activate()
        {
            active = true;
            State = FlightState.Grounded;
            stunTimer = 0f;
            pendingCrashDamage = 0f;

            BloodbugContent.Register();
            GivePerks();
            UpdateDebuffs();
            Stamina = MaxStamina;

            player._OnEat += OnEat;

            Leaderboards.MarkRun();
            CreateEffects();
            ApplySettings();
            Plugin.BiteKey.ConfigFile.SettingChanged += OnSettingChanged;
        }

        private void Deactivate(bool keepDebuffs)
        {
            active = false;
            State = FlightState.Inactive;
            IsAfloat = false;
            IsClinging = false;
            thrust = 0f;
            prey = null;
            EndCharge(0f);

            player.fly = false;
            player._OnEat -= OnEat;
            Plugin.BiteKey.ConfigFile.SettingChanged -= OnSettingChanged;
            if (!keepDebuffs)
            {
                RemoveDebuffs();
            }
            HideEffects();
        }

        internal const string FoodEatType = "food";

        private const float MaxInjuryLoss = 0.9f;
        private const float MinTakeoffStamina = 10f;
        private const float RecoverStamina = 25f;

        public float Stamina { get; private set; }

        public bool OnPills { get; private set; }

        public float MaxStamina
        {
            get
            {
                float lost = Mathf.Min(Balance.InjuryStaminaLoss * Injuries, MaxInjuryLoss);
                return Balance.MaxStamina * (1f - lost);
            }
        }

        internal void Eat(float meals)
        {
            if (!active || meals <= 0f) return;
            Stamina = Mathf.Min(Stamina + Balance.FoodStamina * meals, MaxStamina);
            meter.Punch();
        }

        private void OnEat(string type)
        {
            if (type == FoodEatType)
            {
                Eat(1f);
            }
        }

        private void Recover(float rate, float dt)
        {
            Stamina = Mathf.Min(Stamina + rate * dt, MaxStamina);
        }

        private FlightState AirborneState()
        {
            return Stamina >= MinTakeoffStamina ? FlightState.Flying : FlightState.Falling;
        }
    }
}
