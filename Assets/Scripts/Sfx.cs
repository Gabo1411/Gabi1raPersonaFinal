using UnityEngine;

// Reproduce efectos de sonido aplicando el volumen de "Efectos de sonido" de las opciones.
public static class Sfx
{
    public static float Volume => GameSettings.SfxVolume;

    public static void Play(AudioSource source, AudioClip clip)
    {
        if (source != null && clip != null) source.PlayOneShot(clip, Volume);
    }

    // Reproduce el clip que ya tiene asignado la fuente
    public static void Play(AudioSource source)
    {
        if (source != null) Play(source, source.clip);
    }

    public static void AtPoint(AudioClip clip, Vector3 position)
    {
        if (clip != null) AudioSource.PlayClipAtPoint(clip, position, Volume);
    }
}
