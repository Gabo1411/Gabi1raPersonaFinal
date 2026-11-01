using UnityEngine;

public class BulletDamage : MonoBehaviour
{
    public int damage = 3;
    public float lifetime = 3f;

    private int baseDamage;
    private float timeLeft;
    private Rigidbody body;

    void Awake()
    {
        baseDamage = damage;
        body = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        timeLeft = lifetime;
    }

    void Update()
    {
        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f) PrefabPool.Release(gameObject);
    }

    // damageMultiplier escala el daño total (incluido el bonus de poción), p. ej. 0.8 con Disparo Continuo.
    public void Launch(Vector3 direction, float speed, float damageMultiplier = 1f)
    {
        // El bonus de la poción se aplica al disparar, no al instanciar el prefab.
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        float total = (baseDamage + (player != null ? player.bonusDanio : 0)) * damageMultiplier;

        // Redondeo probabilístico: el daño medio coincide con el valor exacto aunque sea fraccionario
        int rolled = Mathf.FloorToInt(total);
        if (Random.value < total - rolled) rolled++;
        damage = Mathf.Max(1, rolled);

        transform.forward = direction;
        if (body != null)
        {
            body.angularVelocity = Vector3.zero;
            body.linearVelocity = direction * speed;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<BalaJefe>() != null) return;
        if (other.CompareTag("Player")) return;

        if (other.CompareTag("Enemy") || other.CompareTag("Boss"))
        {
            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target != null) target.TakeDamage(damage);
        }

        PrefabPool.Release(gameObject);
    }
}
