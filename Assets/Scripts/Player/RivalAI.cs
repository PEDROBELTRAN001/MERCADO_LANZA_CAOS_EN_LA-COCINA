using UnityEngine;
using UnityEngine.AI;

// El azar se evalúa una vez por recogida; el ataque final usa las mismas reglas que el jugador humano.
public class RivalAI : MonoBehaviour
{
    [Range(0f, 1f)] public float sabotageChance = 0.4f;
    public float minimumInterval = 22f;
    public float maximumInterval = 32f;
    public float pursuitSeconds = 12f;
    public float graceSeconds = 5f;
    private PlayerController owner, worker;
    private TackleSabotage attack;
    private GameObject targetItem;
    private int targetRevision;
    private float nextAttempt, chaseUntil, repathAt, patrolAt;
    private Vector3 home, goal;
    private NavMeshPath path;
    private int corner;
    public bool Active { get; private set; }
    public bool IsPursuing => targetItem != null;
    public void Initialize(PlayerController target)
    {
        owner = GetComponent<PlayerController>();
        worker = target;
        attack = GetComponent<TackleSabotage>();
        path = new NavMeshPath();
        worker.OnItemPickedUp += ConsiderPickup;
        GameManager.Instance.OnGameStarted += ResetForRound;
    }
    public void SetActive(bool value)
    {
        Active = value;
        owner.usesAI = value;
        ResetForRound();
    }
    public void ResetForRound()
    {
        if (owner == null) return;
        targetItem = null;
        nextAttempt = Time.time + graceSeconds;
        chaseUntil = repathAt = patrolAt = 0f;
        home = owner.transform.position;
        goal = home;
        owner.SetAIMovement(Vector3.zero);
        path?.ClearCorners();
    }
    public void ConsiderPickup(PlayerController player, GameObject item)
    {
        if (!Active || !PlayerController.GameplayActive || owner.IsDetained || owner.IsStunned || player != worker ||
            !player.HasSabotageTarget || Time.time < nextAttempt || attack.CooldownRemaining > 0f || IsPursuing) return;
        // Incluso un resultado sin ataque tiene un descanso, para no repetir el sorteo al devolver el mismo insumo.
        nextAttempt = Time.time + Random.Range(minimumInterval, Mathf.Max(minimumInterval, maximumInterval));
        if (Random.value >= sabotageChance) return;
        targetItem = item;
        targetRevision = player.CarryRevision;
        chaseUntil = Time.time + pursuitSeconds;
        repathAt = 0f;
        GameHUD.Warn("¡Posible sabotaje! Un garzón sospechoso se acerca. Mantente en movimiento.", 3f);
    }
    private Vector3 Feet => new Vector3(owner.transform.position.x,
        owner.GetComponent<Collider>().bounds.min.y, owner.transform.position.z);
    private void Update()
    {
        if (owner == null) return;
        owner.SetAIMovement(Vector3.zero);
        if (!Active || !PlayerController.GameplayActive || owner.IsDetained || owner.IsStunned) return;
        var navigation = InvestigationSystem.Instance != null ? InvestigationSystem.Instance.Navigation : null;
        if (navigation == null || !navigation.Ready) return;
        if (IsPursuing && (worker.IsDetained || worker.HeldItem != targetItem || worker.CarryRevision != targetRevision || Time.time >= chaseUntil))
        { targetItem = null; repathAt = 0f; }
        if (attack.IsDashing) return;
        if (IsPursuing)
        {
            Vector3 delta = worker.transform.position - owner.transform.position;
            delta.y = 0f;
            if (!attack.IsPreparing && delta.magnitude <= attack.acquisitionRange &&
                Vector3.Dot(owner.transform.forward, delta.normalized) > 0.4f && attack.HasClearPath(worker))
                attack.TryBeginAttack();
            goal = worker.transform.position - Vector3.up * 0.75f;
        }
        else if (Time.time >= patrolAt)
        {
            patrolAt = Time.time + Random.Range(3f, 5f);
            Vector2 offset = Random.insideUnitCircle * 5f;
            Vector3 center = worker.transform.position - Vector3.up * 0.75f;
            if (!navigation.CanReach(Feet, center + new Vector3(offset.x, 0f, offset.y), out goal)) goal = Feet;
        }
        if (Time.time >= repathAt)
        {
            repathAt = Time.time + 0.45f;
            if (navigation.Sample(Feet, 1.5f, out Vector3 from) && navigation.Sample(goal, 1.5f, out Vector3 to))
            {
                var filter = new NavMeshQueryFilter { agentTypeID = navigation.AgentType, areaMask = NavMesh.AllAreas };
                NavMesh.CalculatePath(from, to, filter, path);
                corner = path.corners.Length > 1 ? 1 : 0;
            }
        }
        Vector3[] corners = path.corners;
        if (path.status != NavMeshPathStatus.PathComplete || corner >= corners.Length) return;
        Vector3 next = corners[corner];
        if (Vector3.Distance(Feet, next) < 0.35f && corner < corners.Length - 1) next = corners[++corner];
        if (Mathf.Abs(next.y - Feet.y) > 0.7f)
        {
            foreach (var stair in FindObjectsByType<StairTrigger>())
                if (stair.TryUseForAI(owner)) { repathAt = 0f; return; }
        }
        Vector3 direction = next - Feet;
        direction.y = 0f;
        if (direction.magnitude > 0.25f)
            owner.SetAIMovement(direction.normalized * (IsPursuing ? 0.88f : 0.42f));
    }
    private void OnDestroy()
    {
        if (worker != null) worker.OnItemPickedUp -= ConsiderPickup;
        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted -= ResetForRound;
    }
}
