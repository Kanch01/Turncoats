using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RotatingPlatform : MonoBehaviour
{
    public bool oscillate = false;   // true = back/forth, false = continuous spin
    public float amplitude = 45f;   // degrees (used if oscillating)
    public float frequency = 0.5f;  // cycles/sec (used if oscillating)
    public float spinSpeed = 90f;   // deg/sec (used if continuous)

    private Rigidbody2D rb;
    private float startAngle;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startAngle = rb.rotation;
    }

    private void FixedUpdate()
    {
        float newAngle;

        if (oscillate)
        {
            float omega = 2f * Mathf.PI * frequency;
            float offset = amplitude * Mathf.Sin(omega * Time.time);
            newAngle = startAngle + offset;
        }
        else
        {
            newAngle = startAngle + spinSpeed * Time.time;
        }

        rb.MoveRotation(newAngle);
    }
}