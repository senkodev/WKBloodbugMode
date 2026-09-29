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
            public bool bypass;
        }

        private static readonly List<Saved> changed = new List<Saved>();
        private static readonly HashSet<AudioSource> pooled = new HashSet<AudioSource>();
        private static AudioLowPassFilter muffle;

        public static bool On { get; private set; }

        public static void Turn(bool on)
        {
            if (on == On) return;
            On = on;
            if (on)
            {
                // https://docs.unity3d.com/ScriptReference/AudioLowPassFilter.html
                muffle = Object.FindAnyObjectByType<AudioListener>().gameObject.AddComponent<AudioLowPassFilter>();
                muffle.cutoffFrequency = Balance.HearingCutoff;
                muffle.lowpassResonanceQ = Balance.HearingResonance;
                // https://docs.unity3d.com/ScriptReference/Object.FindObjectsByType.html
                Adjust(Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None));
            }
            else
            {
                Object.Destroy(muffle);
                Restore();
            }
        }

        public static void Pooled(AudioSource source)
        {
            pooled.Add(source);
            if (On)
            {
                source.bypassListenerEffects = true;
            }
        }

        public static float Fade(Vector3 position)
        {
            Vector3 ears = ENT_Player.playerObject.cam.transform.position;
            return 1f - Mathf.InverseLerp(Balance.HearingNear, Balance.HearingFar, Vector3.Distance(position, ears));
        }

        public static void Adjust(IEnumerable<AudioSource> sources)
        {
            Transform player = ENT_Player.playerObject.transform;
            foreach (AudioSource source in sources)
            {
                if (pooled.Contains(source)) continue;

                changed.Add(new Saved
                {
                    source = source,
                    mode = source.rolloffMode,
                    min = source.minDistance,
                    max = source.maxDistance,
                    bypass = source.bypassListenerEffects
                });

                // music, UI and climber sounds are unaffected
                // https://docs.unity3d.com/ScriptReference/AudioSource-bypassListenerEffects.html
                if (source.spatialBlend <= 0f || source.transform.IsChildOf(player))
                {
                    source.bypassListenerEffects = true;
                    continue;
                }
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = Mathf.Min(source.minDistance, Balance.HearingNear);
                source.maxDistance = Mathf.Min(source.maxDistance, Balance.HearingFar);
            }
        }

        public static void Forget()
        {
            changed.Clear();
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
                saved.source.bypassListenerEffects = saved.bypass;
            }
            changed.Clear();
        }
    }
}
