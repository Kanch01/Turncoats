using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerSelectUI : MonoBehaviour
{
    [SerializeField] private Button heroButton;
    [SerializeField] private Button bossButton;
    [SerializeField] private TMP_Text buttonTextBoss;
    [SerializeField] private TMP_Text buttonTextHero;
    [SerializeField] private Button next;
    private bool role;

    void Awake()
    {
        next.enabled = false;
        // heroButton.onClick.AddListener(() => GameFlowManager.Instance.ChooserSelectsHero());
        // bossButton.onClick.AddListener(() => GameFlowManager.Instance.ChooserSelectsBoss());
    }

    public void BossSelected()
    {
        next.enabled = true;
        buttonTextBoss.text = "Selected";
        buttonTextHero.text = "Select";
        role = false;
    }

    public void HeroSelected()
    {
        next.enabled = true;
        buttonTextHero.text = "Selected";
        buttonTextBoss.text = "Select";
        role = true;
    }

   public void MovetoNext()
    {
        GameFlowManager.Instance.ChooserSelectsHero(role);
    }
    
}
