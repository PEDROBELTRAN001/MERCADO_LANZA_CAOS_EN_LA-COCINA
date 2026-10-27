#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CharacterDesignValidation
{
    private static IEnumerator routine;
    private static int passed;
    private static double deadline;
    public static void Run()
    {
        SessionState.SetBool("CharacterDesignValidation",true);
        EditorSceneManager.OpenScene("Assets/Scenes/KitchenScene.unity");
        EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("CharacterDesignValidation",false))
            {passed=0;routine=Checks();deadline=EditorApplication.timeSinceStartup+90;EditorApplication.update+=Tick;}
        };
    }
    private static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Tiempo agotado");
            if(!routine.MoveNext())Finish(0,"PASS: "+passed+" comprobaciones de personajes finales, manos y animación.");
        }
        catch(Exception e){Finish(1,"FAIL: "+e);}
    }
    private static void Finish(int code,string result)
    {
        EditorApplication.update-=Tick;SessionState.SetBool("CharacterDesignValidation",false);
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/character-design-result.txt",result);Debug.Log(result);EditorApplication.Exit(code);
    }
    private static void Check(bool condition,string message)
    {if(!condition)throw new Exception(message);passed++;Debug.Log("PASS: "+message);}
    private static bool Has(GameObject root,string feature)
    {return root.GetComponentsInChildren<Transform>(true).Any(t=>t.name==feature);}
    private static IEnumerator Checks()
    {
        float until=Time.realtimeSinceStartup+2f;while(Time.realtimeSinceStartup<until)yield return null;
        var local=LocalMultiplayer.Instance;Check(local!=null,"Partida instalada");
        var catalog=Resources.Load<CharacterSkinCatalog>("CharacterSkins");
        string[] ids={"tomas","rival","donaRosa","cholita","capibara"};
        string[][] features={
            new[]{"Sombrero de garzón","Cinta roja del sombrero","Chaleco gris","Camisa blanca","Pluma decorativa","Bandeja de servicio","Sonrisa"},
            new[]{"Sombrero de garzón","Chaleco gris","Corbata negra","Boca seria","Bandeja de servicio"},
            new[]{"Cofia blanca","Barbijo de protección","Mandil blanco","Trenza_L","Trenza_R","Franja de aguayo","Cucharón metálico"},
            new[]{"Pollera verde","Pollera amarilla","Pollera naranja","Mandil azul","Trenza_L","Trenza_R","Canasta de mercado","Pañuelo de aguayo"},
            new[]{"Cuerpo redondo de capibara","Cabeza redonda","Silbato de policía","Gorra de policía","Banda roja","Escarapela dorada","Cuello de camisa blanca","Chaleco verde oliva","Placa policial","Equipo policial de inspección","Radio portátil","Portapapeles"}
        };
        for(int i=0;i<5;i++)
        {
            var model=catalog.Model(i);var rig=model.GetComponent<CharacterVisualRig>();
            Check(rig!=null && rig.designId==ids[i],"Catálogo enlaza diseño final "+ids[i]);
            Check(rig.torso!=null && rig.head!=null && rig.leftLeg!=null && rig.rightLeg!=null && rig.carryPoint!=null,"Articulaciones y punto de carga de "+ids[i]);
            Check(!rig.SabotageMarked,"Aspecto limpio al comenzar: "+ids[i]);
            foreach(string feature in features[i])Check(Has(model,feature),ids[i]+": "+feature);
            foreach(var mesh in model.GetComponentsInChildren<MeshFilter>(true))Check(AssetDatabase.Contains(mesh.sharedMesh),"Malla persistente: "+mesh.sharedMesh.name);
        }
        Check(local.Worker.GetComponent<CharacterAppearance>().ActiveRig.designId=="tomas","Tomás nuevo aplicado automáticamente");
        Check(local.Rival.GetComponent<CharacterAppearance>().ActiveRig.designId=="rival","Rival nuevo aplicado automáticamente");
        var police=InvestigationSystem.Instance.Police.GetComponentInChildren<CharacterVisualRig>();
        Check(police!=null && !police.quadruped && police.leftArm!=null && police.rightArm!=null && police.backLeftLeg==null && police.backRightLeg==null,"Policía usa el diseño erguido de la nueva referencia");
        var appearance=local.Rival.GetComponent<CharacterAppearance>();
        appearance.MarkSabotage();Check(appearance.ActiveRig.SabotageMarked,"El Rival puede mostrar la mancha");
        appearance.Apply(2);Check(appearance.ActiveRig.SabotageMarked,"Cambiar apariencia conserva la evidencia visual");
        local.Rival.ResetForRound(local.Rival.transform.position,local.Rival.transform.rotation);
        Check(!appearance.ActiveRig.SabotageMarked,"Reiniciar limpia el uniforme");
        local.Worker.GetComponent<CharacterAppearance>().MarkSabotage();
        Check(!local.Worker.GetComponent<CharacterAppearance>().ActiveRig.SabotageMarked,"El trabajador no adquiere manchas del impostor");
        foreach(var player in new[]{local.Worker,local.Rival})for(int i=0;i<5;i++)
        {
            var a=player.GetComponent<CharacterAppearance>();var body=player.GetComponent<CapsuleCollider>();float height=body.height;
            Check(a.Apply(i),"Selecciona "+ids[i]+" para "+player.playerName);
            Check(Vector3.Distance(player.Hand.position,a.ActiveRig.carryPoint.position)<.001f,"Objeto sigue el punto de manos de "+ids[i]);
            Check(body.height==height && body.enabled,"Apariencia conserva la cápsula jugable");
            {
                var renderers=a.ActiveRig.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
                foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                Check(bounds.size.x<=body.radius*2f+.02f && bounds.size.z<=1.37f,"El modelo redondo cabe en el ancho y fondo jugables");
            }
        }
        var windows=UnityEngine.Object.FindAnyObjectByType<FloatingWindows>();windows.showIntro=false;
        typeof(FloatingWindows).GetMethod("Close",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(windows,null);
        local.Worker.GetComponent<CharacterAppearance>().Apply(0);appearance.Apply(1);
        local.ConfigureMode(LocalMultiplayer.GameMode.LocalMultiplayer);Time.timeScale=1;GameManager.Instance.SetState(GameManager.GameState.Playing);yield return null;
        var workerAppearance=local.Worker.GetComponent<CharacterAppearance>();workerAppearance.Apply(4);yield return null;
        var capibaraRig=workerAppearance.ActiveRig;
        Check(capibaraRig.idleEquipment!=null && capibaraRig.idleEquipment.activeSelf,"La apariencia muestra el equipo de inspección al caminar sin comida");
        var capibaraItem=new GameObject("Ingrediente para capibara");capibaraItem.AddComponent<Ingredient>();
        Check(local.Worker.TryHoldItem(capibaraItem),"El nuevo policía puede transportar ingredientes");
        until=Time.time+.1f;while(Time.time<until)yield return null;
        Check(!capibaraRig.idleEquipment.activeSelf,"La bandeja decorativa se oculta para mostrar el ingrediente real");
        Check(capibaraRig.idleEquipment.GetComponentsInChildren<MeshRenderer>(true).Length>0,"El equipo conserva sus mallas dentro del grupo ocultable");
        local.Worker.ResetForRound(local.Worker.transform.position,local.Worker.transform.rotation);yield return null;
        Check(capibaraRig.idleEquipment.activeSelf,"Al soltar el ingrediente vuelve el equipo policial");
        Check(police.idleEquipment.activeSelf,"El policía del mercado mantiene su equipo");
        workerAppearance.Apply(0);
        var worker=local.Worker;worker.usesAI=true;worker.SetAIMovement(Vector3.right);
        var joint=worker.GetComponent<CharacterAppearance>().ActiveRig.leftLeg;Quaternion initial=joint.localRotation;
        until=Time.time+.3f;while(Time.time<until)yield return null;
        Check(Quaternion.Angle(initial,joint.localRotation)>.1f,"Caminar mueve las piernas del nuevo modelo");
        worker.SetAIMovement(Vector3.zero);worker.usesAI=false;
        var item=new GameObject("Ingrediente de prueba");item.AddComponent<Ingredient>();Check(worker.TryHoldItem(item),"Toma un ingrediente con la nueva apariencia");
        until=Time.time+.1f;while(Time.time<until)yield return null;
        Check(Mathf.Abs(worker.GetComponent<CharacterAppearance>().ActiveRig.leftArm.localEulerAngles.x)>1f,"Al cargar levanta los brazos");
    }
}
#endif
