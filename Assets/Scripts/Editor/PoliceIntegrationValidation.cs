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
using UnityEngine.AI;

public static class PoliceIntegrationValidation
{
    private static Stack<IEnumerator> routines;private static int passed;private static double deadline;
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    public static void Run(){SessionState.SetBool("PoliceIntegrationValidation",true);EditorSceneManager.OpenScene("Assets/Scenes/KitchenScene.unity");EditorApplication.EnterPlaymode();}
    [InitializeOnLoadMethod]private static void Init()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("PoliceIntegrationValidation",false))
            {passed=0;routines=new Stack<IEnumerator>();routines.Push(Checks());deadline=EditorApplication.timeSinceStartup+150;EditorApplication.update+=Tick;}
        };
    }
    private static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Tiempo agotado");
            if(routines.Count==0){Finish(0,"PASS: "+passed+" comprobaciones de evidencias y captura en ambos modos.");return;}
            var routine=routines.Peek();if(!routine.MoveNext())routines.Pop();else if(routine.Current is IEnumerator child)routines.Push(child);
        }
        catch(Exception e){Finish(1,"FAIL: "+e);}
    }
    private static void Finish(int code,string result)
    {
        EditorApplication.update-=Tick;SessionState.SetBool("PoliceIntegrationValidation",false);Directory.CreateDirectory("Logs");File.WriteAllText("Logs/police-integration-result.txt",result);Debug.Log(result);EditorApplication.Exit(code);
    }
    private static void Check(bool condition,string text){if(!condition)throw new Exception(text);passed++;Debug.Log("PASS: "+text);}
    private static IEnumerator Wait(float seconds){float until=Time.time+seconds;while(Time.time<until)yield return null;}
    private static Vector3 RootAt(PlayerController actor,Vector3 floor)
    {
        var body=actor.GetComponent<CapsuleCollider>();return floor+Vector3.up*(body.height*.5f-body.center.y+.025f);
    }
    private static bool MoveNear(PlayerController actor,Interactable target,InvestigationSystem system,float distance)
    {
        for(int i=0;i<20;i++)
        {
            float a=i*Mathf.PI/10;var desired=target.transform.position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*distance;
            if(!system.Navigation.Sample(desired,1f,out Vector3 floor))continue;
            actor.Teleport(RootAt(actor,floor));Physics.SyncTransforms();
            if(actor.CanInspect(target) && Vector3.Distance(actor.transform.position,target.transform.position+Vector3.up*.7f)<2)return true;
        }
        return false;
    }
    private static bool PutRivalInReach(InvestigationSystem system)
    {
        var rival=LocalMultiplayer.Instance.Rival;Vector3 start=system.Police.transform.position;
        for(int i=0;i<32;i++)
        {
            float a=i*Mathf.PI/16;var desired=start+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*3.1f;
            if(!system.Navigation.CanReach(start,desired,out Vector3 floor) || Vector3.Distance(start,floor)<2.4f)continue;
            rival.Teleport(RootAt(rival,floor));Physics.SyncTransforms();if(system.Police.CanPursue(rival))return true;
        }
        return false;
    }
    private static IEnumerator Checks()
    {
        float until=Time.realtimeSinceStartup+3;while(Time.realtimeSinceStartup<until)yield return null;
        var local=LocalMultiplayer.Instance;var front=GameFrontEnd.Instance;var system=InvestigationSystem.Instance;
        Check(system!=null && system.Navigation!=null && system.Navigation.Ready && system.Police!=null,"Capibara y rutas disponibles sin modificar la escena");
        Check(system.Police.GetComponentInChildren<CharacterVisualRig>()!=null,"El policía utiliza el modelo aprobado");
        Check(system.exileSeconds==8,"La suspensión dura ocho segundos de juego");
        var windows=UnityEngine.Object.FindAnyObjectByType<FloatingWindows>();windows.showIntro=false;typeof(FloatingWindows).GetMethod("Close",Private).Invoke(windows,null);yield return null;yield return null;
        system.exileSeconds=.7f;system.reportCooldown=.05f;system.Police.patrolSpeed=0;system.Police.GetComponent<NavMeshAgent>().speed=0;
        front.SelectMode(LocalMultiplayer.GameMode.LocalMultiplayer);Check(front.StartSelectedGame(),"Inicia multijugador para probar al Rival humano");yield return Wait(1.4f);
        Check(PutRivalInReach(system),"El Rival humano está en una zona conectada al policía");
        Check(MoveNear(local.Worker,system.Police,system,.8f),"Tomás puede acercarse al capibara para denunciar");
        Check(!system.Report(local.Worker) && !system.Police.IsChasing,"Una denuncia sin evidencia no inicia persecución");
        Check(!local.Worker.IsDetained && !local.Rival.IsDetained,"La denuncia sin prueba no suspende a nadie");
        var invalid=system.Ledger.Register(local.Worker.InvestigationId,local.Rival.InvestigationId,"no atribuido al Rival");
        system.Police.Pursue(local.Worker,invalid.Id);
        Check(!system.Police.IsChasing && !system.Arrest(local.Worker,invalid.Id),"El capibara rechaza perseguir o detener a Tomás");
        yield return Wait(.1f);
        var rival=local.Rival;var cooker=GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<CookingStation>();
        cooker.transform.position=rival.transform.position+Vector3.right*.8f;cooker.transform.localScale=Vector3.one*.3f;Physics.SyncTransforms();
        int count=SabotageSystem.Instance.SabotageCount;SabotageSystem.Instance.Sabotage(cooker,local.Worker);
        Check(SabotageSystem.Instance.SabotageCount==count,"El sistema rechaza atribuir un sabotaje a Tomás");
        typeof(PlayerController).GetField("currentInteractable",Private).SetValue(rival,cooker);
        Check(rival.GetComponent<LocalRivalDriver>().TrySabotage(),"El Rival humano comete el sabotaje existente");
        var evidence=UnityEngine.Object.FindAnyObjectByType<SabotageEvidence>();
        Check(evidence!=null && evidence.attacker==rival && evidence.IncidentId>0,"El sabotaje crea evidencia atribuida al responsable");
        evidence.Interact(rival);Check(system.Ledger.AvailableProof(rival.InvestigationId,rival.InvestigationId)==null,"El saboteador no puede validar su propia evidencia");
        yield return CaptureCase(system,evidence,windows,"humano");
        front.ReturnToMenu();yield return null;yield return null;
        Check(!rival.IsDetained && system.Ledger.Incidents.Count==0 && !system.Police.IsChasing,"Reiniciar limpia evidencias y recupera al Rival");
        front.SelectMode(LocalMultiplayer.GameMode.SinglePlayer);SabotageSystem.Instance.firstSabotageAfter=.15f;
        Check(front.StartSelectedGame() && rival.usesAI,"Inicia individual con Rival automático");
        until=Time.time+4;while(SabotageSystem.Instance.SabotageCount==0 && Time.time<until)yield return null;
        Check(SabotageSystem.Instance.SabotageCount>0,"El sabotaje automático se ejecuta en individual");
        evidence=UnityEngine.Object.FindAnyObjectByType<SabotageEvidence>();
        Check(evidence!=null && evidence.attacker==rival && system.Ledger.Find(evidence.IncidentId).Attacker==rival.InvestigationId,"La evidencia automática identifica al Rival, no al personaje cercano");
        yield return CaptureCase(system,evidence,windows,"automático");
        front.ReturnToMenu();yield return null;Check(!rival.IsDetained && !local.Worker.IsDetained,"Volver al menú habilita correctamente a ambos participantes");
    }
    private static IEnumerator CaptureCase(InvestigationSystem system,SabotageEvidence evidence,FloatingWindows windows,string label)
    {
        var local=LocalMultiplayer.Instance;var rival=local.Rival;
        Check(MoveNear(local.Worker,evidence,system,.65f),"Tomás alcanza el rastro del Rival "+label);
        evidence.Interact(local.Worker);var proof=system.Ledger.AvailableProof(local.Worker.InvestigationId,rival.InvestigationId);
        Check(proof!=null && proof.Id==evidence.IncidentId,"Examinar registra una prueba verificable del Rival "+label);
        Check(PutRivalInReach(system),"Hay una ruta de persecución al Rival "+label);
        Check(MoveNear(local.Worker,system.Police,system,1.1f),"Tomás presenta la denuncia al policía");
        yield return Wait(.08f);
        var held=new GameObject("Ingrediente de Tomás");held.AddComponent<Ingredient>();held.transform.SetParent(local.Worker.Hand,false);
        Check(system.Report(local.Worker) && system.Police.IsChasing,"La prueba inicia la persecución del Rival "+label);
        Check(!rival.IsDetained && !system.Arrest(local.Worker,proof.Id),"Iniciar la denuncia no captura a distancia ni suspende a Tomás");
        float until=Time.time+15;while(!rival.IsDetained && Time.time<until)yield return null;
        Check(rival.IsDetained,"El capibara alcanza físicamente y suspende al Rival "+label);
        Check(!local.Worker.IsDetained && held!=null && held.transform.parent==local.Worker.Hand,"Tomás conserva controles e ingrediente durante la suspensión");
        Check(system.Ledger.Find(proof.Id).Used && system.Ledger.ExpulsionsOf(rival.InvestigationId)==1,"La captura consume la evidencia una sola vez");
        Check(!rival.GetComponent<LocalRivalDriver>().TrySabotage() && !rival.GetComponent<CapsuleCollider>().enabled,"El saboteador suspendido no puede actuar");
        if(rival.usesAI)
        {
            typeof(SabotageSystem).GetField("nextSabotage",Private).SetValue(SabotageSystem.Instance,0f);
            int before=SabotageSystem.Instance.SabotageCount;yield return Wait(.12f);
            Check(SabotageSystem.Instance.SabotageCount==before,"El Rival automático tampoco sabotea durante su suspensión");
        }
        var page=typeof(FloatingWindows).GetNestedType("Page",BindingFlags.NonPublic);
        typeof(FloatingWindows).GetMethod("Open",Private).Invoke(windows,new object[]{Enum.Parse(page,"Cards"),false});float remaining=system.ReturnSeconds;
        until=Time.realtimeSinceStartup+.2f;while(Time.realtimeSinceStartup<until)yield return null;
        Check(rival.IsDetained && Mathf.Abs(system.ReturnSeconds-remaining)<.02f,"Las fichas pausan la cuenta de suspensión");
        typeof(FloatingWindows).GetMethod("Close",Private).Invoke(windows,null);yield return null;
        until=Time.time+2;while(rival.IsDetained && Time.time<until)yield return null;
        Check(!rival.IsDetained && rival.GetComponent<CapsuleCollider>().enabled,"El Rival "+label+" regresa al terminar la suspensión");
        Check(rival.GetComponent<LocalRivalDriver>().CooldownRemaining>0,"El Rival vuelve con una espera antes de sabotear");
        system.Police.Pursue(rival,proof.Id);Check(!system.Police.IsChasing && !system.Ledger.Reserve(proof.Id,local.Worker.InvestigationId,rival.InvestigationId),"Una prueba consumida no permite capturar otra vez");
        UnityEngine.Object.Destroy(held);yield return null;
    }
}
#endif
