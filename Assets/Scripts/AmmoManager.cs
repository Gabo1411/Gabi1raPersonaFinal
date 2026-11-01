using TMPro;
using UnityEngine;

public class AmmoManager : MonoBehaviour
{
    public static AmmoManager instance { get; private set; }

    public TextMeshProUGUI ammoDisplay;

    private void Awake()
    {
        // Al recargar la escena, la instancia nueva reemplaza a la anterior
        instance = this;
    }
}
