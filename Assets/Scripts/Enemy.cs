using UnityEngine;
using UnityEngine.AI; // Necesario para el NavMesh

public class Enemy : MonoBehaviour
{
    public int health = 15;
    public int pointsValue = 10;
    public int damage = 10; // Daño que hace el cactus

    [Header("Efectos Visuales")]
    public GameObject particulasMuerte;

    [Header("Ataque y Movimiento")]
    public float distanciaAtaque = 1.8f;
    public float cooldownAtaque = 1.5f;
    private float tiempoSiguienteAtaque;

    [Header("Sonido de Muerte")]
    public AudioClip clipMuerte;

    private NavMeshAgent agent;
    private Transform jugador;
    private Animator animator;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) jugador = playerObj.transform;

        // Iniciamos la animación de caminar/correr de forma aleatoria para que no parezcan robots
        if (animator != null)
        {
            animator.Play("Cactus_WalkFWD", 0, Random.Range(0f, 1f));
        }
    }

    void Update() // Lógica de movimiento y ataque del enemigo
    {
        if (health <= 0 || jugador == null) return;

        // 1. Perseguir al jugador
        agent.SetDestination(jugador.position);

        // 2. Lógica de ataque
        float distancia = Vector3.Distance(transform.position, jugador.position);

        if (distancia <= distanciaAtaque && Time.time >= tiempoSiguienteAtaque)
        {
            Atacar();
        }
    }

    void Atacar()
    {
        tiempoSiguienteAtaque = Time.time + cooldownAtaque;

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // LLAMADA DIRECTA: Sin esperas ni condiciones extra para probar
        AplicarDanioAlJugador();
    }

    void AplicarDanioAlJugador()
    {
        if (health <= 0 || jugador == null) return;

        // Buscamos el script del jugador
        PlayerMovement pScript = jugador.GetComponent<PlayerMovement>();

        if (pScript != null)
        {
            pScript.Health -= damage;
            Debug.Log("¡DAÑO CONFIRMADO! Vida actual: " + pScript.Health);

            if (pScript.Health <= 0)
            {
                pScript.Health = 0;
                FindFirstObjectByType<GameManager>()?.PlayerDied();
            }
        }
        else
        {
            Debug.LogError("No se encontró el script PlayerMovement en el objeto con Tag Player");
        }
    }

    public void TakeDamage(int damage)
    {
        if (health <= 0) return;
        health -= damage;
        if (health <= 0) Die();
    }

    public void ClearFromMap()
    {
        GenerarParticulas();
        Destroy(gameObject);
    }

    void Die()
    {
        if (clipMuerte != null)
        {
            AudioSource.PlayClipAtPoint(clipMuerte, transform.position);
        }
            
        GenerarParticulas();
        FindFirstObjectByType<GameManager>()?.AddScore(pointsValue);

        if (animator != null) animator.SetTrigger("Die");

        if (agent != null) agent.enabled = false;
        GetComponent<Collider>().enabled = false;

        LootTable loot = GetComponent<LootTable>();
        if (loot != null) loot.IntentarSoltarBotin();

        Destroy(gameObject, 2f);
    }

    private void GenerarParticulas()
    {
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