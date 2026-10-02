using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BloodbugMode
{
    // https://docs.bepinex.dev/articles/dev_guide/plugin_tutorial/2_plugin_start.html
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(TrinketAndBindingFramework.Plugin.GUID)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "senkodev.whiteknuckle.bloodbugmode";
        public const string Name = "BloodbugMode";
        public const string Version = "1.0.4";

        internal static ManualLogSource Log;

        public static ConfigEntry<KeyCode> BiteKey;
        public static ConfigEntry<bool> OutlinePrey;
        public static ConfigEntry<bool> BugHands;
        public static ConfigEntry<bool> BloodbugVoice;
        public static ConfigEntry<bool> MotherKinEnabled;
        public static ConfigEntry<bool> AllowClimbing;
        public static ConfigEntry<float> BuzzVolume;
        public static ConfigEntry<bool> CameraBanking;
        public static ConfigEntry<bool> BugView;
        public static ConfigEntry<float> BugViewFisheye;
        public static ConfigEntry<float> BugViewNoRed;
        public static ConfigEntry<float> BugViewBlur;
        public static ConfigEntry<bool> BugHearing;

        private void Awake()
        {
            Log = Logger;
            BindConfig();
            BloodbugContent.RegisterBinding();
            Config.SettingChanged += (sender, args) => BloodbugContent.ApplyBiteKey();

            // SceneManager.sceneLoaded += OnSceneLoaded;

            new Harmony(Guid).PatchAll();
            BloodbugContent.Register();
            Log.LogInfo($"{Name} {Version} loaded");
        }

        // https://docs.bepinex.dev/articles/dev_guide/plugin_tutorial/4_configuration.html
        private void BindConfig()
        {
            BiteKey = Config.Bind("Bite", "BiteKey", KeyCode.V, "Hold near a denizen to feed, press in flight to charge. You must use a key that the game doesn't, you get a warning in game otherwise.");
            OutlinePrey = Config.Bind("Bite", "OutlinePrey", true, "Outline dead bodies that still have blood, gets paler when in reach.");
            BugHands = Config.Bind("Body", "BugHands", true, "Whether to replace the climber's hands with Bloodbug's forelegs.");
            BloodbugVoice = Config.Bind("Body", "BloodbugVoice", true, "Whether to use the Bloodbug's hurt and death sounds instead of the climber's.");
            MotherKinEnabled = Config.Bind("Binding", "MotherKin", true, "Whether the Mother treats you as a relative like she does in Roach Mode, with a Bloodbug ending of your own.");
            AllowClimbing = Config.Bind("Binding", "AllowClimbing", true, "Whether handholds can be grabbed. Turn it off to rely on flying and landing on surfaces only.");

            BuzzVolume = Slider("BuzzVolume", 0.25f, "Volume of the wing buzz.");
            CameraBanking = Config.Bind("Presentation", "CameraBanking", true, "Tilt the view when flying sideways.");
            BugView = Config.Bind("Presentation", "BugView", true, "Fisheye, protanopia and short-sighted effects.");
            BugViewFisheye = Slider("BugViewFisheye", 0.75f, "Fisheye strength. 0 for a flat view.");
            BugViewNoRed = Slider("BugViewNoRed", 1f, "Red blindness (protanopia). 0 for normal colours.");
            BugViewBlur = Slider("BugViewBlur", 0.6f, "How blurry distant things are. 0 for a sharp view.");
            BugHearing = Config.Bind("Presentation", "BugHearing", true, "Muffles the sound and the distance you can hear sounds around you.");
        }

        private ConfigEntry<float> Slider(string key, float value, string description)
        {
            return Config.Bind("Presentation", key, value, new ConfigDescription(description, new AcceptableValueRange<float>(0f, 1f)));
        }

        // private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        // {
        //     Log.LogDebug($"scene {scene.name} loaded");
        //     Leaderboards.OnSceneLoaded();
        //     MotherKin.OnSceneLoaded();
        //     BodyOutline.Forget();
        //     NearHearing.Forget();
        //     BloodbugContent.Register();
        // }
    }
}
