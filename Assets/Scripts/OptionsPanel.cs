using System;
using TMPro;
using UnityEngine;
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
    Slider musicSlider;
    Slider sfxSlider;
    Slider sensitivitySlider;
    TMP_Text volumeLabel;
    TMP_Text musicLabel;
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

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
    }

    void Refresh()
    {
        volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
        musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
        sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
        sensitivitySlider.SetValueWithoutNotify(GameSettings.Sensitivity);
        UpdateLabels();
    }

    void UpdateLabels()
    {
        volumeLabel.text = Mathf.RoundToInt(volumeSlider.value * 100f) + "%";
        musicLabel.text = Mathf.RoundToInt(musicSlider.value * 100f) + "%";
        sfxLabel.text = Mathf.RoundToInt(sfxSlider.value * 100f) + "%";
        sensitivityLabel.text = sensitivitySlider.value.ToString("0.0") + "x";
    }

    // ---------- Construcción de la interfaz ----------

    static OptionsPanel Build()
    {
        Canvas canvas = UITheme.MakeCanvas("OptionsPanel", 500);
        OptionsPanel panel = canvas.gameObject.AddComponent<OptionsPanel>();

        GameObject overlay = UITheme.Overlay(canvas.transform, "Fondo", UITheme.Dim);

        RectTransform card = UITheme.CenteredCard(overlay.transform, 1040f, UITheme.Accent);
        UITheme.VLayout(card.gameObject, 12f, new RectOffset(56, 56, 48, 44));

        TMP_Text title = UITheme.Label(card, "Titulo", "OPCIONES", 70f, UITheme.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
        title.characterSpacing = 12f;
        UITheme.Outline(title, 0.25f);
        UITheme.Size(title.gameObject, height: 96f);

        UITheme.Divider(card, UITheme.Accent);

        RectTransform gap = UITheme.NewRect("Espacio", card);
        UITheme.Size(gap.gameObject, height: 10f);

        panel.volumeSlider = Row(card, "Volumen general", 0f, 1f, out panel.volumeLabel);
        panel.musicSlider = Row(card, "Música", 0f, 1f, out panel.musicLabel);
        panel.sfxSlider = Row(card, "Efectos de sonido", 0f, 1f, out panel.sfxLabel);
        panel.sensitivitySlider = Row(card, "Sensibilidad", GameSettings.MinSensitivity, GameSettings.MaxSensitivity, out panel.sensitivityLabel);

        panel.volumeSlider.onValueChanged.AddListener(value =>
        {
            GameSettings.Volume = value;
            panel.UpdateLabels();
        });
        panel.musicSlider.onValueChanged.AddListener(value =>
        {
            GameSettings.MusicVolume = value;
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

        TMP_Text label = UITheme.Label(row, "Nombre", name, 29f, UITheme.TextMain, TextAlignmentOptions.MidlineLeft);
        UITheme.Size(label.gameObject, 410f, 72f);

        Slider slider = UITheme.MakeSlider(row, min, max);

        valueLabel = UITheme.Label(row, "Valor", "", 30f, UITheme.Accent, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        UITheme.Size(valueLabel.gameObject, 110f, 72f);

        return slider;
    }
}
