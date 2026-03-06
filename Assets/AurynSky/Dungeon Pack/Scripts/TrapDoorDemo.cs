using UnityEngine;

public class TrapDoorDemo : MonoBehaviour
{
    public Animator TrapDoorAnim; // Animator for the trap door;

    [Header("Configuración de Teletransporte")]
    public Transform puntoAparicionJefe; // Arrastra aquí un objeto vacío que esté en la arena del jefe

    private bool estaAbierta = false;

    void Awake()
    {
        // get the Animator component from the trap;
        if (TrapDoorAnim == null)
        {
            TrapDoorAnim = GetComponent<Animator>();
        }
    }

    // Esta función será llamada por el GameManager al llegar a los puntos
    // Esta función es llamada por el GameManager
    public void AbrirEscotilla()
    {
        estaAbierta = true; // Habilita que el jugador pueda teletransportarse

        // ESTA ES LA LÍNEA: Llama a la función "EjecutarAnimacion" después de 2 segundos
        Invoke("EjecutarAnimacion", 2f);
    }

    // Nueva función que realmente activa el Animator
    private void EjecutarAnimacion()
    {
        if (TrapDoorAnim != null)
        {
            TrapDoorAnim.SetTrigger("open");
        }
    }

    // Detectamos si el jugador entra (o cae) en la escotilla
    private void OnTriggerEnter(Collider other)
    {
        // Solo funciona si la escotilla ya se abrió y el que entra es el jugador
        if (estaAbierta && other.CompareTag("Player"))
        {
            if (puntoAparicionJefe == null)
            {
                Debug.LogWarning("¡Falta asignar el punto de aparición del jefe en la escotilla!");
                return;
            }

            // Buscamos el CharacterController del jugador
            CharacterController cc = other.GetComponent<CharacterController>();

            if (cc != null)
            {
                // Apagamos las físicas temporalmente para permitir el teletransporte
                cc.enabled = false;
                other.transform.position = puntoAparicionJefe.position;
                cc.enabled = true;
            }
            else
            {
                // Respaldo por si en el futuro cambias el sistema de movimiento
                other.transform.position = puntoAparicionJefe.position;
            }

            Debug.Log("¡Warp a la zona del jefe exitoso!");
        }
    }
}