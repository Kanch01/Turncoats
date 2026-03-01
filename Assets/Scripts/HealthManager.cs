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
    public Color hurtColor = Color.firebrick;
    public float flashDuration = 0.15f;

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

    // Backward compatible
    public void TakeDamage(float amount)
    {
        TakeDamage(amount, null);
    }

    // NEW overload used by AttackHitboxController
    public void TakeDamage(float amount, GameObject attacker)
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

        Debug.Log($"{name}: AHHHHHHH IT HURTS");
        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);
        onHealthChanged?.Invoke(currentHealth / maxHealth);

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