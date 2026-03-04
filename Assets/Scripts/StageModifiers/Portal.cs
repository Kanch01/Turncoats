using UnityEngine;
using System.Collections.Generic;

// Teleport a rigid body vertically
[RequireComponent(typeof(Collider2D))]
public class Portal : MonoBehaviour
{
    public Transform targetHeight;
    private float targetY;

    // Keep track of all bodies inside trigger zone to prevent retrigger
    private HashSet<Rigidbody2D> insidePortal = new();

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        if (targetHeight != null)
            targetY = targetHeight.position.y;
        if (targetHeight == null)
        {
            // If not assigned, search for child
            Transform child = transform.Find("TargetHeight");
            if (child != null)
            {
                targetHeight = child;
                targetY = targetHeight.position.y;
            }
            else
            {
                Debug.LogWarning("Portal: No child named 'TargetHeight' found!");
            }
        }
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