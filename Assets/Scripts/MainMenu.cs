using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Configuración")]
    public string nombreEscenaJuego = "Cripta"; 

    public void Jugar()
    {
        // Solo cargamos la escena del juego directamente
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}