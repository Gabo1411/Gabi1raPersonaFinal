using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image BarraVida;
    private PlayerMovement playerMovement;
    private float maxHealth;
    void Start()
    {
        playerMovement = GameObject.Find("Player").GetComponent<PlayerMovement>();
        maxHealth = playerMovement.Health;
    }

    // Update is called once per frame
    void Update()
    {
        BarraVida.fillAmount = (float)playerMovement.Health / maxHealth;
    }
}
