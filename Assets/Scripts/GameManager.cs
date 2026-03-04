using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Configuración")]
    public float survivalTime = 60f; // Tiempo meta

    [Header("UI References")]
    public TMP_Text timerText;
    public GameObject winPanel;
    public GameObject losePanel;

    private float timeRemaining;
    private bool gameEnded = false;

    void Start()
    {
        // --- NUEVO CÓDIGO PARA LEER LA MEMORIA ---
        // Preguntamos: ¿Existe un dato guardado llamado "SurvivalTime"?
        if (PlayerPrefs.HasKey("SurvivalTime"))
        {
            // Si existe, lo usamos para sobreescribir el tiempo
            survivalTime = PlayerPrefs.GetFloat("SurvivalTime");
        }
        // ------------------------------------------

        timeRemaining = survivalTime;

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    void Update()
    {
        if (gameEnded) return;

        timeRemaining -= Time.deltaTime;

        if (timerText != null)
        {
            timerText.text = Mathf.Ceil(timeRemaining).ToString();
        }

        if (timeRemaining <= 0)
        {
            WinGame();
        }
    }

    // ESTA ES LA FUNCIÓN QUE TU SCRIPT ESTABA BUSCANDO
    public void PlayerDied()
    {
        if (!gameEnded) LoseGame();
    }

    void WinGame()
    {
        gameEnded = true;
        Debug.Log("You Survived!");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (winPanel != null) winPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    void LoseGame()
    {
        gameEnded = true;
        Debug.Log("Game Over");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (losePanel != null) losePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}