using UnityEngine;
using System.Collections.Generic;

public class AttackHitboxController : MonoBehaviour
{
    private Collider2D[] hitboxes;
    [SerializeField] private LayerMask hittableLayers;
    [SerializeField] private Transform hitboxRoot;
    [SerializeField] public float damage = 1f;
    [SerializeField] public float knockback = 1f;

    // Track already hit targets for this attack (by ROOT)
    private HashSet<GameObject> hitTracker;

    private void Awake()
    {
        if (hitboxes == null || hitboxes.Length == 0)
            hitboxes = hitboxRoot.GetComponentsInChildren<Collider2D>(true);

        SetActive(false);
    }

    public void EnableHitboxes()
    {
        hitTracker = new HashSet<GameObject>();
        Debug.Log($"{name}: EnableHitboxes event fired", this);
        SetActive(true);
    }

    public void DisableHitboxes()
    {
        Debug.Log($"{name}: DisableHitboxes event fired", this);
        SetActive(false);
    }

    private void SetActive(bool on)
    {
        if (hitboxes != null)
        {
            foreach (var c in hitboxes)
                if (c) c.enabled = on;
        }
    }

    public void ApplyNewAttack(float dmg)
    {
        damage = dmg;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hittableLayers) == 0)
            return;

        // attacker = character root owning this hitbox
        GameObject attacker = transform.root.gameObject;

        // Identify the "target character" root (not the collider GameObject)
        GameObject targetRoot = other.transform.root.gameObject;

        // Don't hit self
        if (targetRoot == attacker)
            return;

        // Hit only once per attack (by target ROOT)
        if (hitTracker != null && hitTracker.Contains(targetRoot))
            return;

        hitTracker?.Add(targetRoot);

        // HealthManager might be on the root while collider is on a child
        HealthManager targetHealth = other.GetComponentInParent<HealthManager>();
        if (targetHealth != null)
        {
            // Apply knockback based on distance between GroundChecks because they should be at
            // the same y-level when on even ground no matter player height
            PlayerMovement attacker_mov = transform.root.GetComponent<PlayerMovement>();
            PlayerMovement target_mov = other.transform.root.GetComponent<PlayerMovement>();

            Vector2 direction = (target_mov.groundCheck.position - attacker_mov.groundCheck.position).normalized;

            targetHealth.TakeDamage(damage, attacker, direction, knockback);
        }
    }
}
