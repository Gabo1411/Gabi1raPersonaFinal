using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Configuración de Puntuación")]
    public int targetScore = 100;
    private int currentScore = 0;

    // Elementos de la interfaz antigua de la escena. Se ocultan: ahora la interfaz se construye en
    // HudView (juego) y GameMenus (pausa, victoria y derrota).
    [Header("Interfaz antigua (oculta)")]
    public TMP_Text scoreText;
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject pausePanel;
    public GameObject controlsPanel;
    public GameObject gameplayHUD;
    public Image dashFillImage;

    [Header("Jugador")]
    public PlayerMovement playerScript;

    [Header("Sonido")]
    [Tooltip("Se reproduce al eliminar a un enemigo menor (no al jefe).")]
    public AudioClip killSound;

    [Header("Elementos del Nivel")]
    public TrapDoorDemo escotillaNivel;
    public GameObject portalSalida;

    private bool gameEnded = false;
    private bool scoreReached = false;
    private bool isPaused = false;
    private bool scoreVisible = true;
    private float elapsedTime = 0f;
    private AudioSource uiAudio;
    private GameMenus menus;

    public bool GameEnded => gameEnded;
    public int CurrentScore => currentScore;
    public bool ScoreVisible => scoreVisible;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        Time.timeScale = 1f;

        HideLegacyUI();

        PlayerMovement player = playerScript != null ? playerScript : FindFirstObjectByType<PlayerMovement>();
        HudView.Create(player);
        BuffHUD.Create(player);
        menus = GameMenus.Create(this);

        SetActiveIfAssigned(portalSalida, false);
    }

    // Oculta los elementos de la interfaz original que ahora reemplaza HudView y GameMenus
    void HideLegacyUI()
    {
        SetActiveIfAssigned(winPanel, false);
        SetActiveIfAssigned(losePanel, false);
        SetActiveIfAssigned(pausePanel, false);
        SetActiveIfAssigned(controlsPanel, false);
        SetActiveIfAssigned(scoreText != null ? scoreText.gameObject : null, false);
        SetActiveIfAssigned(dashFillImage != null ? dashFillImage.gameObject : null, false);

        HealthBar healthBar = FindFirstObjectByType<HealthBar>();
        if (healthBar != null) healthBar.gameObject.SetActive(false);

        if (AmmoManager.instance != null && AmmoManager.instance.ammoDisplay != null)
        {
            Canvas ammoCanvas = AmmoManager.instance.ammoDisplay.GetComponentInParent<Canvas>();
            if (ammoCanvas != null && !ammoCanvas.isRootCanvas) ammoCanvas.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!isPaused && !gameEnded) elapsedTime += Time.deltaTime;

        bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (!escape || gameEnded || OptionsPanel.BlocksInput || ControlsPanel.BlocksInput) return;

        if (isPaused) ResumeGame();
        else PauseGame();
    }

    public void PlayKillSound()
    {
        if (killSound == null) return;

        if (uiAudio == null)
        {
            uiAudio = gameObject.AddComponent<AudioSource>();
            uiAudio.playOnAwake = false;
            uiAudio.spatialBlend = 0f; // 2D: suena igual sin importar dónde muera el enemigo
        }
        uiAudio.PlayOneShot(killSound, Sfx.Volume);
    }

    // ---------- Pausa y menús ----------

    public void OpenOptions()
    {
        menus.ShowPause(false);
        OptionsPanel.Open(() => menus.ShowPause(isPaused));
    }

    public void OpenControls()
    {
        menus.ShowPause(false);
        ControlsPanel.Open(() => menus.ShowPause(isPaused));
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        menus.ShowPause(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        menus.HideAll();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ---------- Puntaje y progreso ----------

    public void AddScore(int points)
    {
        if (gameEnded || scoreReached) return;

        currentScore += points;

        if (currentScore >= targetScore) OnTargetScoreReached();
    }

    public void OcultarPuntaje()
    {
        scoreVisible = false;
    }

    private void OnTargetScoreReached()
    {
        scoreReached = true;

        foreach (EnemySpawner spawner in FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None))
        {
            spawner.StopSpawning();
        }

        // Se recorre una copia porque matar enemigos modifica la lista original
        foreach (EnemyBase enemy in new List<EnemyBase>(EnemyBase.Active))
        {
            enemy.Kill();
        }

        if (escotillaNivel != null) escotillaNivel.AbrirEscotilla();
    }

    public void BossDefeated()
    {
        foreach (BossArenaSpawner spawner in FindObjectsByType<BossArenaSpawner>(FindObjectsSortMode.None))
        {
            spawner.DetenerSpawner();
        }

        // Los cactus desaparecen sin más y los hongos mueren con su animación y sonido
        foreach (EnemyBase enemy in new List<EnemyBase>(EnemyBase.Active))
        {
            if (enemy is EnemyMushroom) enemy.Kill();
            else enemy.ClearFromMap();
        }

        // El portal aparece un momento después, cuando ya estalló el jefe
        StartCoroutine(RevealPortal());
    }

    IEnumerator RevealPortal()
    {
        yield return new WaitForSeconds(1.6f);
        PortalVisual.Reveal(portalSalida);
    }

    // ---------- Fin de la partida ----------

    public void PlayerDied()
    {
        if (!gameEnded) EndGame(false);
    }

    public void WinGame()
    {
        if (!gameEnded) EndGame(true);
    }

    void EndGame(bool win)
    {
        gameEnded = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        menus.HideAll();
        menus.ShowResult(win, currentScore, targetScore, elapsedTime);
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

    static void SetActiveIfAssigned(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }
}
