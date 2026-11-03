using UnityEngine;

public class BossArenaSpawner : MonoBehaviour
{
    [Header("Configuración")]
    public GameObject[] enemigosPrefabs; // Cactus y Hongo
    public float tiempoEntreSpawns = 6f;
    public float radioSpawn = 7f;
    public int limiteEnemigosEnArena = 10;

    private bool estaActivo = false;

    public void ActivarSpawner()
    {
        if (estaActivo) return;

        estaActivo = true;
        InvokeRepeating(nameof(SpawnAleatorio), 2f, tiempoEntreSpawns);
    }

    void SpawnAleatorio()
    {
        if (enemigosPrefabs == null || enemigosPrefabs.Length == 0) return;
        if (EnemyBase.AliveCount >= limiteEnemigosEnArena) return;

        GameObject prefab = enemigosPrefabs[Random.Range(0, enemigosPrefabs.Length)];

        Vector2 circulo = Random.insideUnitCircle * radioSpawn;
        Vector3 posFinal = new Vector3(transform.position.x + circulo.x, transform.position.y, transform.position.z + circulo.y);

        GameObject enemy = Instantiate(prefab, posFinal, Quaternion.identity);

        // Los enemigos de la arena mueren con sus propias partículas
        EnemyBase enemyBase = enemy.GetComponent<EnemyBase>();
        if (enemyBase != null) enemyBase.efectoMuerteArena = true;
    }

    public void DetenerSpawner()
    {
        CancelInvoke(nameof(SpawnAleatorio));
        estaActivo = false;
    }
}
