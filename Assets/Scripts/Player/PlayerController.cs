using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Interaction")]
    public float interactionRange = 1.5f;
    public LayerMask interactableLayer;

    private Vector3 moveDirection;
    private Rigidbody rb;
    private Animator anim; // Referencia al Animator
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

        // Busca el Animator en el hijo (Jogging)
        anim = GetComponentInChildren<Animator>();

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
        UpdateAnimation(); // Actualizar el estado de animación

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
        if (anim == null) return;

        // Comprobamos si el jugador se está moviendo
        bool isMoving = moveDirection.sqrMagnitude > 0.01f;

        // Si usas un parámetro Booleano en tu Animator Controller:
        anim.SetBool("isWalking", isMoving);

        // Si prefieres usar un Float (Blend Tree):
        // anim.SetFloat("Speed", isMoving ? moveDirection.magnitude : 0f);
    }

    private void ApplyMovementAndRotation()
    {
        // 1. Rotación instantánea
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