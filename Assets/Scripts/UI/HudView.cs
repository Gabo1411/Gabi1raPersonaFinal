using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Interfaz de juego: vida y dash abajo a la izquierda, munición abajo a la derecha y puntos arriba al centro.
public class HudView : MonoBehaviour
{
    const float BarWidth = 426f;

    PlayerMovement player;
    Weapon weapon;
    CanvasGroup group;

    // Vida
    RectTransform healthFill, healthGhost;
    Image healthFillImage;
    TMP_Text healthValue;
    float shownHealth = 1f, ghostHealth = 1f;
    int lastHealth = -1, lastMaxHealth = -1;

    // Dash
    RectTransform dashFill;
    Image dashFillImage;
    TMP_Text dashLabel;
    bool lastDashReady = true;

    // Munición
    TMP_Text ammoTitle, ammoCurrent;
    GameObject reloadTrack;
    RectTransform reloadFill;
    int lastAmmo = -999, lastAmmoMax = -1;
    bool lastContinuous, lastReloading;

    // Puntos
    GameObject scoreRoot;
    TMP_Text scoreValue;
    RectTransform scoreFill;
    int lastScore = -1, lastTarget = -1;

    public static HudView Create(PlayerMovement player)
    {
        Canvas canvas = UITheme.MakeCanvas("HudView", 30);
        HudView hud = canvas.gameObject.AddComponent<HudView>();
        hud.player = player;
        hud.group = canvas.gameObject.AddComponent<CanvasGroup>();
        hud.group.blocksRaycasts = false;
        hud.group.interactable = false;
        hud.BuildHealth();
        hud.BuildAmmo();
        hud.BuildScore();
        return hud;
    }

    // ---------- Construcción ----------

    void BuildHealth()
    {
        Image panel = UITheme.Box(transform, "Vida", UITheme.Hud);
        UITheme.Place(panel.rectTransform, Vector2.zero, Vector2.zero, new Vector2(48f, 48f), new Vector2(540f, 124f));

        Image icon = UITheme.Icon(panel.transform, "Icono", BuffIcons.Plus, UITheme.Danger);
        UITheme.Place(icon.rectTransform, Vector2.zero, Vector2.zero, new Vector2(24f, 50f), new Vector2(50f, 50f));

        TMP_Text title = UITheme.Label(panel.transform, "Titulo", "VIDA", 22f, UITheme.TextDim, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
        UITheme.Place(title.rectTransform, Vector2.zero, Vector2.zero, new Vector2(92f, 88f), new Vector2(200f, 28f));

        healthValue = UITheme.Label(panel.transform, "Valor", "", 30f, UITheme.TextMain, TextAlignmentOptions.BottomRight, FontStyles.Bold);
        UITheme.Place(healthValue.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(518f, 84f), new Vector2(240f, 34f));

        Image track = UITheme.Box(panel.transform, "Pista", UITheme.Track, true);
        UITheme.Place(track.rectTransform, Vector2.zero, Vector2.zero, new Vector2(92f, 46f), new Vector2(BarWidth, 32f));

        Image ghost = UITheme.Box(track.transform, "Retraso", new Color(1f, 1f, 1f, 0.4f), true);
        healthGhost = ghost.rectTransform;
        InsetBar(healthGhost);

        Image fill = UITheme.Box(track.transform, "Relleno", UITheme.Success, true);
        healthFill = fill.rectTransform;
        healthFillImage = fill;
        InsetBar(healthFill);

        // Dash
        TMP_Text dashKey = UITheme.Label(panel.transform, "TeclaDash", "SHIFT", 18f, UITheme.TextDim, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        UITheme.Place(dashKey.rectTransform, Vector2.zero, Vector2.zero, new Vector2(92f, 12f), new Vector2(70f, 24f));

        Image dashTrack = UITheme.Box(panel.transform, "PistaDash", UITheme.Track, true);
        UITheme.Place(dashTrack.rectTransform, Vector2.zero, Vector2.zero, new Vector2(166f, 18f), new Vector2(190f, 12f));

        Image dash = UITheme.Box(dashTrack.transform, "RellenoDash", UITheme.Accent, true);
        dashFill = dash.rectTransform;
        dashFillImage = dash;
        dashFill.anchorMin = Vector2.zero;
        dashFill.anchorMax = Vector2.one;
        dashFill.offsetMin = dashFill.offsetMax = Vector2.zero;

        dashLabel = UITheme.Label(panel.transform, "EtiquetaDash", "DASH", 18f, UITheme.TextDim, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        UITheme.Place(dashLabel.rectTransform, Vector2.zero, Vector2.zero, new Vector2(370f, 12f), new Vector2(150f, 24f));
    }

    static void InsetBar(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(3f, 3f);
        rect.offsetMax = new Vector2(-3f, -3f);
    }

    void BuildAmmo()
    {
        Image panel = UITheme.Box(transform, "Municion", UITheme.Hud);
        UITheme.Place(panel.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-48f, 48f), new Vector2(340f, 124f));

        ammoTitle = UITheme.Label(panel.transform, "Titulo", "MUNICIÓN", 22f, UITheme.TextDim, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
        UITheme.Place(ammoTitle.rectTransform, Vector2.zero, Vector2.zero, new Vector2(26f, 88f), new Vector2(240f, 28f));

        Image icon = UITheme.Icon(panel.transform, "Icono", BuffIcons.Bullets, UITheme.TextDim);
        UITheme.Place(icon.rectTransform, Vector2.zero, Vector2.zero, new Vector2(276f, 80f), new Vector2(40f, 40f));

        ammoCurrent = UITheme.Label(panel.transform, "Actual", "", 70f, UITheme.TextMain, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
        UITheme.Place(ammoCurrent.rectTransform, Vector2.zero, Vector2.zero, new Vector2(26f, 20f), new Vector2(290f, 72f));

        Image track = UITheme.Box(panel.transform, "PistaRecarga", UITheme.Track, true);
        UITheme.Place(track.rectTransform, Vector2.zero, Vector2.zero, new Vector2(26f, 12f), new Vector2(288f, 10f));
        reloadTrack = track.gameObject;

        Image fill = UITheme.Box(track.transform, "RellenoRecarga", UITheme.Accent, true);
        reloadFill = fill.rectTransform;
        reloadFill.anchorMin = Vector2.zero;
        reloadFill.anchorMax = Vector2.one;
        reloadFill.offsetMin = reloadFill.offsetMax = Vector2.zero;
        reloadTrack.SetActive(false);
    }

    void BuildScore()
    {
        Image panel = UITheme.Box(transform, "Puntos", UITheme.Hud);
        scoreRoot = panel.gameObject;
        UITheme.Place(panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(440f, 112f));

        TMP_Text title = UITheme.Label(panel.transform, "Titulo", "PUNTOS", 22f, UITheme.TextDim, TextAlignmentOptions.Top, FontStyles.Bold);
        UITheme.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(240f, 28f));

        scoreValue = UITheme.Label(panel.transform, "Valor", "", 50f, UITheme.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
        UITheme.Place(scoreValue.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), new Vector2(400f, 64f));

        Image track = UITheme.Box(panel.transform, "Pista", UITheme.Track, true);
        UITheme.Place(track.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(392f, 10f));

        Image fill = UITheme.Box(track.transform, "Relleno", UITheme.Accent, true);
        scoreFill = fill.rectTransform;
        scoreFill.anchorMin = Vector2.zero;
        scoreFill.anchorMax = new Vector2(0f, 1f);
        scoreFill.offsetMin = scoreFill.offsetMax = Vector2.zero;
    }

    // ---------- Actualización ----------

    void Update()
    {
        if (player == null)
        {
            Destroy(gameObject);
            return;
        }

        bool hidden = GameManager.Instance != null && GameManager.Instance.GameEnded;
        group.alpha = hidden ? 0f : 1f;
        if (hidden) return;

        UpdateHealth();
        UpdateDash();
        UpdateAmmo();
        UpdateScore();
    }

    void UpdateHealth()
    {
        float target = Mathf.Clamp01((float)player.Health / Mathf.Max(1, player.maxHealth));
        float dt = Time.unscaledDeltaTime;

        shownHealth = Mathf.MoveTowards(shownHealth, target, 3f * dt);
        // La barra blanca se queda atrás al recibir daño y sube de golpe al curarse
        ghostHealth = ghostHealth > target ? Mathf.MoveTowards(ghostHealth, target, 0.35f * dt) : target;

        healthFill.anchorMax = new Vector2(Mathf.Max(shownHealth, 0.001f), 1f);
        healthGhost.anchorMax = new Vector2(Mathf.Max(ghostHealth, 0.001f), 1f);
        healthFillImage.color = HealthColor(shownHealth);

        if (player.Health != lastHealth || player.maxHealth != lastMaxHealth)
        {
            lastHealth = player.Health;
            lastMaxHealth = player.maxHealth;
            healthValue.text = Mathf.Max(0, player.Health) + " <size=70%><color=#ABABBD>/ " + player.maxHealth + "</color></size>";
        }
    }

    static Color HealthColor(float pct)
    {
        Color low = UITheme.Danger, mid = new Color(0.95f, 0.75f, 0.25f), high = UITheme.Success;
        return pct < 0.5f ? Color.Lerp(low, mid, pct / 0.5f) : Color.Lerp(mid, high, (pct - 0.5f) / 0.5f);
    }

    void UpdateDash()
    {
        float progress = player.GetDashCooldownProgress();
        bool ready = progress >= 1f;

        dashFill.anchorMax = new Vector2(progress, 1f);
        dashFillImage.color = ready ? UITheme.Accent : new Color(0.45f, 0.5f, 0.62f);

        if (ready != lastDashReady)
        {
            lastDashReady = ready;
            dashLabel.text = ready ? "DASH LISTO" : "DASH";
            dashLabel.color = ready ? UITheme.Accent : UITheme.TextDim;
        }
    }

    void UpdateAmmo()
    {
        if (weapon == null) weapon = FindFirstObjectByType<Weapon>();
        if (weapon == null) return;

        bool continuous = weapon.DisparoContinuoActivo;
        bool reloading = weapon.isReloading && !continuous;

        if (weapon.bulletsLeft != lastAmmo || weapon.magazineSize != lastAmmoMax || continuous != lastContinuous || reloading != lastReloading)
        {
            lastAmmo = weapon.bulletsLeft;
            lastAmmoMax = weapon.magazineSize;
            lastContinuous = continuous;
            lastReloading = reloading;

            ammoCurrent.text = continuous
                ? "INF"
                : weapon.bulletsLeft + " <size=50%><color=#ABABBD>/ " + weapon.magazineSize + "</color></size>";
            ammoTitle.text = continuous ? "DISPARO CONTINUO" : (reloading ? "RECARGANDO" : "MUNICIÓN");
            ammoTitle.color = continuous || reloading ? UITheme.Accent : UITheme.TextDim;
            reloadTrack.SetActive(reloading);
        }

        bool low = !continuous && weapon.magazineSize > 0 && weapon.bulletsLeft <= weapon.magazineSize * 0.25f;
        if (continuous) ammoCurrent.color = UITheme.Accent;
        else if (low) ammoCurrent.color = Color.Lerp(UITheme.Danger, UITheme.TextMain, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f));
        else ammoCurrent.color = reloading ? UITheme.TextDim : UITheme.TextMain;

        if (reloading) reloadFill.anchorMax = new Vector2(weapon.ReloadProgress, 1f);
    }

    void UpdateScore()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null) return;

        if (scoreRoot.activeSelf != manager.ScoreVisible) scoreRoot.SetActive(manager.ScoreVisible);
        if (!manager.ScoreVisible) return;

        if (manager.CurrentScore != lastScore || manager.targetScore != lastTarget)
        {
            lastScore = manager.CurrentScore;
            lastTarget = manager.targetScore;
            scoreValue.text = lastScore + " <size=60%><color=#ABABBD>/ " + lastTarget + "</color></size>";
        }

        float pct = Mathf.Clamp01((float)manager.CurrentScore / Mathf.Max(1, manager.targetScore));
        scoreFill.anchorMax = new Vector2(Mathf.MoveTowards(scoreFill.anchorMax.x, pct, 2f * Time.unscaledDeltaTime), 1f);
    }
}
