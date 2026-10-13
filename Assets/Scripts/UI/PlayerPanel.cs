using UnityEngine.UI;
using UnityEngine;

public class PlayerPanel : MonoBehaviour
{
    [SerializeField] private Sprite heartFull;
    [SerializeField] private Sprite heartHalf;
    [SerializeField] private Sprite heartEmpty;
    [SerializeField] private Image[] heartImages;

    private void Awake()
    {
        if (heartImages == null || heartImages.Length == 0)
        {
            heartImages = GetComponentsInChildren<Image>(true);
        }
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
}
