using UnityEngine;

public enum TomeElement { Fire, Water, Lightning, Light }

[CreateAssetMenu(fileName = "Tome", menuName = "Scriptable Objects/Tome")]
public sealed class TomeDefinition : ScriptableObject
{
    public TomeElement element;
    public GameObject abilityPrefab;
    public Sprite panelOutline;
    public Sprite panelBackground;
    public Sprite abilityIcon;
    public RuntimeAnimatorController abilityAnimation;
}
