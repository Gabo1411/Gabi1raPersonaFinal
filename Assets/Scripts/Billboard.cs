using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera camaraPrincipal;

    void Start()
    {
        // Busca tu cámara automáticamente
        camaraPrincipal = Camera.main;
    }

    void LateUpdate()
    {
        if (camaraPrincipal == null) return;

        // Obliga al Canvas a mirar siempre hacia la cámara del jugador
        transform.LookAt(transform.position + camaraPrincipal.transform.rotation * Vector3.forward,
                         camaraPrincipal.transform.rotation * Vector3.up);
    }
}