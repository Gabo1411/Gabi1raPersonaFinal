using UnityEngine;

// Base de todas las pociones: detecta al jugador, reproduce el sonido de recogida y retira la poción.
// Cada poción concreta tiene su propio script y define su efecto en Aplicar().
public abstract class PocionBase : MonoBehaviour
{
    [Header("Sonido")]
    public AudioSource fuenteRecogida;

    // Evita que suene o se aplique dos veces si el jugador la roza varias veces
    private bool yaRecogido = false;

    private void OnTriggerEnter(Collider other)
    {
        if (yaRecogido || !other.CompareTag("Player")) return;

        PlayerMovement jugador = other.GetComponent<PlayerMovement>();
        if (jugador == null) return;

        // Si la poción no se puede aplicar (efecto mejorado aún activo) se queda en el suelo
        if (!Aplicar(jugador)) return;

        yaRecogido = true;

        Sfx.Play(fuenteRecogida);
        OcultarModelo();

        // Se destruye tras 2 segundos para que el sonido termine
        Destroy(gameObject, 2f);
    }

    // Aplica el efecto al jugador. Devuelve false si no se pudo aplicar y la poción debe quedarse.
    protected abstract bool Aplicar(PlayerMovement jugador);

    private void OcultarModelo()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        foreach (Transform child in transform) child.gameObject.SetActive(false);
    }
}
