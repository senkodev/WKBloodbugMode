using HarmonyLib;
using UnityEngine;
using UnityEngine.Audio;

namespace BloodbugMode
{
    [HarmonyPatch(typeof(AudioManager), "GetPooledAudioSource")]
    internal static class PooledSoundPatch
    {
        private static void Postfix(AudioSource __result)
        {
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
        private static void Prefix(Vector3 position, ref float volume, float spatial, ref bool bypass)
        {
            if (!NearHearing.On) return;
            if (spatial > 0f)
            {
                volume *= NearHearing.Fade(position);
            }
            else
            {
                bypass = true;
            }
        }
    }

    [HarmonyPatch(typeof(AudioManager), nameof(AudioManager.PlaySound), typeof(AudioClip), typeof(Transform), typeof(float),
        typeof(float), typeof(float), typeof(bool), typeof(float), typeof(AudioMixerGroup), typeof(string))]
    internal static class SoundOnThingPatch
    {
        private static void Prefix(Transform from, ref float volume, float spatial, ref bool bypass)
        {
            if (!NearHearing.On) return;
            if (spatial > 0f)
            {
                volume *= NearHearing.Fade(from.position);
            }
            else
            {
                bypass = true;
            }
        }
    }
}
