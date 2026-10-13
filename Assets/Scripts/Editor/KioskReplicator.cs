using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Copias DECORATIVAS de los elementos de la cocina de Doña Rosa, acomodadas DENTRO de los
// "Kiosko enorme" del mapa de Pedro (los kioskos se mantienen; su interior esta vacio).
// Representan otros puestos del mercado: no tienen scripts, solo se juega en la cocina original.
// Se copian los muebles y platos, no las paredes ni el piso (el kiosko ya los tiene).
public static class KioskReplicator
{
    const string KioskPrefix = "Kiosko enorme";
    const string ReplicaPrefix = "Cocina_Kiosko_";
    // Las piezas copiadas llevan este prefijo para que la limpieza de SceneBuilder (por prefijos como
    // "Counter_", "Mesa", "Wall_") no las borre al reconstruir la escena
    const string PartPrefix = "Deco_";

    // Cocina original segun SceneBuilder: x -15..-3, z -6..6, cara superior del piso en y = 0.05
    static readonly Rect KitchenArea = Rect.MinMaxRect(-15.3f, -6.3f, -2.7f, 6.3f);
    static readonly Vector3 KitchenPivot = new Vector3(-9f, 0.05f, 0f);
    const float KitchenSize = 12f;

    // Medido en "Kiosko enorme.glb": piso interior elevado 0.53 m sobre la base, de 12.0 x 9.7 m
    const float KioskFloorHeight = 0.53f;
    // Margen para que los muebles no toquen los postes del kiosko
    const float FitMargin = 0.92f;

    [MenuItem("Mercado/Replicar cocina en kioskos")]
    public static void Replicate()
    {
        RemoveReplicas();

        List<GameObject> parts = FindKitchenParts();
        if (parts.Count == 0)
        {
            EditorUtility.DisplayDialog("Replicar cocina", "No se encontro la cocina. Ejecuta primero Mercado/Build Complete Scene.", "OK");
            return;
        }

        // Plantilla: copia de la cocina sin scripts, con el pivote en el centro de la cocina
        GameObject template = new GameObject("CocinaPlantilla");
        template.transform.position = KitchenPivot;
        foreach (GameObject part in parts)
        {
            GameObject copy = Object.Instantiate(part, part.transform.position, part.transform.rotation, template.transform);
            copy.name = part.name;
        }
        StripToDecoration(template);

        int count = 0;
        foreach (GameObject kiosk in FindKiosks())
        {
            if (!GetFootprint(kiosk, out Vector3 center, out float baseY, out Vector3 longAxis, out float length, out float width))
                continue;

            GameObject replica = Object.Instantiate(template);
            replica.name = ReplicaPrefix + (++count);

            // Los muebles mantienen su tamaño: se acercan entre si para caber en el piso del kiosko
            Arrange(replica.transform, Mathf.Min(1f, length * FitMargin / KitchenSize), Mathf.Min(1f, width * FitMargin / KitchenSize));

            // El eje X de la cocina se alinea con el lado largo del kiosko, sobre su piso elevado
            float yaw = Mathf.Atan2(-longAxis.z, longAxis.x) * Mathf.Rad2Deg;
            replica.transform.SetPositionAndRotation(new Vector3(center.x, baseY + KioskFloorHeight, center.z), Quaternion.Euler(0f, yaw, 0f));
            Undo.RegisterCreatedObjectUndo(replica, "Replicar cocina en kioskos");
        }

        Object.DestroyImmediate(template);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Replicar cocina",
            count + " kioskos grandes con elementos de cocina.\nGuarda la escena para conservarlos.", "OK");
    }

    // Acerca cada mueble al centro (sin cambiar su tamaño). Un grupo sin malla propia en el origen
    // (como COCINA_DOÑA_ROSA) se reparte por sus hijos: fogon, estante, barra...
    static void Arrange(Transform replica, float factorX, float factorZ)
    {
        // Se llama recien instanciada la copia, todavia en la posicion original de la cocina
        var units = new List<Transform>();
        foreach (Transform part in replica)
        {
            bool isGroup = part.GetComponent<Renderer>() == null && part.childCount > 0 && part.position.sqrMagnitude < 0.01f;
            if (isGroup) foreach (Transform child in part) units.Add(child);
            else units.Add(part);
        }
        foreach (Transform unit in units)
        {
            Vector3 local = unit.position - replica.position;
            unit.position = replica.position + new Vector3(local.x * factorX, local.y, local.z * factorZ);
        }
    }

    [MenuItem("Mercado/Quitar cocinas de kioskos")]
    public static void RemoveReplicas()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name.StartsWith(ReplicaPrefix))
                Undo.DestroyObjectImmediate(root);
            else if (root.name.StartsWith(KioskPrefix) && !root.activeSelf)
            {
                Undo.RecordObject(root, "Mostrar kiosko");
                root.SetActive(true);
            }
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    // Objetos raiz cuyo centro visible cae dentro de la cocina original (sin jugador ni camara)
    static List<GameObject> FindKitchenParts()
    {
        var parts = new List<GameObject>();
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (!root.activeInHierarchy || root.name == "Player" || root.name.StartsWith(ReplicaPrefix) || root.name.StartsWith(KioskPrefix))
                continue;
            // Paredes y piso no: el kiosko tiene los suyos
            if (root.name.StartsWith("Wall_") || root.name.StartsWith("Floor_"))
                continue;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) continue;

            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
            if (b.center.y < 5f && KitchenArea.Contains(new Vector2(b.center.x, b.center.z)))
                parts.Add(root);
        }
        return parts;
    }

    static List<GameObject> FindKiosks()
    {
        var kiosks = new List<GameObject>();
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name.StartsWith(KioskPrefix)) kiosks.Add(root);
        return kiosks;
    }

    // Quita scripts (decorativo) y renombra las piezas para que el SceneBuilder no las limpie
    static void StripToDecoration(GameObject root)
    {
        foreach (MonoBehaviour script in root.GetComponentsInChildren<MonoBehaviour>(true))
            Object.DestroyImmediate(script);
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t != root.transform) t.name = PartPrefix + t.name;
    }

    // Base del kiosko: centro, altura del suelo y lados (el modelo .glb viene rotado, se mide en su espacio local)
    static bool GetFootprint(GameObject kiosk, out Vector3 center, out float baseY, out Vector3 longAxis, out float length, out float width)
    {
        center = Vector3.zero; baseY = 0f; longAxis = Vector3.right; length = width = 0f;

        MeshFilter mf = kiosk.GetComponentInChildren<MeshFilter>(true);
        Renderer r = mf != null ? mf.GetComponent<Renderer>() : null;
        if (mf == null || mf.sharedMesh == null || r == null) return false;

        Bounds local = mf.sharedMesh.bounds;
        Vector3[] axes =
        {
            mf.transform.TransformVector(new Vector3(local.size.x, 0f, 0f)),
            mf.transform.TransformVector(new Vector3(0f, local.size.y, 0f)),
            mf.transform.TransformVector(new Vector3(0f, 0f, local.size.z)),
        };

        // El eje mas vertical es la altura; los otros dos son los lados de la base
        int up = 0;
        for (int i = 1; i < 3; i++) if (Mathf.Abs(axes[i].y) > Mathf.Abs(axes[up].y)) up = i;
        Vector3 a = axes[(up + 1) % 3], b = axes[(up + 2) % 3];
        a.y = 0f; b.y = 0f;

        Vector3 longSide = a.magnitude >= b.magnitude ? a : b;
        length = Mathf.Max(a.magnitude, b.magnitude);
        width = Mathf.Min(a.magnitude, b.magnitude);
        longAxis = longSide.normalized;

        // Activar un momento para leer los bounds en mundo si el kiosko estaba oculto
        bool wasActive = kiosk.activeSelf;
        kiosk.SetActive(true);
        center = r.bounds.center;
        baseY = r.bounds.min.y;
        kiosk.SetActive(wasActive);
        return length > 0.1f;
    }
}
