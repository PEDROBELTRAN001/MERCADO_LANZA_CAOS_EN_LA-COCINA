using UnityEngine;

// Flecha sobre el piso: señala el rumbo del personaje aunque la cámara esté girando.
public class PlayerDirectionIndicator : MonoBehaviour
{
    private GameObject arrow;
    private Material material;
    private Mesh mesh;
    private PlayerController player;
    private Collider body;
    private readonly RaycastHit[] floorHits = new RaycastHit[16];
    private void Start()
    {
        player = GetComponent<PlayerController>();
        body = GetComponent<Collider>();
        arrow = new GameObject("Dirección de Tomás"); arrow.transform.SetParent(transform, false);
        mesh = new Mesh { name = "Flecha de marcha" };
        mesh.vertices = new[] { new Vector3(0,0,.9f), new Vector3(-.38f,0,.2f),
            new Vector3(0,0,.4f), new Vector3(.38f,0,.2f) };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        arrow.AddComponent<MeshFilter>().sharedMesh = mesh;
        material = MarketDrawing.Material(new Color(.25f, 1f, .87f));
        arrow.AddComponent<MeshRenderer>().sharedMaterial = material;
    }
    private void LateUpdate()
    {
        if (arrow == null || player == null) return;
        arrow.SetActive(GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing);
        if (body == null) return;
        Vector3 ahead = transform.position + transform.forward * 1.9f;
        float floor = body.bounds.min.y;
        int count = Physics.RaycastNonAlloc(ahead + Vector3.up * 2f, Vector3.down, floorHits, 6f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            var hit = floorHits[i];
            if (!hit.transform.IsChildOf(transform) && hit.normal.y > .65f && hit.distance < nearest)
            { floor = hit.point.y; nearest = hit.distance; }
        }
        arrow.transform.SetPositionAndRotation(new Vector3(ahead.x, floor + .10f, ahead.z),
            Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
    }
    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
    }
}
