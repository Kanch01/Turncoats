using UnityEngine;

// Wrapper to expose stage mod scriptable object data
public class StageModifier : MonoBehaviour
{
    public StageModifierData data;

    public void Initialize(StageModifierData modifierData)
    {
        data = modifierData;
    }
}