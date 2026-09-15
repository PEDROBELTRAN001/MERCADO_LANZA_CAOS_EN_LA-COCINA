using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0f, 10f, -8f);
    public float smoothSpeed = 5f;
    public bool useFixedOffset = true;

    [Header("Bounds")]
    public bool useBounds = true;
    public Vector2 minXZ = new Vector2(-10f, -10f);
    public Vector2 maxXZ = new Vector2(10f, 10f);

    private void LateUpdate()
    {
        if (target == null) return;

        FollowTarget();
    }

    private void FollowTarget()
    {
        Vector3 desiredPosition;

        if (useFixedOffset)
        {
            desiredPosition = target.position + offset;
        }
        else
        {
            desiredPosition = target.position + transform.rotation * offset;
        }

        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minXZ.x, maxXZ.x);
            desiredPosition.z = Mathf.Clamp(desiredPosition.z, minXZ.y, maxXZ.y);
        }

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
