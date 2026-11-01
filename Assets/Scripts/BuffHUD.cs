using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Iconos a un lado de la pantalla con los buffs activos de las pociones y el tiempo que les queda.
public class BuffHUD : MonoBehaviour
{
    class Slot
    {
        public BuffType type;
        public GameObject root;
        public Image timerFill;
        public Image icon;
        public TMP_Text seconds;
    }

    const float SlotSize = 84f;
    const float BlinkTime = 1.5f;

    static readonly Color SpeedColor = new Color(0.2f, 0.7f, 1f, 1f);
    static readonly Color DamageColor = new Color(1f, 0.3f, 0.2f, 1f);
    static readonly Color EliteSpeedColor = new Color(0.3f, 1f, 0.95f, 1f);
    static readonly Color EliteDamageColor = new Color(1f, 0.55f, 0.1f, 1f);
    static readonly Color RegenColor = new Color(0.3f, 1f, 0.4f, 1f);
    static readonly Color ContinuousColor = new Color(0.85f, 0.35f, 1f, 1f);
    static readonly Color EliteRingColor = new Color(1f, 0.82f, 0.2f, 1f);

    PlayerMovement player;
    Slot[] slots;

    public static void Create(PlayerMovement player)
    {
        if (player == null) return;

        GameObject root = new GameObject("BuffHUD", typeof(RectTransform));

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        BuffHUD hud = root.AddComponent<BuffHUD>();
        hud.player = player;
        hud.Build();
    }

    void Build()
    {
        GameObject column = new GameObject("Buffs", typeof(RectTransform));
        column.transform.SetParent(transform, false);

        RectTransform columnRect = (RectTransform)column.transform;
        columnRect.anchorMin = columnRect.anchorMax = new Vector2(0f, 0.5f);
        columnRect.pivot = new Vector2(0f, 0.5f);
        columnRect.anchoredPosition = new Vector2(30f, 0f);

        VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 18f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = column.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Los mejorados llevan un aro dorado y un símbolo distinto al de los comunes
        slots = new[]
        {
            CreateSlot(column.transform, BuffType.Velocidad, "Velocidad", BuffIcons.Bolt, SpeedColor, false),
            CreateSlot(column.transform, BuffType.Danio, "Daño", BuffIcons.Sword, DamageColor, false),
            CreateSlot(column.transform, BuffType.VelocidadMejorada, "VelocidadMejorada", BuffIcons.DoubleBolt, EliteSpeedColor, true),
            CreateSlot(column.transform, BuffType.DanioMejorado, "DanioMejorado", BuffIcons.CrossedSwords, EliteDamageColor, true),
            CreateSlot(column.transform, BuffType.Regeneracion, "Regeneracion", BuffIcons.Plus, RegenColor, true),
            CreateSlot(column.transform, BuffType.DisparoContinuo, "DisparoContinuo", BuffIcons.Bullets, ContinuousColor, false)
        };
    }

    Slot CreateSlot(Transform parent, BuffType type, string name, Sprite iconSprite, Color color, bool elite)
    {
        Slot slot = new Slot();
        slot.type = type;

        slot.root = new GameObject(name, typeof(RectTransform));
        slot.root.transform.SetParent(parent, false);
        ((RectTransform)slot.root.transform).sizeDelta = new Vector2(SlotSize, SlotSize);

        // Fondo oscuro circular
        Image background = CreateImage("Fondo", slot.root.transform, BuffIcons.Circle, new Color(0f, 0f, 0f, 0.6f));
        Stretch(background.rectTransform);

        // Relleno circular que se va vaciando con el tiempo restante
        slot.timerFill = CreateImage("Tiempo", slot.root.transform, BuffIcons.Circle, new Color(color.r, color.g, color.b, 0.55f));
        Stretch(slot.timerFill.rectTransform);
        slot.timerFill.type = Image.Type.Filled;
        slot.timerFill.fillMethod = Image.FillMethod.Radial360;
        slot.timerFill.fillOrigin = (int)Image.Origin360.Top;
        slot.timerFill.fillClockwise = false;

        slot.icon = CreateImage("Icono", slot.root.transform, iconSprite, Color.white);
        Stretch(slot.icon.rectTransform);
        slot.icon.rectTransform.offsetMin = new Vector2(10f, 10f);
        slot.icon.rectTransform.offsetMax = new Vector2(-10f, -10f);

        if (elite)
        {
            Image ring = CreateImage("Aro", slot.root.transform, BuffIcons.Ring, EliteRingColor);
            Stretch(ring.rectTransform);
        }

        GameObject textObject = new GameObject("Segundos", typeof(RectTransform));
        textObject.transform.SetParent(slot.root.transform, false);
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 0f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = new Vector2(0f, -2f);
        textRect.sizeDelta = new Vector2(0f, 30f);

        slot.seconds = textObject.AddComponent<TextMeshProUGUI>();
        slot.seconds.fontSize = 26;
        slot.seconds.alignment = TextAlignmentOptions.Top;
        slot.seconds.color = Color.white;
        slot.seconds.raycastTarget = false;

        slot.root.SetActive(false);
        return slot;
    }

    static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    void Update()
    {
        if (player == null)
        {
            Destroy(gameObject);
            return;
        }

        bool hidden = GameManager.Instance != null && GameManager.Instance.GameEnded;

        foreach (Slot slot in slots)
        {
            UpdateSlot(slot, player.GetBuffRemaining(slot.type), player.GetBuffDuration(slot.type), hidden);
        }
    }

    static void UpdateSlot(Slot slot, float remaining, float duration, bool hidden)
    {
        bool active = !hidden && remaining > 0f && duration > 0f;
        if (slot.root.activeSelf != active) slot.root.SetActive(active);
        if (!active) return;

        slot.timerFill.fillAmount = Mathf.Clamp01(remaining / duration);
        slot.seconds.text = Mathf.CeilToInt(remaining) + "s";

        // Parpadea al estar por terminar
        float alpha = remaining < BlinkTime ? 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 14f) : 1f;
        Color color = slot.icon.color;
        color.a = alpha;
        slot.icon.color = color;
    }
}
