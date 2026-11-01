using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image BarraVida;
    private PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = FindFirstObjectByType<PlayerMovement>();
    }

    void Update()
    {
        if (playerMovement == null || BarraVida == null) return;

        BarraVida.fillAmount = Mathf.Clamp01((float)playerMovement.Health / playerMovement.maxHealth);
    }
}
