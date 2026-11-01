using UnityEngine;

public class LootTable : MonoBehaviour
{
    [Header("Posibles Premios")]
    public GameObject[] pociones;

    [Range(0, 100)]
    public float probabilidadDrop = 20f;

    [Range(0, 100)]
    [Tooltip("Probabilidad (en lugar del botín normal) de soltar la poción de Disparo Continuo.")]
    public float probabilidadDisparoContinuo = 0f;

    [Tooltip("Las pociones que suelta son la versión mejorada (aura eléctrica amarilla, efectos más potentes).")]
    public bool mejoradas;

    [Header("Prefabs especiales")]
    [Tooltip("Versiones mejoradas de las pociones (se usan si 'mejoradas' está activado).")]
    public GameObject[] pocionesMejoradas;
    public GameObject pocionDisparoContinuo;

    public void IntentarSoltarBotin()
    {
        if (pociones == null || pociones.Length == 0) return;

        Vector3 posicionSuelo = BuscarSuelo();

        if (Random.Range(0f, 100f) < probabilidadDisparoContinuo && pocionDisparoContinuo != null)
        {
            Instantiate(pocionDisparoContinuo, posicionSuelo, Quaternion.identity);
            return;
        }

        if (Random.Range(0f, 100f) > probabilidadDrop) return;

        GameObject[] opciones = mejoradas && pocionesMejoradas != null && pocionesMejoradas.Length > 0 ? pocionesMejoradas : pociones;
        Instantiate(opciones[Random.Range(0, opciones.Length)], posicionSuelo, Quaternion.identity);
    }

    // Cada enemigo tiene el pivote a una altura distinta (el cactus flota 0,5 m sobre el suelo, el hongo está en el suelo),
    // así que se busca el suelo real bajo él para que la poción nunca quede enterrada.
    Vector3 BuscarSuelo()
    {
        Vector3 origen = transform.position + Vector3.up * 0.5f;
        float mejorDistancia = float.MaxValue;
        Vector3 suelo = transform.position - (Vector3.up * 0.5f);

        foreach (RaycastHit hit in Physics.RaycastAll(origen, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore))
        {
            // Se ignoran el propio enemigo, otros enemigos y al jugador
            if (hit.transform.IsChildOf(transform)) continue;
            if (hit.collider.GetComponentInParent<EnemyBase>() != null) continue;
            if (hit.collider.GetComponentInParent<PlayerMovement>() != null) continue;

            if (hit.distance < mejorDistancia)
            {
                mejorDistancia = hit.distance;
                suelo = hit.point;
            }
        }
        return suelo;
    }
}
