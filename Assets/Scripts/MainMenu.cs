using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Configuración")]
    public string nombreEscenaJuego = "Cripta";

    private GameObject themedMenu;

    private void Start()
    {
        // Estado inicial limpio por si se vuelve al menú desde una partida pausada o terminada
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        BuildThemedMenu();
    }

    void BuildThemedMenu()
    {
        // El fondo (muro, enredaderas y esporas) vive en la escena; si falta, se crea aquí
        if (FindFirstObjectByType<MenuVines>() == null) MenuBackdrop.Create();

        Canvas canvas = UITheme.MakeCanvas("MainMenuView", 20);
        themedMenu = canvas.gameObject;

        // Título del juego
        TMP_Text title = UITheme.Label(canvas.transform, "Titulo", "EL JARDÍN" + "\n" + "DE LAS TUMBAS", 124f, UITheme.TextMain,
            TextAlignmentOptions.Center, FontStyles.Bold);
        title.characterSpacing = 10f;
        title.lineSpacing = -12f;
        UITheme.Outline(title, 0.28f);
        UITheme.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1500f, 300f));

        Image divider = UITheme.Icon(canvas.transform, "Separador", UISprites.Divider, UITheme.Accent);
        UITheme.Place(divider.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -390f), new Vector2(620f, 34f));

        // Tarjeta de botones, igual que la del menú de pausa
        RectTransform card = UITheme.CenteredCard(canvas.transform, 560f, new Color(0.62f, 0.3f, 0.95f));
        card.anchoredPosition = new Vector2(0f, -150f);
        UITheme.VLayout(card.gameObject, 14f, new RectOffset(60, 60, 48, 48));

        UITheme.MakeButton(card, "Jugar", Jugar, ButtonStyle.Primary, 74f);
        UITheme.MakeButton(card, "Controles", MostrarControles);
        UITheme.MakeButton(card, "Opciones", MostrarOpciones);
        UITheme.MakeButton(card, "Salir", Salir, ButtonStyle.Danger);
    }

    void SetMenuVisible(bool visible)
    {
        if (themedMenu != null) themedMenu.SetActive(visible);
    }

    public void Jugar()
    {
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    public void MostrarOpciones()
    {
        SetMenuVisible(false);
        OptionsPanel.Open(() => SetMenuVisible(true));
    }

    public void MostrarControles()
    {
        SetMenuVisible(false);
        ControlsPanel.Open(() => SetMenuVisible(true));
    }

    public void OcultarControles()
    {
        SetMenuVisible(true);
    }

    public void Salir()
    {
        Application.Quit();
    }
}
