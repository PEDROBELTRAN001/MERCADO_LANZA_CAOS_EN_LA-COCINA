using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    [Tooltip("Multiplicador de velocidad al correr con Shift")]
    public float sprintMultiplier = 1.6f;

    [Header("Interaction")]
    public float interactionRange = 1.5f;
    public LayerMask interactableLayer;

    private Vector3 moveDirection;
    private Rigidbody rb;
    private Interactable currentInteractable;

    // Referencia al Animator
    private Animator animator;

    // Lo que el jugador tiene enfrente (lo usa el HUD para mostrar "E · ...")
    public Interactable CurrentInteractable => currentInteractable;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.freezeRotation = true;
            rb.useGravity = true;
        }

        // Busca el Animator en este objeto o en su hijo Camarero_color
        animator = GetComponentInChildren<Animator>();

        if (SimpleInput.Instance == null)
        {
            GameObject inputObj = new GameObject("SimpleInput");
            inputObj.AddComponent<SimpleInput>();
        }
    }

    private void Update()
    {
        CalculateMoveDirection();
        UpdateAnimation(); // Actualiza la animación según el movimiento
        DetectInteractable();

        if (SimpleInput.Instance != null && SimpleInput.Instance.InteractPressed)
        {
            TryInteract();
        }
    }

    private void FixedUpdate()
    {
        ApplyMovementAndRotation();
    }

    private void CalculateMoveDirection()
    {
        Vector2 input = SimpleInput.Instance != null ? SimpleInput.Instance.MoveInput : Vector2.zero;

        if (input.magnitude > 0.1f)
        {
            Vector3 forward = Camera.main.transform.forward;
            Vector3 right = Camera.main.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            moveDirection = (forward * input.y + right * input.x).normalized;
        }
        else
        {
            moveDirection = Vector3.zero;
        }
    }

    private void UpdateAnimation()
    {
        if (animator != null)
        {
            // Si hay dirección de movimiento, isWalking será true
            bool isMoving = moveDirection != Vector3.zero;
            animator.SetBool("isWalking", isMoving);
        }
    }

    private void ApplyMovementAndRotation()
    {
        // 1. Rotación instantánea
        if (moveDirection != Vector3.zero)
        {
            rb.MoveRotation(Quaternion.LookRotation(moveDirection));
        }

        // 2. Aplicar velocidad física recta
        bool sprint = SimpleInput.Instance != null && SimpleInput.Instance.SprintHeld;
        Vector3 velocity = moveDirection * moveSpeed * (sprint ? sprintMultiplier : 1f);
        velocity.y = rb.linearVelocity.y; // Mantener la gravedad
        rb.linearVelocity = velocity;
    }

    private void DetectInteractable()
    {
        float radius = interactionRange * 0.6f;
        Vector3 center = transform.position + transform.forward * radius;
        Collider[] hits = Physics.OverlapSphere(center, radius, interactableLayer, QueryTriggerInteraction.Collide);

        Interactable newInteractable = null;
        float best = float.MaxValue;
        foreach (Collider hit in hits)
        {
            Interactable candidate = hit.GetComponentInParent<Interactable>();
            if (candidate == null || candidate.transform.IsChildOf(transform)) continue;

            float distance = (hit.ClosestPoint(center) - center).sqrMagnitude;
            if (distance < best)
            {
                best = distance;
                newInteractable = candidate;
            }
        }

        if (newInteractable != currentInteractable)
        {
            if (currentInteractable != null)
                currentInteractable.HidePrompt();

            currentInteractable = newInteractable;

            if (currentInteractable != null)
                currentInteractable.ShowPrompt();
        }
    }

    private void TryInteract()
    {
        if (currentInteractable != null)
        {
            currentInteractable.Interact(this);
        }
    }

    public void Teleport(Vector3 position)
    {
        transform.position = position;
        rb.linearVelocity = Vector3.zero;
    }
}