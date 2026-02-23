using UnityEngine;
using UnityEngine.UI;

// Display player health on screen
public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    private HealthManager playerHealth;

    public void Initialize(HealthManager health)
    {
        playerHealth = health;

        // Subscribe to health bar changes
        playerHealth.onHealthChanged.AddListener(UpdateBar);

        // Force immediate update
        UpdateBar(playerHealth.GetHealth() / playerHealth.maxHealth);
    }

    private void UpdateBar(float normalizedHealth)
    {
        fillImage.fillAmount = normalizedHealth;
    }
}