using UnityEngine;

// Hace girar y flotar suavemente un objeto (modelo de las pociones que no usan animación propia).
public class PotionFloat : MonoBehaviour
{
    public float velocidadGiro = 70f;
    public float amplitud = 0.08f;
    public float frecuencia = 1.6f;

    private Vector3 posicionBase;

    void Start()
    {
        posicionBase = transform.localPosition;
    }

    void Update()
    {
        transform.Rotate(0f, velocidadGiro * Time.deltaTime, 0f, Space.World);
        transform.localPosition = posicionBase + Vector3.up * (Mathf.Sin(Time.time * frecuencia) * amplitud);
    }
}
