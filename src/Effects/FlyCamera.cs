using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace BloodbugMode
{
    internal class FlyCamera
    {
        private const string FinalCameraName = "FXCam";
        private const int LensLayer = 31;
        private const int BlurLayer = 30;
        private const float Priority = 1000f;

        private const float MaxDistortion = 85f;
        private const float MaxDistortionScale = 1.25f;
        private const float MaxVignette = 0.3f;
        private const float MaxExtraView = 1.3f;
        private const float MaxFieldOfView = 150f;
        private const float FocalLength = 50f;
        private const float MinAperture = 0.7f;
        private const float MaxAperture = 12f;

        private static readonly Vector3 ProtanRed = new Vector3(56.7f, 43.3f, 0f);
        private static readonly Vector3 ProtanGreen = new Vector3(55.8f, 44.2f, 0f);
        private static readonly Vector3 ProtanBlue = new Vector3(0f, 24.2f, 75.8f);

        private readonly PostProcessLayer finalLayer;
        private readonly PostProcessLayer worldLayer;
        private readonly LayerMask finalMask;
        private readonly LayerMask worldMask;
        private readonly bool worldWasEnabled;

        private PostProcessVolume lensVolume;
        private PostProcessVolume blurVolume;
        private LensDistortion distortion;
        private ColorGrading grading;
        private Vignette vignette;
        private DepthOfField depthOfField;
        private bool shown;

        private FlyCamera(PostProcessLayer finalLayer, PostProcessLayer worldLayer)
        {
            this.finalLayer = finalLayer;
            this.worldLayer = worldLayer;
            finalMask = finalLayer.volumeLayer;
            if (worldLayer != null)
            {
                worldMask = worldLayer.volumeLayer;
                worldWasEnabled = worldLayer.enabled;
            }
        }

        public static FlyCamera Create(ENT_Player player)
        {
            Camera finalCamera = FindFinalCamera(player);
            PostProcessLayer finalLayer = finalCamera != null ? finalCamera.GetComponent<PostProcessLayer>() : null;
            if (finalLayer == null || !finalLayer.enabled)
            {
                Plugin.Log.LogWarning("could not apply post-processing on the final camera");
                return null;
            }

            PostProcessLayer worldLayer = null;
            if (player.cam != finalCamera)
            {
                worldLayer = player.cam.GetComponent<PostProcessLayer>();
            }
            if (worldLayer == null)
            {
                Plugin.Log.LogWarning("could not apply post-processing on the final camera");
            }

            var eye = new FlyCamera(finalLayer, worldLayer);
            eye.CreateVolumes();
            Plugin.Log.LogInfo($"bug view: lens on {finalCamera.name}, blur on {(worldLayer != null ? worldLayer.name : "nothing")}");
            return eye;
        }

        public void Show(bool wanted)
        {
            if (lensVolume == null || finalLayer == null) return;
            if (wanted)
            {
                ApplyConfig();
            }
            if (wanted == shown) return;

            shown = wanted;
            lensVolume.enabled = wanted;
            finalLayer.volumeLayer = wanted ? (LayerMask)(finalMask & ~(1 << BlurLayer)) : finalMask;
            if (!wanted)
            {
                SetBlur(false);
            }
        }

        // https://github.com/Unity-Technologies/PostProcessing/blob/v2/PostProcessing/Runtime/Effects/LensDistortion.cs
        // https://docs.unity3d.com/Packages/com.unity.postprocessing@3.4/manual/Lens-Distortion.html
        public float Widen(float fieldOfView)
        {
            float strength = Plugin.BugViewFisheye.Value;
            if (!shown || strength <= 0f) return fieldOfView;

            float theta = Mathf.Deg2Rad * Mathf.Min(160f, 1.6f * Mathf.Max(strength * MaxDistortion, 1f));
            float sigma = 2f * Mathf.Tan(theta * 0.5f);
            float scale = Mathf.Lerp(1f, MaxDistortionScale, strength);
            float edge = 0.5f / scale;
            float kept = Mathf.Tan(edge * theta) / (edge * sigma) / scale;
            kept /= Mathf.Lerp(1f, MaxExtraView, strength);

            float widened = 2f * Mathf.Atan(Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad) / kept) * Mathf.Rad2Deg;
            return Mathf.Min(widened, MaxFieldOfView);
        }

        public void Destroy()
        {
            Show(false);
            if (lensVolume != null)
            {
                RuntimeUtilities.DestroyVolume(lensVolume, true, true);
                lensVolume = null;
            }
            if (blurVolume != null)
            {
                RuntimeUtilities.DestroyVolume(blurVolume, true, true);
                blurVolume = null;
            }
        }

        private static Camera FindFinalCamera(ENT_Player player)
        {
            foreach (Camera camera in player.transform.root.GetComponentsInChildren<Camera>(true))
            {
                if (camera.name == FinalCameraName && camera.targetTexture == null) return camera;
            }

            foreach (Camera camera in Camera.allCameras)
            {
                if (camera.name == FinalCameraName && camera.targetTexture == null) return camera;
            }
            return null;
        }

        // https://docs.unity3d.com/Packages/com.unity.postprocessing@3.4/manual/Manipulating-the-Stack.html
        private void CreateVolumes()
        {
            distortion = ScriptableObject.CreateInstance<LensDistortion>();
            distortion.enabled.Override(true);

            grading = ScriptableObject.CreateInstance<ColorGrading>();
            grading.enabled.Override(true);
            grading.gradingMode.Override(GradingMode.LowDefinitionRange);

            // https://docs.unity3d.com/Packages/com.unity.postprocessing@3.4/manual/Vignette.html
            vignette = ScriptableObject.CreateInstance<Vignette>();
            vignette.enabled.Override(true);
            vignette.mode.Override(VignetteMode.Classic);
            vignette.color.Override(Color.black);
            vignette.smoothness.Override(0.45f);
            vignette.roundness.Override(1f);

            lensVolume = PostProcessManager.instance.QuickVolume(LensLayer, Priority, distortion, grading, vignette);
            lensVolume.name = "Bloodbug lens";
            lensVolume.enabled = false;

            if (worldLayer == null) return;
            depthOfField = ScriptableObject.CreateInstance<DepthOfField>();
            depthOfField.enabled.Override(true);
            depthOfField.focalLength.Override(FocalLength);

            blurVolume = PostProcessManager.instance.QuickVolume(BlurLayer, Priority, depthOfField);
            blurVolume.name = "Bloodbug blur";
            blurVolume.enabled = false;
        }

        private void ApplyConfig()
        {

            float fisheye = Plugin.BugViewFisheye.Value;
            distortion.enabled.Override(fisheye > 0f);
            distortion.intensity.Override(fisheye * MaxDistortion);
            distortion.scale.Override(Mathf.Lerp(1f, MaxDistortionScale, fisheye));
            vignette.enabled.Override(fisheye > 0f);
            vignette.intensity.Override(fisheye * MaxVignette);

            // channel mixer, https://docs.unity3d.com/Packages/com.unity.postprocessing@3.4/manual/Color-Grading.html
            float noRed = Plugin.BugViewNoRed.Value;
            Vector3 red = Vector3.Lerp(new Vector3(100f, 0f, 0f), ProtanRed, noRed);
            Vector3 green = Vector3.Lerp(new Vector3(0f, 100f, 0f), ProtanGreen, noRed);
            Vector3 blue = Vector3.Lerp(new Vector3(0f, 0f, 100f), ProtanBlue, noRed);
            grading.enabled.Override(noRed > 0f);
            grading.mixerRedOutRedIn.Override(red.x);
            grading.mixerRedOutGreenIn.Override(red.y);
            grading.mixerRedOutBlueIn.Override(red.z);
            grading.mixerGreenOutRedIn.Override(green.x);
            grading.mixerGreenOutGreenIn.Override(green.y);
            grading.mixerGreenOutBlueIn.Override(green.z);
            grading.mixerBlueOutRedIn.Override(blue.x);
            grading.mixerBlueOutGreenIn.Override(blue.y);
            grading.mixerBlueOutBlueIn.Override(blue.z);

            // https://docs.unity3d.com/Packages/com.unity.postprocessing@3.4/manual/Depth-of-Field.html
            float blur = Plugin.BugViewBlur.Value;
            SetBlur(blur > 0f);
            if (depthOfField != null && blur > 0f)
            {
                depthOfField.focusDistance.Override(Balance.BugViewSharpTo);
                depthOfField.aperture.Override(Mathf.Lerp(MaxAperture, MinAperture, blur));
                depthOfField.kernelSize.Override(blur > 0.66f ? KernelSize.VeryLarge : blur > 0.33f ? KernelSize.Large : KernelSize.Medium);
            }
        }

        private void SetBlur(bool on)
        {
            if (blurVolume == null || worldLayer == null || blurVolume.enabled == on) return;
            blurVolume.enabled = on;
            worldLayer.volumeLayer = on ? (LayerMask)(1 << BlurLayer) : worldMask;
            worldLayer.enabled = on || worldWasEnabled;
        }
    }
}
