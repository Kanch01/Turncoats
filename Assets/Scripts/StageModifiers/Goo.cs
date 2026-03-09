using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class Goo : MonoBehaviour
{
    public float damping = 10f;         // Limit upward speed
    public float horizontalScaling = 10f;
    public float normScaling = 0.6f;
    private static Dictionary<Rigidbody2D, int> globalGooCount = new();
    private void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        // Add new object to dict
        if (rb.gameObject.CompareTag("Hero"))
        {
            if (!globalGooCount.ContainsKey(rb))
            {
                globalGooCount[rb] = 0;
            }

            globalGooCount[rb]++;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;
        if (!globalGooCount.ContainsKey(rb)) return;

        globalGooCount[rb]--;

        // Restore gravity
        if (globalGooCount[rb] <= 0)
        {
            globalGooCount.Remove(rb);
        }
    }

    private void FixedUpdate()
    {
        foreach (var kv in globalGooCount)
        {
            Rigidbody2D rb = kv.Key;
            int count = kv.Value;

            // // Limit Hero velocity with damping
            Vector2 dampingForce = -rb.linearVelocity * damping * Time.fixedDeltaTime;
            // dampingForce /= Mathf.Max(1, count);
            // dampingForce.x *= horizontalScaling;
            // UnityEngine.Debug.Log($"Damping force: {dampingForce}       Count: {count}");
            dampingForce.y *= normScaling;
            rb.AddForce(dampingForce, ForceMode2D.Force);
        }
    }
}