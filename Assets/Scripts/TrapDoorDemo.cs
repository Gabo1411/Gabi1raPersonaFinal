using UnityEngine;

public class TrapDoorDemo : MonoBehaviour
{
    public Animator TrapDoorAnim;

    [Header("Configuración de Teletransporte")]
    public Transform puntoAparicionJefe;

    private bool estaAbierta = false;
    private Collider miCollider; // NUEVO: Referencia al collider de la escotilla

    void Awake()
    {
        if (TrapDoorAnim == null)
        {
            TrapDoorAnim = GetComponent<Animator>();
        }

        // Buscamos el collider y lo apagamos desde el inicio
        miCollider = GetComponent<Collider>();
        if (miCollider != null)
        {
            miCollider.enabled = false;
        }
    }

    public void AbrirEscotilla()
    {
        // Esperamos 2 segundos antes de ejecutar la animación y habilitar la caída
        Invoke("EjecutarAnimacion", 2f);
    }

    private void EjecutarAnimacion()
    {
        estaAbierta = true;

        if (TrapDoorAnim != null)
        {
            TrapDoorAnim.SetTrigger("open");
        }

        // Encendemos el collider justo cuando la puerta se abre
        if (miCollider != null)
        {
            miCollider.enabled = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (estaAbierta && other.CompareTag("Player"))
        {
            if (puntoAparicionJefe == null) return;

            CharacterController cc = other.GetComponent<CharacterController>();

            if (cc != null)
            {
                cc.enabled = false;
                other.transform.position = puntoAparicionJefe.position;
                cc.enabled = true;
            }
            else
            {
                other.transform.position = puntoAparicionJefe.position;
            }
        }
    }
}