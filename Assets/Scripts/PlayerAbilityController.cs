using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerAbilityController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button[] selectButtons;   // size 3
    [SerializeField] private Button nextButton;

    [Header("Button Labels")]
    [SerializeField] private TMP_Text[] selectButtonLabels; 

    private string selectedAbility = ""; 

    private readonly string[] abilityNames = { "Double Dash", "Double Jump", "Tank" };

    private void Awake()
    {
        if (selectButtons == null || selectButtons.Length != 3)
            Debug.LogError("SelectButtons must be an array of size 3.");

        if (selectButtonLabels == null || selectButtonLabels.Length != 3)
            Debug.LogError("SelectButtonLabels must be an array of size 3.");

        // Hook up button events
        for (int i = 0; i < selectButtons.Length; i++)
        {
            int index = i;
            selectButtons[i].onClick.AddListener(() => SelectAbility(index));
        }

        // Initial UI state
        SetAllSelectText("Select");
        if (nextButton != null) nextButton.interactable = false;
        selectedAbility = "";
    }

    private void SelectAbility(int index)
    {
        // Reset all, then mark chosen
        SetAllSelectText("Select");
        if (index >= 0 && index < selectButtonLabels.Length && selectButtonLabels[index] != null)
            selectButtonLabels[index].text = "Selected";

        // Update string variable
        if (index >= 0 && index < abilityNames.Length)
            selectedAbility = abilityNames[index];

        // Enable Next
        if (nextButton != null) nextButton.interactable = true;

        Debug.Log("Selected ability: " + selectedAbility);
    }

    private void SetAllSelectText(string value)
    {
        for (int i = 0; i < selectButtonLabels.Length; i++)
        {
            if (selectButtonLabels[i] != null)
                selectButtonLabels[i].text = value;
        }
    }    
    public void OnConfirmPressed()
    {
        GameFlowManager.Instance.State.hero.ability = selectedAbility;
        GameFlowManager.Instance.GoAbilityToBoss();
    }
}
