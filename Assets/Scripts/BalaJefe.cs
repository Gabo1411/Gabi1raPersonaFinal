using UnityEngine;

public class BalaJefe : MonoBehaviour
{
    public float velocidad = 20f;
    public int damage = 10;
    public float tiempoDeVida = 4f;

    private float timeLeft;
    private Rigidbody body;

    void Awake()
    {
        // La bala se mueve por Translate: la gravedad del Rigidbody solo la desviaría hacia abajo
        body = GetComponent<Rigidbody>();
        if (body != null) body.useGravity = false;
    }

    void OnEnable()
    {
        timeLeft = tiempoDeVida;

        // Al reutilizar la bala desde el pool no debe conservar la velocidad de su uso anterior
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    void Update()
    {
        transform.Translate(Vector3.forward * velocidad * Time.deltaTime);

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f) PrefabPool.Release(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Las balas del jugador no la detienen
        if (other.GetComponent<BulletDamage>() != null) return;

        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null) player.TakeDamage(damage);
        }

        if (!other.CompareTag("Boss")) PrefabPool.Release(gameObject);
    }
}
