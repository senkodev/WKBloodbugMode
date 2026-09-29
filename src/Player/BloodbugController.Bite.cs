using System;
using System.Collections.Generic;
using UnityEngine;

namespace BloodbugMode
{
    public partial class BloodbugController
    {
        private enum Charge
        {
            None,
            WindingUp,
            Dashing
        }

        internal const float FeedReach = 1.6f;

        private const float ChargeWindup = 0.5f;
        private const float ChargeSpeed = 20f;
        private const float ChargeDuration = 0.45f;
        private const float ChargeKnockback = 3f;
        private const float FeedDamageRate = 0.5f;
        private const float StrikeReach = 0.6f;
        private const float StrikeRadius = 0.7f;
        private const float ReboundSpeed = 4f;
        private const float FeedTickSeconds = 0.5f;

        internal static Func<bool> BiteOverride;

        private static readonly Collider[] overlaps = new Collider[48];
        private static KeyCode lastCheckedKey = KeyCode.None;

        private readonly Dictionary<int, float> bloodLeft = new Dictionary<int, float>();

        private Charge charge;
        private float chargeTimer;
        private float chargeCooldown;
        private Vector3 dashDirection;
        private bool biteHeld;
        private bool bitePressed;
        private GameEntity prey;
        private float feedTick;
        private PerkModule_HungerMeter hunger;

        public bool IsCharging => charge != Charge.None;

        public bool IsFeeding => prey != null;

        internal GameEntity Prey => prey;

        internal float BloodLeft(GameEntity entity)
        {
            return bloodLeft.TryGetValue(entity.GetInstanceID(), out float left) ? left : Balance.BloodPerDenizen;
        }

        internal static string GameBindingFor(KeyCode key)
        {
            if (key == KeyCode.None) return null;
            // https://guavaman.com/projects/rewired/docs/HowTos.html
            Rewired.Player input = Rewired.ReInput.players.GetPlayer(0);
            foreach (Rewired.ControllerMap map in input.controllers.maps.GetAllMaps(Rewired.ControllerType.Keyboard))
            {
                foreach (Rewired.ActionElementMap binding in map.AllMaps)
                {
                    Rewired.InputAction action = Rewired.ReInput.mapping.GetAction(binding.actionId);
                    if (binding.keyCode.ToString() == key.ToString() && action != null && action.name != "Fly")
                    {
                        return action.name;
                    }
                }
            }
            return null;
        }

        private void ReadBiteKey()
        {
            bool down = BiteOverride != null ? BiteOverride() : Input.GetKey(Plugin.BiteKey.Value);
            bool held = down && !player.IsInputLocked();
            if (held && !biteHeld)
            {
                bitePressed = true;
            }
            biteHeld = held;
        }

        private void WarnIfBiteKeyTaken()
        {
            KeyCode key = Plugin.BiteKey.Value;
            if (key == lastCheckedKey) return;
            lastCheckedKey = key;

            string action = GameBindingFor(key);
            if (action != null)
            {
                Plugin.Log.LogWarning($"bite key {key} is also the game's {action} key");
                CL_UIManager.instance.highscoreHeader.ShowText($"Bloodbug bite key {key} is also the game's {action} key. Change BiteKey in the mod's config.");
            }
        }

        // todo: when a barnacle grabs you, you can sting it a bunch of times way too often with no cooldown
        private void UpdateBite(ref Vector3 vel, float dt)
        {
            chargeCooldown -= dt;
            bool pressed = bitePressed;
            bitePressed = false;

            GameEntity previousPrey = prey;
            prey = null;
            if (IsStunned || State == FlightState.Resting)
            {
                EndCharge(0f);
                return;
            }

            if (biteHeld && charge == Charge.None)
            {
                GameEntity found = FindPrey();
                if (found != null)
                {
                    Feed(found, previousPrey, dt);
                    return;
                }
            }

            switch (charge)
            {
                case Charge.None:
                    bool canAfford = OnPills || Stamina >= Balance.ChargeStamina;
                    if (pressed && State == FlightState.Flying && chargeCooldown <= 0f && canAfford)
                    {
                        charge = Charge.WindingUp;
                        chargeTimer = 0f;
                        PlayBloodbugSound("attack-start", 0.5f);
                    }
                    break;

                case Charge.WindingUp:
                    chargeTimer += dt;
                    if (State != FlightState.Flying)
                    {
                        EndCharge(0f);
                    }
                    else if (chargeTimer >= ChargeWindup)
                    {
                        charge = Charge.Dashing;
                        chargeTimer = ChargeDuration;
                        dashDirection = player.cam.transform.forward;
                        if (!OnPills)
                        {
                            Stamina = Mathf.Max(Stamina - Balance.ChargeStamina, 0f);
                        }
                        PlayBloodbugSound("attack-activate", 0.6f);
                    }
                    break;

                case Charge.Dashing:
                    chargeTimer -= dt;
                    GameEntity target = State == FlightState.Flying ? FindChargeTarget() : null;
                    if (target != null)
                    {
                        Strike(ref vel, target);
                    }
                    else if (State != FlightState.Flying || chargeTimer <= 0f)
                    {
                        vel = Vector3.ClampMagnitude(vel, Balance.CruiseSpeed / PlayerAccess.VelocityToMetres);
                        EndCharge(Balance.ChargeCooldown);
                    }
                    break;
            }
        }

        private void EndCharge(float cooldown)
        {
            if (charge == Charge.None) return;
            charge = Charge.None;
            chargeCooldown = cooldown;
        }

        private void Strike(ref Vector3 vel, GameEntity target)
        {
            // float damage = Balance.ChargeDamage + player.curBuffs.GetBuff("addStrike");
            // Damageable.DamageInfo hit = Damageable.DamageInfo.CreateDamageInfo(damage, player, "bloodbug");
            Damageable.DamageInfo hit = Damageable.DamageInfo.CreateDamageInfo(Balance.ChargeDamage, player, "bloodbug");
            hit.position = player.GetControllerPosition();
            hit.direction = dashDirection;
            target.Damage(hit);
            target.AddForce((dashDirection + Vector3.up * 0.3f) * ChargeKnockback, "denizen");

            vel = -dashDirection * (ReboundSpeed / PlayerAccess.VelocityToMetres);
            crashCooldown = CrashCooldown;
            perchCooldown = PerchCooldown;

            PlayBloodbugSound("hurt-impact", 0.5f);
            CL_CameraControl.Shake(0.08f);
            player.SplatterScreenBlood(1);
            EndCharge(Balance.ChargeCooldown);
        }

        private void Feed(GameEntity target, GameEntity previousPrey, float dt)
        {
            float left = BloodLeft(target);
            if (left <= 0f) return;

            prey = target;
            if (previousPrey != target)
            {
                feedTick = 0f;
                PlayBloodbugSound("attack-start", 0.35f);
                player.SplatterScreenBlood(1);
            }

            float drunk = Mathf.Min(Balance.FeedStaminaRate * dt, left);
            bloodLeft[target.GetInstanceID()] = left - drunk;
            Stamina = Mathf.Min(Stamina + drunk, MaxStamina);
            player.Heal(Balance.FeedHealRate * dt);
            FillHunger(drunk / Balance.BloodPerDenizen);

            feedTick -= dt;
            if (feedTick > 0f) return;
            feedTick = FeedTickSeconds;
            CL_CameraControl.Shake(0.01f);
            if (!target.dead)
            {
                target.Damage(Damageable.DamageInfo.CreateDamageInfo(FeedDamageRate * FeedTickSeconds, player, "bloodbug"));
            }
        }

        // a whole denizen fills the hunger bar
        private void FillHunger(float share)
        {
            if (hunger != null)
            {
                hunger.hungerMeter = Mathf.Min(hunger.hungerMeter + share * hunger.hungerMax, hunger.hungerMax);
            }
        }

        private float HungerSpeed()
        {
            if (hunger == null) return 1f;
            float fed = hunger.hungerMeter / hunger.hungerMax;
            return Mathf.Lerp(Balance.StarvingSpeed, 1f, fed / Balance.HungryBelow);
        }

        // survival's hunger bar
        private PerkModule_HungerMeter FindHunger()
        {
            foreach (Perk perk in player.perks)
            {
                foreach (PerkModule module in perk.modules)
                {
                    if (module is PerkModule_HungerMeter meter) return meter;
                }
            }
            return null;
        }

        private GameEntity FindPrey()
        {
            Transform cam = player.cam.transform;
            Vector3 centre = cam.position + cam.forward * (FeedReach * 0.5f);
            // https://docs.unity3d.com/ScriptReference/Physics.OverlapSphereNonAlloc.html
            int count = Physics.OverlapSphereNonAlloc(centre, FeedReach * 0.5f + 0.2f, overlaps, ~0, QueryTriggerInteraction.Collide);

            GameEntity nearest = null;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (!(EntityOf(overlaps[i]) is Denizen denizen)) continue;
                float distance = (overlaps[i].ClosestPoint(cam.position) - cam.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = denizen;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        private GameEntity FindChargeTarget()
        {
            Vector3 centre = player.GetControllerPosition() + dashDirection * StrikeReach;
            int count = Physics.OverlapSphereNonAlloc(centre, StrikeRadius, overlaps, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                GameEntity entity = EntityOf(overlaps[i]);
                if (entity != null && !entity.dead && entity.damageable) return entity;
            }
            return null;
        }

        private GameEntity EntityOf(Collider collider)
        {
            GameEntity entity = GameEntity.GetGameEntity(collider.gameObject);
            if (entity == null)
            {
                entity = collider.GetComponentInParent<GameEntity>();
            }
            return entity == player ? null : entity;
        }
    }
}
