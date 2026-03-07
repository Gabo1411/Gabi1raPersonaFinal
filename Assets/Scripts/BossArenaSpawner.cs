using UnityEngine;

public class BossArenaSpawner : MonoBehaviour
{
    [Header("Configuración")]
    public GameObject enemigoDistraccionPrefab; // Pon tu nuevo prefab de 0 puntos aquí
    public float tiempoEntreSpawns = 8f; // Un tiempo mucho más alto que arriba
    public float radioSpawn = 5f;

    private bool estaActivo = false;

    // Esta función la llamaremos cuando el jugador caiga por la escotilla
    public void ActivarSpawner()
    {
        if (!estaActivo)
        {
            estaActivo = true;
            InvokeRepeating("Spawn", 2f, tiempoEntreSpawns); // Empieza tras 2 segundos de caer
        }
    }

    public void DetenerSpawner()
    {
        CancelInvoke("Spawn");
        estaActivo = false;
    }

    void Spawn()
    {
        Vector2 circuloAleatorio = Random.insideUnitCircle * radioSpawn;

        Vector3 lugarFinal = new Vector3(
            transform.position.x + circuloAleatorio.x,
            transform.position.y,
            transform.position.z + circuloAleatorio.y
        );

        Instantiate(enemigoDistraccionPrefab, lugarFinal, Quaternion.identity);
    }
}