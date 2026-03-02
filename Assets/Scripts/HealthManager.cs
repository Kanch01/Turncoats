using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class HealthManager : MonoBehaviour
{
    [SerializeField] public float maxHealth = 3f;
    private float currentHealth;

    public UnityEvent<float> onHealthChanged;
    public UnityEvent onDeath;

    public SpriteRenderer spriteRenderer;
    public Color hurtColor = Color.red;
    public float flashDuration = 0.15f;
    public float knockbackDuration = 0.5f;
    
    private Color originalColor;
    private Coroutine flashRoutine;

    // Parry support
    private PlayerMovement movement;

    // Prevent multiple parry triggers from the same attacker within a short time
    private readonly Dictionary<int, float> parryLockoutUntil = new Dictionary<int, float>();
    [SerializeField] private float parryRepeatBlockSeconds = 0.15f;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        movement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        onHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    // Coroutine to apply knockback force, and then disable after set amount of time
    private Coroutine knockbackCoroutine;
    private void StartKnockbackTimer(Vector2 knockback, float duration)
    {
        // Stop any existing coroutine so you don’t overwrite
        if (knockbackCoroutine != null)
            StopCoroutine(knockbackCoroutine);

        knockbackCoroutine = StartCoroutine(ResetKnockbackAfterTime(knockback, duration));
    }

    private IEnumerator ResetKnockbackAfterTime(Vector2 knockback, float duration)
    {
        // Apply knockback immediately
        var playerController = GetComponent<PlayerMovement>();
        if (playerController != null)
        {
            playerController.AddActingKnockbackForce(knockback);
        }

        // Wait for duration
        yield return new WaitForSeconds(duration);

        // Reset acting knockback
        if (playerController != null)
        {
            playerController.SetActingKnockbackForce(Vector2.zero);
        }

        knockbackCoroutine = null;
    }

    /// <summary>
    /// Object will take damage and take knockback
    /// Called when damage colliion is triggered
    /// </summary>
    public void TakeDamage(float damage, GameObject attacker, Vector2 direction, float knockback_mag)
    {

        // Parry check
        if (movement != null && movement.IsParrying && attacker != null)
        {
            int id = attacker.GetInstanceID();
            if (parryLockoutUntil.TryGetValue(id, out float until) && Time.time < until)
                return;

            parryLockoutUntil[id] = Time.time + parryRepeatBlockSeconds;

            movement.HandleParrySuccess(attacker);
            return;
        }

        // Debug.Log($"{name}: AHHHHHHH IT HURTS");
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        onHealthChanged?.Invoke(currentHealth / maxHealth);

        // Look for rigid body and apply knockback if found
        if (Mathf.Abs(direction.y) < 0.7f)  // Limit vertical knockback by angle
                direction.y = 0f;
        Vector2 knockback = direction*knockback_mag;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = GetComponentInParent<Rigidbody2D>();
        if (rb != null)
        {
            float y_knockback_lim = (4f / 8f) * rb.gravityScale;            // Limit grav scale of 8 to 5, lim scale of 5 to smaller
            knockback.y = Mathf.Clamp(knockback.y, -y_knockback_lim, y_knockback_lim);
            // UnityEngine.Debug.Log($"Knockback: {knockback}"); 
            StartKnockbackTimer(knockback, knockbackDuration);
        }
        else
        {
            UnityEngine.Debug.Log($"Couldn't find rigidbody!");    
        }
        
        if (spriteRenderer != null)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
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

    private void Die()
    {
        Debug.Log($"{name} bleh (ˆ𐃷ˆ)");
        onDeath?.Invoke();
        Destroy(gameObject, 0f);
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        onHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    public float GetHealth() => currentHealth;

    public void ApplyMaxHealth(float newMaxHealth, bool fillToMax = true)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);
        currentHealth = fillToMax ? maxHealth : Mathf.Clamp(currentHealth, 0f, maxHealth);
        onHealthChanged?.Invoke(currentHealth / maxHealth);
    }
}