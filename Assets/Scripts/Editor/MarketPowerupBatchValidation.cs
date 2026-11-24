#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Unity -batchmode -executeMethod MarketPowerupBatchValidation.Run (sin -quit).
public static class MarketPowerupBatchValidation
{
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private static Stack<IEnumerator> routines;
    private static double deadline;
    private static int passed, regression;
    private static bool[] previousEnabled;
    private static readonly List<string> errors=new List<string>();
    public static void Run()
    {
        SessionState.SetBool("MarketPowerupBatchValidation",true);Application.runInBackground=true;
        var view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));view.Show();view.Focus();
        typeof(MarketLifeValidation).GetMethod("ConfigureView",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{view});
        EditorSceneManager.OpenScene("Assets/Scenes/KitchenScene.unity");EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("MarketPowerupBatchValidation",false))return;
            passed=regression=0;errors.Clear();previousEnabled=null;Application.logMessageReceived+=Record;
            routines=new Stack<IEnumerator>();routines.Push(Checks());deadline=EditorApplication.timeSinceStartup+240;
            EditorApplication.update+=Tick;
        };
    }
    private static void Record(string message,string stack,LogType type)
    {if(type==LogType.Error || type==LogType.Exception)errors.Add(message);}
    private static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Tiempo agotado");
            if(routines.Count==0){Check(errors.Count==0,"Sin errores de ejecución");Finish(0);return;}
            var routine=routines.Peek();if(!routine.MoveNext())routines.Pop();else if(routine.Current is IEnumerator child)routines.Push(child);
        }
        catch(Exception e){Finish(1,e.ToString()+"\n"+string.Join("\n",errors));}
    }
    private static void Finish(int code,string failure="")
    {
        EditorApplication.update-=Tick;Application.logMessageReceived-=Record;
        SessionState.SetBool("MarketPowerupBatchValidation",false);Time.timeScale=1f;
        if(previousEnabled!=null && MarketAudio.Instance!=null)
            for(int i=0;i<3;i++)MarketAudio.Instance.SetEnabled((MarketAudio.Channel)i,previousEnabled[i]);
        PlayerPrefs.Save();Directory.CreateDirectory("Logs");
        string result=code==0?"PASS: "+passed+" comprobaciones de tandas y "+regression+" comprobaciones de regresión del mercado.":"FAIL: "+failure;
        File.WriteAllText("Logs/market-powerup-batch-result.txt",result);Debug.Log(result);EditorApplication.Exit(code);
    }
    private static void Check(bool condition,string message)
    {if(!condition)throw new Exception(message);passed++;Debug.Log("PASS TANDAS: "+message);}
    private static IEnumerator Wait(float seconds)
    {float until=Time.time+seconds;while(Time.time<until)yield return null;}
    private static IEnumerator RealWait(float seconds)
    {double until=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<until)yield return null;}
    private static int Active(IReadOnlyList<MarketPowerup> list)
    {int count=0;foreach(var p in list)if(p!=null && p.Available)count++;return count;}
    private static bool Collect(MarketLife market,MarketPowerup pickup)
    {
        var worker=market.Worker;
        pickup.transform.position=new Vector3(worker.transform.position.x,worker.GetComponent<Collider>().bounds.min.y,worker.transform.position.z);
        Physics.SyncTransforms();return market.TryCollect(pickup,worker);
    }
    private static IEnumerator Checks()
    {
        double until=EditorApplication.timeSinceStartup+40;
        while((MarketLife.Instance==null || MarketLife.Instance.Pedestrians.Count<36) && EditorApplication.timeSinceStartup<until)yield return null;
        var market=MarketLife.Instance;Check(market!=null && market.Layout!=null,"La navegación y el mercado están preparados");
        var windows=UnityEngine.Object.FindAnyObjectByType<FloatingWindows>();windows.showIntro=false;
        typeof(FloatingWindows).GetMethod("Close",Private).Invoke(windows,null);yield return RealWait(.2f);
        previousEnabled=new bool[3];for(int i=0;i<3;i++){previousEnabled[i]=MarketAudio.Instance.IsEnabled((MarketAudio.Channel)i);MarketAudio.Instance.SetEnabled((MarketAudio.Channel)i,false);}
        var game=GameManager.Instance;game.gameDuration=5000f;OrderSystem.Instance.firstOrderDelay=1000f;
        Check(GameFrontEnd.Instance.StartSelectedGame(),"La jornada inicia con las nuevas tandas");
        SabotageSystem.Instance.sabotageEnabled=false;Time.timeScale=4f;
        Check(MarketLife.CocaInterval==20f && MarketLife.CascaditaInterval==20f && MarketLife.PickupLifetime==10f,"Ambos tipos usan diez segundos visibles y veinte de espera");
        yield return Wait(19f);
        Check(Active(market.CocaPickups)==0 && Active(market.CascaditaPickups)==0,"No aparecen antes de cumplir los veinte segundos iniciales");
        yield return Wait(1.3f);
        Check(Active(market.CocaPickups)==3 && Active(market.CascaditaPickups)==3,"Cada tipo aparece simultáneamente en tres lugares");
        var all=new List<MarketPowerup>(market.CocaPickups);all.AddRange(market.CascaditaPickups);
        var oldPositions=new List<Vector3>();
        var nav=InvestigationSystem.Instance.Navigation;nav.Sample(market.Worker.transform.position,3f,out Vector3 origin);
        foreach(var pickup in all)
        {
            Check(pickup.Remaining>9f && pickup.Remaining<=10f,"Cada copia comienza con su ventana de diez segundos");
            Check(nav.CanReach(origin,pickup.transform.position,out _),"La ubicación sorteada se alcanza por los pasillos");
            foreach(var point in oldPositions)Check(Vector3.Distance(point,pickup.transform.position)>1.4f,"Las copias tienen ubicaciones distintas");
            oldPositions.Add(pickup.transform.position);
        }
        Check(market.CocaCountdown==0f && market.CascaditaCountdown==0f,"La espera siguiente no avanza mientras hay objetos disponibles");
        ((IList)typeof(FloatingWindows).GetField("toasts",Private).GetValue(windows)).Clear();
        Time.timeScale=1f;ScreenCapture.CaptureScreenshot("Logs/market-three-powerups.png");yield return RealWait(.3f);
        game.TogglePause();float remaining=market.Coca.Remaining;yield return RealWait(.4f);
        Check(Active(market.CocaPickups)==3 && Active(market.CascaditaPickups)==3 && Mathf.Abs(market.Coca.Remaining-remaining)<.01f,"La pausa congela las seis copias");
        game.TogglePause();Time.timeScale=4f;yield return Wait(10f);
        Check(Active(market.CocaPickups)==0 && Active(market.CascaditaPickups)==0,"Las seis copias caducan al completar sus diez segundos");
        Check(market.CocaCountdown>18f && market.CascaditaCountdown>18f,"Al caducar comienza una espera nueva de veinte segundos");
        yield return Wait(18f);
        Check(market.Coca==null && market.Cascadita==null && market.CocaCountdown>0f,"Durante la espera no aparecen copias de reemplazo");
        yield return Wait(2f);
        Check(Active(market.CocaPickups)==3 && Active(market.CascaditaPickups)==3,"La tanda siguiente vuelve a crear tres copias de cada tipo");
        Check(Vector3.Distance(oldPositions[0],market.Coca.transform.position)>.01f,"Las ubicaciones se vuelven a sortear");
        var first=market.Coca;Check(Collect(market,first),"Tomás puede recoger una copia");
        Check(Active(market.CocaPickups)==2 && Active(market.CascaditaPickups)==3,"Recoger una copia conserva las otras ubicaciones y el otro tipo");
        Check(!market.TryCollect(first,market.Worker) && market.Worker.CocaLeaves==1,"Una copia no entrega su efecto dos veces");
        Check(!market.TryCollect(market.Cascadita,LocalMultiplayer.Instance.Rival),"El Rival sigue sin poder recoger las copias");
        game.RestartGame();yield return Wait(.2f);
        Check(market.CocaPickups.Count==0 && market.CascaditaPickups.Count==0 && market.Worker.CocaLeaves==0,"Reiniciar limpia todas las copias y las bonificaciones");
        Check(market.CocaCountdown>19f && market.CascaditaCountdown>19f,"Reiniciar restablece ambas esperas");
        Time.timeScale=4f;yield return Wait(20.1f);
        foreach(var pickup in new List<MarketPowerup>(market.CocaPickups))Check(Collect(market,pickup),"Se pueden recoger las tres hojas por separado");
        yield return Wait(.2f);
        Check(market.Worker.CocaLeaves==3 && market.Coca==null && market.CocaCountdown>19f,"Agotar las tres copias inicia veinte segundos de espera para ese tipo");
        Check(Active(market.CascaditaPickups)==3 && market.CascaditaCountdown==0f,"El tipo que sigue visible mantiene su propia tanda");
        yield return Wait(10f);
        Check(market.CocaCountdown<11f && market.CascaditaCountdown>18f,"Los tiempos de espera de los dos tipos son independientes");
        yield return Wait(10.2f);
        Check(Active(market.CocaPickups)==3 && market.Cascadita==null,"Recoger las tres hojas permite que su siguiente tanda aparezca antes");
        game.RestartGame();yield return Wait(.2f);
        Check(market.GetComponentsInChildren<MarketPowerup>(true).Length==0,"El reinicio no deja copias huérfanas ni marcadores antiguos");
        GameFrontEnd.Instance.ReturnToMenu();Time.timeScale=1f;yield return RealWait(.2f);
        var staticPrivate=BindingFlags.Static|BindingFlags.NonPublic;
        typeof(MarketLifeValidation).GetField("passed",staticPrivate).SetValue(null,0);
        yield return (IEnumerator)typeof(MarketLifeValidation).GetMethod("Checks",staticPrivate).Invoke(null,null);
        regression=(int)typeof(MarketLifeValidation).GetField("passed",staticPrivate).GetValue(null);
        Check(regression>=67,"La cámara, NPC, misiones, audio y recogidas mantienen sus comprobaciones anteriores");
    }
}
#endif
