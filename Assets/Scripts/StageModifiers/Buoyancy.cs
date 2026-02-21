using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Buoyancy : MonoBehaviour
{
    public float buoyancyForce = 10f; // Upward force
    public float drag = 1f;           // Water drag
    public float angularDrag = 1f;    // Water angular drag

    private void Reset()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true; // Force collier to be a trigger
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        // Buoyant force
        rb.AddForce(Vector2.up * buoyancyForce * rb.mass);

        // Drag in water
        rb.linearVelocity *= 1f - drag * Time.fixedDeltaTime;
        // rb.angularVelocity *= 1f - angularDrag * Time.fixedDeltaTime;
    }
}