using UnityEngine;

public class MarketPowerup : MonoBehaviour
{
    public enum Kind { CocaLeaf, Cascadita }
    public Kind Type { get; private set; }
    public bool Available { get; private set; }
    public float Remaining => Mathf.Max(0f, MarketLife.PickupLifetime - age);
    private MarketLife market;
    private float age, phase;
    private Transform visual;
    private Material material;
    private Material detailMaterial;
    private Mesh mesh;

    public void Initialize(MarketLife owner, Kind kind)
    {
        market = owner; Type = kind; Available = true; phase = Random.value * 6f;
        visual = new GameObject("Símbolo flotante").transform;
        visual.SetParent(transform, false);
        material = MarketDrawing.Material(kind == Kind.CocaLeaf ? new Color(.22f, .8f, .28f) : new Color(1f, .65f, .08f));
        detailMaterial = MarketDrawing.Material(kind == Kind.CocaLeaf ? new Color(.8f, 1f, .42f) : new Color(.9f, .16f, .08f));
        GameObject shape;
        if (kind == Kind.CocaLeaf)
        {
            shape = new GameObject("Hoja y nervadura"); shape.transform.SetParent(visual, false);
            mesh = new Mesh { name = "Hoja de coca" };
            Vector3[] outline = {
                new Vector3(0,-.6f,0), new Vector3(-.19f,-.35f,0), new Vector3(-.3f,0,0),
                new Vector3(-.21f,.32f,0), new Vector3(0,.65f,0), new Vector3(.21f,.32f,0),
                new Vector3(.3f,0,0), new Vector3(.19f,-.35f,0), Vector3.zero };
            var triangles = new int[48];
            for (int i = 0; i < 8; i++)
            {
                int n = (i + 1) % 8, t = i * 6;
                triangles[t] = 8; triangles[t+1] = i; triangles[t+2] = n;
                triangles[t+3] = 8; triangles[t+4] = n; triangles[t+5] = i;
            }
            mesh.vertices = outline; mesh.triangles = triangles; mesh.RecalculateNormals();
            shape.AddComponent<MeshFilter>().sharedMesh = mesh;
            shape.AddComponent<MeshRenderer>().sharedMaterial = material;
            var vein = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vein.transform.SetParent(visual, false); vein.transform.localScale = new Vector3(.025f, 1f, .045f);
            vein.GetComponent<Collider>().enabled = false; Destroy(vein.GetComponent<Collider>());
            vein.GetComponent<Renderer>().sharedMaterial = detailMaterial;
            visual.localRotation = Quaternion.Euler(12, 0, -25);
        }
        else
        {
            shape = GameObject.CreatePrimitive(PrimitiveType.Cube); shape.name = "Sachet de refresco";
            shape.transform.SetParent(visual, false); shape.transform.localScale = new Vector3(.7f, 1f, .16f);
            shape.GetComponent<Collider>().enabled = false; Destroy(shape.GetComponent<Collider>());
            shape.GetComponent<Renderer>().sharedMaterial = material;
            foreach (float y in new[] { -.45f, .45f })
            {
                var seal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seal.name = "Sello del sachet"; seal.transform.SetParent(visual, false);
                seal.transform.localPosition = Vector3.up * y;
                seal.transform.localScale = new Vector3(.73f, .075f, .18f);
                seal.GetComponent<Collider>().enabled = false; Destroy(seal.GetComponent<Collider>());
                seal.GetComponent<Renderer>().sharedMaterial = detailMaterial;
            }
            for (int side = -1; side <= 1; side += 2)
            {
                var label = new GameObject("Cascadita"); label.transform.SetParent(visual, false);
                label.transform.localPosition = new Vector3(0, 0, side * .09f);
                label.transform.localRotation = Quaternion.Euler(0, side == -1 ? 0 : 180, 0);
                var text = label.AddComponent<TextMesh>();
                text.text = "CASCADITA\nREFRESCO"; text.fontSize = 48; text.characterSize = .032f;
                text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
                text.color = new Color(.18f, .24f, .12f); text.fontStyle = FontStyle.Bold;
            }
        }
        var trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true; trigger.center = Vector3.up * .8f; trigger.radius = .7f;
    }
    private void Update()
    {
        if (!Available || !PlayerController.GameplayActive) return;
        age += Time.deltaTime;
        if (age >= MarketLife.PickupLifetime) { Consume(); return; }
        visual.localPosition = Vector3.up * (.95f + Mathf.Sin(age * 4f + phase) * .12f);
        visual.Rotate(0, 70f * Time.deltaTime, 0, Space.World);
        // Parpadea al agotarse los diez segundos de permanencia.
        visual.gameObject.SetActive(Remaining > 1.5f || Mathf.Sin(age * 18f) > 0f);
        market.TryCollect(this, market.Worker);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (market != null) market.TryCollect(this, other.GetComponentInParent<PlayerController>());
    }
    public void Consume()
    {
        if (!Available) return;
        Available = false; gameObject.SetActive(false); Destroy(gameObject);
    }
    private void OnDestroy()
    {
        if (material != null) Destroy(material);
        if (detailMaterial != null) Destroy(detailMaterial);
        if (mesh != null) Destroy(mesh);
    }
}
