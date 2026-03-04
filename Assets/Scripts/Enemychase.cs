using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyChase : MonoBehaviour
{
    public Transform player;
    private NavMeshAgent agent;
    private Animator animator; // Referencia al Animator

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Buscamos el Animator (puede estar en este objeto o en el hijo "Cactus")
        animator = GetComponentInChildren<Animator>();

        // TRUCO PRO: Des-sincronización
        // Esto hace que la animación empiece en un punto aleatorio (0% a 100%)
        // Así no parecerán robots marchando al mismo tiempo.
        if (animator != null)
        {
            animator.Play("Run", 0, Random.Range(0f, 1f));
        }

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (player == null || !agent.isOnNavMesh) return;

        // 1. Mover al enemigo
        agent.SetDestination(player.position);

        // 2. Controlar la animación de correr
        // Si la velocidad es mayor a 0.1, está corriendo
        if (animator != null)
        {
            animator.SetBool("isRunning", agent.velocity.magnitude > 0.1f);
        }
    }

    // Esta función detecta cuando el enemigo toca al jugador para atacar
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // AQUÍ ACTIVAMOS EL ATAQUE
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            // Aquí iría tu lógica de hacer daño (ej: player.TakeDamage(10))
            Debug.Log("¡El cactus ataca!");
        }
    }
}