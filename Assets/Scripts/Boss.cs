using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Boss : MonoBehaviour
{
    [Header("Estadísticas y UI")]
    public int maxHealth = 100;
    private int currentHealth;
    public Image barraDeVida;
    public GameObject particulasMuerte;

    [Header("Referencias de Combate")]
    public GameObject balaPrefab;
    public Transform puntoDisparo;
    private Transform player;

    [Header("Configuración General IA")]
    public float tiempoEntreCiclos = 4.0f;
    private bool estaAtacando = false;

    [Header("Ataque 1: Ráfaga")]
    public int balasRafaga = 5;
    public float cadenciaRafaga = 0.15f;

    [Header("Ataque 2: Metralladora")]
    public float duracionMetralladora = 3.0f;
    public float cadenciaMetralladora = 0.1f;

    [Header("Ataque 3: Triángulo (Muro)")]
    public int repeticionesTriangulo = 3;
    public float esperaEntreTriangulos = 0.8f;
    public float separacionTriangulo = 0.6f;

    void Start()
    {
        currentHealth = maxHealth;
        ActualizarUI();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        StartCoroutine(CicloDeIA());
    }

    void Update()
    {
        if (player == null || currentHealth <= 0) return;

        // El jefe siempre mira al jugador
        Vector3 direccion = (player.position - transform.position);
        direccion.y = 0;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direccion), Time.deltaTime * 3f);
    }

    IEnumerator CicloDeIA()
    {
        while (currentHealth > 0)
        {
            yield return new WaitForSeconds(tiempoEntreCiclos);

            int eleccion = Random.Range(0, 3);

            if (eleccion == 0) yield return AtaqueRafaga();
            else if (eleccion == 1) yield return AtaqueMetralladora();
            else yield return AtaqueTriangulo();
        }
    }

    // --- LÓGICA DE LOS ATAQUES ---

    IEnumerator AtaqueRafaga()
    {
        estaAtacando = true;
        for (int i = 0; i < balasRafaga; i++)
        {
            InstanciarBala(puntoDisparo.position, true); // Sigue al jugador
            yield return new WaitForSeconds(cadenciaRafaga);
        }
        estaAtacando = false;
    }

    IEnumerator AtaqueMetralladora()
    {
        estaAtacando = true;
        float tiempoFin = Time.time + duracionMetralladora;
        while (Time.time < tiempoFin)
        {
            InstanciarBala(puntoDisparo.position, true); // Sigue al jugador
            yield return new WaitForSeconds(cadenciaMetralladora);
        }
        estaAtacando = false;
    }

    IEnumerator AtaqueTriangulo()
    {
        estaAtacando = true;
        for (int i = 0; i < repeticionesTriangulo; i++)
        {
            // Posiciones en formación de triángulo (usando ejes locales del jefe)
            Vector3 posArriba = puntoDisparo.position + puntoDisparo.up * separacionTriangulo;
            Vector3 posIzquierda = puntoDisparo.position - puntoDisparo.up * (separacionTriangulo / 2f) - puntoDisparo.right * separacionTriangulo;
            Vector3 posDerecha = puntoDisparo.position - puntoDisparo.up * (separacionTriangulo / 2f) + puntoDisparo.right * separacionTriangulo;

            // Disparamos las 3 SIN LookAt (falso) para que viajen en paralelo
            InstanciarBala(posArriba, false);
            InstanciarBala(posIzquierda, false);
            InstanciarBala(posDerecha, false);

            yield return new WaitForSeconds(esperaEntreTriangulos);
        }
        estaAtacando = false;
    }

    void InstanciarBala(Vector3 pos, bool seguirJugador)
    {
        if (balaPrefab == null || puntoDisparo == null) return;

        GameObject bala = Instantiate(balaPrefab, pos, puntoDisparo.rotation);

        if (seguirJugador)
        {
            // Apunta al torso para mayor precisión
            bala.transform.LookAt(player.position + Vector3.up * 1.2f);
        }
        // Si seguirJugador es falso, la bala mantiene la rotación del puntoDisparo (paralela)
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        ActualizarUI();
        if (currentHealth <= 0) Die();
    }

    void ActualizarUI() => barraDeVida.fillAmount = (float)currentHealth / maxHealth;

    void Die()
    {
        StopAllCoroutines();
        if (particulasMuerte != null) Instantiate(particulasMuerte, transform.position, Quaternion.identity);
        FindFirstObjectByType<GameManager>()?.BossDefeated();
        Destroy(gameObject);
    }
}