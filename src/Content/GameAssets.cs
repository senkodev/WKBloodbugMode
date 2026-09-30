using System.Collections.Generic;
using UnityEngine;

namespace BloodbugMode
{
    internal static class GameAssets
    {
        public const string HalfInventoryId = "Perk_Binding_HalfInventory";
        public const string SurvivalId = "Perk_Binding_Survival";
        public const string MoonRocksId = "Perk_Trinket_MoonRock";
        // public const string CarnalBloodlustId = "Perk_CarnalBloodlust";

        private const string BloodbugPrefabName = "Denizen_Bloodbug";
        private const string HealedClipName = "player_injury_healed";

        private static readonly string[] HurtSets = { "hurt", "hurt-falling", "hurt-heat" };
        private static readonly string[] DeathSets = { "death" };

        private static readonly Dictionary<string, List<AudioClip>> sounds = new Dictionary<string, List<AudioClip>>();
        private static AudioClip healedClip;
        private static int healedClipSearches;
        private static bool loaded;

        public static GameObject BloodbugPrefab { get; private set; }
        public static AudioClip BuzzClip { get; private set; }
        public static Perk HalfInventory { get; private set; }
        public static Perk Survival { get; private set; }
        // public static Perk CarnalBloodlust { get; private set; }

        public static AudioClipHandlerOverride Voice { get; private set; }

        public static AudioClip HealedClip
        {
            get
            {
                if (healedClip != null || healedClipSearches >= 3) return healedClip;
                healedClipSearches++;
                foreach (AudioClip clip in Resources.FindObjectsOfTypeAll<AudioClip>())
                {
                    if (clip.name == HealedClipName)
                    {
                        healedClip = clip;
                        return clip;
                    }
                }
                Plugin.Log.LogWarning($"{HealedClipName} isn't loaded");
                return null;
            }
        }

        public static bool Load()
        {
            if (loaded) return false;
            loaded = true;

            WKAssetDatabase game = CL_AssetManager.GetBaseAssetDatabase();
            BloodbugPrefab = FindBloodbugPrefab(game);
            if (BloodbugPrefab != null)
            {
                AudioSource source = BloodbugPrefab.GetComponent<AudioSource>();
                BuzzClip = source != null ? source.clip : null;
                Voice = CreateVoice(BloodbugPrefab.GetComponent<UT_AudioClipHandler>());
            }
            HalfInventory = game.perkAssets.Find(perk => perk.id == HalfInventoryId);
            Survival = game.perkAssets.Find(perk => perk.id == SurvivalId);
            // CarnalBloodlust = game.perkAssets.Find(perk => perk.id == CarnalBloodlustId);

            Plugin.Log.LogInfo($"game assets: prefab={Name(BloodbugPrefab)}, buzz={Name(BuzzClip)}, "
                + $"voice={(Voice != null ? Voice.setOverrides.Count : 0)} sets, half inventory={Name(HalfInventory)}, survival={Name(Survival)}");
            return true;
        }

        public static AudioClip Sound(string set)
        {
            if (sounds.TryGetValue(set, out List<AudioClip> clips))
            {
                return clips[Random.Range(0, clips.Count)];
            }
            return null;
        }

        private static GameObject FindBloodbugPrefab(WKAssetDatabase game)
        {
            GameObject found = null;
            foreach (GameObject prefab in game.denizenPrefabs)
            {
                if (prefab == null || !prefab.name.StartsWith(BloodbugPrefabName) || prefab.GetComponentInChildren<Animator>(true) == null)
                {
                    continue;
                }
                if (found == null || prefab.name == BloodbugPrefabName)
                {
                    found = prefab;
                }
            }
            return found;
        }

        private static AudioClipHandlerOverride CreateVoice(UT_AudioClipHandler bloodbug)
        {
            if (bloodbug == null) return null;

            foreach (UT_AudioClipHandler.AudioGroup group in bloodbug.groups)
            {
                foreach (UT_AudioClipHandler.AudioSet set in group.audioSets)
                {
                    if (set.clips.Count > 0)
                    {
                        sounds[set.name] = set.clips;
                    }
                }
            }

            sounds.TryGetValue("hurt", out List<AudioClip> hurt);
            if (!sounds.TryGetValue("die", out List<AudioClip> death))
            {
                sounds.TryGetValue("death", out death);
            }

            var voice = ModFiles.Keep(ScriptableObject.CreateInstance<AudioClipHandlerOverride>());
            voice.name = "Bloodbug_Voice";
            voice.setOverrides = new List<AudioClipHandlerOverride.AudioSetOverride>();
            AddOverrides(voice, HurtSets, hurt);
            AddOverrides(voice, DeathSets, death);
            return voice.setOverrides.Count > 0 ? voice : null;
        }

        private static void AddOverrides(AudioClipHandlerOverride voice, string[] playerSets, List<AudioClip> clips)
        {
            if (clips == null) return;
            foreach (string set in playerSets)
            {
                voice.setOverrides.Add(new AudioClipHandlerOverride.AudioSetOverride { groupName = "player", setName = set, clips = clips });
            }
        }

        private static string Name(Object asset)
        {
            return asset != null ? asset.name : "none";
        }
    }
}
