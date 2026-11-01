using UnityEngine;

// Hace parpadear una luz de forma irregular (efecto eléctrico) alrededor de su intensidad original.
[RequireComponent(typeof(Light))]
public class LightPulse : MonoBehaviour
{
    [Range(0f, 1f)]
    public float variacion = 0.5f;
    public float velocidad = 9f;

    private Light luz;
    private float intensidadBase;
    private float semilla;

    void Awake()
    {
        luz = GetComponent<Light>();
        intensidadBase = luz.intensity;
        semilla = Random.value * 100f;
    }

    void Update()
    {
        float ruido = Mathf.PerlinNoise(semilla, Time.time * velocidad);
        luz.intensity = intensidadBase * (1f - variacion + variacion * 2f * ruido);
    }
}
