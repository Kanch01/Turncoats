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

        // If this menu opens for the first time and stats are all 0,
        // you may want to auto-spend points or force user to allocate.
        // This keeps your old behavior: user must spend all points to confirm.

        _suppressCallbacks = false;

        // Hook listeners
        hpSlider.onValueChanged.AddListener(_ => OnAnySliderChanged());
        speedSlider.onValueChanged.AddListener(_ => OnAnySliderChanged());
        attackSlider.onValueChanged.AddListener(_ => OnAnySliderChanged());
        jumpSlider.onValueChanged.AddListener(_ => OnAnySliderChanged());

        OnAnySliderChanged();
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

    private void OnAnySliderChanged()
    {
        if (_suppressCallbacks) return;

        int sum =
            (int)hpSlider.value +
            (int)speedSlider.value +
            (int)attackSlider.value +
            (int)jumpSlider.value;

        // Clamp overflow by reducing the slider that currently has the largest value
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

            // Recompute after the adjustment
            sum =
                (int)hpSlider.value +
                (int)speedSlider.value +
                (int)attackSlider.value +
                (int)jumpSlider.value;
        }

        // Update labels
        hpValueText.text = ((int)hpSlider.value).ToString();
        speedValueText.text = ((int)speedSlider.value).ToString();
        attackValueText.text = ((int)attackSlider.value).ToString();
        jumpValueText.text = ((int)jumpSlider.value).ToString();

        int remaining = totalPoints - sum;
        remainingPointsText.text = $"Remaining: {remaining}";

        // Require all points be spent
        confirmButton.interactable = (remaining == 0);
    }

    // Hook this to your Confirm button (same as before)
    public void OnConfirmPressed()
    {
        var state = GameFlowManager.Instance.State;
        var cfg = state.hero;

        cfg.attack = (int)attackSlider.value;
        cfg.speed  = (int)speedSlider.value;
        cfg.health = (int)hpSlider.value;
        cfg.jump   = (int)jumpSlider.value;

        GameFlowManager.Instance.GoStatsToWeapon();
    }

    // Hook this to your Back button (same as before)
    public void OnBackPressed()
    {
        GameFlowManager.Instance.BackToMainMenu();
    }
}
