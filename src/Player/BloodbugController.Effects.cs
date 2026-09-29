using BepInEx.Configuration;
using UnityEngine;

namespace BloodbugMode
{
    public partial class BloodbugController
    {
        private const float SputterBelow = 0.2f;
        private const float BankSpeed = 6f;
        private const float BankAngle = 6f;
        private const float HealedVolume = 0.6f;

        private readonly HandCosmeticSwap bugHands = new HandCosmeticSwap();

        private FlightMeter meter;
        private AudioSource buzz;
        private BodyOutline outline;
        private FlyCamera eye;
        private bool hasVoice;
        private bool outlineFailed;
        private bool eyeFailed;
        private float bank;
        private float climberNearClip;

        internal float WidenView(float fieldOfView)
        {
            return active && eye != null ? eye.Widen(fieldOfView) : fieldOfView;
        }

        private void CreateEffects()
        {
            if (meter == null)
            {
                meter = FlightMeter.Create(CL_UIManager.instance.uiMeterLayoutRoot);
            }
            meter.SetVisible(true);

            // https://docs.unity3d.com/ScriptReference/Camera-nearClipPlane.html
            climberNearClip = player.cam.nearClipPlane;
            player.cam.nearClipPlane = climberNearClip * Balance.NearClip;

            if (buzz == null && GameAssets.BuzzClip != null)
            {
                buzz = gameObject.AddComponent<AudioSource>();
                buzz.clip = GameAssets.BuzzClip;
                buzz.loop = true;
                buzz.playOnAwake = false;
                buzz.spatialBlend = 0f;
                buzz.volume = 0f;
                buzz.outputAudioMixerGroup = AudioManager.instance.gameMixer;
                buzz.bypassListenerEffects = true;
            }
        }

        private void ApplySettings()
        {
            SetVoice(Plugin.BloodbugVoice.Value);
            SetOutline(Plugin.OutlinePrey.Value);
            SetEye(Plugin.BugView.Value);
            NearHearing.Turn(Plugin.BugHearing.Value);
            bugHands.Apply(player, Plugin.BugHands.Value);
            WarnIfBiteKeyTaken();
        }

        private void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            ApplySettings();
        }

        private void SetVoice(bool wanted)
        {
            wanted &= GameAssets.Voice != null;
            if (wanted == hasVoice) return;
            hasVoice = wanted;

            if (!wanted)
            {
                player.clipHandler.RemoveOverride(GameAssets.Voice);
                return;
            }
            player.clipHandler.overrides.Insert(0, GameAssets.Voice);
        }

        private void SetOutline(bool wanted)
        {
            if (!wanted)
            {
                outline?.Destroy();
                outline = null;
            }
            else if (outline == null && !outlineFailed)
            {
                outline = BodyOutline.Create(player.cam, this);
                outlineFailed = outline == null;
            }
            outline?.Show(wanted);
        }

        private void SetEye(bool wanted)
        {
            if (eye == null && wanted && !eyeFailed)
            {
                eye = FlyCamera.Create(player);
                eyeFailed = eye == null;
            }
            eye?.Show(wanted);
        }

        private void UpdateEffects()
        {
            float needed = State == FlightState.Falling ? RecoverStamina : MinTakeoffStamina;
            meter.Set(Stamina / Balance.MaxStamina, State, Stamina < needed, OnPills, IsFeeding);
            if (buzz != null)
            {
                UpdateBuzz();
            }
        }

        private void UpdateBuzz()
        {
            bool flying = State == FlightState.Flying && !IsStunned && !IsClinging;
            float volume = flying ? Mathf.Lerp(0.45f, 1f, thrust) * Plugin.BuzzVolume.Value : 0f;

            bool sputtering = flying && !OnPills && Stamina < MaxStamina * SputterBelow;
            if (sputtering && Mathf.PerlinNoise(Time.time * 9f, 0f) < 0.45f)
            {
                volume *= 0.3f;
            }
            buzz.volume = Mathf.Lerp(buzz.volume, volume, Time.deltaTime * (sputtering ? 20f : 6f));

            float pitch = charge == Charge.WindingUp ? 1.45f : 1f + 0.25f * thrust;
            buzz.pitch = Mathf.Lerp(buzz.pitch, flying ? pitch : 0.8f, Time.deltaTime * 4f);

            if (buzz.volume > 0.01f && !buzz.isPlaying)
            {
                buzz.Play();
            }
            else if (buzz.volume <= 0.01f && buzz.isPlaying && !flying)
            {
                buzz.Stop();
            }
        }

        private void HideEffects()
        {
            if (buzz != null)
            {
                buzz.Stop();
            }
            meter.SetVisible(false);
            outline?.Show(false);
            eye?.Show(false);
            NearHearing.Turn(false);
            player.cam.nearClipPlane = climberNearClip;

            bugHands.Restore(player);
            if (hasVoice)
            {
                player.clipHandler.RemoveOverride(GameAssets.Voice);
            }
            hasVoice = false;
        }

        private void DestroyEffects()
        {
            meter?.Destroy();
            outline?.Destroy();
            eye?.Destroy();
        }

        private void UpdateCameraBank(float dt)
        {
            float target = 0f;
            if (Plugin.CameraBanking.Value && State == FlightState.Flying && !IsStunned)
            {
                Vector3 forward = Vector3.ProjectOnPlane(player.cam.transform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                float sideways = Vector3.Dot(player.GetVelocity() * PlayerAccess.VelocityToMetres, right);
                target = Mathf.Clamp(-sideways / Balance.CruiseSpeed * BankAngle, -BankAngle, BankAngle);
            }

            bank = Mathf.Lerp(bank, target, dt * BankSpeed);
            if (Mathf.Abs(bank) > 0.01f)
            {
                PlayerAccess.CamSway(player) = bank;
            }
        }

        private void PlayLandingSound()
        {
            player.clipHandler.GetGroup("movement").GetSet("land-soft").Play(0.8f);
        }

        private void PlayHealedSound()
        {
            AudioClip clip = GameAssets.HealedClip;
            if (clip != null)
            {
                AudioManager.PlaySound(clip, player.transform.position, HealedVolume);
            }
        }

        private void PlayBloodbugSound(string set, float volume)
        {
            AudioClip clip = GameAssets.Sound(set);
            if (clip != null)
            {
                AudioManager.PlaySound(clip, player.transform, volume, Random.Range(0.95f, 1.05f), 0f);
            }
        }
    }
}
