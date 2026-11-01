using UnityEngine;
using UnityEngine.InputSystem;

public class MouseMovement : MonoBehaviour
{
    // Con 500 equivale a ~0.83 grados por píxel de movimiento del mouse, multiplicado por la sensibilidad de Opciones.
    public float mouseSensitivity = 500f;

    const float DegreesPerPixelFactor = 0.1f / 60f;

    float xRotation = 0f;
    float yRotation = 0f;
    float userSensitivity = 1f;

    void OnEnable()
    {
        userSensitivity = GameSettings.Sensitivity;
        GameSettings.Changed += OnSettingsChanged;
    }

    void OnDisable()
    {
        GameSettings.Changed -= OnSettingsChanged;
    }

    void OnSettingsChanged() => userSensitivity = GameSettings.Sensitivity;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // No girar la cámara con el juego en pausa o en pantallas de victoria/derrota
        if (Time.timeScale == 0f || Mouse.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue() * (mouseSensitivity * DegreesPerPixelFactor * userSensitivity);

        xRotation = Mathf.Clamp(xRotation - delta.y, -90f, 90f);
        yRotation += delta.x;

        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}
