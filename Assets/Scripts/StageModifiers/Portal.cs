using UnityEngine;
using System.Collections.Generic;

// Teleport a rigid body vertically
[RequireComponent(typeof(Collider2D))]
public class Portal : MonoBehaviour
{
    public float targetY = 5f;

    // Keep track of all bodies inside trigger zone to prevent retrigger
    private HashSet<Rigidbody2D> insidePortal = new();

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null || rb.bodyType == RigidbodyType2D.Static) return;

        if (insidePortal.Contains(rb)) return;

        insidePortal.Add(rb);

        // Teleport to target y position
        Vector2 newPos = rb.position;
        newPos.y = targetY;

        rb.linearVelocity = Vector2.zero;
        rb.position = newPos;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return;

        insidePortal.Remove(rb);
    }
}