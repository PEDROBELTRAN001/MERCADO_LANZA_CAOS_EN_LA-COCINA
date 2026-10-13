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

        BuildContents();
        EditorUtility.DisplayDialog("Listo", "Mercado y cocina de Doña Rosa construidos. Guarda la escena para conservarlos.", "OK");
    }

    public static void BuildContents()
    {
        // Limpiar escena anterior
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        System.Collections.Generic.List<GameObject> toDestroy = new System.Collections.Generic.List<GameObject>();
        string[] prefixes = { "Wall_", "Floor_", "Counter_", "Stove", "Oven", "Prep", "Washing", "Plate", "Trash", "Stair", "Step_", "Mesa", "Serving", "Player", "GameManager", "OrderSystem", "SimpleInput", "Tomate", "Lechuga", "Pan", "Railing", "Prompt_", "Plato_", "Api_", "Insumo_", "Rotulo_" };

        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;
            // Las copias hechas a mano ("Wall_K_W (3)", "Floor_Kitchen (1)") son parte del mapa del
            // mercado de Pedro (paredes del perimetro y piso grande); el builder nunca crea nombres asi
            if (obj.name.Contains(" (")) continue;
            foreach (string prefix in prefixes)
            {
                if (obj.name.StartsWith(prefix))
                {
                    toDestroy.Add(obj);
                    break;
                }
            }
        }
        foreach (GameObject obj in toDestroy)
        {
            if (obj != null) DestroyImmediate(obj);
        }

        BuildKitchenFloor();
        BuildKitchenWalls();
        BuildKitchenCounters();
        BuildCookingStations();
        BuildFoodCourtFloor();
        BuildFoodCourtRailings();
        BuildFoodCourtTables();
        DishMaterialSetup.SetupAll();
        BuildFoodCourtDishes();
        BuildPlayer();
        SetupCamera();
        SetupGameManager();
        BuildIngredients();
        BuildRecipes();

        DonaRosaBuilder.Integrate();
        BuildStationLabels();
    }

    static Material CreateMaterial(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        // Buscar shaders disponibles
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = name;
        mat.color = color;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Material CreateURPMaterial(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.name = name;
        mat.color = color;
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
        // Pared este partida en dos: deja una puerta hacia el comedor entre z = -3.9 y z = -1.3
        // (libre de PlateStation y de la mesa de la sarten de Doña Rosa)
        CreatePrimitive(PrimitiveType.Cube, "Wall_K_E_Norte", new Vector3(-3, h/2, 2.35f), new Vector3(t, h, 7.3f), wallMat);
        CreatePrimitive(PrimitiveType.Cube, "Wall_K_E_Sur", new Vector3(-3, h/2, -4.95f), new Vector3(t, h, 2.1f), wallMat);
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
        p1.AddComponent<ChoppingStation>().displayName = "Tabla de picar";

        // Lavabo
        GameObject w = CreatePrimitive(PrimitiveType.Cube, "WashingStation", new Vector3(-14.3f, 0.45f, 0), new Vector3(1.2f, 0.9f, 1.8f), washMat);
        WashingStation washing = w.AddComponent<WashingStation>();
        washing.displayName = "Lavaplatos";
        washing.interactionPrompt = "E · Tomar plato limpio";
        BuildSinkDecor(w.transform.position);

        // Mesa de armado: los ingredientes listos se juntan aqui y se convierten en el plato del pedido
        GameObject d = CreatePrimitive(PrimitiveType.Cube, "Mesa_Armado", new Vector3(-3.7f, 0.45f, 0), new Vector3(1.2f, 0.9f, 1.8f), prepMat);
        AssemblyStation assembly = d.AddComponent<AssemblyStation>();
        assembly.displayName = "Mesa de armado";
        assembly.interactionPrompt = "E · Dejar ingrediente / recoger plato";

        // Basura
        GameObject t = CreatePrimitive(PrimitiveType.Cube, "TrashStation", new Vector3(-3.7f, 0.45f, 3f), new Vector3(1.2f, 0.9f, 1.2f), trashMat);
        TrashStation trash = t.AddComponent<TrashStation>();
        trash.displayName = "Basura";
        trash.interactionPrompt = "E · Tirar lo que llevas";
    }

    // Aspecto de lavaplatos sobre la estacion (antes era un cubo igual al mostrador y no se distinguia):
    // pileta de acero con agua, grifo junto a la pared y una pila de platos limpios.
    // Nombres con prefijo "Washing" para que la limpieza del SceneBuilder los borre al reconstruir.
    static void BuildSinkDecor(Vector3 station)
    {
        Material steel = CreateURPMaterial("LavaplatosAceroMaterial", new Color(0.72f, 0.76f, 0.8f));
        Material water = CreateURPMaterial("LavaplatosAguaMaterial", new Color(0.35f, 0.55f, 0.7f));
        Material plateMat = CreateURPMaterial("LavaplatosLozaMaterial", new Color(0.95f, 0.95f, 0.97f));
        float top = station.y + 0.45f;

        Decor(PrimitiveType.Cube, "Washing_Pileta", station + new Vector3(0f, 0.48f, -0.35f), new Vector3(1.0f, 0.08f, 1.0f), steel);
        Decor(PrimitiveType.Cube, "Washing_Agua", station + new Vector3(0.05f, 0.525f, -0.35f), new Vector3(0.75f, 0.02f, 0.8f), water);
        Decor(PrimitiveType.Cylinder, "Washing_Grifo", new Vector3(station.x - 0.45f, top + 0.25f, station.z - 0.35f), new Vector3(0.07f, 0.25f, 0.07f), steel);
        Decor(PrimitiveType.Cube, "Washing_Caño", new Vector3(station.x - 0.3f, top + 0.48f, station.z - 0.35f), new Vector3(0.3f, 0.05f, 0.05f), steel);

        for (int i = 0; i < 4; i++)
            Decor(PrimitiveType.Cylinder, "Washing_Plato_" + i, new Vector3(station.x, top + 0.02f + i * 0.04f, station.z + 0.5f), new Vector3(0.5f, 0.015f, 0.5f), plateMat);
    }

    static void Decor(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material mat)
    {
        GameObject obj = CreatePrimitive(type, name, position, scale, mat);
        DestroyImmediate(obj.GetComponent<Collider>()); // la interaccion la da el colisionador de la estacion
    }

    // ==================== PATIO DE COMIDAS (al lado de la cocina, mismo nivel) ====================
    // Ocupa x = -3..9, z = -6..6, pegado a la pared este de la cocina. Sin paredes, como en un mercado;
    // se entra caminando por la puerta de Wall_K_E. Ya no hay escalera ni teletransporte.
    static void BuildFoodCourtFloor()
    {
        // URP/Lit para que el piso reciba sombras (UpperFloorMaterial es Unlit)
        Material floorMat = CreateURPMaterial("ComedorPisoMaterial", new Color(0.82f, 0.72f, 0.55f));
        CreatePrimitive(PrimitiveType.Cube, "Floor_FoodCourt", new Vector3(3, 0, 0), new Vector3(12, 0.1f, 12), floorMat);
    }

    static void BuildFoodCourtRailings()
    {
        // El piso del mercado esta ~1.4 m mas abajo: baranda baja en los bordes abiertos para no caerse
        Material railMat = CreateMaterial("RailMaterial", new Color(0.45f, 0.37f, 0.29f));
        float h = 0.8f;
        float t = 0.15f;
        CreatePrimitive(PrimitiveType.Cube, "Railing_FC_N", new Vector3(3, h/2, 6), new Vector3(12, h, t), railMat);
        CreatePrimitive(PrimitiveType.Cube, "Railing_FC_S", new Vector3(3, h/2, -6), new Vector3(12, h, t), railMat);
        CreatePrimitive(PrimitiveType.Cube, "Railing_FC_E", new Vector3(9, h/2, 0), new Vector3(t, h, 12), railMat);
    }

    static void BuildFoodCourtTables()
    {
        Material tableMat = CreateMaterial("TableMaterial", new Color(0.55f, 0.47f, 0.39f));
        float y = 0.45f;

        // 6 mesas en cuadricula; la fila sur deja libre el paso que viene de la puerta (z ~ -2.6)
        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                float x = 1.5f + col * 3f;
                float z = -1f + row * 3.5f;
                GameObject table = CreatePrimitive(PrimitiveType.Cube, "Mesa_" + (row*3+col+1), new Vector3(x, y, z), new Vector3(2f, 0.9f, 1.5f), tableMat);
                table.AddComponent<FoodCourtTable>();
            }
        }

        // Mostrador de servicio
        Material serveMat = CreateMaterial("ServeMaterial", new Color(0.7f, 0.7f, 0.7f));
        GameObject serving = CreatePrimitive(PrimitiveType.Cube, "ServingWindow", new Vector3(3, 0.55f, 5.3f), new Vector3(5f, 1.1f, 1.2f), serveMat);
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

        player.AddComponent<PlayerController>().interactableLayer = Physics.DefaultRaycastLayers;

        // Tag
        player.tag = "Player";
    }

    // ==================== CAMARA ====================
    static void SetupCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            // La escena se quedo sin camara: crear una en vez de fallar con NullReferenceException
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            mainCam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        mainCam.transform.position = new Vector3(0, 14, -12);
        mainCam.transform.rotation = Quaternion.Euler(50f, 0, 0);
        mainCam.orthographic = true;
        mainCam.orthographicSize = 12;
        mainCam.backgroundColor = new Color(0.12f, 0.12f, 0.18f);
        mainCam.clearFlags = CameraClearFlags.SolidColor;

        CameraFollow follow = mainCam.GetComponent<CameraFollow>();
        if (follow == null) follow = mainCam.gameObject.AddComponent<CameraFollow>();
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
        gm.AddComponent<GameHUD>();

        GameObject os = new GameObject("OrderSystem");
        os.transform.position = Vector3.zero;
        os.AddComponent<OrderSystem>();

        GameObject input = new GameObject("SimpleInput");
        input.AddComponent<SimpleInput>();
    }

    // ==================== INGREDIENTES ====================
    // Cajas de insumos infinitas, en fila frente al estante de Doña Rosa (z = 4.2, x = -13 .. -8.6):
    // libre de la barra de pedidos (x > -8.2), del mostrador oeste y de las cajas/sacos de la esquina.
    static void BuildIngredients()
    {
        Material crateMat = CreateMaterial("CounterMaterial", new Color(0.63f, 0.39f, 0.2f));

        BuildCrate(0, "Papa", true, Ingredient.IngredientType.Potato, new Color(0.76f, 0.60f, 0.42f), PrimitiveType.Sphere, crateMat);
        BuildCrate(1, "Choclo", false, Ingredient.IngredientType.Corn, new Color(0.98f, 0.84f, 0.42f), PrimitiveType.Capsule, crateMat);
        BuildCrate(2, "Haba", true, Ingredient.IngredientType.Bean, new Color(0.42f, 0.62f, 0.20f), PrimitiveType.Sphere, crateMat);
        BuildCrate(3, "Queso", false, Ingredient.IngredientType.Cheese, new Color(1f, 0.95f, 0.70f), PrimitiveType.Cube, crateMat);
        BuildCrate(4, "Pan", false, Ingredient.IngredientType.Bread, new Color(0.90f, 0.70f, 0.40f), PrimitiveType.Sphere, crateMat);
        BuildCrate(5, "Cerdo", false, Ingredient.IngredientType.Meat, new Color(0.75f, 0.40f, 0.35f), PrimitiveType.Cube, crateMat);
        BuildCrate(6, "Maíz morado", false, Ingredient.IngredientType.PurpleCorn, new Color(0.40f, 0.15f, 0.45f), PrimitiveType.Capsule, crateMat);
        BuildCrate(7, "Masa", true, Ingredient.IngredientType.Dough, new Color(0.95f, 0.90f, 0.80f), PrimitiveType.Sphere, crateMat);
        BuildCrate(8, "Fruta", true, Ingredient.IngredientType.Fruit, new Color(0.90f, 0.25f, 0.30f), PrimitiveType.Sphere, crateMat);
    }

    static void BuildCrate(int index, string ingredient, bool femenino, Ingredient.IngredientType type, Color color, PrimitiveType shape, Material crateMat)
    {
        // Raiz sin escala (los hijos no se deforman) con el colisionador y el script.
        // Prefijo "Insumo_" (no "Caja_": el estante de Doña Rosa ya tiene piezas "Caja_PAPA"...)
        GameObject crate = new GameObject("Insumo_" + ingredient.Replace(" ", "_"));
        crate.transform.position = new Vector3(-13f + index * 0.55f, 0f, 4.2f);
        BoxCollider col = crate.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 0.3f, 0);
        col.size = new Vector3(0.5f, 0.6f, 0.5f);

        IngredientCrate supply = crate.AddComponent<IngredientCrate>();
        supply.ingredientName = ingredient;
        supply.femenino = femenino;
        supply.ingredientType = type;
        supply.color = color;
        supply.shape = shape;
        supply.size = shape == PrimitiveType.Capsule ? new Vector3(0.15f, 0.15f, 0.15f) : new Vector3(0.25f, 0.25f, 0.25f);
        supply.interactionPrompt = "E · Sacar " + ingredient.ToLower();
        supply.displayName = "Caja de " + ingredient.ToLower();

        // Caja de madera
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(box.GetComponent<Collider>());
        box.name = "Caja";
        box.transform.SetParent(crate.transform, false);
        box.transform.localPosition = new Vector3(0, 0.25f, 0);
        box.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        box.GetComponent<Renderer>().sharedMaterial = crateMat;

        // Muestra del ingrediente encima (color del insumo)
        GameObject sample = GameObject.CreatePrimitive(shape);
        Object.DestroyImmediate(sample.GetComponent<Collider>());
        sample.name = "Muestra";
        sample.transform.SetParent(crate.transform, false);
        sample.transform.localPosition = new Vector3(0, 0.6f, 0);
        sample.transform.localScale = supply.size * 1.2f;
        sample.GetComponent<Renderer>().sharedMaterial = CreateURPMaterial("Insumo_" + type, color);

        // Nombre legible desde la camara de arriba
        GameObject label = new GameObject("Rotulo");
        label.transform.SetParent(crate.transform, false);
        label.transform.localPosition = new Vector3(0, 0.95f, -0.1f);
        label.transform.localRotation = Quaternion.Euler(50f, 0f, 0f);
        TextMesh text = label.AddComponent<TextMesh>();
        text.text = ingredient.ToUpper().Replace(" ", "\n");
        text.fontSize = 48;
        text.characterSize = 0.02f;
        text.anchor = TextAnchor.MiddleCenter;
        text.color = LabelColor;
    }

    // ==================== ROTULOS DE ESTACIONES ====================
    // Texto oscuro (el piso de la cocina es blanco), inclinado hacia la camara de arriba
    static readonly Color LabelColor = new Color(0.08f, 0.2f, 0.16f);

    // Se llama despues de DonaRosaBuilder.Integrate, que mueve la tabla de picar y arma las hornallas
    static void BuildStationLabels()
    {
        // Las estaciones pegadas a una pared llevan el rotulo corrido hacia el centro de la cocina
        AddLabel("Prep_1", "TABLA DE PICAR", Vector3.zero);
        AddLabel("Mesa_Armado", "MESA DE ARMADO", new Vector3(-1.4f, 0f, 0f));
        AddLabel("TrashStation", "BASURA", new Vector3(-0.9f, 0f, -0.9f));
        AddLabel("WashingStation", "LAVAPLATOS\n(platos limpios)", new Vector3(1.6f, 0f, 0f));

        GameObject cocina = GameObject.Find("COCINA_DOÑA_ROSA");
        if (cocina != null)
        {
            Transform stove = cocina.transform.Find("01_Fogon_Industrial");
            if (stove != null) AddLabel(stove.gameObject, "HORNALLAS (cocinar)", Vector3.zero);
        }
    }

    static void AddLabel(string objectName, string text, Vector3 offset)
    {
        GameObject target = GameObject.Find(objectName);
        if (target != null) AddLabel(target, text, offset);
    }

    static void AddLabel(GameObject target, string text, Vector3 offset)
    {
        // Encima de lo que se ve del objeto; raiz propia para no heredar la escala del cubo
        Bounds bounds = new Bounds(target.transform.position, Vector3.zero);
        foreach (Renderer r in target.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);

        GameObject label = new GameObject("Rotulo_" + target.name);
        label.transform.position = new Vector3(bounds.center.x, bounds.max.y + 0.5f, bounds.center.z) + offset;
        label.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
        TextMesh mesh = label.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.fontSize = 64;
        mesh.characterSize = 0.04f;
        mesh.fontStyle = FontStyle.Bold;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.color = LabelColor;
    }

    // ==================== RECETAS ====================
    static void BuildRecipes()
    {
        string folder = "Assets/Resources/Recipes";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources", "Recipes");

        // Recetas de prueba anteriores (no son del mercado paceño): se eliminan
        foreach (string old in new[] { "Hamburguesa", "Ensalada", "Sandwich" })
            AssetDatabase.DeleteAsset(folder + "/" + old + ".asset");

        // Platos del juego, con el modelo de Blender de cada uno.
        // RecipeData.CheckRecipe exige picar/cocinar TODOS los ingredientes de la receta.
        CreateRecipe(folder, "PlatoPaceno", "Plato Paceño", "plato_pacenio.fbx", new Ingredient.IngredientType[] {
            Ingredient.IngredientType.Potato, Ingredient.IngredientType.Corn,
            Ingredient.IngredientType.Bean, Ingredient.IngredientType.Cheese
        }, false, true, 40);

        // Simplificado: el cerdo ya viene horneado, solo se arma con el pan
        CreateRecipe(folder, "SandwichDeChola", "Sándwich de Chola", "SandwichDeChola.fbx", new Ingredient.IngredientType[] {
            Ingredient.IngredientType.Bread, Ingredient.IngredientType.Meat
        }, false, false, 25);

        CreateRecipe(folder, "ApiConPastel", "Api con Pastel", "ApiConPastel.fbx", new Ingredient.IngredientType[] {
            Ingredient.IngredientType.PurpleCorn, Ingredient.IngredientType.Dough
        }, false, true, 30);

        CreateRecipe(folder, "EnsaladaDeFrutas", "Ensalada de Frutas", "EnsaladaDeFrutas.fbx", new Ingredient.IngredientType[] {
            Ingredient.IngredientType.Fruit
        }, true, false, 20);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Recetas creadas en " + folder);
    }

    static void CreateRecipe(string folder, string fileName, string displayName, string dishFbx,
        Ingredient.IngredientType[] ingredients, bool chop, bool cook, int points)
    {
        string path = folder + "/" + fileName + ".asset";
        RecipeData recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
        bool isNew = recipe == null;
        if (isNew) recipe = ScriptableObject.CreateInstance<RecipeData>();

        // Si ya existe se actualiza, para que los cambios de diseño se apliquen al reconstruir
        recipe.recipeName = displayName;
        recipe.dishPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + dishFbx);
        recipe.requiredIngredients = ingredients;
        recipe.requiresChopping = chop;
        recipe.requiresCooking = cook;
        recipe.basePoints = points;

        if (isNew) AssetDatabase.CreateAsset(recipe, path);
        else EditorUtility.SetDirty(recipe);
    }

    // ==================== PLATOS - COMIDA (modelos de Blender exportados como FBX) ====================
    // Todos van sobre mostradores de la cocina; la superficie de los mostradores esta en y = 0.9
    // Dos platos por mostrador del lado oeste (los del norte los reemplaza la cocina de Doña Rosa).
    // Cada mostrador mide 2.5 de largo en Z; los platos (~0.65) van a +-0.6 del centro.
    static void BuildFoodCourtDishes()
    {
        // Counter_W_2 (oeste, arriba, z = 3)
        PlaceDish("EnsaladaDeFrutas.fbx", "Plato_EnsaladaDeFrutas_1", new Vector3(-14.3f, 0.92f, 3.6f));
        PlaceDish("ApiConPastel.fbx", "Plato_ApiConPastel_1", new Vector3(-14.3f, 0.92f, 2.4f));
        // Counter_W_0 (oeste, abajo, z = -3) - Plato Paceño: Ficha FCH-GAS-006
        // Counter_W_1 (al medio) queda libre: ahi esta el lavaplatos (WashingStation)
        PlaceDish("plato_pacenio.fbx", "Plato_PlatoPaceno_1", new Vector3(-14.3f, 0.92f, -2.4f));
        PlaceDish("SandwichDeChola.fbx", "Plato_SandwichDeChola_1", new Vector3(-14.3f, 0.92f, -3.6f));
    }

    static void PlaceDish(string fbxName, string objectName, Vector3 position)
    {
        string path = "Assets/Prefabs/" + fbxName;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning("No se encontro " + path + " - no se pudo colocar " + objectName + ".");
            return;
        }

        GameObject dish = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        dish.name = objectName;
        dish.transform.position = position;
    }
}
