using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Configuración")]
    public string nombreEscenaJuego = "Cripta";

    [Header("Paneles UI")]
    public GameObject panelBotones;    // Arrastra aquí el panel que tiene Jugar, Controles, Salir
    public GameObject panelControles;  // Arrastra aquí el panel con la imagen de los controles

    private void Start()
    {
        // Aseguramos el estado inicial al cargar el menú
        if (panelBotones != null) panelBotones.SetActive(true);
        if (panelControles != null) panelControles.SetActive(false);
    }

    public void Jugar()
    {
        // Solo cargamos la escena del juego directamente
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    public void MostrarControles()
    {
        // Apaga los botones y enciende el cartel de controles
        if (panelBotones != null) panelBotones.SetActive(false);
        if (panelControles != null) panelControles.SetActive(true);
    }

    public void OcultarControles()
    {
        // Apaga el cartel de controles y devuelve los botones
        if (panelControles != null) panelControles.SetActive(false);
        if (panelBotones != null) panelBotones.SetActive(true);
    }

    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}