using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    [SerializeField] public string moveType = "x";  // x, y, or circle
    public float amplitude = 3f;      // Distance left/right
    public float frequency = 0.5f;    // Cycles per second

    private Rigidbody2D rb;
    private Vector2 startPos;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = rb.position;
    }

    private void FixedUpdate()
    {
        float omega = 2f * Mathf.PI * frequency;
        // float offset = Mathf.PingPong(Time.time * speed, amplitude * 2f) - amplitude; // This one will give linear
        float offset = amplitude * Mathf.Sin(omega * Time.time); // This one will give smooth-ish

        Vector2 newPos = new Vector2(startPos.x + offset, startPos.y);
        if (moveType == "x")
        {
            newPos = new Vector2(startPos.x + offset, startPos.y);
        }
        else if (moveType == "y")
        {
            newPos = new Vector2(startPos.x, startPos.y + offset);
        }
        else if (moveType == "circle")
        {
            float xOffset = amplitude * Mathf.Cos(omega * Time.time);
            float yOffset = amplitude * Mathf.Sin(omega * Time.time);

            newPos = new Vector2(startPos.x + xOffset,
                                startPos.y + yOffset);
        }
        else
        {
            UnityEngine.Debug.Log("Unknown movement type for moving platform...");
        }
        rb.MovePosition(newPos);
    }
}