using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemigoPrefab;
    public float tiempoEntreSpawns = 3f;
    public float radioSpawn = 5f;

    void Start()
    {
        InvokeRepeating("Spawn", 0f, tiempoEntreSpawns);
    }

    void Spawn()
    {
        // Usamos un círculo 2D para calcular solo X y Z
        Vector2 circuloAleatorio = Random.insideUnitCircle * radioSpawn;

        Vector3 lugarFinal = new Vector3(
            transform.position.x + circuloAleatorio.x,
            transform.position.y, // AHORA USA LA ALTURA DEL SPAWNER
            transform.position.z + circuloAleatorio.y
        );

        Instantiate(enemigoPrefab, lugarFinal, Quaternion.identity);
    }
}