using TMPro;
using UnityEngine;

public class AmmoManager : MonoBehaviour
{
    public static AmmoManager instance { get; set; }

    // UI
    public TextMeshProUGUI ammoDisplay;

    private void Awake()
    {
        // Al recargar la escena, esta nueva instancia sobreescribe a la vieja sin problemas
        instance = this;
    }
}