using UnityEngine;

namespace BloodbugMode
{
    public partial class BloodbugController
    {
        private const float WaterEscapeSeconds = 0.6f;
        private const float RideDepth = 0.04f;
        private const float HeadUnderMargin = 0.05f;
        private const float SurfacingSpeed = 1.6f;
        private const float SinkingSpeed = 0.6f;
        private const float FloatStiffness = 5f;
        private const float FloatDamping = 8f;
        private const float BobHeight = 0.015f;
        private const float BobRate = 2.2f;
        private const float MaxSoak = 3f;

        private float soakedSeconds;
        private float waterEscape;

        public bool IsAfloat { get; private set; }

        private bool Float(ref Vector3 vel, bool jump, bool dive, float dt)
        {
            IsAfloat = false;
            if (!FindWaterSurface(out float surface)) return false;

            IsAfloat = true;
            player.ResetFallingHeight();
            bool headUnder = player.IsCameraUnderwater();

            if (jump && !headUnder && !IsStunned && Stamina >= MinTakeoffStamina)
            {
                Vector3 forward = Vector3.ProjectOnPlane(player.cam.transform.forward, Vector3.up);
                vel = (Vector3.up + forward * 0.3f).normalized * (PushOffSpeed / PlayerAccess.VelocityToMetres);
                waterEscape = WaterEscapeSeconds;
                IsAfloat = false;
                State = FlightState.Flying;
                return true;
            }

            if (!headUnder)
            {
                Recover(Balance.PerchRegen, dt);
            }
            soakedSeconds = Mathf.Min(soakedSeconds + dt, Balance.FloatSeconds * MaxSoak);

            if (jump || dive) return true;

            float eyeHeight = Mathf.Max(player.cam.transform.position.y - player.transform.position.y, HeadUnderMargin);
            float depth = RideDepth + (eyeHeight + HeadUnderMargin - RideDepth) * (soakedSeconds / Balance.FloatSeconds);
            float bob = Mathf.Sin(Time.time * BobRate) * BobHeight;
            float error = surface - depth + bob - player.transform.position.y;

            float speed = Mathf.Clamp(error * FloatStiffness, -SinkingSpeed, SurfacingSpeed);
            vel.y = Mathf.Lerp(vel.y, speed / PlayerAccess.VelocityToMetres, Mathf.Min(dt * FloatDamping, 1f));
            return true;
        }

        private void Dry(float dt)
        {
            IsAfloat = false;
            if (soakedSeconds > 0f)
            {
                soakedSeconds = Mathf.Max(soakedSeconds - dt * Balance.FloatSeconds / Balance.WetSeconds, 0f);
            }
        }

        private bool FindWaterSurface(out float height)
        {
            height = 0f;
            bool found = false;
            Vector3 position = player.transform.position;

            // swim zones can be stacked on top of eahc other
            for (int i = 0; i < 4; i++)
            {
                if (!UT_SwimZone.IsPositionInAnySwimZone(position, out UT_SwimZone zone)) break;
                Vector3 local = zone.transform.InverseTransformPoint(position);
                local.y = zone.swimBounds.max.y;
                Vector3 top = zone.transform.TransformPoint(local);

                height = top.y;
                found = true;
                position = top + Vector3.up * 0.05f;
            }
            return found;
        }
    }
}
