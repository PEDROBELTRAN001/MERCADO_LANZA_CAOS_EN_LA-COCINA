using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Mercado > Organizar cajas de insumos de la cocina:
// deja en la cocina de Doña Rosa una caja por cada ingrediente de las 4 recetas, ordenadas por plato
// para que sea facil encontrarlas, y las apoya en el piso (con el collider desde el piso hasta 1 m).
// - Fila del estante (z = 4.2): Plato Paceño | Api con Pastel | Sandwich de Chola
// - En "L" con la fila (x = -13.35, contra el mostrador oeste): las frutas de la ensalada
// Las cajas que faltan se crean copiando una caja de la cocina. Las de los puestos del mercado no se tocan.
// Despues hay que guardar la escena.
public static class KitchenCratesTool
{
    struct Spec
    {
        public Ingredient.IngredientType type;
        public string name;
        public bool femenino;
        public Color color;
        public PrimitiveType shape;
        public Vector2 slot; // x, z en el mundo
    }

    static readonly Spec[] Specs =
    {
        // Plato Paceño
        S(Ingredient.IngredientType.Potato, "Papa", true, new Color(0.76f, 0.60f, 0.42f), PrimitiveType.Sphere, -13.0f, 4.2f),
        S(Ingredient.IngredientType.Corn, "Choclo", false, new Color(0.98f, 0.84f, 0.42f), PrimitiveType.Capsule, -12.4f, 4.2f),
        S(Ingredient.IngredientType.Bean, "Haba", true, new Color(0.42f, 0.62f, 0.20f), PrimitiveType.Sphere, -11.8f, 4.2f),
        S(Ingredient.IngredientType.Cheese, "Queso", false, new Color(1f, 0.95f, 0.70f), PrimitiveType.Cube, -11.2f, 4.2f),
        // Api con Pastel
        S(Ingredient.IngredientType.PurpleCorn, "Maíz morado", false, new Color(0.40f, 0.15f, 0.45f), PrimitiveType.Capsule, -10.4f, 4.2f),
        S(Ingredient.IngredientType.Dough, "Masa", true, new Color(0.95f, 0.90f, 0.80f), PrimitiveType.Sphere, -9.8f, 4.2f),
        // Sandwich de Chola
        S(Ingredient.IngredientType.Bread, "Pan", false, new Color(0.90f, 0.70f, 0.40f), PrimitiveType.Sphere, -9.1f, 4.2f),
        S(Ingredient.IngredientType.Meat, "Cerdo", false, new Color(0.75f, 0.40f, 0.35f), PrimitiveType.Cube, -8.5f, 4.2f),
        // Ensalada de Frutas: columna pegada a la papa, contra el mostrador oeste (arriba del lavaplatos),
        // asi no tapa el fogon, el lavaplatos ni la mesa de armado
        S(Ingredient.IngredientType.Banana, "Plátano", false, SceneBuilder.FruitColors[0], PrimitiveType.Capsule, -13.35f, 3.5f),
        S(Ingredient.IngredientType.Papaya, "Papaya", true, SceneBuilder.FruitColors[1], PrimitiveType.Sphere, -13.35f, 2.9f),
        S(Ingredient.IngredientType.Strawberry, "Frutilla", true, SceneBuilder.FruitColors[2], PrimitiveType.Sphere, -13.35f, 2.3f),
    };

    static Spec S(Ingredient.IngredientType type, string name, bool femenino, Color color, PrimitiveType shape, float x, float z) =>
        new Spec { type = type, name = name, femenino = femenino, color = color, shape = shape, slot = new Vector2(x, z) };

    // Zona de la cocina (las cajas fuera de aqui son de los puestos del mercado)
    static bool InKitchen(Vector3 p) => p.x > -17f && p.x < 0f && p.z > -8f && p.z < 10f;

    [MenuItem("Mercado/Organizar cajas de insumos de la cocina")]
    static void Organize()
    {
        var kitchen = new List<IngredientCrate>();
        foreach (IngredientCrate crate in Object.FindObjectsByType<IngredientCrate>())
            if (InKitchen(crate.transform.position)) kitchen.Add(crate);
        if (kitchen.Count == 0)
        {
            EditorUtility.DisplayDialog("Cajas de insumos", "No encontré cajas de insumos en la cocina para usar de modelo.", "OK");
            return;
        }

        IngredientCrate template = kitchen.Find(c => c.ingredientType == Ingredient.IngredientType.Bread) ?? kitchen[0];
        Physics.SyncTransforms();
        int created = 0;

        foreach (Spec spec in Specs)
        {
            // La caja "Fruta" generica pasa a ser la de platano
            IngredientCrate crate = kitchen.Find(c => c.ingredientType == spec.type);
            bool configure = false;
            if (crate == null && spec.type == Ingredient.IngredientType.Banana)
            {
                crate = kitchen.Find(c => c.ingredientType == Ingredient.IngredientType.Fruit);
                configure = crate != null;
            }
            if (crate == null)
            {
                GameObject copy = Object.Instantiate(template.gameObject, template.transform.parent);
                Undo.RegisterCreatedObjectUndo(copy, "Organizar cajas");
                crate = copy.GetComponent<IngredientCrate>();
                kitchen.Add(crate);
                configure = true;
                created++;
            }

            Undo.RecordObject(crate.transform, "Organizar cajas");
            if (configure) Setup(crate, spec);
            Place(crate, spec.slot);
        }

        EditorSceneManager.MarkSceneDirty(template.gameObject.scene);
        EditorUtility.DisplayDialog("Cajas de insumos",
            "Listo: " + Specs.Length + " cajas en la cocina (" + created + " nuevas), ordenadas por receta.\n" +
            "Guarda la escena (Ctrl+S) para conservarlas.", "OK");
    }

    // Lleva la caja a su lugar y la apoya en el piso; el collider va desde el piso hasta 1 m
    static void Place(IngredientCrate crate, Vector2 slot)
    {
        Transform t = crate.transform;
        t.position = new Vector3(slot.x, t.position.y, slot.y);

        Transform box = t.Find("Caja");
        Renderer boxRenderer = box != null ? box.GetComponent<Renderer>() : null;
        if (boxRenderer == null) return;

        Bounds b = boxRenderer.bounds;
        if (FloorBelow(new Vector3(b.center.x, b.max.y + 3f, b.center.z), out float floor))
            t.position += Vector3.up * (floor - b.min.y);

        if (crate.TryGetComponent(out BoxCollider col))
        {
            Undo.RecordObject(col, "Organizar cajas");
            float bottom = t.InverseTransformPoint(boxRenderer.bounds.min).y;
            col.center = new Vector3(col.center.x, bottom + 0.5f, col.center.z);
            col.size = new Vector3(col.size.x, 1f, col.size.z);
        }
    }

    // Lo mas alto debajo del punto que no sea otra caja de insumos ni el jugador
    static bool FloorBelow(Vector3 from, out float y)
    {
        y = 0f;
        bool found = false;
        foreach (RaycastHit hit in Physics.RaycastAll(from, Vector3.down, 50f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<IngredientCrate>() != null) continue;
            if (hit.collider.GetComponentInParent<PlayerController>() != null) continue;
            if (!found || hit.point.y > y) { y = hit.point.y; found = true; }
        }
        return found;
    }

    static void Setup(IngredientCrate crate, Spec spec)
    {
        Undo.RecordObject(crate, "Organizar cajas");
        Undo.RecordObject(crate.gameObject, "Organizar cajas");
        crate.gameObject.name = "Insumo_" + spec.name.Replace(" ", "_");
        crate.ingredientName = spec.name;
        crate.femenino = spec.femenino;
        crate.ingredientType = spec.type;
        crate.color = spec.color;
        crate.shape = spec.shape;
        crate.size = spec.shape == PrimitiveType.Capsule ? new Vector3(0.12f, 0.2f, 0.12f) : new Vector3(0.22f, 0.22f, 0.22f);
        crate.interactionPrompt = "E · Sacar " + spec.name.ToLower();
        crate.displayName = "Caja de " + spec.name.ToLower();
        EditorUtility.SetDirty(crate);

        Transform label = crate.transform.Find("Rotulo");
        if (label != null && label.TryGetComponent(out TextMesh text))
        {
            Undo.RecordObject(text, "Organizar cajas");
            text.text = spec.name.ToUpper().Replace(" ", "\n");
        }

        Transform sample = crate.transform.Find("Muestra");
        if (sample != null && sample.TryGetComponent(out MeshRenderer renderer) && sample.TryGetComponent(out MeshFilter filter))
        {
            Undo.RecordObject(filter, "Organizar cajas");
            Undo.RecordObject(renderer, "Organizar cajas");
            filter.sharedMesh = PrimitiveMesh(spec.shape);
            renderer.sharedMaterial = SampleMaterial(renderer.sharedMaterial, spec.type, spec.color);
        }
    }

    // Un material por ingrediente, guardado como asset para que la escena lo conserve
    static Material SampleMaterial(Material template, Ingredient.IngredientType type, Color color)
    {
        string path = "Assets/Materials/Insumo_" + type + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    static Mesh PrimitiveMesh(PrimitiveType shape)
    {
        GameObject temp = GameObject.CreatePrimitive(shape);
        Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(temp);
        return mesh;
    }
}
