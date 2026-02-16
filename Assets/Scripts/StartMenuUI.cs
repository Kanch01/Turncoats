using UnityEngine;

public class StartMenuUI : MonoBehaviour
{
    public void OnStartPressed()
    {
        GameFlowManager.Instance.StartNewGame();
    }
}
