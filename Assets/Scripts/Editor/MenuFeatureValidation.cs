#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MenuFeatureValidation
{
    private static IEnumerator routine;
    private static int passed;
    private static double deadline;
    public static void Run()
    {
        SessionState.SetBool("MenuFeatureValidation",true);
        EditorSceneManager.OpenScene("Assets/Scenes/KitchenScene.unity");
        EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("MenuFeatureValidation",false))
            {passed=0;routine=Checks();deadline=EditorApplication.timeSinceStartup+120;EditorApplication.update+=Tick;}
        };
    }
    private static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Tiempo de prueba agotado");
            if(!routine.MoveNext())Finish(0,"PASS: "+passed+" comprobaciones del menú, apariencias, modelos e IA.");
        }
        catch(Exception e){Finish(1,"FAIL: "+e);}
    }
    private static void Finish(int code,string text)
    {
        EditorApplication.update-=Tick;SessionState.SetBool("MenuFeatureValidation",false);Debug.Log(text);
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/menu-feature-result.txt",text);
        EditorApplication.Exit(code);
    }
    private static void Check(bool condition,string text)
    {
        if(!condition)throw new Exception(text);passed++;Debug.Log("PASS: "+text);
    }
    private static GameObject Hold(PlayerController player)
    {
        var go=new GameObject("Papa para prueba individual");
        var ingredient=go.AddComponent<Ingredient>();ingredient.ingredientType=Ingredient.IngredientType.Potato;
        go.AddComponent<FoodVisual>().Refresh(ingredient.ingredientType,false,false);
        Check(player.TryHoldItem(go),"Recoge comida con el modelo 3D nuevo");return go;
    }
    private static IEnumerator Checks()
    {
        float until=Time.realtimeSinceStartup+2f;while(Time.realtimeSinceStartup<until)yield return null;
        var local=LocalMultiplayer.Instance;var front=GameFrontEnd.Instance;var gm=GameManager.Instance;
        Check(front!=null && local!=null,"El menú previo se instala en la escena jugable");
        Check(FloatingWindows.IsOpen && !front.StartSelectedGame(),"La historia inicial se conserva y bloquea el inicio accidental");
        Check(local.Mode==LocalMultiplayer.GameMode.SinglePlayer && local.Rival.usesAI,"El modo individual activa al Rival automático");
        Check(local.Worker.viewCamera.rect.width==1f && !local.Rival.viewCamera.gameObject.activeSelf,"El modo individual utiliza la pantalla completa");
        var catalog=Resources.Load<CharacterSkinCatalog>("CharacterSkins");
        Check(catalog!=null,"El catálogo de apariencias se carga en tiempo de juego");
        for(int i=0;i<CharacterSkinCatalog.Names.Length;i++)
            Check(catalog.Model(i)!=null && catalog.Model(i).GetComponentsInChildren<Renderer>().Length>0,"Apariencia disponible: "+CharacterSkinCatalog.Names[i]);
        foreach(Ingredient.IngredientType type in Enum.GetValues(typeof(Ingredient.IngredientType)))
            foreach(FoodModelFactory.Preparation state in Enum.GetValues(typeof(FoodModelFactory.Preparation)))
            {
                var model=FoodModelLibrary.IngredientPrefab(type,state);
                Check(model!=null && model.GetComponentsInChildren<MeshFilter>().Length>0,"Mallas 3D disponibles: "+type+" / "+state);
            }
        foreach(string name in new[]{"Sandwich","Api","Paceno","Frutas","Saltena"})
            Check(Resources.Load<GameObject>("FoodModels/Dishes/"+name)!=null,"Plato 3D original disponible: "+name);
        var windows=UnityEngine.Object.FindAnyObjectByType<FloatingWindows>();
        windows.showIntro=false;
        typeof(FloatingWindows).GetMethod("Close",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(windows,null);
        Time.timeScale=1f;yield return null;
        float radius=local.Worker.GetComponent<CapsuleCollider>().radius;
        for(int i=0;i<CharacterSkinCatalog.Names.Length;i++)
        {
            Check(front.SelectSkin(false,i) && front.SelectSkin(true,i),"Se elige la apariencia "+CharacterSkinCatalog.Names[i]+" para ambos participantes");
            Check(local.Worker.role==PlayerController.PlayerRole.Worker && local.Rival.role==PlayerController.PlayerRole.Impostor && local.Worker.GetComponent<CapsuleCollider>().radius==radius,
                "La apariencia conserva roles y colisiones");
        }
        Check(!front.SelectSkin(false,99),"Rechaza una apariencia inexistente");
        front.ToggleGuide();
        Check(GameFrontEnd.GuideVisible && Time.timeScale==0f && !front.StartSelectedGame(),"El recetario previo no inicia el turno");
        front.CloseGuide();yield return null;
        Check(front.SelectMode(LocalMultiplayer.GameMode.LocalMultiplayer) && !local.Rival.usesAI,"Seleccionar multijugador desactiva la IA");
        Check(local.Worker.viewCamera.rect.width==.5f && local.Rival.viewCamera.gameObject.activeSelf,"Seleccionar multijugador activa dos vistas");
        Check(front.StartSelectedGame(),"El botón de inicio comienza el modo seleccionado");
        Check(!front.SelectMode(LocalMultiplayer.GameMode.SinglePlayer) && !front.SelectSkin(false,0),"No cambia modo ni apariencia durante un turno");
        front.ToggleGuide();float left=gm.GetTimeRemaining();
        until=Time.realtimeSinceStartup+.25f;while(Time.realtimeSinceStartup<until)yield return null;
        Check(!PlayerController.GameplayActive && Mathf.Abs(left-gm.GetTimeRemaining())<.05f,"El recetario pausa reloj, jugadores e IA");
        front.CloseGuide();yield return null;
        Check(PlayerController.GameplayActive,"Cerrar el recetario reanuda el turno");
        front.ReturnToMenu();
        Check(gm.CurrentState==GameManager.GameState.Ready,"Se puede volver al menú sin recargar el mapa");
        Check(front.SelectMode(LocalMultiplayer.GameMode.SinglePlayer) && front.StartSelectedGame(),"Se puede iniciar una partida individual después de la local");
        var ai=local.Rival.GetComponent<RivalAI>();var attack=local.Rival.GetComponent<TackleSabotage>();
        ai.graceSeconds=0;ai.sabotageChance=1f;ai.minimumInterval=ai.maximumInterval=.2f;
        attack.warningSeconds=.15f;attack.cooldownSeconds=.5f;
        var system=InvestigationSystem.Instance;
        Check(system.Navigation.Ready && system.Police!=null,"La IA comparte las rutas navegables del mercado");
        Vector3 origin=system.Police.transform.position;Vector3 point=origin;bool found=false;
        for(int i=0;i<16 && !found;i++)
        {
            float a=i*Mathf.PI/8;
            if(!system.Navigation.CanReach(origin,origin+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*2.1f,out point))continue;
            local.Worker.ResetForRound(point+Vector3.up*.75f,Quaternion.identity);
            local.Rival.ResetForRound(origin+Vector3.up*.75f,Quaternion.LookRotation(point-origin));
            Physics.SyncTransforms();found=attack.HasClearPath(local.Worker) && Vector3.Distance(point,origin)>1.5f;
        }
        Check(found,"La IA puede aproximarse sin atravesar un obstáculo");
        ai.ResetForRound();attack.ResetForRound();
        Hold(local.Worker);
        Check(ai.IsPursuing,"Una recogida elegible puede iniciar el sabotaje automático");
        until=Time.time+4f;while(local.Worker.HeldItem!=null && Time.time<until)yield return null;
        Check(local.Worker.HeldItem==null,"El Rival automático alcanza y taclea usando el ataque común");
        Check(UnityEngine.Object.FindAnyObjectByType<SabotageEvidence>()!=null,"El ataque automático también deja una pista investigable");
        front.ReturnToMenu();front.SelectMode(LocalMultiplayer.GameMode.SinglePlayer);front.StartSelectedGame();
        local.Worker.ResetForRound(point+Vector3.up*.75f,Quaternion.identity);
        local.Rival.ResetForRound(origin+Vector3.up*.75f,Quaternion.LookRotation(point-origin));
        Physics.SyncTransforms();ai.ResetForRound();attack.ResetForRound();
        GameObject first=Hold(local.Worker);
        local.Worker.ReleaseHeldItem();
        until=Time.time+.2f;while(Time.time<until)yield return null;
        Check(!ai.IsPursuing,"Dejar la comida cancela la persecución automática");
        UnityEngine.Object.Destroy(first);
        front.ReturnToMenu();
        Check(UnityEngine.Object.FindObjectsByType<PlayerController>().Length==2,"Cambiar de modo no duplica personajes");
    }
}
#endif
