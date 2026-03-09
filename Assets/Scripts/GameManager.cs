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

    [Header("Contenedor de Interfaz (NUEVO)")]
    public GameObject gameplayHUD; // Aquí agruparemos Vida, Balas y Dash

    [Header("UI Dash")]
    public PlayerMovement playerScript;
    public Image dashFillImage;

    [Header("Elementos del Nivel")]
    public TrapDoorDemo escotillaNivel;
    public GameObject portalSalida;

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

        // Nos aseguramos de que la UI de juego esté visible al empezar
        if (gameplayHUD != null) gameplayHUD.SetActive(true);
        if (scoreText != null) scoreText.gameObject.SetActive(true);

        if (portalSalida != null) portalSalida.SetActive(false);
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

    public void OcultarPuntaje()
    {
        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(false);
        }
    }

    private void OnTargetScoreReached()
    {
        scoreReached = true;
        Debug.Log("¡Puntaje alcanzado! Limpiando el mapa y abriendo escotilla...");

        EnemySpawner[] spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        foreach (EnemySpawner spawner in spawners)
        {
            spawner.StopSpawning();
        }

        Enemy[] enemigosVivos = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemigo in enemigosVivos)
        {
            enemigo.TakeDamage(999);
        }

        if (escotillaNivel != null)
        {
            escotillaNivel.AbrirEscotilla();
        }
    }

    public void BossDefeated()
    {
        Debug.Log("¡Jefe derrotado! Limpiando arena...");

        // 1. Detener spawners (Ya lo tienes)
        BossArenaSpawner[] spawners = FindObjectsByType<BossArenaSpawner>(FindObjectsSortMode.None);
        foreach (BossArenaSpawner spawner in spawners)
        {
            spawner.DetenerSpawner();
        }

        // 2. Limpiar Cactus (Clase Enemy)
        Enemy[] cactusVivos = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy cactus in cactusVivos)
        {
            cactus.ClearFromMap();
        }

        // 3. NUEVO: Limpiar Hongos (Clase EnemyMushroom)
        EnemyMushroom[] hongosVivos = FindObjectsByType<EnemyMushroom>(FindObjectsSortMode.None);
        foreach (EnemyMushroom hongo in hongosVivos)
        {
            // Usamos TakeDamage con un número alto para activar su muerte normal
            hongo.TakeDamage(999);
        }

        // 4. Activar portal
        if (portalSalida != null) portalSalida.SetActive(true);
    }

    public void PlayerDied()
    {
        if (!gameEnded) LoseGame();
    }

    public void WinGame()
    {
        gameEnded = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Ocultamos la interfaz al ganar
        if (gameplayHUD != null) gameplayHUD.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(false);

        if (winPanel != null) winPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    void LoseGame()
    {
        gameEnded = true;
        Debug.Log("Game Over");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Ocultamos la interfaz al perder
        if (gameplayHUD != null) gameplayHUD.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(false);

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