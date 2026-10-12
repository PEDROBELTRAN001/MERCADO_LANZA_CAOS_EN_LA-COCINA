using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0, 14, -12);
    public float smoothSpeed = 5f;

    [Header("Bounds")]
    public bool useBounds = true;
    public Vector2 minXZ = new Vector2(-16, -8);
    public Vector2 maxXZ = new Vector2(16, 8);

    private void Start()
    {
        // Si no se asignó un objetivo manualmente, buscar al jugador de forma automática
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogWarning("CameraFollow: No se encontró un objeto con el Tag 'Player'.");
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

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