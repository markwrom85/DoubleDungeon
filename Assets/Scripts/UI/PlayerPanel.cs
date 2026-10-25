using UnityEngine.UI;
using UnityEngine;

public class PlayerPanel : MonoBehaviour
{
    [SerializeField] private Sprite heartFull;
    [SerializeField] private Sprite heartHalf;
    [SerializeField] private Sprite heartEmpty;
    [SerializeField] private Image[] heartImages;
    [SerializeField] private RectTransform[] panelSides;
    [SerializeField, Min(0.01f)] private float sideTransitionDuration = 0.35f;
    private RectTransform currentPanelSide;
    private int currentPanelSideIndex;
    private Vector2 targetPanelPosition;
    private Vector2 panelPositionVelocity;

    private void Awake()
    {
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
