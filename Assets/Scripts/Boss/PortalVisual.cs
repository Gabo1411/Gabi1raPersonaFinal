using UnityEngine;

// Portal de salida: aparece creciendo con un destello, rodeado de un vórtice de luz que sube en espiral,
// aros girando en el suelo, una luz que late y un zumbido mágico.
public class PortalVisual : MonoBehaviour
{
    const float RevealDuration = 1.1f;
    const float ColumnHeight = 3.6f;

    static readonly Color Teal = new Color(0.25f, 0.95f, 0.9f);

    private Vector3 baseScale;
    private float elapsed;
    private Transform fx;
    private Light glowLight;
    private AudioSource hum;
    private float humVolume;
    private float seed;

    // Activa el portal con su efecto de aparición
    public static void Reveal(GameObject portal)
    {
        if (portal == null) return;

        PortalVisual visual = portal.GetComponent<PortalVisual>();
        if (visual == null) visual = portal.AddComponent<PortalVisual>();

        portal.SetActive(true);
    }

    void Awake()
    {
        baseScale = transform.localScale;
        seed = Random.value * 100f;

        // Los efectos van en un objeto aparte, sin la escala del portal, para que el vórtice no se deforme
        fx = new GameObject("PortalEfectos").transform;
        fx.position = transform.position;
        BuildEffects();
    }

    void OnDestroy()
    {
        if (fx != null) Destroy(fx.gameObject);
    }

    void OnEnable()
    {
        elapsed = 0f;
        transform.localScale = Vector3.zero;
        Sfx.AtPoint(Resources.Load<AudioClip>("Audio/Portal_Abre"), transform.position);
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        // Crece con un pequeño rebote y después "respira" suavemente
        float t = Mathf.Clamp01(elapsed / RevealDuration);
        float overshoot = 1.70158f;
        float grow = 1f + (overshoot + 1f) * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);
        float breathe = t >= 1f ? 1f + 0.03f * Mathf.Sin(Time.time * 2.4f) : 1f;
        transform.localScale = baseScale * Mathf.Max(0f, grow) * breathe;

        if (glowLight != null)
        {
            float noise = Mathf.PerlinNoise(seed, Time.time * 2.2f);
            glowLight.intensity = Mathf.Lerp(3f, 8f, noise) * Mathf.SmoothStep(0f, 1f, t);
        }

        if (hum != null) hum.volume = humVolume * Sfx.Volume * Mathf.SmoothStep(0f, 1f, t);
    }

    // ---------- Construcción de los efectos ----------

    void BuildEffects()
    {
        Material glow = ParticleMaterials.Additive(BuffIcons.Glow.texture);
        Material ring = ParticleMaterials.Additive(BuffIcons.Ring.texture);

        float floorY = FindFloorY();
        float baseY = transform.position.y - ColumnHeight * 0.5f;

        // Vórtice: partículas que suben girando alrededor del portal
        ParticleSystem vortex = NewSystem("Vortice", glow, new Vector3(0f, baseY - transform.position.y, 0f));
        ParticleSystem.MainModule vortexMain = vortex.main;
        vortexMain.startLifetime = 2.2f;
        vortexMain.startSpeed = 0f;
        vortexMain.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        vortexMain.startColor = new ParticleSystem.MinMaxGradient(Teal, new Color(0.85f, 1f, 1f));
        vortexMain.simulationSpace = ParticleSystemSimulationSpace.World;
        vortexMain.maxParticles = 300;
        ParticleSystem.EmissionModule vortexEmission = vortex.emission;
        vortexEmission.rateOverTime = 95f;
        ParticleSystem.ShapeModule vortexShape = vortex.shape;
        vortexShape.shapeType = ParticleSystemShapeType.Circle;
        vortexShape.radius = 1.25f;
        vortexShape.radiusThickness = 0.15f;
        vortexShape.rotation = new Vector3(-90f, 0f, 0f);
        ParticleSystem.VelocityOverLifetimeModule velocity = vortex.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        // Todas las curvas de velocidad deben usar el mismo modo (dos constantes)
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalY = new ParticleSystem.MinMaxCurve(2.4f, 2.4f);
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.radial = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.speedModifier = new ParticleSystem.MinMaxCurve(1f, 1f);
        ParticleSystem.SizeOverLifetimeModule vortexSize = vortex.sizeOverLifetime;
        vortexSize.enabled = true;
        vortexSize.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)));
        ParticleSystem.ColorOverLifetimeModule vortexColor = vortex.colorOverLifetime;
        vortexColor.enabled = true;
        vortexColor.color = FadeInOut();

        // Aros horizontales en el suelo que giran y laten
        for (int i = 0; i < 2; i++)
        {
            ParticleSystem ground = NewSystem("Aro" + i, ring, new Vector3(0f, floorY - transform.position.y + 0.06f, 0f));
            ParticleSystem.MainModule groundMain = ground.main;
            groundMain.startLifetime = 3f;
            groundMain.startSpeed = 0f;
            groundMain.startSize = i == 0 ? 4.2f : 2.9f;
            groundMain.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            groundMain.startColor = new Color(Teal.r, Teal.g, Teal.b, 0.8f);
            groundMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            groundMain.maxParticles = 4;
            ParticleSystem.EmissionModule groundEmission = ground.emission;
            groundEmission.rateOverTime = 0f;
            groundEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            groundMain.duration = 3f;
            ParticleSystem.ShapeModule groundShape = ground.shape;
            groundShape.enabled = false;
            ParticleSystem.RotationOverLifetimeModule groundRotation = ground.rotationOverLifetime;
            groundRotation.enabled = true;
            groundRotation.z = new ParticleSystem.MinMaxCurve((i == 0 ? 1f : -1.4f) * Mathf.PI * 0.6f);
            ParticleSystem.ColorOverLifetimeModule groundColor = ground.colorOverLifetime;
            groundColor.enabled = true;
            groundColor.color = FadeInOut();
            ground.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        }

        // Halo grande y suave detrás del portal
        ParticleSystem halo = NewSystem("Halo", glow, Vector3.zero);
        ParticleSystem.MainModule haloMain = halo.main;
        haloMain.startLifetime = 2.4f;
        haloMain.startSpeed = 0f;
        haloMain.startSize = 5.5f;
        haloMain.startColor = new Color(Teal.r, Teal.g, Teal.b, 0.55f);
        haloMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        haloMain.maxParticles = 4;
        ParticleSystem.EmissionModule haloEmission = halo.emission;
        haloEmission.rateOverTime = 0f;
        haloEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
        haloMain.duration = 2.4f;
        ParticleSystem.ShapeModule haloShape = halo.shape;
        haloShape.enabled = false;
        ParticleSystem.ColorOverLifetimeModule haloColor = halo.colorOverLifetime;
        haloColor.enabled = true;
        haloColor.color = FadeInOut();

        // Haces de luz que suben rápido, estirados en vertical
        ParticleSystem beams = NewSystem("Haces", glow, new Vector3(0f, baseY - transform.position.y, 0f));
        ParticleSystem.MainModule beamMain = beams.main;
        beamMain.startLifetime = 1.1f;
        beamMain.startSpeed = 0f;
        beamMain.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        beamMain.startColor = new ParticleSystem.MinMaxGradient(new Color(Teal.r, Teal.g, Teal.b, 0.55f), new Color(0.9f, 1f, 1f, 0.7f));
        beamMain.simulationSpace = ParticleSystemSimulationSpace.World;
        beamMain.maxParticles = 60;
        ParticleSystem.EmissionModule beamEmission = beams.emission;
        beamEmission.rateOverTime = 14f;
        ParticleSystem.ShapeModule beamShape = beams.shape;
        beamShape.shapeType = ParticleSystemShapeType.Circle;
        beamShape.radius = 1.3f;
        beamShape.radiusThickness = 0.4f;
        beamShape.rotation = new Vector3(-90f, 0f, 0f);
        ParticleSystem.VelocityOverLifetimeModule beamVelocity = beams.velocityOverLifetime;
        beamVelocity.enabled = true;
        beamVelocity.space = ParticleSystemSimulationSpace.World;
        beamVelocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        beamVelocity.y = new ParticleSystem.MinMaxCurve(4f, 7f);
        beamVelocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        beamVelocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
        beamVelocity.orbitalY = new ParticleSystem.MinMaxCurve(0f, 0f);
        beamVelocity.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f);
        beamVelocity.radial = new ParticleSystem.MinMaxCurve(0f, 0f);
        beamVelocity.speedModifier = new ParticleSystem.MinMaxCurve(1f, 1f);
        ParticleSystem.ColorOverLifetimeModule beamColor = beams.colorOverLifetime;
        beamColor.enabled = true;
        beamColor.color = FadeInOut();
        ParticleSystemRenderer beamRenderer = beams.GetComponent<ParticleSystemRenderer>();
        beamRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        beamRenderer.lengthScale = 5f;
        beamRenderer.velocityScale = 0.15f;

        // Destello inicial al aparecer
        ParticleSystem burst = NewSystem("Destello", glow, Vector3.zero);
        ParticleSystem.MainModule burstMain = burst.main;
        burstMain.loop = false;
        burstMain.duration = 0.5f;
        burstMain.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
        burstMain.startSpeed = new ParticleSystem.MinMaxCurve(3f, 9f);
        burstMain.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
        burstMain.startColor = new ParticleSystem.MinMaxGradient(Teal, Color.white);
        burstMain.simulationSpace = ParticleSystemSimulationSpace.World;
        burstMain.maxParticles = 80;
        ParticleSystem.EmissionModule burstEmission = burst.emission;
        burstEmission.rateOverTime = 0f;
        burstEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
        ParticleSystem.ShapeModule burstShape = burst.shape;
        burstShape.shapeType = ParticleSystemShapeType.Sphere;
        burstShape.radius = 0.8f;
        ParticleSystem.SizeOverLifetimeModule burstSize = burst.sizeOverLifetime;
        burstSize.enabled = true;
        burstSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Luz que late
        GameObject lightObject = new GameObject("Luz");
        lightObject.transform.SetParent(fx, false);
        glowLight = lightObject.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = Teal;
        glowLight.range = 14f;
        glowLight.intensity = 0f;
        glowLight.shadows = LightShadows.None;

        // Los sistemas creados por código no arrancan solos: se encienden aquí, ya configurados
        foreach (ParticleSystem particles in fx.GetComponentsInChildren<ParticleSystem>()) particles.Play();

        // Zumbido continuo
        AudioClip humClip = Resources.Load<AudioClip>("Audio/Portal_Zumbido");
        if (humClip != null)
        {
            hum = gameObject.AddComponent<AudioSource>();
            hum.clip = humClip;
            hum.loop = true;
            hum.spatialBlend = 1f;
            hum.minDistance = 4f;
            hum.maxDistance = 40f;
            humVolume = 0.45f;
            hum.volume = 0f;
            hum.Play();
        }
    }

    ParticleSystem NewSystem(string name, Material material, Vector3 localOffset)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(fx, false);
        go.transform.localPosition = localOffset;

        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.duration = 5f;
        main.loop = true;
        main.playOnAwake = true;

        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        return particles;
    }

    float FindFloorY()
    {
        foreach (RaycastHit hit in Physics.RaycastAll(transform.position, Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(transform)) continue;
            if (hit.collider.GetComponentInParent<PlayerMovement>() != null) continue;
            return hit.point.y;
        }
        return transform.position.y - ColumnHeight * 0.5f;
    }

    static Gradient FadeInOut()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
        return gradient;
    }
}
