using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Construye rutas a partir de los colliders existentes, sin mover el escenario.
public class MarketNavigation : MonoBehaviour
{
    private NavMeshData data;
    private NavMeshDataInstance instance;
    private readonly List<NavMeshLinkInstance> stairLinks = new List<NavMeshLinkInstance>();
    public bool Ready { get; private set; }
    public int AgentType { get; private set; }
    public bool Build()
    {
        if (Ready) return true;
        if (NavMesh.GetSettingsCount() == 0) return false;
        NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(0);
        AgentType = settings.agentTypeID;
        settings.agentRadius = 0.4f;
        settings.agentHeight = 1.6f;
        settings.agentClimb = 0.3f;
        var sources = new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources((Transform)null, Physics.DefaultRaycastLayers,
            NavMeshCollectGeometry.PhysicsColliders, 0, false, new List<NavMeshBuildMarkup>(), false, sources);
        sources.RemoveAll(s => s.component != null &&
            (s.component.GetComponentInParent<PlayerController>() != null ||
             s.component.GetComponentInParent<Ingredient>() != null ||
             s.component.GetComponentInParent<Plate>() != null ||
             s.component.GetComponentInParent<DishItem>() != null ||
             s.component.GetComponentInParent<SabotageEvidence>() != null ||
             s.component.GetComponentInParent<CapibaraPolice>() != null));
        if (sources.Count == 0) return false;
        Bounds bounds = new Bounds();
        bool initialized = false;
        foreach (var source in sources)
        {
            Collider collider = source.component as Collider;
            if (collider == null) continue;
            if (!initialized) { bounds = collider.bounds; initialized = true; }
            else bounds.Encapsulate(collider.bounds);
        }
        if (!initialized) return false;
        bounds.Expand(4f);
        data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
        if (data == null) return false;
        instance = NavMesh.AddNavMeshData(data);
        Ready = instance.valid;
        if (Ready) AddStairLinks();
        return Ready;
    }
    private void AddStairLinks()
    {
        foreach (StairTrigger stair in FindObjectsByType<StairTrigger>())
        {
            Collider trigger = stair.GetComponent<Collider>();
            Vector3 entrance = trigger != null ? trigger.bounds.center : stair.transform.position;
            if (!Sample(entrance, 4f, out Vector3 start) || !Sample(stair.teleportDestination, 4f, out Vector3 end)) continue;
            var link = new NavMeshLinkData { startPosition = start, endPosition = end,
                width = 0.6f, bidirectional = false, agentTypeID = AgentType, area = 0, costModifier = 2f };
            stairLinks.Add(NavMesh.AddLink(link));
        }
    }
    public bool Sample(Vector3 point, float radius, out Vector3 result)
    {
        var filter = new NavMeshQueryFilter { agentTypeID = AgentType, areaMask = NavMesh.AllAreas };
        bool found = NavMesh.SamplePosition(point, out NavMeshHit hit, radius, filter);
        result = found ? hit.position : point;
        return found;
    }
    public bool CanReach(Vector3 start, Vector3 destination, out Vector3 goal)
    {
        goal = destination;
        if (!Ready || !Sample(start, 1.5f, out Vector3 from) || !Sample(destination, 1.5f, out goal)) return false;
        var filter = new NavMeshQueryFilter { agentTypeID = AgentType, areaMask = NavMesh.AllAreas };
        var path = new NavMeshPath();
        return NavMesh.CalculatePath(from, goal, filter, path) && path.status == NavMeshPathStatus.PathComplete;
    }
    private void OnDestroy()
    {
        foreach (var link in stairLinks) if (NavMesh.IsLinkValid(link)) NavMesh.RemoveLink(link);
        if (instance.valid) instance.Remove();
        if (data != null) Destroy(data);
    }
}
