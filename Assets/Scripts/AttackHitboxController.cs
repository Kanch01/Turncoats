using UnityEngine;
using System.Collections.Generic;

public class AttackHitboxController : MonoBehaviour
{
    private Collider2D[] hitboxes;
    [SerializeField] private LayerMask hittableLayers;
    [SerializeField] private Transform hitboxRoot;
    [SerializeField] public float damage = 1f;
    [SerializeField] public float knockback = 1f;

    // Track already hit targets for this attack
    private HashSet<GameObject> hitTracker;

    private void Awake()
    {
        if (hitboxes == null || hitboxes.Length == 0)
            hitboxes = hitboxRoot.GetComponentsInChildren<Collider2D>(true);
        
        
        SetActive(false);
    }

    // Called by Animation Events
    public void EnableHitboxes()
    {
        hitTracker = new HashSet<GameObject>();
        Debug.Log($"{name}: EnableHitboxes event fired", this);
        SetActive(true);
    }

    // Called by Animation Events
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
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hittableLayers) == 0)
            return;

        // Hit only once per attack
        if (hitTracker.Contains(other.gameObject))
            return;

        hitTracker.Add(other.gameObject);

        HealthManager targetHealth = other.GetComponent<HealthManager>();
        if (targetHealth != null)
        {
            // Apply knockback based on distance between GroundChecks because they should be at
            // the same y-level when on even ground no matter player height
            PlayerMovement attacker = transform.root.GetComponent<PlayerMovement>();
            PlayerMovement target = other.transform.root.GetComponent<PlayerMovement>();

            Vector2 direction = (target.groundCheck.position - attacker.groundCheck.position).normalized;

            targetHealth.TakeDamage(damage, direction, knockback);
        }
    }
}
