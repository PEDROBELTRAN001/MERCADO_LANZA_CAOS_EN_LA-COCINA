using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public enum LocalControls { WASD, IJKL }
    public LocalControls controls;
    public Camera viewCamera;
    public bool usesAI;
    public enum PlayerRole { Worker, Impostor }
    public PlayerRole role;
    private static int nextIdentity;private int identity;
    public int InvestigationId=>identity;
    public bool IsDetained {get;private set;}
    public bool IsStunned=>false;
    public GameObject HeldItem=>Hand.childCount>0?Hand.GetChild(0).gameObject:null;
    private Renderer[] hidden;private Collider[] disabled;private bool previousKinematic;
    private void Awake(){identity=++nextIdentity;}
    public bool CanInspect(Interactable target)
    {
        if(target==null || IsDetained)return false;
        Vector3 from=transform.position+Vector3.up*.2f,to=target.transform.position+Vector3.up*.7f;
        foreach(var hit in Physics.RaycastAll(from,(to-from).normalized,Vector3.Distance(from,to),Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            if(!hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(target.transform))return false;
        return true;
    }
    public void SetDetained(bool value)
    {
        if(IsDetained==value)return;IsDetained=value;if(rb==null)rb=GetComponent<Rigidbody>();
        if(value)
        {
            hidden=System.Array.FindAll(GetComponentsInChildren<Renderer>(true),r=>r.enabled);disabled=System.Array.FindAll(GetComponentsInChildren<Collider>(true),c=>c.enabled);
            foreach(var renderer in hidden)renderer.enabled=false;foreach(var collider in disabled)collider.enabled=false;
            if(rb!=null){rb.linearVelocity=Vector3.zero;previousKinematic=rb.isKinematic;rb.isKinematic=true;}moveDirection=aiMovement=Vector3.zero;
            if(currentInteractable!=null)currentInteractable.HidePrompt();currentInteractable=null;
        }
        else
        {
            if(hidden!=null)foreach(var renderer in hidden)if(renderer!=null)renderer.enabled=true;
            if(disabled!=null)foreach(var collider in disabled)if(collider!=null)collider.enabled=true;
            if(rb!=null)rb.isKinematic=previousKinematic;
        }
    }
    private Vector3 aiMovement;
    public static bool GameplayActive => GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing && Time.timeScale > 0 && !FloatingWindows.BlocksInput;
    public string InteractionKey => controls == LocalControls.IJKL ? "O" : "E";
    public bool SprintHeld => !usesAI && Keyboard.current != null && (controls == LocalControls.WASD ? Keyboard.current.leftShiftKey.isPressed : Keyboard.current.rightShiftKey.isPressed);
    public Transform Hand
    {
        get
        {
            var hand=transform.Find("Hand");
            if(hand==null){hand=new GameObject("Hand").transform;hand.SetParent(transform,false);hand.localPosition=new Vector3(0,.35f,.95f);}
            hand.gameObject.SetActive(true);return hand;
        }
    }
    public void SetAIMovement(Vector3 direction){aiMovement=Vector3.ClampMagnitude(direction,1);}
    public static Vector2 ReadMovement(LocalControls controls,Keyboard keyboard)
    {
        if(keyboard==null)return Vector2.zero;
        if(controls==LocalControls.IJKL)return new Vector2((keyboard.lKey.isPressed?1:0)-(keyboard.jKey.isPressed?1:0),(keyboard.iKey.isPressed?1:0)-(keyboard.kKey.isPressed?1:0));
        return new Vector2((keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed?1:0)-(keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed?1:0),(keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed?1:0)-(keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed?1:0));
    }
    public void RefreshAnimator()
    {
        animator=null;
        foreach(var candidate in GetComponentsInChildren<Animator>())if(candidate.runtimeAnimatorController!=null){animator=candidate;break;}
    }
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
        RefreshAnimator();

        if (SimpleInput.Instance == null)
        {
            GameObject inputObj = new GameObject("SimpleInput");
            inputObj.AddComponent<SimpleInput>();
        }
    }

    private void Update()
    {
        if(!GameplayActive || IsDetained){moveDirection=Vector3.zero;UpdateAnimation();return;}
        CalculateMoveDirection();
        UpdateAnimation(); // Actualiza la animación según el movimiento
        DetectInteractable();

        if (!usesAI && Keyboard.current != null && (controls==LocalControls.IJKL ? Keyboard.current.oKey.wasPressedThisFrame : Keyboard.current.eKey.wasPressedThisFrame))
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
        if(usesAI){moveDirection=aiMovement;return;}
        Vector2 input = ReadMovement(controls,Keyboard.current);

        if (input.magnitude > 0.1f)
        {
            Camera reference=viewCamera!=null?viewCamera:Camera.main;
            Vector3 forward = reference!=null?reference.transform.forward:Vector3.forward;
            Vector3 right = reference!=null?reference.transform.right:Vector3.right;
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
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            // Si hay dirección de movimiento, isWalking será true
            bool isMoving = moveDirection != Vector3.zero;
            foreach(var parameter in animator.parameters)if(parameter.name=="isWalking" && parameter.type==AnimatorControllerParameterType.Bool){animator.SetBool("isWalking",isMoving);break;}
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
        if(IsDetained || rb.isKinematic)return;
        Vector3 velocity = GameplayActive ? moveDirection * moveSpeed * (SprintHeld ? sprintMultiplier : 1f) : Vector3.zero;
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
        if(rb==null)rb=GetComponent<Rigidbody>();
        if(rb!=null){rb.position=position;rb.linearVelocity=Vector3.zero;rb.angularVelocity=Vector3.zero;}
        moveDirection=aiMovement=Vector3.zero;
        if(currentInteractable!=null)currentInteractable.HidePrompt();currentInteractable=null;
    }
}
