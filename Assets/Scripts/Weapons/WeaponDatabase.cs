using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Game/Weapon Database")]
public class WeaponDatabase : ScriptableObject
{
    public List<WeaponData> weapons;
}
