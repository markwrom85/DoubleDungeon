using UnityEngine;

public class PlayerPanel : MonoBehaviour
{
    [SerializeField] private Sprite heartFull;
    [SerializeField] private Sprite heartHalf;
    [SerializeField] private Sprite heartEmpty;

    public void ChangeHealthDisplay(int currentHealth, int maxHealth)
    {
        int fullHearts = currentHealth / 2;
        bool hasHalfHeart = currentHealth % 2 == 1;
        int emptyHearts = (maxHealth / 2) - fullHearts - (hasHalfHeart ? 1 : 0);

        // Update the heart display based on the player's health
        for (int i = 0; i < fullHearts; i++)
        {
            // Set the heart image to full
            transform.GetChild(i).GetComponent<UnityEngine.UI.Image>().sprite = heartFull;
        }

        if (hasHalfHeart)
        {
            // Set the next heart image to half
            transform.GetChild(fullHearts).GetComponent<UnityEngine.UI.Image>().sprite = heartHalf;
        }

        for (int i = fullHearts + (hasHalfHeart ? 1 : 0); i < fullHearts + emptyHearts + (hasHalfHeart ? 1 : 0); i++)
        {
            // Set the remaining heart images to empty
            transform.GetChild(i).GetComponent<UnityEngine.UI.Image>().sprite = heartEmpty;
        }
    }
}
