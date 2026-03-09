using UnityEngine;
using System.Collections;

public class PowerUp : MonoBehaviour
{
    public enum TipoPocion { Vida, Danio, Velocidad }
    public TipoPocion tipo;

    [Header("Ajustes")]
    public float valorEfecto = 20f; // Para Vida, es la cantidad de salud a recuperar. Para Daño y Velocidad, es el aumento temporal.
    public float duracionEfecto = 5f; // Solo se usará para Daño y Velocidad

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AplicarEfecto(other.gameObject);

            // Si es vida, destruimos al instante. Si es temporal, ocultamos y esperamos.
            if (tipo == TipoPocion.Vida)
            {
                Destroy(gameObject);
            }
            else
            {
                StartCoroutine(DesactivarVisualmente());
            }
        }
    }

    void AplicarEfecto(GameObject player)
    {
        PlayerMovement moveScript = player.GetComponent<PlayerMovement>();

        switch (tipo)
        {
            case TipoPocion.Vida:
                // Curación instantánea
                moveScript.Health = Mathf.Min(moveScript.Health + (int)valorEfecto, moveScript.maxHealth);
                Debug.Log("¡Vida recuperada al instante!");
                break;

            case TipoPocion.Velocidad:
                StartCoroutine(EfectoVelocidad(moveScript));
                break;

            case TipoPocion.Danio:
                StartCoroutine(EfectoDanio(player));
                break;
        }
    }

    IEnumerator EfectoVelocidad(PlayerMovement move)
    {
        float original = move.speed;
        move.speed += valorEfecto;
        yield return new WaitForSeconds(duracionEfecto);
        move.speed = original;
        Destroy(gameObject);
    }

    IEnumerator EfectoDanio(GameObject player)
    {
        PlayerMovement p = player.GetComponent<PlayerMovement>();
        p.bonusDanio = (int)valorEfecto;
        Debug.Log("¡Ataque potenciado!");
        yield return new WaitForSeconds(duracionEfecto);
        p.bonusDanio = 0;
        Destroy(gameObject);
    }

    IEnumerator DesactivarVisualmente()
    {
        GetComponent<Collider>().enabled = false;
        foreach (Transform child in transform) child.gameObject.SetActive(false);
        yield return null;
    }
}