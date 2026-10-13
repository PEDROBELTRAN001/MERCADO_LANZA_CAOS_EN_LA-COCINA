using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0, 14, -12);
    public float smoothSpeed = 5f;

    [Header("Bounds (zona que puede mirar la camara)")]
    public bool useBounds = true;
    public Vector2 minXZ = new Vector2(-16, -8);
    public Vector2 maxXZ = new Vector2(16, 8);

    [Header("Zoom con la rueda del mouse (camara ortografica)")]
    public float zoomStep = 2f;
    public float minZoom = 6f;
    public float maxZoom = 70f;

    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();

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

    private void Update()
    {
        if (cam == null || !cam.orthographic || Mouse.current == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - Mathf.Sign(scroll) * zoomStep, minZoom, maxZoom);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Se limita el punto que mira la camara (no la camara, que va desplazada por el offset)
        Vector3 focus = target.position;
        if (useBounds)
        {
            focus.x = Mathf.Clamp(focus.x, minXZ.x, maxXZ.x);
            focus.z = Mathf.Clamp(focus.z, minXZ.y, maxXZ.y);
        }
        Vector3 desiredPosition = focus + offset;

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}