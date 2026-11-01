using System;
using UnityEngine;

// Ajustes del jugador guardados en PlayerPrefs (volumen general y sensibilidad del mouse).
public static class GameSettings
{
    const string VolumeKey = "opt_volume";
    const string SfxVolumeKey = "opt_sfx_volume";
    const string SensitivityKey = "opt_sensitivity";

    public const float MinSensitivity = 0.2f;
    public const float MaxSensitivity = 3f;

    public static event Action Changed;

    public static float Volume
    {
        get => PlayerPrefs.GetFloat(VolumeKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
            Apply();
            Changed?.Invoke();
        }
    }

    // Volumen solo de los efectos de sonido (disparos, pociones, dash, jefe...). El general afecta a todo.
    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
            Changed?.Invoke();
        }
    }

    public static float Sensitivity
    {
        get => Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, 1f), MinSensitivity, MaxSensitivity);
        set
        {
            PlayerPrefs.SetFloat(SensitivityKey, Mathf.Clamp(value, MinSensitivity, MaxSensitivity));
            Changed?.Invoke();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
        AudioListener.volume = Volume;
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }
}
