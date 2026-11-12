using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Estado compartido de las herramientas de depuración y el componente que las mantiene vivas:
// guarda el punto de inicio de la cripta, dibuja los textos en pantalla (FPS y estadísticas) y restaura los ajustes al salir.
public static class DebugRuntime
{
    public static bool ShowFps, ShowStats, ShowHitboxes, ShowNavMesh, ShowMissiles;
    public static bool HasStart;
    public static Vector3 StartPosition;
    public static float ShadowDistanceBackup = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!DebugConsole.Enabled) return;
        if (Object.FindFirstObjectByType<DebugRuntimeHost>() != null) return;

        GameObject go = new GameObject("HerramientasDebug");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<DebugRuntimeHost>();
        go.AddComponent<DebugGizmos>();
    }

    public static void RestoreSettings()
    {
        if (ShadowDistanceBackup >= 0f && UniversalRenderPipeline.asset != null)
        {
            UniversalRenderPipeline.asset.shadowDistance = ShadowDistanceBackup;
            ShadowDistanceBackup = -1f;
        }
    }
}

public class DebugRuntimeHost : MonoBehaviour
{
    private float smoothDelta = 0.016f;
    private string statsText = "";
    private float nextStats;
    private GUIStyle style, shadowStyle;
    private Texture2D box;

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    void OnApplicationQuit() => DebugRuntime.RestoreSettings();
    void OnDestroy() => DebugRuntime.RestoreSettings();

    // Cada escena nueva olvida el punto de inicio y restaura los ajustes de pantalla
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DebugRuntime.HasStart = false;
        EnemyBase.Frozen = false;
    }

    void Update()
    {
        smoothDelta = Mathf.Lerp(smoothDelta, Time.unscaledDeltaTime, 0.05f);

        // El primer punto en que se ve al jugador tras cargar la escena es el inicio de la cripta
        if (!DebugRuntime.HasStart && Time.timeSinceLevelLoad > 0.4f)
        {
            PlayerMovement p = FindFirstObjectByType<PlayerMovement>();
            if (p != null)
            {
                DebugRuntime.StartPosition = p.transform.position;
                DebugRuntime.HasStart = true;
            }
        }

        if (DebugRuntime.ShowStats && Time.unscaledTime >= nextStats)
        {
            nextStats = Time.unscaledTime + 0.2f;
            statsText = BuildStats();
        }
    }

    string BuildStats()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("Escena ").Append(SceneManager.GetActiveScene().name)
          .Append("   tiempo ").Append(FormatTime(Time.timeSinceLevelLoad))
          .Append("   timeScale ").Append(Time.timeScale.ToString("0.##")).Append('\n');

        PlayerMovement p = FindFirstObjectByType<PlayerMovement>();
        if (p != null)
        {
            sb.Append("Jugador  vida ").Append(p.Health).Append('/').Append(p.maxHealth);
            if (p.godMode) sb.Append("  [DIOS]");
            if (p.noclip) sb.Append("  [NOCLIP]");
            if (p.fly) sb.Append("  [VUELO]");
            if (p.IsCrouching) sb.Append("  [AGACHADO]");
            sb.Append('\n');
            Vector3 pos = p.transform.position;
            sb.Append("  pos ").Append(pos.x.ToString("F1")).Append(", ").Append(pos.y.ToString("F1")).Append(", ").Append(pos.z.ToString("F1"))
              .Append("   velocidad ").Append(p.CurrentSpeed.ToString("0.#")).Append("   daño extra +").Append(p.bonusDanio).Append('\n');

            string buffs = "";
            foreach (BuffType type in System.Enum.GetValues(typeof(BuffType)))
            {
                if (p.IsBuffActive(type)) buffs += type + " " + Mathf.CeilToInt(p.GetBuffRemaining(type)) + "s  ";
            }
            sb.Append("  efectos: ").Append(buffs.Length > 0 ? buffs : "ninguno").Append('\n');
        }

        Weapon w = FindFirstObjectByType<Weapon>();
        if (w != null)
        {
            sb.Append("Arma  munición ").Append(w.infiniteAmmo ? "INF" : w.bulletsLeft.ToString()).Append('/').Append(w.magazineSize);
            sb.Append("   cristal ").Append(w.SniperCharge >= 1f ? "listo" : Mathf.RoundToInt(w.SniperCharge * 100f) + " %");
            if (w.SniperActive) sb.Append("  [apuntando]");
            if (w.isReloading) sb.Append("  [recargando]");
            sb.Append('\n');
        }

        int cactus = 0, mushrooms = 0;
        foreach (EnemyBase enemy in EnemyBase.Active)
        {
            if (enemy.health <= 0) continue;
            if (enemy is EnemyMushroom) mushrooms++; else cactus++;
        }
        sb.Append("Enemigos vivos ").Append(cactus + mushrooms).Append("  (").Append(cactus).Append(" cactus, ").Append(mushrooms).Append(" hongos)");
        if (EnemyBase.Frozen) sb.Append("  [IA congelada]");
        sb.Append('\n');

        Boss boss = FindFirstObjectByType<Boss>();
        if (boss != null)
        {
            sb.Append("Jefe  vida ").Append(boss.CurrentHealth).Append('/').Append(boss.maxHealth)
              .Append(" (").Append(Mathf.RoundToInt(boss.HealthFraction * 100f)).Append(" %)");
            if (boss.frozen) sb.Append("  [congelado]");
            sb.Append('\n');
        }

        GameManager gm = GameManager.Instance;
        if (gm != null) sb.Append("Puntos ").Append(gm.CurrentScore).Append('/').Append(gm.targetScore);
        return sb.ToString();
    }

    static string FormatTime(float seconds) => Mathf.FloorToInt(seconds / 60f).ToString("00") + ":" + Mathf.FloorToInt(seconds % 60f).ToString("00");

    void OnGUI()
    {
        if (!DebugRuntime.ShowFps && !DebugRuntime.ShowStats) return;
        EnsureStyles();

        if (DebugRuntime.ShowFps)
        {
            string fps = Mathf.RoundToInt(1f / Mathf.Max(0.0001f, smoothDelta)) + " FPS  (" + (smoothDelta * 1000f).ToString("0.0") + " ms)";
            Rect rect = new Rect(Screen.width - 250f, 8f, 242f, 28f);
            GUI.DrawTexture(rect, box);
            DrawText(new Rect(rect.x + 8f, rect.y + 3f, rect.width, rect.height), fps, 16);
        }

        if (DebugRuntime.ShowStats)
        {
            int lines = statsText.Split('\n').Length;
            Rect rect = new Rect(8f, 8f, 560f, 12f + lines * 19f);
            GUI.DrawTexture(rect, box);
            DrawText(new Rect(rect.x + 8f, rect.y + 6f, rect.width, rect.height), statsText, 14);
        }
    }

    void DrawText(Rect rect, string text, int size)
    {
        style.fontSize = size;
        shadowStyle.fontSize = size;
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, shadowStyle);
        GUI.Label(rect, text, style);
    }

    void EnsureStyles()
    {
        if (style != null) return;

        box = new Texture2D(1, 1);
        box.SetPixel(0, 0, new Color(0.02f, 0.015f, 0.025f, 0.78f));
        box.Apply();
        box.hideFlags = HideFlags.HideAndDontSave;

        style = new GUIStyle(GUI.skin.label) { richText = false, wordWrap = false, normal = { textColor = new Color(0.95f, 0.9f, 0.78f) } };
        shadowStyle = new GUIStyle(style) { normal = { textColor = Color.black } };
    }
}
