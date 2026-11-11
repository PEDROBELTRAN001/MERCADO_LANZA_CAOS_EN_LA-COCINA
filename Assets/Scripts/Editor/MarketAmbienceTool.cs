using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Mercado > Colocar carteles, murales y altar de la Virgen:
// ambienta el mapa con fotos reales del Mercado Lanza (Assets/Textures/MercadoLanza, tomadas por Shirley)
// y el altar de la Virgen de Copacabana modelado en Blender (Assets/Models/MercadoLanza/AltarVirgenCopacabana.fbx,
// fuente Downloads/AltarVirgenCopacabana.blend).
// Todo va sobre el recorrido del jugador (cocina -> ventana de pedidos -> mesas), que es lo que muestra la camara,
// y mirando al sur, hacia donde esta la camara:
// - Al sur de la cocina (inicio): columna "Centro Comercial Lanza" con sus placas y la placa del Gobierno Municipal.
// - Pared de atras de la cocina: mural "Coka Quina" pintado y stickers Cascada.
// - Triangulo de barandas junto a la ventana de pedidos: rincon de la Virgen (altar + cuadro "Merlan Altos").
// - Letreros sobre mastiles: niveles (ventana de pedidos), "Calle 12" (camino a las mesas) y "Sector Comidas".
// - Mesas: kiosco "Bienvenidos al comedor Merlan Altos".
// Todo queda en "Ambientacion_MercadoLanza" y cada cosa se puede mover a mano. Si ya existe se vuelve a armar.
public static class MarketAmbienceTool
{
    const string RootName = "Ambientacion_MercadoLanza";
    const string Textures = "Assets/Textures/MercadoLanza/";
    const string Materials = "Assets/Materials/MercadoLanza";
    const string AltarPath = "Assets/Models/MercadoLanza/AltarVirgenCopacabana.fbx";

    [MenuItem("Mercado/Colocar carteles, murales y altar de la Virgen")]
    static void Build()
    {
        GameObject old = GameObject.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old);
        if (!AssetDatabase.IsValidFolder(Materials)) AssetDatabase.CreateFolder("Assets/Materials", "MercadoLanza");

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Ambientacion");
        Transform r = root.transform;
        Quaternion south = Quaternion.identity; // las fotos (quads) miran a -z, hacia la camara
        Color white = new Color(0.9f, 0.9f, 0.88f), concrete = new Color(0.62f, 0.6f, 0.56f), orange = new Color(0.92f, 0.45f, 0.1f);

        // ---------------- Al sur de la cocina: columna con placas y placa municipal
        Transform start = Group(r, "Columna_y_Placas");
        Vector3 column = OnFloor(1.5f, -10f);
        Box(start, "Columna_CentroComercial", column + Vector3.up * 2.75f, new Vector3(2.4f, 5.5f, 1.2f), white, true);
        Picture(start, "Foto_Columna", "Columna_CentroComercial", column + new Vector3(0, 1.75f, -0.62f), south, 2.4f, 3.3f, false);
        Vector3 plaqueWall = OnFloor(5.2f, -10f);
        Box(start, "Muro_Placa", plaqueWall + Vector3.up * 1.4f, new Vector3(2.6f, 2.8f, 0.4f), concrete, true);
        Picture(start, "Placa_Municipal", "Placa_Municipal", plaqueWall + new Vector3(0, 1.45f, -0.23f), south, 1.65f, 2.2f, true);

        // ---------------- Dentro del triangulo de barandas (valla 6 a 9), detras de la baranda de abajo:
        // rincon de la Virgen (altar + muro con el cuadro "Merlan Altos"); el letrero de niveles mas abajo
        Transform virgin = Group(r, "Rincon_Virgen");
        // Afuera del triangulo, con la espalda contra la baranda de abajo; el cuadro sobre mastiles detras del altar
        Vector3 railing = OutsideRailing(OnFloor(17.5f, 6f), 9.4f);
        Vector3 altar = railing + Vector3.back * 0.85f;
        Sign(virgin, "Cuadro_MerlanAltos", "Cuadro_MerlanAltos", railing + Vector3.back * 0.25f, 3.3f, 2.2f, 3.9f);
        PlaceAltar(virgin, altar, south);
        Sign(r, "Letrero_Niveles", "Letrero_Niveles", OnFloor(13.5f, -1f), 3.6f, 2.36f, 2.6f);

        // ---------------- Pared de atras de la cocina de Dona Rosa (lado norte): mural pintado y stickers Cascada.
        // Queda detras de la cocina (no tapa nada) y la camara la ve de frente mientras Tomas cocina.
        Transform mural = Group(r, "Pared_Mural");
        Vector3 wallBase = OnFloor(-9.05f, 9.1f);
        Color wallWhite = new Color(0.9f, 0.89f, 0.86f);
        Box(mural, "Pared", wallBase + Vector3.up * 2.75f, new Vector3(12.5f, 5.5f, 0.3f), wallWhite, true);
        Box(mural, "Franja_Azul", wallBase + new Vector3(0, 5.25f, -0.16f), new Vector3(12.5f, 0.5f, 0.04f), new Color(0.12f, 0.3f, 0.75f), false);
        Box(mural, "Zocalo", wallBase + new Vector3(0, 0.2f, -0.16f), new Vector3(12.5f, 0.4f, 0.04f), new Color(0.45f, 0.12f, 0.08f), false);
        Picture(mural, "Mural_CokaQuina", "Mural_CokaQuina", wallBase + new Vector3(3.6f, 2.75f, -0.17f), south, 3.8f, 4.77f, false);
        foreach (float x in new[] { -5.2f, 0.4f })
            Picture(mural, "Sticker_Cascada", "Sticker_Cascada", wallBase + new Vector3(x, 1.0f, -0.17f), south, 0.9f, 0.71f, false);
        Sign(r, "Letrero_Calle12", "Letrero_Calle12", OnFloor(7.5f, 26f), 1.8f, 0.8f, 2.3f);

        // ---------------- Mesas: kiosco del comedor y letrero detras (no las tapa desde la camara)
        Transform dining = Group(r, "Comedor_MerlanAltos");
        // Detras (al norte) del salon de vidrio de las mesas: no estorba el paso y la camara lo ve por encima
        Vector3 kiosk = OnFloor(19f, 46.5f);
        Box(dining, "Kiosco_Comedor", kiosk + Vector3.up * 1.6f, new Vector3(2.6f, 3.2f, 1.4f), orange, true);
        Picture(dining, "Letrero_ComedorMerlan", "Letrero_ComedorMerlan", kiosk + new Vector3(0, 1.6f, -0.71f), south, 2.42f, 3.16f, false);
        GameObject pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pipe.name = "Chimenea";
        pipe.transform.SetParent(dining, false);
        pipe.transform.position = kiosk + new Vector3(0.7f, 3.7f, 0f);
        pipe.transform.localScale = new Vector3(0.18f, 0.5f, 0.18f);
        pipe.GetComponent<Renderer>().sharedMaterial = Solid("Metal", new Color(0.65f, 0.66f, 0.68f));
        Object.DestroyImmediate(pipe.GetComponent<Collider>());
        // Afuera del salon de vidrio de las mesas (Kiosko enorme (3)), detras
        Sign(dining, "Letrero_SectorComidas", "Letrero_SectorComidas", OnFloor(4f, 47f), 4.6f, 2.16f, 2.4f);

        // ---------------- Letras pintadas en la pared grande del fondo (Wall_K_W (12)), donde no hay puestos delante
        GameObject backWall = GameObject.Find("Wall_K_W (12)");
        if (backWall != null)
        {
            Transform painted = Group(r, "Letras_Pintadas");
            PaintedWall(painted, backWall.transform, 19f, "BIENVENIDOS AL SECTOR", "“MERLAN”");
            PaintedWall(painted, backWall.transform, -25f, "BIENVENIDOS AL SECTOR", "“FRUTAS”");
            PaintedWall(painted, backWall.transform, 56f, "BIENVENIDOS AL SECTOR", "“VERDURAS”");
        }

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog("Ambientación",
            "Listo: carteles, murales y el altar de la Virgen de Copacabana, sobre el recorrido de Tomás.\nCada cosa se puede mover a mano.\nGuarda la escena (Ctrl+S) para conservarlos.", "OK");
    }

    static void PlaceAltar(Transform parent, Vector3 position, Quaternion facing)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(AltarPath);
        if (model == null) return;
        var holder = new GameObject("Altar_VirgenCopacabana");
        holder.transform.SetParent(parent, false);
        holder.transform.SetPositionAndRotation(position, facing);
        var altar = (GameObject)PrefabUtility.InstantiatePrefab(model, holder.transform);
        altar.transform.localPosition = Vector3.zero;
        altar.transform.localRotation = Quaternion.identity;

        // Unidad del FBX (el marcador mide 5 cm) y frente hacia la camara
        Transform marker = altar.transform.Find("MarcadorFrente");
        Renderer markerRenderer = marker != null ? marker.GetComponent<Renderer>() : null;
        if (markerRenderer != null)
        {
            if (markerRenderer.bounds.size.y > 1e-6f) altar.transform.localScale *= 0.05f / markerRenderer.bounds.size.y;
            Vector3 south = facing * Vector3.back;
            if (Vector3.Dot(markerRenderer.bounds.center - position, south) < 0f) altar.transform.Rotate(0f, 180f, 0f, Space.World);
            marker.gameObject.SetActive(false);
        }

        // Apoyado en el piso, con choque, y la foto de la Virgen dentro del nicho
        Transform body = altar.transform.Find("Altar");
        if (body != null)
        {
            Renderer bodyRenderer = body.GetComponent<Renderer>();
            if (bodyRenderer != null) altar.transform.position += Vector3.up * (position.y - bodyRenderer.bounds.min.y);
            if (body.GetComponent<Collider>() == null) body.gameObject.AddComponent<BoxCollider>();
        }
        Transform image = altar.transform.Find("Imagen_Virgen");
        if (image != null && image.TryGetComponent(out Renderer imageRenderer)) imageRenderer.sharedMaterial = Photo("Virgen_Copacabana");
    }

    static Transform Group(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static Vector3 OnFloor(float x, float z) => new Vector3(x, FloorBelow(new Vector3(x, 0f, z)), z);

    // Letras magenta pintadas sobre una franja blanca con zocalo rojo (como en el sector Merlan),
    // en la cara de la pared que mira al sur, a esa x del mundo
    static void PaintedWall(Transform parent, Transform wall, float x, string line1, string line2)
    {
        Vector3 along = new Vector3(wall.forward.x, 0f, wall.forward.z).normalized;
        Vector3 south = new Vector3(wall.right.x, 0f, wall.right.z).normalized;
        if (south.z > 0f) south = -south;
        float k = (x - wall.position.x) / (Mathf.Abs(along.x) > 0.01f ? along.x : 1f);
        Vector3 face = wall.position + along * k + south * (wall.lossyScale.x / 2f + 0.02f);
        face.y = FloorBelow(face + south * 2f);
        Quaternion facing = Quaternion.LookRotation(-south, Vector3.up);

        var holder = new GameObject("Pintado " + line2.Trim('“', '”'));
        holder.transform.SetParent(parent, false);
        holder.transform.SetPositionAndRotation(face, facing);
        Box(holder.transform, "Pintura_Blanca", face + Vector3.up * 1.55f, new Vector3(13f, 2.5f, 0.03f), new Color(0.96f, 0.95f, 0.95f), false, facing);
        Box(holder.transform, "Zocalo_Rojo", face + Vector3.up * 0.15f + south * 0.01f, new Vector3(13f, 0.3f, 0.03f), new Color(0.65f, 0.15f, 0.08f), false, facing);

        Color magenta = new Color(0.82f, 0.08f, 0.42f);
        Letters(holder.transform, line1, face + Vector3.up * 2.2f + south * 0.03f, facing, 0.95f, magenta);
        Letters(holder.transform, line2, face + Vector3.up * 1.2f + south * 0.03f, facing, 0.8f, magenta);
    }

    static void Letters(Transform parent, string text, Vector3 position, Quaternion rotation, float height, Color color)
    {
        var go = new GameObject("Letras");
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, rotation);
        TextMesh mesh = go.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.fontSize = 100;
        mesh.characterSize = height / 10f;
        mesh.fontStyle = FontStyle.Bold;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = color;
    }

    // Desde afuera del triangulo (al sur) busca hacia el norte la cara de afuera de la baranda de abajo (con su base)
    static Vector3 OutsideRailing(Vector3 outside, float fallbackZ)
    {
        Physics.SyncTransforms();
        Vector3 origin = outside + Vector3.up * 0.3f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.forward, 12f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            string owner = hit.collider.transform.root.name;
            if (owner == "valla (8)" || owner == "valla (6)") return new Vector3(outside.x, outside.y, hit.point.z);
        }
        return new Vector3(outside.x, outside.y, fallbackZ);
    }

    // El piso mas alto debajo del punto (las paredes altas y el jugador no cuentan)
    static float FloorBelow(Vector3 p)
    {
        float best = float.MinValue;
        foreach (RaycastHit hit in Physics.RaycastAll(new Vector3(p.x, 50f, p.z), Vector3.down, 100f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<PlayerController>() != null) continue;
            if (hit.point.y > best && hit.point.y < -1f) best = hit.point.y;
        }
        return best > float.MinValue ? best : -3.475f;
    }

    // Foto en un marco: el quad mira hacia -z local (hacia la camara del juego)
    static void Picture(Transform parent, string name, string texture, Vector3 center, Quaternion rotation, float width, float height, bool backing)
    {
        var holder = new GameObject(name);
        holder.transform.SetParent(parent, false);
        holder.transform.SetPositionAndRotation(center, rotation);
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Foto";
        quad.transform.SetParent(holder.transform, false);
        quad.transform.localScale = new Vector3(width, height, 1f);
        quad.GetComponent<Renderer>().sharedMaterial = Photo(texture);
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        if (backing)
        {
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Marco";
            board.transform.SetParent(holder.transform, false);
            board.transform.localPosition = new Vector3(0, 0, 0.03f);
            board.transform.localScale = new Vector3(width + 0.08f, height + 0.08f, 0.05f);
            board.GetComponent<Renderer>().sharedMaterial = Solid("MarcoOscuro", new Color(0.18f, 0.18f, 0.2f));
            Object.DestroyImmediate(board.GetComponent<Collider>());
        }
    }

    // Letrero sobre dos mastiles que salen del piso (con travesano arriba), un poco inclinado hacia la camara
    static void Sign(Transform parent, string name, string texture, Vector3 floorPoint, float width, float height, float bottom)
    {
        var holder = new GameObject(name);
        holder.transform.SetParent(parent, false);
        holder.transform.position = floorPoint;
        Vector3 center = floorPoint + new Vector3(0f, bottom + height / 2f, -0.12f);
        Picture(holder.transform, "Letrero", texture, center, Quaternion.Euler(8f, 0f, 0f), width, height, true);

        Material metal = Solid("MastilVerde", new Color(0.16f, 0.4f, 0.28f));
        float top = bottom + height + 0.25f;
        foreach (float side in new[] { -1f, 1f })
        {
            GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.name = "Mastil";
            mast.transform.SetParent(holder.transform, false);
            mast.transform.position = floorPoint + new Vector3(side * (width / 2f + 0.08f), top / 2f, 0.05f);
            mast.transform.localScale = new Vector3(0.12f, top / 2f, 0.12f);
            mast.GetComponent<Renderer>().sharedMaterial = metal;

            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "Remate";
            cap.transform.SetParent(holder.transform, false);
            cap.transform.position = floorPoint + new Vector3(side * (width / 2f + 0.08f), top, 0.05f);
            cap.transform.localScale = Vector3.one * 0.2f;
            cap.GetComponent<Renderer>().sharedMaterial = metal;
            Object.DestroyImmediate(cap.GetComponent<Collider>());
        }
        Box(holder.transform, "Travesano", floorPoint + new Vector3(0f, top - 0.12f, 0.05f), new Vector3(width + 0.3f, 0.08f, 0.08f), new Color(0.16f, 0.4f, 0.28f), false);
    }

    static void Box(Transform parent, string name, Vector3 center, Vector3 size, Color color, bool collider, Quaternion? rotation = null)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.SetPositionAndRotation(center, rotation ?? Quaternion.identity);
        box.transform.localScale = size;
        box.GetComponent<Renderer>().sharedMaterial = Solid(name, color);
        if (!collider) Object.DestroyImmediate(box.GetComponent<Collider>());
    }

    // Material sin luz con la foto, para que se lea igual de dia que en las zonas oscuras
    static Material Photo(string texture)
    {
        string path = Materials + "/Foto_" + texture + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            mat = new Material(unlit != null ? unlit : Shader.Find("Unlit/Texture"));
            AssetDatabase.CreateAsset(mat, path);
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + texture + ".jpg");
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        mat.mainTexture = tex;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", UnityEngine.Color.white);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material Solid(string name, Color color)
    {
        string path = Materials + "/" + name + ".mat";
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
}
