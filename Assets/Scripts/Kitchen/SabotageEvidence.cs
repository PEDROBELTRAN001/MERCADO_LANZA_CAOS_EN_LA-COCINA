using UnityEngine;

// El objeto del suelo es temporal; la prueba examinada permanece en el registro del turno.
public class SabotageEvidence : Interactable
{
    public PlayerController attacker { get; private set; }
    public PlayerController victim { get; private set; }
    public string lostItemName { get; private set; }
    public float occurredAt { get; private set; }
    public int IncidentId { get; private set; }
    public bool UsedForAccusation => InvestigationSystem.Instance?.Ledger.Find(IncidentId)?.Used ?? false;
    public override void Interact(PlayerController player)
    {
        if (!PlayerController.GameplayActive || player == null || player.IsDetained || player.IsStunned ||
            player.role != PlayerController.PlayerRole.Worker || !player.CanInspect(this) ||
            Vector3.Distance(player.transform.position, transform.position + Vector3.up * 0.7f) > 2f) return;
        var system = InvestigationSystem.Instance;
        if (system == null || IncidentId == 0) return;
        if (system.Ledger.Examine(IncidentId, player.InvestigationId))
            GameHUD.Notify("Examinaste los restos de " + lostItemName + ". Hay huellas del forcejeo: presenta la prueba al Capibara con E.", 5f);
        else GameHUD.Notify(UsedForAccusation ? "Este incidente ya fue resuelto." : "Ya examinaste este rastro. Busca al Capibara.", 3f);
    }
    public static void Create(PlayerController culprit, PlayerController target, string itemName, Vector3 position)
    {
        var root = new GameObject("Rastro de comida caída");
        var evidence = root.AddComponent<SabotageEvidence>();
        evidence.displayName = "Restos de comida · examinar rastro";
        evidence.interactionPrompt = "Examinar rastro";
        evidence.attacker = culprit;
        evidence.victim = target;
        evidence.lostItemName = itemName;
        evidence.occurredAt = Time.time;
        if (InvestigationSystem.Instance != null && culprit != null && target != null)
            evidence.IncidentId = InvestigationSystem.Instance.Ledger.Register(culprit.InvestigationId, target.InvestigationId, itemName).Id;
        Vector3 floor = position - Vector3.up * 0.75f;
        float bestDistance = float.PositiveInfinity;
        foreach (var hit in Physics.RaycastAll(position + Vector3.up, Vector3.down, 5f,
                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (hit.collider.GetComponentInParent<PlayerController>() == null && hit.normal.y > 0.7f && hit.distance < bestDistance)
            { floor = hit.point; bestDistance = hit.distance; }
        root.transform.position = floor + Vector3.up * 0.025f;
        var trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.45f;
        trigger.center = Vector3.up * 0.2f;
        for (int i = 0; i < 4; i++)
        {
            GameObject crumb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crumb.name = "Restos de comida";
            crumb.GetComponent<Collider>().enabled = false;
            Destroy(crumb.GetComponent<Collider>());
            crumb.transform.SetParent(root.transform, false);
            crumb.transform.localPosition = new Vector3((i % 2) * 0.15f, 0f, (i / 2) * 0.13f);
            crumb.transform.localScale = new Vector3(0.13f, 0.045f, 0.11f);
            crumb.GetComponent<Renderer>().material.color = new Color(0.6f, 0.28f, 0.12f);
        }
        Destroy(root, 30f);
    }
}
