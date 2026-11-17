using UnityEngine;

// Luz de antorcha con vida: la intensidad y el color varían de forma suave y orgánica (con ráfagas ocasionales),
// la luz se mueve un poco para que las sombras bailen, y alrededor de la llama hay un halo y unas brasas que suben.
public class TorchFlicker : MonoBehaviour
{
    [Header("Parpadeo")]
    [Tooltip("Cuánto varía la intensidad (0.2 = ±20 %).")]
    public float amplitude = 0.2f;
    [Tooltip("Cuánto se desplaza el color hacia naranja al bajar la intensidad.")]
    public float colorShift = 0.5f;
    [Tooltip("Desplazamiento aleatorio de la luz en metros (hace bailar las sombras). 0 para luces que se mueven solas.")]
    public float jitter = 0.035f;

    [Header("Rendimiento")]
    [Tooltip("Quita las sombras de esta luz: cada luz puntual con sombras dibuja la escena hasta 6 veces más. Una antorcha pequeña casi no las necesita.")]
    public bool disableShadows = true;

    [Header("Efectos")]
    public bool halo = true;
    public float haloSize = 2.4f;
    public bool embers = true;

    private Light torchLight;
    private float baseIntensity, seed, gustEnd, nextGust;
    private Color baseColor;
    private Vector3 basePosition;
    private GlowBillboard glow;

    static readonly Color Warm = new Color(1f, 0.55f, 0.14f);

    void Start()
    {
        torchLight = GetComponent<Light>();
        if (torchLight == null) { enabled = false; return; }

        if (disableShadows && torchLight.type == LightType.Point) torchLight.shadows = LightShadows.None;

        baseIntensity = torchLight.intensity;
        baseColor = torchLight.color;
        basePosition = transform.localPosition;
        seed = Random.Range(0f, 500f);
        nextGust = Time.time + Random.Range(4f, 12f);

        if (halo) glow = GlowBillboard.Create(transform, transform.position, haloSize, Warm);
        if (embers) BuildEmbers();
    }

    void Update()
    {
        float t = Time.time;
        float slow = Mathf.PerlinNoise(seed + t * 1.7f, 0.5f);
        float fast = Mathf.PerlinNoise(seed + t * 9f, 3.1f);
        float variation = (0.65f * slow + 0.35f * fast) * 2f - 1f;

        // Ráfaga ocasional: la llama se encoge un instante
        if (t >= nextGust)
        {
            gustEnd = t + Random.Range(0.25f, 0.5f);
            nextGust = t + Random.Range(5f, 13f);
        }
        float gust = t < gustEnd ? Mathf.Lerp(0.62f, 1f, Mathf.Abs(Mathf.Sin((gustEnd - t) * 9f)) * 0.5f + 0.5f) : 1f;

        float level = (1f + amplitude * variation) * gust;
        torchLight.intensity = baseIntensity * level;
        torchLight.color = Color.Lerp(baseColor, Warm, Mathf.Clamp01(colorShift + (0.5f - slow) * 0.7f));

        if (jitter > 0f)
        {
            Vector3 offset = new Vector3(slow - 0.5f, fast - 0.5f, Mathf.PerlinNoise(seed + t * 2.3f, 8f) - 0.5f) * (2f * jitter);
            transform.localPosition = basePosition + offset;
        }

        if (glow != null)
        {
            glow.size = haloSize * (0.9f + 0.18f * level);
            glow.intensity = Mathf.Clamp(0.45f * level, 0f, 1.2f);
        }
    }

    // Brasas diminutas que suben y se apagan
    void BuildEmbers()
    {
        ParticleSystem particles = GlowKit.NewParticles("Brasas", transform, GlowKit.Additive, Vector3.zero);

        ParticleSystem.MainModule main = particles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.75f, 0.3f, 0.9f), new Color(1f, 0.35f, 0.08f, 0.9f));
        main.gravityModifier = -0.04f;
        main.maxParticles = 30;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 3.5f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.06f;

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.25f;
        noise.frequency = 0.8f;

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        GlowKit.FadeOut(particles);
        particles.Play();
    }
}
