using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerWeaponController : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private WeaponDatabase allWeapons; // <-- your ScriptableObject asset

    [Header("UI (3 slots)")]
    [SerializeField] private TMP_Text[] titleTexts;        // size 3
    [SerializeField] private TMP_Text[] descriptionTexts;  // size 3
    [SerializeField] private Button[] selectButtons;       // size 3
    [SerializeField] private Button nextButton;

    // The 3 currently offered weapons
    private readonly List<WeaponData> offered = new List<WeaponData>(3);

    // Temporary selection
    private WeaponData selectedWeapon;
    private int selectedIndex = -1;

    private void Awake()
    {
        // Basic safety checks
        if (titleTexts == null || titleTexts.Length != 3)
            Debug.LogError("PlayerWeaponController: titleTexts must be size 3.");
        if (descriptionTexts == null || descriptionTexts.Length != 3)
            Debug.LogError("PlayerWeaponController: descriptionTexts must be size 3.");
        if (selectButtons == null || selectButtons.Length != 3)
            Debug.LogError("PlayerWeaponController: selectButtons must be size 3.");
        if (nextButton == null)
            Debug.LogError("PlayerWeaponController: nextButton is not assigned.");

        // Wire up select buttons -> SelectSlot(0/1/2)
        for (int i = 0; i < selectButtons.Length; i++)
        {
            int captured = i;
            selectButtons[i].onClick.RemoveAllListeners();
            selectButtons[i].onClick.AddListener(() => SelectSlot(captured));
        }

        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(OnNext);
    }

    private void OnEnable()
    {
        BuildRandomOffer();
        RenderOffer();
        ClearSelection();
    }

    private void BuildRandomOffer()
    {
        offered.Clear();

        if (allWeapons == null || allWeapons.weapons == null)
        {
            Debug.LogError("PlayerWeaponController: AllWeapons database not assigned or empty.");
            return;
        }

        var pool = allWeapons.weapons;
        if (pool.Count < 3)
        {
            Debug.LogError($"PlayerWeaponController: Need at least 3 weapons in AllWeapons. Found {pool.Count}.");
            return;
        }

        // Pick 3 unique random indices
        HashSet<int> used = new HashSet<int>();
        while (offered.Count < 3)
        {
            int idx = Random.Range(0, pool.Count);
            if (used.Add(idx))
                offered.Add(pool[idx]);
        }
    }

    private void RenderOffer()
    {
        for (int i = 0; i < 3; i++)
        {
            if (i >= offered.Count || offered[i] == null)
            {
                titleTexts[i].text = "N/A";
                descriptionTexts[i].text = "";
                selectButtons[i].interactable = false;
                continue;
            }

            var w = offered[i];

            // --- CHANGE THESE FIELD NAMES IF YOUR WeaponData USES DIFFERENT ONES ---
            titleTexts[i].text = w.displayName;
            descriptionTexts[i].text = w.description;

            selectButtons[i].interactable = true;
        }
    }

    private void ClearSelection()
    {
        selectedWeapon = null;
        selectedIndex = -1;

        // Optional: disable Next until selected
        if (nextButton != null) nextButton.interactable = false;

        // Optional: reset button labels / visuals
        for (int i = 0; i < selectButtons.Length; i++)
        {
            var label = selectButtons[i].GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = "Select";
        }
    }

    private void SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= offered.Count) return;
        if (offered[slotIndex] == null) return;

        selectedWeapon = offered[slotIndex];
        selectedIndex = slotIndex;

        if (nextButton != null) nextButton.interactable = true;

        // Optional: show which is selected
        for (int i = 0; i < selectButtons.Length; i++)
        {
            var label = selectButtons[i].GetComponentInChildren<TMP_Text>();
            if (label == null) continue;
            label.text = (i == selectedIndex) ? "Selected" : "Select";
        }
    }

    public void OnNext()
    {
        if (selectedWeapon == null)
        {
            Debug.LogWarning("PlayerWeaponController: Next pressed without selecting a weapon.");
            return;
        }

        // Apply buff/debuff to HERO stats
        var state = GameFlowManager.Instance.State;
        if (state == null || state.hero == null)
        {
            Debug.LogError("PlayerWeaponController: GameState/hero config not found.");
            return;
        }

        ApplyWeaponToConfig(state.hero, selectedWeapon);

        // Store chosen weapon name in gamestate (your PlayerConfig has a string weapon)
        state.hero.weapon = selectedWeapon.displayName;

        // Proceed
        GameFlowManager.Instance.GoWeaponToAbility();
    }

    private void ApplyWeaponToConfig(PlayerConfig config, WeaponData weapon)
    {
        // --- CHANGE THESE FIELD NAMES IF YOUR WeaponData USES DIFFERENT ONES ---
        ApplyStatDelta(config, weapon.buff, weapon.buffAmount);
        ApplyStatDelta(config, weapon.debuff, -weapon.debuffAmount);
    }

    private void ApplyStatDelta(PlayerConfig config, StatType stat, int delta)
    {
        // If your StatType enum uses different names, update here
        switch (stat)
        {
            case StatType.Attack:
                config.attack += delta;
                break;
            case StatType.Speed:
                config.speed += delta;
                break;
            case StatType.Health:
                config.health += delta;
                break;
            case StatType.Jump:
                config.jump += delta;
                break;

            case StatType.None:
            default:
                break;
        }

        // Optional: clamp stats so they never go below 1
        config.attack = Mathf.Max(1, config.attack);
        config.speed  = Mathf.Max(1, config.speed);
        config.health = Mathf.Max(1, config.health);
        config.jump   = Mathf.Max(1, config.jump);
    }
}
