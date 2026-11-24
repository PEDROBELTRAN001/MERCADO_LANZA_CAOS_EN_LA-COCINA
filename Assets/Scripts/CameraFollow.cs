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

    [Header("Seguimiento dinámico del mercado")]
    [Range(0.4f, 1f)] public float framingScale = 0.70f;
    public bool followWholeMarket = true;
    public float turnSmoothTime = 0.65f;
    public float maxTurnSpeed = 100f;
    public float lookAhead = 0.8f;
    public float focusHeight = 0.6f;

    private Camera cam;
    private float targetZoom;
    private float targetPitchX;
    private Vector3 currentTargetOffset;
    private bool isOverriddenByContext = false;
    private PlayerController actor;
    private Vector3 smoothFocus;
    private float yaw, yawVelocity, distance;
    private float MinimumGameplayZoom => Mathf.Max(2.8f, minZoom * framingScale);

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

        targetZoom = Mathf.Clamp(startZoom * framingScale, MinimumGameplayZoom, maxZoom);
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
        actor = target != null ? target.GetComponent<PlayerController>() : null;
        yaw = transform.eulerAngles.y;
        distance = offset.magnitude * framingScale;
        if (target != null) smoothFocus = target.position + Vector3.up * focusHeight;
    }

    private void Update()
    {
        if (!PlayerController.GameplayActive || cam == null || !cam.orthographic || Mouse.current == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            isOverriddenByContext = false;
            targetZoom = Mathf.Clamp(targetZoom - Mathf.Sign(scroll) * zoomStep, MinimumGameplayZoom, maxZoom);
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (actor == null || actor.transform != target) actor = target.GetComponent<PlayerController>();
        float dt = Time.deltaTime;
        Vector3 motion = actor != null && PlayerController.GameplayActive ? actor.MovementDirection : Vector3.zero;
        if (motion.sqrMagnitude > 0.01f)
            yaw = Mathf.SmoothDampAngle(yaw, Mathf.Atan2(motion.x, motion.z) * Mathf.Rad2Deg,
                ref yawVelocity, turnSmoothTime, maxTurnSpeed, dt);

        // Acerca el encuadre; al correr deja un poco más de camino visible.
        if (cam != null && cam.orthographic)
        {
            float runningRoom = actor != null && actor.SprintHeld && motion.sqrMagnitude > 0.01f ? 0.5f : 0f;
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom + runningRoom, 1f - Mathf.Exp(-zoomSmoothSpeed * dt));
        }

        // 2. Suavizado de la inclinación en X
        Quaternion targetRot = Quaternion.Euler(targetPitchX, yaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-rotationSmoothSpeed * dt));

        // 3. Seguimiento suave del objetivo
        Vector3 focus = target.position + Vector3.up * focusHeight + motion * lookAhead;
        if (useBounds && !followWholeMarket)
        {
            focus.x = Mathf.Clamp(focus.x, minXZ.x, maxXZ.x);
            focus.z = Mathf.Clamp(focus.z, minXZ.y, maxXZ.y);
        }

        smoothFocus = Vector3.Lerp(smoothFocus, focus, 1f - Mathf.Exp(-smoothSpeed * dt));
        distance = Mathf.Lerp(distance, currentTargetOffset.magnitude * framingScale, 1f - Mathf.Exp(-zoomSmoothSpeed * dt));
        transform.position = smoothFocus - transform.forward * distance;
    }

    // --- Métodos contextuales llamados por CameraZoomTrigger ---

    public void SetContextView(float zoom, float pitchX, Vector3 customOffset)
    {
        targetZoom = Mathf.Clamp(zoom * framingScale, MinimumGameplayZoom, maxZoom);
        targetPitchX = pitchX;
        currentTargetOffset = customOffset;
        isOverriddenByContext = true;
    }

    public void ResetToDefaultView()
    {
        if (isOverriddenByContext)
        {
            targetZoom = Mathf.Clamp(startZoom * framingScale, MinimumGameplayZoom, maxZoom);
            targetPitchX = defaultPitchX;
            currentTargetOffset = offset;
            isOverriddenByContext = false;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        actor = target != null ? target.GetComponent<PlayerController>() : null;
    }
}
