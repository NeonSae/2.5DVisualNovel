using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Put this on the Global Volume of every scene.
// Applies the Brightness setting through the post-processing exposure.
[RequireComponent(typeof(Volume))]
public class BrightnessFromSettings : MonoBehaviour
{
    [Tooltip("How much exposure the brightness slider can add or remove.")]
    [SerializeField] private float maxExposureChange = 1.5f;

    private ColorAdjustments colorAdjustments;
    private float baseExposure;

    private void Awake()
    {
        // .profile makes a runtime copy, so the profile asset itself is never changed.
        VolumeProfile profile = GetComponent<Volume>().profile;

        if (!profile.TryGet(out colorAdjustments))
            colorAdjustments = profile.Add<ColorAdjustments>(false);

        baseExposure = colorAdjustments.postExposure.overrideState
            ? colorAdjustments.postExposure.value
            : 0f;

        Apply(GameSettings.Brightness);
        GameSettings.BrightnessChanged += Apply;
    }

    private void OnDestroy()
    {
        GameSettings.BrightnessChanged -= Apply;
    }

    private void Apply(float brightness)
    {
        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.postExposure.value = baseExposure + brightness * maxExposureChange;
    }
}
