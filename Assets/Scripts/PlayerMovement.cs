using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Necesario para modificar la UI

public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;

    public float speed = 12f;
    public float gravity = -9.81f * 2;
    public float jumpHeight = 3f;

    public int maxHealth = 50;
    public int Health;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    Vector3 velocity;
    bool isGrounded;
    bool isMoving;
    public bool IsMoving => isMoving;
    private Vector3 lastPosition = new Vector3(0f, 0f, 0f);

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 30f; // Asegúrate de darle un valor alto en el Inspector
    [SerializeField] private float dashTime = 0.2f; // Un tiempo corto, como 0.2 segundos
    [SerializeField] private TrailRenderer dashTrail;
    [SerializeField] private float dashCooldown = 1.5f; // Tiempo de espera entre dashes

    private bool isDashing;
    private float nextDashTime;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        lastPosition = transform.position;
        Health = maxHealth;

        if (dashTrail != null)
        {
            dashTrail.emitting = false; // Nos aseguramos de que el rastro empiece apagado
        }
    }

    void Update()
    {
        // 1. Si estamos haciendo un dash, salimos del Update para no aplicar movimiento normal ni gravedad
        if (isDashing)
        {
            return;
        }

        // 2. Detectar el input para iniciar el dash
        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= nextDashTime)
        {
            StartCoroutine(DashCoroutine());
        }

        // --- MOVIMIENTO NORMAL ---
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);

        // Check para saltar 
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Caer
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Comprobación de movimiento para animaciones/estado
        if (lastPosition != gameObject.transform.position && isGrounded == true)
        {
            isMoving = true;
        }
        else
        {
            isMoving = false;
        }
        lastPosition = gameObject.transform.position;

    }

    // --- LA LÓGICA DEL DASH ---
    private IEnumerator DashCoroutine()
    {
        isDashing = true; // Bloquea el Update normal
        nextDashTime = Time.time + dashCooldown; // Registra cuándo podremos volver a usarlo

        if (dashTrail != null) dashTrail.emitting = true; // Enciende la estela

        // Obtenemos la dirección actual de los inputs
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 dashDirection = (transform.right * x + transform.forward * z).normalized;

        // Si el jugador no está presionando nada, el dash será hacia adelante
        if (dashDirection == Vector3.zero)
        {
            dashDirection = transform.forward;
        }

        float startTime = Time.time;

        // Mientras no se acabe el tiempo del dash, nos movemos a gran velocidad
        while (Time.time < startTime + dashTime)
        {
            controller.Move(dashDirection * dashSpeed * Time.deltaTime);
            yield return null; // Espera al siguiente frame
        }

        // Apagamos todo al terminar
        if (dashTrail != null) dashTrail.emitting = false;
        isDashing = false;
    }

    public float GetDashCooldownProgress()
    {
        // Si ya pasó el tiempo, el dash está listo (100% o 1f)
        if (Time.time >= nextDashTime)
        {
            return 1f;
        }

        // Si está en enfriamiento, calculamos el porcentaje restante (de 0 a 1)
        float tiempoRestante = nextDashTime - Time.time;
        return 1f - (tiempoRestante / dashCooldown);
    }
}