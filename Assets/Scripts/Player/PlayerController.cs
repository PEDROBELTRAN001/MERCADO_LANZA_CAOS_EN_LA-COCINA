using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    // Eliminamos la variable rotationSpeed para forzar la rotación ágil por código

    [Header("Interaction")]
    public float interactionRange = 1.5f;
    public LayerMask interactableLayer;

    private Vector3 moveDirection;
    private Rigidbody rb;
    private Interactable currentInteractable;

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

        if (SimpleInput.Instance == null)
        {
            GameObject inputObj = new GameObject("SimpleInput");
            inputObj.AddComponent<SimpleInput>();
        }
    }

    private void Update()
    {
        CalculateMoveDirection();
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

    private void ApplyMovementAndRotation()
    {
        // 1. Rotación instantánea (elimina por completo el efecto de semicircunferencia)
        if (moveDirection != Vector3.zero)
        {
            rb.MoveRotation(Quaternion.LookRotation(moveDirection));
        }

        // 2. Aplicar velocidad física recta
        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = rb.linearVelocity.y; // Mantener la gravedad
        rb.linearVelocity = velocity;
    }

    private void DetectInteractable()
    {
        // Esfera delante del jugador: detecta estaciones de cualquier altura (un rayo a la altura del
        // pecho pasaba por encima de mostradores y mesas de 0.9 m). Gana la mas cercana al centro.
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