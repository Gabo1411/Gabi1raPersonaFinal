using UnityEngine;

// Reproduce el sonido de un efecto al aparecer, respetando el volumen de "Efectos de sonido" de las opciones.
[RequireComponent(typeof(AudioSource))]
public class EffectSound : MonoBehaviour
{
    void Start()
    {
        AudioSource source = GetComponent<AudioSource>();
        source.volume *= Sfx.Volume;
        source.Play();
    }
}

// Destruye el objeto pasado un tiempo (efectos de partículas que terminan solos).
public class TimedDestroy : MonoBehaviour
{
    public float seconds = 1f;

    void Start()
    {
        Destroy(gameObject, seconds);
    }
}
