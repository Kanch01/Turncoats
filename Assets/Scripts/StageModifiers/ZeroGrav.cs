using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class FloatyZone : MonoBehaviour
{
    public float floatStrength = 5f;      // How strongly objects are pushed upward
    public float maxUpSpeed = 3f;         // Limit upward speed
    public float drag = 2f;               // Optional: slow horizontal movement
    public bool ignoreStatic = true;

    private Dictionary<Rigidbody2D, int> objectsInZone = new();
    private Dictionary<Rigidbody2D, float> originalGravity = new();

    private void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;
        if (ignoreStatic && rb.bodyType != RigidbodyType2D.Dynamic) return;

        // Add new object to dict
        if (!objectsInZone.ContainsKey(rb))
        {
            objectsInZone[rb] = 0;
            originalGravity[rb] = rb.gravityScale;
        }

        objectsInZone[rb]++;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;
        if (!objectsInZone.ContainsKey(rb)) return;

        objectsInZone[rb]--;

        // Restore gravity
        if (objectsInZone[rb] <= 0)
        {
            rb.gravityScale = originalGravity[rb];
            objectsInZone.Remove(rb);
            originalGravity.Remove(rb);
        }
    }

    private void FixedUpdate()
    {
        foreach (var kv in objectsInZone)
        {
            Rigidbody2D rb = kv.Key;

            // Apply gentle upward force
            if (rb.linearVelocity.y < maxUpSpeed)
                rb.AddForce(Vector2.up * floatStrength * rb.mass * originalGravity[rb]/5, ForceMode2D.Force);

            // Horizontal damping
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * (1f - drag * Time.fixedDeltaTime), rb.linearVelocity.y);
        }
    }
}