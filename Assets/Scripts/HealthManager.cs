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

    // Tank ability (hero-only): prevents death once by setting HP to 1.
    private bool tankUsed;

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
    /// Called when damage collision is triggered
    /// </summary>
    public void TakeDamage(float damage, GameObject attacker, Vector2 direction, float knockback_mag, Color col)
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

        // Apply damage (with Tank death-prevention once for the hero).
        float newHealth = currentHealth - damage;
        bool wouldDie = newHealth <= 0f;

        if (wouldDie && !tankUsed && movement != null && movement.IsHero && movement.HasAbility("Tank"))
        {
            tankUsed = true;
            currentHealth = 1f;
        }
        else
        {
            currentHealth = Mathf.Max(newHealth, 0f);
        }

        onHealthChanged?.Invoke(currentHealth / maxHealth);

        // Look for rigid body and apply knockback if found
        if (Mathf.Abs(direction.y) < 0.7f)  // Limit vertical knockback by angle
            direction.y = 0f;

        Vector2 knockback = direction * knockback_mag;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = GetComponentInParent<Rigidbody2D>();

        if (rb != null)
        {
            float y_knockback_lim = (4f / 8f) * rb.gravityScale;
            knockback.y = Mathf.Clamp(knockback.y, -y_knockback_lim, y_knockback_lim);
            StartKnockbackTimer(knockback, knockbackDuration);
        }
        else
        {
            UnityEngine.Debug.Log($"Couldn't find rigidbody!");
        }

        if (spriteRenderer != null)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(HurtFlash(col));
        }

        if (currentHealth <= 0)
            Die();
    }

    private IEnumerator HurtFlash(Color col)
    {
        spriteRenderer.color = col;

        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            float lerp = t / flashDuration;
            spriteRenderer.color = Color.Lerp(col, originalColor, lerp);
            yield return null;
        }

        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        Debug.Log($"{name} bleh (ˆ𐃷ˆ)");
        onDeath?.Invoke();

        var manager = FindFirstObjectByType<PlayerManager>();
        manager?.OnPlayerDeath(this);
        Destroy(gameObject);
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