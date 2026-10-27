using UnityEngine;
using UnityEngine.InputSystem;

// La entrada humana inicia el ataque; el impacto valida el objeto y su recogida exacta.
public class TackleSabotage : MonoBehaviour
{
    [Min(0.1f)] public float warningSeconds = 1.5f;
    [Min(0.1f)] public float cooldownSeconds = 25f;
    [Min(0.1f)] public float acquisitionRange = 3.5f;
    [Min(0.1f)] public float dashSeconds = 0.25f;
    [Min(0.1f)] public float dashSpeed = 9f;
    public float stunSeconds = 0.6f;
    public float protectionSeconds = 3f;
    private PlayerController owner, victim;
    private GameObject expectedItem;
    private int expectedRevision;
    private float launchAt, finishAt, availableAt;
    private Vector3 dashDirection;
    private bool preparing, dashing;
    public bool IsPreparing => preparing;
    public bool IsDashing => dashing;
    public Vector3 DashVelocity => dashDirection * dashSpeed;
    public float CooldownRemaining => Mathf.Max(0f, availableAt - Time.time);

    private void Awake() { owner = GetComponent<PlayerController>(); }
    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState == GameManager.GameState.GameOver ||
            GameManager.Instance.CurrentState == GameManager.GameState.Ready)
        { Cancel(); return; }
        // Una pausa congela el ataque; no consume su aviso ni genera otro intento.
        if (!PlayerController.GameplayActive) return;
        if (owner == null || owner.role != PlayerController.PlayerRole.Impostor || owner.IsStunned || owner.IsDetained)
        { Cancel(); return; }
        if (preparing)
        {
            if (!SameCarriedItem()) { Cancel(); return; }
            if (Time.time >= launchAt)
            {
                Vector3 direction = victim.transform.position - transform.position;
                direction.y = 0f;
                // La dirección se fija al lanzar: el trabajador puede esquivar lateralmente.
                dashDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
                preparing = false;
                dashing = true;
                finishAt = Time.time + dashSeconds;
            }
        }
        if (dashing && Time.time >= finishAt) Cancel();
        if (!owner.usesAI && Keyboard.current != null && Keyboard.current.uKey.wasPressedThisFrame && !preparing && !dashing)
            TryBeginAttack();
    }

    public bool TryBeginAttack()
    {
        if (!PlayerController.GameplayActive || owner == null || owner.role != PlayerController.PlayerRole.Impostor ||
            owner.IsStunned || owner.IsDetained || preparing || dashing || Time.time < availableAt) return false;
        PlayerController closest = null;
        float best = acquisitionRange;
        foreach (var candidate in FindObjectsByType<PlayerController>())
        {
            if (candidate == owner || candidate.role != PlayerController.PlayerRole.Worker ||
                candidate.IsProtected || candidate.IsDetained || !candidate.HasSabotageTarget) continue;
            Vector3 delta = candidate.transform.position - transform.position;
            if (Mathf.Abs(delta.y) > 1f || delta.magnitude > best) continue;
            delta.y = 0f;
            if (Vector3.Dot(transform.forward, delta.normalized) < 0.25f || !HasClearPath(candidate)) continue;
            best = delta.magnitude;
            closest = candidate;
        }
        if (closest == null)
        {
            GameHUD.Notify("Acércate y mira a un trabajador que lleve un ingrediente o plato terminado.");
            return false;
        }
        victim = closest;
        expectedItem = closest.HeldItem;
        expectedRevision = closest.CarryRevision;
        availableAt = Time.time + cooldownSeconds;
        launchAt = Time.time + warningSeconds;
        preparing = true;
        FloatingWindows.ShowTackleWarning(closest.playerName, warningSeconds);
        return true;
    }

    public bool HasClearPath(PlayerController target)
    {
        Vector3 from = transform.position + Vector3.up * 0.6f;
        Vector3 to = target.transform.position + Vector3.up * 0.6f;
        foreach (var hit in Physics.RaycastAll(from, (to - from).normalized, Vector3.Distance(from, to),
                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            Transform t = hit.collider.transform;
            if (!t.IsChildOf(transform) && !t.IsChildOf(target.transform)) return false;
        }
        return true;
    }

    private bool SameCarriedItem() => victim != null && expectedItem != null &&
        victim.HeldItem == expectedItem && victim.CarryRevision == expectedRevision;

    private void OnCollisionEnter(Collision collision) { ResolveCollision(collision); }
    private void OnCollisionStay(Collision collision) { ResolveCollision(collision); }
    private void ResolveCollision(Collision collision)
    {
        if (!dashing || !PlayerController.GameplayActive) return;
        var hitPlayer = collision.collider.GetComponentInParent<PlayerController>();
        if (hitPlayer == victim && SameCarriedItem() && HasClearPath(victim))
        {
            if (victim.LoseHeldItem(expectedItem, expectedRevision, dashDirection, stunSeconds, protectionSeconds, owner))
                owner.GetComponent<CharacterAppearance>()?.MarkSabotage();
        }
        // Un choque con pared, mueble u otro personaje también detiene la embestida.
        if (hitPlayer != null || Mathf.Abs(collision.GetContact(0).normal.y) < 0.5f) Cancel();
    }

    private void Cancel() { preparing = dashing = false; victim = null; expectedItem = null; }
    public void SuspendForExpulsion() { Cancel(); availableAt = Time.time + cooldownSeconds; }
    public void ResetForRound() { Cancel(); availableAt = 0f; }
    private void OnDisable() { Cancel(); }
}
