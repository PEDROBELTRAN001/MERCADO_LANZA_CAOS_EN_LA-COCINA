using UnityEngine;
using UnityEngine.AI;

public class CapibaraPolice : Interactable
{
    public float patrolSpeed = 2f;
    public float pursuitSpeed = 5.7f;
    public float pursuitSeconds = 20f;
    public float captureDistance = 1.15f;
    public float patrolRadius = 6f;
    private InvestigationSystem system;
    private NavMeshAgent agent;
    private Vector3 home;
    private PlayerController suspect;
    private int proofId;
    private float chaseUntil, repathAt, patrolAt, traverseAt;
    private bool traversing;
    public bool IsChasing => suspect != null;
    public void Initialize(InvestigationSystem owner, Vector3 position)
    {
        system = owner;
        home = position;
        displayName = "Capibara-policía · presentar denuncia";
        interactionPrompt = "Presentar denuncia";
        agent = gameObject.AddComponent<NavMeshAgent>();
        agent.agentTypeID = system.Navigation.AgentType;
        agent.radius = 0.4f;
        agent.height = 1.6f;
        agent.speed = patrolSpeed;
        agent.acceleration = 18f;
        agent.angularSpeed = 360f;
        agent.stoppingDistance = 0.35f;
        agent.autoTraverseOffMeshLink = false;
    }
    public override void Interact(PlayerController player)
    {
        if (player.role != PlayerController.PlayerRole.Worker)
        { GameHUD.Notify("El Capibara vigila el mercado. Sigue con tus tareas."); return; }
        system.Report(player);
    }
    private Vector3 Feet(PlayerController player)
    {
        Collider body = player.GetComponent<Collider>();
        return body != null ? new Vector3(player.transform.position.x, body.bounds.min.y, player.transform.position.z) : player.transform.position;
    }
    public bool CanPursue(PlayerController player) => LocalMultiplayer.Instance!=null && player==LocalMultiplayer.Instance.Rival && !player.IsDetained && agent != null && agent.isOnNavMesh &&
        system.Navigation.CanReach(transform.position, Feet(player), out _);
    public void Pursue(PlayerController player, int incident)
    {
        var proof=system.Ledger.Find(incident);
        if(LocalMultiplayer.Instance==null || player!=LocalMultiplayer.Instance.Rival || player.IsDetained || proof==null || proof.Attacker!=player.InvestigationId || !proof.Reserved || proof.Used)return;
        suspect = player;
        proofId = incident;
        chaseUntil = Time.time + pursuitSeconds;
        repathAt = 0f;
        agent.speed = pursuitSpeed;
    }
    public bool CanArrest(PlayerController player, int incident) => suspect == player && proofId == incident &&
        Vector3.Distance(transform.position, Feet(player)) <= captureDistance && ClearSight(player);
    private void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;
        agent.isStopped = !PlayerController.GameplayActive;
        if (!PlayerController.GameplayActive) return;
        if (agent.isOnOffMeshLink)
        {
            if (!traversing) { traversing = true; traverseAt = Time.time + 0.35f; }
            if (Time.time >= traverseAt)
            {
                // Las escaleras del jugador ya usan teletransporte: el policía usa el mismo enlace.
                Vector3 destination = agent.currentOffMeshLinkData.endPos;
                agent.CompleteOffMeshLink();
                agent.Warp(destination);
                traversing = false;
            }
            return;
        }
        if (IsChasing)
        {
            if (suspect.IsDetained || Time.time >= chaseUntil)
            { StopPursuit(true); return; }
            Vector3 feet = Feet(suspect);
            if (Vector3.Distance(transform.position, feet) <= captureDistance && ClearSight(suspect))
            {
                bool caught = system.Arrest(suspect, proofId);
                StopPursuit(!caught);
                return;
            }
            if (Time.time >= repathAt)
            {
                repathAt = Time.time + 0.35f;
                if (system.Navigation.CanReach(transform.position, feet, out Vector3 goal)) agent.SetDestination(goal);
                else { agent.ResetPath(); } // No atraviesa paredes si el rival sale de una zona alcanzable.
            }
        }
        else if (Time.time >= patrolAt && (!agent.hasPath || agent.remainingDistance < 0.5f))
        {
            patrolAt = Time.time + 3f;
            Vector2 random = Random.insideUnitCircle * patrolRadius;
            if (system.Navigation.CanReach(transform.position, home + new Vector3(random.x, 0f, random.y), out Vector3 goal))
                agent.SetDestination(goal);
        }
    }
    private bool ClearSight(PlayerController target)
    {
        Vector3 from = transform.position + Vector3.up * 0.7f;
        Vector3 to = Feet(target) + Vector3.up * 0.7f;
        foreach (var hit in Physics.RaycastAll(from, (to - from).normalized, Vector3.Distance(from, to),
                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (!hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(target.transform)) return false;
        return true;
    }
    private void StopPursuit(bool release)
    {
        if (release)
        {
            system.Ledger.Release(proofId);
            GameHUD.Notify("El sospechoso escapó. Conservas la prueba para otra denuncia; no hay penalización.", 4f);
        }
        suspect = null;
        proofId = 0;
        agent.speed = patrolSpeed;
        agent.ResetPath();
    }
    public void ResetForRound()
    {
        suspect = null;
        proofId = 0;
        chaseUntil = repathAt = patrolAt = traverseAt = 0f;
        traversing = false;
        if (agent != null && agent.isOnNavMesh)
        { agent.ResetPath(); agent.Warp(home); agent.speed = patrolSpeed; }
    }
}
