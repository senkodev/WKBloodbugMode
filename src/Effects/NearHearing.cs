using System.Collections.Generic;
using UnityEngine;

namespace BloodbugMode
{
    internal static class NearHearing
    {
        private struct Saved
        {
            public AudioSource source;
            public AudioRolloffMode mode;
            public float min;
            public float max;
            public AudioLowPassFilter muffle;
        }

        private const float OwnSoundRadius = 0.75f;

        private static readonly List<Saved> changed = new List<Saved>();
        private static readonly Dictionary<AudioSource, AudioLowPassFilter> pooled = new Dictionary<AudioSource, AudioLowPassFilter>();

        public static bool On { get; private set; }

        public static void Turn(bool on)
        {
            if (on == On) return;
            On = on;
            if (on)
            {
                // https://docs.unity3d.com/ScriptReference/Object.FindObjectsByType.html
                Adjust(Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None));
            }
            else
            {
                Restore();
            }
        }

        public static void Pooled(AudioSource source)
        {
            if (!pooled.ContainsKey(source))
            {
                pooled.Add(source, Muffle(source, false));
            }
        }

        public static void Played(AudioSource source, Vector3 position, Transform from, float spatial)
        {
            if (source == null || !pooled.TryGetValue(source, out AudioLowPassFilter muffle)) return;
            muffle.enabled = On && spatial > 0f && !IsOwn(position, from);
        }

        public static float Fade(Vector3 position)
        {
            Vector3 ears = ENT_Player.playerObject.cam.transform.position;
            return 1f - Mathf.InverseLerp(Balance.HearingNear, Balance.HearingFar, Vector3.Distance(position, ears));
        }

        public static bool IsOwn(Vector3 position, Transform from)
        {
            Transform player = ENT_Player.playerObject.transform;
            if (from != null)
            {
                return from == player || from.IsChildOf(player);
            }
            return Vector3.Distance(position, player.position) < OwnSoundRadius;
        }

        public static void Adjust(IEnumerable<AudioSource> sources)
        {
            Transform player = ENT_Player.playerObject.transform;
            foreach (AudioSource source in sources)
            {
                if (pooled.ContainsKey(source)) continue;
                if (source.spatialBlend <= 0f || source.transform.IsChildOf(player)) continue;

                changed.Add(new Saved
                {
                    source = source,
                    mode = source.rolloffMode,
                    min = source.minDistance,
                    max = source.maxDistance,
                    muffle = Muffle(source, true)
                });
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = Mathf.Min(source.minDistance, Balance.HearingNear);
                source.maxDistance = Mathf.Min(source.maxDistance, Balance.HearingFar);
            }
        }

        // https://docs.unity3d.com/ScriptReference/AudioLowPassFilter.html
        private static AudioLowPassFilter Muffle(AudioSource source, bool on)
        {
            AudioLowPassFilter muffle = source.GetComponent<AudioLowPassFilter>();
            if (muffle == null)
            {
                muffle = source.gameObject.AddComponent<AudioLowPassFilter>();
            }
            muffle.cutoffFrequency = Balance.HearingCutoff;
            muffle.lowpassResonanceQ = Balance.HearingResonance;
            muffle.enabled = on;
            return muffle;
        }

        public static void Forget()
        {
            changed.Clear();
            pooled.Clear();
            On = false;
        }

        private static void Restore()
        {
            for (int i = changed.Count - 1; i >= 0; i--)
            {
                Saved saved = changed[i];
                if (saved.source == null) continue;
                saved.source.rolloffMode = saved.mode;
                saved.source.minDistance = saved.min;
                saved.source.maxDistance = saved.max;
                if (saved.muffle != null)
                {
                    saved.muffle.enabled = false;
                }
            }
            changed.Clear();
            foreach (AudioLowPassFilter muffle in pooled.Values)
            {
                if (muffle != null)
                {
                    muffle.enabled = false;
                }
            }
        }
    }
}
