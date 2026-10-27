#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DocumentInterfaceValidation
{
    private static IEnumerator routine;private static double deadline;private static int passed;
    private const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    public static void Run()
    {
        SessionState.SetBool("DocumentInterfaceValidation",true);
        EditorSceneManager.OpenScene("Assets/Scenes/KitchenScene.unity");
        var view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));view.Show();view.Focus();view.Repaint();
        EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] private static void Initialize()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("DocumentInterfaceValidation",false))
            {passed=0;routine=Checks();deadline=EditorApplication.timeSinceStartup+100;EditorApplication.update+=Tick;}
        };
    }
    private static void Tick()
    {
        try{if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Tiempo agotado");if(!routine.MoveNext())Finish(0,"PASS: "+passed+" comprobaciones de fotos, cinco fichas y currículum.");}
        catch(Exception e){Finish(1,"FAIL: "+e);}
    }
    private static void Finish(int code,string result)
    {
        EditorApplication.update-=Tick;SessionState.SetBool("DocumentInterfaceValidation",false);
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/document-interface-result.txt",result);Debug.Log(result);EditorApplication.Exit(code);
    }
    private static void Check(bool condition,string text){if(!condition)throw new Exception(text);passed++;Debug.Log("PASS: "+text);}
    private static void Open(FloatingWindows windows,string page)
    {
        var type=typeof(FloatingWindows).GetNestedType("Page",BindingFlags.NonPublic);
        typeof(FloatingWindows).GetMethod("Open",Private).Invoke(windows,new object[]{Enum.Parse(type,page),false});
    }
    private static Bounds BoundsOf(GameObject model)
    {var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);return bounds;}
    private static IEnumerator Checks()
    {
        Directory.CreateDirectory("Logs/DocumentUI");float until=Time.realtimeSinceStartup+2;while(Time.realtimeSinceStartup<until)yield return null;
        for(int i=0;i<5;i++)
        {
            Check(CharacterPortraitLibrary.Get(i)!=null && CharacterPortraitLibrary.Get(i,true)!=null,"Fotos de cuerpo y rostro disponibles para "+CharacterSkinCatalog.Names[i]);
            Check(CharacterPortraitLibrary.Get(i,true).width>=512,"Retrato nítido de "+CharacterSkinCatalog.Names[i]);
            using(var library=new KitchenVisualLibrary())Check(library.SkinPicture(i) is Texture2D,"El selector usa una foto persistente: "+CharacterSkinCatalog.Names[i]);
        }
        Check(CharacterPortraitLibrary.Get(-1)==null && CharacterPortraitLibrary.Get(5)==null,"Rechaza fotos inexistentes");
        var catalog=Resources.Load<CharacterSkinCatalog>("CharacterSkins");var waiter=BoundsOf(catalog.tomas);var rival=BoundsOf(catalog.rival);var cook=BoundsOf(catalog.donaRosa);
        Check(Mathf.Abs(waiter.size.y-rival.size.y)<.025f && Mathf.Abs(waiter.size.x-rival.size.x)<.025f,"Tomás y Rival comparten proporciones");
        Check(cook.size.y<waiter.size.y && cook.size.x>waiter.size.x,"Doña Rosa es más baja y ancha que el garzón");
        var windows=UnityEngine.Object.FindAnyObjectByType<FloatingWindows>();
        var characters=(Array)typeof(FloatingWindows).GetField("Characters",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
        Check(characters.Length==5,"La introducción incluye a los cinco personajes del PDF");
        Open(windows,"Curriculum");
        until=Time.realtimeSinceStartup+2f;while(Time.realtimeSinceStartup<until)yield return null;
        if(RuntimePreview.Available)ScreenCapture.CaptureScreenshot("Logs/DocumentUI/Curriculum.png");
        until=Time.realtimeSinceStartup+.6f;while(Time.realtimeSinceStartup<until)yield return null;
        if(RuntimePreview.Available)Check(File.Exists("Logs/DocumentUI/Curriculum.png"),"Captura real del currículum guardada");
        Open(windows,"Cards");
        for(int i=0;i<5;i++)
        {
            typeof(FloatingWindows).GetField("index",Private).SetValue(windows,i);
            until=Time.realtimeSinceStartup+.45f;while(Time.realtimeSinceStartup<until)yield return null;
            if(RuntimePreview.Available)ScreenCapture.CaptureScreenshot("Logs/DocumentUI/Ficha_"+i+".png");
            until=Time.realtimeSinceStartup+.8f;while(Time.realtimeSinceStartup<until)yield return null;
            if(RuntimePreview.Available)Check(File.Exists("Logs/DocumentUI/Ficha_"+i+".png"),"Captura real de ficha "+i+" guardada");
        }
        typeof(FloatingWindows).GetMethod("Close",Private).Invoke(windows,null);
        until=Time.realtimeSinceStartup+.6f;while(Time.realtimeSinceStartup<until)yield return null;
        if(RuntimePreview.Available)ScreenCapture.CaptureScreenshot("Logs/DocumentUI/Menu.png");
        until=Time.realtimeSinceStartup+.6f;while(Time.realtimeSinceStartup<until)yield return null;
    }
}
#endif
