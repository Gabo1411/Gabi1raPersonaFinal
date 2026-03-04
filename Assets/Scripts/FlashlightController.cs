using UnityEngine;

public partial class FlashlightController : MonoBehaviour
{
    public GameObject lightSource; // Arrastra aquí tu Spotlight
    private bool isOn = true;

    void Update()
    {
        // Detecta si presionas la tecla "F"
        if (Input.GetKeyDown(KeyCode.F))
        {
            isOn = !isOn; // Cambia el estado (si es true pasa a false y viceversa)
            lightSource.SetActive(isOn);
        }
    }
}