using UnityEngine;

public class TrapDoorDemo : MonoBehaviour
{
    public Animator TrapDoorAnim;

    [Header("Configuración de Teletransporte")]
    public Transform puntoAparicionJefe;

    [Header("Configuración del Jefe (NUEVO)")]
    public GameObject jefePrincipal; // Arrastra el objeto de tu Jefe aquí

    [Header("Marcador")]
    [Tooltip("Segundos tras limpiar la arena hasta que aparece la señal de la trampilla.")]
    public float retrasoMarcador = 8f;

    private bool estaAbierta = false;
    private bool yaUsada = false;
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
        Invoke(nameof(EjecutarAnimacion), 2f);
        Invoke(nameof(MarcarEscotilla), retrasoMarcador);
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

    // Baliza verde y marcador en pantalla para que el jugador encuentre la trampilla
    private void MarcarEscotilla()
    {
        if (yaUsada) return;

        GameObject luz = new GameObject("BalizaEscotilla");
        luz.transform.SetParent(transform, false);
        luz.transform.localPosition = Vector3.up * 1.5f;

        Light baliza = luz.AddComponent<Light>();
        baliza.type = LightType.Point;
        baliza.color = ObjectiveMarker.MarkerColor;
        baliza.range = 12f;
        baliza.intensity = 4f;

        ObjectiveMarker.Show(transform, baliza);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (estaAbierta && other.CompareTag("Player"))
        {
            yaUsada = true;
            ObjectiveMarker.Hide();

            if (puntoAparicionJefe == null) return;

            WarpPlayer(other.transform);
            ActivarArena();
        }
    }

    // Mueve al jugador al punto de aparición de la arena del jefe
    public void WarpPlayer(Transform player)
    {
        if (puntoAparicionJefe == null) return;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            player.position = puntoAparicionJefe.position;
            cc.enabled = true;
        }
        else
        {
            player.position = puntoAparicionJefe.position;
        }
    }

    // Despierta al jefe y a los spawners de la arena y oculta la puntuación
    public void ActivarArena()
    {
        yaUsada = true;
        ObjectiveMarker.Hide();

        if (jefePrincipal != null) jefePrincipal.SetActive(true);
        ArenaAtmosphere.WakeUp();

        foreach (BossArenaSpawner spawner in FindObjectsByType<BossArenaSpawner>(FindObjectsSortMode.None))
        {
            spawner.ActivarSpawner();
        }

        GameManager manager = GameManager.Instance;
        if (manager != null) manager.OcultarPuntaje();

        Debug.Log("¡Arena del jefe activada!");
    }

    public bool ArenaActive => jefePrincipal != null && jefePrincipal.activeSelf;
}
