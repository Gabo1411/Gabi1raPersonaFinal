using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
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
    public AudioClip sonidoDash;
    private AudioSource audioSource;

    [Header("Sonidos")]
    public AudioSource fuenteDash; // Arrastra el AudioSource del Player aquí
    public AudioClip clipDash;

    [Header("Detección de Suelo")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    Vector3 velocity;
    bool isGrounded;
    private Vector3 lastPosition;

    [Header("Configuración Dash")]
    [SerializeField] private float dashSpeed = 30f;
    [SerializeField] private float dashTime = 0.2f;
    [SerializeField] private TrailRenderer dashTrail;
    [SerializeField] private float dashCooldown = 1.5f;

    private bool isDashing;
    private float nextDashTime;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        lastPosition = transform.position;
        Health = maxHealth;
        audioSource = GetComponent<AudioSource>();

        if (dashTrail != null) dashTrail.emitting = false;
    }

    void Update()
    {
        if (isDashing) return;

        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= nextDashTime)
        {
            StartCoroutine(DashCoroutine());
        }

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0) velocity.y = -2f;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
        lastPosition = transform.position;
    }

    private IEnumerator DashCoroutine()
    {
        isDashing = true;
        nextDashTime = Time.time + dashCooldown;

        if (fuenteDash != null && clipDash != null)
        {
            fuenteDash.PlayOneShot(clipDash);
        }

        if (dashTrail != null) dashTrail.emitting = true;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 dashDirection = (transform.right * x + transform.forward * z).normalized;

        if (dashDirection == Vector3.zero) dashDirection = transform.forward;

        float startTime = Time.time;
        while (Time.time < startTime + dashTime)
        {
            controller.Move(dashDirection * dashSpeed * Time.deltaTime);
            yield return null;
        }

        if (dashTrail != null) dashTrail.emitting = false;
        isDashing = false;
    }

    public float GetDashCooldownProgress()
    {
        if (Time.time >= nextDashTime) return 1f;
        return 1f - ((nextDashTime - Time.time) / dashCooldown);
    }
}