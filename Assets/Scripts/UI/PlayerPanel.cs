using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class PlayerPanel : MonoBehaviour
{
    [Header("Tome Appearance")]
    [SerializeField] private Image tomeOutline;
    [SerializeField] private Image tomeBackground;
    [SerializeField] private Image specialAbilityIcon;
    [SerializeField] private Animator specialAbilityAnimator;
    private TomeDefinition displayedTome;
    [Header("Special Ability Cooldown")]
    [SerializeField] private Vector2 abilitySlotCenter = new Vector2(33.3f, 5.8f);
    [SerializeField] private Vector2 abilitySlotSize = new Vector2(28f, 28f);
    private Image cooldownOverlay;
    private TextMeshProUGUI cooldownNumber;
    private bool abilityWasReady = true;
    private static Sprite cooldownShape;

    private void CreateCooldownDisplay()
    {
        if (cooldownOverlay != null) return;
        // Fit the entire HUD slot independently of the icon's transparent padding.
        var overlayObject = new GameObject("Special Cooldown Sweep", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayObject.transform.SetParent(transform, false);
        cooldownOverlay = overlayObject.GetComponent<Image>();
        cooldownOverlay.rectTransform.anchoredPosition = abilitySlotCenter;
        cooldownOverlay.rectTransform.sizeDelta = abilitySlotSize;
        if (cooldownShape == null)
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { name = "Ability slot cooldown mask", filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    int cornerDistance = Mathf.Min(x, 31 - x) + Mathf.Min(y, 31 - y);
                    pixels[y * 32 + x] = cornerDistance < 3 ? Color.clear : Color.white;
                }
            texture.SetPixels(pixels);
            texture.Apply();
            cooldownShape = Sprite.Create(texture, new Rect(0, 0, 32, 32), Vector2.one * 0.5f);
            cooldownShape.hideFlags = HideFlags.HideAndDontSave;
        }
        cooldownOverlay.sprite = cooldownShape;
        cooldownOverlay.color = new Color(0.14f, 0.14f, 0.14f, 0.88f);
        cooldownOverlay.type = Image.Type.Filled;
        cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
        cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
        // The remaining covered sector shrinks as the revealed sector sweeps clockwise.
        cooldownOverlay.fillClockwise = false;
        cooldownOverlay.raycastTarget = false;
        cooldownOverlay.enabled = false;

        var numberObject = new GameObject("Special Cooldown Seconds", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        numberObject.transform.SetParent(overlayObject.transform, false);
        cooldownNumber = numberObject.GetComponent<TextMeshProUGUI>();
        cooldownNumber.rectTransform.anchorMin = Vector2.zero;
        cooldownNumber.rectTransform.anchorMax = Vector2.one;
        cooldownNumber.rectTransform.sizeDelta = Vector2.zero;
        cooldownNumber.alignment = TextAlignmentOptions.Center;
        cooldownNumber.fontSize = 13;
        cooldownNumber.fontStyle = FontStyles.Bold;
        cooldownNumber.color = Color.white;
        cooldownNumber.raycastTarget = false;
        cooldownNumber.enabled = false;
    }

    public void SetSpecialAbilityCooldown(SpecialAttack ability)
    {
        CreateCooldownDisplay();
        bool ready = ability == null || ability.IsReady;
        cooldownOverlay.enabled = !ready;
        cooldownNumber.enabled = !ready;
        if (!ready)
        {
            cooldownOverlay.fillAmount = ability.CooldownDuration > 0f
                ? Mathf.Clamp01(ability.CooldownRemaining / ability.CooldownDuration) : 1f;
            cooldownNumber.SetText("{0}", Mathf.Max(1, Mathf.CeilToInt(ability.CooldownRemaining)));
        }
        if (specialAbilityAnimator != null)
        {
            specialAbilityAnimator.speed = ready ? 1f : 0f;
            if (ready && !abilityWasReady && specialAbilityAnimator.isActiveAndEnabled && specialAbilityAnimator.runtimeAnimatorController != null)
                specialAbilityAnimator.Play(0, 0, 0f);
        }
        abilityWasReady = ready;
    }

    public void ApplyTome(TomeDefinition tome)
    {
        if (tome == null || displayedTome == tome) return;
        displayedTome = tome;
        if (tomeOutline != null && tome.panelOutline != null) tomeOutline.sprite = tome.panelOutline;
        if (tomeBackground != null && tome.panelBackground != null) tomeBackground.sprite = tome.panelBackground;
        if (specialAbilityAnimator != null)
        {
            specialAbilityAnimator.runtimeAnimatorController = tome.abilityAnimation;
            specialAbilityAnimator.enabled = tome.abilityAnimation != null;
        }
        if (specialAbilityIcon != null)
        {
            specialAbilityIcon.sprite = tome.abilityIcon;
            specialAbilityIcon.enabled = tome.abilityIcon != null;
            // Keep every element's icon centered in the same full-size ability slot.
            specialAbilityIcon.rectTransform.localScale = Vector3.one;
            specialAbilityIcon.rectTransform.anchoredPosition = abilitySlotCenter;
            specialAbilityIcon.rectTransform.sizeDelta = abilitySlotSize;
        }
    }
    [SerializeField] private Sprite heartFull;
    [SerializeField] private Sprite heartHalf;
    [SerializeField] private Sprite heartEmpty;
    [SerializeField] private Image[] heartImages;
    [SerializeField] private Image[] teleportBarSegments;
    [SerializeField] private RectTransform[] panelSides;
    [SerializeField, Min(0.01f)] private float sideTransitionDuration = 0.35f;
    private RectTransform currentPanelSide;
    private int currentPanelSideIndex;
    private Vector2 targetPanelPosition;
    private Vector2 panelPositionVelocity;

    private void Awake()
    {
        CreateCooldownDisplay();
        SetTeleportCooldownProgress(1f);

        if (heartImages == null || heartImages.Length == 0)
        {
            heartImages = GetComponentsInChildren<Image>(true);
        }

        if (panelSides != null && panelSides.Length > 0)
        {
            currentPanelSide = panelSides[0];
            currentPanelSideIndex = 0;
            ApplyPanelSide();
        }
    }

    private void Update()
    {
        RectTransform panel = transform as RectTransform;
        if (panel == null) return;

        panel.anchoredPosition = Vector2.SmoothDamp(
            panel.anchoredPosition,
            targetPanelPosition,
            ref panelPositionVelocity,
            sideTransitionDuration);
    }

    public void ChangeHealthDisplay(float currentHealth, int maxHealth)
    {
        if (heartImages == null || heartImages.Length == 0)
        {
            Debug.LogWarning("PlayerPanel has no heart Image components assigned.", this);
            return;
        }

        int currentHealthUnits = Mathf.Clamp(
            Mathf.RoundToInt(currentHealth),
            0,
            Mathf.Max(0, maxHealth));
        int maxHearts = Mathf.CeilToInt(maxHealth / 2f);

        for (int index = 0; index < heartImages.Length; index++)
        {
            Image heartImage = heartImages[index];
            if (heartImage == null) continue;

            int healthUnitsForHeart = currentHealthUnits - index * 2;
            heartImage.enabled = index < maxHearts;
            heartImage.sprite = healthUnitsForHeart >= 2
                ? heartFull
                : healthUnitsForHeart == 1
                    ? heartHalf
                    : heartEmpty;
        }
    }

    public void SetTeleportCooldownProgress(float progress)
    {
        if (teleportBarSegments == null || teleportBarSegments.Length == 0) return;

        float filledSegments = Mathf.Clamp01(progress) * teleportBarSegments.Length;
        for (int index = 0; index < teleportBarSegments.Length; index++)
        {
            Image segment = teleportBarSegments[index];
            if (segment != null)
            {
                segment.fillAmount = Mathf.Clamp01(filledSegments - index);
            }
        }
    }

    public void ChangePanelSide()
    {
        SetPanelSide(currentPanelSideIndex == 1);
    }

    public void SetPanelSide(bool isLeftSide)
    {
        currentPanelSideIndex = isLeftSide ? 0 : 1;

        if (panelSides != null && panelSides.Length > currentPanelSideIndex)
        {
            currentPanelSide = panelSides[currentPanelSideIndex];
            ApplyPanelSide();
        }
        else
        {
            RectTransform panel = transform as RectTransform;
            if (panel != null)
            {
                Vector2 position = panel.anchoredPosition;
                position.x = Mathf.Abs(position.x) * (isLeftSide ? -1f : 1f);
                targetPanelPosition = position;
                panelPositionVelocity = Vector2.zero;
            }
        }
    }

    private void ApplyPanelSide()
    {
        if (currentPanelSide == null) return;

        RectTransform panel = transform as RectTransform;
        if (panel == null) return;

        targetPanelPosition = currentPanelSide.anchoredPosition;
        panelPositionVelocity = Vector2.zero;
    }
}
