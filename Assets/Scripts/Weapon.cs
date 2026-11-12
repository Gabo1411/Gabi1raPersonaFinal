using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    // Disparar
    public bool isFiring, readytoFire;
    public float shootingDelay = 0.5f;

    // Poción "Disparo Continuo": cadencia de subfusil con un daño ligeramente menor y sin gastar munición
    [Header("Disparo Continuo")]
    public float shootingDelayContinuo = 0.08f;
    [Range(0.1f, 1f)]
    public float multiplicadorDanioContinuo = 0.8f;

    // Modo francotirador (mantener clic derecho): dispara el cristal, gasta varias balas y hace mucho más daño
    [Header("Modo francotirador")]
    public int sniperBulletCost = 8;
    public float sniperDelay = 1.6f;
    [Tooltip("Multiplicador del daño de una bala con el coste completo; si quedan menos balas el daño baja proporcionalmente.")]
    public float sniperDamageMultiplier = 8f;
    public float sniperSpeed = 90f;
    [Range(0.1f, 1f)]
    public float sniperMoveFactor = 0.45f;
    [Range(0.4f, 1f)]
    public float sniperZoom = 0.72f;

    // Consola debug: munición infinita
    [HideInInspector] public bool infiniteAmmo;

    // Precisión
    private float spreadIntensity = 0.1f;

    // Bala
    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletSpeed = 20;
    public float bulletPrefabLifetime = 3f;

    // Sonidos propios del arma (si faltan se usan los del SoundManager)
    [Header("Sonidos")]
    public AudioClip shotClip;
    public AudioClip reloadClip;
    public AudioClip emptyClip;
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    // Efectos
    public GameObject muzzleEffect;
    private ParticleSystem muzzleParticles;
    private WeaponAnimator weaponAnimator;
    private AudioSource sfxSource;
    private float nextEmptyClick;

    // Recarga
    public float reloadTime;
    public int magazineSize;
    public int bulletsLeft;
    public bool isReloading;

    public Vector3 spawnPosition;
    public Vector3 spawnRotation;

    private Camera mainCamera;
    private PlayerMovement player;
    private float reloadStartTime;
    private bool disparoContinuoPrevio;
    private float baseFov, sniperShotTime = -10f;
    private bool sniperActive;

    // Al reiniciar la escena el AmmoManager nuevo tarda un frame en existir
    private bool uiConectada = false;

    private void Awake()
    {
        readytoFire = true;
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        weaponAnimator = GetComponentInChildren<WeaponAnimator>();
        if (muzzleEffect != null) muzzleParticles = muzzleEffect.GetComponent<ParticleSystem>();
    }

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera != null) baseFov = mainCamera.fieldOfView;
        player = FindFirstObjectByType<PlayerMovement>();

        if (magazineSize <= 0) magazineSize = 30;

        bulletsLeft = magazineSize;
        isReloading = false;
        uiConectada = false;
    }

    void Update()
    {
        if (!uiConectada && AmmoManager.instance != null && AmmoManager.instance.ammoDisplay != null)
        {
            UpdateAmmoUI();
            uiConectada = true;
        }

        // El contador de munición muestra "INF" mientras dure el Disparo Continuo
        bool continuo = DisparoContinuoActivo;
        if (continuo != disparoContinuoPrevio)
        {
            disparoContinuoPrevio = continuo;
            UpdateAmmoUI();
        }

        // Sin disparar ni recargar con el juego en pausa o terminado
        if (Time.timeScale == 0f || Mouse.current == null || Keyboard.current == null) return;

        if (DebugConsole.IsOpen)
        {
            // Con la consola abierta no se dispara ni se apunta
            isFiring = false;
            sniperActive = false;
            if (player != null) player.aimSlow = 1f;
            UpdateZoom();
            return;
        }

        // Modo francotirador: mantener clic derecho (no con Disparo Continuo ni recargando)
        sniperActive = Mouse.current.rightButton.isPressed && !continuo && !isReloading && !(player != null && player.IsDead);
        if (player != null) player.aimSlow = sniperActive ? sniperMoveFactor : 1f;
        UpdateZoom();

        // Arma automática: mantener presionado para disparar
        isFiring = Mouse.current.leftButton.isPressed;

        if (sniperActive)
        {
            if (readytoFire && isFiring && (bulletsLeft > 0 || infiniteAmmo)) FireSniper();
            else if (isFiring && bulletsLeft <= 0 && Time.time >= nextEmptyClick)
            {
                nextEmptyClick = Time.time + 0.35f;
                PlaySound(emptyClip, SoundManager.instance != null ? SoundManager.instance.emptySoundM107 : null);
                if (weaponAnimator != null) weaponAnimator.OnDryFire();
            }
        }
        else if (readytoFire && isFiring && (continuo || (!isReloading && (bulletsLeft > 0 || infiniteAmmo))))
        {
            FireWeapon();
        }
        else if (isFiring && !continuo && !isReloading && bulletsLeft <= 0 && Time.time >= nextEmptyClick)
        {
            // Sin munición: chasquido seco en lugar de disparar
            nextEmptyClick = Time.time + 0.35f;
            PlaySound(emptyClip, SoundManager.instance != null ? SoundManager.instance.emptySoundM107 : null);
            if (weaponAnimator != null) weaponAnimator.OnDryFire();
        }

        if (Keyboard.current.rKey.wasPressedThisFrame && bulletsLeft < magazineSize && !isReloading)
        {
            Reload();
        }
    }

    // ---------- Consola debug ----------

    public void DebugSetAmmo(int amount)
    {
        bulletsLeft = Mathf.Clamp(amount, 0, magazineSize);
        UpdateAmmoUI();
    }

    public void DebugSetAmmoUi() => UpdateAmmoUI();

    public void DebugReload()
    {
        if (!isReloading && bulletsLeft < magazineSize) Reload();
    }

    // El cristal vuelve a estar listo al instante
    public void DebugRechargeSniper()
    {
        CancelInvoke(nameof(ResetShot));
        sniperShotTime = -10f;
        readytoFire = true;
    }

    public void DebugSetFov(float fov)
    {
        baseFov = Mathf.Clamp(fov, 20f, 140f);
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null && !sniperActive) mainCamera.fieldOfView = baseFov;
    }

    public float BaseFov => baseFov;

    // Para el arma y la interfaz
    public bool SniperActive => sniperActive;

    // 0 justo después del disparo (el cristal salió), 1 cuando ya volvió a crecer en su sitio
    public float SniperRegrow => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.time - sniperShotTime) / (sniperDelay * 0.2f)));

    // Energía del cristal tras un disparo cargado: 0 recién disparado (apagado), 1 cuando ya se puede disparar de nuevo
    public float SniperCharge => Mathf.Clamp01((Time.time - sniperShotTime) / Mathf.Max(0.01f, sniperDelay));

    void UpdateZoom()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null || baseFov <= 0f) return;
        float target = sniperActive ? baseFov * sniperZoom : baseFov;
        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, target, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
    }

    void OnDisable()
    {
        if (player != null) player.aimSlow = 1f;
        if (mainCamera != null && baseFov > 0f) mainCamera.fieldOfView = baseFov;
    }

    // Dispara el cristal: gasta hasta 'sniperBulletCost' balas y el daño es proporcional a las que se gastaron
    private void FireSniper()
    {
        int spent = Mathf.Min(sniperBulletCost, bulletsLeft);
        if (!infiniteAmmo) bulletsLeft -= spent;
        UpdateAmmoUI();

        float multiplier = sniperDamageMultiplier * spent / Mathf.Max(1, sniperBulletCost);
        int baseDamage = bulletPrefab != null && bulletPrefab.GetComponent<BulletDamage>() != null ? bulletPrefab.GetComponent<BulletDamage>().damage : 3;
        int damage = Mathf.Max(1, Mathf.RoundToInt((baseDamage + (player != null ? player.bonusDanio : 0)) * multiplier));

        Vector3 origin = weaponAnimator != null ? weaponAnimator.CoreWorldPosition : bulletSpawn.position;
        if (mainCamera == null) mainCamera = Camera.main;
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 target = Physics.Raycast(ray, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore) && hit.distance > 2f ? hit.point : ray.GetPoint(100f);
        Vector3 direction = (target - origin).normalized;

        SniperShot.Fire(origin, direction, damage, sniperSpeed, 150f);

        sniperShotTime = Time.time;
        readytoFire = false;
        if (muzzleParticles != null) muzzleParticles.Play();
        if (weaponAnimator != null) weaponAnimator.OnSniperFire();
        PlaySound(shotClip, SoundManager.instance != null ? SoundManager.instance.shootingSoundM107 : null);
        Invoke(nameof(ResetShot), sniperDelay);
    }

    // Para la interfaz
    public float ReloadProgress => isReloading && reloadTime > 0f ? Mathf.Clamp01((Time.time - reloadStartTime) / reloadTime) : 1f;

    public bool DisparoContinuoActivo => player != null && player.IsBuffActive(BuffType.DisparoContinuo);

    void UpdateAmmoUI()
    {
        if (AmmoManager.instance != null && AmmoManager.instance.ammoDisplay != null)
        {
            string loaded = DisparoContinuoActivo || infiniteAmmo ? "INF" : bulletsLeft.ToString();
            AmmoManager.instance.ammoDisplay.text = loaded + " / " + magazineSize;
        }
    }

    private void Reload()
    {
        isReloading = true;
        reloadStartTime = Time.time;
        Invoke(nameof(FinishReloading), reloadTime);

        PlaySound(reloadClip, SoundManager.instance != null ? SoundManager.instance.reloadM107 : null);
    }

    private void FinishReloading()
    {
        bulletsLeft = magazineSize;
        isReloading = false;
        UpdateAmmoUI();
    }

    private void FireWeapon()
    {
        bool continuo = DisparoContinuoActivo;

        if (!continuo && !infiniteAmmo)
        {
            bulletsLeft--;
            UpdateAmmoUI();
        }

        if (muzzleParticles != null) muzzleParticles.Play();
        if (weaponAnimator != null) weaponAnimator.OnFire(continuo);

        PlaySound(shotClip, SoundManager.instance != null ? SoundManager.instance.shootingSoundM107 : null);

        readytoFire = false;

        Vector3 shootingDirection = CalculateDirectionAndSpread().normalized;

        GameObject bullet = PrefabPool.Get(bulletPrefab, bulletSpawn.position, Quaternion.identity);
        BulletDamage bulletDamage = bullet.GetComponent<BulletDamage>();
        bulletDamage.lifetime = bulletPrefabLifetime;
        bulletDamage.Launch(shootingDirection, bulletSpeed, continuo ? multiplicadorDanioContinuo : 1f);

        Invoke(nameof(ResetShot), continuo ? shootingDelayContinuo : shootingDelay);
    }

    private void ResetShot()
    {
        readytoFire = true;
    }

    public Vector3 CalculateDirectionAndSpread()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit) ? hit.point : ray.GetPoint(100);

        Vector3 direction = targetPoint - bulletSpawn.position;
        float x = Random.Range(-spreadIntensity, spreadIntensity);
        float y = Random.Range(-spreadIntensity, spreadIntensity);

        return direction + new Vector3(x, y, 0);
    }

    // El arma reproduce sus propios clips; si no tiene, usa la fuente del SoundManager
    void PlaySound(AudioClip clip, AudioSource fallback)
    {
        if (clip != null) sfxSource.PlayOneShot(clip, soundVolume * Sfx.Volume);
        else Sfx.Play(fallback);
    }
}
