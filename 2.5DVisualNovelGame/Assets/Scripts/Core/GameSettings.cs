using System;
using UnityEngine;

// Saved player settings (volume, brightness, quality).
// Values are stored in PlayerPrefs, so they survive closing the game.
public static class GameSettings
{
    private const string MasterVolumeKey = "Settings_MasterVolume";
    private const string MusicVolumeKey = "Settings_MusicVolume";
    private const string SfxVolumeKey = "Settings_SfxVolume";
    private const string BrightnessKey = "Settings_Brightness";
    private const string QualityKey = "Settings_Quality";

    // Called when music or SFX volume changes, so AudioManager can update.
    public static event Action VolumeChanged;

    // Called when brightness changes, so the Global Volume can update.
    public static event Action<float> BrightnessChanged;

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, value);
            AudioListener.volume = value;
        }
    }

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, value);
            VolumeChanged?.Invoke();
        }
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(SfxVolumeKey, value);
            VolumeChanged?.Invoke();
        }
    }

    // -1 = darker, 0 = normal, 1 = brighter.
    public static float Brightness
    {
        get => PlayerPrefs.GetFloat(BrightnessKey, 0f);
        set
        {
            PlayerPrefs.SetFloat(BrightnessKey, value);
            BrightnessChanged?.Invoke(value);
        }
    }

    public static int QualityLevel
    {
        get => PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
        set
        {
            PlayerPrefs.SetInt(QualityKey, value);
            QualitySettings.SetQualityLevel(value, true);
        }
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    // Runs automatically when the game starts, before any scene loads.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedSettings()
    {
        AudioListener.volume = MasterVolume;

        if (PlayerPrefs.HasKey(QualityKey))
            QualitySettings.SetQualityLevel(PlayerPrefs.GetInt(QualityKey), true);
    }
}
