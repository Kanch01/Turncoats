using UnityEngine;
using UnityEngine.UI;

public class PlayerSelectUI : MonoBehaviour
{
    [SerializeField] private Button heroButton;
    [SerializeField] private Button bossButton;

    void Awake()
    {
        heroButton.onClick.AddListener(() => GameFlowManager.Instance.ChooserSelectsHero());
        bossButton.onClick.AddListener(() => GameFlowManager.Instance.ChooserSelectsBoss());
    }
}
