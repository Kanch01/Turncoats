using UnityEngine;

// A scriptable object that holds general data for a stage modifier
[CreateAssetMenu(menuName = "StageModifier", fileName = "NewStageModifier")]
public class StageModifierData : ScriptableObject
{
    public string modifierName;
    public float damage;
    public float knockbackForce;
}