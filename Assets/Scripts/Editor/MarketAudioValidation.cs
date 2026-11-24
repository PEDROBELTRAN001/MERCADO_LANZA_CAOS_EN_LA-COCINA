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

// Unity -batchmode -executeMethod MarketAudioValidation.Run (sin -quit).
public static class MarketAudioValidation
{
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private static Stack<IEnumerator> routines;
    private static double deadline;
    private static int passed;
    private static float[] previousVolumes;
    private static bool[] previousEnabled;
    private static readonly List<string> errors=new List<string>();
    public static void Run()
    {
        SessionState.SetBool("MarketAudioValidation",true); Application.runInBackground=true;
        var view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")); view.Show();view.Focus();
        typeof(MarketLifeValidation).GetMethod("ConfigureView",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{view});
        EditorSceneManager.OpenScene("Assets/Scenes/KitchenScene.unity"); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("MarketAudioValidation",false))return;
            errors.Clear(); passed=0; previousVolumes=null;
            Application.logMessageReceived+=RecordError;
            routines=new Stack<IEnumerator>();routines.Push(Checks());
            deadline=EditorApplication.timeSinceStartup+240; EditorApplication.update+=Tick;
        };
    }
    private static void RecordError(string message,string stack,LogType type)
    {if(type==LogType.Error || type==LogType.Exception)errors.Add(message);}
    private static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Tiempo agotado");
            if(routines.Count==0){Check(errors.Count==0,"Sin errores de ejecución durante la prueba");Finish(0,"PASS: "+passed+" comprobaciones de audio, pausa y minimapa.");return;}
            var routine=routines.Peek();if(!routine.MoveNext())routines.Pop();else if(routine.Current is IEnumerator child)routines.Push(child);
        }
        catch(Exception error){Finish(1,"FAIL tras "+passed+" comprobaciones: "+error+"\n"+string.Join("\n",errors));}
    }
    private static void Finish(int code,string result)
    {
        EditorApplication.update-=Tick;Application.logMessageReceived-=RecordError;
        SessionState.SetBool("MarketAudioValidation",false);
        var audio=MarketAudio.Instance;
        if(audio!=null && previousVolumes!=null)
            for(int i=0;i<3;i++){audio.SetVolume((MarketAudio.Channel)i,previousVolumes[i]);audio.SetEnabled((MarketAudio.Channel)i,previousEnabled[i]);}
        PlayerPrefs.Save();Directory.CreateDirectory("Logs");File.WriteAllText("Logs/market-audio-result.txt",result);
        Debug.Log(result);EditorApplication.Exit(code);
    }
    private static void Check(bool condition,string message){if(!condition)throw new Exception(message);passed++;Debug.Log("PASS AUDIO: "+message);}
    private static void Set(object target,string field,object value)=>target.GetType().GetField(field,Private).SetValue(target,value);
    private static IEnumerator Wait(float seconds)
    {double end=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<end)yield return null;}
    private static IEnumerator Until(Func<bool> condition,float timeout=15f)
    {
        double end=EditorApplication.timeSinceStartup+timeout;
        while(!condition() && EditorApplication.timeSinceStartup<end)yield return null;
        Check(condition(),"Se alcanza el estado esperado dentro del plazo");
    }
    private static void Key(MarketPauseMenu menu,Key key)
    {
        var keyboard=InputSystem.AddDevice<Keyboard>();
        var old=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));InputSystem.Update();
        typeof(MarketPauseMenu).GetMethod("Update",Private).Invoke(menu,null);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
        InputSystem.RemoveDevice(keyboard);InputSystem.settings.editorInputBehaviorInPlayMode=old;
    }
    private static IEnumerator Checks()
    {
        yield return Until(()=>MarketLife.Instance!=null && MarketLife.Instance.Layout!=null && MarketLife.Instance.Pedestrians.Count==36,45f);
        var windows=UnityEngine.Object.FindAnyObjectByType<FloatingWindows>();windows.showIntro=false;
        typeof(FloatingWindows).GetMethod("Close",Private).Invoke(windows,null);yield return Wait(.3f);
        var audio=MarketAudio.Instance;var menu=UnityEngine.Object.FindAnyObjectByType<MarketPauseMenu>();
        var game=GameManager.Instance;var orders=OrderSystem.Instance;var market=MarketLife.Instance;var worker=market.Worker;
        Check(audio!=null && menu!=null,"Audio y menú de pausa conectados automáticamente");
        Check(UnityEngine.Object.FindObjectsByType<MarketAudio>().Length==1,"Solo existe un sistema de audio");
        int listeners=0;foreach(var listener in UnityEngine.Object.FindObjectsByType<AudioListener>())if(listener.enabled)listeners++;
        Check(listeners==1,"Una sola escucha de audio para ambas cámaras");
        previousVolumes=new float[3];previousEnabled=new bool[3];
        for(int i=0;i<3;i++){var channel=(MarketAudio.Channel)i;previousVolumes[i]=audio.Volume(channel);previousEnabled[i]=audio.IsEnabled(channel);audio.SetEnabled(channel,true);audio.SetVolume(channel,.4f);}
        Check(audio.BackgroundSource.clip.name=="mercado" && audio.BackgroundSource.loop && audio.BackgroundSource.isPlaying,"El mercado se reproduce continuamente en bucle");
        foreach(var source in audio.GetComponentsInChildren<AudioSource>())Check(source.spatialBlend==0f && !source.playOnAwake,"Fuentes 2D controladas por sus categorías");
        Check(audio.MissionSource.clip.length>0 && audio.PickupSource.clip.length>0 && audio.LossSource.clip.length>0,"Los tres efectos suministrados se importaron correctamente");
        Check(Directory.GetFiles(MarketAudio.MusicFolder,"*.mp3").Length>=1,"La carpeta de radio contiene la canción de Milo J");
        var minimap=UnityEngine.Object.FindAnyObjectByType<MarketMinimap>();
        Check(((Texture2D)typeof(MarketMinimap).GetField("map",Private).GetValue(minimap)).width==512,"El plano ampliado utiliza una textura de 512 píxeles");
        Check(MarketLife.PickupLifetime==10f,"Ambos potenciadores permanecen diez segundos");
        game.gameDuration=5000f;orders.firstOrderDelay=1000f;
        Check(GameFrontEnd.Instance.StartSelectedGame(),"Comienza la jornada con el menú existente");
        if(SabotageSystem.Instance!=null)SabotageSystem.Instance.sabotageEnabled=false;
        Check(audio.Phase==MarketAudio.RadioPhase.Transition && audio.RadioSource.clip.name=="radio_transicion","La radio comienza con la transición principal completa");
        yield return Until(()=>audio.Phase==MarketAudio.RadioPhase.Song);yield return Wait(.3f);
        Check(audio.RadioSource.isPlaying && audio.CurrentSongName=="Milo j - Ama De Mi Sol","La radio carga y reproduce el MP3 desde su carpeta");
        Check(audio.SecondsRemaining>19f && audio.SecondsRemaining<=20f,"Cada fragmento de canción dispone de veinte segundos");
        Check(audio.SegmentStart>=0f && audio.SegmentStart<=audio.RadioSource.clip.length-20f,"El punto de inicio está dentro del rango aleatorio válido");
        Check(Mathf.Abs(audio.RadioSource.time-audio.SegmentStart-.3f)<.7f,"La canción comienza en el punto seleccionado");
        typeof(OrderSystem).GetMethod("GenerateNewOrder",Private).Invoke(orders,null);
        Check(orders.GetActiveOrders().Count>0 && audio.MissionSource.isPlaying,"Cada misión nueva reproduce la campana");
        Set(market,"cocaTimer",19.95f);yield return Wait(.2f);
        var coca=market.Coca;Check(coca!=null,"Aparece una hoja para comprobar el efecto de recogida");
        coca.transform.position=new Vector3(worker.transform.position.x,worker.GetComponent<Collider>().bounds.min.y,worker.transform.position.z);
        Physics.SyncTransforms();Check(market.TryCollect(coca,worker) && audio.PickupSource.isPlaying,"Recoger un potenciador reproduce jump");
        Set(market,"cascTimer",19.95f);yield return Wait(.2f);var casc=market.Cascadita;
        Check(casc!=null && casc.Remaining>9.5f,"Cascadita también comienza con diez segundos");
        yield return Wait(5.1f);Check(casc!=null && casc.Available,"El potenciador permanece después de los cinco segundos anteriores");
        Key(menu,UnityEngine.InputSystem.Key.Escape);yield return Wait(.2f);
        Check(menu.IsOpen && game.CurrentState==GameManager.GameState.Paused && Time.timeScale==0f && !PlayerController.GameplayActive,"Escape abre la pausa y bloquea el movimiento");
        float clock=game.GetTimeRemaining(),left=casc.Remaining,radioLeft=audio.SecondsRemaining,radioTime=audio.RadioSource.time;
        yield return Wait(.4f);
        Check(Mathf.Abs(clock-game.GetTimeRemaining())<.01f && Mathf.Abs(left-casc.Remaining)<.01f,"La pausa conserva el reloj y la duración de objetos");
        Check(!audio.RadioSource.isPlaying && !audio.BackgroundSource.isPlaying && Mathf.Abs(radioLeft-audio.SecondsRemaining)<.01f && Mathf.Abs(radioTime-audio.RadioSource.time)<.03f,"La radio y el ambiente se pausan sin perder posición");
        ScreenCapture.CaptureScreenshot("Logs/market-pause.png");yield return Wait(.6f);
        menu.ShowVolumes();Check(menu.VolumesOpen,"Volúmenes abre los tres controles independientes");
        foreach(MarketAudio.Channel channel in Enum.GetValues(typeof(MarketAudio.Channel)))
        {
            audio.SetEnabled(channel,false);
            if(channel==MarketAudio.Channel.Radio)Check(audio.RadioSource.volume==0f && audio.BackgroundSource.volume>0f,"Silenciar radio conserva ambiente y efectos");
            if(channel==MarketAudio.Channel.Background)Check(audio.BackgroundSource.volume==0f && audio.RadioSource.volume>0f,"Silenciar ambiente conserva la radio");
            if(channel==MarketAudio.Channel.Effects)Check(audio.MissionSource.volume==0f && audio.PickupSource.volume==0f && audio.LossSource.volume==0f,"Silenciar efectos afecta a campana, recogida y derrota");
            audio.SetVolume(channel,.37f);audio.SetEnabled(channel,true);
            Check(Mathf.Abs(audio.Volume(channel)-.37f)<.001f && PlayerPrefs.GetFloat("Mercado.Audio.Volume."+(int)channel)==.37f,"Volumen y activación quedan guardados");
        }
        ScreenCapture.CaptureScreenshot("Logs/market-volumes.png");yield return Wait(.6f);
        Key(menu,UnityEngine.InputSystem.Key.Escape);Check(menu.IsOpen && !menu.VolumesOpen,"Escape vuelve desde volúmenes al menú de pausa");
        Key(menu,UnityEngine.InputSystem.Key.P);yield return Wait(.3f);
        Check(!menu.IsOpen && Time.timeScale==1f && audio.RadioSource.isPlaying && audio.BackgroundSource.isPlaying,"P reanuda la jornada y los audios");
        Check(audio.SecondsRemaining<radioLeft && casc.Remaining<left,"Al reanudar continúan los mismos fragmentos y objetos");
        yield return Wait(5.1f);Check(casc==null || !casc.Available,"El objeto desaparece al completar diez segundos de juego activo");
        ((IList)typeof(FloatingWindows).GetField("toasts",Private).GetValue(windows)).Clear();
        ScreenCapture.CaptureScreenshot("Logs/market-larger-minimap.png");yield return Wait(.6f);
        var oldSong=audio.RadioSource.clip;
        yield return Until(()=>audio.Phase==MarketAudio.RadioPhase.Transition,22f);
        Check(oldSong==null && audio.RadioSource.clip.name=="radio_transicion","A los veinte segundos libera la canción y vuelve a la transición");
        yield return Until(()=>audio.Phase==MarketAudio.RadioPhase.Song);
        Check(audio.SecondsRemaining>19f && audio.RadioSource.isPlaying,"El ciclo reproduce otra selección aleatoria");
        FloatingWindows.OpenHistory();yield return Wait(.2f);float modalLeft=audio.SecondsRemaining;
        yield return Wait(.2f);Check(!audio.RadioSource.isPlaying && Mathf.Abs(modalLeft-audio.SecondsRemaining)<.01f,"Las ventanas de historia también pausan el audio");
        typeof(FloatingWindows).GetMethod("Close",Private).Invoke(windows,null);yield return Wait(.2f);
        menu.Open();menu.Restart();yield return Wait(.2f);
        Check(game.CurrentState==GameManager.GameState.Playing && game.GetTimeRemaining()>4999f && worker.CocaLeaves==0,"Volver a jugar reinicia el turno y sus bonificaciones");
        Check(audio.Phase==MarketAudio.RadioPhase.Transition && audio.RadioSource.time<1f,"Reiniciar comienza otra vez por la transición");
        Check(audio.Volume(MarketAudio.Channel.Radio)==.37f && audio.IsEnabled(MarketAudio.Channel.Radio),"Reiniciar conserva los ajustes de audio");
        game.RegisterOrderLost();game.RegisterOrderLost();game.RegisterOrderLost();
        Check(game.CurrentState==GameManager.GameState.GameOver && !game.WorkersWon && audio.LossSource.isPlaying && audio.LossSource.clip.name=="error","Perder reproduce error una vez");
        Check(audio.Phase==MarketAudio.RadioPhase.Idle && !audio.RadioSource.isPlaying && audio.BackgroundSource.isPlaying,"La radio termina con la partida y el ambiente continúa");
        game.RestartGame();yield return Wait(.2f);
        typeof(GameManager).GetMethod("FinishRound",Private).Invoke(game,new object[]{true,"Validación de victoria"});
        Check(game.WorkersWon && !audio.LossSource.isPlaying,"Ganar no reproduce el sonido de derrota");
        GameFrontEnd.Instance.ReturnToMenu();yield return Wait(.2f);
        Check(audio.Phase==MarketAudio.RadioPhase.Idle && audio.BackgroundSource.isPlaying,"Volver al menú mantiene solo el ambiente");
    }
}
#endif
