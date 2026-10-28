#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class SelectiveMenuValidation
{
    private static IEnumerator routine;private static double deadline;private static int passed;
    private static InputSettings.BackgroundBehavior oldBackground;private static bool backgroundChanged;
    private static InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private static void UpdateGameInput()
    {
        typeof(InputSystem).GetMethod("Update",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(InputUpdateType)},null).Invoke(null,new object[]{InputUpdateType.Dynamic});
    }
    public static void Run(){Begin(false);}
    public static void Visual(){Begin(true);}
    public static void RunWithVisual()
    {
        SessionState.SetBool("SelectiveMenuChainVisual",true);var view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));view.Show();view.Focus();Begin(false);
    }
    private static void Begin(bool visual)
    {
        SessionState.SetBool("SelectiveMenuValidation",true);SessionState.SetBool("SelectiveMenuVisual",visual);
        EditorSceneManager.OpenScene("Assets/Scenes/KitchenScene.unity");
        if(visual){var view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));view.Show();view.Focus();}
        EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]private static void Init()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("SelectiveMenuValidation",false))
            {passed=0;routine=SessionState.GetBool("SelectiveMenuVisual",false)?VisualChecks():Checks();deadline=EditorApplication.timeSinceStartup+120;EditorApplication.update+=Tick;}
        };
    }
    private static void Tick()
    {
        try{if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Tiempo agotado");if(!routine.MoveNext())Finish(0,"PASS: "+passed+" comprobaciones de integración selectiva.");}
        catch(Exception e){Finish(1,"FAIL: "+e);}
    }
    private static void Finish(int code,string result)
    {
        if(backgroundChanged){InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;backgroundChanged=false;}
        EditorApplication.update-=Tick;SessionState.SetBool("SelectiveMenuValidation",false);Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/"+(SessionState.GetBool("SelectiveMenuVisual",false)?"selective-menu-visual":"selective-menu-result")+".txt",result);Debug.Log(result);
        if(code==0 && SessionState.GetBool("SelectiveMenuChainVisual",false) && !SessionState.GetBool("SelectiveMenuVisual",false))
        {SessionState.SetBool("SelectiveMenuChainVisual",false);SessionState.SetBool("SelectiveMenuVisual",true);passed=0;routine=VisualChecks();deadline=EditorApplication.timeSinceStartup+100;EditorApplication.update+=Tick;return;}
        EditorApplication.Exit(code);
    }
    private static void Check(bool condition,string text){if(!condition)throw new Exception(text);passed++;Debug.Log("PASS: "+text);}
    private static void Empty(PlayerController actor){foreach(Transform item in actor.Hand)UnityEngine.Object.Destroy(item.gameObject);}
    private static void Open(FloatingWindows windows,string page)
    {
        var kind=typeof(FloatingWindows).GetNestedType("Page",BindingFlags.NonPublic);
        typeof(FloatingWindows).GetMethod("Open",Private).Invoke(windows,new object[]{Enum.Parse(kind,page),false});
    }
    private static IEnumerator Checks()
    {
        float until=Time.realtimeSinceStartup+3;while(Time.realtimeSinceStartup<until)yield return null;
        var local=LocalMultiplayer.Instance;var front=GameFrontEnd.Instance;var gm=GameManager.Instance;
        Check(local!=null && front!=null,"El menú y los participantes se instalan sin editar la escena");
        Check(UnityEngine.Object.FindObjectsByType<PlayerController>().Length==2,"Hay exactamente dos controladores");
        Check(FloatingWindows.IsOpen && !front.StartSelectedGame(),"La introducción bloquea el inicio accidental");
        Check(local.Mode==LocalMultiplayer.GameMode.SinglePlayer && local.Rival.usesAI,"Individual activo con Rival automático");
        Check(SabotageSystem.Instance.sabotageEnabled && SabotageSystem.Instance.firstSabotageAfter==45,"Se mantiene el sabotaje automático y su espera original");
        Check(local.Worker.viewCamera.rect.width==1 && !local.Rival.viewCamera.gameObject.activeSelf,"Individual usa una pantalla completa");
        Check(local.Worker.GetComponent<TomasAvatar>().enabled,"Tomás conserva su diseño y animación de la base");
        Check(Vector3.Distance(local.Worker.transform.position,local.Rival.transform.position)>1,"Los participantes aparecen separados");
        var catalog=Resources.Load<CharacterSkinCatalog>("MenuIntegration/CharacterSkins");Check(catalog!=null,"Catálogo de selección disponible");
        for(int i=0;i<5;i++)
        {
            Check(CharacterPortraitLibrary.Get(i)!=null && CharacterPortraitLibrary.Get(i,true)!=null,"Foto y retrato: "+CharacterSkinCatalog.Names[i]);
            Check(catalog.Model(i)!=null,"Modelo existente enlazado: "+CharacterSkinCatalog.Names[i]);
            if(i>0)
            {
                foreach(var filter in catalog.Model(i).GetComponentsInChildren<MeshFilter>(true))Check(filter.sharedMesh!=null,"Referencia de malla resuelta");
                foreach(var renderer in catalog.Model(i).GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)Check(material!=null,"Referencia de material resuelta");
            }
        }
        var windows=UnityEngine.Object.FindAnyObjectByType<FloatingWindows>();bool story=false,cv=false,cards=false;int guard=0;
        while(FloatingWindows.IsOpen && guard++<30)
        {
            string page=typeof(FloatingWindows).GetField("page",Private).GetValue(windows).ToString();story|=page=="Story";cv|=page=="Curriculum";cards|=page=="Cards";
            typeof(FloatingWindows).GetMethod("Next",Private).Invoke(windows,null);yield return null;
        }
        Check(story && cv && cards && !FloatingWindows.IsOpen,"Flujo completo: historia, currículum, fichas y menú");
        var characters=(Array)typeof(FloatingWindows).GetField("Characters",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        Check(characters.Length==5,"Las cinco fichas están incluidas");
        yield return null;yield return null;
        oldBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;backgroundChanged=true;
        oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard=InputSystem.AddDevice<Keyboard>();Vector2 workerInput=Vector2.zero,rivalInput=Vector2.zero;
        Action sample=()=>{if(InputState.currentUpdateType==InputUpdateType.Dynamic){workerInput=PlayerController.ReadMovement(PlayerController.LocalControls.WASD,keyboard);rivalInput=PlayerController.ReadMovement(PlayerController.LocalControls.IJKL,keyboard);}};
        InputSystem.onAfterUpdate+=sample;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.L));UpdateGameInput();InputSystem.onAfterUpdate-=sample;
        Check(workerInput==Vector2.up,"W controla solo al jugador 1");
        Check(rivalInput==Vector2.right,"L controla al Rival de forma independiente");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());UpdateGameInput();
        Check(front.SelectMode(LocalMultiplayer.GameMode.LocalMultiplayer) && !local.Rival.usesAI,"El menú activa controles locales para el Rival");
        Check(local.Worker.viewCamera.rect.width==.5f && local.Rival.viewCamera.gameObject.activeSelf && local.Rival.viewCamera.rect.x==.5f,"Multijugador divide las dos cámaras");
        Check(!SabotageSystem.Instance.sabotageEnabled,"En local el sabotaje queda a cargo del Rival humano");
        float height=local.Worker.GetComponent<CapsuleCollider>().height;
        for(int i=0;i<5;i++)
        {
            Check(front.SelectSkin(false,i) && front.SelectSkin(true,i),"Selección de apariencia "+CharacterSkinCatalog.Names[i]);
            Check(local.Worker.GetComponent<CapsuleCollider>().height==height && local.Rival.controls==PlayerController.LocalControls.IJKL,"La apariencia conserva física y controles");yield return null;
        }
        front.SelectSkin(false,0);front.SelectSkin(true,1);yield return null;
        Check(local.Worker.GetComponent<TomasAvatar>().enabled,"Volver a Tomás recupera el diseño original");
        Check(!front.SelectSkin(false,99),"Se rechaza una selección inexistente");
        Check(front.StartSelectedGame(),"El menú inicia la partida elegida");
        Check(!front.SelectMode(LocalMultiplayer.GameMode.SinglePlayer) && !front.SelectSkin(false,2),"No cambia modo o apariencia en medio del turno");
        until=Time.time+1.3f;while(Time.time<until)yield return null;
        Check(Mathf.Abs(local.Worker.transform.position.y-local.Rival.transform.position.y)<.6f,"Los participantes se apoyan en el mismo piso al iniciar: "+local.Worker.transform.position+" / "+local.Rival.transform.position);
        var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.position=new Vector3(1000,0,1000);ground.transform.localScale=new Vector3(30,1,30);
        local.Worker.Teleport(new Vector3(1000,1.3f,1000));local.Rival.Teleport(new Vector3(1003,1.3f,1000));Physics.SyncTransforms();
        Vector3 beforeWorker=local.Worker.transform.position,beforeRival=local.Rival.transform.position;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.L));UpdateGameInput();until=Time.time+.3f;while(Time.time<until)yield return null;
        Check(Vector3.Distance(beforeWorker,local.Worker.transform.position)>.2f && Vector3.Distance(beforeRival,local.Rival.transform.position)>.2f,"Ambos jugadores se mueven simultáneamente: "+beforeWorker+" → "+local.Worker.transform.position+" / "+beforeRival+" → "+local.Rival.transform.position);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());UpdateGameInput();
        var crate=new GameObject("Caja de prueba").AddComponent<IngredientCrate>();crate.ingredientType=Ingredient.IngredientType.Potato;
        crate.Interact(local.Worker);crate.Interact(local.Rival);yield return null;
        Check(local.Worker.Hand.childCount==1 && local.Rival.Hand.childCount==1 && local.Worker.Hand.GetChild(0)!=local.Rival.Hand.GetChild(0),"Inventarios independientes con las cajas existentes");
        var counter=GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<KitchenCounter>();counter.transform.position=new Vector3(1010,1,1000);
        counter.Interact(local.Worker);Check(local.Worker.Hand.childCount==0 && counter.GetStoredItem()!=null,"El jugador 1 deja un ingrediente en la barra existente");
        counter.Interact(local.Rival);Check(counter.GetStoredItem()!=null && local.Rival.Hand.childCount==1,"El Rival no recoge si tiene la mano ocupada");
        Empty(local.Rival);yield return null;counter.Interact(local.Rival);Check(local.Rival.Hand.childCount==1 && counter.GetStoredItem()==null,"El Rival recoge de una estación compartida");
        Empty(local.Rival);yield return null;crate.Interact(local.Worker);
        var cooker=GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<CookingStation>();cooker.transform.position=new Vector3(1015,1,1000);cooker.cookingTime=.15f;
        cooker.Interact(local.Worker);until=Time.time+.5f;while(Time.time<until)yield return null;
        Check(cooker.GetStoredItem()!=null && cooker.GetStoredItem().GetComponent<Ingredient>().isCooked,"La cocina estable prepara el ingrediente");
        cooker.Interact(local.Rival);Check(local.Rival.Hand.childCount==1 && local.Rival.Hand.GetChild(0).GetComponent<Ingredient>().isCooked,"El segundo jugador puede recoger el ingrediente cocinado");
        var driver=local.Rival.GetComponent<LocalRivalDriver>();int sabotageCount=SabotageSystem.Instance.SabotageCount;bool attributed=false;
        Action<PlayerController,Interactable> onSabotage=(actor,target)=>attributed=actor==local.Rival && target==cooker;
        LocalRivalDriver.OnSabotageCommitted+=onSabotage;
        typeof(PlayerController).GetField("currentInteractable",Private).SetValue(local.Rival,cooker);
        Check(!driver.TrySabotage(),"No activa sabotajes en una estación lejana");
        local.Rival.Teleport(cooker.transform.position+Vector3.back);Physics.SyncTransforms();
        typeof(PlayerController).GetField("currentInteractable",Private).SetValue(local.Rival,cooker);
        Check(driver.TrySabotage(),"El Rival activa el humo existente en una estación");
        Check(SabotageSystem.Instance.SabotageCount==sabotageCount+1 && attributed,"El sabotaje queda atribuido al Rival que lo ejecuta");
        Check(local.Worker.GetComponent<LocalRivalDriver>()==null && !driver.TrySabotage(),"El trabajador no recibe U de sabotaje y el Rival respeta su espera");
        LocalRivalDriver.OnSabotageCommitted-=onSabotage;
        Open(windows,"Cards");Check(Time.timeScale==0 && !PlayerController.GameplayActive,"Las fichas pausan a los dos jugadores");
        typeof(FloatingWindows).GetMethod("Close",Private).Invoke(windows,null);yield return null;
        front.ReturnToMenu();yield return null;Check(gm.CurrentState==GameManager.GameState.Ready && local.Worker.Hand.childCount==0 && local.Rival.Hand.childCount==0,"Volver al menú limpia las manos y recupera el estado inicial");
        Check(front.SelectMode(LocalMultiplayer.GameMode.SinglePlayer) && local.Rival.usesAI && SabotageSystem.Instance.sabotageEnabled,"Volver a individual recupera la IA y el sabotaje automático");
        SabotageSystem.Instance.firstSabotageAfter=.15f;Check(front.StartSelectedGame(),"Comienza otra partida sin duplicar participantes");
        until=Time.time+.6f;while(Time.time<until)yield return null;
        Check(SabotageSystem.Instance.SabotageCount>0,"El sabotaje automático estable sigue funcionando");
        Check(UnityEngine.Object.FindObjectsByType<PlayerController>().Length==2,"Reiniciar conserva exactamente dos jugadores");
        InputSystem.RemoveDevice(keyboard);
    }
    private static IEnumerator VisualChecks()
    {
        GameFrontEnd.Instance.ReturnToMenu();yield return null;
        Directory.CreateDirectory("Logs/MenuIntegration");float until=Time.realtimeSinceStartup+3;while(Time.realtimeSinceStartup<until)yield return null;
        var windows=UnityEngine.Object.FindAnyObjectByType<FloatingWindows>();string[] pages={"Story","Curriculum","Cards","Cards","Cards","Cards","Cards"};
        for(int i=0;i<pages.Length;i++)
        {
            Open(windows,pages[i]);if(i>=2)typeof(FloatingWindows).GetField("index",Private).SetValue(windows,i-2);
            until=Time.realtimeSinceStartup+.65f;while(Time.realtimeSinceStartup<until)yield return null;
            ScreenCapture.CaptureScreenshot("Logs/MenuIntegration/Entrada_"+i+".png");until=Time.realtimeSinceStartup+.6f;while(Time.realtimeSinceStartup<until)yield return null;
            Check(File.Exists("Logs/MenuIntegration/Entrada_"+i+".png"),"Pantalla de entrada capturada "+i);
        }
        typeof(FloatingWindows).GetMethod("Close",Private).Invoke(windows,null);until=Time.realtimeSinceStartup+.5f;while(Time.realtimeSinceStartup<until)yield return null;
        ScreenCapture.CaptureScreenshot("Logs/MenuIntegration/Menu.png");until=Time.realtimeSinceStartup+.6f;while(Time.realtimeSinceStartup<until)yield return null;
        Check(File.Exists("Logs/MenuIntegration/Menu.png"),"Menú capturado");
        GameFrontEnd.Instance.SelectMode(LocalMultiplayer.GameMode.LocalMultiplayer);GameFrontEnd.Instance.StartSelectedGame();
        until=Time.realtimeSinceStartup+1;while(Time.realtimeSinceStartup<until)yield return null;
        ScreenCapture.CaptureScreenshot("Logs/MenuIntegration/Local.png");until=Time.realtimeSinceStartup+.6f;while(Time.realtimeSinceStartup<until)yield return null;
        Check(File.Exists("Logs/MenuIntegration/Local.png"),"Partida local capturada");
    }
}
#endif
