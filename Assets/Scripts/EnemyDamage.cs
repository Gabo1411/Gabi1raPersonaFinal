using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public int damage = 10;
    public float damageCooldown = 1f;
    public float attackRange = 2.0f; // Un poco más que tu Stopping Distance (1.5)

    private float nextDamageTime = 0f;
    private Transform playerTransform;
    private PlayerMovement playerScript;

    void Start()
    {
        // Buscamos al jugador una sola vez al inicio para optimizar
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerScript = playerObj.GetComponent<PlayerMovement>();
        }
    }

    void Update()
    {
        // Si no encontramos al jugador, no hacemos nada
        if (playerTransform == null || playerScript == null) return;

        // Calculamos la distancia real
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // Si está dentro del rango y pasó el tiempo de cooldown
        if (distance <= attackRange)
        {
            if (Time.time >= nextDamageTime)
            {
                AttackPlayer();
                nextDamageTime = Time.time + damageCooldown;
            }
        }
    }

    void AttackPlayer()
    {
        playerScript.Health -= damage;

        if (playerScript.Health <= 0)
        {
            playerScript.Health = 0;

            // CAMBIO: Actualizado a la nueva sintaxis de Unity
            GameManager manager = FindFirstObjectByType<GameManager>();

            if (manager != null)
            {
                manager.PlayerDied();
            }
        }
    }
}