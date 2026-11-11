using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Interfaz de juego con aspecto de cripta: vida y dash abajo a la izquierda, munición abajo a la derecha
// (una bala por cada tiro del cargador) y puntos arriba al centro.
public class HudView : MonoBehaviour
{
    const float HealthBarWidth = 470f;
    const float PipsWidth = 384f;

    static readonly Color BloodDark = new Color(0.45f, 0.04f, 0.05f);
    static readonly Color BloodBright = new Color(0.9f, 0.16f, 0.14f);
    static readonly Color Warm = new Color(0.95f, 0.62f, 0.2f);

    PlayerMovement player;
    Weapon weapon;
    CanvasGroup group;

    // Vida
    RectTransform healthFill, healthGhost;
    Image healthFillImage, heartIcon;
    TMP_Text healthValue;
    float shownHealth = 1f, ghostHealth = 1f;
    int lastHealth = -1, lastMaxHealth = -1;

    // Dash
    Image dashFill, dashRing, dashIcon;
    TMP_Text dashKey;

    // Munición
    TMP_Text ammoTitle, ammoCurrent;
    readonly List<Image> pips = new List<Image>();
    RectTransform pipsRoot;
    int lastAmmo = -999, lastAmmoMax = -1, lastShownPips = -1;
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
        hud.BuildDash();
        hud.BuildAmmo();
        hud.BuildScore();
        Crosshair.Create(canvas.transform, player);
        return hud;
    }

    // ---------- Construcción ----------

    void BuildHealth()
    {
        Image panel = UITheme.Box(transform, "Vida", UITheme.Hud);
        UITheme.Place(panel.rectTransform, Vector2.zero, Vector2.zero, new Vector2(170f, 48f), new Vector2(590f, 112f));

        heartIcon = UITheme.Icon(panel.transform, "Corazon", BuffIcons.Heart, BloodBright);
        UITheme.Place(heartIcon.rectTransform, Vector2.zero, Vector2.zero, new Vector2(30f, 36f), new Vector2(48f, 48f));

        TMP_Text title = UITheme.Label(panel.transform, "Titulo", "VIDA", 21f, UITheme.Bronze, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
        title.characterSpacing = 6f;
        UITheme.Place(title.rectTransform, Vector2.zero, Vector2.zero, new Vector2(94f, 64f), new Vector2(200f, 30f));

        healthValue = UITheme.Label(panel.transform, "Valor", "", 34f, UITheme.TextMain, TextAlignmentOptions.BottomRight, FontStyles.Bold);
        UITheme.Outline(healthValue);
        UITheme.Place(healthValue.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(540f, 58f), new Vector2(300f, 40f));

        Image track = UITheme.Box(panel.transform, "Pista", UITheme.Track, true);
        UITheme.Place(track.rectTransform, Vector2.zero, Vector2.zero, new Vector2(94f, 24f), new Vector2(HealthBarWidth, 28f));

        Image ghost = UITheme.Box(track.transform, "Retraso", new Color(1f, 0.9f, 0.8f, 0.45f), true);
        healthGhost = ghost.rectTransform;
        InsetBar(healthGhost);

        Image fill = UITheme.Box(track.transform, "Relleno", BloodBright, true);
        healthFill = fill.rectTransform;
        healthFillImage = fill;
        InsetBar(healthFill);

        // Brillo en la mitad superior de la barra
        Image gloss = UITheme.Box(fill.transform, "Brillo", new Color(1f, 1f, 1f, 0.2f), true);
        gloss.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        gloss.rectTransform.anchorMax = Vector2.one;
        gloss.rectTransform.offsetMin = new Vector2(2f, 0f);
        gloss.rectTransform.offsetMax = new Vector2(-2f, -2f);
    }

    static void InsetBar(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(3f, 3f);
        rect.offsetMax = new Vector2(-3f, -3f);
    }

    // Runa circular que se llena mientras se recarga el dash
    void BuildDash()
    {
        RectTransform rune = UITheme.NewRect("Dash", transform);
        UITheme.Place(rune, Vector2.zero, Vector2.zero, new Vector2(52f, 64f), new Vector2(100f, 100f));

        Image back = UITheme.Icon(rune, "Fondo", BuffIcons.Circle, new Color(0.04f, 0.03f, 0.05f, 0.92f));
        UITheme.Stretch(back.rectTransform);

        dashFill = UITheme.Icon(rune, "Relleno", BuffIcons.Circle, new Color(0.45f, 0.5f, 0.62f, 0.8f));
        UITheme.Stretch(dashFill.rectTransform, 9f);
        dashFill.type = Image.Type.Filled;
        dashFill.fillMethod = Image.FillMethod.Radial360;
        dashFill.fillOrigin = (int)Image.Origin360.Top;
        dashFill.fillClockwise = true;

        dashRing = UITheme.Icon(rune, "Aro", BuffIcons.Ring, UITheme.Bronze);
        UITheme.Stretch(dashRing.rectTransform);

        dashIcon = UITheme.Icon(rune, "Icono", BuffIcons.Chevrons, UITheme.TextMain);
        UITheme.Place(dashIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f));

        dashKey = UITheme.Label(transform, "TeclaDash", "SHIFT", 19f, UITheme.Bronze, TextAlignmentOptions.Center, FontStyles.Bold);
        dashKey.characterSpacing = 5f;
        UITheme.Place(dashKey.rectTransform, Vector2.zero, Vector2.zero, new Vector2(52f, 32f), new Vector2(100f, 28f));
    }

    void BuildAmmo()
    {
        Image panel = UITheme.Box(transform, "Municion", UITheme.Hud);
        UITheme.Place(panel.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-48f, 48f), new Vector2(450f, 150f));

        ammoTitle = UITheme.Label(panel.transform, "Titulo", "MUNICIÓN", 21f, UITheme.Bronze, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
        ammoTitle.characterSpacing = 6f;
        UITheme.Place(ammoTitle.rectTransform, Vector2.zero, Vector2.zero, new Vector2(32f, 114f), new Vector2(300f, 28f));

        ammoCurrent = UITheme.Label(panel.transform, "Actual", "", 64f, UITheme.TextMain, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
        UITheme.Outline(ammoCurrent, 0.18f);
        UITheme.Place(ammoCurrent.rectTransform, Vector2.zero, Vector2.zero, new Vector2(30f, 42f), new Vector2(390f, 66f));

        pipsRoot = UITheme.NewRect("Balas", panel.transform);
        UITheme.Place(pipsRoot, Vector2.zero, Vector2.zero, new Vector2(34f, 16f), new Vector2(PipsWidth, 24f));
    }

    // Una bala por cada tiro del cargador; se recrean solo si cambia el tamaño del cargador
    void RebuildPips(int count)
    {
        foreach (Image pip in pips) if (pip != null) Destroy(pip.gameObject);
        pips.Clear();

        count = Mathf.Clamp(count, 1, 40);
        float pitch = PipsWidth / count;
        float width = Mathf.Min(pitch * 0.62f, 14f);

        for (int i = 0; i < count; i++)
        {
            Image pip = UITheme.Icon(pipsRoot, "Bala " + i, BuffIcons.SingleBullet, Warm);
            UITheme.Place(pip.rectTransform, Vector2.zero, new Vector2(0f, 0f), new Vector2(i * pitch, 0f), new Vector2(width, 24f));
            pips.Add(pip);
        }
        lastShownPips = -1;
    }

    void BuildScore()
    {
        Image panel = UITheme.Box(transform, "Puntos", UITheme.Hud);
        scoreRoot = panel.gameObject;
        UITheme.Place(panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(520f, 122f));

        TMP_Text title = UITheme.Label(panel.transform, "Titulo", "PUNTOS", 21f, UITheme.Bronze, TextAlignmentOptions.Center, FontStyles.Bold);
        title.characterSpacing = 8f;
        UITheme.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(220f, 28f));

        foreach (float side in new[] { -1f, 1f })
        {
            Image diamond = UITheme.Icon(panel.transform, "Adorno", BuffIcons.Diamond, UITheme.Bronze);
            UITheme.Place(diamond.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(side * 96f, -28f), new Vector2(12f, 12f));
        }

        scoreValue = UITheme.Label(panel.transform, "Valor", "", 56f, UITheme.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
        UITheme.Outline(scoreValue);
        UITheme.Place(scoreValue.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(460f, 70f));

        Image track = UITheme.Box(panel.transform, "Pista", UITheme.Track, true);
        UITheme.Place(track.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(420f, 12f));

        Image fill = UITheme.Box(track.transform, "Relleno", UITheme.Accent, true);
        scoreFill = fill.rectTransform;
        scoreFill.anchorMin = Vector2.zero;
        scoreFill.anchorMax = new Vector2(0f, 1f);
        scoreFill.offsetMin = new Vector2(2f, 2f);
        scoreFill.offsetMax = new Vector2(-2f, -2f);
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
        // La franja clara se queda atrás al recibir daño y sube de golpe al curarse
        ghostHealth = ghostHealth > target ? Mathf.MoveTowards(ghostHealth, target, 0.35f * dt) : target;

        healthFill.anchorMax = new Vector2(Mathf.Max(shownHealth, 0.001f), 1f);
        healthGhost.anchorMax = new Vector2(Mathf.Max(ghostHealth, 0.001f), 1f);
        healthFillImage.color = Color.Lerp(BloodDark, BloodBright, Mathf.Clamp01(shownHealth * 1.6f));

        // Con poca vida el corazón late
        bool low = target <= 0.3f && !player.IsDead;
        float beat = low ? 1f + 0.18f * Mathf.Max(0f, Mathf.Sin(Time.unscaledTime * 7f)) : 1f;
        heartIcon.rectTransform.localScale = Vector3.one * beat;

        if (player.Health != lastHealth || player.maxHealth != lastMaxHealth)
        {
            lastHealth = player.Health;
            lastMaxHealth = player.maxHealth;
            healthValue.text = Mathf.Max(0, player.Health) + " <size=65%><color=#AD9A80>/ " + player.maxHealth + "</color></size>";
        }
    }

    void UpdateDash()
    {
        float progress = player.GetDashCooldownProgress();
        bool ready = progress >= 1f;

        dashFill.fillAmount = progress;
        dashFill.color = ready ? new Color(0.98f, 0.7f, 0.27f, 0.85f) : new Color(0.4f, 0.45f, 0.58f, 0.8f);
        dashRing.color = ready ? UITheme.Accent : UITheme.Bronze;
        dashIcon.color = ready ? new Color(0.12f, 0.07f, 0.03f) : UITheme.TextMain;
        dashKey.color = ready ? UITheme.Accent : UITheme.Bronze;
    }

    void UpdateAmmo()
    {
        if (weapon == null) weapon = FindFirstObjectByType<Weapon>();
        if (weapon == null) return;

        int max = Mathf.Max(1, weapon.magazineSize);
        if (max != lastAmmoMax || pips.Count == 0)
        {
            lastAmmoMax = max;
            RebuildPips(max);
        }

        bool continuous = weapon.DisparoContinuoActivo;
        bool reloading = weapon.isReloading && !continuous;

        if (weapon.bulletsLeft != lastAmmo || continuous != lastContinuous || reloading != lastReloading)
        {
            lastAmmo = weapon.bulletsLeft;
            lastContinuous = continuous;
            lastReloading = reloading;

            ammoCurrent.text = continuous
                ? "INF"
                : weapon.bulletsLeft + " <size=48%><color=#AD9A80>/ " + weapon.magazineSize + "</color></size>";
            ammoTitle.text = continuous ? "DISPARO CONTINUO" : (reloading ? "RECARGANDO" : "MUNICIÓN");
            ammoTitle.color = continuous || reloading ? UITheme.Accent : UITheme.Bronze;
        }

        bool low = !continuous && weapon.magazineSize > 0 && weapon.bulletsLeft <= weapon.magazineSize * 0.25f;
        if (continuous) ammoCurrent.color = UITheme.Accent;
        else if (low && !reloading) ammoCurrent.color = Color.Lerp(UITheme.Danger, UITheme.TextMain, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f));
        else ammoCurrent.color = reloading ? UITheme.TextDim : UITheme.TextMain;

        // Balas encendidas: las que quedan, o las que se van cargando durante la recarga
        int lit = continuous ? pips.Count : (reloading ? Mathf.FloorToInt(weapon.ReloadProgress * pips.Count) : Mathf.Clamp(weapon.bulletsLeft, 0, pips.Count));
        if (lit != lastShownPips || continuous)
        {
            lastShownPips = lit;
            for (int i = 0; i < pips.Count; i++)
            {
                bool on = i < lit;
                pips[i].color = on ? (continuous ? UITheme.Accent : Warm) : new Color(0.2f, 0.17f, 0.16f, 0.9f);
            }
        }
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
            scoreValue.text = lastScore + " <size=55%><color=#AD9A80>/ " + lastTarget + "</color></size>";
        }

        float pct = Mathf.Clamp01((float)manager.CurrentScore / Mathf.Max(1, manager.targetScore));
        scoreFill.anchorMax = new Vector2(Mathf.MoveTowards(scoreFill.anchorMax.x, pct, 2f * Time.unscaledDeltaTime), 1f);
    }
}
