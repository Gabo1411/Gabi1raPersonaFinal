using UnityEngine;
using UnityEngine.AI;

public class EnemyMushroom : MonoBehaviour
{
    [Header("Estadísticas")]
    public int health = 10;
    public int damage = 5;
    public int pointsValue = 15;

    [Header("Ataque")]
    public float distanciaAtaque = 2.0f;
    public float cooldownAtaque = 1.2f;
    private float tiempoSiguienteAtaque;

    [Header("Efectos y Sonido")]
    public ParticleSystem particulasMovimiento;
    public GameObject particulasMuerte;
    public AudioClip clipMuerte; // NUEVO: Asigna el sonido de muerte aquí

    private NavMeshAgent agent;
    private Transform jugador;
    private Animator animator;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) jugador = p.transform;

        if (animator != null)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            animator.Play(state.fullPathHash, 0, Random.Range(0f, 1f));
        }
    }

    void Update()
    {
        if (health <= 0 || jugador == null) return;

        agent.SetDestination(jugador.position);
        ControlarParticulas();

        float distancia = Vector3.Distance(transform.position, jugador.position);
        if (distancia <= distanciaAtaque && Time.time >= tiempoSiguienteAtaque)
        {
            Atacar();
        }
    }

    void ControlarParticulas()
    {
        if (particulasMovimiento == null) return;
        if (agent.velocity.magnitude > 0.2f)
        {
            if (!particulasMovimiento.isPlaying) particulasMovimiento.Play();
        }
        else
        {
            if (particulasMovimiento.isPlaying) particulasMovimiento.Stop();
        }
    }

    void Atacar()
    {
        tiempoSiguienteAtaque = Time.time + cooldownAtaque;
        if (animator != null) animator.SetTrigger("Attack");

        PlayerMovement pScript = jugador.GetComponent<PlayerMovement>();
        if (pScript != null)
        {
            pScript.Health -= damage;
            if (pScript.Health <= 0)
            {
                pScript.Health = 0;
                FindFirstObjectByType<GameManager>()?.PlayerDied();
            }
        }
    }

    public void TakeDamage(int amount)
    {
        if (health <= 0) return;
        health -= amount;
        if (health <= 0) Die();
    }

    void Die()
    {
        health = 0;

        // REPRODUCIR SONIDO DE MUERTE
        if (clipMuerte != null)
        {
            AudioSource.PlayClipAtPoint(clipMuerte, transform.position);
        }

        if (animator != null) animator.SetTrigger("Die");

        if (agent != null) agent.enabled = false;
        GetComponent<Collider>().enabled = false;

        if (particulasMuerte != null) Instantiate(particulasMuerte, transform.position, Quaternion.identity);
        FindFirstObjectByType<GameManager>()?.AddScore(pointsValue);

        Destroy(gameObject, 2f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaAtaque);
    }
}