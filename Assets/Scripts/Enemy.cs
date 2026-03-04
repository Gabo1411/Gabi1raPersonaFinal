using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int health = 5;
    public int pointsValue = 10; // Puntos que el jugador gana al derrotar a este enemigo
    public void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    { 
        GameManager manager = FindFirstObjectByType<GameManager>();

        if (manager != null)
        {
            manager.AddScore(pointsValue); // Suma puntos al jugador
        }

        // Solo desaparece. No avisa al manager (porque ahora vamos por tiempo)
        Destroy(gameObject);
    }
}
