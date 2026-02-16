using UnityEngine;

public class PlayerAbilityController : MonoBehaviour
{
    public void OnConfirmPressed()
    {
        GameFlowManager.Instance.GoAbilityToBoss();
    }
}
