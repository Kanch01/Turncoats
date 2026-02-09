using UnityEngine;
using UnityEngine.Events;

public class HealthManager : MonoBehaviour
{
    [SerializeField] private float maxHealth = 3f;   // Health when full
    private float currentHealth;

    public UnityEvent<float> onHealthChanged;
    public UnityEvent onDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Object will take damage
    /// Called when damage colliion is triggered
    /// </summary>
    public void TakeDamage(float amount)
    {
        Debug.Log($"{name}: AHHHHHHH IT HURTS");
        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);
        onHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
            Die();
    }

    /// <summary>
    /// Object will die
    /// </summary>
    private void Die()
    {
        Debug.Log($"{name} bleh (ˆ𐃷ˆ)");
        onDeath?.Invoke();
        // TODO: Death animation
        Destroy(gameObject, 1f);    // Will just kill object for now lol
    }

    /// <summary>
    /// Restore health given an amount to restore
    /// </summary>
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        onHealthChanged?.Invoke(currentHealth);
    }

    public float GetHealth() => currentHealth;
}