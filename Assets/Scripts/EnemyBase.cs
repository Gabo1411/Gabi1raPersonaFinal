using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Lógica común de los enemigos que persiguen al jugador: vida, persecución, ataque y muerte.
// Animación de aparición de un enemigo: mientras dura, no persigue ni ataca
public interface IEntrance
{
    bool IsEmerging { get; }
}

public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    public static readonly List<EnemyBase> Active = new List<EnemyBase>();

    public static int AliveCount
    {
        get
        {
            int count = 0;
            foreach (EnemyBase enemy in Active) if (enemy.health > 0) count++;
            return count;
        }
    }

    [Header("Estadísticas")]
    public int health = 15;
    public int pointsValue = 10;
    public int damage = 10;

    [Header("Ataque")]
    public float distanciaAtaque = 1.8f;
    public float cooldownAtaque = 1.5f;
    [Range(0f, 1f)]
    [Tooltip("Momento de la animación de ataque (0-1) en el que el golpe hace daño.")]
    public float momentoGolpe = 0.5f;

    [Header("Efectos y Sonido")]
    public GameObject particulasMuerte;
    [Tooltip("Usa las partículas de muerte propias de la arena del jefe (las activa el spawner de la arena).")]
    public bool efectoMuerteArena;
    public AudioClip clipMuerte;

    protected NavMeshAgent agent;
    protected Animator animator;
    protected Collider hitCollider;
    protected PlayerMovement jugador;

    private const float RepathInterval = 0.2f;
    private const float HitRangeTolerance = 1.3f;
    private const float FallbackHitDelay = 0.3f;

    private float tiempoSiguienteAtaque;
    private float nextRepathTime;
    private float hitDelay = FallbackHitDelay;

    private IEntrance entrance;
    bool IsEmerging => entrance != null && entrance.IsEmerging;

    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);

    protected virtual void Start()
    {
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        hitCollider = GetComponent<Collider>();
        jugador = FindFirstObjectByType<PlayerMovement>();

        hitDelay = ComputeHitDelay();
        OnStarted();

        entrance = StartEntrance();
    }

    // Animación de aparición: por defecto salen reptando de la tierra
    protected virtual IEntrance StartEntrance()
    {
        return EnemyEmerge.Begin(this, agent, hitCollider, animator, jugador != null ? jugador.transform : null);
    }

    protected virtual void OnStarted() { }
    protected virtual void OnTick() { }

    void Update()
    {
        if (health <= 0 || jugador == null || jugador.IsDead) return;
        if (IsEmerging) return;

        if (agent != null && agent.enabled && Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + RepathInterval;
            agent.SetDestination(jugador.transform.position);
        }

        OnTick();

        float distancia = Vector3.Distance(transform.position, jugador.transform.position);
        if (distancia <= distanciaAtaque && Time.time >= tiempoSiguienteAtaque)
        {
            Atacar();
        }
    }

    void Atacar()
    {
        tiempoSiguienteAtaque = Time.time + cooldownAtaque;

        if (animator != null) animator.SetTrigger("Attack");

        StartCoroutine(AplicarGolpe());
    }

    // El daño se aplica a mitad de la animación, y solo si el jugador sigue al alcance.
    IEnumerator AplicarGolpe()
    {
        yield return new WaitForSeconds(hitDelay);

        if (health <= 0 || jugador == null) yield break;

        float distancia = Vector3.Distance(transform.position, jugador.transform.position);
        if (distancia <= distanciaAtaque * HitRangeTolerance)
        {
            jugador.TakeDamage(damage);
        }
    }

    float ComputeHitDelay()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return FallbackHitDelay;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name.IndexOf("attack", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return clip.length * momentoGolpe;
            }
        }
        return FallbackHitDelay;
    }

    public void TakeDamage(int amount)
    {
        ApplyDamage(amount, true);
    }

    void ApplyDamage(int amount, bool byDamage)
    {
        if (health <= 0) return;

        health -= amount;
        if (byDamage) HitFlash.Play(this);
        if (health <= 0) Die(byDamage);
    }

    // Elimina al enemigo sin dar puntos (limpieza de arena).
    public virtual void ClearFromMap()
    {
        Kill();
    }

    // Muerte normal sin importar la vida restante (limpieza del mapa: sin sonido de baja ni botín).
    public void Kill()
    {
        ApplyDamage(Mathf.Max(health, 1), false);
    }

    void Die(bool byDamage)
    {
        health = 0;

        if (byDamage && GameManager.Instance != null) GameManager.Instance.PlayKillSound();

        Sfx.AtPoint(clipMuerte, transform.position);

        GenerarParticulas();

        if (GameManager.Instance != null) GameManager.Instance.AddScore(pointsValue);

        if (animator != null) animator.SetTrigger("Die");
        if (agent != null) agent.enabled = false;
        if (hitCollider != null) hitCollider.enabled = false;

        // La limpieza de enemigos (sin daño del jugador) no suelta botín
        LootTable loot = GetComponent<LootTable>();
        if (byDamage && loot != null) loot.IntentarSoltarBotin();

        Destroy(gameObject, 2f);
    }

    // Cada tipo de enemigo define cuál es su efecto de muerte en la arena del jefe
    protected abstract EnemyDeathEffects.Kind DeathKind { get; }

    protected void GenerarParticulas()
    {
        if (efectoMuerteArena)
        {
            // El efecto se centra en el cuerpo real del enemigo y crece con su tamaño
            float height = 1.7f;
            Vector3 center = transform.position + Vector3.up * 0.8f;
            bool hasBounds = false;
            Bounds bounds = new Bounds(center, Vector3.one);
            foreach (Renderer rend in GetComponentsInChildren<Renderer>())
            {
                if (rend is ParticleSystemRenderer) continue;

                if (!hasBounds) { bounds = rend.bounds; hasBounds = true; }
                else bounds.Encapsulate(rend.bounds);
            }
            if (hasBounds)
            {
                height = bounds.size.y;
                center = bounds.center;
            }
            float scale = Mathf.Clamp(height / 1.7f, 1f, 4f);
            EnemyDeathEffects.Play(DeathKind, center, scale);
            return;
        }

        if (particulasMuerte != null)
        {
            Instantiate(particulasMuerte, transform.position, Quaternion.identity);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaAtaque);
    }
}
