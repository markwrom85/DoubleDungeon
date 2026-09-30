using UnityEngine;

// I make the player targetable while their character is active.
public class EnemyTarget : MonoBehaviour
{
    [SerializeField] private GameObject character;

    // I check the character object that is hidden during teleport placement.
    public bool IsTargetable => isActiveAndEnabled && character != null && character.activeInHierarchy;
}
