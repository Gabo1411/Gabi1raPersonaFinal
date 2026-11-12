using UnityEngine;
using UnityEngine.InputSystem;

// Animación procedural del arma arcana: respiración, vaivén al caminar, balanceo al mover la cámara, retroceso,
// recarga, dash y aparición inicial. También maneja el brillo del núcleo, las runas y los anillos giratorios.
public class WeaponAnimator : MonoBehaviour
{
    [Header("Piezas")]
    [Tooltip("Pivote que se mueve con todas las animaciones (retroceso, vaivén, recarga...).")]
    public Transform visual;
    public Transform core;
    public Transform ringA;
    public Transform ringB;
    public Transform pendant;
    public Renderer[] coreRenderers;
    public Renderer[] runeRenderers;
    public Renderer[] ringRenderers;
    public Light coreLight;

    [Header("Recarga: cristal")]
    [Tooltip("Cristal viejo que sale disparado hacia un costado.")]
    public Transform ejectedCrystal;
    [Tooltip("Cristal nuevo que entra por arriba de la cápsula.")]
    public Transform incomingCrystal;
    [Tooltip("Punta de la jaula, que se levanta para dejar pasar el cristal nuevo.")]
    public Transform cageCap;

    [Header("Colores")]
    public Color coreColor = new Color(0.2f, 0.95f, 0.85f);
    public Color continuousColor = new Color(0.8f, 0.35f, 1f);
    public Color sniperColor = new Color(1f, 0.92f, 0.12f);

    [Header("Ajustes")]
    public float equipDuration = 0.8f;

    private Weapon weapon;
    private PlayerMovement player;
    private CharacterController body;
    private MaterialPropertyBlock block;

    private Vector3 basePosition;
    private Quaternion baseRotation;

    // Muelles de posición y rotación (desplazamientos respecto a la pose de reposo)
    private Vector3 posOffset, posVelocity;
    private Vector3 rotOffset, rotVelocity;

    private float bobTimer, moveAmount, equipTime, flash, ringSpeed, swayX, swayY;
    private bool wasReloading, wasGrounded = true;
    private Vector3 coreOrigin, capPosition;
    private Quaternion capRotation = Quaternion.identity;
    private bool crystalInserted;

    // Modo francotirador: 0..1 suavizado y arcos eléctricos que salen del cristal hacia el arma
    private float sniper, arcTimer, spinPitch, spinSpeed;
    private bool wasCharging;
    private float sniperCharge = 1f;
    private Color arcColor = Color.yellow;
    private Vector3[][] arcJitter;
    private Transform spinPivot;
    private const float SniperTilt = 90f;   // grados que se inclina el cristal con su jaula hacia delante
    private LineRenderer[] arcs;
    private Transform[] arcTargets;

    public Vector3 CoreWorldPosition => core != null ? core.position : transform.position;

    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        weapon = GetComponentInParent<Weapon>();
        player = GetComponentInParent<PlayerMovement>();
        body = GetComponentInParent<CharacterController>();
        block = new MaterialPropertyBlock();

        // El arma no proyecta sombras (la linterna sale de la cámara y dibujaba la silueta de la pistola en el suelo)
        foreach (Renderer rend in GetComponentsInChildren<Renderer>(true))
        {
            if (rend is ParticleSystemRenderer || rend is TrailRenderer || rend is LineRenderer) continue;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        if (visual == null) visual = transform;
        basePosition = visual.localPosition;
        baseRotation = visual.localRotation;

        if (core != null) coreOrigin = core.localPosition;
        BuildSpinPivot();
        if (cageCap != null) { capPosition = cageCap.localPosition; capRotation = cageCap.localRotation; }
        if (ejectedCrystal != null) ejectedCrystal.gameObject.SetActive(false);
        if (incomingCrystal != null) incomingCrystal.gameObject.SetActive(false);
    }

    // El cristal y su jaula (aros, postes y punta) cuelgan de un pivote en el centro del núcleo para poder girar juntos
    // hacia delante en el modo francotirador.
    void BuildSpinPivot()
    {
        if (core == null || core.parent == null) return;

        Transform parent = core.parent;
        spinPivot = new GameObject("GiroCristal").transform;
        spinPivot.SetParent(parent, false);
        spinPivot.position = core.position;
        spinPivot.localRotation = Quaternion.identity;

        core.SetParent(spinPivot, true);
        string[] cage = { "AroJaulaBase", "AroJaulaTope", "PosteJaula0", "PosteJaula1", "PosteJaula2", "PosteJaula3", "PuntaJaula" };
        foreach (string pieceName in cage)
        {
            Transform piece = parent.Find(pieceName);
            if (piece != null) piece.SetParent(spinPivot, true);
        }
    }

    // Retroceso al disparar; con Disparo Continuo es más suave porque los tiros son mucho más seguidos
    public void OnFire(bool continuous)
    {
        float strength = continuous ? 0.45f : 1f;
        posOffset += new Vector3(Random.Range(-0.004f, 0.004f), 0.006f, -0.06f) * strength;
        rotOffset += new Vector3(-6.5f, Random.Range(-1.2f, 1.2f), Random.Range(-2.5f, 2.5f)) * strength;
        flash = 1f;
    }

    // El cristal sale disparado: gran retroceso y destello
    public void OnSniperFire()
    {
        posOffset += new Vector3(Random.Range(-0.006f, 0.006f), 0.02f, -0.14f);
        rotOffset += new Vector3(-15f, Random.Range(-2f, 2f), Random.Range(-4f, 4f));
        flash = 1.8f;
    }

    // Chasquido seco al intentar disparar sin munición
    public void OnDryFire()
    {
        posOffset += new Vector3(0f, -0.004f, -0.015f);
        rotOffset += new Vector3(1.5f, 0f, 0f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return; // en pausa no se anima

        float t = Time.time;
        bool continuous = weapon != null && weapon.DisparoContinuoActivo;
        bool reloading = weapon != null && weapon.isReloading && !continuous;

        Vector3 targetPos = Vector3.zero;
        Vector3 targetRot = Vector3.zero;

        // --- Aparición inicial: el arma sube desde abajo ---
        equipTime += dt;
        float equip = 1f - Mathf.SmoothStep(0f, 1f, equipTime / Mathf.Max(0.01f, equipDuration));
        targetPos += new Vector3(0.03f, -0.35f, -0.08f) * equip;
        targetRot += new Vector3(45f, -15f, 12f) * equip;

        // --- Modo francotirador: el arma se acerca y se estabiliza ---
        sniper = Mathf.MoveTowards(sniper, weapon != null && weapon.SniperActive ? 1f : 0f, 4f * dt);
        targetPos += new Vector3(-0.012f, 0.01f, 0.025f) * sniper;
        targetRot += new Vector3(-1.5f, 0f, 0f) * sniper;

        // --- Respiración ---
        targetPos += new Vector3(Mathf.Sin(t * 0.7f) * 0.0015f, Mathf.Sin(t * 1.4f) * 0.0025f, 0f);
        targetRot += new Vector3(0f, 0f, Mathf.Sin(t * 1.1f) * 0.4f);

        // --- Vaivén al caminar ---
        float speedRatio = 0f;
        bool grounded = body == null || body.isGrounded;
        if (body != null && player != null)
        {
            Vector3 velocity = body.velocity;
            speedRatio = new Vector2(velocity.x, velocity.z).magnitude / Mathf.Max(1f, player.speed);
        }
        moveAmount = Mathf.MoveTowards(moveAmount, grounded ? Mathf.Clamp01(speedRatio) * (1f - 0.6f * sniper) : 0f, 4f * dt);
        bobTimer += dt * (5f + moveAmount * 7f);
        targetPos += new Vector3(Mathf.Sin(bobTimer * 0.5f) * 0.012f, -Mathf.Abs(Mathf.Sin(bobTimer)) * 0.014f, 0f) * moveAmount;
        targetRot += new Vector3(Mathf.Abs(Mathf.Sin(bobTimer)) * 0.8f, 0f, Mathf.Sin(bobTimer * 0.5f) * 1.2f) * moveAmount;

        // --- Aterrizaje y salto ---
        if (body != null)
        {
            targetPos.y += Mathf.Clamp(body.velocity.y * 0.0015f, -0.02f, 0.02f);
            if (grounded && !wasGrounded) posOffset.y -= 0.03f;
        }
        wasGrounded = grounded;

        // --- Balanceo al mover la cámara: el arma se retrasa un poco respecto al giro ---
        Vector2 look = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        swayX = Mathf.Lerp(swayX, Mathf.Clamp(look.x * 0.05f, -3f, 3f), 10f * dt);
        swayY = Mathf.Lerp(swayY, Mathf.Clamp(look.y * 0.05f, -3f, 3f), 10f * dt);
        targetRot += new Vector3(swayY, swayX, -swayX * 0.7f);
        targetPos += new Vector3(-swayX * 0.003f, swayY * 0.002f, 0f);

        // --- Dash: el arma se echa hacia atrás ---
        if (player != null && player.IsDashing)
        {
            targetPos += new Vector3(0.02f, -0.03f, -0.07f);
            targetRot += new Vector3(10f, 6f, -12f);
        }

        // --- Recarga: baja y se inclina mientras el núcleo se reenciende ---
        float reloadProgress = reloading ? weapon.ReloadProgress : 0f;
        if (reloading)
        {
            float lower = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(reloadProgress / 0.25f)) - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((reloadProgress - 0.75f) / 0.25f));
            targetPos += new Vector3(-0.02f, -0.09f, -0.04f) * lower;
            targetRot += new Vector3(24f, 8f, -30f) * lower;
            targetRot.z += Mathf.Sin(t * 30f) * 1.2f * lower;
        }
        if (wasReloading && !reloading)
        {
            // Al terminar la recarga el arma da un pequeño golpe hacia arriba
            posOffset += new Vector3(0f, 0.015f, -0.02f);
            rotOffset += new Vector3(-5f, 0f, 3f);
            flash = 0.8f;
        }
        wasReloading = reloading;

        // --- Muelles ---
        posOffset = Vector3.SmoothDamp(posOffset, targetPos, ref posVelocity, 0.07f, Mathf.Infinity, dt);
        rotOffset = Vector3.SmoothDamp(rotOffset, targetRot, ref rotVelocity, 0.08f, Mathf.Infinity, dt);

        visual.localPosition = basePosition + posOffset;
        visual.localRotation = baseRotation * Quaternion.Euler(rotOffset);

        UpdateReloadCrystal(reloading, reloadProgress);
        UpdateEffects(dt, t, continuous, reloading, reloadProgress);
    }

    // Recarga: el cristal viejo sale despedido hacia la derecha, la punta de la jaula se abre y un cristal nuevo
    // desciende desde arriba hasta quedar en su sitio con un destello.
    void UpdateReloadCrystal(bool reloading, float p)
    {
        if (core == null || ejectedCrystal == null || incomingCrystal == null) return;

        if (!reloading)
        {
            ejectedCrystal.gameObject.SetActive(false);
            incomingCrystal.gameObject.SetActive(false);
            // Tras un disparo francotirador el cristal vuelve a crecer en su sitio
            core.localScale = Vector3.one * (weapon != null ? weapon.SniperRegrow : 1f);
            if (cageCap != null) { cageCap.localPosition = capPosition; cageCap.localRotation = capRotation; }
            crystalInserted = false;
            return;
        }

        // El cristal de la jaula desaparece mientras el viejo sale y el nuevo entra
        bool coreHidden = p > 0.04f && p < 0.8f;
        core.localScale = coreHidden ? Vector3.zero : Vector3.one;

        // --- Cristal viejo: sale en arco hacia el costado, girando y encogiéndose ---
        bool ejecting = p > 0.04f && p < 0.5f;
        ejectedCrystal.gameObject.SetActive(ejecting);
        if (ejecting)
        {
            float e = Mathf.Clamp01((p - 0.04f) / 0.38f);
            Vector3 position = coreOrigin + Vector3.right * (0.46f * e) + Vector3.up * (0.12f * Mathf.Sin(e * Mathf.PI * 0.9f) - 0.24f * e * e);
            ejectedCrystal.localPosition = position;
            ejectedCrystal.localRotation = Quaternion.Euler(e * 540f, e * 720f, e * 300f);
            ejectedCrystal.localScale = Vector3.one * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1f, e)));
        }

        // --- Punta de la jaula: se levanta y se inclina para abrir la tapa ---
        if (cageCap != null)
        {
            float open = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.34f, 0.46f, p)) - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.76f, 0.88f, p));
            cageCap.localPosition = capPosition + Vector3.up * (0.09f * open) + Vector3.right * (0.03f * open);
            cageCap.localRotation = capRotation * Quaternion.Euler(0f, 0f, -40f * open);
        }

        // --- Cristal nuevo: entra por arriba y se acomoda girando cada vez más despacio ---
        bool inserting = p > 0.42f && p < 0.84f;
        incomingCrystal.gameObject.SetActive(inserting);
        if (inserting)
        {
            float u = Mathf.Clamp01((p - 0.42f) / 0.38f);
            float ease = 1f - Mathf.Pow(1f - u, 3f);
            incomingCrystal.localPosition = Vector3.Lerp(coreOrigin + Vector3.up * 0.3f, coreOrigin, ease);
            incomingCrystal.localRotation = Quaternion.Euler(0f, (1f - ease) * 540f, 0f);
            incomingCrystal.localScale = Vector3.one * Mathf.Lerp(0.5f, 1f, ease);
        }

        // Al terminar de entrar, el núcleo se enciende con un destello
        if (!crystalInserted && p >= 0.8f)
        {
            crystalInserted = true;
            flash = 1f;
        }
    }

    void UpdateEffects(float dt, float t, bool continuous, bool reloading, float reloadProgress)
    {
        flash = Mathf.MoveTowards(flash, 0f, dt * (continuous ? 9f : 6f));

        float ammo = 1f;
        if (weapon != null && !continuous) ammo = weapon.magazineSize > 0 ? (float)weapon.bulletsLeft / weapon.magazineSize : 0f;

        // El núcleo muestra la munición: brilla con el cargador lleno, titila con poca y se apaga al vaciarse
        float level;
        if (reloading) level = Mathf.Lerp(0.04f, 1f, Mathf.SmoothStep(0.45f, 1f, reloadProgress)) * (0.75f + 0.25f * Mathf.Sin(t * 40f));
        else if (ammo <= 0f) level = 0f;
        else level = 0.3f + 0.7f * ammo;

        if (!reloading && ammo > 0f && ammo <= 0.2f) level *= 0.65f + 0.35f * Mathf.Sin(t * 14f);
        level *= 1f + 0.1f * Mathf.Sin(t * 3f);

        Color color = continuous ? continuousColor : Color.Lerp(coreColor, sniperColor, sniper);
        level *= 1f + 0.8f * sniper;

        // Tras un disparo cargado el cristal queda apagado y se va cargando hasta poder disparar de nuevo
        float charge = weapon != null && !continuous ? weapon.SniperCharge : 1f;
        if (charge < 1f)
        {
            float energy = charge * charge;
            level *= Mathf.Lerp(0.03f, 1f, energy) * (0.88f + 0.12f * Mathf.Sin(t * (10f + 40f * charge)));
            color = Color.Lerp(new Color(0.22f, 0.26f, 0.28f), color, Mathf.Clamp01(charge * 1.3f));
        }
        else if (wasCharging)
        {
            flash = 1.5f;   // destello: ya se puede volver a disparar
        }
        wasCharging = charge < 1f;
        sniperCharge = charge;
        // Todo el brillo (cristal, runas, anillos y luz sobre el arma) es proporcional al nivel del cristal:
        // con el cargador vacío el arma queda casi apagada
        float coreIntensity = 1.45f * level * (1f + flash * 1.1f);

        SetEmission(coreRenderers, color * coreIntensity);
        SetEmission(runeRenderers, color * (0.55f * level * (1f + flash * 1.6f)));
        SetEmission(ringRenderers, color * (1.05f * level * (1f + flash * 0.8f)));

        if (coreLight != null)
        {
            coreLight.color = color;
            coreLight.intensity = 0.48f * level * (1f + flash * 1.4f);
        }

        // --- Piezas que giran o flotan ---
        float targetRing = 26f + flash * 220f + (reloading ? 380f : 0f) + moveAmount * 40f + sniper * 520f;
        ringSpeed = Mathf.Lerp(ringSpeed, targetRing, 5f * dt);
        if (ringA != null) ringA.Rotate(0f, 0f, ringSpeed * dt, Space.Self);
        if (ringB != null) ringB.Rotate(0f, 0f, -ringSpeed * 1.35f * dt, Space.Self);

        if (core != null)
        {
            core.Rotate(0f, (40f + flash * 300f) * dt, 0f, Space.Self);
        }

        // Modo francotirador: cristal y jaula se tumban hacia delante hasta quedar paralelos al cañón
        // (un cuarto de vuelta con un pequeño rebote) y vuelven a la vertical al soltar
        if (spinPivot != null)
        {
            bool aiming = weapon != null && weapon.SniperActive;
            float target = aiming ? SniperTilt : 0f;
            // Muelle amortiguado: llega con un leve pasarse y se asienta
            float accel = (target - spinPitch) * 260f - spinSpeed * 20f;
            spinSpeed += accel * dt;
            spinPitch += spinSpeed * dt;
            if (!aiming && Mathf.Abs(spinPitch) < 0.05f && Mathf.Abs(spinSpeed) < 1f) { spinPitch = 0f; spinSpeed = 0f; }
            spinPivot.localRotation = Quaternion.Euler(spinPitch, 0f, 0f);
        }

        arcColor = color;

        if (pendant != null)
        {
            float swing = Mathf.Sin(t * 2.3f) * 5f + posVelocity.z * 180f + moveAmount * Mathf.Sin(bobTimer * 0.5f) * 14f;
            float side = Mathf.Sin(t * 1.7f) * 4f + posVelocity.x * 260f;
            pendant.localRotation = Quaternion.Euler(swing, 0f, side);
        }
    }

    // Corriente eléctrica: finos rayos amarillos que saltan del cristal a las piezas del arma mientras se apunta
    // Se actualiza en LateUpdate para seguir exactamente la pose final del arma (sin retraso al moverse)
    void LateUpdate()
    {
        UpdateArcs(Time.deltaTime, arcColor);
    }

    void UpdateArcs(float dt, Color color)
    {
        if (core == null) return;

        if (arcs == null)
        {
            // Cada pieza recibe dos rayos para que la corriente se note más
            arcTargets = new[] { ringA, ringB, pendant, visual, ringA, ringB, pendant, visual };
            arcs = new LineRenderer[arcTargets.Length];
            Material material = new Material(RuntimeShaders.Find("Sprites/Default"));
            for (int i = 0; i < arcs.Length; i++)
            {
                GameObject go = new GameObject("ArcoElectrico");
                go.transform.SetParent(transform, false);
                LineRenderer line = go.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.useWorldSpace = true;
                line.positionCount = 8;
                line.numCapVertices = 2;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                arcs[i] = line;
            }
            arcJitter = new Vector3[arcs.Length][];
            for (int i = 0; i < arcs.Length; i++) arcJitter[i] = new Vector3[arcs[i].positionCount];
        }

        float visible = sniper * Mathf.Clamp01(core.localScale.x) * sniperCharge;
        arcTimer -= dt;
        bool refresh = arcTimer <= 0f;
        if (refresh) arcTimer = 0.04f;

        for (int i = 0; i < arcs.Length; i++)
        {
            LineRenderer line = arcs[i];
            line.enabled = visible > 0.05f && arcTargets[i] != null;
            if (!line.enabled) continue;

            float width = 0.016f * visible * (0.8f + 0.5f * Random.value);
            line.startWidth = width;
            line.endWidth = width * 0.6f;
            Color c = Color.Lerp(color, Color.white, 0.55f);
            c.a = Mathf.Clamp01(visible * 1.4f) * (0.75f + 0.25f * Random.value);
            line.startColor = c;
            line.endColor = new Color(c.r, c.g, c.b, c.a * 0.5f);

            // La forma del rayo cambia cada pocos fotogramas (desplazamientos en el espacio del arma);
            // los extremos se recalculan en cada fotograma para que sigan fijos al cristal y a las piezas
            if (refresh)
            {
                for (int k = 1; k < line.positionCount - 1; k++) arcJitter[i][k] = Random.insideUnitSphere * 0.035f;
            }

            Vector3 start = core.position, end = arcTargets[i].position;
            for (int k = 0; k < line.positionCount; k++)
            {
                float u = k / (float)(line.positionCount - 1);
                Vector3 point = Vector3.Lerp(start, end, u) + visual.TransformVector(arcJitter[i][k]);
                line.SetPosition(k, point);
            }
        }
    }

    void SetEmission(Renderer[] renderers, Color color)
    {
        if (renderers == null) return;

        block.SetColor(EmissionColor, color);
        foreach (Renderer rend in renderers)
        {
            if (rend != null) rend.SetPropertyBlock(block);
        }
    }
}
