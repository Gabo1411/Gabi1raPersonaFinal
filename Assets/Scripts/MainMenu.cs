using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Configuración")]
    public string nombreEscenaJuego = "Cripta";

    [Header("Paneles UI")]
    public GameObject panelBotones;    // Panel con Jugar, Controles y Salir
    public GameObject panelControles;  // Panel con la imagen de los controles

    private void Start()
    {
        // Estado inicial limpio por si se vuelve al menú desde una partida pausada o terminada
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (panelBotones != null) panelBotones.SetActive(true);
        if (panelControles != null) panelControles.SetActive(false);

        AddOptionsButton();
    }

    // Añade el botón "Opciones" copiando el estilo del botón "Controles"
    void AddOptionsButton()
    {
        Button controlsButton = OptionsPanel.FindButtonByMethod(panelBotones, nameof(MostrarControles));
        if (controlsButton == null) return;

        OptionsPanel.AddOpenButton(controlsButton, MostrarOpciones);
    }

    public void Jugar()
    {
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    public void MostrarOpciones()
    {
        if (panelBotones != null) panelBotones.SetActive(false);
        OptionsPanel.Open(() => { if (panelBotones != null) panelBotones.SetActive(true); });
    }

    public void MostrarControles()
    {
        if (panelBotones != null) panelBotones.SetActive(false);
        ControlsPanel.Open(() => { if (panelBotones != null) panelBotones.SetActive(true); });
    }

    public void OcultarControles()
    {
        if (panelControles != null) panelControles.SetActive(false);
        if (panelBotones != null) panelBotones.SetActive(true);
    }

    public void Salir()
    {
        Application.Quit();
    }
}
