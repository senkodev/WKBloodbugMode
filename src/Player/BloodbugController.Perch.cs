using UnityEngine;

namespace BloodbugMode
{
    public partial class BloodbugController
    {
        private const float PerchIntent = 0.3f;
        private const float PerchReach = 0.5f;
        private const float PerchSkin = 0.05f;
        private const float PerchSettleSpeed = 1f;
        private const float PerchCooldown = 0.4f;
        private const float KnockOffSpeed = 5f;
        private const float CrawlSpeed = 0.75f;
        private const float PushOffSpeed = 4f;

        private float perchCooldown;
        private Vector3 perchNormal;
        private Transform perchSurface;

        public Vector3 PerchNormal => perchNormal;

        private void TryPerch(ref Vector3 vel, ControllerColliderHit hit)
        {
            if (perchCooldown > 0f || IsStunned || !CanPerchOn(hit.collider)) return;

            bool locked = player.IsInputLocked();
            Vector2 move = locked ? Vector2.zero : InputManager.GetVector("Move").vector;
            bool jumpHeld = !locked && InputManager.GetButton("Jump").Pressed;
            bool crouchHeld = !locked && InputManager.GetButton("Crouch").Pressed;

            Transform cam = player.cam.transform;
            Vector3 steering = cam.forward * move.y + cam.right * move.x;
            if (jumpHeld)
            {
                steering += player.GetPlayerUpVector();
            }
            bool steeringIntoIt = Vector3.Dot(Vector3.ClampMagnitude(steering, 1f), -hit.normal) > PerchIntent;
            if (!steeringIntoIt && !crouchHeld) return;

            State = FlightState.Perched;
            perchNormal = hit.normal;
            perchSurface = hit.collider.transform;
            vel = Vector3.zero;
            velocityBeforeMove = Vector3.zero;
            EndCharge(0f);
            CL_CameraControl.Shake(0.01f);
            PlayLandingSound();
        }

        private void Perch(ref Vector3 vel, Vector2 move, bool jump, float dt)
        {
            player.fly = true;
            player.SetFrameSpeedMult(0f);
            player.ResetFallingHeight();

            bool knockedOff = vel.magnitude * PlayerAccess.VelocityToMetres > KnockOffSpeed;
            if (knockedOff || !FindPerchSurface(out float gap))
            {
                LeavePerch();
                return;
            }
            if (jump)
            {
                Vector3 direction = (perchNormal + player.GetPlayerUpVector() * 0.35f).normalized;
                vel = direction * (PushOffSpeed / PlayerAccess.VelocityToMetres);
                LeavePerch();
                return;
            }

            if (perchSurface.CompareTag("Platform") || perchSurface.CompareTag("World"))
            {
                player.ForceSoftParent(perchSurface, preventVelocityTransfer: true);
            }

            Transform cam = player.cam.transform;
            // https://docs.unity3d.com/ScriptReference/Vector3.ProjectOnPlane.html
            Vector3 along = Vector3.ProjectOnPlane(cam.forward * move.y + cam.right * move.x, perchNormal);
            along = Vector3.ClampMagnitude(along, 1f);
            bool crawling = along.sqrMagnitude > 0.01f;

            vel = crawling ? along * (CrawlSpeed / PlayerAccess.VelocityToMetres) : Vector3.zero;
            if (Mathf.Abs(gap) > PerchSkin)
            {
                vel -= perchNormal * (Mathf.Sign(gap) * PerchSettleSpeed / PlayerAccess.VelocityToMetres);
            }
            if (!crawling)
            {
                Recover(Balance.PerchRegen, dt);
            }
        }

        private void LeavePerch()
        {
            perchCooldown = PerchCooldown;
            State = AirborneState();
        }

        private bool FindPerchSurface(out float gap)
        {
            gap = 0f;
            if (perchSurface == null) return false;

            CharacterController capsule = player.cCon;
            Vector3 scale = player.transform.lossyScale;
            float radius = capsule.radius * Mathf.Max(scale.x, scale.z);
            float halfHeight = Mathf.Max(capsule.height * 0.5f * scale.y, radius);
            float extent = Mathf.Lerp(radius, halfHeight, Mathf.Abs(Vector3.Dot(perchNormal, player.GetPlayerUpVector())));

            // https://docs.unity3d.com/ScriptReference/Physics.Raycast.html
            bool found = Physics.Raycast(player.GetControllerPosition(), -perchNormal, out RaycastHit hit,
                extent + PerchReach, surfaceMask, QueryTriggerInteraction.Ignore);
            if (!found || !CanPerchOn(hit.collider)) return false;

            gap = hit.distance - extent;
            perchNormal = Vector3.Slerp(perchNormal, hit.normal, 0.5f).normalized;
            perchSurface = hit.collider.transform;
            return true;
        }

        private static bool CanPerchOn(Collider collider)
        {
            if (collider.isTrigger) return false;
            Rigidbody body = collider.attachedRigidbody;
            return body == null || body.isKinematic;
        }

        private const float ClingGrip = 12f;

        public bool IsClinging { get; private set; }

        private bool IsHoldingLever()
        {
            foreach (ENT_Player.Hand hand in player.hands)
            {
                if (hand.interactState == ENT_Player.InteractType.hold
                    && hand.holdTarget != null
                    && !ENT_Player.IsNullOrDestroyed(hand.currentInteract))
                {
                    return true;
                }
            }
            return false;
        }

        private void Cling(ref Vector3 vel, float dt)
        {
            player.fly = true;
            player.SetFrameSpeedMult(0f);
            player.ResetFallingHeight();
            vel = Vector3.Lerp(vel, Vector3.zero, Mathf.Min(dt * ClingGrip, 1f));

            Recover(Balance.PerchRegen, dt);
            State = AirborneState();
            EndCharge(0f);
        }
    }
}
