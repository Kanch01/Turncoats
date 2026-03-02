using UnityEngine;

public enum StatType { None, Health, Attack, Defense, Speed, Jump, Weight }

[CreateAssetMenu(fileName = "Weapon_", menuName = "Game/Weapon")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string displayName;

    [Header("Buff")]
    public StatType buff;
    public int buffAmount;

    [Header("Debuff")]
    public StatType debuff;
    public int debuffAmount;
    
    public Sprite icon;
    [TextArea] public string description;
}
