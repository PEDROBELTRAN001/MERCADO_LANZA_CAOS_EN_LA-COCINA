using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f;

    [Header("Interaction")]
    public float interactionRange = 1.5f;
    public LayerMask interactableLayer;

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

        if (SimpleInput.Instance == null)
        {
            GameObject inputObj = new GameObject("SimpleInput");
            inputObj.AddComponent<SimpleInput>();
        }
    }

    private void Update()
    {
        HandleMovement();
        DetectInteractable();

        if (SimpleInput.Instance != null && SimpleInput.Instance.InteractPressed)
        {
            TryInteract();
        }
    }

    private void HandleMovement()
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

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
        else
        {
            moveDirection = Vector3.zero;
        }

        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
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
