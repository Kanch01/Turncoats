using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsMenuController : MonoBehaviour
{
    [Header("Point Rules")]
    [SerializeField] private int totalPoints = 15;
    [SerializeField] private int maxPerStat = 10;

    [Header("Sliders")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider speedSlider;
    [SerializeField] private Slider attackSlider;
    [SerializeField] private Slider jumpSlider;

    [Header("Value Text")]
    [SerializeField] private TMP_Text hpValueText;
    [SerializeField] private TMP_Text speedValueText;
    [SerializeField] private TMP_Text attackValueText;
    [SerializeField] private TMP_Text jumpValueText;

    [Header("UI")]
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

    private void OnEnable()
    {
        // IMPORTANT: Use the HERO config in the new GameState
        var state = GameFlowManager.Instance.State;
        var cfg = state.hero;

        // Load saved values (or defaults) into sliders
        _suppressCallbacks = true;

        hpSlider.value = Mathf.Clamp(cfg.health, 0, maxPerStat);
        speedSlider.value = Mathf.Clamp(cfg.speed, 0, maxPerStat);
        attackSlider.value = Mathf.Clamp(cfg.attack, 0, maxPerStat);
        jumpSlider.value = Mathf.Clamp(cfg.jump, 0, maxPerStat);

        _suppressCallbacks = false;

        // Hook listeners (pass WHICH slider changed)
        hpSlider.onValueChanged.AddListener(_ => OnAnySliderChanged(hpSlider));
        speedSlider.onValueChanged.AddListener(_ => OnAnySliderChanged(speedSlider));
        attackSlider.onValueChanged.AddListener(_ => OnAnySliderChanged(attackSlider));
        jumpSlider.onValueChanged.AddListener(_ => OnAnySliderChanged(jumpSlider));

        // Initial refresh
        OnAnySliderChanged(null);
    }

    private void OnDisable()
    {
        // Clean listeners (prevents duplicates if scene reloads or object toggles)
        hpSlider.onValueChanged.RemoveAllListeners();
        speedSlider.onValueChanged.RemoveAllListeners();
        attackSlider.onValueChanged.RemoveAllListeners();
        jumpSlider.onValueChanged.RemoveAllListeners();
    }

    private void ConfigureSlider(Slider s)
    {
        if (s == null) return;
        s.minValue = 0;
        s.maxValue = maxPerStat;
        s.wholeNumbers = true;
    }

    private void OnAnySliderChanged(Slider changed)
    {
        if (_suppressCallbacks) return;

        int hp = (int)hpSlider.value;
        int spd = (int)speedSlider.value;
        int atk = (int)attackSlider.value;
        int jmp = (int)jumpSlider.value;

        int sum = hp + spd + atk + jmp;

        // If we exceeded the cap, clamp ONLY the slider the user is moving
        if (sum > totalPoints && changed != null)
        {
            int overflow = sum - totalPoints;
            int newValue = Mathf.Max((int)changed.minValue, (int)changed.value - overflow);

            _suppressCallbacks = true;
            changed.SetValueWithoutNotify(newValue);
            _suppressCallbacks = false;

            // Recompute after clamping
            hp = (int)hpSlider.value;
            spd = (int)speedSlider.value;
            atk = (int)attackSlider.value;
            jmp = (int)jumpSlider.value;
            sum = hp + spd + atk + jmp;
        }

        int remaining = totalPoints - sum;

        // Update labels
        hpValueText.text = (hp + 10).ToString();
        speedValueText.text = (spd + 10).ToString();
        attackValueText.text = ((atk / 2) + 1).ToString();
        jumpValueText.text = (jmp + 15).ToString();

        remainingPointsText.text = $"Remaining: {remaining}";

        // Require all points be spent
        confirmButton.interactable = (remaining == 0);
    }

    // Hook this to your Confirm button (same as before)
    public void OnConfirmPressed()
    {
        var state = GameFlowManager.Instance.State;
        var cfg = state.hero;

        cfg.attack = ((int)attackSlider.value / 2) + 1;
        cfg.speed  = (int)speedSlider.value + 10;
        cfg.health = (int)hpSlider.value + 10;
        cfg.jump   = (int)jumpSlider.value + 15;

        GameFlowManager.Instance.GoStatsToWeapon();
    }
}
