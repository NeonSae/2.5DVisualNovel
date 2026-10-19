using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Display")]
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;

    private readonly List<Resolution> resolutions = new List<Resolution>();

    private void Awake()
    {
        SetupSlider(masterVolumeSlider, 0f, 1f, GameSettings.MasterVolume,
            value => GameSettings.MasterVolume = value);
        SetupSlider(musicVolumeSlider, 0f, 1f, GameSettings.MusicVolume,
            value => GameSettings.MusicVolume = value);
        SetupSlider(sfxVolumeSlider, 0f, 1f, GameSettings.SfxVolume,
            value => GameSettings.SfxVolume = value);
        SetupSlider(brightnessSlider, -1f, 1f, GameSettings.Brightness,
            value => GameSettings.Brightness = value);

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        SetupResolutions();
        SetupQuality();
    }

    private void OnDisable()
    {
        GameSettings.Save();
    }

    private void SetupSlider(
        Slider slider,
        float min,
        float max,
        float value,
        UnityEngine.Events.UnityAction<float> onChanged)
    {
        if (slider == null)
            return;

        slider.minValue = min;
        slider.maxValue = max;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(onChanged);
    }

    private void SetupResolutions()
    {
        if (resolutionDropdown == null)
            return;

        resolutions.Clear();
        List<string> options = new List<string>();
        int currentIndex = 0;

        foreach (Resolution resolution in Screen.resolutions)
        {
            // Skip duplicates that only differ by refresh rate.
            bool alreadyAdded = resolutions.Exists(r =>
                r.width == resolution.width && r.height == resolution.height);

            if (alreadyAdded)
                continue;

            resolutions.Add(resolution);
            options.Add(resolution.width + " x " + resolution.height);

            if (resolution.width == Screen.width && resolution.height == Screen.height)
                currentIndex = resolutions.Count - 1;
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValueWithoutNotify(currentIndex);
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    private void SetupQuality()
    {
        if (qualityDropdown == null)
            return;

        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        qualityDropdown.SetValueWithoutNotify(QualitySettings.GetQualityLevel());
        qualityDropdown.RefreshShownValue();
        qualityDropdown.onValueChanged.AddListener(level => GameSettings.QualityLevel = level);
    }

    private void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreenMode = isFullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;
    }

    private void SetResolution(int index)
    {
        if (index < 0 || index >= resolutions.Count)
            return;

        Resolution resolution = resolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode);
    }
}
