using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Mercado > Reemplazar puestos tematicos:
// cambia puestos de Pedro (Puesteishon, .glb) por puestos completos modelados en Blender a partir de fotos del
// Mercado Lanza (Assets/Models/MercadoLanza). El puesto de Pedro deja de dibujarse pero su choque se queda;
// el nuevo se pone en su lugar, del mismo ancho y fondo, con el frente hacia el centro del mercado.
// - Peluches (Puesteishon 25 a 27, izquierda de la entrada sur): PuestoPeluches.fbx, fuente Downloads/PuestoPeluches.blend.
// - Flores (Puesteishon 28 a 31, derecha de la entrada sur): PuestoFlores.fbx, fuente Downloads/PuestoFlores.blend;
//   cada uno muestra solo su letrero (Letrero_1 Narciso, 2 Buganvillas, 3 La Ilusion, 4 Popelina).
// - Sector frutas (pared norte): fruterias en las esquinas (Puesteishon (3) Norfran y Puesteishon Frutas Rosario,
//   PuestoFrutas.fbx) y ensaladas de frutas en los kiosko clausurado (Jose 36 y Petra 35, PuestoEnsaladas.fbx).
// - Sector verduras (pared norte, a la derecha de las frutas): PuestoVerduras.fbx, fuente
//   Downloads/PuestoVerdurasCompleto.blend (Juan y Diana, N 84, de 1a Calidad, Dona Carmen).
// Todo queda en "Puestos_Tematicos"; si ya existe se vuelve a armar. Despues guardar (Ctrl+S).
public static class MarketStallsTool
{
    const string RootName = "Puestos_Tematicos";
    const string Folder = "Assets/Models/MercadoLanza/";
    // Medidas con las que se modelaron los puestos en Blender (ancho, alto, fondo)
    static readonly Vector3 Nominal = new Vector3(4.47f, 5.02f, 4.52f);
    // Centro aproximado del mercado (entre la cocina, las mesas y los triangulos de barandas)
    static readonly Vector3 MarketCenter = new Vector3(15f, 0f, 5f);

    static readonly (string stall, string model, string sign)[] Stalls =
    {
        ("Puesteishon (25)", "PuestoPeluches", null),
        ("Puesteishon (26)", "PuestoPeluches", null),
        ("Puesteishon (27)", "PuestoPeluches", null),
        ("Puesteishon (28)", "PuestoFlores", "Letrero_1"),
        ("Puesteishon (29)", "PuestoFlores", "Letrero_2"),
        ("Puesteishon (30)", "PuestoFlores", "Letrero_3"),
        ("Puesteishon (31)", "PuestoFlores", "Letrero_4"),
        ("Puesteishon (3)", "PuestoFrutas", "Letrero_1"),
        ("kiosko clausurado", "PuestoEnsaladas", "Letrero_1"),
        ("kiosko clausurado (1)", "PuestoEnsaladas", "Letrero_2"),
        ("Puesteishon", "PuestoFrutas", "Letrero_2"),
        (LooseStall, "PuestoVerduras", "Letrero_1"),
        ("Puesteishon (6)", "PuestoVerduras", "Letrero_2"),
        ("Puesteishon (5)", "PuestoVerduras", "Letrero_3"),
        ("Puesteishon (4)", "PuestoVerduras", "Letrero_4"),
    };

    // Puesto suelto (malla del .glb sin prefab) junto a las verduras; hay otro con el mismo nombre en (67, 44)
    // y los hijos de los Puesteishon se llaman igual, asi que se busca entre los objetos raiz el mas cercano
    const string LooseStall = "Mesh_Mesh_head_geo.001_lambert2SG.001";
    static readonly Vector3 LooseStallNear = new Vector3(27.7f, 0f, 58.2f);

    static GameObject FindStall(string name)
    {
        if (name != LooseStall) return GameObject.Find(name);
        GameObject best = null;
        float bestDistance = float.MaxValue;
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go.name != name) continue;
            Vector3 d = go.transform.position - LooseStallNear;
            d.y = 0f;
            if (d.sqrMagnitude < bestDistance) { bestDistance = d.sqrMagnitude; best = go; }
        }
        return best;
    }

    [MenuItem("Mercado/Reemplazar puestos temáticos")]
    static void Replace()
    {
        GameObject old = GameObject.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Puestos tematicos");

        int done = 0;
        string missing = "";
        for (int i = 0; i < Stalls.Length; i++)
        {
            GameObject target = FindStall(Stalls[i].stall);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + Stalls[i].model + ".fbx");
            // Todas las mallas del puesto (el kiosko clausurado tiene varias piezas)
            MeshFilter[] filters = target != null ? target.GetComponentsInChildren<MeshFilter>(true) : new MeshFilter[0];
            if (model == null || filters.Length == 0) { missing += "\n- " + Stalls[i].stall; continue; }
            Place(root.transform, target.transform, filters, model, Stalls[i].model, Stalls[i].sign);
            done++;
        }

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog("Puestos temáticos",
            "Listo: " + done + " puestos reemplazados." + (missing.Length > 0 ? "\nNo encontré:" + missing : "") +
            "\nGuarda la escena (Ctrl+S) para conservarlos.", "OK");
    }

    static void Place(Transform root, Transform stall, MeshFilter[] filters, GameObject model, string modelName, string sign)
    {
        // Los puestos vienen rotados (X = -90 por el .glb): se toma el eje del modelo que quedo horizontal
        Vector3 axis = Horizontal(stall.right);
        if (axis == Vector3.zero) axis = Horizontal(stall.up);
        if (axis == Vector3.zero) axis = Horizontal(stall.forward);
        Vector3 other = Vector3.Cross(Vector3.up, axis);

        Measure(filters, axis, other, out Vector3 min, out Vector3 max);
        Vector3 center = axis * (min.x + max.x) / 2f + other * (min.z + max.z) / 2f + Vector3.up * min.y;

        // Frente: de los 4 lados del puesto, el que mira hacia el centro del mercado
        // (los rayos no sirven: los costados del .glb tienen ventanas grandes)
        Vector3 inward = MarketCenter - center;
        inward.y = 0f;
        Vector3 front = axis;
        foreach (Vector3 dir in new[] { axis, -axis, other, -other })
            if (Vector3.Dot(dir, inward) > Vector3.Dot(front, inward)) front = dir;
        Vector3 right = Vector3.Cross(Vector3.up, front);
        Measure(filters, right, front, out min, out max);
        Vector3 size = max - min;

        // El de Pedro deja de dibujarse; su choque sigue
        foreach (Renderer r in stall.GetComponentsInChildren<Renderer>(true))
        {
            Undo.RecordObject(r, "Puestos tematicos");
            r.enabled = false;
        }

        var holder = new GameObject(modelName + " - " + stall.name);
        holder.transform.SetParent(root, false);
        holder.transform.SetPositionAndRotation(center, Quaternion.LookRotation(front, Vector3.up));

        var deco = (GameObject)PrefabUtility.InstantiatePrefab(model, holder.transform);
        deco.transform.localPosition = Vector3.zero;
        deco.transform.localRotation = Quaternion.identity;

        // El FBX puede entrar con otra unidad (cm en vez de m): el marcador mide 5 cm en Blender
        Transform marker = deco.transform.Find("MarcadorFrente");
        Renderer markerRenderer = marker != null ? marker.GetComponent<Renderer>() : null;
        if (markerRenderer != null && markerRenderer.bounds.size.y > 1e-6f)
            deco.transform.localScale *= 0.05f / markerRenderer.bounds.size.y;

        // Solo el letrero de este puesto
        foreach (Transform child in deco.transform)
            if (child.name.StartsWith("Letrero_")) child.gameObject.SetActive(child.name == sign);

        // Mismo ancho y fondo que el de Pedro; el alto sigue a esos dos para no aplastar los peluches
        float sx = size.x / Nominal.x, sz = size.z / Nominal.z;
        holder.transform.localScale = new Vector3(sx, (sx + sz) / 2f, sz);

        // El marcador esta dibujado delante: si quedo atras (ejes de Blender), se gira
        if (marker != null)
        {
            Vector3 point = markerRenderer != null ? markerRenderer.bounds.center : marker.position;
            if (Vector3.Dot(point - center, front) < 0f) deco.transform.Rotate(0f, 180f, 0f, Space.World);
            marker.gameObject.SetActive(false);
        }

        // Apoyado en el piso del puesto
        Transform body = deco.transform.Find("Caseta");
        Renderer bodyRenderer = body != null ? body.GetComponent<Renderer>() : null;
        if (bodyRenderer != null) deco.transform.position += Vector3.up * (center.y - bodyRenderer.bounds.min.y);
    }

    static Vector3 Horizontal(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.5f ? v.normalized : Vector3.zero;
    }

    // Extremos del modelo medidos en (a, altura, b)
    static void Measure(MeshFilter[] filters, Vector3 a, Vector3 b, out Vector3 min, out Vector3 max)
    {
        min = Vector3.one * float.MaxValue;
        max = Vector3.one * float.MinValue;
        foreach (MeshFilter filter in filters)
        {
            if (filter.sharedMesh == null) continue;
            Matrix4x4 toWorld = filter.transform.localToWorldMatrix;
            foreach (Vector3 v in filter.sharedMesh.vertices)
            {
                Vector3 w = toWorld.MultiplyPoint3x4(v);
                var p = new Vector3(Vector3.Dot(w, a), w.y, Vector3.Dot(w, b));
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
        }
    }

}
