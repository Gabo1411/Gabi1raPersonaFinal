using UnityEngine;

public class BalaJefe : MonoBehaviour
{
    public float velocidad = 20f;
    public int damage = 10;
    public float tiempoDeVida = 4f; // Se destruye sola a los 4 segundos

    void Start()
    {
        Destroy(gameObject, tiempoDeVida);
    }

    void Update()
    {
        // Mueve la bala hacia adelante constantemente
        transform.Translate(Vector3.forward * velocidad * Time.deltaTime);
    }

    // Usamos OnTriggerEnter y Collider other
    private void OnTriggerEnter(Collider other)
    {
        // Si tocamos la bala del jugador, la ignoramos y cortamos la función
        if (other.GetComponent<BulletDamage>() != null) return;

        // 1. Si la bala choca con el jugador...
        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null)
            {
                player.Health -= damage;
                Debug.Log("¡El jefe te golpeó! Vida restante: " + player.Health);
            }
            if (player.Health <= 0)
            {
            player.Health = 0;
            // Avisamos al GameManager que el jefe nos ganó
            FindFirstObjectByType<GameManager>()?.PlayerDied();
            }
        }
        
        

        // 2. Al chocar con cualquier cosa que NO sea el jefe, se destruye
        if (!other.CompareTag("Boss"))
        {
            Destroy(gameObject);
        }
    }
}