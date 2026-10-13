using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f;

    [Header("Interaction")]
    public float interactionRange = 1.5f;
    public LayerMask interactableLayer;

    private Vector2 moveInput;
    private Rigidbody rb;
    private Interactable currentInteractable;

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
        // 1. Solo leemos el input en Update para mayor precisión
        moveInput = SimpleInput.Instance != null ? SimpleInput.Instance.MoveInput : Vector2.zero;

        DetectInteractable();

        if (SimpleInput.Instance != null && SimpleInput.Instance.InteractPressed)
        {
            TryInteract();
        }
    }

    private void FixedUpdate()
    {
        // 2. Todo el movimiento y físicas se procesan estrictamente en FixedUpdate
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (moveInput.magnitude > 0.1f)
        {
            Vector3 cameraForward = Camera.main.transform.forward;
            Vector3 cameraRight = Camera.main.transform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
            }

            // Usar MovePosition en lugar de alterar la velocidad directamente previene que atraviese paredes
            Vector3 targetPosition = rb.position + moveDirection * (moveInput.magnitude * moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(targetPosition);
        }
        else
        {
            // Mantener solo la velocidad vertical (gravedad) cuando no hay input
            Vector3 velocity = rb.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            rb.linearVelocity = velocity;
        }
    }

    private void DetectInteractable()
    {
        Ray ray = new Ray(transform.position + Vector3.up, transform.forward);
        RaycastHit hit;

        Interactable newInteractable = null;

        if (Physics.Raycast(ray, out hit, interactionRange, interactableLayer))
        {
            newInteractable = hit.collider.GetComponent<Interactable>();
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