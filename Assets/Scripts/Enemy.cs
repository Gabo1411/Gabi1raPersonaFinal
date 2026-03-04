using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int health = 3;

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
        // Solo desaparece. No avisa al manager (porque ahora vamos por tiempo)
        Destroy(gameObject);
    }
}
