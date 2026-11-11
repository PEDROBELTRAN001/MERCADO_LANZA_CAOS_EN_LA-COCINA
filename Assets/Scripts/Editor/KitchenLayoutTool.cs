using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Aplica posiciones finales, no desplazamientos acumulativos: puede usarse varias veces.
public static class KitchenLayoutTool
{

    // Solicitud puntual: aplica a la escena cargada al volver a modo edicion.
    private const string RowRequest = "Library/KitchenRowLayout.request";

    [InitializeOnLoadMethod]
    private static void WaitForRowLayout()
    {
        EditorApplication.update -= ApplyPendingRow;
        if (System.IO.File.Exists(RowRequest)) EditorApplication.update += ApplyPendingRow;
    }

    private static void ApplyPendingRow()
    {
        if (!System.IO.File.Exists(RowRequest))
        {
            EditorApplication.update -= ApplyPendingRow;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/KitchenScene.unity");
        if (!scene.IsValid() || !scene.isLoaded) return;
        var objects = new Dictionary<string, Transform>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (!objects.ContainsKey(t.name)) objects.Add(t.name, t);
        string[] required = { "Prep_1", "01_Fogon_Industrial", "04_Olla", "05_Sarten", "Mesa_olla", "Cubierta_olla", "Mesa_sarten", "Cubierta_sarten" };
        foreach (var name in required) if (!objects.ContainsKey(name)) return;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Alinear hornallas, olla y sarten");
        void Place(string name, float x, float? width = null)
        {
            if (!objects.TryGetValue(name, out var t)) return;
            Undo.RecordObject(t, "Alinear cocina");
            var p = t.position; p.x = x; p.z = -5.1f; t.position = p;
            if (width.HasValue) { var scale = t.localScale; scale.x = width.Value; t.localScale = scale; }
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        }
        Place("Prep_1", -14.2f, 2.16f);
        Place("Rotulo_Prep_1", -14.2f);
        Place("01_Fogon_Industrial", -10.245f);
        Place("Rotulo_01_Fogon_Industrial", -10.245f);
        Place("04_Olla", -6.2775f);
        Place("Mesa_olla", -6.2775f, 2.185f);
        Place("Cubierta_olla", -6.2775f, 2.185f);
        Place("05_Sarten", -4.0925f);
        Place("Mesa_sarten", -4.0925f, 2.185f);
        Place("Cubierta_sarten", -4.0925f, 2.185f);
        if (objects.TryGetValue("Counter_N_0", out var decorative))
        {
            Undo.RecordObject(decorative.gameObject, "Retirar meson decorativo");
            decorative.gameObject.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(decorative.gameObject);
        }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) return;
        System.IO.File.Delete(RowRequest);
        EditorApplication.update -= ApplyPendingRow;
        Debug.Log("Cocina guardada: tabla, hornallas, olla y sarten alineadas en una fila.");
    }


    private const string PrepRequest = "Library/KitchenPrepFlush.request";

    [InitializeOnLoadMethod]
    private static void WaitForPrepFlush()
    {
        EditorApplication.update -= ApplyPendingPrepFlush;
        if (System.IO.File.Exists(PrepRequest)) EditorApplication.update += ApplyPendingPrepFlush;
    }

    private static void ApplyPendingPrepFlush()
    {
        if (!System.IO.File.Exists(PrepRequest)) { EditorApplication.update -= ApplyPendingPrepFlush; return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/KitchenScene.unity");
        if (!scene.IsValid() || !scene.isLoaded) return;
        Transform prep = null, stove = null, label = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Prep_1") prep = t;
                if (t.name == "01_Fogon_Industrial") stove = t;
                if (t.name == "Rotulo_Prep_1") label = t;
            }
        if (prep == null || stove == null) return;
        var top = stove.Find("Cubierta");
        var target = top != null ? top.GetComponent<Renderer>() : null;
        var surface = prep.GetComponent<Renderer>();
        if (target == null || surface == null) return;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Alinear tabla con cubierta de hornallas");
        Undo.RecordObject(prep, "Alinear tabla de picar");
        Vector3 before = prep.position;
        var scale = prep.localScale;
        scale.z *= target.bounds.size.z / surface.bounds.size.z;
        prep.localScale = scale;
        var position = prep.position;
        position.z += target.bounds.center.z - surface.bounds.center.z;
        position.y += target.bounds.max.y - surface.bounds.max.y;
        prep.position = position;
        PrefabUtility.RecordPrefabInstancePropertyModifications(prep);
        if (label != null)
        {
            Undo.RecordObject(label, "Alinear rotulo de picado");
            label.position += prep.position - before;
            PrefabUtility.RecordPrefabInstancePropertyModifications(label);
        }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) return;
        System.IO.File.Delete(PrepRequest);
        EditorApplication.update -= ApplyPendingPrepFlush;
        Debug.Log("Tabla de picar alineada con las hornallas: misma altura y bordes, conservando los otros muebles.");
    }

    private const string Menu = "Mercado/Acomodar cocina y seleccionar cambios";

    [MenuItem(Menu, true)]
    private static bool CanApply() => !EditorApplication.isPlayingOrWillChangePlaymode;

    [MenuItem(Menu)]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "KitchenScene")
        {
            EditorUtility.DisplayDialog("Cocina", "Abre KitchenScene antes de usar esta opción.", "OK");
            return;
        }
        var changed = new List<Object>();
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Acomodar cocina de Doña Rosa");

        Transform Find(string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == name) return root.transform;
            return null;
        }
        void Record(Transform t)
        {
            Undo.RecordObject(t, "Acomodar cocina");
            if (!changed.Contains(t.gameObject)) changed.Add(t.gameObject);
        }
        void Finish(Transform t) => PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        void MoveX(string name, float x)
        {
            var t = Find(name);
            if (t == null) return;
            Record(t);
            var p = t.position; p.x = x; t.position = p;
            Finish(t);
        }
        void Surface(string name, float x, float z, float width, float depth)
        {
            var t = Find(name);
            if (t == null) return;
            Record(t);
            var p = t.position; p.x = x; p.z = z; t.position = p;
            var scale = t.localScale; scale.x = width; scale.z = depth; t.localScale = scale;
            Finish(t);
        }
        void Position(Transform t, Vector3 position)
        {
            if (t == null) return;
            Record(t);
            t.position = position;
            Finish(t);
        }
        void Decor(string name, Vector3 position, Vector3 scale)
        {
            var t = Find(name);
            if (t == null) return;
            Record(t);
            t.position = position;
            t.localScale = scale;
            Finish(t);
        }

        Surface("Floor_Kitchen", -9.9f, 0, 13.8f, 12);
        Surface("Wall_K_S", -9.9f, -6, 13.8f, .2f);
        // Picado ocupa el antiguo sitio de la olla, a la izquierda de las hornallas.
        Surface("Prep_1", -14.2f, -5.1f, 2.16f, 1.44f);
        Surface("Counter_W_0", -16.2f, -3f, 1.2f, 2.76f);
        Surface("Mesa_Armado", -4.1f, 0, 1.44f, 2.07f);
        MoveX("Rotulo_WashingStation", -14.4f);
        MoveX("Rotulo_Mesa_Armado", -5.5f);
        MoveX("Rotulo_01_Fogon_Industrial", -10.245f);
        var prepLabel = Find("Rotulo_Prep_1");
        if (prepLabel != null)
        {
            Record(prepLabel);
            var p = prepLabel.position; p.x = -14.2f; p.z = -5.1f; prepLabel.position = p;
            Finish(prepLabel);
        }
        foreach (string name in new[] { "Counter_W_2", "Plato_EnsaladaDeFrutas_1", "Plato_ApiConPastel_1" })
            MoveX(name, -16.2f);

        // El mesón de los dos platos vuelve bajo el lavaplatos, como indica la flecha.
        MoveX("Counter_W_0", -16.2f);
        MoveX("Plato_SandwichDeChola_1", -16.2f);
        MoveX("Plato_PlatoPaceno_1", -16.2f);

        // Dos cubos ocupaban el mismo lugar y compartían la superficie superior.
        var decorative = Find("Counter_N_0");
        if (decorative != null)
        {
            Undo.RecordObject(decorative.gameObject, "Retirar mes?n decorativo");
            decorative.gameObject.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(decorative.gameObject);
            changed.Add(decorative.gameObject);
        }
        var duplicate = Find("Counter_W_1");
        if (duplicate != null)
        {
            Undo.RecordObject(duplicate.gameObject, "Quitar mesón superpuesto");
            duplicate.gameObject.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(duplicate.gameObject);
            changed.Add(duplicate.gameObject);
        }
        Surface("WashingStation", -16f, 0, 1.6f, 2.3f);
        var sink = Find("WashingStation");
        if (sink != null)
        {
            float top = sink.position.y + sink.localScale.y * .5f;
            Decor("Washing_Pileta", new Vector3(-16, top + .10f, -.4f), new Vector3(1.4f, .12f, 1.2f));
            // El agua queda por encima del acero, con separación entre superficies.
            Decor("Washing_Agua", new Vector3(-15.95f, top + .175f, -.4f), new Vector3(1.1f, .02f, .95f));
            Decor("Washing_Grifo", new Vector3(-16.55f, top + .4f, -.4f), new Vector3(.09f, .35f, .09f));
            Decor("Washing_Caño", new Vector3(-16.3f, top + .65f, -.4f), new Vector3(.45f, .07f, .07f));
            for (int i = 0; i < 4; i++)
                Decor("Washing_Plato_" + i, new Vector3(-16, top + .06f + i * .05f, .7f), new Vector3(.65f, .015f, .65f));
        }

        // Una sola fila para los once ingredientes, incluidas las tres frutas.
        var types = new[] { Ingredient.IngredientType.Potato, Ingredient.IngredientType.Corn,
            Ingredient.IngredientType.Bean, Ingredient.IngredientType.Cheese, Ingredient.IngredientType.PurpleCorn,
            Ingredient.IngredientType.Dough, Ingredient.IngredientType.Bread, Ingredient.IngredientType.Meat,
            Ingredient.IngredientType.Banana, Ingredient.IngredientType.Papaya, Ingredient.IngredientType.Strawberry };
        var crates = Object.FindObjectsByType<IngredientCrate>();
        float? rowHeight = null;
        for (int i = 0; i < types.Length; i++)
        {
            IngredientCrate best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (var crate in crates)
            {
                if (crate.gameObject.scene != scene || crate.ingredientType != types[i]) continue;
                Vector3 p = crate.transform.position;
                if (p.x < -18 || p.x > -2 || p.z < -8 || p.z > 12) continue;
                float distance = (new Vector2(p.x, p.z) - new Vector2(-16 + i * .78f, 4.2f)).sqrMagnitude;
                if (distance < bestDistance) { best = crate; bestDistance = distance; }
            }
            if (best == null) continue;
            var position = best.transform.position;
            if (!rowHeight.HasValue) rowHeight = position.y;
            position.x = -16 + i * .78f; position.z = 4.2f; position.y = rowHeight.Value;
            Position(best.transform, position);
        }

        var kitchen = Find("COCINA_DOÑA_ROSA");
        if (kitchen != null)
        {
            // La barra y los platos se desplazan juntos, sin regenerar sus modelos.
            foreach (string name in new[] { "03_Barra_Despacho", "07_Plato_Paceno", "08_Sandwich_Chola" })
            {
                var t = kitchen.Find(name);
                if (t == null) continue;
                Vector3 p = t.position;
                p.x = name == "07_Plato_Paceno" ? -6.75f : name == "08_Sandwich_Chola" ? -5.15f : -5.95f;
                p.z = 4.6f;
                Position(t, p);
            }
            foreach (string name in new[] { "04_Olla", "Mesa_olla", "Cubierta_olla", "01_Fogon_Industrial" })
            {
                var t = kitchen.Find(name);
                if (t == null) continue;
                Record(t);
                var p = t.localPosition;
                if (name == "01_Fogon_Industrial") p.x = -10.245f;
                else p.x = -6.2775f;
                p.z = -5.1f;
                t.localPosition = p;
                if (name == "01_Fogon_Industrial") t.localScale = new Vector3(1.15f, 1, 1.1f);
                else if (name != "04_Olla")
                {
                    var scale = t.localScale; scale.x = 2.185f; t.localScale = scale;
                }
                Finish(t);
            }
        }
        // Cubierta continua desde las hornallas hasta el extremo este.
        if (kitchen != null)
        {
            foreach (string name in new[] { "Mesa_sarten", "Cubierta_sarten", "05_Sarten" })
            {
                var t = kitchen.Find(name);
                if (t == null) continue;
                Record(t);
                var p = t.localPosition; p.x = -4.0925f; p.z = -5.1f; t.localPosition = p;
                if (name != "05_Sarten")
                {
                    var scale = t.localScale; scale.x = 2.185f; t.localScale = scale;
                }
                Finish(t);
            }
        }
        var zone = Find("ZonaZoom_Cocina");
        if (zone != null)
        {
            var zoom = zone.GetComponent<CameraZoomTrigger>();
            if (zoom != null)
            {
                Undo.RecordObject(zoom, "Vista de cocina");
                var data = new SerializedObject(zoom);
                data.FindProperty("closeZoom").floatValue = 5;
                data.FindProperty("frontalAngleX").floatValue = 48;
                data.FindProperty("closeOffset").vector3Value = new Vector3(0, 12, -11);
                data.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(zoom);
            }
            var collider = zone.GetComponent<BoxCollider>();
            if (collider != null)
            {
                Undo.RecordObject(collider, "Área de cámara de cocina");
                collider.size = new Vector3(25, 4, 16);
                collider.center = new Vector3(-4.9f, -.5f, -.5f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
            }
        }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.objects = changed.ToArray();
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
        Debug.Log("Cocina acomodada y objetos seleccionados. Guarda con Ctrl+S. Puedes deshacer con Ctrl+Z.");
    }
}
