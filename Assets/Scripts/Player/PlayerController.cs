using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public enum LocalControls { WASD, IJKL }
    public enum PlayerRole { Worker, Impostor }

    [Header("Jugador local")]
    public LocalControls controls;
    public PlayerRole role;
    public Camera viewCamera;
    public string playerName = "Tomás";
    [Header("Movement")]
    public float moveSpeed = 5f;
    [Header("Interaction")]
    public float interactionRange = 1.5f;
    public LayerMask interactableLayer;

    private Rigidbody rb;
    private Animator animator;
    private bool hasWalkingParameter;
    private Vector3 moveDirection;
    private Vector3 aiMovement;
    public bool usesAI;
    public void SetAIMovement(Vector3 direction) { aiMovement = Vector3.ClampMagnitude(direction, 1f); }
    private Interactable currentInteractable;
    private Transform hand;
    private float stunnedUntil, protectedUntil;
    private Vector3 knockback;
    public event Action<PlayerController, GameObject> OnItemPickedUp;
    public Interactable CurrentInteractable => currentInteractable;
    public string InteractionKey => controls == LocalControls.WASD ? "E" : "O";
    public bool IsStunned => Time.time < stunnedUntil;
    public bool IsProtected => Time.time < protectedUntil;
    public bool IsDetained { get; private set; }
    private static int nextInvestigationId;
    public int InvestigationId { get; private set; }
    private Renderer[] hiddenRenderers;
    private Collider[] disabledColliders;
    private bool wasKinematic;
    public int CarryRevision { get; private set; }
    public static bool GameplayActive => GameManager.Instance != null &&
        GameManager.Instance.CurrentState == GameManager.GameState.Playing &&
        Time.timeScale > 0f && !FloatingWindows.BlocksInput && !GameFrontEnd.BlocksInput;
    public Transform Hand
    {
        get
        {
            if (hand == null) hand = transform.Find("Hand");
            if (hand == null)
            {
                hand = new GameObject("Hand").transform;
                hand.SetParent(transform, false);
                hand.localPosition = new Vector3(0.6f, 0.5f, 0.5f);
            }
            // La escena anterior guardaba Hand desactivada, ocultando lo transportado.
            if (!hand.gameObject.activeSelf) hand.gameObject.SetActive(true);
            return hand;
        }
    }
    public GameObject HeldItem => Hand.childCount > 0 ? Hand.GetChild(0).gameObject : null;
    public bool HasSabotageTarget => HeldItem != null &&
        (HeldItem.GetComponent<Ingredient>() != null || HeldItem.GetComponent<DishItem>() != null);

    private void Awake()
    {
        InvestigationId = ++nextInvestigationId;
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        RefreshAnimator();
    }

    public void RefreshAnimator()
    {
        hasWalkingParameter = false;
        animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
            foreach (var p in animator.parameters)
                if (p.name == "isWalking" && p.type == AnimatorControllerParameterType.Bool)
                    hasWalkingParameter = true;
    }

    private void Update()
    {
        moveDirection = Vector3.zero;
        if (GameplayActive && !IsStunned && !IsDetained)
        {
            Vector2 input = ReadMovement(controls, Keyboard.current);
            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            Vector3 forward = cam != null ? cam.transform.forward : Vector3.forward;
            Vector3 right = cam != null ? cam.transform.right : Vector3.right;
            forward.y = right.y = 0f;
            moveDirection = usesAI ? aiMovement : Vector3.ClampMagnitude(forward.normalized * input.y + right.normalized * input.x, 1f);
            if (!usesAI) DetectInteractable();
            if (!usesAI && InteractionPressed && currentInteractable != null)
                currentInteractable.Interact(this);
        }
        else SetInteractable(null);
        if (hasWalkingParameter) animator.SetBool("isWalking", moveDirection.sqrMagnitude > 0.01f);
    }

    public static Vector2 ReadMovement(LocalControls scheme, Keyboard keyboard)
    {
        if (keyboard == null) return Vector2.zero;
        bool second = scheme == LocalControls.IJKL;
        float x = ((second ? keyboard.lKey : keyboard.dKey).isPressed ? 1f : 0f) -
                  ((second ? keyboard.jKey : keyboard.aKey).isPressed ? 1f : 0f);
        float y = ((second ? keyboard.iKey : keyboard.wKey).isPressed ? 1f : 0f) -
                  ((second ? keyboard.kKey : keyboard.sKey).isPressed ? 1f : 0f);
        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }

    public bool InteractionPressed => !usesAI && Keyboard.current != null &&
        (controls == LocalControls.WASD ? Keyboard.current.eKey : Keyboard.current.oKey).wasPressedThisFrame;

    private void FixedUpdate()
    {
        Vector3 horizontal = Vector3.zero;
        if (IsDetained) return;
        if (GameplayActive)
        {
            TackleSabotage attack = GetComponent<TackleSabotage>();
            if (IsStunned) horizontal = knockback;
            else if (attack != null && attack.IsDashing) horizontal = attack.DashVelocity;
            else horizontal = moveDirection * moveSpeed * (attack != null && attack.IsPreparing ? 0.45f : 1f);
            if (!IsStunned && horizontal.sqrMagnitude > 0.01f)
                rb.MoveRotation(Quaternion.LookRotation(horizontal));
        }
        horizontal.y = rb.linearVelocity.y;
        rb.linearVelocity = horizontal;
    }

    private void DetectInteractable()
    {
        float radius = interactionRange * 0.6f;
        Vector3 center = transform.position + transform.forward * radius;
        Interactable bestTarget = null;
        float best = float.MaxValue;
        foreach (Collider hit in Physics.OverlapSphere(center, radius, interactableLayer, QueryTriggerInteraction.Collide))
        {
            Interactable candidate = hit.GetComponentInParent<Interactable>();
            if (candidate == null || candidate.transform.IsChildOf(transform)) continue;
            if (candidate is SabotageEvidence && role != PlayerRole.Worker) continue;
            if (candidate is CapibaraPolice && role != PlayerRole.Worker) continue;
            if ((candidate is SabotageEvidence || candidate is CapibaraPolice) && !CanInspect(candidate)) continue;
            float distance = (hit.ClosestPoint(center) - center).sqrMagnitude;
            if (distance < best) { best = distance; bestTarget = candidate; }
        }
        SetInteractable(bestTarget);
    }

    private void SetInteractable(Interactable target)
    {
        if (target == currentInteractable) return;
        if (currentInteractable != null) currentInteractable.HidePrompt();
        currentInteractable = target;
        if (currentInteractable != null) currentInteractable.ShowPrompt();
    }

    public bool TryHoldItem(GameObject item)
    {
        if (IsDetained || item == null || HeldItem != null) return false;
        CarryRevision++;
        foreach (Collider col in item.GetComponentsInChildren<Collider>()) col.enabled = false;
        Rigidbody itemBody = item.GetComponent<Rigidbody>();
        if (itemBody != null) { itemBody.isKinematic = true; itemBody.useGravity = false; }
        item.transform.SetParent(Hand, false);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        OnItemPickedUp?.Invoke(this, item);
        return true;
    }

    public GameObject ReleaseHeldItem()
    {
        GameObject item = HeldItem;
        if (item == null) return null;
        CarryRevision++;
        item.transform.SetParent(null, true);
        return item;
    }

    public bool LoseHeldItem(GameObject expected, int revision, Vector3 direction, float stun, float protection, PlayerController attacker)
    {
        if (!GameplayActive || IsDetained || IsProtected || expected == null || HeldItem != expected || CarryRevision != revision)
            return false;
        GameObject lost = ReleaseHeldItem();
        string lostName = lost.name;
        SabotageEvidence.Create(attacker, this, lostName, transform.position);
        Destroy(lost);
        stunnedUntil = Time.time + stun;
        protectedUntil = Time.time + protection;
        direction.y = 0f;
        knockback = direction.normalized * 2.5f;
        GameHUD.Warn(playerName + " perdió: " + lostName + ". ¡Puedes preparar un reemplazo!");
        return true;
    }

    public bool CanInspect(Interactable target)
    {
        Vector3 from = transform.position + Vector3.up * 0.4f;
        Vector3 to = target.transform.position + Vector3.up * 0.4f;
        foreach (var hit in Physics.RaycastAll(from, (to - from).normalized, Vector3.Distance(from, to),
                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (!hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(target.transform)) return false;
        return true;
    }

    public void SetDetained(bool value)
    {
        if (IsDetained == value) return;
        IsDetained = value;
        if (value)
        {
            GameObject held = ReleaseHeldItem();
            if (held != null) Destroy(held);
            hiddenRenderers = System.Array.FindAll(GetComponentsInChildren<Renderer>(true), r => r.enabled);
            disabledColliders = System.Array.FindAll(GetComponentsInChildren<Collider>(true), c => c.enabled);
            foreach (var renderer in hiddenRenderers) renderer.enabled = false;
            foreach (var collider in disabledColliders) collider.enabled = false;
            rb.linearVelocity = Vector3.zero;
            wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
            moveDirection = Vector3.zero;
            SetInteractable(null);
            GetComponent<TackleSabotage>()?.SuspendForExpulsion();
        }
        else
        {
            if (hiddenRenderers != null) foreach (var renderer in hiddenRenderers) if (renderer != null) renderer.enabled = true;
            if (disabledColliders != null) foreach (var collider in disabledColliders) if (collider != null) collider.enabled = true;
            rb.isKinematic = wasKinematic;
        }
    }

    public void ResetForRound(Vector3 position, Quaternion rotation)
    {
        SetDetained(false);
        GameObject item = ReleaseHeldItem();
        if (item != null) Destroy(item);
        GetComponent<CharacterAppearance>()?.ResetVisualState();
        stunnedUntil = protectedUntil = 0f;
        moveDirection = knockback = aiMovement = Vector3.zero;
        Teleport(position);
        transform.rotation = rotation;
        rb.rotation = rotation;
        SetInteractable(null);
    }

    public void Teleport(Vector3 position)
    {
        transform.position = position;
        rb.position = position;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
