using UnityEngine;

public class MultiTargetCamera : MonoBehaviour
{
    public Transform target1;
    public Transform target2;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float smoothTime = 0.5f; // For smooth movement

    private Vector3 velocity;

    void LateUpdate()
    {
        if (target1 == null || target2 == null)
            return;

        // Calculate the midpoint between the two targets
        Vector3 centerPoint = (target1.position + target2.position) / 2f;

        // Calculate the desired new position
        Vector3 newPosition = centerPoint + offset;

        // Smoothly move the camera to the new position
        transform.position = Vector3.SmoothDamp(transform.position, newPosition, ref velocity, smoothTime);
    }
}