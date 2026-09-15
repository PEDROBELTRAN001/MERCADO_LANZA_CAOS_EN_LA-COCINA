using UnityEngine;
using UnityEditor;

public class SceneBuilder : EditorWindow
{
    [MenuItem("Mercado/Build Complete Scene")]
    public static void BuildScene()
    {
        if (!EditorUtility.DisplayDialog("Construir Escena",
            "Esto creará toda la escena del mercado.\n¿Continuar?",
            "Si", "Cancelar"))
            return;

        // Limpiar escena anterior
        foreach (GameObject obj in FindObjectsByType<GameObject>())
        {
            if (obj.name.StartsWith("Wall_") || obj.name.StartsWith("Floor_") ||
                obj.name.StartsWith("Counter_") || obj.name.StartsWith("Stove") ||
                obj.name.StartsWith("Oven") || obj.name.StartsWith("Prep") ||
                obj.name.StartsWith("Washing") || obj.name.StartsWith("Plate") ||
                obj.name.StartsWith("Trash") || obj.name.StartsWith("Stair") ||
                obj.name.StartsWith("Step_") || obj.name.StartsWith("Mesa") ||
                obj.name.StartsWith("Serving") || obj.name.StartsWith("Player") ||
                obj.name.StartsWith("GameManager") || obj.name.StartsWith("OrderSystem") ||
                obj.name.StartsWith("Tomate") || obj.name.StartsWith("Lechuga") ||
                obj.name.StartsWith("Pan") || obj.name.Contains("Material"))
            {
                DestroyImmediate(obj);
            }
        }

        BuildKitchenFloor();
        BuildKitchenWalls();
        BuildKitchenCounters();
        BuildCookingStations();
        BuildStairs();
        BuildFoodCourtFloor();
        BuildFoodCourtWalls();
        BuildFoodCourtTables();
        BuildPlayer();
        SetupCamera();
        SetupGameManager();
        BuildIngredients();

        EditorUtility.DisplayDialog("Listo",
            "Escena construida!\n\n" +
            "Planta BAJA: Cocina (izquierda)\n" +
            "Planta ALTA: Patio de comidas (derecha)\n\n" +
            "Pulsa Play para probar.\n" +
            "WASD: Mover | E: Interactuar",
            "OK");
    }

    static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.name = name;
        mat.color = color;
        string path = "Assets/Materials/" + name + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
            AssetDatabase.CreateAsset(mat, path);
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    static GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.position = position;
        obj.transform.localScale = scale;
        if (mat != null) obj.GetComponent<Renderer>().material = mat;
        return obj;
    }

    // ==================== PLANTA BAJA - COCINA ====================
    static void BuildKitchenFloor()
    {
        Material floorMat = CreateMaterial("FloorMaterial", new Color(0.94f, 0.94f, 0.94f));
        CreatePrimitive(PrimitiveType.Cube, "Floor_Kitchen", new Vector3(-9, 0, 0), new Vector3(12, 0.1f, 12), floorMat);
    }

    static void BuildKitchenWalls()
    {
        Material wallMat = CreateMaterial("WallMaterial", new Color(0.27f, 0.51f, 0.71f));
        float h = 2.5f;
        float t = 0.2f;

        // Paredes cocina
        CreatePrimitive(PrimitiveType.Cube, "Wall_K_N", new Vector3(-9, h/2, 6), new Vector3(12, h, t), wallMat);
        CreatePrimitive(PrimitiveType.Cube, "Wall_K_S", new Vector3(-9, h/2, -6), new Vector3(12, h, t), wallMat);
        CreatePrimitive(PrimitiveType.Cube, "Wall_K_E", new Vector3(-3, h/2, 0), new Vector3(t, h, 12), wallMat);
        CreatePrimitive(PrimitiveType.Cube, "Wall_K_W", new Vector3(-15, h/2, 0), new Vector3(t, h, 12), wallMat);
    }

    static void BuildKitchenCounters()
    {
        Material counterMat = CreateMaterial("CounterMaterial", new Color(0.63f, 0.39f, 0.2f));
        float y = 0.45f;

        // Fila Norte
        for (int i = -1; i <= 1; i++)
            CreatePrimitive(PrimitiveType.Cube, "Counter_N_" + (i+1), new Vector3(-9 + i*3, y, 5.3f), new Vector3(2.5f, 0.9f, 1.2f), counterMat);

        // Fila Oeste
        for (int i = -1; i <= 1; i++)
            CreatePrimitive(PrimitiveType.Cube, "Counter_W_" + (i+1), new Vector3(-14.3f, y, i*3), new Vector3(1.2f, 0.9f, 2.5f), counterMat);

        // Fila Sur (parcial, deja hueco para escalera)
        CreatePrimitive(PrimitiveType.Cube, "Counter_S_1", new Vector3(-13, y, -5.3f), new Vector3(2.5f, 0.9f, 1.2f), counterMat);
        CreatePrimitive(PrimitiveType.Cube, "Counter_S_2", new Vector3(-5, y, -5.3f), new Vector3(2.5f, 0.9f, 1.2f), counterMat);
    }

    static void BuildCookingStations()
    {
        Material stoveMat = CreateMaterial("StoveMaterial", new Color(0.3f, 0.3f, 0.3f));
        Material prepMat = CreateMaterial("PrepMaterial", new Color(0.75f, 0.6f, 0.42f));
        Material washMat = CreateMaterial("WashMaterial", new Color(0.39f, 0.59f, 0.79f));
        Material trashMat = CreateMaterial("TrashMaterial", new Color(0.4f, 0.4f, 0.4f));

        // Estufas
        GameObject s1 = CreatePrimitive(PrimitiveType.Cube, "Stove_1", new Vector3(-11, 0.55f, -5.3f), new Vector3(1.8f, 1.1f, 1.2f), stoveMat);
        s1.AddComponent<CookingStation>();
        GameObject s2 = CreatePrimitive(PrimitiveType.Cube, "Stove_2", new Vector3(-7, 0.55f, -5.3f), new Vector3(1.8f, 1.1f, 1.2f), stoveMat);
        s2.AddComponent<CookingStation>();

        // Preparacion
        GameObject p1 = CreatePrimitive(PrimitiveType.Cube, "Prep_1", new Vector3(-9, 0.45f, -3.5f), new Vector3(1.8f, 0.9f, 1.2f), prepMat);
        p1.AddComponent<ChoppingStation>();

        // Lavabo
        GameObject w = CreatePrimitive(PrimitiveType.Cube, "WashingStation", new Vector3(-14.3f, 0.45f, 0), new Vector3(1.2f, 0.9f, 1.8f), washMat);
        w.AddComponent<WashingStation>();

        // Platos
        GameObject d = CreatePrimitive(PrimitiveType.Cube, "PlateStation", new Vector3(-3.7f, 0.45f, 0), new Vector3(1.2f, 0.9f, 1.8f), prepMat);
        d.AddComponent<PlateStation>();

        // Basura
        GameObject t = CreatePrimitive(PrimitiveType.Cube, "TrashStation", new Vector3(-3.7f, 0.45f, 3f), new Vector3(1.2f, 0.9f, 1.2f), trashMat);
        t.AddComponent<TrashStation>();
    }

    // ==================== ESCALERA ====================
    static void BuildStairs()
    {
        Material stairMat = CreateMaterial("StairMaterial", new Color(0.55f, 0.47f, 0.39f));

        // Conecta planta baja (x=-3) con planta alta (x=+3)
        for (int i = 0; i < 8; i++)
        {
            float x = -3f + (i * 0.8f);
            float y = 0.1f + (i * 0.35f);
            CreatePrimitive(PrimitiveType.Cube, "Step_" + i, new Vector3(x, y, 0), new Vector3(0.7f, 0.2f, 2f), stairMat);
        }

        // Barandal
        Material railMat = CreateMaterial("RailMaterial", new Color(0.45f, 0.37f, 0.29f));
        CreatePrimitive(PrimitiveType.Cube, "Railing_Front", new Vector3(-0.5f, 1.5f, -1.1f), new Vector3(7f, 0.15f, 0.15f), railMat);
        CreatePrimitive(PrimitiveType.Cube, "Railing_Back", new Vector3(-0.5f, 1.5f, 1.1f), new Vector3(7f, 0.15f, 0.15f), railMat);
    }

    // ==================== PLANTA ALTA - PATIO DE COMIDAS ====================
    static void BuildFoodCourtFloor()
    {
        Material floorMat = CreateMaterial("UpperFloorMaterial", new Color(0.82f, 0.72f, 0.55f));
        CreatePrimitive(PrimitiveType.Cube, "Floor_FoodCourt", new Vector3(9, 2.8f, 0), new Vector3(12, 0.1f, 12), floorMat);
    }

    static void BuildFoodCourtWalls()
    {
        Material wallMat = CreateMaterial("WallMaterial_Upper", new Color(0.22f, 0.45f, 0.65f));
        float h = 2.5f;
        float t = 0.2f;
        float y = 2.8f;

        CreatePrimitive(PrimitiveType.Cube, "Wall_FC_N", new Vector3(9, y + h/2, 6), new Vector3(12, h, t), wallMat);
        CreatePrimitive(PrimitiveType.Cube, "Wall_FC_S", new Vector3(9, y + h/2, -6), new Vector3(12, h, t), wallMat);
        CreatePrimitive(PrimitiveType.Cube, "Wall_FC_E", new Vector3(15, y + h/2, 0), new Vector3(t, h, 12), wallMat);
        CreatePrimitive(PrimitiveType.Cube, "Wall_FC_W", new Vector3(3, y + h/2, 0), new Vector3(t, h, 12), wallMat);
    }

    static void BuildFoodCourtTables()
    {
        Material tableMat = CreateMaterial("TableMaterial", new Color(0.55f, 0.47f, 0.39f));
        float y = 3.25f;

        // 6 mesas en cuadricula
        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                float x = 7f + col * 3f;
                float z = -1.5f + row * 3f;
                GameObject table = CreatePrimitive(PrimitiveType.Cube, "Mesa_" + (row*3+col+1), new Vector3(x, y, z), new Vector3(2f, 0.9f, 1.5f), tableMat);
                table.AddComponent<FoodCourtTable>();
            }
        }

        // Mostrador de servicio
        Material serveMat = CreateMaterial("ServeMaterial", new Color(0.7f, 0.7f, 0.7f));
        GameObject serving = CreatePrimitive(PrimitiveType.Cube, "ServingWindow", new Vector3(9, 3.4f, 5.3f), new Vector3(5f, 1.1f, 1.2f), serveMat);
        serving.AddComponent<ServingWindow>();
    }

    // ==================== JUGADOR ====================
    static void BuildPlayer()
    {
        Material playerMat = CreateMaterial("PlayerMaterial", new Color(1f, 0.55f, 0f));

        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(-9, 0.5f, 0);

        // Cuerpo
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Body";
        body.transform.SetParent(player.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        body.GetComponent<Renderer>().material = playerMat;

        // Cabeza
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(player.transform);
        head.transform.localPosition = new Vector3(0, 1.3f, 0);
        head.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        head.GetComponent<Renderer>().material = playerMat;

        // Mano
        GameObject hand = new GameObject("Hand");
        hand.transform.SetParent(player.transform);
        hand.transform.localPosition = new Vector3(0.6f, 0.5f, 0.5f);

        // Fisicas
        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = true;
        rb.mass = 5f;

        CapsuleCollider col = player.AddComponent<CapsuleCollider>();
        col.height = 2.5f;
        col.center = new Vector3(0, 0.5f, 0);

        player.AddComponent<PlayerController>();

        // Tag
        player.tag = "Player";
    }

    // ==================== CAMARA ====================
    static void SetupCamera()
    {
        Camera mainCam = Camera.main;
        mainCam.transform.position = new Vector3(0, 14, -12);
        mainCam.transform.rotation = Quaternion.Euler(50f, 0, 0);
        mainCam.orthographic = true;
        mainCam.orthographicSize = 12;
        mainCam.backgroundColor = new Color(0.12f, 0.12f, 0.18f);
        mainCam.clearFlags = CameraClearFlags.SolidColor;

        CameraFollow follow = mainCam.gameObject.AddComponent<CameraFollow>();
        GameObject player = GameObject.Find("Player");
        if (player != null) follow.target = player.transform;
        follow.offset = new Vector3(0, 14, -12);
        follow.smoothSpeed = 5f;
        follow.useBounds = true;
        follow.minXZ = new Vector2(-16, -8);
        follow.maxXZ = new Vector2(16, 8);
    }

    // ==================== GAME MANAGER ====================
    static void SetupGameManager()
    {
        GameObject gm = new GameObject("GameManager");
        gm.transform.position = Vector3.zero;
        gm.AddComponent<GameManager>();

        GameObject os = new GameObject("OrderSystem");
        os.transform.position = Vector3.zero;
        os.AddComponent<OrderSystem>();
    }

    // ==================== INGREDIENTES ====================
    static void BuildIngredients()
    {
        Material tomatoMat = CreateMaterial("TomatoMaterial", new Color(1f, 0.2f, 0.1f));
        Material lettuceMat = CreateMaterial("LettuceMaterial", new Color(0.1f, 0.7f, 0.2f));
        Material breadMat = CreateMaterial("BreadMaterial", new Color(1f, 0.85f, 0.4f));

        // Tomate
        GameObject tomate = CreatePrimitive(PrimitiveType.Sphere, "Tomate", new Vector3(-9f, 1.0f, -3.5f), new Vector3(0.35f, 0.35f, 0.35f), tomatoMat);
        tomate.AddComponent<IngredientPickup>().ingredientName = "Tomate";
        tomate.GetComponent<IngredientPickup>().ingredientType = IngredientPickup.IngredientType.Tomato;
        tomate.AddComponent<Ingredient>().ingredientName = "Tomate";
        tomate.GetComponent<Ingredient>().ingredientType = Ingredient.IngredientType.Tomato;

        // Lechuga
        GameObject lechuga = CreatePrimitive(PrimitiveType.Sphere, "Lechuga", new Vector3(-8.5f, 1.0f, -3.5f), new Vector3(0.4f, 0.3f, 0.4f), lettuceMat);
        lechuga.AddComponent<IngredientPickup>().ingredientName = "Lechuga";
        lechuga.GetComponent<IngredientPickup>().ingredientType = IngredientPickup.IngredientType.Lettuce;
        lechuga.AddComponent<Ingredient>().ingredientName = "Lechuga";
        lechuga.GetComponent<Ingredient>().ingredientType = Ingredient.IngredientType.Lettuce;

        // Pan
        GameObject pan = CreatePrimitive(PrimitiveType.Cube, "Pan", new Vector3(-9.5f, 1.0f, -3.5f), new Vector3(0.4f, 0.25f, 0.4f), breadMat);
        pan.AddComponent<IngredientPickup>().ingredientName = "Pan";
        pan.GetComponent<IngredientPickup>().ingredientType = IngredientPickup.IngredientType.Bread;
        pan.AddComponent<Ingredient>().ingredientName = "Pan";
        pan.GetComponent<Ingredient>().ingredientType = Ingredient.IngredientType.Bread;
    }
}
