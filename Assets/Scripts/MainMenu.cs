using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Configuración")]
    public string nombreEscenaJuego = "Cripta";

    [Header("Paneles UI (interfaz antigua, oculta)")]
    public GameObject panelBotones;
    public GameObject panelControles;

    private GameObject themedMenu;

    private void Start()
    {
        // Estado inicial limpio por si se vuelve al menú desde una partida pausada o terminada
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (panelControles != null) panelControles.SetActive(false);

        HideLegacyButtons();
        BuildThemedMenu();
    }

    // Los botones originales de la escena se reemplazan por los del tema gótico
    void HideLegacyButtons()
    {
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                string method = button.onClick.GetPersistentMethodName(i);
                if (method == nameof(Jugar) || method == nameof(Salir) || method == nameof(MostrarControles))
                {
                    button.gameObject.SetActive(false);
                    break;
                }
            }
        }
    }

    void BuildThemedMenu()
    {
        Canvas canvas = UITheme.MakeCanvas("MainMenuView", 20);
        themedMenu = canvas.gameObject;

        // Viñeta suave y brasas sobre la ilustración, sin bloquear los clics
        Image vignette = UITheme.Icon(canvas.transform, "Vineta", UISprites.Vignette, new Color(1f, 1f, 1f, 0.75f));
        UITheme.Stretch(vignette.rectTransform);

        RectTransform embers = UITheme.NewRect("Brasas", canvas.transform);
        UITheme.Stretch(embers);
        embers.gameObject.AddComponent<EmberField>();

        RectTransform column = UITheme.NewRect("Botones", canvas.transform);
        UITheme.Place(column, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(460f, 0f));
        UITheme.VLayout(column.gameObject, 12f, new RectOffset(0, 0, 0, 0));
        column.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        UITheme.MakeButton(column, "Jugar", Jugar, ButtonStyle.Primary, 70f);
        UITheme.MakeButton(column, "Controles", MostrarControles, ButtonStyle.Secondary, 62f);
        UITheme.MakeButton(column, "Opciones", MostrarOpciones, ButtonStyle.Secondary, 62f);
        UITheme.MakeButton(column, "Salir", Salir, ButtonStyle.Danger, 62f);
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
