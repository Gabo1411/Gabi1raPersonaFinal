using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum ButtonStyle { Primary, Secondary, Danger }

// Estilo visual común de toda la interfaz (colores, sprites redondeados y constructores de elementos).
public static class UITheme
{
    public static readonly Color Dim = new Color(0.02f, 0.02f, 0.035f, 0.8f);
    public static readonly Color Card = new Color(0.1f, 0.1f, 0.13f, 0.97f);
    public static readonly Color CardLight = new Color(0.17f, 0.17f, 0.21f, 1f);
    public static readonly Color Hud = new Color(0.03f, 0.03f, 0.05f, 0.62f);
    public static readonly Color Track = new Color(0.04f, 0.04f, 0.06f, 0.95f);
    public static readonly Color Accent = new Color(0.93f, 0.71f, 0.28f, 1f);
    public static readonly Color Danger = new Color(0.86f, 0.25f, 0.22f, 1f);
    public static readonly Color Success = new Color(0.36f, 0.8f, 0.44f, 1f);
    public static readonly Color TextMain = new Color(0.95f, 0.94f, 0.92f, 1f);
    public static readonly Color TextDim = new Color(0.67f, 0.67f, 0.74f, 1f);

    static Sprite rounded, roundedSmall;

    public static Sprite Rounded => rounded != null ? rounded : (rounded = MakeRounded(64, 18f, 22f));
    public static Sprite RoundedSmall => roundedSmall != null ? roundedSmall : (roundedSmall = MakeRounded(32, 8f, 10f));

    // Rectángulo redondeado con bordes suaves, listo para usarse como Image "Sliced"
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

    public static Image Box(Transform parent, string name, Color color, bool small = false)
    {
        RectTransform rect = NewRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = small ? RoundedSmall : Rounded;
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

    public static TMP_Text Label(Transform parent, string name, string text, float size, Color color,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center, FontStyles style = FontStyles.Normal)
    {
        RectTransform rect = NewRect(name, parent);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
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

    // Fondo oscuro a pantalla completa que bloquea los clics y aparece con un fundido
    public static GameObject Overlay(Transform parent, string name, Color color)
    {
        Image dim = Box(parent, name, color);
        dim.sprite = null;
        dim.type = Image.Type.Simple;
        dim.raycastTarget = true;
        Stretch(dim.rectTransform);
        dim.gameObject.AddComponent<CanvasGroup>();
        dim.gameObject.AddComponent<UIFade>();
        return dim.gameObject;
    }

    // Tarjeta centrada de ancho fijo y alto automático, con una franja de color arriba
    public static RectTransform CenteredCard(Transform parent, float width, Color accent)
    {
        Image card = Box(parent, "Tarjeta", Card);
        Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, 0f));

        ContentSizeFitter fitter = card.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image bar = Box(card.transform, "Franja", accent, true);
        bar.rectTransform.anchorMin = new Vector2(0f, 1f);
        bar.rectTransform.anchorMax = new Vector2(1f, 1f);
        bar.rectTransform.pivot = new Vector2(0.5f, 1f);
        bar.rectTransform.sizeDelta = new Vector2(-48f, 6f);
        bar.rectTransform.anchoredPosition = new Vector2(0f, -4f);
        bar.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        return card.rectTransform;
    }

    public static Button MakeButton(Transform parent, string text, UnityAction onClick, ButtonStyle style = ButtonStyle.Secondary, float height = 68f)
    {
        Color normal, hover, pressed, textColor;
        switch (style)
        {
            case ButtonStyle.Primary:
                normal = Accent; hover = new Color(1f, 0.8f, 0.4f); pressed = new Color(0.75f, 0.56f, 0.2f); textColor = new Color(0.1f, 0.08f, 0.04f);
                break;
            case ButtonStyle.Danger:
                normal = new Color(0.5f, 0.15f, 0.14f); hover = new Color(0.7f, 0.22f, 0.2f); pressed = new Color(0.4f, 0.11f, 0.1f); textColor = TextMain;
                break;
            default:
                normal = CardLight; hover = new Color(0.3f, 0.3f, 0.37f); pressed = new Color(0.13f, 0.13f, 0.17f); textColor = TextMain;
                break;
        }

        Image image = Box(parent, "Boton " + text, Color.white);
        image.raycastTarget = true;

        Button button = image.gameObject.AddComponent<Button>();
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

        TMP_Text label = Label(image.transform, "Texto", text, 34f, textColor, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(label.rectTransform);

        Size(image.gameObject, height: height);
        return button;
    }

    public static Slider MakeSlider(Transform parent, float min, float max)
    {
        RectTransform root = NewRect("Slider", parent);
        Size(root.gameObject, 420f, 40f, 1f);

        Image track = Box(root, "Pista", Track, true);
        track.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        track.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        track.rectTransform.offsetMin = new Vector2(0f, -7f);
        track.rectTransform.offsetMax = new Vector2(0f, 7f);

        RectTransform fillArea = NewRect("AreaRelleno", root);
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.offsetMin = new Vector2(7f, -7f);
        fillArea.offsetMax = new Vector2(-7f, 7f);

        Image fill = Box(fillArea, "Relleno", Accent, true);
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

        RectTransform handleArea = NewRect("AreaManija", root);
        handleArea.anchorMin = new Vector2(0f, 0.5f);
        handleArea.anchorMax = new Vector2(1f, 0.5f);
        handleArea.offsetMin = new Vector2(15f, -15f);
        handleArea.offsetMax = new Vector2(-15f, 15f);

        Image handle = Icon(handleArea, "Manija", BuffIcons.Circle, Color.white);
        handle.rectTransform.sizeDelta = new Vector2(30f, 0f);
        handle.raycastTarget = true;

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
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
    const float Duration = 0.18f;

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
