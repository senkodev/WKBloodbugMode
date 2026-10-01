using HarmonyLib;
using UnityEngine;
using UnityEngine.Audio;

namespace BloodbugMode
{
    [HarmonyPatch(typeof(AudioManager), "GetPooledAudioSource")]
    internal static class PooledSoundPatch
    {
        internal static AudioSource Last;

        private static void Postfix(AudioSource __result)
        {
            Last = __result;
            if (__result != null)
            {
                NearHearing.Pooled(__result);
            }
        }
    }

    [HarmonyPatch(typeof(AudioManager), nameof(AudioManager.PlaySound), typeof(AudioClip), typeof(Vector3), typeof(float),
        typeof(float), typeof(float), typeof(bool), typeof(float), typeof(AudioMixerGroup), typeof(string))]
    internal static class SoundAtPlacePatch
    {
        // https://harmony.pardeike.net/articles/patching-injections.html
        private static void Prefix(Vector3 position, ref float volume, float spatial)
        {
            if (NearHearing.On && spatial > 0f)
            {
                volume *= NearHearing.Fade(position);
            }
        }

        private static void Postfix(Vector3 position, float spatial)
        {
            NearHearing.Played(PooledSoundPatch.Last, position, null, spatial);
        }
    }

    [HarmonyPatch(typeof(AudioManager), nameof(AudioManager.PlaySound), typeof(AudioClip), typeof(Transform), typeof(float),
        typeof(float), typeof(float), typeof(bool), typeof(float), typeof(AudioMixerGroup), typeof(string))]
    internal static class SoundOnThingPatch
    {
        private static void Prefix(Transform from, ref float volume, float spatial)
        {
            if (NearHearing.On && spatial > 0f)
            {
                volume *= NearHearing.Fade(from.position);
            }
        }

        private static void Postfix(Transform from, float spatial)
        {
            NearHearing.Played(PooledSoundPatch.Last, from.position, from, spatial);
        }
    }
}
