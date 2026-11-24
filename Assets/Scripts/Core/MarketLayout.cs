using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// El mismo plano transitable alimenta el minimapa, los peatones y los coleccionables.
// Descarta azoteas y superficies aisladas que Tomás no puede alcanzar.
public sealed class MarketLayout
{
    public Vector3[] Vertices { get; private set; }
    public List<int> Indices { get; } = new List<int>();
    public Bounds Bounds { get; private set; }
    private readonly List<float> areas = new List<float>();
    private readonly NavMeshPath path = new NavMeshPath();
    private MarketNavigation navigation;
    private NavMeshQueryFilter filter;
    private int[] parents;

    public bool Build(MarketNavigation nav, Vector3 origin)
    {
        navigation = nav;
        filter = new NavMeshQueryFilter { agentTypeID = nav.AgentType, areaMask = NavMesh.AllAreas };
        if (!nav.Sample(origin, 2f, out origin)) return false;
        var mesh = NavMesh.CalculateTriangulation();
        Vertices = mesh.vertices;
        parents = new int[Vertices.Length];
        var welded = new Dictionary<Vector3Int, int>();
        for (int i = 0; i < parents.Length; i++)
        {
            parents[i] = i;
            var key = Vector3Int.RoundToInt(Vertices[i] * 1000f);
            if (welded.TryGetValue(key, out int previous)) Join(i, previous);
            else welded.Add(key, i);
        }
        for (int i = 0; i < mesh.indices.Length; i += 3)
        {
            Join(mesh.indices[i], mesh.indices[i + 1]);
            Join(mesh.indices[i], mesh.indices[i + 2]);
        }
        var connected = new Dictionary<int, bool>();
        Bounds bounds = new Bounds(origin, Vector3.zero);
        float total = 0f;
        Indices.Clear(); areas.Clear();
        for (int i = 0; i < mesh.indices.Length; i += 3)
        {
            int a = mesh.indices[i], b = mesh.indices[i + 1], c = mesh.indices[i + 2];
            int root = Root(a);
            if (!connected.TryGetValue(root, out bool reachable))
            {
                Vector3 middle = (Vertices[a] + Vertices[b] + Vertices[c]) / 3f;
                reachable = NavMesh.CalculatePath(origin, middle, filter, path) && path.status == NavMeshPathStatus.PathComplete;
                connected.Add(root, reachable);
            }
            if (!reachable) continue;
            float area = Mathf.Abs(Vector3.Cross(Vertices[b] - Vertices[a], Vertices[c] - Vertices[a]).y) * .5f;
            if (area < .005f) continue;
            Indices.Add(a); Indices.Add(b); Indices.Add(c);
            total += area; areas.Add(total);
            bounds.Encapsulate(Vertices[a]); bounds.Encapsulate(Vertices[b]); bounds.Encapsulate(Vertices[c]);
        }
        bounds.Expand(3f); Bounds = bounds;
        return Indices.Count > 0;
    }

    private int Root(int i)
    {
        while (parents[i] != i) { parents[i] = parents[parents[i]]; i = parents[i]; }
        return i;
    }
    private void Join(int a, int b) { parents[Root(a)] = Root(b); }

    public bool TryRandomPoint(out Vector3 point, float clearance = .3f)
    {
        point = default;
        if (areas.Count == 0) return false;
        for (int attempt = 0; attempt < 40; attempt++)
        {
            int index = areas.BinarySearch(Random.value * areas[areas.Count - 1]);
            if (index < 0) index = ~index;
            index = Mathf.Min(index, areas.Count - 1) * 3;
            float u = Mathf.Sqrt(Random.value), v = Random.value;
            Vector3 candidate = Vertices[Indices[index]] * (1f - u) +
                Vertices[Indices[index + 1]] * (u * (1f - v)) + Vertices[Indices[index + 2]] * (u * v);
            if (!navigation.Sample(candidate, .15f, out point)) continue;
            if (!NavMesh.FindClosestEdge(point, out NavMeshHit edge, filter) || edge.distance < clearance) continue;
            if (Physics.CheckCapsule(point + Vector3.up * .5f, point + Vector3.up * 1.45f, .32f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            return true;
        }
        return false;
    }
}
