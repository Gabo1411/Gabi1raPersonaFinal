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

    [Header("Agacharse (mantener Ctrl o C)")]
    [Tooltip("Altura de la cápsula del jugador al agacharse (de pie mide la altura original del CharacterController).")]
    [SerializeField] private float crouchHeight = 2.2f;
    [Tooltip("Multiplicador de la velocidad al agacharse (0.5 = la mitad).")]
    [SerializeField, Range(0.2f, 1f)] private float crouchSpeedFactor = 0.5f;
    [Tooltip("Cuánto baja la cámara además de lo que baja el cuerpo (el arma baja con el cuerpo, así que mejor dejarlo en 0).")]
    [SerializeField] private float crouchCameraDrop = 0f;

    // Controlados por la consola debug
    [HideInInspector] public bool godMode, noclip, fly;
    [HideInInspector] public float debugSpeedMultiplier = 1f;
    [HideInInspector] public int debugBonusDamage;

    private float standHeight, crouchAmount;
    private Transform cameraTransform;
    private Vector3 cameraBaseLocal;

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

    // El arma lo reduce mientras apunta en modo francotirador
    [HideInInspector] public float aimSlow = 1f;

    public float CurrentSpeed => (speed + BuffAmount(BuffType.Velocidad) + BuffAmount(BuffType.VelocidadMejorada)) * aimSlow * Mathf.Lerp(1f, crouchSpeedFactor, crouchAmount) * debugSpeedMultiplier;
    public bool IsCrouching => crouchAmount > 0.5f;

    // Ajustes de agacharse (consola debug)
    public float CrouchHeight { get => crouchHeight; set => crouchHeight = Mathf.Clamp(value, 0.8f, standHeight > 0f ? standHeight : 3.75f); }
    public float CrouchSpeedFactor { get => crouchSpeedFactor; set => crouchSpeedFactor = Mathf.Clamp(value, 0.1f, 1f); }
    public float StandHeight => standHeight;

    // Punto al que apuntan los ataques del jefe: el centro del cuerpo, esté de pie o agachado
    public Vector3 AimPoint => controller != null ? controller.bounds.center : transform.position;
    public bool IsDead => Health <= 0;
    public bool IsDashing => isDashing;

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

        standHeight = controller.height;
        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            cameraTransform = cam.transform;
            cameraBaseLocal = cameraTransform.localPosition;
        }

        if (dashTrail != null) dashTrail.emitting = false;
    }

    void Update()
    {
        UpdateBuffs();
        UpdateCrouch();

        // Si ya no está en noclip, la cápsula vuelve a activarse
        if (!noclip && controller != null && !controller.enabled && !isDashing) controller.enabled = true;

        if (isDashing || Keyboard.current == null) return;

        if (noclip || fly)
        {
            FlyUpdate();
            return;
        }

        bool blocked = DebugConsole.IsOpen;

        if (!blocked && Keyboard.current.leftShiftKey.wasPressedThisFrame && Time.time >= lastDashTime + CurrentDashCooldown)
        {
            StartCoroutine(DashCoroutine());
        }

        isGrounded = Physics.CheckSphere(GroundCheckPosition, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0) velocity.y = -2f;

        Vector2 input = ReadMoveInput();
        Vector3 move = Flatten(transform.right) * input.x + Flatten(transform.forward) * input.y;
        controller.Move(move * CurrentSpeed * Time.deltaTime);

        if (!blocked && Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded && crouchAmount < 0.3f)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }


    // ---------- Consola debug ----------

    public void DebugTeleport(Vector3 position)
    {
        bool wasEnabled = controller != null && controller.enabled;
        if (controller != null) controller.enabled = false;
        transform.position = position;
        velocity = Vector3.zero;
        if (controller != null) controller.enabled = wasEnabled || !noclip;
    }

    public void DebugClearBuffs()
    {
        foreach (Buff buff in buffs) buff.remaining = 0f;
        regenBuffer = 0f;
    }

    // Vuelo libre (consola debug): sigue la dirección de la cámara; Espacio sube y Ctrl baja.
    // Con noclip además atraviesa paredes y suelo.
    void FlyUpdate()
    {
        if (controller == null) return;

        velocity = Vector3.zero;
        controller.enabled = !noclip;

        Vector2 input = ReadMoveInput();
        Vector3 direction = transform.right * input.x + transform.forward * input.y;

        Keyboard kb = Keyboard.current;
        if (!DebugConsole.IsOpen)
        {
            if (kb.spaceKey.isPressed) direction += Vector3.up;
            if (kb.leftCtrlKey.isPressed) direction += Vector3.down;
        }

        Vector3 move = direction * (CurrentSpeed * 1.6f * Time.deltaTime);
        if (noclip) transform.position += move;
        else controller.Move(move);
    }

    // La cámara se inclina al mirar arriba o abajo y arrastra a todo el jugador. Para el suelo y el movimiento solo
    // cuenta el giro horizontal, así se puede saltar y avanzar a toda velocidad mirando en cualquier dirección.
    Quaternion YawRotation => Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

    // Al agacharse la cápsula se encoge alrededor de su centro, así que el punto de suelo sube con ella
    Vector3 GroundCheckPosition => transform.position + YawRotation * groundCheck.localPosition + Vector3.up * ((standHeight - controller.height) * 0.5f);

    // Agacharse: la cápsula y la cámara bajan suavemente. Al soltar solo se levanta si hay espacio sobre la cabeza.
    void UpdateCrouch()
    {
        if (controller == null) return;

        Keyboard kb = Keyboard.current;
        bool wants = kb != null && (kb.leftCtrlKey.isPressed || kb.cKey.isPressed) && !IsDead && !DebugConsole.IsOpen && !noclip && !fly;
        if (!wants && crouchAmount > 0f && !HasHeadroom()) wants = true;

        crouchAmount = Mathf.MoveTowards(crouchAmount, wants ? 1f : 0f, Time.deltaTime * 6f);

        float height = Mathf.Lerp(standHeight, crouchHeight, Mathf.SmoothStep(0f, 1f, crouchAmount));
        float delta = height - controller.height;
        if (Mathf.Abs(delta) > 0.0001f)
        {
            // Los pies se quedan en el mismo sitio: al crecer se sube primero el centro, al encoger se baja después
            if (delta > 0f) { controller.Move(Vector3.up * (delta * 0.5f)); controller.height = height; }
            else { controller.height = height; controller.Move(Vector3.up * (delta * 0.5f)); }
        }

        if (cameraTransform != null)
        {
            cameraTransform.localPosition = cameraBaseLocal + Vector3.down * (crouchCameraDrop * Mathf.SmoothStep(0f, 1f, crouchAmount));
        }
    }

    bool HasHeadroom()
    {
        float radius = controller.radius * 0.9f;
        float feetY = transform.position.y - controller.height * 0.5f;
        Vector3 head = new Vector3(transform.position.x, feetY + standHeight - radius, transform.position.z);

        foreach (Collider other in Physics.OverlapSphere(head, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (other.transform.IsChildOf(transform) || other.transform == transform) continue;
            return false;
        }
        return true;
    }

    static Vector3 Flatten(Vector3 direction)
    {
        direction.y = 0f;
        return direction.normalized;
    }

    static Vector2 ReadMoveInput()
    {
        Keyboard kb = Keyboard.current;
        if (DebugConsole.IsOpen) return Vector2.zero;
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
        if (IsDead || amount <= 0 || godMode) return;

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

        bonusDanio = Mathf.RoundToInt(BuffAmount(BuffType.Danio) + BuffAmount(BuffType.DanioMejorado)) + debugBonusDamage;

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
