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
        if (target1 == null && target2 == null)
            return;

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

    public void RegisterPlayer(Transform player)
    {
        if (target1 == null)
            target1 = player;
        else if (target2 == null)
            target2 = player;
    }
}