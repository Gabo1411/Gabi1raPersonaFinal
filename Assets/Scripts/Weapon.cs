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

        // Arma automática: mantener presionado para disparar
        isFiring = Mouse.current.leftButton.isPressed;

        if (readytoFire && isFiring && (continuo || (!isReloading && bulletsLeft > 0)))
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

    // Para la interfaz
    public float ReloadProgress => isReloading && reloadTime > 0f ? Mathf.Clamp01((Time.time - reloadStartTime) / reloadTime) : 1f;

    public bool DisparoContinuoActivo => player != null && player.IsBuffActive(BuffType.DisparoContinuo);

    void UpdateAmmoUI()
    {
        if (AmmoManager.instance != null && AmmoManager.instance.ammoDisplay != null)
        {
            string loaded = DisparoContinuoActivo ? "INF" : bulletsLeft.ToString();
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

        if (!continuo)
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
