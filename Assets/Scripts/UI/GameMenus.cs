using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Menú de pausa y pantallas de victoria y derrota. Los botones llaman al GameManager.
public class GameMenus : MonoBehaviour
{
    GameManager manager;
    GameObject pausePanel;
    GameObject resultPanel;

    public static GameMenus Create(GameManager manager)
    {
        Canvas canvas = UITheme.MakeCanvas("GameMenus", 200);
        GameMenus menus = canvas.gameObject.AddComponent<GameMenus>();
        menus.manager = manager;
        menus.BuildPause(canvas.transform);
        menus.pausePanel.SetActive(false);
        return menus;
    }

    public void ShowPause(bool show)
    {
        pausePanel.SetActive(show);
    }

    public void HideAll()
    {
        pausePanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    // ---------- Pausa ----------

    void BuildPause(Transform parent)
    {
        pausePanel = UITheme.Overlay(parent, "Pausa", UITheme.Dim);

        RectTransform card = UITheme.CenteredCard(pausePanel.transform, 620f, UITheme.Accent);
        UITheme.VLayout(card.gameObject, 16f, new RectOffset(56, 56, 52, 44));

        TMP_Text title = UITheme.Label(card, "Titulo", "PAUSA", 76f, UITheme.Accent, TextAlignmentOptions.Center, FontStyles.Bold);
        UITheme.Size(title.gameObject, height: 100f);

        Spacer(card, 6f);

        UITheme.MakeButton(card, "Reanudar", manager.ResumeGame, ButtonStyle.Primary);
        UITheme.MakeButton(card, "Controles", manager.OpenControls);
        UITheme.MakeButton(card, "Opciones", manager.OpenOptions);
        UITheme.MakeButton(card, "Salir del juego", manager.QuitGame, ButtonStyle.Danger);

        Spacer(card, 4f);

        TMP_Text hint = UITheme.Label(card, "Ayuda", "ESC para continuar", 24f, UITheme.TextDim);
        UITheme.Size(hint.gameObject, height: 32f);
    }

    static void Spacer(Transform parent, float height)
    {
        RectTransform spacer = UITheme.NewRect("Espacio", parent);
        UITheme.Size(spacer.gameObject, height: height);
    }

    // ---------- Victoria / derrota ----------

    public void ShowResult(bool win, int score, int target, float seconds)
    {
        if (resultPanel != null) Destroy(resultPanel);

        Color accent = win ? UITheme.Success : UITheme.Danger;
        Color dim = win ? new Color(0.02f, 0.1f, 0.05f, 0.86f) : new Color(0.13f, 0.02f, 0.02f, 0.9f);

        resultPanel = UITheme.Overlay(transform, win ? "Victoria" : "Derrota", dim);

        RectTransform card = UITheme.CenteredCard(resultPanel.transform, 820f, accent);
        UITheme.VLayout(card.gameObject, 18f, new RectOffset(60, 60, 56, 52));

        TMP_Text title = UITheme.Label(card, "Titulo", win ? "VICTORIA" : "HAS MUERTO", 92f, accent, TextAlignmentOptions.Center, FontStyles.Bold);
        UITheme.Size(title.gameObject, height: 120f);

        TMP_Text subtitle = UITheme.Label(card, "Subtitulo",
            win ? "Escapaste de la cripta" : "La cripta te ha reclamado", 34f, UITheme.TextDim);
        UITheme.Size(subtitle.gameObject, height: 46f);

        Spacer(card, 10f);

        RectTransform stats = UITheme.NewRect("Estadisticas", card);
        UITheme.HLayout(stats.gameObject, 20f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter, true);
        UITheme.Size(stats.gameObject, height: 130f);

        Stat(stats, "PUNTOS", score.ToString());
        Stat(stats, "TIEMPO", FormatTime(seconds));

        Spacer(card, 10f);

        UITheme.MakeButton(card, "Reintentar", manager.RestartGame, ButtonStyle.Primary, 74f);
        UITheme.MakeButton(card, "Salir del juego", manager.QuitGame, ButtonStyle.Danger, 74f);
    }

    static void Stat(Transform parent, string title, string value)
    {
        Image box = UITheme.Box(parent, "Dato " + title, UITheme.CardLight);
        UITheme.VLayout(box.gameObject, 2f, new RectOffset(12, 12, 14, 12), TextAnchor.MiddleCenter);
        UITheme.Size(box.gameObject, flexibleWidth: 1f);

        TMP_Text label = UITheme.Label(box.transform, "Titulo", title, 24f, UITheme.TextDim, TextAlignmentOptions.Center, FontStyles.Bold);
        UITheme.Size(label.gameObject, height: 32f);

        TMP_Text number = UITheme.Label(box.transform, "Valor", value, 58f, UITheme.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
        UITheme.Size(number.gameObject, height: 72f);
    }

    static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }
}
