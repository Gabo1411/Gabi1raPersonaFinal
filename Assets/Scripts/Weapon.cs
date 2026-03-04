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
    public int magazineSize; // Asegurate en el Inspector que esto NO sea 0
    public int bulletsLeft;
    public bool isReloading;

    public Vector3 spawnPosition;
    public Vector3 spawnRotation;

    private void Awake()
    {
        readytoFire = true;
        animator = GetComponent<Animator>();
        // Quitamos la inicialización de balas de aquí y la pasamos a Start
    }

    private void Start()
    {
        // --- INICIO DEL PARCHE DE SEGURIDAD ---
        // Si por alguna razón el cargador llega como 0, lo forzamos a 30.
        if (magazineSize <= 0)
        {
            magazineSize = 30;
            Debug.Log("¡Cargador corregido por código! Se forzó a 30.");
        }
        // --- FIN DEL PARCHE ---

        bulletsLeft = magazineSize;
        isReloading = false;

        UpdateAmmoUI();
    }

    void Update()
    {
        // Actualizar UI constantemente
        UpdateAmmoUI();

        // Lógica de disparo vacía para ahorrar espacio visual aquí...
        // (Mantén tu lógica de Input y disparo original aquí abajo)

        // ... PEGA AQUÍ TU LÓGICA DE DISPARO DEL SCRIPT ANTERIOR ...
        // Si la borraste, avísame y te la paso completa de nuevo.

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

    // Función auxiliar para actualizar UI sin repetir código
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
    }

    private void FireWeapon()
    {
        if (isReloading || bulletsLeft <= 0) return;

        bulletsLeft--;

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
        bullet.GetComponent<Rigidbody>().linearVelocity = shootingDirection * bulletSpeed; // Actualizado a linearVelocity (Unity 6/2023+) o velocity

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