using UnityEngine;

public class TrapDoorDemo : MonoBehaviour
{
    public Animator TrapDoorAnim;

    [Header("Configuración de Teletransporte")]
    public Transform puntoAparicionJefe;

    [Header("Configuración del Jefe (NUEVO)")]
    public GameObject jefePrincipal; // Arrastra el objeto de tu Jefe aquí

    private bool estaAbierta = false;
    private Collider miCollider;

    void Awake()
    {
        if (TrapDoorAnim == null)
        {
            TrapDoorAnim = GetComponent<Animator>();
        }

        miCollider = GetComponent<Collider>();
        if (miCollider != null)
        {
            miCollider.enabled = false;
        }

        // NUEVO: Nos aseguramos de apagar al jefe al iniciar el juego
        // para que no interactúe con nada hasta que bajes.
        if (jefePrincipal != null)
        {
            jefePrincipal.SetActive(false);
        }
    }

    public void AbrirEscotilla()
    {
        Invoke("EjecutarAnimacion", 2f);
    }

    private void EjecutarAnimacion()
    {
        estaAbierta = true;

        if (TrapDoorAnim != null)
        {
            TrapDoorAnim.SetTrigger("open");
        }

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

            // --- NUEVO: DESPERTAR LA ARENA DEL JEFE ---

            // 1. Encendemos al jefe principal
            if (jefePrincipal != null)
            {
                jefePrincipal.SetActive(true);
            }

            // 2. Buscamos todos los spawners de la arena y los activamos
            BossArenaSpawner[] spawners = FindObjectsByType<BossArenaSpawner>(FindObjectsSortMode.None);
            foreach (BossArenaSpawner spawner in spawners)
            {
                spawner.ActivarSpawner();
            }

            Debug.Log("¡Warp a la zona del jefe exitoso! Arena activada.");

            GameManager manager = FindFirstObjectByType<GameManager>();
            if (manager != null)
            {
                manager.OcultarPuntaje();
            }

            Debug.Log("¡Warp a la zona del jefe exitoso! Arena activada y puntos ocultos.");
        }
    }
}