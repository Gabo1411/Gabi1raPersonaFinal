using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour, IDamageable
{
    private CharacterController controller;

    [Header("Estadísticas")]
    public float speed = 12f;
    public float gravity = -9.81f * 2;
    public float jumpHeight = 3f;
    public int maxHealth = 50;
    public int Health;
    public int bonusDanio = 0;

    [Header("Sonidos")]
    public AudioSource fuenteDash;
    public AudioClip clipDash;

    [Header("Detección de Suelo")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("Configuración Dash")]
    [SerializeField] private float dashSpeed = 30f;
    [SerializeField] private float dashTime = 0.2f;
    [SerializeField] private TrailRenderer dashTrail;
    [SerializeField] private float dashCooldown = 1.5f;

    [Header("Dash con Poción de Velocidad II")]
    [Tooltip("Multiplicador del cooldown del dash mientras dura la Poción de Velocidad II (0.5 = la mitad).")]
    [SerializeField] private float cooldownDashMejorado = 0.5f;

    Vector3 velocity;
    bool isGrounded;
    private bool isDashing;
    private float lastDashTime = -999f;

    // Buffs temporales de pociones. Los comunes renuevan su temporizador al repetirse;
    // los mejorados no se pueden acumular mientras sigan activos.
    class Buff
    {
        public float amount;
        public float remaining;
        public float duration;
        public bool Active => remaining > 0f;
    }

    readonly Buff[] buffs = CreateBuffs();
    float regenBuffer;

    static Buff[] CreateBuffs()
    {
        Buff[] list = new Buff[System.Enum.GetValues(typeof(BuffType)).Length];
        for (int i = 0; i < list.Length; i++) list[i] = new Buff();
        return list;
    }

    public float CurrentSpeed => speed + BuffAmount(BuffType.Velocidad) + BuffAmount(BuffType.VelocidadMejorada);
    public bool IsDead => Health <= 0;

    // Para mostrar los buffs activos en la interfaz
    public bool IsBuffActive(BuffType type) => buffs[(int)type].Active;
    public float GetBuffRemaining(BuffType type) => buffs[(int)type].remaining;
    public float GetBuffDuration(BuffType type) => buffs[(int)type].duration;

    float BuffAmount(BuffType type)
    {
        Buff buff = buffs[(int)type];
        return buff.Active ? buff.amount : 0f;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Health = maxHealth;

        if (dashTrail != null) dashTrail.emitting = false;
    }

    void Update()
    {
        UpdateBuffs();

        if (isDashing || Keyboard.current == null) return;

        if (Keyboard.current.leftShiftKey.wasPressedThisFrame && Time.time >= lastDashTime + CurrentDashCooldown)
        {
            StartCoroutine(DashCoroutine());
        }

        isGrounded = Physics.CheckSphere(GroundCheckPosition, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0) velocity.y = -2f;

        Vector2 input = ReadMoveInput();
        Vector3 move = Flatten(transform.right) * input.x + Flatten(transform.forward) * input.y;
        controller.Move(move * CurrentSpeed * Time.deltaTime);

        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // La cámara se inclina al mirar arriba o abajo y arrastra a todo el jugador. Para el suelo y el movimiento solo
    // cuenta el giro horizontal, así se puede saltar y avanzar a toda velocidad mirando en cualquier dirección.
    Quaternion YawRotation => Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

    Vector3 GroundCheckPosition => transform.position + YawRotation * groundCheck.localPosition;

    static Vector3 Flatten(Vector3 direction)
    {
        direction.y = 0f;
        return direction.normalized;
    }

    static Vector2 ReadMoveInput()
    {
        Keyboard kb = Keyboard.current;
        float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
        float z = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
        Vector2 v = new Vector2(x, z);
        return v.sqrMagnitude > 1f ? v.normalized : v;
    }

    private IEnumerator DashCoroutine()
    {
        isDashing = true;
        lastDashTime = Time.time;

        if (fuenteDash != null && clipDash != null)
        {
            fuenteDash.PlayOneShot(clipDash);
        }

        if (dashTrail != null) dashTrail.emitting = true;

        Vector2 input = ReadMoveInput();
        Vector3 dashDirection = (Flatten(transform.right) * input.x + Flatten(transform.forward) * input.y).normalized;

        if (dashDirection == Vector3.zero) dashDirection = Flatten(transform.forward);

        float startTime = Time.time;
        while (Time.time < startTime + dashTime)
        {
            controller.Move(dashDirection * dashSpeed * Time.deltaTime);
            yield return null;
        }

        if (dashTrail != null) dashTrail.emitting = false;
        isDashing = false;
    }

    float CurrentDashCooldown => IsBuffActive(BuffType.VelocidadMejorada) ? dashCooldown * cooldownDashMejorado : dashCooldown;

    public float GetDashCooldownProgress()
    {
        float elapsed = Time.time - lastDashTime;
        return Mathf.Clamp01(elapsed / CurrentDashCooldown);
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        Health = Mathf.Max(0, Health - amount);
        DamageFeedback.Show(Mathf.Clamp01(amount / (float)maxHealth * 2f));

        if (IsDead && GameManager.Instance != null) GameManager.Instance.PlayerDied();
    }

    public void Heal(int amount, bool showFeedback = true)
    {
        Health = Mathf.Min(Health + amount, maxHealth);
        if (showFeedback) DamageFeedback.ShowHeal();
    }

    // Poción común: si ya estaba activo se renueva el valor y el temporizador.
    public void ApplyBuff(BuffType type, float amount, float duration)
    {
        SetBuff(buffs[(int)type], amount, duration);
    }

    // Poción mejorada: no se acumula. Devuelve false (y no hace nada) si ese efecto sigue activo.
    public bool TryApplyEliteBuff(BuffType type, float amount, float duration)
    {
        Buff buff = buffs[(int)type];
        if (buff.Active) return false;

        SetBuff(buff, amount, duration);
        if (type == BuffType.Regeneracion)
        {
            regenBuffer = 0f;
            DamageFeedback.ShowHeal();
        }
        return true;
    }

    static void SetBuff(Buff buff, float amount, float duration)
    {
        buff.amount = amount;
        buff.duration = duration;
        buff.remaining = duration;
    }

    void UpdateBuffs()
    {
        foreach (Buff buff in buffs)
        {
            if (buff.Active) buff.remaining = Mathf.Max(0f, buff.remaining - Time.deltaTime);
        }

        bonusDanio = Mathf.RoundToInt(BuffAmount(BuffType.Danio) + BuffAmount(BuffType.DanioMejorado));

        // Regeneración: cura gradualmente (amount = puntos de vida por segundo)
        Buff regen = buffs[(int)BuffType.Regeneracion];
        if (regen.Active && !IsDead && Health < maxHealth)
        {
            regenBuffer += regen.amount * Time.deltaTime;
            int whole = Mathf.FloorToInt(regenBuffer);
            if (whole > 0)
            {
                regenBuffer -= whole;
                Heal(whole, false);
            }
        }
    }
}

public enum BuffType
{
    Velocidad,
    Danio,
    VelocidadMejorada,
    DanioMejorado,
    Regeneracion,
    DisparoContinuo
}
