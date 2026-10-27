using UnityEngine;

// Icono del ingrediente flotando sobre su caja de insumos (el mismo de las tarjetas de pedido),
// siempre mirando a la camara. Se agrega solo a cada IngredientCrate al cargar la escena.
public class CrateIcon : MonoBehaviour
{
    [Tooltip("Tamaño del icono en metros")]
    public float size = 0.6f;
    [Tooltip("Cuanto crece el icono de la caja a la que apunta el jugador")]
    public float targetScale = 1.7f;
    [Tooltip("Altura del icono sobre la caja")]
    public float height = 0.85f;

    private Transform quad;
    private Material material;
    private float bob, grow;
    private Vector3 basePosition;
    private IngredientCrate crate;
    private static PlayerController player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Attach()
    {
        foreach (IngredientCrate crate in FindObjectsByType<IngredientCrate>())
            if (crate.GetComponent<CrateIcon>() == null) crate.gameObject.AddComponent<CrateIcon>();
    }

    private void Start()
    {
        crate = GetComponent<IngredientCrate>();
        Texture2D icon = crate != null ? GameIcons.Ingredient(crate.ingredientType) : null;
        if (icon == null) { enabled = false; return; }

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        material = new Material(unlit != null ? unlit : Shader.Find("Sprites/Default"));
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", icon);
        material.mainTexture = icon;
        GameIcons.MakeTransparent(material);

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(go.GetComponent<Collider>());
        go.name = "Icono";
        go.GetComponent<Renderer>().sharedMaterial = material;
        go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        quad = go.transform;
        quad.SetParent(transform, true);
        quad.localScale = Vector3.one * size;

        // Encima de la caja visible (en algunas cajas la raiz no esta a la altura de la caja)
        Transform box = transform.Find("Caja");
        Renderer boxRenderer = box != null ? box.GetComponent<Renderer>() : GetComponentInChildren<Renderer>();
        Vector3 top = boxRenderer != null ? new Vector3(boxRenderer.bounds.center.x, boxRenderer.bounds.max.y, boxRenderer.bounds.center.z) : transform.position;
        quad.position = top + Vector3.up * height;
        basePosition = quad.position;
        bob = Random.Range(0f, Mathf.PI * 2f);
    }

    private void LateUpdate()
    {
        if (quad == null || Camera.main == null) return;
        // La caja a la que apunta el jugador: su icono crece y sube, para saber que se va a sacar
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        bool targeted = player != null && player.CurrentInteractable == crate;
        grow = Mathf.MoveTowards(grow, targeted ? 1f : 0f, Time.deltaTime * 6f);

        // Mira a la camara y late un poquito para llamar la atencion
        quad.rotation = Camera.main.transform.rotation;
        float scale = Mathf.Lerp(1f, targetScale, grow) * (1f + Mathf.Sin(Time.time * 2f + bob) * 0.04f);
        quad.localScale = Vector3.one * size * scale;
        quad.position = basePosition + Vector3.up * (grow * size * 0.6f);
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }
}
