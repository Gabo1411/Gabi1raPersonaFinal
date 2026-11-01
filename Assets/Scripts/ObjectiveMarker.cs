using UnityEngine;
using UnityEngine.UI;

// Marcador de objetivo en pantalla: flecha amarilla con un halo que se desvanece y reaparece. Se ve sobre el objetivo
// cuando está a la vista, o en el borde de la pantalla apuntando hacia él cuando no.
public class ObjectiveMarker : MonoBehaviour
{
    public static readonly Color MarkerColor = new Color(1f, 0.85f, 0.25f, 1f);

    static ObjectiveMarker instance;

    const float EdgeX = 0.46f;
    const float EdgeY = 0.42f;
    const float HeightOffset = 2.5f;
    const float FadePeriod = 2.2f;
    const float MinAlpha = 0.25f;

    Transform target;
    Light targetLight;
    float lightBaseIntensity;
    Camera mainCamera;
    RectTransform marker;
    RectTransform arrow;
    Image arrowImage;
    Image glowImage;

    public static void Show(Transform target, Light beaconLight = null)
    {
        if (instance == null) Create();

        instance.target = target;
        instance.targetLight = beaconLight;
        instance.lightBaseIntensity = beaconLight != null ? beaconLight.intensity : 0f;
        instance.marker.gameObject.SetActive(true);
    }

    public static void Hide()
    {
        if (instance == null) return;

        instance.marker.gameObject.SetActive(false);
        if (instance.targetLight != null) instance.targetLight.enabled = false;
    }

    static void Create()
    {
        GameObject root = new GameObject("ObjectiveMarker", typeof(RectTransform));
        instance = root.AddComponent<ObjectiveMarker>();

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject markerObject = new GameObject("Marcador", typeof(RectTransform));
        markerObject.transform.SetParent(root.transform, false);
        instance.marker = (RectTransform)markerObject.transform;
        instance.marker.sizeDelta = Vector2.zero;

        // Halo suave detrás de la flecha
        GameObject glowObject = new GameObject("Halo", typeof(RectTransform));
        glowObject.transform.SetParent(markerObject.transform, false);
        ((RectTransform)glowObject.transform).sizeDelta = new Vector2(190f, 190f);

        instance.glowImage = glowObject.AddComponent<Image>();
        instance.glowImage.sprite = BuffIcons.Glow;
        instance.glowImage.raycastTarget = false;

        GameObject arrowObject = new GameObject("Flecha", typeof(RectTransform));
        arrowObject.transform.SetParent(markerObject.transform, false);
        instance.arrow = (RectTransform)arrowObject.transform;
        instance.arrow.sizeDelta = new Vector2(64f, 64f);

        instance.arrowImage = arrowObject.AddComponent<Image>();
        instance.arrowImage.sprite = BuffIcons.Arrow;
        instance.arrowImage.raycastTarget = false;

        instance.marker.gameObject.SetActive(false);
    }

    void Update()
    {
        if (target == null)
        {
            marker.gameObject.SetActive(false);
            return;
        }

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        bool hidden = GameManager.Instance != null && GameManager.Instance.GameEnded;
        if (hidden) { marker.gameObject.SetActive(false); return; }

        Vector3 viewport = mainCamera.WorldToViewportPoint(target.position + Vector3.up * HeightOffset);

        // Detrás de la cámara las coordenadas se invierten: se refleja para apuntar hacia el lado correcto
        bool behind = viewport.z < 0f;
        if (behind) viewport = new Vector3(1f - viewport.x, 1f - viewport.y, viewport.z);

        Vector2 fromCenter = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
        bool onScreen = !behind && Mathf.Abs(fromCenter.x) <= EdgeX && Mathf.Abs(fromCenter.y) <= EdgeY;

        if (onScreen)
        {
            arrow.localRotation = Quaternion.Euler(0f, 0f, 180f); // apunta hacia abajo, al objetivo
        }
        else
        {
            if (fromCenter.sqrMagnitude < 0.0001f) fromCenter = Vector2.down;

            float scale = Mathf.Max(Mathf.Abs(fromCenter.x) / EdgeX, Mathf.Abs(fromCenter.y) / EdgeY);
            fromCenter /= scale;

            float angle = Mathf.Atan2(fromCenter.y, fromCenter.x) * Mathf.Rad2Deg - 90f;
            arrow.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        marker.anchorMin = marker.anchorMax = new Vector2(0.5f + fromCenter.x, 0.5f + fromCenter.y);
        marker.anchoredPosition = Vector2.zero;

        // Luz amarilla que se desvanece y vuelve a brillar
        float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (2f * Mathf.PI / FadePeriod));
        float alpha = Mathf.Lerp(MinAlpha, 1f, wave);

        Color tint = MarkerColor;
        tint.a = alpha;
        arrowImage.color = tint;

        Color glow = MarkerColor;
        glow.a = alpha * 0.55f;
        glowImage.color = glow;

        arrow.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.08f, wave);

        if (targetLight != null) targetLight.intensity = lightBaseIntensity * Mathf.Lerp(0.3f, 1f, wave);
    }
}
