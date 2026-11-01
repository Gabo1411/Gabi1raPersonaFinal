using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Boss : MonoBehaviour, IDamageable
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
    public AudioSource fuenteTaunt;
    public AudioSource fuenteDisparo;
    public AudioSource fuenteMuerte;

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

        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null) player = playerMovement.transform;

        StartCoroutine(CicloDeIA());
    }

    void Update()
    {
        if (player == null || estaMuerto) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 3f);
    }

    IEnumerator CicloDeIA()
    {
        while (!estaMuerto)
        {
            yield return new WaitForSeconds(tiempoEntreCiclos);
            if (estaMuerto) yield break;

            if (animator != null) animator.SetTrigger("Taunt");

            Sfx.Play(fuenteTaunt, clipTaunt);

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
        if (estaMuerto || balaPrefab == null || puntoDisparo == null) return;

        Sfx.Play(fuenteDisparo, clipDisparo);

        GameObject bala = PrefabPool.Get(balaPrefab, pos, puntoDisparo.rotation);
        if (seguir && player != null) bala.transform.LookAt(player.position + Vector3.up * 1.2f);
    }

    public void TakeDamage(int damage)
    {
        if (estaMuerto) return;

        currentHealth -= damage;
        ActualizarUI();
        if (currentHealth <= 0) Die();
    }

    void ActualizarUI()
    {
        if (barraDeVida != null) barraDeVida.fillAmount = Mathf.Clamp01((float)currentHealth / maxHealth);
    }

    void Die()
    {
        estaMuerto = true;
        StopAllCoroutines();

        Sfx.Play(fuenteMuerte, clipMuerte);

        if (animator != null) animator.SetTrigger("Die");
        if (particulasMuerte != null) Instantiate(particulasMuerte, transform.position, Quaternion.identity);

        if (GameManager.Instance != null) GameManager.Instance.BossDefeated();
        Destroy(gameObject, 2.5f);
    }
}
