using UnityEngine;
using UnityEngine.AI;

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

        Vector3 posicionSuelo = AlejarDelJefe(BuscarSuelo());

        if (Random.Range(0f, 100f) < probabilidadDisparoContinuo && pocionDisparoContinuo != null)
        {
            Instantiate(pocionDisparoContinuo, posicionSuelo, Quaternion.identity);
            return;
        }

        if (Random.Range(0f, 100f) > probabilidadDrop) return;

        GameObject[] opciones = mejoradas && pocionesMejoradas != null && pocionesMejoradas.Length > 0 ? pocionesMejoradas : pociones;
        Instantiate(opciones[Random.Range(0, opciones.Length)], posicionSuelo, Quaternion.identity);
    }

    // Las pociones no pueden aparecer pegadas al jefe: si el punto cae demasiado cerca, se busca el punto de suelo
    // válido más cercano fuera de ese radio.
    Vector3 AlejarDelJefe(Vector3 punto)
    {
        Boss jefe = FindFirstObjectByType<Boss>();
        if (jefe == null) return punto;

        Bounds limites = new Bounds(jefe.transform.position, Vector3.zero);
        foreach (Renderer rend in jefe.GetComponentsInChildren<Renderer>())
        {
            if (rend is ParticleSystemRenderer) continue;
            limites.Encapsulate(rend.bounds);
        }

        Vector3 centro = limites.center;
        float radio = Mathf.Max(limites.extents.x, limites.extents.z) + 3f;

        Vector3 plano = new Vector3(punto.x - centro.x, 0f, punto.z - centro.z);
        if (plano.magnitude >= radio) return punto;

        Vector3 mejor = punto;
        float mejorDistancia = float.MaxValue;
        bool encontrado = false;

        // Se prueban varios radios y direcciones; gana el candidato válido más cercano al punto original
        for (float r = radio; r <= radio + 6f && !encontrado; r += 1.5f)
        {
            for (int i = 0; i < 24; i++)
            {
                float angulo = i * Mathf.PI * 2f / 24f;
                Vector3 candidato = new Vector3(centro.x + Mathf.Cos(angulo) * r, punto.y, centro.z + Mathf.Sin(angulo) * r);

                if (!NavMesh.SamplePosition(candidato, out NavMeshHit navegable, 2f, NavMesh.AllAreas)) continue;
                if (new Vector2(navegable.position.x - centro.x, navegable.position.z - centro.z).magnitude < radio - 0.5f) continue;

                float d = (navegable.position - punto).sqrMagnitude;
                if (d < mejorDistancia)
                {
                    mejorDistancia = d;
                    mejor = navegable.position;
                    encontrado = true;
                }
            }
        }
        return mejor;
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
