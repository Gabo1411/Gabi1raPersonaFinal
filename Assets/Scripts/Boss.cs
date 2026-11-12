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
    private PlayerMovement playerMovement;
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
    [Tooltip("Ráfaga de misiles: cuántos salen por cada lado (en total el doble), el intervalo entre parejas y el daño de cada uno.")]
    public int misilesPorLado = 4;
    public float intervaloMisiles = 0.3f;
    public int danioMisil = 15;

    // Para los efectos del escenario (el foco de la arena reacciona a la vida del jefe)
    // ---------- Consola debug ----------
    [HideInInspector] public bool frozen;
    public int CurrentHealth => currentHealth;
    public bool IsFrozen => frozen;

    public void DebugSetHealth(int value)
    {
        if (estaMuerto) return;
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        ActualizarUI();
        if (currentHealth <= 0) Die();
    }

    // Lanza un ataque concreto ahora mismo (con el gesto de burla previo). Devuelve false si el nombre no existe.
    public bool DebugAttack(string name)
    {
        if (estaMuerto) return false;
        switch (name)
        {
            case "bullets": case "rafaga": case "ráfaga": StartCoroutine(DebugAttackRoutine(AtaqueRafaga())); return true;
            case "gatling": case "metralladora": StartCoroutine(DebugAttackRoutine(AtaqueMetralladora())); return true;
            case "missiles": case "misiles": StartCoroutine(DebugAttackRoutine(AtaqueMisiles())); return true;
        }
        return false;
    }

    IEnumerator DebugAttackRoutine(IEnumerator attack)
    {
        if (animator != null) animator.SetTrigger("Taunt");
        Sfx.Play(fuenteTaunt, clipTaunt);
        yield return new WaitForSeconds(esperaAperturaBoca);
        yield return attack;
    }

    public float HealthFraction => maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 1f;
    public bool IsDead => estaMuerto;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
        ActualizarUI();

        playerMovement = FindFirstObjectByType<PlayerMovement>();
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
            while (frozen && !estaMuerto) yield return null;
            if (estaMuerto) yield break;

            if (animator != null) animator.SetTrigger("Taunt");

            Sfx.Play(fuenteTaunt, clipTaunt);

            yield return new WaitForSeconds(esperaAperturaBoca);

            int eleccion = Random.Range(0, 3);
            if (eleccion == 0) yield return AtaqueRafaga();
            else if (eleccion == 1) yield return AtaqueMetralladora();
            else yield return AtaqueMisiles();
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

    // Ráfaga de misiles: salen por parejas, uno de cada costado del jefe. Cada uno sube, fija la posición del jugador
    // en ese momento y cae sobre ella en línea recta (con un anillo en el suelo que marca el punto).
    IEnumerator AtaqueMisiles()
    {
        for (int i = 0; i < misilesPorLado; i++)
        {
            if (estaMuerto || puntoDisparo == null) yield break;

            Sfx.Play(fuenteDisparo, clipDisparo);
            Vector3 lado = transform.right;
            Vector3 origen = puntoDisparo.position;
            BossMissile.Launch(origen + lado * 1.8f, player, danioMisil, balaPrefab, lado);
            BossMissile.Launch(origen - lado * 1.8f, player, danioMisil, balaPrefab, -lado);
            yield return new WaitForSeconds(intervaloMisiles);
        }
    }

    void InstanciarBala(Vector3 pos, bool seguir)
    {
        if (estaMuerto || balaPrefab == null || puntoDisparo == null) return;

        Sfx.Play(fuenteDisparo, clipDisparo);

        GameObject bala = PrefabPool.Get(balaPrefab, pos, puntoDisparo.rotation);
        if (seguir && player != null) bala.transform.LookAt(playerMovement != null ? playerMovement.AimPoint : player.position + Vector3.up * 1.2f);
    }

    public void TakeDamage(int damage)
    {
        if (estaMuerto) return;

        currentHealth -= damage;
        HitFlash.Play(this);
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

        StartCoroutine(ExplotarBotin());

        if (GameManager.Instance != null) GameManager.Instance.BossDefeated();
    }

    // El jefe se queda en el suelo y de su interior revienta un botín de cofre de fantasía
    IEnumerator ExplotarBotin()
    {
        yield return new WaitForSeconds(0.5f);

        Bounds bounds = new Bounds(transform.position, Vector3.one);
        bool hasBounds = false;
        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            if (rend is ParticleSystemRenderer) continue;

            if (!hasBounds) { bounds = rend.bounds; hasBounds = true; }
            else bounds.Encapsulate(rend.bounds);
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        BossExplosion.Play(bounds.center, bounds.size, colliders);

        // El cuerpo ya no estorba: así el jugador puede llegar al portal
        foreach (Collider col in colliders) col.enabled = false;
    }
}
