using System.Collections.Generic;
using UnityEngine;

// Aplica el volumen de "Música" de las opciones solo a las fuentes de audio que reproducen música
// (clips largos), sin tocar los efectos de sonido. El volumen general sigue afectando a todo.
public class MusicController : MonoBehaviour
{
    // Los efectos son clips cortos; la música dura bastante más
    const float MinMusicSeconds = 30f;
    const float ScanInterval = 0.5f;

    static MusicController instance;

    private readonly Dictionary<AudioSource, float> baseVolumes = new Dictionary<AudioSource, float>();
    private float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (instance != null) return;

        GameObject go = new GameObject("MusicController");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MusicController>();
    }

    void OnEnable() => GameSettings.Changed += Apply;
    void OnDisable() => GameSettings.Changed -= Apply;

    void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + ScanInterval;

        // Las fuentes nuevas (cambio de escena, música del jefe...) se detectan aquí
        foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            if (!IsMusic(source) || baseVolumes.ContainsKey(source)) continue;
            baseVolumes[source] = source.volume;
        }
        Apply();
    }

    static bool IsMusic(AudioSource source)
    {
        return source.clip != null && source.clip.length >= MinMusicSeconds;
    }

    void Apply()
    {
        List<AudioSource> destroyed = null;
        float music = GameSettings.MusicVolume;

        foreach (KeyValuePair<AudioSource, float> entry in baseVolumes)
        {
            if (entry.Key == null)
            {
                if (destroyed == null) destroyed = new List<AudioSource>();
                destroyed.Add(entry.Key);
                continue;
            }
            entry.Key.volume = entry.Value * music;
        }

        if (destroyed != null) foreach (AudioSource source in destroyed) baseVolumes.Remove(source);
    }
}
