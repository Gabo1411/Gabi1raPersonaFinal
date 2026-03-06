using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Configuración de Puntuación")]
    public int targetScore = 100;
    private int currentScore = 0;

    [Header("UI References")]
    public TMP_Text scoreText;
    public GameObject winPanel; // Guardado para cuando derrotes al jefe final
    public GameObject losePanel;

    [Header("Elementos del Nivel")]
    public TrapDoorDemo escotillaNivel; // Referencia a la escotilla

    private bool gameEnded = false;
    private bool scoreReached = false; // Bandera para saber si ya llegamos a los puntos

    void Start()
    {
        UpdateScoreUI();

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    public void AddScore(int points)
    {
        // Si ya perdimos o ya alcanzamos el objetivo, dejamos de sumar puntos para la meta
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