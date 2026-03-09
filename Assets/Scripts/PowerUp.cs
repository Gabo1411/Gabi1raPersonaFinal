using UnityEngine;
using System.Collections;

public class PowerUp : MonoBehaviour
{
    public enum TipoPocion { Vida, Danio, Velocidad }
    public TipoPocion tipo;

    [Header("Referencia de Sonido")]
    public AudioSource fuenteRecogida; // Arrastra el AudioSource de la poción aquí

    [Header("Ajustes")]
    public float valorEfecto = 20f;
    public float duracionEfecto = 5f;

    // Bandera de seguridad para que no suene dos veces si el jugador la roza raro
    private bool yaRecogido = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !yaRecogido)
        {
            yaRecogido = true;

            // 1. REPRODUCIR SONIDO
            if (fuenteRecogida != null)
            {
                fuenteRecogida.Play();
            }

            // 2. APLICAR EL EFECTO
            AplicarEfecto(other.gameObject);

            // 3. OCULTAR LA POCIÓN AL INSTANTE
            // Se apaga el modelo y el collider para que parezca que desapareció
            DesactivarObjeto();

            // 4. DESTRUCCIÓN RETRASADA
            if (tipo == TipoPocion.Vida)
            {
                // Espera 2 segundos para que el sonido termine antes de destruir el objeto
                Destroy(gameObject, 2f);
            }
            // (Si es de daño o velocidad, las corrutinas de abajo ya se encargan de destruirlo al final)
        }
    }

    void AplicarEfecto(GameObject player)
    {
        PlayerMovement moveScript = player.GetComponent<PlayerMovement>();

        switch (tipo)
        {
            case TipoPocion.Vida:
                moveScript.Health = Mathf.Min(moveScript.Health + (int)valorEfecto, moveScript.maxHealth);
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
        yield return new WaitForSeconds(duracionEfecto);
        p.bonusDanio = 0;
        Destroy(gameObject);
    }

    void DesactivarObjeto()
    {
        // Apaga la colisión
        GetComponent<Collider>().enabled = false;

        // Apaga todos los modelos 3D hijos (la botella en sí)
        foreach (Transform child in transform) child.gameObject.SetActive(false);
    }
}