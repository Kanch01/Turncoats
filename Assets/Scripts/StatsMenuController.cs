using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsMenuController : MonoBehaviour
{
    [SerializeField] private int totalPoints = 15;
    [SerializeField] private int maxPerStat = 10;
    
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider speedSlider;
    [SerializeField] private Slider attackSlider;
    [SerializeField] private Slider jumpSlider;
    
    [SerializeField] private TMP_Text hpValueText;
    [SerializeField] private TMP_Text speedValueText;
    [SerializeField] private TMP_Text attackValueText;
    [SerializeField] private TMP_Text jumpValueText;

    [SerializeField] private TMP_Text remainingPointsText;
    [SerializeField] private Button confirmButton;

    private bool _suppressCallbacks;

    private void Awake()
    {
        ConfigureSlider(hpSlider);
        ConfigureSlider(speedSlider);
        ConfigureSlider(attackSlider);
        ConfigureSlider(jumpSlider);
    }

    private void Start()
    {
        var cfg = GameFlowManager.Instance.State.player;

        _suppressCallbacks = true;
        hpSlider.value = Mathf.Clamp(cfg.health, 0, maxPerStat);
        speedSlider.value = Mathf.Clamp(cfg.speed, 0, maxPerStat);
        attackSlider.value = Mathf.Clamp(cfg.attack, 0, maxPerStat);
        jumpSlider.value = Mathf.Clamp(cfg.jump, 0, maxPerStat);
        _suppressCallbacks = false;
        
        hpSlider.onValueChanged.AddListener(_ => OnAnySliderChanged());
        speedSlider.onValueChanged.AddListener(_ => OnAnySliderChanged());
        attackSlider.onValueChanged.AddListener(_ => OnAnySliderChanged());
        jumpSlider.onValueChanged.AddListener(_ => OnAnySliderChanged());

        OnAnySliderChanged();
    }

    private void OnDestroy()
    {
        hpSlider.onValueChanged.RemoveAllListeners();
        speedSlider.onValueChanged.RemoveAllListeners();
        attackSlider.onValueChanged.RemoveAllListeners();
        jumpSlider.onValueChanged.RemoveAllListeners();
    }

    private void ConfigureSlider(Slider s)
    {
        s.minValue = 0;
        s.maxValue = maxPerStat;
        s.wholeNumbers = true;
    }

    private void OnAnySliderChanged()
    {
        if (_suppressCallbacks) return;
        
        int sum = (int)hpSlider.value + (int)speedSlider.value + (int)attackSlider.value + (int)jumpSlider.value;

        if (sum > totalPoints)
        {
            int overflow = sum - totalPoints;
            
            Slider biggest = hpSlider;
            if (speedSlider.value > biggest.value) biggest = speedSlider;
            if (attackSlider.value > biggest.value) biggest = attackSlider;
            if (jumpSlider.value > biggest.value) biggest = jumpSlider;

            _suppressCallbacks = true;
            biggest.value = Mathf.Max(0, biggest.value - overflow);
            _suppressCallbacks = false;
        }
        
        hpValueText.text = ((int)hpSlider.value).ToString();
        speedValueText.text = ((int)speedSlider.value).ToString();
        attackValueText.text = ((int)attackSlider.value).ToString();
        jumpValueText.text = ((int)jumpSlider.value).ToString();

        int remaining = totalPoints - ((int)hpSlider.value + (int)speedSlider.value + (int)attackSlider.value + (int)jumpSlider.value);
        remainingPointsText.text = $"Remaining: {remaining}";
        
        confirmButton.interactable = (remaining == 0);
    }
    
    public void OnConfirmPressed()
    {
        var cfg = GameFlowManager.Instance.State.player;
        cfg.attack = (int)attackSlider.value;
        cfg.speed    = (int)speedSlider.value;
        cfg.health   = (int)hpSlider.value;
        cfg.jump = (int)jumpSlider.value;

        GameFlowManager.Instance.GoStatsToWeapon();
    }
    
    public void OnBackPressed()
    {
        GameFlowManager.Instance.BackToMainMenu();
    }
}
