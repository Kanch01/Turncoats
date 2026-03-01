using UnityEngine;

// Allows a stage modifier to cause contact damage
public class DamageOnTouch : MonoBehaviour
{
    private StageModifier modifier;

    void Awake()
    {
        modifier = GetComponentInParent<StageModifier>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var health = other.GetComponent<HealthManager>();
        if (health == null) return;

        Vector2 direction = (other.transform.position - transform.position).normalized;
        health.TakeDamage(modifier.data.damage, direction, modifier.data.knockback);
    }
}