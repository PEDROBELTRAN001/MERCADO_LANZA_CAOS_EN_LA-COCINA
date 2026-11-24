using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Mercado > Poner mural de Doña Elvira en la pared seleccionada:
// pega el cartel luminoso "Sandwich de Chorizo - Doña Elvira" (foto de Shirley, ya enderezada en
// Assets/Textures/MercadoLanza/Mural_DonaElvira.jpg) sobre la pared que este seleccionada en la Jerarquia.
// - Elige la cara que mira hacia la vista Scene (la que estas viendo) y ajusta el tamaño al ancho y alto de la pared.
// - Si ya hay un mural puesto, lo borra y lo pasa a la cara de enfrente: se puede repetir hasta que quede del lado bueno.
// - No toca la pared: el mural es un objeto aparte (Mural_DonaElvira), se puede mover y escalar a mano.
// Despues guardar la escena (Ctrl+S).
public static class MuralTool
{
    const string MuralName = "Mural_DonaElvira";
    const string TexturePath = "Assets/Textures/MercadoLanza/Mural_DonaElvira.jpg";
    const string MaterialFolder = "Assets/Materials/MercadoLanza";

    [MenuItem("Mercado/Poner mural de Doña Elvira en la pared seleccionada")]
    static void Place()
    {
        GameObject wall = Selection.activeGameObject;
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        if (wall == null || texture == null)
        {
            EditorUtility.DisplayDialog("Mural", wall == null
                ? "Primero selecciona la pared en la Jerarquía (un clic sobre su nombre) y vuelve a usar este menú."
                : "No encontré la imagen " + TexturePath + ".", "OK");
            return;
        }

        Renderer renderer = wall.GetComponent<Renderer>();
        if (renderer == null) renderer = wall.GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            EditorUtility.DisplayDialog("Mural", "La pared seleccionada no tiene malla visible (Mesh Renderer).", "OK");
            return;
        }

        // Medidas en los ejes del objeto: el eje mas delgado es el grosor de la pared
        Transform t = renderer.transform;
        Bounds local = renderer.localBounds;
        Vector3 scale = t.lossyScale;
        float[] size = { Mathf.Abs(local.size.x * scale.x), Mathf.Abs(local.size.y * scale.y), Mathf.Abs(local.size.z * scale.z) };
        Vector3[] axes = { t.right, t.up, t.forward };
        int thick = 0;
        for (int i = 1; i < 3; i++) if (size[i] < size[thick]) thick = i;
        int height = -1;
        for (int i = 0; i < 3; i++)
        {
            if (i == thick) continue;
            if (height < 0 || Mathf.Abs(Vector3.Dot(axes[i], Vector3.up)) > Mathf.Abs(Vector3.Dot(axes[height], Vector3.up))) height = i;
        }
        int width = 3 - thick - height;
        Vector3 center = t.TransformPoint(local.center);

        // Cara: la que mira a la vista Scene; si ya habia un mural, la contraria
        Vector3 normal = axes[thick].normalized;
        GameObject old = GameObject.Find(MuralName);
        Vector3 viewer = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.camera.transform.position : center + Vector3.back * 10f;
        if (old != null)
        {
            if (Vector3.Dot(old.transform.position - center, normal) > 0f) normal = -normal;
            Undo.DestroyObjectImmediate(old);
        }
        else if (Vector3.Dot(viewer - center, normal) < 0f) normal = -normal;

        // Tamaño: entra en la pared con un pequeño margen y conserva la proporcion de la foto
        float aspect = (float)texture.width / texture.height;
        float w = Mathf.Min(size[width] * 0.94f, size[height] * 0.94f * aspect), h = w / aspect;

        var mural = new GameObject(MuralName);
        Undo.RegisterCreatedObjectUndo(mural, "Mural Dona Elvira");
        mural.transform.SetPositionAndRotation(center + normal * (size[thick] / 2f + 0.03f), Quaternion.LookRotation(-normal, Vector3.up));

        // Marco oscuro detras y la foto delante (el quad se ve por su lado -z)
        Quad(mural.transform, "Marco", new Vector3(0f, 0f, 0.012f), new Vector3(w + 0.25f, h + 0.25f, 1f), Solid("MarcoCartel", new Color(0.12f, 0.12f, 0.14f)));
        Quad(mural.transform, "Foto", Vector3.zero, new Vector3(w, h, 1f), Photo(texture));

        EditorSceneManager.MarkSceneDirty(wall.scene);
        Selection.activeGameObject = mural;
        EditorUtility.DisplayDialog("Mural", "Listo: mural de Doña Elvira en «" + wall.name + "».\n" +
            "Si quedó del lado equivocado, vuelve a usar este menú con la pared seleccionada y pasa a la otra cara.\n" +
            "Guarda la escena (Ctrl+S) para conservarlo.", "OK");
    }

    static void Quad(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = localPosition;
        quad.transform.localScale = localScale;
        quad.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(quad.GetComponent<Collider>());
    }

    // Material sin luz con la foto: se lee igual de dia que en las zonas oscuras
    static Material Photo(Texture2D texture)
    {
        EnsureFolder();
        string path = MaterialFolder + "/Foto_Mural_DonaElvira.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            mat = new Material(unlit != null ? unlit : Shader.Find("Unlit/Texture"));
            AssetDatabase.CreateAsset(mat, path);
        }
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
        mat.mainTexture = texture;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material Solid(string name, Color color)
    {
        EnsureFolder();
        string path = MaterialFolder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Materials", "MercadoLanza");
    }
}
