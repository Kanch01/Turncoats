using UnityEngine;
using UnityEngine.UI;

public class PlayerWeaponController : MonoBehaviour
{
    public void OnConfirmPressed()
    {
        GameFlowManager.Instance.GoWeaponToAbility();
    }
}
