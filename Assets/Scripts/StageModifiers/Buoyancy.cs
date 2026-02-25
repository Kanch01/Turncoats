using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Buoyancy : MonoBehaviour
{
    public float baseBuoyancy = 50f; // Upward force
    public float drag = 20f;           // Water drag
    public float angularDrag = 1f;    // Water angular drag
    public float maxForcePerMass = 20f;  // Optional cap to prevent tiny objects from flying

    private void Reset()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true; // Force collier to be a trigger
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        // Estimate "volume" using collider size
        Collider2D col = other.GetComponent<Collider2D>();
        float area = 1f;
        if (col is BoxCollider2D box)
            area = box.size.x * box.size.y * rb.transform.localScale.x * rb.transform.localScale.y;
        else if (col is CircleCollider2D circle)
            area = Mathf.PI * Mathf.Pow(circle.radius * rb.transform.localScale.x, 2);

        // Apply buoyancy scaled by area (approximation of volume)
        float force = baseBuoyancy * area * rb.gravityScale * (1 / 5.0f);  
        force = Mathf.Min(force, maxForcePerMass * rb.mass); // optional clamp
        rb.AddForce(Vector2.up * force);

        // Apply drag
        rb.linearVelocity *= 1f - drag * Time.fixedDeltaTime;
        rb.angularVelocity *= 1f - angularDrag * Time.fixedDeltaTime;
    }
}