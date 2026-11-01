using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightController : MonoBehaviour
{
    public GameObject lightSource;
    private bool isOn = true;

    void Update()
    {
        if (Keyboard.current == null || lightSource == null) return;

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            isOn = !isOn;
            lightSource.SetActive(isOn);
        }
    }
}
