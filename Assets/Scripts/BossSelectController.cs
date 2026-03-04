using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossSelectController : MonoBehaviour
{
    [Header("Boss Select Buttons")]
    [SerializeField] private Button[] bossSelectButtons;

    [Header("UI")]
    [SerializeField] private Button nextButton;
    [SerializeField] private GameObject statboard;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private GameObject statsRoot;
    private int randHealth;
    private int randAttack;
    

    public int SelectedIndex { get; private set; } = -1;
    public bool HasSelection => SelectedIndex >= 0;
    

    private void Awake()
    {
        randHealth = (int)Random.Range(-6f, 6f);
        randAttack = (int)Random.Range(-2f, 2f);
        
        if (bossSelectButtons == null || bossSelectButtons.Length != 3)
            Debug.LogWarning($"{nameof(BossSelectController)}: bossSelectButtons should be size 3.");
        
        // Wire up button callbacks
        for (int i = 0; i < bossSelectButtons.Length; i++)
        {
            int captured = i;
            if (bossSelectButtons[captured] != null)
                bossSelectButtons[captured].onClick.AddListener(() => SelectBoss(captured));
        }
    }

    private void Start()
    {
        SetNextEnabled(false);
        SetStatsVisible(false);
        RefreshButtonLabels(-1);
    }

    private void SelectBoss(int index)
    {
        SelectedIndex = index;

        // Update button labels
        RefreshButtonLabels(index);
        
        SetNextEnabled(true);
        SetStatsVisible(true);

        // Update stats text
        UpdateStatsText(index);
    }
    

    private void RefreshButtonLabels(int selectedIndex)
    {
        for (int i = 0; i < bossSelectButtons.Length; i++)
        {
            var btn = bossSelectButtons[i];
            if (btn == null) continue;
            
            TMP_Text label = btn.GetComponentInChildren<TMP_Text>(true);
            if (label == null) continue;

            label.text = (i == selectedIndex) ? "Selected" : "Select";
        }
    }

    private void UpdateStatsText(int index)
    {
        var state = GameFlowManager.Instance.State;
        var cfg = state.boss;

        // var randHealth = (int)Random.Range(-6f, 6f);
        // var randAttack = (int)Random.Range(-2f, 2f);

        Debug.Log($"Random Health: {randHealth}     Random Attack: {randAttack}");

        if (index == 0)
        {
            // Mole is tank
            cfg.attack = (int)(state.hero.health/10) + randAttack;
            cfg.health = state.hero.attack*10 + randHealth;  // 10 hits to die
            cfg.jump = 18;
            cfg.speed = 8;
        }
        else if (index == 1)
        {
            // Clouds is glass cannon
            cfg.attack = (int)(state.hero.health/6) + randAttack;
            cfg.health = state.hero.attack*6 + randHealth;   // 6 hits to die
            cfg.jump = 25;
            cfg.speed = 17;
        }
        else
        {
            // Map 2 is balanced
            cfg.attack = (int)(state.hero.health/8) + randAttack;
            cfg.health = state.hero.attack*8 + randHealth;   // 8 hits to die
            cfg.jump = 20;
            cfg.speed = 12;
        }
        
        cfg.health = Mathf.Max(6, cfg.health);

        statsText.text = $"HP: {cfg.health} | Damage: {cfg.attack} | Jump: {cfg.jump} | Speed: {cfg.speed}";
    }

    private void SetNextEnabled(bool enabled)
    {
        if (nextButton == null) return;
        nextButton.interactable = enabled;
    }

    private void SetStatsVisible(bool visible)
    {
        if (statsRoot != null)
        {
            statsRoot.SetActive(visible);
        }
        else if (statsText != null)
        {
            statsText.gameObject.SetActive(visible);
        }
    }

    public void OnNextPressed()
    {
        GameFlowManager.Instance.GoBossToStageMod(SelectedIndex);
    }
}
