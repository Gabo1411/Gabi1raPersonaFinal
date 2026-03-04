using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Necesario para el Input Field

public class MainMenu : MonoBehaviour
{
    [Header("Configuración")]
    public string nombreEscenaJuego = "Cripta"; // PON AQUÍ EL NOMBRE EXACTO DE TU ESCENA DE JUEGO
    public TMP_InputField inputTiempo; // Arrastra aquí el Input Field

    public void Jugar()
    {
        float tiempo = 60f; // Valor por defecto si la caja está vacía

        // Verificamos si el jugador escribió algo
        if (inputTiempo != null && inputTiempo.text.Length > 0)
        {
            // Convertimos el texto a número (Parse)
            float.TryParse(inputTiempo.text, out tiempo);
        }

        // GUARDAMOS EL DATO EN LA MEMORIA DEL JUEGO
        // "SurvivalTime" es la clave secreta para recuperar el dato luego
        PlayerPrefs.SetFloat("SurvivalTime", tiempo);
        PlayerPrefs.Save(); // Confirmar guardado

        // Cargar el juego
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}