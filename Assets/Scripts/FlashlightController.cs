using UnityEngine;
using UnityEngine.InputSystem;

// Linterna (tecla F): foco con sombras suaves y borde difuso, que se enciende y apaga con un fundido,
// sigue la cámara con un leve retraso (como una linterna en la mano) y de vez en cuando titila.
public class FlashlightController : MonoBehaviour
{
    public GameObject lightSource;

    [Header("Ajustes")]
    [Tooltip("Duración del fundido al encender y apagar (segundos).")]
    public float fadeTime = 0.14f;
    [Tooltip("Qué tan rápido alcanza la linterna la dirección de la cámara (más alto = menos retraso).")]
    public float followSpeed = 13f;
    [Tooltip("Cada cuántos segundos (mín, máx) titila un instante.")]
    public Vector2 flickerEvery = new Vector2(9f, 20f);

    private bool isOn = true;

    public bool IsOn { get => isOn; set => isOn = value; }
    private Light spot;
    private float baseIntensity, level, nextFlicker, flickerEnd;
    private Quaternion localRotation;
    private bool wasActive;

    static Texture2D cookie;

    void Start()
    {
        if (lightSource == null) return;

        spot = lightSource.GetComponent<Light>();
        localRotation = lightSource.transform.localRotation;
        level = isOn ? 1f : 0f;
        nextFlicker = Time.time + Random.Range(flickerEvery.x, flickerEvery.y);

        if (spot != null)
        {
            baseIntensity = spot.intensity;
            spot.shadows = LightShadows.Soft;
            spot.shadowStrength = 0.9f;
            spot.shadowBias = 0.05f;
            spot.shadowNormalBias = 0.4f;
            spot.cookie = Cookie;
        }
        wasActive = lightSource.activeSelf;
    }

    void Update()
    {
        if (lightSource == null || spot == null) return;

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame && !DebugConsole.IsOpen) isOn = !isOn;

        // Con el juego en pausa el tiempo no avanza, así que el fundido usa el tiempo real
        float dt = Time.timeScale > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
        level = Mathf.MoveTowards(level, isOn ? 1f : 0f, dt / Mathf.Max(0.01f, fadeTime));

        bool active = level > 0.001f;
        if (active != lightSource.activeSelf) lightSource.SetActive(active);
        if (!active) { wasActive = false; return; }

        // Al encender, la linterna aparece ya apuntando a donde mira la cámara
        if (!wasActive) SnapToCamera();
        wasActive = true;

        // Parpadeo suave continuo y, de vez en cuando, un titileo breve como de batería floja
        float steady = 0.97f + 0.03f * Mathf.PerlinNoise(Time.time * 3f, 0.3f);
        if (Time.time >= nextFlicker)
        {
            flickerEnd = Time.time + Random.Range(0.18f, 0.35f);
            nextFlicker = Time.time + Random.Range(flickerEvery.x, flickerEvery.y);
        }
        float flicker = Time.time < flickerEnd ? Mathf.Lerp(0.25f, 1f, Mathf.PerlinNoise(Time.time * 45f, 7.1f)) : 1f;

        spot.intensity = baseIntensity * Mathf.SmoothStep(0f, 1f, level) * steady * flicker;
    }

    // El haz se mueve con un pequeño retraso respecto a la cámara
    void LateUpdate()
    {
        if (lightSource == null || spot == null || !lightSource.activeSelf) return;

        Transform t = lightSource.transform;
        Quaternion target = t.parent != null ? t.parent.rotation * localRotation : localRotation;
        t.rotation = Quaternion.Slerp(t.rotation, target, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
    }

    void SnapToCamera()
    {
        Transform t = lightSource.transform;
        t.rotation = t.parent != null ? t.parent.rotation * localRotation : localRotation;
    }

    // Máscara del haz: centro lleno y borde que se desvanece poco a poco, con una leve irregularidad
    static Texture2D Cookie
    {
        get
        {
            if (cookie != null) return cookie;

            const int N = 256;
            cookie = new Texture2D(N, N, TextureFormat.RGBA32, true);
            cookie.name = "CookieLinterna";
            cookie.hideFlags = HideFlags.HideAndDontSave;
            cookie.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixels = new Color32[N * N];
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float edge = 1f - Mathf.SmoothStep(0.45f, 0.98f, r);
                    float grain = 0.93f + 0.07f * Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                    byte v = (byte)(Mathf.Clamp01(edge * grain) * 255f);
                    pixels[y * N + x] = new Color32(v, v, v, v);
                }
            }
            cookie.SetPixels32(pixels);
            cookie.Apply();
            return cookie;
        }
    }
}
