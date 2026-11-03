using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum ButtonStyle { Primary, Secondary, Danger }

// Estilo visual de toda la interfaz: cripta gótica con piedra oscura, bronce y brasas.
// Tipografía Cinzel (SIL OFL) para títulos, números y botones.
public static class UITheme
{
    public static readonly Color Dim = new Color(0.01f, 0.008f, 0.015f, 0.8f);
    public static readonly Color Hud = new Color(1f, 1f, 1f, 0.94f);
    public static readonly Color Track = new Color(0.025f, 0.02f, 0.03f, 0.96f);
    public static readonly Color Bronze = new Color(0.74f, 0.55f, 0.28f, 1f);
    public static readonly Color Accent = new Color(0.97f, 0.7f, 0.27f, 1f);
    public static readonly Color Danger = new Color(0.86f, 0.2f, 0.18f, 1f);
    public static readonly Color Success = new Color(0.5f, 0.8f, 0.4f, 1f);
    public static readonly Color TextMain = new Color(0.94f, 0.9f, 0.8f, 1f);
    public static readonly Color TextDim = new Color(0.68f, 0.6f, 0.5f, 1f);

    static Sprite rounded, roundedSmall;
    static TMP_FontAsset displayFont;
    static bool fontSearched;

    public static Sprite RoundedSmall => roundedSmall != null ? roundedSmall : (roundedSmall = MakeRounded(32, 8f, 10f));

    // Fuente decorativa (Cinzel). Si no se encuentra se usa la fuente por defecto de TextMeshPro.
    public static TMP_FontAsset DisplayFont
    {
        get
        {
            if (displayFont == null && !fontSearched)
            {
                fontSearched = true;
                displayFont = Resources.Load<TMP_FontAsset>("UI/Cinzel SDF");
            }
            return displayFont;
        }
    }

    // Rectángulo redondeado para barras y relleno, listo para usarse como Image "Sliced"
    static Sprite MakeRounded(int size, float radius, float border)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;

        float half = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f - half, py = y + 0.5f - half;
                float qx = Mathf.Abs(px) - (half - radius), qy = Mathf.Abs(py) - (half - radius);
                float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
                float alpha = Mathf.Clamp01(0.5f - (outside + inside - radius));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    // ---------- Estructura ----------

    public static Canvas MakeCanvas(string name, int sortingOrder)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static void Stretch(RectTransform rect, float margin = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(margin, margin);
        rect.offsetMax = new Vector2(-margin, -margin);
    }

    public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    // ---------- Elementos ----------

    // Panel de piedra con marco de bronce. Con small = true es una barra redondeada lisa (pistas y rellenos).
    public static Image Box(Transform parent, string name, Color color, bool small = false)
    {
        RectTransform rect = NewRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = small ? RoundedSmall : UISprites.Frame;
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static Image Icon(Transform parent, string name, Sprite sprite, Color color)
    {
        RectTransform rect = NewRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    // Texto. Por defecto usa la fuente decorativa; display = false para textos largos y de ayuda.
    public static TMP_Text Label(Transform parent, string name, string text, float size, Color color,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center, FontStyles style = FontStyles.Normal, bool display = true)
    {
        RectTransform rect = NewRect(name, parent);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (display && DisplayFont != null) label.font = DisplayFont;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.fontStyle = style;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        return label;
    }

    // Contorno oscuro para dar peso a títulos y números sobre cualquier fondo
    public static TMP_Text Outline(TMP_Text label, float width = 0.22f)
    {
        label.outlineWidth = width;
        label.outlineColor = new Color32(8, 4, 6, 255);
        return label;
    }

    public static Image Divider(Transform parent, Color color, float width = 420f, float height = 26f)
    {
        Image divider = Icon(parent, "Separador", UISprites.Divider, color);
        Size(divider.gameObject, width, height);
        return divider;
    }

    public static VerticalLayoutGroup VLayout(GameObject target, float spacing, RectOffset padding, TextAnchor alignment = TextAnchor.UpperCenter)
    {
        VerticalLayoutGroup layout = target.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = padding;
        layout.childAlignment = alignment;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return layout;
    }

    public static HorizontalLayoutGroup HLayout(GameObject target, float spacing, RectOffset padding, TextAnchor alignment = TextAnchor.MiddleLeft, bool expandWidth = false)
    {
        HorizontalLayoutGroup layout = target.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = padding;
        layout.childAlignment = alignment;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = expandWidth;
        layout.childForceExpandHeight = false;
        return layout;
    }

    public static LayoutElement Size(GameObject target, float width = -1f, float height = -1f, float flexibleWidth = -1f)
    {
        LayoutElement element = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
        if (width >= 0f) element.preferredWidth = width;
        if (height >= 0f) { element.preferredHeight = height; element.minHeight = height; }
        if (flexibleWidth >= 0f) element.flexibleWidth = flexibleWidth;
        return element;
    }

    // Fondo oscuro a pantalla completa con viñeta y brasas; bloquea los clics y aparece con un fundido
    public static GameObject Overlay(Transform parent, string name, Color color, bool embers = true)
    {
        RectTransform root = NewRect(name, parent);
        Stretch(root);

        Image dim = root.gameObject.AddComponent<Image>();
        dim.color = color;
        dim.raycastTarget = true;
        root.gameObject.AddComponent<CanvasGroup>();
        root.gameObject.AddComponent<UIFade>();

        Image vignette = Icon(root, "Vineta", UISprites.Vignette, Color.white);
        Stretch(vignette.rectTransform);

        if (embers)
        {
            RectTransform field = NewRect("Brasas", root);
            Stretch(field);
            field.gameObject.AddComponent<EmberField>();
        }
        return root.gameObject;
    }

    // Tarjeta centrada de ancho fijo y alto automático, con un resplandor del color de acento detrás
    public static RectTransform CenteredCard(Transform parent, float width, Color accent)
    {
        Image card = Box(parent, "Tarjeta", Color.white);
        Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, 0f));

        ContentSizeFitter fitter = card.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // El resplandor se crea antes de la tarjeta en la jerarquía para quedar detrás
        Image glow = Icon(parent, "Resplandor", BuffIcons.Glow, new Color(accent.r, accent.g, accent.b, 0.3f));
        glow.transform.SetSiblingIndex(card.transform.GetSiblingIndex());
        Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width + 300f, 700f));
        MatchSize match = glow.gameObject.AddComponent<MatchSize>();
        match.target = card.rectTransform;
        match.padding = 360f;

        return card.rectTransform;
    }

    public static Button MakeButton(Transform parent, string text, UnityAction onClick, ButtonStyle style = ButtonStyle.Secondary, float height = 68f)
    {
        Color normal, hover, pressed, textColor;
        switch (style)
        {
            case ButtonStyle.Primary:
                normal = new Color(0.78f, 0.42f, 0.14f); hover = new Color(1f, 0.62f, 0.2f); pressed = new Color(0.55f, 0.28f, 0.09f);
                textColor = new Color(1f, 0.96f, 0.86f);
                break;
            case ButtonStyle.Danger:
                normal = new Color(0.5f, 0.12f, 0.12f); hover = new Color(0.78f, 0.2f, 0.18f); pressed = new Color(0.36f, 0.08f, 0.08f);
                textColor = new Color(1f, 0.9f, 0.86f);
                break;
            default:
                normal = new Color(0.3f, 0.28f, 0.33f); hover = new Color(0.5f, 0.44f, 0.38f); pressed = new Color(0.2f, 0.18f, 0.22f);
                textColor = TextMain;
                break;
        }

        RectTransform rect = NewRect("Boton " + text, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = UISprites.Slab;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.raycastTarget = true;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = hover;
        colors.pressedColor = pressed;
        colors.selectedColor = normal;
        colors.disabledColor = new Color(0.3f, 0.3f, 0.33f, 0.6f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        TMP_Text label = Label(rect, "Texto", text.ToUpperInvariant(), 31f, textColor, TextAlignmentOptions.Center, FontStyles.Bold);
        label.characterSpacing = 6f;
        Outline(label, 0.18f);
        Stretch(label.rectTransform);

        Size(rect.gameObject, height: height);
        return button;
    }

    public static Slider MakeSlider(Transform parent, float min, float max)
    {
        RectTransform root = NewRect("Slider", parent);
        Size(root.gameObject, 420f, 40f, 1f);

        Image track = Box(root, "Pista", Track, true);
        track.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        track.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        track.rectTransform.offsetMin = new Vector2(0f, -8f);
        track.rectTransform.offsetMax = new Vector2(0f, 8f);

        RectTransform fillArea = NewRect("AreaRelleno", root);
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.offsetMin = new Vector2(10f, -5f);
        fillArea.offsetMax = new Vector2(-10f, 5f);

        Image fill = Box(fillArea, "Relleno", Accent, true);
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

        RectTransform handleArea = NewRect("AreaManija", root);
        handleArea.anchorMin = new Vector2(0f, 0.5f);
        handleArea.anchorMax = new Vector2(1f, 0.5f);
        handleArea.offsetMin = new Vector2(18f, -18f);
        handleArea.offsetMax = new Vector2(-18f, 18f);

        // Manija en forma de rombo con borde de bronce
        Image handleBorder = Icon(handleArea, "Manija", BuffIcons.Diamond, Bronze);
        handleBorder.rectTransform.sizeDelta = new Vector2(36f, 0f);
        handleBorder.raycastTarget = true;

        Image handleCore = Icon(handleBorder.transform, "Nucleo", BuffIcons.Diamond, new Color(0.96f, 0.9f, 0.76f));
        Place(handleCore.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f));

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handleBorder.rectTransform;
        slider.targetGraphic = handleBorder;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        return slider;
    }
}

// Aparición suave de un panel (funciona con el juego en pausa porque usa tiempo sin escala)
[RequireComponent(typeof(CanvasGroup))]
public class UIFade : MonoBehaviour
{
    const float Duration = 0.22f;

    private CanvasGroup group;
    private float elapsed;

    void OnEnable()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        elapsed = 0f;
    }

    void Update()
    {
        if (group.alpha >= 1f) return;

        elapsed += Time.unscaledDeltaTime;
        group.alpha = Mathf.SmoothStep(0f, 1f, elapsed / Duration);
    }
}
