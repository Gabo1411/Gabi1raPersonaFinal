using UnityEngine;

public class BulletDamage : MonoBehaviour
{
    public int damage = 3;

    // Usamos OnTriggerEnter y Collider other
    private void OnTriggerEnter(Collider other)
    {

        if (other.GetComponent<BalaJefe>() != null) return;

        // Si choca con un enemigo normal (Cactus)
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            // --- NUEVO: También buscamos al Hongo ---
            EnemyMushroom hongo = other.GetComponent<EnemyMushroom>();
            if (hongo != null)
            {
                hongo.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
        else if (other.CompareTag("Boss"))
        {
            Boss boss = other.GetComponent<Boss>();
            if (boss != null)
            {
                boss.TakeDamage(damage);
            }
            Destroy(gameObject); // Destruir bala
        }
        // 2. Si choca con pared o suelo, solo destruirse
        else if (!other.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        PlayerMovement p = FindFirstObjectByType<PlayerMovement>();
        if (p != null) damage += p.bonusDanio;
    }
}