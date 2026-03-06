using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int health = 15;
    public int pointsValue = 10;

    [Header("Efectos Visuales")]
    public GameObject particulasMuerte; // Arrastra tu prefab de partículas aquí

    public void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            Die();
        }
    }

    // Muerte normal por daño del jugador (Da puntos)
    void Die()
    {
        GenerarParticulas();

        GameManager manager = FindFirstObjectByType<GameManager>();
        if (manager != null)
        {
            manager.AddScore(pointsValue);
        }

        Destroy(gameObject);
    }

    // Muerte por limpieza de mapa (NO da puntos, pero sí hace el efecto)
    public void ClearFromMap()
    {
        GenerarParticulas();
        Destroy(gameObject);
    }

    // Instancia las partículas en el lugar exacto del enemigo
    private void GenerarParticulas()
    {
        if (particulasMuerte != null)
        {
            Instantiate(particulasMuerte, transform.position, Quaternion.identity);
        }
    }
}