using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Consola de depuración dentro del juego. Se abre con la tecla ` (la de la izquierda del 1) o con F1.
// Solo existe en el editor y en builds de desarrollo ("Development Build"): en una build normal no se crea.
//
//  - Escribe "help" para ver todos los comandos, "help <comando>" para el detalle de uno.
//  - Tab autocompleta comandos y opciones, ↑ ↓ recorren el historial, RePág / AvPág o la rueda desplazan el registro.
//  - Se pueden encadenar comandos con ";" y pegar varias líneas con Ctrl+V.
public class DebugConsole : MonoBehaviour
{
    public static DebugConsole Instance { get; private set; }

    // Verdadero con la consola abierta (o el mismo fotograma en que se cerró con Escape): el juego ignora el teclado y el ratón
    public static bool IsOpen => Instance != null && Instance.open;
    public static bool BlocksInput => Instance != null && (Instance.open || Instance.closedFrame == Time.frameCount);

    public static bool Enabled => Application.isEditor || Debug.isDebugBuild;

    struct Entry
    {
        public string text;
        public Color color;
    }

    struct UnityLogEntry
    {
        public string text;
        public LogType type;
    }

    static readonly List<UnityLogEntry> unityLog = new List<UnityLogEntry>();
    static bool showErrors = true;
    static readonly List<Entry> pending = new List<Entry>();

    // ---------- Arranque ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void HookLog()
    {
        if (!Enabled) return;
        Application.logMessageReceived -= OnUnityLog;
        Application.logMessageReceived += OnUnityLog;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!Enabled || Instance != null) return;

        GameObject go = new GameObject("ConsolaDebug");
        DontDestroyOnLoad(go);
        go.AddComponent<DebugConsole>();
    }

    static void OnUnityLog(string message, string stack, LogType type)
    {
        unityLog.Add(new UnityLogEntry { text = message, type = type });
        if (unityLog.Count > 400) unityLog.RemoveAt(0);

        if (showErrors && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
        {
            string firstLine = message.Split('\n')[0];
            Print("[error] " + firstLine, Palette.Error);
        }
    }

    // ---------- API pública ----------

    public static void Print(string text, Color? color = null)
    {
        Entry entry = new Entry { text = text, color = color ?? Palette.Normal };
        if (Instance != null) Instance.Add(entry);
        else pending.Add(entry);
    }

    public static void Clear()
    {
        if (Instance != null) { Instance.entries.Clear(); Instance.scroll = 0; Instance.dirty = true; }
    }

    public static void SetShowErrors(bool value) => showErrors = value;
    public static bool ShowErrors => showErrors;

    public static string RecentUnityLog(int count)
    {
        StringBuilder sb = new StringBuilder();
        int start = Mathf.Max(0, unityLog.Count - count);
        for (int i = start; i < unityLog.Count; i++)
        {
            UnityLogEntry e = unityLog[i];
            string tag = e.type == LogType.Log ? "" : "[" + e.type + "] ";
            sb.Append(tag).Append(e.text.Split('\n')[0]);
            if (i < unityLog.Count - 1) sb.Append('\n');
        }
        return sb.Length == 0 ? "(sin mensajes)" : sb.ToString();
    }

    static class Palette
    {
        public static readonly Color Normal = new Color(0.9f, 0.86f, 0.76f);
        public static readonly Color Dim = new Color(0.6f, 0.55f, 0.48f);
        public static readonly Color Accent = new Color(0.97f, 0.7f, 0.27f);
        public static readonly Color Error = new Color(1f, 0.38f, 0.33f);
        public static readonly Color Ok = new Color(0.55f, 0.85f, 0.5f);
    }

    // ---------- Estado ----------

    private bool open;
    private int closedFrame = -1;
    private int openedFrame = -1;

    private readonly List<Entry> entries = new List<Entry>();
    private readonly List<string> history = new List<string>();
    private int historyIndex = -1;
    private string draft = "";

    private string input = "";
    private int caret;
    private int scroll;
    private bool dirty = true;
    private float lastTabTime;

    private Canvas canvas;
    private TMP_Text logText, inputText;
    private RectTransform panel;
    private bool monoFont;
    private CursorLockMode previousLock;
    private bool previousVisible;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        BuildUI();
        canvas.gameObject.SetActive(false);

        foreach (Entry e in pending) entries.Add(e);
        pending.Clear();

        Print("Consola debug. Escribe help para ver los comandos.", Palette.Accent);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------- Interfaz ----------

    void BuildUI()
    {
        canvas = UITheme.MakeCanvas("ConsolaDebugUI", 5000);
        canvas.transform.SetParent(transform, false);

        panel = UITheme.NewRect("Panel", canvas.transform);
        panel.anchorMin = new Vector2(0f, 1f);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(0f, 600f);

        Image background = panel.gameObject.AddComponent<Image>();
        background.color = new Color(0.025f, 0.018f, 0.03f, 0.94f);
        background.raycastTarget = true;

        Image edge = UITheme.Icon(panel, "Borde", null, UITheme.Bronze);
        edge.rectTransform.anchorMin = new Vector2(0f, 0f);
        edge.rectTransform.anchorMax = new Vector2(1f, 0f);
        edge.rectTransform.pivot = new Vector2(0.5f, 0f);
        edge.rectTransform.sizeDelta = new Vector2(0f, 4f);
        edge.rectTransform.anchoredPosition = Vector2.zero;

        TMP_FontAsset mono = TryCreateMonoFont();
        monoFont = mono != null;

        // Título y ayuda de teclas
        TMP_Text title = UITheme.Label(panel, "Titulo", "CONSOLA DEBUG", 26f, UITheme.Accent, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        UITheme.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -14f), new Vector2(500f, 40f));
        title.characterSpacing = 6f;

        TMP_Text hint = UITheme.Label(panel, "Ayuda", "Tab autocompleta   ↑↓ historial   RePág/AvPág desplaza   Ctrl+L limpia   ` cierra",
            20f, UITheme.TextDim, TextAlignmentOptions.TopRight, FontStyles.Normal, false);
        UITheme.Place(hint.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -18f), new Vector2(1100f, 34f));

        // Registro
        RectTransform logArea = UITheme.NewRect("Registro", panel);
        logArea.anchorMin = Vector2.zero;
        logArea.anchorMax = Vector2.one;
        logArea.offsetMin = new Vector2(28f, 84f);
        logArea.offsetMax = new Vector2(-28f, -60f);
        logArea.gameObject.AddComponent<RectMask2D>();

        logText = UITheme.Label(logArea, "Texto", "", 24f, Palette.Normal, TextAlignmentOptions.BottomLeft, FontStyles.Normal, false);
        UITheme.Stretch(logText.rectTransform);
        logText.textWrappingMode = TextWrappingModes.Normal;
        logText.richText = true;
        if (mono != null) logText.font = mono;

        // Línea de entrada
        TMP_Text prompt = UITheme.Label(panel, "Prompt", "›", 34f, UITheme.Accent, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        UITheme.Place(prompt.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 20f), new Vector2(40f, 52f));

        inputText = UITheme.Label(panel, "Entrada", "", 28f, UITheme.TextMain, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, false);
        inputText.rectTransform.anchorMin = new Vector2(0f, 0f);
        inputText.rectTransform.anchorMax = new Vector2(1f, 0f);
        inputText.rectTransform.pivot = new Vector2(0f, 0f);
        inputText.rectTransform.anchoredPosition = new Vector2(70f, 20f);
        inputText.rectTransform.sizeDelta = new Vector2(-100f, 52f);
        inputText.richText = true;
        inputText.textWrappingMode = TextWrappingModes.NoWrap;
        if (mono != null) inputText.font = mono;
    }

    // Fuente de ancho fijo del sistema (para que las columnas queden alineadas). Si no hay, se usa la de TextMeshPro.
    static TMP_FontAsset TryCreateMonoFont()
    {
        try
        {
            Font os = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Lucida Console", "Courier New", "Menlo", "DejaVu Sans Mono" }, 28);
            if (os == null) return null;
            return TMP_FontAsset.CreateFontAsset(os);
        }
        catch (Exception)
        {
            return null;
        }
    }

    void Add(Entry entry)
    {
        entries.Add(entry);
        if (entries.Count > 600) entries.RemoveAt(0);
        if (scroll > 0) scroll = Mathf.Min(scroll + 1, entries.Count - 1);
        dirty = true;
    }

    static string ColorTag(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

    void Refresh()
    {
        dirty = false;

        const int visibleEntries = 90;
        int end = Mathf.Max(0, entries.Count - scroll);
        int start = Mathf.Max(0, end - visibleEntries);

        StringBuilder sb = new StringBuilder();
        for (int i = start; i < end; i++)
        {
            Entry e = entries[i];
            sb.Append("<color=").Append(ColorTag(e.color)).Append("><noparse>").Append(e.text).Append("</noparse></color>");
            if (i < end - 1) sb.Append('\n');
        }
        if (scroll > 0) sb.Append("\n<color=").Append(ColorTag(Palette.Dim)).Append(">▼ desplazado ").Append(scroll).Append(" líneas (AvPág para bajar)</color>");
        logText.text = sb.ToString();
    }

    void RefreshInput()
    {
        caret = Mathf.Clamp(caret, 0, input.Length);
        bool blink = Mathf.Repeat(Time.unscaledTime, 1f) < 0.6f;
        string before = input.Substring(0, caret);
        string after = input.Substring(caret);
        string cursor = blink ? "<color=" + ColorTag(Palette.Accent) + ">|</color>" : "<color=#00000000>|</color>";
        inputText.text = "<noparse>" + before + "</noparse>" + cursor + "<noparse>" + after + "</noparse>";
    }

    // ---------- Abrir y cerrar ----------

    void Open()
    {
        if (open) return;
        open = true;
        openedFrame = Time.frameCount;

        previousLock = Cursor.lockState;
        previousVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        canvas.gameObject.SetActive(true);
        if (Keyboard.current != null) Keyboard.current.onTextInput += OnText;
        dirty = true;
    }

    void Close(bool byEscape)
    {
        if (!open) return;
        open = false;
        if (byEscape) closedFrame = Time.frameCount;

        if (Keyboard.current != null) Keyboard.current.onTextInput -= OnText;
        canvas.gameObject.SetActive(false);

        // Si el juego está en pausa o terminado el cursor sigue libre; si no, vuelve a quedar bloqueado
        bool gameWantsCursor = GameManager.Instance != null && (GameManager.Instance.GameEnded || Time.timeScale == 0f);
        if (previousLock == CursorLockMode.Locked && !gameWantsCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisible;
        }
    }

    // ---------- Entrada ----------

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.backquoteKey.wasPressedThisFrame || kb.f1Key.wasPressedThisFrame)
        {
            if (open) Close(false); else Open();
            return;
        }

        if (!open) return;

        if (kb.escapeKey.wasPressedThisFrame)
        {
            if (input.Length > 0) { input = ""; caret = 0; }
            else Close(true);
            return;
        }

        bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;

        if (kb.upArrowKey.wasPressedThisFrame) NavigateHistory(-1);
        if (kb.downArrowKey.wasPressedThisFrame) NavigateHistory(1);
        if (kb.leftArrowKey.wasPressedThisFrame) caret = Mathf.Max(0, caret - 1);
        if (kb.rightArrowKey.wasPressedThisFrame) caret = Mathf.Min(input.Length, caret + 1);
        if (kb.homeKey.wasPressedThisFrame) caret = 0;
        if (kb.endKey.wasPressedThisFrame) caret = input.Length;
        if (kb.tabKey.wasPressedThisFrame) Autocomplete();

        if (kb.pageUpKey.wasPressedThisFrame) ScrollBy(8);
        if (kb.pageDownKey.wasPressedThisFrame) ScrollBy(-8);
        if (Mouse.current != null)
        {
            float wheel = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) > 0.1f) ScrollBy(wheel > 0f ? 3 : -3);
        }

        if (ctrl && kb.lKey.wasPressedThisFrame) Clear();
        if (ctrl && kb.vKey.wasPressedThisFrame) Paste(GUIUtility.systemCopyBuffer);

        if (dirty) Refresh();
        RefreshInput();
    }

    void ScrollBy(int lines)
    {
        scroll = Mathf.Clamp(scroll + lines, 0, Mathf.Max(0, entries.Count - 1));
        dirty = true;
    }

    // Texto que llega del teclado (ya con la distribución del usuario aplicada)
    void OnText(char c)
    {
        if (!open || Time.frameCount == openedFrame) return;
        caret = Mathf.Clamp(caret, 0, input.Length);
        if (c == '`' || c == 'º' || c == 'ª') return;       // teclas que abren y cierran la consola

        switch (c)
        {
            case '\b':
                if (caret > 0) { input = input.Remove(caret - 1, 1); caret--; }
                return;
            case (char)127:
                if (caret < input.Length) input = input.Remove(caret, 1);
                return;
            case '\n':
            case '\r':
                Submit();
                return;
            case '\t':
                return;
        }

        if (char.IsControl(c)) return;
        input = input.Insert(caret, c.ToString());
        caret++;
        historyIndex = -1;
    }

    void Paste(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        string[] lines = text.Replace("\r", "").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            input = input.Insert(caret, lines[i]);
            caret += lines[i].Length;

            // Cada salto de línea del texto pegado ejecuta lo escrito hasta ahí
            if (i < lines.Length - 1) Submit();
        }
    }

    void NavigateHistory(int direction)
    {
        if (history.Count == 0) return;

        if (historyIndex == -1)
        {
            if (direction > 0) return;
            draft = input;
            historyIndex = history.Count - 1;
        }
        else
        {
            historyIndex += direction;
            if (historyIndex >= history.Count) { historyIndex = -1; input = draft; caret = input.Length; return; }
            historyIndex = Mathf.Max(0, historyIndex);
        }
        input = history[historyIndex];
        caret = input.Length;
    }

    void Autocomplete()
    {
        List<string> candidates = CommandRegistry.Complete(input.Substring(0, caret), out string prefix);
        if (candidates.Count == 0) return;

        string replacement;
        if (candidates.Count == 1) replacement = candidates[0] + " ";
        else
        {
            replacement = CommonPrefix(candidates);
            // Si no se puede avanzar más, se muestran las opciones
            if (replacement.Length <= prefix.Length && Time.unscaledTime - lastTabTime > 0.05f)
            {
                Add(new Entry { text = string.Join("   ", candidates), color = Palette.Dim });
            }
        }
        lastTabTime = Time.unscaledTime;

        int start = caret - prefix.Length;
        input = input.Remove(start, prefix.Length).Insert(start, replacement);
        caret = start + replacement.Length;
    }

    static string CommonPrefix(List<string> values)
    {
        string prefix = values[0];
        foreach (string v in values)
        {
            int n = 0;
            while (n < prefix.Length && n < v.Length && char.ToLowerInvariant(prefix[n]) == char.ToLowerInvariant(v[n])) n++;
            prefix = prefix.Substring(0, n);
        }
        return prefix;
    }

    // ---------- Ejecución ----------

    void Submit()
    {
        string line = input.Trim();
        input = "";
        caret = 0;
        historyIndex = -1;
        scroll = 0;
        if (line.Length == 0) return;

        if (history.Count == 0 || history[history.Count - 1] != line) history.Add(line);
        if (history.Count > 100) history.RemoveAt(0);

        Add(new Entry { text = "› " + line, color = Palette.Accent });

        // Varios comandos en una línea separados por ';'
        foreach (string part in line.Split(';'))
        {
            string command = part.Trim();
            if (command.Length == 0) continue;

            string result = CommandRegistry.Execute(command, out bool isError);
            if (string.IsNullOrEmpty(result)) continue;

            Color color = isError ? Palette.Error : (result == "ok" ? Palette.Ok : Palette.Normal);
            foreach (string resultLine in result.Split('\n')) Add(new Entry { text = resultLine, color = color });
        }
    }
}
