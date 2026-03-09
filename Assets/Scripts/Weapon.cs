using System.Collections;
using TMPro;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    // Disparar
    public bool isFiring, readytoFire;
    bool allowReset = true;
    public float shootingDelay = 0.5f;

    // Precision
    private float spreadIntensity = 0.1f;

    // Bullet
    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletSpeed = 20;
    public float bulletPrefabLifetime = 3f;

    // Efectos
    public GameObject muzzleEffect;
    private Animator animator;

    // Recarga
    public float reloadTime;
    public int magazineSize;
    public int bulletsLeft;
    public bool isReloading;

    public Vector3 spawnPosition;
    public Vector3 spawnRotation;

    // --- NUEVO SEGURO PARA LA UI ---
    private bool uiConectada = false;

    private void Awake()
    {
        readytoFire = true;
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (magazineSize <= 0)
        {
            magazineSize = 30;
            Debug.Log("¡Cargador corregido por código! Se forzó a 30.");
        }

        bulletsLeft = magazineSize;
        isReloading = false;
        uiConectada = false; // Reiniciamos el seguro
    }

    void Update()
    {
        // --- SEGURO: Apenas detecte el nuevo AmmoManager al reiniciar, actualiza la pantalla ---
        if (!uiConectada && AmmoManager.instance != null && AmmoManager.instance.ammoDisplay != null)
        {
            UpdateAmmoUI();
            uiConectada = true; // Lo apaga para no consumir recursos
        }

        // Arma automática: mantener presionado para disparar
        isFiring = Input.GetMouseButton(0);

        if (readytoFire && isFiring && !isReloading && bulletsLeft > 0)
        {
            FireWeapon();
        }

        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && isReloading == false)
        {
            Reload();
        }
    }

    void UpdateAmmoUI()
    {
        if (AmmoManager.instance != null && AmmoManager.instance.ammoDisplay != null)
        {
            AmmoManager.instance.ammoDisplay.text = bulletsLeft + " / " + magazineSize;
        }
    }

    private void Reload()
    {
        isReloading = true;
        Invoke("FinishReloading", reloadTime);

        if (SoundManager.instance != null && SoundManager.instance.reloadM107 != null)
        {
            SoundManager.instance.reloadM107.PlayOneShot(SoundManager.instance.reloadM107.clip);
        }

        if (animator != null) animator.SetTrigger("Reload");
    }

    private void FinishReloading()
    {
        bulletsLeft = magazineSize;
        isReloading = false;
        UpdateAmmoUI();
    }

    private void FireWeapon()
    {
        if (isReloading || bulletsLeft <= 0) return;

        bulletsLeft--;
        UpdateAmmoUI();

        if (muzzleEffect != null) muzzleEffect.GetComponent<ParticleSystem>().Play();
        if (animator != null) animator.SetTrigger("RECOIL");

        if (SoundManager.instance != null && SoundManager.instance.shootingSoundM107 != null)
        {
            SoundManager.instance.shootingSoundM107.PlayOneShot(SoundManager.instance.shootingSoundM107.clip);
        }

        readytoFire = false;

        Vector3 shootingDirection = CalculateDirectionAndSpread().normalized;

        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity);
        bullet.transform.forward = shootingDirection;
        bullet.GetComponent<Rigidbody>().linearVelocity = shootingDirection * bulletSpeed;

        StartCoroutine(DestroyBulletAfterTime(bullet, bulletPrefabLifetime));

        if (allowReset)
        {
            Invoke("ResetShot", shootingDelay);
            allowReset = false;
        }
    }

    private void ResetShot()
    {
        readytoFire = true;
        allowReset = true;
    }

    public Vector3 CalculateDirectionAndSpread()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out hit)) targetPoint = hit.point;
        else targetPoint = ray.GetPoint(100);

        Vector3 direction = targetPoint - bulletSpawn.position;
        float x = UnityEngine.Random.Range(-spreadIntensity, spreadIntensity);
        float y = UnityEngine.Random.Range(-spreadIntensity, spreadIntensity);

        return direction + new Vector3(x, y, 0);
    }

    private IEnumerator DestroyBulletAfterTime(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(bullet);
    }
}