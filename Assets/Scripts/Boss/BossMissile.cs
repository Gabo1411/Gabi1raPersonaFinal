using UnityEngine;

// Misil del jefe: sale hacia arriba por un costado, se fija en la posición del jugador y se lanza en línea recta
// hacia ese punto (no lo persigue: hay que salirse del anillo rojo que marca dónde va a caer).
// Usa el mismo aspecto que las demás balas del jefe.
public class BossMissile : MonoBehaviour
{
    enum Phase { Rise, Dive }

    static Texture2D ringTexture;
    static Material markerMaterial, bodyMaterial, glowMaterial;

    // Para la consola debug (dibuja el punto fijado de cada misil)
    public static readonly System.Collections.Generic.List<BossMissile> Active = new System.Collections.Generic.List<BossMissile>();
    public bool IsLocked => locked;
    public Vector3 AimPoint => aimPoint;
    public Vector3 MarkerPoint => markerPosition;
    public static float BlastRadiusValue => BlastRadius;

    void OnEnable() { Active.Add(this); }
    void OnDisable() { Active.Remove(this); }

    private Transform player;
    private PlayerMovement movement;
    private Phase phase = Phase.Rise;
    private float phaseTime, lifeTime, speed;
    private Vector3 riseVelocity, direction, aimPoint;
    private bool locked;
    private float apexY;
    private int damage;
    private bool exploded;

    private Transform marker;
    private Material markerInstance;
    private Vector3 markerPosition, markerVelocity;

    const float RiseDuration = 0.9f, RiseHeight = 14f, CeilingMargin = 1.4f;
    const float StartSpeed = 14f, MaxSpeed = 26f, Acceleration = 30f;
    const float BlastRadius = 2.4f;

    // 'visual' es el prefab de la bala del jefe; 'outward' la dirección lateral hacia la que se abre al subir
    public static BossMissile Launch(Vector3 origin, Transform player, int damage, GameObject visual, Vector3 outward)
    {
        GameObject go = new GameObject("MisilJefe");
        go.transform.position = origin;

        BossMissile missile = go.AddComponent<BossMissile>();
        missile.player = player;
        missile.movement = player != null ? player.GetComponent<PlayerMovement>() : null;
        missile.damage = damage;
        missile.Build(visual, outward);
        return missile;
    }

    void Build(GameObject visualPrefab, Vector3 outward)
    {
        BuildBody(visualPrefab);

        // Indicador de impacto en el suelo
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(quad.GetComponent<Collider>());
        quad.name = "IndicadorMisil";
        marker = quad.transform;
        marker.rotation = Quaternion.Euler(90f, 0f, 0f);
        marker.localScale = Vector3.one * (BlastRadius * 2.1f);
        MeshRenderer markerRenderer = quad.GetComponent<MeshRenderer>();
        markerInstance = new Material(MarkerMaterial);
        markerRenderer.sharedMaterial = markerInstance;
        markerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        markerRenderer.receiveShadows = false;

        Vector3 target = player != null ? GroundUnder(player.position) : transform.position;
        markerPosition = target;
        marker.position = target + Vector3.up * 0.06f;

        // Sube abriéndose hacia su costado, con una pequeña variación distinta en cada misil
        Vector3 side = new Vector3(outward.x, 0f, outward.z).normalized * Random.Range(3f, 6f);
        Vector2 jitter = Random.insideUnitCircle * 1.5f;
        riseVelocity = new Vector3(side.x + jitter.x, 20f, side.z + jitter.y);
        direction = riseVelocity.normalized;

        // La altura máxima respeta el techo: sube como mucho hasta un poco por debajo de él
        float height = RiseHeight;
        int ceilingHits = PhysicsBuffers.RaycastSorted(transform.position, Vector3.up, RiseHeight + 4f, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < ceilingHits; i++)
        {
            RaycastHit hit = PhysicsBuffers.Hits[i];
            if (IsIgnored(hit.collider)) continue;
            height = Mathf.Min(height, hit.distance - CeilingMargin);
        }
        apexY = transform.position.y + Mathf.Max(1.2f, height);
        speed = StartSpeed;
    }

    // Copia el aspecto de la bala del jefe (sin su lógica, colisiones ni físicas) para que todos sus ataques se vean igual
    void BuildBody(GameObject visualPrefab)
    {
        if (visualPrefab != null)
        {
            // Se instancia dentro de un padre inactivo para que no llegue a ejecutarse el script de la bala
            GameObject holder = new GameObject("Temporal");
            holder.SetActive(false);
            GameObject copy = Instantiate(visualPrefab, holder.transform);
            foreach (BalaJefe logic in copy.GetComponentsInChildren<BalaJefe>(true)) DestroyImmediate(logic);
            foreach (Rigidbody body in copy.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(body);
            foreach (Collider col in copy.GetComponentsInChildren<Collider>(true)) DestroyImmediate(col);
            copy.transform.SetParent(transform, false);
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = Quaternion.identity;
            Destroy(holder);
            return;
        }

        // Sin prefab: esfera simple
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(sphere.GetComponent<Collider>());
        sphere.transform.SetParent(transform, false);
        sphere.transform.localScale = Vector3.one * 0.7f;
        sphere.GetComponent<MeshRenderer>().sharedMaterial = BodyMaterial;
    }

    void Update()
    {
        if (exploded) return;

        float dt = Time.deltaTime;
        phaseTime += dt;
        lifeTime += dt;

        UpdateMarker(dt);

        if (phase == Phase.Rise)
        {
            // Si el avance lateral chocaría con una pared o un techo se anula esa parte y sigue subiendo en vertical
            Vector3 step = riseVelocity * dt;
            if (HitSolid(step.magnitude, step.normalized)) step = new Vector3(0f, step.y, 0f);
            bool blocked = HitSolid(step.magnitude, step.normalized);

            if (blocked || transform.position.y >= apexY || phaseTime >= RiseDuration)
            {
                // Fija la posición actual del jugador: el misil irá hacia ese punto, aunque el jugador se mueva
                phase = Phase.Dive;
                phaseTime = 0f;
                locked = true;
                aimPoint = player != null ? BodyCenter : transform.position + Vector3.down * 5f;
                Vector3 toTarget = aimPoint - transform.position;
                direction = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : Vector3.down;
            }
            else
            {
                transform.position += step;
                transform.forward = step.sqrMagnitude > 0.0001f ? step.normalized : Vector3.up;
            }
            return;
        }

        // Picada en línea recta hacia el punto fijado, acelerando
        speed = Mathf.Min(MaxSpeed, speed + Acceleration * dt);
        float distance = speed * dt;
        float remaining = Vector3.Distance(transform.position, aimPoint);
        if (remaining <= distance) { Explode(aimPoint); return; }

        int count = PhysicsBuffers.SphereCastSorted(transform.position, 0.35f, direction, distance, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = PhysicsBuffers.Hits[i];
            Collider other = hit.collider;
            if (IsIgnored(other)) continue;
            bool isPlayer = ColliderInfo.IsPlayer(other);
            if (other.isTrigger && !isPlayer) continue;

            // Las superficies que quedan a su espalda (el techo del que acaba de salir) no cuentan como impacto
            if (hit.distance > 0f && Vector3.Dot(hit.normal, direction) > 0.1f) continue;
            if (hit.distance <= 0f && phaseTime < 0.3f && !isPlayer) continue;

            Explode(transform.position + direction * Mathf.Max(0f, hit.distance));
            return;
        }

        transform.position += direction * distance;
        transform.forward = direction;

        if (lifeTime > 7f) Explode(transform.position);
    }

    // Durante la subida solo importa no atravesar techos: si choca, empieza la picada antes
    bool HitSolid(float distance, Vector3 dir)
    {
        int count = PhysicsBuffers.SphereCastSorted(transform.position, 0.3f, dir, distance, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (!IsIgnored(PhysicsBuffers.Hits[i].collider)) return true;
        }
        return false;
    }

    // Centro del cuerpo del jugador (de pie o agachado)
    Vector3 BodyCenter => movement != null ? movement.AimPoint : player.position + Vector3.up * 0.9f;

    // Qué atraviesa un misil: jefe, enemigos, balas, otros misiles y el candelabro colgante (clasificación en caché)
    static bool IsIgnored(Collider other) => ColliderInfo.MissileIgnores(other);

    // El anillo sigue al jugador pegado al suelo hasta que el misil lo fija; entonces se queda quieto y parpadea cada vez más rápido
    void UpdateMarker(float dt)
    {
        if (marker == null) return;

        if (!locked && player != null)
        {
            Vector3 target = GroundUnder(player.position);
            markerPosition = Vector3.SmoothDamp(markerPosition, target, ref markerVelocity, 0.12f);
        }
        marker.position = markerPosition + Vector3.up * 0.06f;

        float closeness = locked ? Mathf.Clamp01(1f - Vector3.Distance(transform.position, markerPosition) / 20f) : 0f;
        float alpha = 0.55f + 0.3f * Mathf.Sin(Time.time * (6f + 18f * closeness)) + 0.15f * closeness;
        RuntimeShaders.SetColor(markerInstance, new Color(1f, 0.15f + 0.2f * closeness, 0.08f, Mathf.Clamp01(alpha)));
        marker.localScale = Vector3.one * (BlastRadius * 2.1f) * (1f + 0.06f * Mathf.Sin(Time.time * 10f));
    }

    static Vector3 GroundUnder(Vector3 position)
    {
        int count = PhysicsBuffers.RaycastSorted(position + Vector3.up * 1.5f, Vector3.down, 12f, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = PhysicsBuffers.Hits[i];
            if (ColliderInfo.IsPlayer(hit.collider)) continue;
            return hit.point;     // están ordenados: el primero que no es el jugador es el suelo
        }
        return position;
    }

    void Explode(Vector3 point)
    {
        if (exploded) return;
        exploded = true;

        // Daño en área: el jugador que esté dentro del radio recibe el golpe, aunque no lo toque el misil
        if (player != null)
        {
            Vector3 center = BodyCenter;
            if (Vector3.Distance(center, point) <= BlastRadius + 0.6f)
            {
                PlayerMovement movement = player.GetComponent<PlayerMovement>();
                if (movement != null) movement.TakeDamage(damage);
            }
        }

        Burst(point);
        if (marker != null) Destroy(marker.gameObject);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (marker != null) Destroy(marker.gameObject);
    }

    static void Burst(Vector3 position)
    {
        GameObject go = new GameObject("ExplosionMisil");
        go.transform.position = position;

        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.8f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.7f, 0.2f), new Color(0.95f, 0.2f, 0.05f));
        main.gravityModifier = 0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 45) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = GlowMaterial;

        Light flash = go.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = new Color(1f, 0.5f, 0.15f);
        flash.intensity = 6f;
        flash.range = 10f;

        particles.Play();
        Destroy(go, 1.2f);
    }

    // ---------- Recursos generados por código ----------

    static Material BodyMaterial
    {
        get
        {
            if (bodyMaterial != null) return bodyMaterial;
            bodyMaterial = new Material(RuntimeShaders.Find("Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit"));
            RuntimeShaders.SetColor(bodyMaterial, new Color(1f, 0.3f, 0.1f));
            if (bodyMaterial.HasProperty("_EmissionColor"))
            {
                bodyMaterial.EnableKeyword("_EMISSION");
                bodyMaterial.SetColor("_EmissionColor", new Color(1f, 0.35f, 0.08f) * 4f);
            }
            return bodyMaterial;
        }
    }

    static Material GlowMaterial
    {
        get
        {
            if (glowMaterial != null) return glowMaterial;
            glowMaterial = new Material(RuntimeShaders.Find("Legacy Shaders/Particles/Additive", "Sprites/Default"));
            RuntimeShaders.SetColor(glowMaterial, new Color(1f, 0.55f, 0.2f, 0.6f));
            return glowMaterial;
        }
    }

    static Material MarkerMaterial
    {
        get
        {
            if (markerMaterial != null) return markerMaterial;
            markerMaterial = new Material(RuntimeShaders.Find("Sprites/Default"));
            markerMaterial.mainTexture = Ring;
            return markerMaterial;
        }
    }

    // Anillo rojo con relleno tenue, cuatro marcas hacia dentro y un punto central
    static Texture2D Ring
    {
        get
        {
            if (ringTexture != null) return ringTexture;

            const int N = 128;
            ringTexture = new Texture2D(N, N, TextureFormat.RGBA32, true);
            ringTexture.hideFlags = HideFlags.HideAndDontSave;
            ringTexture.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixels = new Color32[N * N];
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = 0f;

                    alpha = Mathf.Max(alpha, Mathf.Clamp01(1f - Mathf.Abs(r - 0.9f) / 0.06f));            // anillo
                    alpha = Mathf.Max(alpha, r < 0.84f ? 0.16f : 0f);                                    // relleno
                    alpha = Mathf.Max(alpha, Mathf.Clamp01(1f - r / 0.07f));                             // punto central
                    float cross = Mathf.Min(Mathf.Abs(dx), Mathf.Abs(dy));
                    if (r > 0.55f && r < 0.84f) alpha = Mathf.Max(alpha, Mathf.Clamp01(1f - cross / 0.035f) * 0.8f);   // marcas

                    pixels[y * N + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha) * 255f));
                }
            }
            ringTexture.SetPixels32(pixels);
            ringTexture.Apply();
            return ringTexture;
        }
    }
}
