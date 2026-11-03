using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Mercado > Construir entrada con letrero "MERCADO LANZA":
// pone la fachada del Mercado Lanza modelada en Blender (Assets/Models/MercadoLanza/FachadaMercadoLanza.fbx,
// fuente Downloads/FachadaMercadoLanza.blend) al fondo de la entrada sur, sobre la pared Wall_K_W (3):
// losas blancas, franjas de vidrio, columnas y las letras rojas en volumen arriba.
// Mira hacia el sur, que es hacia donde esta la camara, y se ajusta al ancho de esa pared.
// Si ya existe, la vuelve a poner. Despues hay que guardar la escena (Ctrl+S).
public static class MercadoLanzaEntranceTool
{
    const string RootName = "Entrada_MercadoLanza";
    const string ModelPath = "Assets/Models/MercadoLanza/FachadaMercadoLanza.fbx";

    [MenuItem("Mercado/Construir entrada con letrero \"MERCADO LANZA\"")]
    static void Build()
    {
        GameObject wall = GameObject.Find("Wall_K_W (3)");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (wall == null || model == null)
        {
            EditorUtility.DisplayDialog("Entrada del mercado",
                wall == null ? "No encontré la pared del fondo de la entrada (Wall_K_W (3))." : "No encontré el modelo " + ModelPath + ".", "OK");
            return;
        }

        GameObject old = GameObject.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        // La pared es un cubo largo en su eje z: ese es el ancho de la fachada
        Transform w = wall.transform;
        Vector3 along = new Vector3(w.forward.x, 0f, w.forward.z).normalized;
        float width = w.lossyScale.z;
        Vector3 south = Vector3.Cross(along, Vector3.up);
        if (south.z > 0) south = -south;

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Entrada Mercado Lanza");
        root.transform.SetPositionAndRotation(new Vector3(w.position.x, FloorBelow(w.position), w.position.z), Quaternion.LookRotation(-south, Vector3.up));

        var facade = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
        facade.transform.localPosition = Vector3.zero;
        facade.transform.localRotation = Quaternion.identity;

        // El marcador esta dibujado delante de la fachada (letreros hacia afuera, al sur):
        // si quedo atras por los ejes de Blender, se gira. Se mira donde esta su malla, no su pivote.
        Transform marker = facade.transform.Find("MarcadorFrente");
        if (marker != null)
        {
            Renderer markerRenderer = marker.GetComponent<Renderer>();
            Vector3 markerPoint = markerRenderer != null ? markerRenderer.bounds.center : marker.position;
            Bounds all = BoundsOf(facade);
            if (Vector3.Dot(markerPoint - all.center, south) < 0f) facade.transform.Rotate(0f, 180f, 0f, Space.World);
            marker.gameObject.SetActive(false);
        }

        // Mismo ancho que la pared, apoyada en el piso, con el frente sobre la linea de la pared
        Bounds bounds = BoundsOf(facade);
        float scale = width / Mathf.Max(0.01f, Vector3.Dot(bounds.size, new Vector3(Mathf.Abs(along.x), 0f, Mathf.Abs(along.z))));
        facade.transform.localScale *= scale;
        bounds = BoundsOf(facade);
        float frontOffset = Vector3.Dot(bounds.center - root.transform.position, south) + Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(south.x), 0f, Mathf.Abs(south.z)));
        facade.transform.position += -south * frontOffset + Vector3.up * (root.transform.position.y - bounds.min.y);

        // Choque: el edificio no se atraviesa
        Transform body = facade.transform.Find("Fachada");
        if (body != null && body.GetComponent<Collider>() == null) Undo.AddComponent<BoxCollider>(body.gameObject);

        // La pared del fondo queda en la misma linea y tapaba la fachada: deja de dibujarse (su choque sigue)
        foreach (Renderer wallRenderer in wall.GetComponentsInChildren<Renderer>())
        {
            Undo.RecordObject(wallRenderer, "Entrada Mercado Lanza");
            wallRenderer.enabled = false;
        }

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog("Entrada del mercado",
            "Listo: fachada del Mercado Lanza al fondo de la entrada sur.\nGuarda la escena (Ctrl+S) para conservarla.", "OK");
    }

    static Bounds BoundsOf(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        Bounds b = renderers.Length > 0 ? renderers[0].bounds : new Bounds(go.transform.position, Vector3.zero);
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    // El piso mas alto debajo del punto (sin contar paredes altas ni al jugador)
    static float FloorBelow(Vector3 p)
    {
        float best = float.MinValue;
        foreach (RaycastHit hit in Physics.RaycastAll(new Vector3(p.x, 50f, p.z + 3f), Vector3.down, 100f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<PlayerController>() != null) continue;
            if (hit.point.y > best && hit.point.y < -1f) best = hit.point.y;
        }
        return best > float.MinValue ? best : -3.475f;
    }
}
