using UnityEngine;

public class LootTable : MonoBehaviour
{
    [Header("Posibles Premios")]
    public GameObject[] pociones; // Arrastra aquí tus 3 prefabs de pociones

    [Range(0, 100)]
    public float probabilidadDrop = 20f; // 20% de probabilidad

    public void IntentarSoltarBotin()
    {
        if (Random.Range(0, 100) <= probabilidadDrop)
        {
            int indiceAleatorio = Random.Range(0, pociones.Length);

            // CAMBIO: En lugar de sumar altura (+ Vector3.up), restamos (- Vector3.up)
            // o multiplicamos por un valor como 0.5f para ajustarlo al suelo real.
            Vector3 posicionSuelo = transform.position - (Vector3.up * 0.5f);

            Instantiate(pociones[indiceAleatorio], posicionSuelo, Quaternion.identity);
        }
    }
}