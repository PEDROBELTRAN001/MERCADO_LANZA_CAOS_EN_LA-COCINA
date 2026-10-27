#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.AI;

// Verificación reproducible: Unity -batchmode -projectPath ... -executeMethod LocalSabotageValidation.Run
public static class LocalSabotageValidation
{
    private static IEnumerator checks;
    private static Stack<IEnumerator> steps;
    private static double deadline;
    private static int passed;
    [MenuItem("Mercado/Probar multijugador y sabotaje")]
    public static void RunInEditor()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
        { Debug.LogWarning("Guarda los cambios de la escena antes de ejecutar la prueba."); return; }
        Run();
    }
    public static void Run()
    {
        passed = 0;
        SessionState.SetBool("LocalSabotageValidation", true);
        EditorSceneManager.OpenScene("Assets/Scenes/KitchenScene.unity");
        EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    private static void Resume()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("LocalSabotageValidation", false))
            {
                checks = CheckGame();
                steps = new Stack<IEnumerator>();
                steps.Push(checks);
                deadline = EditorApplication.timeSinceStartup + 180;
                EditorApplication.update += Tick;
            }
        };
    }
    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Tiempo de prueba agotado.");
            while (steps.Count > 0)
            {
                IEnumerator step = steps.Peek();
                if (!step.MoveNext()) { steps.Pop(); continue; }
                if (step.Current is IEnumerator child) steps.Push(child);
                return;
            }
            Finish(0, "PASS: " + passed + " comprobaciones del multijugador, sabotaje e investigación.");
        }
        catch (Exception error) { Finish(1, "FAIL: " + error); }
    }
    private static void Finish(int code, string result)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool("LocalSabotageValidation", false);
        Debug.Log(result);
        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Logs"));
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Logs/local-sabotage-result.txt"), result);
        if (Application.isBatchMode) EditorApplication.Exit(code);
        else EditorApplication.isPlaying = false;
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        passed++;
        Debug.Log("PASS: " + message);
    }
    private static GameObject Food(PlayerController player, string name = "Papa de prueba")
    {
        var food = new GameObject(name);
        food.AddComponent<Ingredient>();
        Check(player.TryHoldItem(food), "Recoge " + name);
        return food;
    }
    private static void Empty(PlayerController player)
    {
        var held = player.ReleaseHeldItem();
        if (held != null) UnityEngine.Object.Destroy(held);
    }
    private static IEnumerator CheckGame()
    {
        // Start del juego y del bootstrap se ejecutan antes de evaluar la escena.
        float waitUntil = Time.realtimeSinceStartup + 1f;
        while (Time.realtimeSinceStartup < waitUntil) yield return null;
        var local = LocalMultiplayer.Instance;
        Check(local != null && local.Worker != null && local.Rival != null, "Dos personajes instalados en KitchenScene");
        if (GameManager.Instance.CurrentState == GameManager.GameState.Ready) local.ConfigureMode(LocalMultiplayer.GameMode.LocalMultiplayer);
        var worker = local.Worker;
        var rival = local.Rival;
        var attack = rival.GetComponent<TackleSabotage>();
        Check(UnityEngine.Object.FindObjectsByType<PlayerController>().Length == 2, "No duplica jugadores");
        Check(worker.viewCamera.rect.width == 0.5f && rival.viewCamera.rect.x == 0.5f, "Dos vistas de cámara independientes");
        Check(worker.Hand.gameObject.activeSelf && rival.Hand.gameObject.activeSelf, "Comida visible en ambas manos");
        Check(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Player/Model/Camarero_color.fbx") != null,
            "El modelo real del garzón se importa correctamente");
        foreach (string model in new[] { "ApiConPastel", "EnsaladaDeFrutas", "plato_pacenio", "SandwichDeChola" })
            Check(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + model + ".fbx") != null,
                "Se importa el modelo de comida " + model);
        var keyboard = InputSystem.AddDevice<Keyboard>();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.L));
        InputSystem.Update();
        Check(PlayerController.ReadMovement(PlayerController.LocalControls.WASD, keyboard) == Vector2.up, "W mueve solo al trabajador");
        Check(PlayerController.ReadMovement(PlayerController.LocalControls.IJKL, keyboard) == Vector2.right, "L mueve solo al Rival simultáneamente");
        InputSystem.RemoveDevice(keyboard);
        FloatingWindows windows = UnityEngine.Object.FindAnyObjectByType<FloatingWindows>();
        if (windows != null)
        {
            windows.showIntro = false;
            typeof(FloatingWindows).GetMethod("Close", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(windows, null);
            Check(!windows.simulateSabotage, "Desactiva avisos de sabotaje simulado");
        }
        Time.timeScale = 1f;
        GameManager gm = GameManager.Instance;
        gm.SetState(GameManager.GameState.Playing);
        yield return null;
        gm.AddScore(17);
        float remaining = gm.GetTimeRemaining();
        gm.TogglePause();
        gm.TogglePause();
        Check(gm.GetCurrentScore() == 17 && Mathf.Abs(remaining - gm.GetTimeRemaining()) < 0.1f, "Pausar conserva puntos y tiempo");
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Suelo de pruebas aislado";
        ground.transform.position = new Vector3(1000f, 0.75f, 1000f);
        ground.transform.localScale = new Vector3(20f, 1f, 20f);
        Vector3 position = new Vector3(1000f, 2f, 1000f);
        worker.ResetForRound(position, Quaternion.identity);
        rival.ResetForRound(position + Vector3.back * 2.1f, Quaternion.identity);
        Physics.SyncTransforms();
        GameObject first = Food(worker);
        var extra = new GameObject("Segundo ingrediente");
        Check(!worker.TryHoldItem(extra), "No permite dos objetos en la mano");
        UnityEngine.Object.Destroy(extra);
        GameObject rivalFood = Food(rival, "Comida del Rival");
        Check(worker.HeldItem != rival.HeldItem, "Inventarios independientes");
        Empty(rival);
        // No puede iniciar el tacle a través de un muro.
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = position + Vector3.back;
        wall.transform.localScale = new Vector3(4f, 3f, 0.2f);
        Physics.SyncTransforms();
        Check(!attack.TryBeginAttack(), "Un muro impide iniciar el ataque");
        Check(!rival.GetComponent<CharacterAppearance>().ActiveRig.SabotageMarked, "Un intento bloqueado conserva el mandil limpio");
        UnityEngine.Object.Destroy(wall);
        yield return null;
        Physics.SyncTransforms();
        attack.warningSeconds = 0.15f;
        attack.cooldownSeconds = 0.6f;
        Check(attack.TryBeginAttack(), "Inicia tacle contra ingrediente transportado");
        Check(worker.HeldItem == first, "La advertencia no elimina la comida");
        Check(!rival.GetComponent<CharacterAppearance>().ActiveRig.SabotageMarked, "La advertencia todavía no mancha al Rival");
        Check(!attack.TryBeginAttack(), "No permite encadenar ataques durante la preparación");
        waitUntil = Time.time + 0.65f;
        while (Time.time < waitUntil) yield return null;
        Check(worker.HeldItem == null && first == null, "Un impacto físico elimina el ingrediente correcto");
        Check(rival.GetComponent<CharacterAppearance>().ActiveRig.SabotageMarked, "El impacto exitoso deja manchas visibles en el Rival");
        Check(worker.IsProtected, "El golpe concede protección temporal");
        Check(gm.OrdersLost == 0, "Perder comida no cuenta como pedido perdido");
        var evidence = UnityEngine.Object.FindAnyObjectByType<SabotageEvidence>();
        Check(evidence != null && evidence.attacker == rival && evidence.victim == worker, "El golpe deja un rastro vinculado a sus participantes");
        waitUntil = Time.time + 1f;
        while (Time.time < waitUntil) yield return null;
        worker.Teleport(evidence.transform.position + Vector3.up * 0.75f + Vector3.back * 0.5f);
        Physics.SyncTransforms();
        evidence.Interact(worker);
        Check(InvestigationSystem.Instance.Ledger.AvailableProof(worker.InvestigationId, rival.InvestigationId) != null, "Examinar registra una prueba para denunciar");

        // Cambio de objeto durante la advertencia: el nuevo objeto debe sobrevivir.
        worker.ResetForRound(position, Quaternion.identity);
        rival.ResetForRound(position + Vector3.back * 2.1f, Quaternion.identity);
        attack.ResetForRound();
        Physics.SyncTransforms();
        Check(!rival.GetComponent<CharacterAppearance>().ActiveRig.SabotageMarked, "La nueva ronda limpia el mandil del Rival");
        GameObject oldItem = Food(worker, "Ingrediente anterior");
        Check(attack.TryBeginAttack(), "Prepara intento para comprobar cambio de objeto");
        Empty(worker);
        GameObject replacement = Food(worker, "Ingrediente nuevo");
        waitUntil = Time.time + 0.5f;
        while (Time.time < waitUntil) yield return null;
        Check(worker.HeldItem == replacement, "No elimina un objeto recogido después del aviso");

        // Dejar y volver a tomar el mismo objeto también invalida el ataque anterior.
        worker.ResetForRound(position, Quaternion.identity);
        rival.ResetForRound(position + Vector3.back * 2.1f, Quaternion.identity);
        attack.ResetForRound();
        Physics.SyncTransforms();
        GameObject same = Food(worker);
        Check(attack.TryBeginAttack(), "Prepara intento sobre objeto que se volverá a recoger");
        worker.ReleaseHeldItem();
        worker.TryHoldItem(same);
        waitUntil = Time.time + 0.5f;
        while (Time.time < waitUntil) yield return null;
        Check(worker.HeldItem == same, "Una recogida nueva invalida el ataque aunque sea el mismo objeto");

        // Escape durante el aviso y plato terminado.
        worker.ResetForRound(position, Quaternion.identity);
        rival.ResetForRound(position + Vector3.back * 2.1f, Quaternion.identity);
        attack.ResetForRound();
        var dish = new GameObject("Plato terminado de prueba");
        dish.AddComponent<DishItem>();
        worker.TryHoldItem(dish);
        Physics.SyncTransforms();
        Check(attack.TryBeginAttack(), "También puede apuntar a un plato terminado");
        worker.Teleport(position + Vector3.right * 8f);
        waitUntil = Time.time + 0.5f;
        while (Time.time < waitUntil) yield return null;
        Check(worker.HeldItem == dish, "Alejarse permite conservar el plato");

        gm.RestartGame();
        Check(worker.HeldItem == null && rival.HeldItem == null && attack.CooldownRemaining == 0f,
            "Reiniciar limpia manos, protección y espera del tacle");
        yield return null;
        Check(UnityEngine.Object.FindObjectsByType<SabotageEvidence>().Length == 0, "Reiniciar elimina rastros anteriores");
        yield return CheckKitchen();
        yield return CheckInvestigation();
    }

    // La prueba policial usa las rutas de la escena real.
    private static IEnumerator CheckInvestigation()
    {
        var local = LocalMultiplayer.Instance;
        var system = InvestigationSystem.Instance;
        var gm = GameManager.Instance;
        Check(system != null && system.Navigation != null && system.Navigation.Ready && system.Police != null,
            "El Capibara aparece y la navegación del mercado está disponible");
        var police = system.Police;
        var agent = police.GetComponent<NavMeshAgent>();
        Check(agent != null && agent.isOnNavMesh, "El Capibara se encuentra sobre una ruta transitable");
        system.reportCooldown = 0.1f;
        system.exileSeconds = 0.6f;
        police.patrolRadius = 0f;
        police.patrolSpeed = 0f;
        police.ResetForRound();
        if (GameManager.Instance.CurrentState == GameManager.GameState.Ready) local.ConfigureMode(LocalMultiplayer.GameMode.LocalMultiplayer);
        var worker = local.Worker;
        var rival = local.Rival;
        Vector3 policeHome = police.transform.position;
        Vector3 reportingPlace = policeHome;
        Vector3 rivalPlace = policeHome;
        bool found = false;
        for (int i = 0; i < 16 && !found; i++)
        {
            float angle = i * Mathf.PI / 8f;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            if (system.Navigation.CanReach(policeHome, policeHome - direction * 1.5f, out reportingPlace) &&
                system.Navigation.CanReach(policeHome, policeHome + direction * 3.5f, out rivalPlace) &&
                Vector3.Distance(rivalPlace, policeHome) > 2f)
            {
                worker.ResetForRound(reportingPlace + Vector3.up * 0.75f, Quaternion.identity);
                rival.ResetForRound(rivalPlace + Vector3.up * 0.75f, Quaternion.identity);
                Physics.SyncTransforms();
                found = worker.CanInspect(police) && police.CanPursue(rival);
            }
        }
        Check(found, "Existen posiciones conectadas para denunciar y perseguir en el mercado real");
        float before = gm.GetTimeRemaining();
        Check(!system.Report(worker), "Rechaza una denuncia sin prueba examinada");
        Check(Mathf.Abs((before - gm.GetTimeRemaining()) - system.falseReportPenalty) < 0.2f,
            "La denuncia sin pruebas descuenta exactamente cinco segundos");
        before = gm.GetTimeRemaining();
        system.Report(worker);
        Check(Mathf.Abs(before - gm.GetTimeRemaining()) < 0.1f, "Repetir la denuncia inmediatamente no acumula penalizaciones");
        float until = Time.time + 0.15f;
        while (Time.time < until) yield return null;
        for (int number = 1; number <= 3; number++)
        {
            police.ResetForRound();
            worker.ResetForRound(reportingPlace + Vector3.up * 0.75f, Quaternion.identity);
            rival.ResetForRound(rivalPlace + Vector3.up * 0.75f, Quaternion.identity);
            Physics.SyncTransforms();
            // El golpe ya se verificó con física antes; aquí se comprueba el ciclo policial.
            SabotageEvidence.Create(rival, worker, "Plato de prueba policial", worker.transform.position);
            var incident = system.Ledger.Incidents[system.Ledger.Incidents.Count - 1];
            Check(system.Ledger.Examine(incident.Id, worker.InvestigationId), "Investiga un incidente nuevo para la expulsión " + number);
            Check(system.Report(worker) && police.IsChasing, "La prueba inicia la persecución " + number);
            Check(!system.Arrest(rival, incident.Id), "No expulsa al sospechoso antes de alcanzarlo");
            until = Time.time + 8f;
            while (!rival.IsDetained && Time.time < until) yield return null;
            Check(rival.IsDetained && system.Ledger.ExpulsionsOf(rival.InvestigationId) == number,
                "El Capibara alcanza físicamente al Rival y registra la expulsión " + number);
            Check(!rival.GetComponent<TackleSabotage>().TryBeginAttack(), "El Rival detenido no puede atacar");
            Check(system.Ledger.AvailableProof(worker.InvestigationId, rival.InvestigationId) == null,
                "La captura invalida la prueba utilizada");
            if (number < 3)
            {
                Check(gm.CurrentState == GameManager.GameState.Playing && !gm.WonByExpulsions,
                    "Las primeras expulsiones permiten continuar el turno");
                gm.TogglePause();
                float seconds = system.ReturnSeconds;
                float realtimeUntil = Time.realtimeSinceStartup + 0.2f;
                while (Time.realtimeSinceStartup < realtimeUntil) yield return null;
                Check(rival.IsDetained && Mathf.Abs(seconds - system.ReturnSeconds) < 0.05f, "La pausa congela la expulsión temporal");
                gm.TogglePause();
                until = Time.time + 2f;
                while (rival.IsDetained && Time.time < until) yield return null;
                Check(!rival.IsDetained, "El Rival regresa después de la expulsión temporal");
                Check(rival.GetComponent<TackleSabotage>().CooldownRemaining > 0f, "El Rival regresa con el ataque en espera");
            }
        }
        Check(gm.CurrentState == GameManager.GameState.GameOver && gm.WonByExpulsions,
            "La tercera expulsión termina el nivel con victoria de los trabajadores");
        gm.RestartGame();
        Check(!rival.IsDetained && !gm.WonByExpulsions && system.Ledger.ExpulsionsOf(rival.InvestigationId) == 0,
            "Reiniciar después de ganar recupera al Rival y limpia las expulsiones");
    }

    private static IEnumerator CheckKitchen()
    {
        var worker = LocalMultiplayer.Instance.Worker;
        var gm = GameManager.Instance;
        var cooking = UnityEngine.Object.FindAnyObjectByType<CookingStation>();
        var chopping = UnityEngine.Object.FindAnyObjectByType<ChoppingStation>();
        var assembly = UnityEngine.Object.FindAnyObjectByType<AssemblyStation>();
        var washing = UnityEngine.Object.FindAnyObjectByType<WashingStation>();
        Check(cooking != null && chopping != null && assembly != null && washing != null,
            "El mapa contiene fogón, mesa de picado, armado y lavaplatos");
        Food(worker);
        cooking.Interact(worker);
        Check(worker.HeldItem == null && cooking.IsCooking(), "Dejar un ingrediente inicia la cocción");
        float until = Time.time + cooking.cookingTime + 1f;
        while (cooking.IsCooking() && Time.time < until) yield return null;
        cooking.Interact(worker);
        Check(worker.HeldItem != null && worker.HeldItem.GetComponent<Ingredient>().isCooked,
            "La cocción termina y permite recuperar el ingrediente cocido");
        Empty(worker);
        Food(worker);
        chopping.Interact(worker);
        Check(worker.HeldItem == null && chopping.IsChopping(), "Dejar un ingrediente inicia el picado");
        until = Time.time + chopping.choppingTime + 1f;
        while (chopping.IsChopping() && Time.time < until) yield return null;
        chopping.Interact(worker);
        Check(worker.HeldItem != null && worker.HeldItem.GetComponent<Ingredient>().isChopped,
            "El picado termina y permite recuperar el ingrediente preparado");
        Empty(worker);
        var recipes = Resources.LoadAll<RecipeData>("Recipes");
        Check(recipes.Length >= 4, "Están disponibles las cuatro recetas del nivel");
        foreach (var recipe in recipes)
        {
            washing.Interact(worker);
            Check(worker.HeldItem != null && worker.HeldItem.GetComponent<Plate>() != null,
                "El lavaplatos entrega un plato limpio para " + recipe.recipeName);
            assembly.Interact(worker);
            foreach (var type in recipe.requiredIngredients)
            {
                var food = new GameObject("Ingrediente de receta");
                var ingredient = food.AddComponent<Ingredient>();
                ingredient.ingredientType = type;
                if (recipe.requiresCooking) ingredient.MarkCooked();
                if (recipe.requiresChopping) ingredient.MarkChopped();
                Check(worker.TryHoldItem(food), "Recoge ingrediente para " + recipe.recipeName);
                assembly.Interact(worker);
            }
            assembly.Interact(worker);
            var dish = worker.HeldItem != null ? worker.HeldItem.GetComponent<DishItem>() : null;
            Check(dish != null && dish.recipe == recipe, "La mesa arma y entrega " + recipe.recipeName);
            var table = UnityEngine.Object.FindAnyObjectByType<FoodCourtTable>();
            var order = new Order(recipe.recipeName, recipe, 60f);
            OrderSystem.Instance.GetActiveOrders().Add(order);
            table.SetOrder(order);
            int delivered = gm.OrdersDelivered;
            int score = gm.GetCurrentScore();
            table.Interact(worker);
            Check(worker.HeldItem == null && gm.OrdersDelivered == delivered + 1 && gm.GetCurrentScore() > score,
                "Entregar " + recipe.recipeName + " completa el pedido y suma puntos");
        }
        gm.RestartGame();
        yield return null;
    }
}
#endif
