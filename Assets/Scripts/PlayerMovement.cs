using UnityEngine;

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
    public bool IsMoving => isMoving; // Expose read-only property so the field is read and the compiler warning disappears
    private Vector3 lastPosition = new Vector3(0f, 0f, 0f);
    void Start()
    {
        controller = GetComponent<CharacterController>();
        // Initialize lastPosition to current position to avoid a false positive movement on the first frame
        lastPosition = transform.position;

        Health = maxHealth;

    }


    void Update()
    {
        //Verificar si el personaje esta en el suelo
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        //Calcular la direccion del movimiento
        Vector3 move = transform.right * x + transform.forward * z;

        //Mover el personaje
        controller.Move(move * speed * Time.deltaTime);

        //Check para saltar 
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            //Saltar
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        //Caer
        velocity.y += gravity * Time.deltaTime;

        //Ejecutar el salto 
        controller.Move(velocity * Time.deltaTime);
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
}
