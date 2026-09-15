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

        BuildFloor();
        BuildWalls();
        BuildCounters();
        BuildCookingStations();
        BuildStairs();
        BuildUpperFloor();
        BuildFoodCourt();
        BuildPlayer();
        SetupCamera();
        SetupGameManager();
        BuildIngredients();

        EditorUtility.DisplayDialog("Listo",
            "Escena construida!\n\n" +
            "Pulsa Play para probar.\n" +
            "WASD: Mover | E: Interactuar",
            "OK");
    }

    static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        Material mat = new Material(shader);
        mat.name = name;
        mat.color = color;
        string path = "Assets/Materials/" + name + ".mat";
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.position = position;
        obj.transform.localScale = scale;
        if (mat != null)
        {
            obj.GetComponent<Renderer>().material = mat;
        }
        return obj;
    }

    static void BuildFloor()
    {
        Material floorMat = CreateMaterial("FloorMaterial", new Color(0.94f, 0.94f, 0.94f));
        CreatePrimitive(PrimitiveType.Cube, "Floor_Kitchen", new Vector3(0, -0.05f, 0), new Vector3(14, 0.1f, 14), floorMat);
    }

    static void BuildWalls()
    {
        Material wallMat = CreateMaterial("WallMaterial", new Color(0.27f, 0.51f, 0.71f));
        float wallHeight = 3f;
        float wallThickness = 0.3f;

        // Pared Norte
        CreatePrimitive(PrimitiveType.Cube, "Wall_North", new Vector3(0, wallHeight / 2, 7), new Vector3(14, wallHeight, wallThickness), wallMat);
        // Pared Sur
        CreatePrimitive(PrimitiveType.Cube, "Wall_South", new Vector3(0, wallHeight / 2, -7), new Vector3(14, wallHeight, wallThickness), wallMat);
        // Pared Este
        CreatePrimitive(PrimitiveType.Cube, "Wall_East", new Vector3(7, wallHeight / 2, 0), new Vector3(wallThickness, wallHeight, 14), wallMat);
        // Pared Oeste
        CreatePrimitive(PrimitiveType.Cube, "Wall_West", new Vector3(-7, wallHeight / 2, 0), new Vector3(wallThickness, wallHeight, 14), wallMat);
    }

    static void BuildCounters()
    {
        Material counterMat = CreateMaterial("CounterMaterial", new Color(0.63f, 0.39f, 0.2f));
        float counterY = 0.45f;
        float counterH = 0.9f;

        // Fila Norte (contra pared norte)
        for (int i = -2; i <= 2; i++)
        {
            CreatePrimitive(PrimitiveType.Cube, "Counter_North_" + (i + 2),
                new Vector3(i * 2.5f, counterY, 6.2f), new Vector3(2.2f, counterH, 1.2f), counterMat);
        }

        // Fila Oeste (contra pared oeste)
        for (int i = -2; i <= 2; i++)
        {
            CreatePrimitive(PrimitiveType.Cube, "Counter_West_" + (i + 2),
                new Vector3(-6.2f, counterY, i * 2.5f), new Vector3(1.2f, counterH, 2.2f), counterMat);
        }

        // Fila Este (contra pared este)
        for (int i = -2; i <= 2; i++)
        {
            CreatePrimitive(PrimitiveType.Cube, "Counter_East_" + (i + 2),
                new Vector3(6.2f, counterY, i * 2.5f), new Vector3(1.2f, counterH, 2.2f), counterMat);
        }
    }

    static void BuildCookingStations()
    {
        Material stoveMat = CreateMaterial("StoveMaterial", new Color(0.3f, 0.3f, 0.3f));
        Material prepMat = CreateMaterial("PrepMaterial", new Color(0.75f, 0.6f, 0.42f));

        // Estufas (fila sur)
        GameObject estufa1 = CreatePrimitive(PrimitiveType.Cube, "Stove_1", new Vector3(-3, 0.55f, -5.5f), new Vector3(1.8f, 1.1f, 1.4f), stoveMat);
        estufa1.AddComponent<CookingStation>();

        GameObject estufa2 = CreatePrimitive(PrimitiveType.Cube, "Stove_2", new Vector3(-0.5f, 0.55f, -5.5f), new Vector3(1.8f, 1.1f, 1.4f), stoveMat);
        estufa2.AddComponent<CookingStation>();

        GameObject horno = CreatePrimitive(PrimitiveType.Cube, "Oven", new Vector3(2, 0.55f, -5.5f), new Vector3(1.8f, 1.1f, 1.4f), stoveMat);
        horno.AddComponent<CookingStation>();

        // Mesas de preparacion
        GameObject prep1 = CreatePrimitive(PrimitiveType.Cube, "PrepStation_1", new Vector3(4.5f, 0.45f, -5.5f), new Vector3(1.8f, 0.9f, 1.2f), prepMat);
        prep1.AddComponent<ChoppingStation>();

        GameObject prep2 = CreatePrimitive(PrimitiveType.Cube, "PrepStation_2", new Vector3(-5.5f, 0.45f, -3f), new Vector3(1.2f, 0.9f, 1.8f), prepMat);
        prep2.AddComponent<ChoppingStation>();

        // Lavabo
        Material washMat = CreateMaterial("WashMaterial", new Color(0.39f, 0.59f, 0.79f));
        GameObject lavabo = CreatePrimitive(PrimitiveType.Cube, "WashingStation", new Vector3(-5.5f, 0.45f, 0), new Vector3(1.2f, 0.9f, 1.8f), washMat);
        lavabo.AddComponent<WashingStation>();

        // Dispensador de platos
        GameObject dispenser = CreatePrimitive(PrimitiveType.Cube, "PlateStation", new Vector3(5.5f, 0.45f, 0), new Vector3(1.2f, 0.9f, 1.8f), prepMat);
        dispenser.AddComponent<PlateStation>();

        // Basura
        Material trashMat = CreateMaterial("TrashMaterial", new Color(0.4f, 0.4f, 0.4f));
        GameObject trash = CreatePrimitive(PrimitiveType.Cube, "TrashStation", new Vector3(5.5f, 0.45f, 3f), new Vector3(1.2f, 0.9f, 1.2f), trashMat);
        trash.AddComponent<TrashStation>();
    }

    static void BuildStairs()
    {
        Material stairMat = CreateMaterial("StairMaterial", new Color(0.55f, 0.47f, 0.39f));

        // Escalera principal
        GameObject stairBase = CreatePrimitive(PrimitiveType.Cube, "Stairs_Base", new Vector3(0, 0.25f, -3), new Vector3(2.5f, 0.5f, 3f), stairMat);

        // Escalones
        for (int i = 0; i < 6; i++)
        {
            float stepHeight = 0.5f + (i * 0.5f);
            float stepZ = -3f + (i * 0.3f);
            CreatePrimitive(PrimitiveType.Cube, "Step_" + i, new Vector3(0, stepHeight, stepZ), new Vector3(2.3f, 0.15f, 0.4f), stairMat);
        }
    }

    static void BuildUpperFloor()
    {
        Material upperFloorMat = CreateMaterial("UpperFloorMaterial", new Color(0.47f, 0.39f, 0.31f));
        CreatePrimitive(PrimitiveType.Cube, "Floor_Upper", new Vector3(0, 3f, 0), new Vector3(14, 0.1f, 14), upperFloorMat);

        Material wallMat = CreateMaterial("WallMaterial_Upper", new Color(0.27f, 0.51f, 0.71f));
        float wallHeight = 3f;
        float wallThickness = 0.3f;

        // Muros planta alta
        CreatePrimitive(PrimitiveType.Cube, "Wall_Upper_North", new Vector3(0, 3 + wallHeight / 2, 7), new Vector3(14, wallHeight, wallThickness), wallMat);
        CreatePrimitive(PrimitiveType.Cube, "Wall_Upper_South", new Vector3(0, 3 + wallHeight / 2, -7), new Vector3(14, wallHeight, wallThickness), wallMat);
        CreatePrimitive(PrimitiveType.Cube, "Wall_Upper_East", new Vector3(7, 3 + wallHeight / 2, 0), new Vector3(wallThickness, wallHeight, 14), wallMat);
        CreatePrimitive(PrimitiveType.Cube, "Wall_Upper_West", new Vector3(-7, 3 + wallHeight / 2, 0), new Vector3(wallThickness, wallHeight, 14), wallMat);
    }

    static void BuildFoodCourt()
    {
        Material tableMat = CreateMaterial("TableMaterial", new Color(0.55f, 0.47f, 0.39f));
        float tableY = 3.45f;

        // Mesas
        string[] tableNames = { "Mesa_1", "Mesa_2", "Mesa_3", "Mesa_4", "Mesa_5", "Mesa_6" };
        Vector3[] tablePositions = {
            new Vector3(-3.5f, tableY, 2),
            new Vector3(0, tableY, 2),
            new Vector3(3.5f, tableY, 2),
            new Vector3(-3.5f, tableY, -2),
            new Vector3(0, tableY, -2),
            new Vector3(3.5f, tableY, -2)
        };

        for (int i = 0; i < tableNames.Length; i++)
        {
            GameObject table = CreatePrimitive(PrimitiveType.Cube, tableNames[i], tablePositions[i], new Vector3(2f, 0.9f, 1.5f), tableMat);
            table.AddComponent<FoodCourtTable>();
        }

        // Mostrador de servicio
        Material serveMat = CreateMaterial("ServeMaterial", new Color(0.7f, 0.7f, 0.7f));
        GameObject serving = CreatePrimitive(PrimitiveType.Cube, "ServingWindow", new Vector3(0, 3.55f, 6f), new Vector3(5f, 1.1f, 1.2f), serveMat);
        serving.AddComponent<ServingWindow>();
    }

    static void BuildPlayer()
    {
        Material playerMat = CreateMaterial("PlayerMaterial", new Color(1f, 0.55f, 0f));

        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0, 1, 0);

        // Cuerpo (cilindro)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Body";
        body.transform.SetParent(player.transform);
        body.transform.localPosition = new Vector3(0, 0, 0);
        body.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        body.GetComponent<Renderer>().material = playerMat;

        // Cabeza (esfera)
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(player.transform);
        head.transform.localPosition = new Vector3(0, 1.3f, 0);
        head.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        head.GetComponent<Renderer>().material = playerMat;

        // Mano (para objetos)
        GameObject hand = new GameObject("Hand");
        hand.transform.SetParent(player.transform);
        hand.transform.localPosition = new Vector3(0.6f, 0.5f, 0.5f);

        // Componentes
        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = true;
        rb.mass = 5f;

        CapsuleCollider col = player.AddComponent<CapsuleCollider>();
        col.height = 2.5f;
        col.center = new Vector3(0, 0.5f, 0);

        player.AddComponent<PlayerController>();
    }

    static void SetupCamera()
    {
        Camera mainCam = Camera.main;
        mainCam.transform.position = new Vector3(0, 12, -10);
        mainCam.transform.rotation = Quaternion.Euler(45f, 0, 0);
        mainCam.orthographic = true;
        mainCam.orthographicSize = 9;
        mainCam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
        mainCam.clearFlags = CameraClearFlags.SolidColor;

        CameraFollow follow = mainCam.gameObject.AddComponent<CameraFollow>();
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            follow.target = player.transform;
        }
        follow.offset = new Vector3(0, 12, -10);
        follow.smoothSpeed = 5f;
        follow.useBounds = true;
        follow.minXZ = new Vector2(-10, -10);
        follow.maxXZ = new Vector2(10, 10);
    }

    static void SetupGameManager()
    {
        GameObject gm = new GameObject("GameManager");
        gm.transform.position = Vector3.zero;
        gm.AddComponent<GameManager>();
        gm.AddComponent<OrderSystem>();

        GameObject orderSystem = new GameObject("OrderSystem");
        orderSystem.transform.position = Vector3.zero;
        orderSystem.AddComponent<OrderSystem>();
    }

    static void BuildIngredients()
    {
        // Crear ingredientes en la mesa de preparacion
        Material tomatoMat = CreateMaterial("TomatoMaterial", new Color(1f, 0.2f, 0.1f));
        Material lettuceMat = CreateMaterial("LettuceMaterial", new Color(0.1f, 0.7f, 0.2f));
        Material breadMat = CreateMaterial("BreadMaterial", new Color(1f, 0.85f, 0.4f));

        GameObject tomato = CreatePrimitive(PrimitiveType.Sphere, "Tomate", new Vector3(4f, 1.1f, -5.5f), new Vector3(0.4f, 0.4f, 0.4f), tomatoMat);
        IngredientPickup tp = tomato.AddComponent<IngredientPickup>();
        tp.ingredientName = "Tomate";
        tp.ingredientType = IngredientPickup.IngredientType.Tomato;
        Ingredient ti = tomato.AddComponent<Ingredient>();
        ti.ingredientName = "Tomate";
        ti.ingredientType = Ingredient.IngredientType.Tomato;

        GameObject lettuce = CreatePrimitive(PrimitiveType.Sphere, "Lechuga", new Vector3(4.8f, 1.1f, -5.5f), new Vector3(0.45f, 0.35f, 0.45f), lettuceMat);
        IngredientPickup lp = lettuce.AddComponent<IngredientPickup>();
        lp.ingredientName = "Lechuga";
        lp.ingredientType = IngredientPickup.IngredientType.Lettuce;
        Ingredient li = lettuce.AddComponent<Ingredient>();
        li.ingredientName = "Lechuga";
        li.ingredientType = Ingredient.IngredientType.Lettuce;

        GameObject bread = CreatePrimitive(PrimitiveType.Cube, "Pan", new Vector3(-5.5f, 1.1f, -3f), new Vector3(0.5f, 0.3f, 0.5f), breadMat);
        IngredientPickup bp = bread.AddComponent<IngredientPickup>();
        bp.ingredientName = "Pan";
        bp.ingredientType = IngredientPickup.IngredientType.Bread;
        Ingredient bi = bread.AddComponent<Ingredient>();
        bi.ingredientName = "Pan";
        bi.ingredientType = Ingredient.IngredientType.Bread;
    }
}
