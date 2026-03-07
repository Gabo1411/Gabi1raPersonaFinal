using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Configuración de Puntuación")]
    public int targetScore = 100;
    private int currentScore = 0;

    [Header("UI References")]
    public TMP_Text scoreText;
    public GameObject winPanel; 
    public GameObject losePanel;
    public GameObject pausePanel;
    public GameObject controlsPanel;

    [Header("UI Dash")]
    public PlayerMovement playerScript; 
    public Image dashFillImage;

    [Header("Elementos del Nivel")]
    public TrapDoorDemo escotillaNivel; 

    private bool gameEnded = false;
    private bool scoreReached = false;
    private bool isPaused = false; 

    void Start()
    {
        UpdateScoreUI();

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }

    void Update()
    {
        if (playerScript != null && dashFillImage != null)
        {
            dashFillImage.fillAmount = playerScript.GetDashCooldownProgress();

            if (dashFillImage.fillAmount >= 1f)
            {
                dashFillImage.color = new Color(1f, 1f, 1f, 0.8f);
            }
            else
            {
                dashFillImage.color = new Color(1f, 1f, 1f, 0.3f);
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape) && !gameEnded)
        {
            if (isPaused)
            {
                if (controlsPanel != null) controlsPanel.SetActive(false);
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; 

        if (pausePanel != null) pausePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenControls()
    {
        if (pausePanel != null) pausePanel.SetActive(false); 
        if (controlsPanel != null) controlsPanel.SetActive(true); 
    }

    public void CloseControls()
    {
        if (controlsPanel != null) controlsPanel.SetActive(false); 
        if (pausePanel != null) pausePanel.SetActive(true); 
    }

    public void AddScore(int points)
    {
       
        if (gameEnded || scoreReached) return;

        currentScore += points;
        UpdateScoreUI();

        if (currentScore >= targetScore)
        {
            OnTargetScoreReached();
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Puntos: " + currentScore + " / " + targetScore;
        }
    }

    // Lógica al llegar a los puntos
    private void OnTargetScoreReached()
    {
        scoreReached = true;
        Debug.Log("¡Puntaje alcanzado! Limpiando el mapa y abriendo escotilla...");

        // 1. Apagamos todos los spawners de la escena
        EnemySpawner[] spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        foreach (EnemySpawner spawner in spawners)
        {
            spawner.StopSpawning();
        }

        // 2. Destruimos a todos los enemigos que ya están en el mapa
        Enemy[] enemigosVivos = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemigo in enemigosVivos)
        {
            // Usamos la nueva función en lugar de Destroy(enemigo.gameObject)
            enemigo.ClearFromMap(); 
        }

        // 3. Abrimos la escotilla para habilitar el warp
        if (escotillaNivel != null)
        {
            escotillaNivel.AbrirEscotilla();
        }
        else
        {
            Debug.LogWarning("No asignaste la escotilla en el GameManager.");
        }
    }

    public void PlayerDied()
    {
        if (!gameEnded) LoseGame();
    }

    // Esta función la dejamos intacta para usarla cuando mates al jefe
    void WinGame()
    {
        gameEnded = true;
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