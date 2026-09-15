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
    private Vector3 moveDirection;
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
    }

    private void Update()
    {
        HandleInput();
        HandleInteraction();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void HandleInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(horizontal, vertical);
        moveInput.Normalize();

        if (moveInput.magnitude > 0.1f)
        {
            Vector3 forward = Camera.main.transform.forward;
            Vector3 right = Camera.main.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );
        }
    }

    private void Move()
    {
        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
    }

    private void HandleInteraction()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }

        DetectInteractable();
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
            {
                currentInteractable.HidePrompt();
            }

            currentInteractable = newInteractable;

            if (currentInteractable != null)
            {
                currentInteractable.ShowPrompt();
            }
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
