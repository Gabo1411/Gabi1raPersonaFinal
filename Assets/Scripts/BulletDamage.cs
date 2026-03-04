using UnityEngine;

public class BulletDamage : MonoBehaviour
{
    public int damage = 1;

    private void OnCollisionEnter(Collision collision)
    {
        // 1. Si choca con enemigo, hacer daño
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            Destroy(gameObject); // Destruir bala
        }
        // 2. Si choca con pared o suelo, solo destruirse
        else if (!collision.gameObject.CompareTag("Player"))
        {
            // El !Tag("Player") evita que la bala se destruya al salir del arma si choca con el propio jugador por error
            Destroy(gameObject);
        }
    }
}