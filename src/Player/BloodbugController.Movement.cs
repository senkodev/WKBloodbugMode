using UnityEngine;

namespace BloodbugMode
{
    public partial class BloodbugController
    {
        private const float AutoTakeoffDelay = 0.2f;
        private const float JumpTakeoffDelay = 0.04f;
        private const float HoverBrake = 2.5f;
        private const float AirRegen = 3f;
        private const float InjectorSpeedBonus = 0.5f;

        private bool jumpPressed;
        private float airborneTime;
        private float thrust;
        private bool touchedFloor;
        private Vector3 velocityBeforeMove;
        private Vector3 wobbleSeed;

        internal void BeforeMovement()
        {
            if (!active || player.noclip) return;
            float dt = Time.fixedDeltaTime;
            ref Vector3 vel = ref PlayerAccess.Vel(player);

            stunTimer -= dt;
            crashCooldown -= dt;
            perchCooldown -= dt;
            waterEscape -= dt;
            thrust = 0f;
            touchedFloor = false;

            OnPills = BloodbugItems.OnPills(player);
            if (OnPills)
            {
                Stamina = MaxStamina;
            }

            // https://docs.unity3d.com/ScriptReference/CharacterController-isGrounded.html
            bool grounded = player.cCon.isGrounded;
            bool swimming = player.IsSwimming();
            // right after taking off from water the player is still inside the swim zone
            bool inWater = swimming && waterEscape <= 0f;
            if (player.IsHanging() || inWater)
            {
                State = FlightState.Resting;
            }
            else if (State == FlightState.Resting)
            {
                State = grounded ? FlightState.Grounded : AirborneState();
            }
            if (!swimming)
            {
                Dry(dt);
            }

            bool locked = player.IsInputLocked();
            Vector2 move = locked ? Vector2.zero : InputManager.GetVector("Move").vector;
            bool jumpHeld = !locked && InputManager.GetButton("Jump").Pressed;
            bool crouchHeld = !locked && InputManager.GetButton("Crouch").Pressed;
            bool sprintHeld = !locked && InputManager.GetButton("Sprint").Pressed;
            bool jump = jumpPressed && !locked;
            jumpPressed = false;

            UpdateBite(ref vel, dt);

            IsClinging = (State == FlightState.Flying || State == FlightState.Falling) && !IsStunned && IsHoldingLever();
            if (IsClinging)
            {
                Cling(ref vel, dt);
                velocityBeforeMove = vel;
                return;
            }

            switch (State)
            {
                case FlightState.Grounded:
                    player.fly = false;
                    player.SetFrameSpeedMult(Balance.WalkSpeed * Balance.WalkSpeed);
                    Recover(Balance.GroundRegen, dt);
                    airborneTime = grounded ? 0f : airborneTime + dt;
                    if (airborneTime > AutoTakeoffDelay || (jumpHeld && airborneTime > JumpTakeoffDelay))
                    {
                        airborneTime = 0f;
                        State = AirborneState();
                    }
                    break;

                case FlightState.Flying:
                    if (IsStunned)
                    {
                        player.fly = false;
                        player.SetFrameSpeedMult(0f);
                        break;
                    }
                    Fly(ref vel, move, jumpHeld, crouchHeld, sprintHeld, dt);
                    if (Stamina <= 0f)
                    {
                        State = FlightState.Falling;
                        player.fly = false;
                    }
                    break;

                case FlightState.Falling:
                    player.fly = false;
                    Recover(AirRegen, dt);
                    if (grounded)
                    {
                        State = FlightState.Grounded;
                    }
                    else if (Stamina >= RecoverStamina && (jumpHeld || jump))
                    {
                        State = FlightState.Flying;
                    }
                    break;

                case FlightState.Perched:
                    Perch(ref vel, move, jump, dt);
                    break;

                case FlightState.Resting:
                    player.fly = false;
                    if (inWater && Float(ref vel, jumpHeld || jump, crouchHeld, dt)) break;
                    IsAfloat = false;
                    Recover(Balance.PerchRegen, dt);
                    break;
            }

            velocityBeforeMove = vel;
        }

        internal void AfterMovement()
        {
            if (!active) return;

            bool airborne = State == FlightState.Flying || State == FlightState.Falling;
            if (airborne && touchedFloor && player.cCon.enabled && player.cCon.isGrounded)
            {
                if (-velocityBeforeMove.y * PlayerAccess.VelocityToMetres < player.softLandCutoff)
                {
                    PlayLandingSound();
                }
                State = FlightState.Grounded;
                airborneTime = 0f;
                player.fly = false;
            }

            UpdateCameraBank(Time.fixedDeltaTime);
            ApplyCrashDamage();
        }

        // https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnControllerColliderHit.html
        internal void OnSurfaceHit(ControllerColliderHit hit)
        {
            if (!active || (State != FlightState.Flying && State != FlightState.Falling)) return;
            if (EntityOf(hit.collider) is Denizen) return;

            ref Vector3 vel = ref PlayerAccess.Vel(player);
            Vector3 normal = hit.normal;
            Vector3 incoming = Vector3.Dot(vel, normal) <= Vector3.Dot(velocityBeforeMove, normal) ? vel : velocityBeforeMove;
            float impact = Mathf.Max(-Vector3.Dot(incoming, normal), 0f) * PlayerAccess.VelocityToMetres;
            bool floor = Vector3.Angle(player.GetPlayerUpVector(), normal) <= player.slopeLimit;
            touchedFloor |= floor;

            if (impact >= Balance.CrashSpeed)
            {
                bool fellOntoFloor = floor && (State == FlightState.Falling || IsStunned);
                if (crashCooldown <= 0f && !fellOntoFloor)
                {
                    Crash(ref vel, incoming, normal, impact);
                }
                return;
            }

            if (!floor)
            {
                TryPerch(ref vel, hit);
            }
        }

        private void Fly(ref Vector3 vel, Vector2 move, bool jumpHeld, bool crouchHeld, bool sprintHeld, float dt)
        {
            player.fly = true;
            player.ResetFallingHeight();

            if (charge == Charge.Dashing)
            {
                thrust = 1f;
                player.SetFrameSpeedMult(0f);
                vel = dashDirection * (ChargeSpeed / PlayerAccess.VelocityToMetres);
                return;
            }

            bool windingUp = charge == Charge.WindingUp;
            thrust = windingUp ? 0f : Mathf.Clamp01(move.magnitude + (jumpHeld ? 1f : 0f) + (crouchHeld ? 1f : 0f));
            bool bursting = sprintHeld && thrust > 0f;

            float topSpeed = bursting ? Balance.BurstSpeed : Balance.CruiseSpeed;
            topSpeed *= 1f + InjectorSpeedBonus * BloodbugItems.InjectorStrength(player);
            topSpeed *= HungerSpeed();
            player.SetFrameSpeedMult(windingUp ? 0f : SpeedMultFor(topSpeed));

            float brake = 1f / Balance.Responsiveness;
            if (thrust <= 0f)
            {
                brake *= HoverBrake;
            }
            vel -= vel * Mathf.Min(brake * dt, 1f);

            // https://docs.unity3d.com/ScriptReference/Mathf.PerlinNoise.html
            float t = Time.time * 1.3f;
            var drift = new Vector3(
                Mathf.PerlinNoise(t + wobbleSeed.x, 0f) - 0.5f,
                Mathf.PerlinNoise(t + wobbleSeed.y, 0f) - 0.5f,
                Mathf.PerlinNoise(t + wobbleSeed.z, 0f) - 0.5f);
            vel += drift * (2f * Balance.Wobble / PlayerAccess.VelocityToMetres) * dt;

            if (!OnPills)
            {
                float drain = bursting ? Balance.BurstDrain : Mathf.Lerp(Balance.HoverDrain, Balance.CruiseDrain, thrust);
                if (HasWetWings)
                {
                    drain *= Balance.WetDrainMultiplier;
                }
                if (moonRocks != null)
                {
                    drain *= Balance.MoonRocksDrain;
                }
                Stamina = Mathf.Max(Stamina - drain * dt, 0f);
            }
        }

        private float SpeedMultFor(float metresPerSecond)
        {
            float v = metresPerSecond / PlayerAccess.VelocityToMetres;
            float drag = player.dragCoefficient * (1f + player.curBuffs.GetBuff("addDrag"));
            float needed = v / Balance.Responsiveness + drag * v * v;

            float speedBuffs = Mathf.Max(1f + player.curBuffs.GetBuff("addSpeed"), 0.1f);
            float accelPerUnit = player.speed * player.airControl * PlayerAccess.InputAcceleration * speedBuffs;
            return needed / accelPerUnit;
        }

        private const float CrashCooldown = 0.6f;
        private const float CrashStun = 0.4f;
        private const float CrashBounce = 0.4f;
        private const float CrashStaminaLoss = 15f;

        private float stunTimer;
        private float crashCooldown;
        private float pendingCrashDamage;

        public bool IsStunned => stunTimer > 0f;

        private void Crash(ref Vector3 vel, Vector3 incoming, Vector3 normal, float impact)
        {
            float over = impact - Balance.CrashSpeed;
            float damage = Balance.CrashBaseDamage + over * Balance.CrashDamagePerSpeed;

            crashCooldown = CrashCooldown;
            stunTimer = CrashStun;
            // https://docs.unity3d.com/ScriptReference/Vector3.Reflect.html
            vel = Vector3.Reflect(incoming, normal) * CrashBounce;
            Stamina = Mathf.Max(Stamina - CrashStaminaLoss, 0f);
            pendingCrashDamage += damage;
            EndCharge(Balance.ChargeCooldown);

            if (impact >= Balance.InjurySpeed)
            {
                InjureWing();
            }

            meter.Punch();
            PlayBloodbugSound("hurt-impact", Mathf.Clamp(0.45f + over * 0.05f, 0.45f, 1f));
            CL_CameraControl.Shake(Mathf.Clamp(0.04f + over * 0.01f, 0.04f, 0.15f));
            if (damage >= 1f)
            {
                player.SplatterScreenBlood(2);
            }
            Plugin.Log.LogDebug($"crashed at {impact:0.0} m/s");
        }

        private void ApplyCrashDamage()
        {
            if (pendingCrashDamage <= 0f) return;
            float damage = pendingCrashDamage;
            pendingCrashDamage = 0f;
            player.Damage(Damageable.DamageInfo.CreateDamageInfo(damage, "falling"));
        }
    }
}
