#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Ejecutar Unity con -batchmode -executeMethod MarketLifeValidation.Run (sin -quit).
public static class MarketLifeValidation
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static Stack<IEnumerator> routines;
    private static double deadline;
    private static int passed;
    public static void Run()
    {
        SessionState.SetBool("MarketLifeValidation", true);
        Application.runInBackground = true;
        var view = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        view.Show(); view.Focus();
        ConfigureView(view);
        EditorSceneManager.OpenScene("Assets/Scenes/KitchenScene.unity");
        EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("MarketLifeValidation", false)) return;
            routines = new Stack<IEnumerator>(); routines.Push(Checks());
            passed = 0; deadline = EditorApplication.timeSinceStartup + 240;
            EditorApplication.update += Tick;
        };
    }
    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Tiempo de validación agotado");
            if (routines.Count == 0) { Finish(0, "PASS: " + passed + " comprobaciones del mercado dinámico."); return; }
            var routine = routines.Peek();
            if (!routine.MoveNext()) routines.Pop();
            else if (routine.Current is IEnumerator child) routines.Push(child);
        }
        catch (Exception e) { Finish(1, "FAIL tras " + passed + " comprobaciones: " + e); }
    }
    private static void Finish(int code, string result)
    {
        EditorApplication.update -= Tick; SessionState.SetBool("MarketLifeValidation", false);
        Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/market-life-result.txt", result);
        Debug.Log(result); EditorApplication.Exit(code);
    }
    private static void ConfigureView(EditorWindow view)
    {
        var assembly = typeof(Editor).Assembly;
        Type sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        object sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
        Type groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
        object group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
        Type sizeType = assembly.GetType("UnityEditor.GameViewSize");
        object fixedSize = Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution");
        object size = Activator.CreateInstance(sizeType, new object[] { fixedSize, 1280, 720, "Market validation" });
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null) - 1;
        view.GetType().GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(view, index);
    }
    private static void Check(bool value, string text)
    {
        if (!value) throw new Exception(text); passed++; Debug.Log("PASS: " + text);
    }
    private static void Set(object owner, string field, object value) => owner.GetType().GetField(field, Private).SetValue(owner, value);
    private static IEnumerator Wait(float seconds)
    {
        float until = Time.time + seconds;
        while (Time.time < until) yield return null;
    }
    private static IEnumerator RealWait(float seconds)
    {
        double until = EditorApplication.timeSinceStartup + seconds;
        while (EditorApplication.timeSinceStartup < until) yield return null;
    }
    private static void PutAtWorker(MarketPowerup pickup, PlayerController worker)
    {
        var body = worker.GetComponent<Collider>();
        pickup.transform.position = new Vector3(worker.transform.position.x, body.bounds.min.y, worker.transform.position.z);
        Physics.SyncTransforms();
    }
    private static Order AddOrder(OrderSystem orders, RecipeData recipe, int tableIndex, float remaining, bool vip = false)
    {
        var order = new Order(recipe.recipeName, recipe, 100f) { timeRemaining = remaining, isVip = vip };
        orders.tables[tableIndex].SetOrder(order); orders.GetActiveOrders().Add(order); return order;
    }
    private static IEnumerator Checks()
    {
        yield return RealWait(5f);
        double readyBy = EditorApplication.timeSinceStartup + 35f;
        while ((MarketLife.Instance == null || MarketLife.Instance.Pedestrians.Count < 36) && EditorApplication.timeSinceStartup < readyBy)
            yield return null;
        var windows = UnityEngine.Object.FindAnyObjectByType<FloatingWindows>();
        windows.showIntro = false; typeof(FloatingWindows).GetMethod("Close", Private).Invoke(windows, null);
        yield return RealWait(.2f);
        var local = LocalMultiplayer.Instance; var market = MarketLife.Instance;
        Check(market != null && market.Layout != null && market.Layout.Indices.Count > 0, "El plano contiene pasillos conectados al jugador");
        Check(market.Pedestrians.Count == 36, "Se crearon 36 caseros sin reconstruir la escena");
        Check(market.GetComponent<MarketMinimap>() != null && local.Worker.GetComponent<PlayerDirectionIndicator>() != null, "Minimapa e indicador de rumbo conectados");
        var game = GameManager.Instance; var orders = OrderSystem.Instance;
        orders.firstOrderDelay = 1000f; game.gameDuration = 5000f;
        GameFrontEnd.Instance.SelectMode(LocalMultiplayer.GameMode.SinglePlayer);
        Check(GameFrontEnd.Instance.StartSelectedGame(), "La jornada individual inicia con los nuevos sistemas");
        if (SabotageSystem.Instance != null) SabotageSystem.Instance.sabotageEnabled = false;
        yield return Wait(.2f);
        Check(local.Worker.CocaLeaves == 0 && market.CocaCountdown > 24f && market.CascaditaCountdown > 39f, "Los intervalos de 25 y 40 segundos empiezan desde cero");
        Set(market, "cocaTimer", 24f); yield return Wait(.3f);
        Check(market.Coca == null, "La coca no aparece antes de 25 segundos");
        Set(market, "cocaTimer", 24.95f); yield return Wait(.15f);
        var coca = market.Coca;
        Check(coca != null && coca.Available && coca.Remaining > 9.6f, "La coca aparece al completar su intervalo y dispone de diez segundos");
        Check(!market.TryCollect(coca, local.Rival), "El Rival no puede recoger la coca");
        Vector3 randomLocation = coca.transform.position;
        coca.transform.position = local.Worker.transform.position + Vector3.right * 15f;
        Check(!market.TryCollect(coca, local.Worker) && coca.Available, "Intentar recoger una hoja distante no consume el objeto");
        coca.transform.position = randomLocation;
        PutAtWorker(coca, local.Worker);
        Check(market.TryCollect(coca, local.Worker) && local.Worker.CocaLeaves == 1, "Tomás recoge la hoja automáticamente al pasar");
        Check(!market.TryCollect(coca, local.Worker) && Mathf.Approximately(local.Worker.CocaSpeedMultiplier, 1.15f), "La recogida es única y aumenta un 15 % la velocidad");
        Set(market, "cocaTimer", 24.95f); yield return Wait(.15f);
        PutAtWorker(market.Coca, local.Worker); market.TryCollect(market.Coca, local.Worker);
        Check(Mathf.Abs(local.Worker.CocaSpeedMultiplier - 1.3225f) < .001f, "Dos hojas acumulan 1,15 × 1,15 sin alterar la velocidad base");
        Set(market, "cascTimer", 39.95f); yield return Wait(.15f);
        var casc = market.Cascadita;
        Check(casc != null && casc.Available && !market.TryCollect(casc, local.Rival), "Cascadita aparece a los 40 segundos y es exclusiva de Tomás");
        float remaining = casc.Remaining, countdown = market.CocaCountdown;
        game.TogglePause(); yield return RealWait(.5f);
        Check(casc.Available && Mathf.Abs(casc.Remaining-remaining)<.03f && Mathf.Abs(market.CocaCountdown-countdown)<.03f, "Pausar congela la duración y los intervalos");
        game.TogglePause(); yield return Wait(10.1f);
        Check(casc == null || !casc.Available, "El sachet desaparece tras diez segundos sin recogerlo");
        var recipe = Resources.LoadAll<RecipeData>("Recipes")[0];
        Order slow = AddOrder(orders, recipe, 0, 90f), urgent = AddOrder(orders, recipe, 1, 20f, true);
        Set(market, "cascTimer", 39.95f); yield return Wait(.15f);
        PutAtWorker(market.Cascadita, local.Worker);
        Check(market.TryCollect(market.Cascadita, local.Worker), "Recoger Cascadita dispara una entrega");
        Check(orders.GetActiveOrders().Contains(slow) && !orders.GetActiveOrders().Contains(urgent) && !urgent.table.hasOrder, "La entrega atiende primero el pedido más urgente y libera su mesa");
        Check(game.OrdersDelivered == 1 && game.VipOrdersDelivered == 1 && game.GetCurrentScore() >= recipe.basePoints, "La entrega cuenta para puntuación, reputación y objetivos VIP");
        orders.GetActiveOrders().Clear(); slow.table.ClearOrder();
        Set(market, "cascTimer", 39.95f); yield return Wait(.15f);
        PutAtWorker(market.Cascadita, local.Worker); market.TryCollect(market.Cascadita, local.Worker);
        Check(market.PendingDeliveries == 1, "Sin pedidos se conserva una entrega para el siguiente");
        AddOrder(orders, recipe, 0, 90f); yield return Wait(.15f);
        Check(market.PendingDeliveries == 0 && orders.GetActiveOrders().Count == 0, "La entrega reservada atiende el siguiente pedido una sola vez");
        int moving = 0, stationary = 0;
        foreach (var npc in market.Pedestrians)
        {
            Check(npc.Agent.isOnNavMesh, "Casero situado en un pasillo transitable");
            if (npc.Stationary) stationary++; else if (npc.Agent.hasPath || npc.Agent.velocity.sqrMagnitude > .01f) moving++;
        }
        Check(stationary >= 6 && moving > 0, "El mercado combina puestos quietos con peatones en movimiento");
        local.Worker.usesAI = true; local.Worker.SetAIMovement(Vector3.right);
        yield return Wait(3f);
        var camera = local.Worker.viewCamera;
        Check(camera.orthographicSize < 5.9f && Mathf.Abs(Mathf.DeltaAngle(camera.transform.eulerAngles.y,90f)) < 15f, "La cámara se acerca y gira hacia la marcha");
        local.Worker.SetAIMovement(Vector3.zero); local.Worker.usesAI = false;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var oldInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); InputSystem.Update();
        var calculate = typeof(PlayerController).GetMethod("CalculateMoveDirection", Private);
        calculate.Invoke(local.Worker, null); Vector3 heldDirection = local.Worker.MovementDirection;
        Quaternion savedRotation = camera.transform.rotation;
        camera.transform.Rotate(0f, 55f, 0f, Space.World); calculate.Invoke(local.Worker, null);
        Check(heldDirection.sqrMagnitude > .9f && Vector3.Dot(heldDirection,local.Worker.MovementDirection) > .999f, "Mantener una tecla conserva el rumbo mientras gira la cámara");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); calculate.Invoke(local.Worker,null);
        InputSystem.RemoveDevice(keyboard); InputSystem.settings.editorInputBehaviorInPlayMode = oldInput;
        camera.transform.rotation = savedRotation;
        ScreenCapture.CaptureScreenshot("Logs/market-life-single.png"); yield return RealWait(1f);
        local.ConfigureMode(LocalMultiplayer.GameMode.LocalMultiplayer);
        local.Rival.GetComponent<LocalRivalDriver>().enabled = false;
        local.Worker.usesAI = true; local.Worker.SetAIMovement(Vector3.forward);
        local.Rival.usesAI = true; local.Rival.SetAIMovement(Vector3.left);
        yield return Wait(3.5f);
        Check(Mathf.Abs(Mathf.DeltaAngle(local.Worker.viewCamera.transform.eulerAngles.y,local.Rival.viewCamera.transform.eulerAngles.y)) > 55f, "Cada cámara local sigue el rumbo de su propio jugador");
        local.Worker.SetAIMovement(Vector3.zero); local.Rival.SetAIMovement(Vector3.zero);
        local.Worker.usesAI = false; local.ConfigureMode(LocalMultiplayer.GameMode.SinglePlayer);
        local.Rival.GetComponent<LocalRivalDriver>().enabled = true;
        game.RestartGame(); yield return Wait(.1f);
        Check(local.Worker.CocaLeaves == 0 && market.PendingDeliveries == 0 && market.Coca == null && market.Cascadita == null, "Reiniciar limpia bonificaciones, objetos y entregas pendientes");
        Check(market.CocaCountdown > 24f && market.CascaditaCountdown > 39f, "Reiniciar restablece ambos temporizadores");
        yield return Wait(1.5f);
        ((IList)typeof(FloatingWindows).GetField("toasts",Private).GetValue(windows)).Clear();
        ScreenCapture.CaptureScreenshot("Logs/market-life-reset.png"); yield return RealWait(1f);
        Set(market,"cocaTimer",24.95f); Set(market,"cascTimer",39.95f); yield return Wait(.15f);
        Check(market.Coca != null && market.Cascadita != null, "Los dos potenciadores pueden coexistir con marcadores independientes");
        Check(market.CocaCountdown > 24f && market.CascaditaCountdown > 39f, "Recoger o aparecer un objeto no desplaza el intervalo del otro");
        AddOrder(orders,recipe,0,80f);
        Vector3 pickupPoint = local.Worker.transform.position + local.Worker.transform.forward * 3f;
        var nav=InvestigationSystem.Instance.Navigation;
        if(nav.Sample(pickupPoint,2f,out Vector3 floor)) market.Coca.transform.position=floor;
        yield return Wait(.2f);
        var direction=local.Worker.transform.Find("Dirección de Tomás");
        Check(direction!=null && direction.gameObject.activeInHierarchy && direction.GetComponent<Renderer>().enabled,
            "La flecha de rumbo permanece visible después de seleccionar apariencia y reiniciar");
        Check(direction.position.y > local.Worker.GetComponent<Collider>().bounds.min.y,
            "La flecha se coloca sobre el piso real");
        var dialogActor = market.Pedestrians[0];
        Vector3 npcHome = dialogActor.transform.position;
        if (nav.Sample(local.Worker.transform.position + Vector3.right * 2f,2f,out Vector3 talkPoint)) dialogActor.Agent.Warp(talkPoint);
        dialogActor.Interact(local.Worker);
        Check((float)typeof(MarketPedestrian).GetField("speechRemaining",Private).GetValue(dialogActor) > 0f,
            "Los caseros responden al interactuar y muestran su burbuja");
        ((IList)typeof(FloatingWindows).GetField("toasts",Private).GetValue(windows)).Clear();
        ScreenCapture.CaptureScreenshot("Logs/market-life-powerups.png"); yield return RealWait(1f);
        dialogActor.Agent.Warp(npcHome);
    }
}
#endif
