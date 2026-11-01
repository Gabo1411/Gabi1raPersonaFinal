using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemigoPrefab;
    public float tiempoEntreSpawns = 3f;
    public float radioSpawn = 5f;

    void Start()
    {
        InvokeRepeating(nameof(Spawn), 0f, tiempoEntreSpawns);
    }

    void Spawn()
    {
        // Círculo 2D para calcular solo X y Z; la altura es la del spawner
        Vector2 circuloAleatorio = Random.insideUnitCircle * radioSpawn;

        Vector3 lugarFinal = new Vector3(
            transform.position.x + circuloAleatorio.x,
            transform.position.y,
            transform.position.z + circuloAleatorio.y
        );

        Instantiate(enemigoPrefab, lugarFinal, Quaternion.identity);
    }

    public void StopSpawning()
    {
        CancelInvoke(nameof(Spawn));
    }
}
