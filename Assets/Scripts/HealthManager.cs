using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class HealthManager : MonoBehaviour
{
    [SerializeField] private float maxHealth = 3f;   // Health when full
    private float currentHealth;

    public UnityEvent<float> onHealthChanged;
    public UnityEvent onDeath;

    public SpriteRenderer spriteRenderer;
    public Color hurtColor = Color.firebrick;
    public float flashDuration = 0.15f;
    
    private Color originalColor;
    private Coroutine flashRoutine;
    
    private void Awake()
    {
        currentHealth = maxHealth;
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
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
        
        if (spriteRenderer != null)
        {
            if (flashRoutine != null)
                StopCoroutine(flashRoutine);

            flashRoutine = StartCoroutine(HurtFlash());
        }

        if (currentHealth <= 0)
            Die();
    }
    
    private IEnumerator HurtFlash()
    {
        spriteRenderer.color = hurtColor;

        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            float lerp = t / flashDuration;
            spriteRenderer.color = Color.Lerp(hurtColor, originalColor, lerp);
            yield return null;
        }

        spriteRenderer.color = originalColor;
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