using UnityEngine;
using System;


public class MultiTargetCamera : MonoBehaviour
{
    public Transform target1;
    public Transform target2;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float smoothTime = 0.5f; // For smooth movement
    [SerializeField] private float minSize = 6f;     // Minimum zoom
    [SerializeField] private float zoomLimiter = 1f; // Controls how aggressive scaling is
    [SerializeField] private bool move = true;         // Whether to actually move the camera

    private Vector3 velocity;
    private Camera cam;
    private BoxCollider2D leftCol;
    private BoxCollider2D rightCol;

    void Awake()
    {
        cam = GetComponent<Camera>();

        try
        {
            leftCol = transform.Find("LeftBound").GetComponent<BoxCollider2D>();
            rightCol = transform.Find("RightBound").GetComponent<BoxCollider2D>();
        }
        catch (NullReferenceException)
        {
            UnityEngine.Debug.Log("Camera colliders not found, assuming they're not needed for this map");
        }
    }
    
    void LateUpdate()
    {
        if ((target1 == null && target2 == null) || !move)
            return;

        MoveCamera();
        ZoomCamera();
        UpdateBounds();
    }

    void MoveCamera()
    {
        // Calculate the midpoint between the two targets
        Vector3 centerPoint;
        if (target1 != null && target2 != null)
            centerPoint = (target1.position + target2.position) / 2f;
        else if (target1 != null)
            centerPoint = target1.position;
        else
            centerPoint = target2.position;

        // Calculate the desired new position
        Vector3 newPosition = centerPoint + offset;
        newPosition.z = -10f;

        // Smoothly move the camera to the new position
        transform.position = Vector3.SmoothDamp(transform.position, newPosition, ref velocity, smoothTime);
    }

    // Set camera zoom based on vertical distance between players
    void ZoomCamera()
    {
        float newSize = Mathf.Lerp(
            cam.orthographicSize,
            Mathf.Max(GetGreatestDistance() / zoomLimiter, minSize),
            Time.deltaTime
        );
        cam.orthographicSize = newSize;
    }

    void UpdateBounds()
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth  = halfHeight * cam.aspect;

        float visibleHeight = 2f * halfHeight;

        // Reposition
        leftCol.transform.localPosition  = new Vector3(-halfWidth, 0f, 0f);
        rightCol.transform.localPosition = new Vector3( halfWidth, 0f, 0f);

        // Resize height to match camera
        leftCol.size  = new Vector2(leftCol.size.x, visibleHeight);
        rightCol.size = new Vector2(rightCol.size.x, visibleHeight);
    }

    float GetGreatestDistance()
    {
        if (target1 == null || target2 == null)
            return 0f;

        float distance = Mathf.Abs(target1.position.y - target2.position.y);
        return distance;
    }

    public void RegisterPlayer(Transform player)
    {
        if (target1 == null)
            target1 = player;
        else if (target2 == null)
            target2 = player;
    }
}