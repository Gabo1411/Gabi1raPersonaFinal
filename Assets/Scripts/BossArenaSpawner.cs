using UnityEngine;

public class BossArenaSpawner : MonoBehaviour
{
    [Header("Configuración")]
    public GameObject[] enemigosPrefabs; // Pon aquí al Cactus y al Hongo
    public float tiempoEntreSpawns = 6f;
    public float radioSpawn = 7f;
    public int limiteEnemigosEnArena = 10; // ¡EL LÍMITE QUE PEDISTE!

    private bool estaActivo = false;

    public void ActivarSpawner()
    {
        if (!estaActivo)
        {
            estaActivo = true;
            InvokeRepeating("SpawnAleatorio", 2f, tiempoEntreSpawns);
        }
    }

    void SpawnAleatorio()
    {
        // Contamos cuántos objetos con el Tag "Enemy" hay actualmente
        int enemigosActuales = GameObject.FindGameObjectsWithTag("Enemy").Length;

        if (enemigosActuales < limiteEnemigosEnArena)
        {
            int indice = Random.Range(0, enemigosPrefabs.Length);

            Vector2 circulo = Random.insideUnitCircle * radioSpawn;
            Vector3 posFinal = new Vector3(transform.position.x + circulo.x, transform.position.y, transform.position.z + circulo.y);

            Instantiate(enemigosPrefabs[indice], posFinal, Quaternion.identity);
        }
    }

    public void DetenerSpawner()
    {
        CancelInvoke("SpawnAleatorio");
        estaActivo = false;
    }
}