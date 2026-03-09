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

    [Header("Referencias")]
    public GameObject balaPrefab;
    public Transform puntoDisparo;
    private Transform player;
    private Animator animator;

    [Header("Fuentes de Sonido (AudioSources)")]
    public AudioSource fuenteTaunt;    // Arrastra aquí el AudioSource para el rugido
    public AudioSource fuenteDisparo;  // Arrastra aquí el AudioSource para los disparos
    public AudioSource fuenteMuerte;   // Arrastra aquí el AudioSource para la muerte

    [Header("Clips de Audio")]
    public AudioClip clipTaunt;
    public AudioClip clipDisparo;
    public AudioClip clipMuerte;

    [Header("Configuración IA")]
    public float tiempoEntreCiclos = 4.0f;
    public float esperaAperturaBoca = 0.5f;
    private bool estaMuerto = false;

    [Header("Ataques (5 segundos)")]
    public int balasRafaga = 15;
    public float cadenciaRafaga = 0.33f;
    public float duracionMetralladora = 5.0f;
    public int seriesDeTriangulos = 6;
    public float pausaEntreSeries = 0.8f;
    public float separacionTriangulo = 1.2f;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
        ActualizarUI();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        StartCoroutine(CicloDeIA());
    }

    void Update()
    {
        if (player == null || estaMuerto) return;
        Vector3 dir = (player.position - transform.position);
        dir.y = 0;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 3f);
    }

    IEnumerator CicloDeIA()
    {
        while (!estaMuerto)
        {
            yield return new WaitForSeconds(tiempoEntreCiclos);
            if (estaMuerto) yield break;

            if (animator != null) animator.SetTrigger("Taunt");

            // --- REPRODUCIR RUGIDO ---
            if (fuenteTaunt != null && clipTaunt != null) fuenteTaunt.PlayOneShot(clipTaunt);

            yield return new WaitForSeconds(esperaAperturaBoca);

            int eleccion = Random.Range(0, 3);
            if (eleccion == 0) yield return AtaqueRafaga();
            else if (eleccion == 1) yield return AtaqueMetralladora();
            else yield return AtaqueTriangulo();
        }
    }

    IEnumerator AtaqueRafaga()
    {
        for (int i = 0; i < balasRafaga; i++)
        {
            InstanciarBala(puntoDisparo.position, true);
            yield return new WaitForSeconds(cadenciaRafaga);
        }
    }

    IEnumerator AtaqueMetralladora()
    {
        float tiempoFin = Time.time + duracionMetralladora;
        while (Time.time < tiempoFin)
        {
            InstanciarBala(puntoDisparo.position, true);
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator AtaqueTriangulo()
    {
        for (int i = 0; i < seriesDeTriangulos; i++)
        {
            Vector3 up = puntoDisparo.up * separacionTriangulo;
            Vector3 right = puntoDisparo.right * separacionTriangulo;

            InstanciarBala(puntoDisparo.position + up, false);
            InstanciarBala(puntoDisparo.position - up / 2f - right, false);
            InstanciarBala(puntoDisparo.position - up / 2f + right, false);

            yield return new WaitForSeconds(pausaEntreSeries);
        }
    }

    void InstanciarBala(Vector3 pos, bool seguir)
    {
        if (estaMuerto || balaPrefab == null) return;

        // --- REPRODUCIR DISPARO ---
        if (fuenteDisparo != null && clipDisparo != null) fuenteDisparo.PlayOneShot(clipDisparo);

        GameObject b = Instantiate(balaPrefab, pos, puntoDisparo.rotation);
        if (seguir) b.transform.LookAt(player.position + Vector3.up * 1.2f);
    }

    public void TakeDamage(int damage)
    {
        if (estaMuerto) return;
        currentHealth -= damage;
        ActualizarUI();
        if (currentHealth <= 0) Die();
    }

    void ActualizarUI() => barraDeVida.fillAmount = (float)currentHealth / maxHealth;

    void Die()
    {
        estaMuerto = true;
        StopAllCoroutines();

        // --- REPRODUCIR MUERTE ---
        if (fuenteMuerte != null && clipMuerte != null) fuenteMuerte.PlayOneShot(clipMuerte);

        if (animator != null) animator.SetTrigger("Die");
        if (particulasMuerte != null) Instantiate(particulasMuerte, transform.position, Quaternion.identity);

        FindFirstObjectByType<GameManager>()?.BossDefeated();
        Destroy(gameObject, 2.5f);
    }
}