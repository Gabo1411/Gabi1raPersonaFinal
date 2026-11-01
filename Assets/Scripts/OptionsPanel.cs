using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Panel de opciones (volumen, efectos de sonido y sensibilidad). Se construye por código para usarlo en el menú y en la pausa.
public class OptionsPanel : MonoBehaviour
{
    static OptionsPanel instance;
    static int closedFrame = -1;

    public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

    // Verdadero mientras el panel está abierto o justo el frame en que se cerró con Escape
    public static bool BlocksInput => IsOpen || closedFrame == Time.frameCount;

    Slider volumeSlider;
    Slider sfxSlider;
    Slider sensitivitySlider;
    TMP_Text volumeLabel;
    TMP_Text sfxLabel;
    TMP_Text sensitivityLabel;
    Action onClose;

    public static void Open(Action onClose = null)
    {
        if (instance == null) instance = Build();

        instance.onClose = onClose;
        instance.Refresh();
        instance.gameObject.SetActive(true);
    }

    public static void Close()
    {
        if (!IsOpen) return;

        GameSettings.Save();
        closedFrame = Time.frameCount;

        Action callback = instance.onClose;
        instance.onClose = null;
        instance.gameObject.SetActive(false);
        callback?.Invoke();
    }

    // Crea un botón "Opciones" copiando el estilo de otro botón de la escena (lo usa el menú principal).
    public static Button AddOpenButton(Button template, UnityAction onClick)
    {
        Transform parent = template.transform.parent;

        GameObject clone = Instantiate(template.gameObject, parent);
        clone.name = "Opciones";

        Button button = clone.GetComponent<Button>();
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
        }
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);

        TMP_Text tmpLabel = clone.GetComponentInChildren<TMP_Text>(true);
        if (tmpLabel != null) tmpLabel.text = "Opciones";
        else
        {
            Text label = clone.GetComponentInChildren<Text>(true);
            if (label != null) label.text = "Opciones";
        }

        // Sin layout automático el botón se coloca debajo del más bajo del panel
        if (parent.GetComponent<LayoutGroup>() == null)
        {
            RectTransform templateRect = (RectTransform)template.transform;
            float lowest = templateRect.anchoredPosition.y;
            foreach (Button sibling in parent.GetComponentsInChildren<Button>(true))
            {
                if (sibling.transform.parent != parent || sibling.gameObject == clone) continue;
                lowest = Mathf.Min(lowest, ((RectTransform)sibling.transform).anchoredPosition.y);
            }

            RectTransform cloneRect = (RectTransform)clone.transform;
            cloneRect.anchoredPosition = new Vector2(templateRect.anchoredPosition.x, lowest - (templateRect.rect.height + 20f));
        }

        return button;
    }

    public static Button FindButtonByMethod(GameObject root, string methodName)
    {
        if (root == null) return null;

        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentMethodName(i) == methodName) return button;
            }
        }
        return null;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
    }

    void Refresh()
    {
        volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
        sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
        sensitivitySlider.SetValueWithoutNotify(GameSettings.Sensitivity);
        UpdateLabels();
    }

    void UpdateLabels()
    {
        volumeLabel.text = Mathf.RoundToInt(volumeSlider.value * 100f) + "%";
        sfxLabel.text = Mathf.RoundToInt(sfxSlider.value * 100f) + "%";
        sensitivityLabel.text = sensitivitySlider.value.ToString("0.0") + "x";
    }

    // ---------- Construcción de la interfaz ----------

    static OptionsPanel Build()
    {
        Canvas canvas = UITheme.MakeCanvas("OptionsPanel", 500);
        OptionsPanel panel = canvas.gameObject.AddComponent<OptionsPanel>();

        GameObject overlay = UITheme.Overlay(canvas.transform, "Fondo", UITheme.Dim);

        RectTransform card = UITheme.CenteredCard(overlay.transform, 900f, UITheme.Accent);
        UITheme.VLayout(card.gameObject, 12f, new RectOffset(56, 56, 48, 44));

        TMP_Text title = UITheme.Label(card, "Titulo", "OPCIONES", 64f, UITheme.Accent, TextAlignmentOptions.Center, FontStyles.Bold);
        UITheme.Size(title.gameObject, height: 90f);

        RectTransform gap = UITheme.NewRect("Espacio", card);
        UITheme.Size(gap.gameObject, height: 10f);

        panel.volumeSlider = Row(card, "Volumen general", 0f, 1f, out panel.volumeLabel);
        panel.sfxSlider = Row(card, "Efectos de sonido", 0f, 1f, out panel.sfxLabel);
        panel.sensitivitySlider = Row(card, "Sensibilidad", GameSettings.MinSensitivity, GameSettings.MaxSensitivity, out panel.sensitivityLabel);

        panel.volumeSlider.onValueChanged.AddListener(value =>
        {
            GameSettings.Volume = value;
            panel.UpdateLabels();
        });
        panel.sfxSlider.onValueChanged.AddListener(value =>
        {
            GameSettings.SfxVolume = value;
            panel.UpdateLabels();
        });
        panel.sensitivitySlider.onValueChanged.AddListener(value =>
        {
            GameSettings.Sensitivity = value;
            panel.UpdateLabels();
        });

        RectTransform gap2 = UITheme.NewRect("Espacio", card);
        UITheme.Size(gap2.gameObject, height: 14f);

        UITheme.MakeButton(card, "Volver", Close, ButtonStyle.Primary, 70f);

        canvas.gameObject.SetActive(false);
        return panel;
    }

    // Fila con nombre a la izquierda, slider en el centro y valor a la derecha
    static Slider Row(Transform parent, string name, float min, float max, out TMP_Text valueLabel)
    {
        RectTransform row = UITheme.NewRect("Fila " + name, parent);
        UITheme.HLayout(row.gameObject, 24f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleLeft);
        UITheme.Size(row.gameObject, height: 72f);

        TMP_Text label = UITheme.Label(row, "Nombre", name, 32f, UITheme.TextMain, TextAlignmentOptions.MidlineLeft);
        UITheme.Size(label.gameObject, 330f, 72f);

        Slider slider = UITheme.MakeSlider(row, min, max);

        valueLabel = UITheme.Label(row, "Valor", "", 30f, UITheme.Accent, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        UITheme.Size(valueLabel.gameObject, 110f, 72f);

        return slider;
    }
}
