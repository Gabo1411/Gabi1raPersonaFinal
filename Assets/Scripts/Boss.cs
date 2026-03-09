using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Boss : MonoBehaviour
{
    [Header("Estadísticas")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Efectos y UI")]
    public GameObject particulasMuerte;
    public Image barraDeVida;

    [Header("Ataques Comunes")]
    public GameObject balaPrefab;
    public Transform puntoDisparo;
    private Transform player;

    [Header("Ataque 1: Ráfaga de 5")]
    public int balasPorRafaga = 5;
    public float tiempoEntreBalasRafaga = 0.15f;

    [Header("Ataque 2: Metralladora Continua")]
    public float duracionMetralladora = 3.0f; // Tiempo que está disparando sin parar
    public float cadenciaMetralladora = 0.1f; // Muy rápido entre balas

    [Header("IA del Jefe")]
    public float tiempoEntreAtaques = 4.0f;
    private float temporizadorIA;
    private bool estaAtacando = false;

    void Start()
    {
        currentHealth = maxHealth;
        ActualizarUI();
        temporizadorIA = tiempoEntreAtaques;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void Update()
    {
        if (player == null || currentHealth <= 0 || estaAtacando) return;

        // Seguir al jugador con la mirada
        Vector3 direccion = player.position - transform.position;
        direccion.y = 0;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direccion), Time.deltaTime * 3f);

        // Temporizador para elegir ataque
        temporizadorIA -= Time.deltaTime;
        if (temporizadorIA <= 0f)
        {
            ElegirAtaqueAleatorio();
            temporizadorIA = tiempoEntreAtaques;
        }
    }

    void ElegirAtaqueAleatorio()
    {
        int eleccion = Random.Range(0, 2); // 0 o 1
        if (eleccion == 0) StartCoroutine(AtaqueRafaga());
        else StartCoroutine(AtaqueMetralladora());
    }

    // ATAQUE 1: Las 5 balas rápidas
    IEnumerator AtaqueRafaga()
    {
        estaAtacando = true;
        for (int i = 0; i < balasPorRafaga; i++)
        {
            DispararUnaBala();
            yield return new WaitForSeconds(tiempoEntreBalasRafaga);
        }
        estaAtacando = false;
    }

    // ATAQUE 2: Metralladora continua (Te sigue mientras dispara)
    IEnumerator AtaqueMetralladora()
    {
        estaAtacando = true;
        float tiempoFin = Time.time + duracionMetralladora;

        while (Time.time < tiempoFin)
        {
            // Mientras dispara en este modo, el jefe rota más rápido para seguirte
            Vector3 direccion = (player.position + Vector3.up * 1f) - puntoDisparo.position;
            puntoDisparo.rotation = Quaternion.LookRotation(direccion);

            DispararUnaBala();
            yield return new WaitForSeconds(cadenciaMetralladora);
        }
        estaAtacando = false;
    }

    void DispararUnaBala()
    {
        if (balaPrefab != null && puntoDisparo != null)
        {
            GameObject bala = Instantiate(balaPrefab, puntoDisparo.position, puntoDisparo.rotation);
            bala.transform.LookAt(player.position + Vector3.up * 1.0f);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        ActualizarUI();
        if (currentHealth <= 0) Die();
    }

    private void ActualizarUI()
    {
        if (barraDeVida != null) barraDeVida.fillAmount = (float)currentHealth / maxHealth;
    }

    void Die()
    {
        if (particulasMuerte != null) Instantiate(particulasMuerte, transform.position, Quaternion.identity);
        FindFirstObjectByType<GameManager>()?.BossDefeated();
        Destroy(gameObject);
    }
}