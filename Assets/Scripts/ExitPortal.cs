using UnityEngine;

public class ExitPortal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Verificamos si el que tocó el portal es el jugador
        if (other.CompareTag("Player"))
        {
            GameManager manager = FindFirstObjectByType<GameManager>();
            if (manager != null)
            {
                manager.WinGame(); // Llamamos a la victoria
            }

            Debug.Log("¡El jugador ha escapado por el portal!");
        }
    }
}