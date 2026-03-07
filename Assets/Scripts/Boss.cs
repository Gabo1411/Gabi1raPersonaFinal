using UnityEngine;

public class Boss : MonoBehaviour
{
    [Header("Estadísticas")]
    public int health = 100; // Mucha más vida que un enemigo normal

    [Header("Efectos")]
    public GameObject particulasMuerte;

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
        // 1. Generamos las partículas épicas
        if (particulasMuerte != null)
        {
            Instantiate(particulasMuerte, transform.position, Quaternion.identity);
        }

        // 2. Le avisamos al GameManager que ganamos
        GameManager manager = FindFirstObjectByType<GameManager>();
        if (manager != null)
        {
            manager.BossDefeated(); // Llamamos a una nueva función
        }

        // 3. El jefe desaparece
        Destroy(gameObject);
    }
}