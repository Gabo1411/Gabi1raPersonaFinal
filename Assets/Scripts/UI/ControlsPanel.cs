using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Panel de controles, usado tanto en el menú principal como en la pausa.
public class ControlsPanel : MonoBehaviour
{
    static ControlsPanel instance;
    static int closedFrame = -1;

    public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

    // Verdadero mientras el panel está abierto o justo el frame en que se cerró con Escape
    public static bool BlocksInput => IsOpen || closedFrame == Time.frameCount;

    Action onClose;

    static readonly (string[] keys, string action)[] Rows =
    {
        (new[] { "W", "A", "S", "D" }, "Moverse (también con las flechas)"),
        (new[] { "Mouse" }, "Apuntar"),
        (new[] { "Click izquierdo" }, "Disparar (mantén para fuego automático)"),
        (new[] { "Click derecho" }, "Mantener: francotirador (dispara el cristal)"),
        (new[] { "R" }, "Recargar"),
        (new[] { "Shift" }, "Dash"),
        (new[] { "Espacio" }, "Saltar"),
        (new[] { "Ctrl", "C" }, "Agacharse (mantén; vas más lento)"),
        (new[] { "F" }, "Linterna"),
        (new[] { "Esc" }, "Pausa")
    };

    public static void Open(Action onClose = null)
    {
        if (instance == null) instance = Build();

        instance.onClose = onClose;
        instance.gameObject.SetActive(true);
    }

    public static void Close()
    {
        if (!IsOpen) return;

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

    static ControlsPanel Build()
    {
        Canvas canvas = UITheme.MakeCanvas("ControlsPanel", 400);
        ControlsPanel panel = canvas.gameObject.AddComponent<ControlsPanel>();

        GameObject overlay = UITheme.Overlay(canvas.transform, "Fondo", UITheme.Dim);

        RectTransform card = UITheme.CenteredCard(overlay.transform, 1040f, UITheme.Accent);
        UITheme.VLayout(card.gameObject, 10f, new RectOffset(56, 56, 48, 44));

        TMP_Text title = UITheme.Label(card, "Titulo", "CONTROLES", 70f, UITheme.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
        title.characterSpacing = 12f;
        UITheme.Outline(title, 0.25f);
        UITheme.Size(title.gameObject, height: 96f);

        UITheme.Divider(card, UITheme.Accent);

        RectTransform gap = UITheme.NewRect("Espacio", card);
        UITheme.Size(gap.gameObject, height: 8f);

        foreach (var row in Rows) BuildRow(card, row.keys, row.action);

        RectTransform gap2 = UITheme.NewRect("Espacio", card);
        UITheme.Size(gap2.gameObject, height: 12f);

        UITheme.MakeButton(card, "Volver", Close, ButtonStyle.Primary, 70f);

        canvas.gameObject.SetActive(false);
        return panel;
    }

    static void BuildRow(Transform parent, string[] keys, string action)
    {
        RectTransform row = UITheme.NewRect("Fila " + action, parent);
        UITheme.HLayout(row.gameObject, 24f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleLeft);
        UITheme.Size(row.gameObject, height: 54f);

        RectTransform keyCell = UITheme.NewRect("Teclas", row);
        UITheme.HLayout(keyCell.gameObject, 8f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleLeft);
        UITheme.Size(keyCell.gameObject, 360f, 54f);

        foreach (string key in keys) KeyCap(keyCell, key);

        TMP_Text description = UITheme.Label(row, "Descripcion", action, 28f, UITheme.TextMain, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, false);
        UITheme.Size(description.gameObject, height: 54f, flexibleWidth: 1f);
    }

    static void KeyCap(Transform parent, string text)
    {
        Image cap = UITheme.Box(parent, "Tecla " + text, new Color(0.34f, 0.27f, 0.2f), true);
        cap.sprite = UISprites.Slab;
        UITheme.HLayout(cap.gameObject, 0f, new RectOffset(20, 20, 6, 6), TextAnchor.MiddleCenter);
        UITheme.Size(cap.gameObject, height: 48f);

        TMP_Text label = UITheme.Label(cap.transform, "Texto", text, 27f, new Color(1f, 0.93f, 0.78f), TextAlignmentOptions.Center, FontStyles.Bold);
        UITheme.Size(label.gameObject, height: 38f);
    }
}
