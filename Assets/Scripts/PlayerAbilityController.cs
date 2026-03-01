using UnityEngine;
using TMPro;

public class PlayerAbilityController : MonoBehaviour
{
    public void OnConfirmPressed()
    {
        GameFlowManager.Instance.GoAbilityToBoss();
    }
}
