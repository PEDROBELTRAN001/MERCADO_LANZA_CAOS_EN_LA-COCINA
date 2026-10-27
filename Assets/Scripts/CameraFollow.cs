using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Target")]
    public Transform target;

    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0, 14, -12);
    public float smoothSpeed = 5f;
    public float defaultPitchX = 50f;

    [Header("Velocidades de Suavizado")]
    public float zoomSmoothSpeed = 5f;
    public float rotationSmoothSpeed = 5f;

    [Header("Bounds (zona que puede mirar la camara)")]
    public bool useBounds = true;
    public Vector2 minXZ = new Vector2(-16, -8);
    public Vector2 maxXZ = new Vector2(16, 8);

    [Header("Zoom con Mouse (camara ortografica)")]
    public float zoomStep = 1.5f;
    public float minZoom = 4f;
    public float maxZoom = 15f;
    public float startZoom = 8f;

    private Camera cam;
    private float targetZoom;
    private float targetPitchX;
    private Vector3 currentTargetOffset;
    private bool isOverriddenByContext = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        cam = GetComponent<Camera>();

        targetZoom = Mathf.Clamp(startZoom, minZoom, maxZoom);
        targetPitchX = defaultPitchX;
        currentTargetOffset = offset;

        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize = targetZoom;
        }

        transform.rotation = Quaternion.Euler(defaultPitchX, 0f, 0f);

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
        {
            isOverriddenByContext = false;
            targetZoom = Mathf.Clamp(targetZoom - Mathf.Sign(scroll) * zoomStep, minZoom, maxZoom);
        }
    }

    private void LateUpdate()
    {
        // 1. Suavizado del Zoom
        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomSmoothSpeed * Time.deltaTime);
        }

        // 2. Suavizado de la inclinación en X
        Quaternion targetRot = Quaternion.Euler(targetPitchX, 0f, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmoothSpeed * Time.deltaTime);

        // 3. Seguimiento suave del objetivo
        if (target == null) return;

        Vector3 focus = target.position;
        if (useBounds)
        {
            focus.x = Mathf.Clamp(focus.x, minXZ.x, maxXZ.x);
            focus.z = Mathf.Clamp(focus.z, minXZ.y, maxXZ.y);
        }

        Vector3 desiredPosition = focus + currentTargetOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }

    // --- Métodos contextuales llamados por CameraZoomTrigger ---

    public void SetContextView(float zoom, float pitchX, Vector3 customOffset)
    {
        targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        targetPitchX = pitchX;
        currentTargetOffset = customOffset;
        isOverriddenByContext = true;
    }

    public void ResetToDefaultView()
    {
        if (isOverriddenByContext)
        {
            targetZoom = startZoom;
            targetPitchX = defaultPitchX;
            currentTargetOffset = offset;
            isOverriddenByContext = false;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}