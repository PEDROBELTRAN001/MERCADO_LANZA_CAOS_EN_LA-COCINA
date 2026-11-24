using UnityEngine;
using UnityEngine.InputSystem;

// Conserva el estilo verde/aguayo del HUD y utiliza el mismo estado de pausa del juego.
[DefaultExecutionOrder(-100)]
public class MarketPauseMenu : MonoBehaviour
{
    public bool VolumesOpen { get; private set; }
    public bool IsOpen => GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Paused;
    private GUIStyle title, subtitle, button, label, caption;
    private readonly Color panel = new Color(.04f,.15f,.13f,.99f);
    private readonly Color card = new Color(.08f,.23f,.20f);
    private readonly Color gold = new Color(.98f,.72f,.30f);
    private readonly Color ink = new Color(.94f,.97f,.89f);

    private void Update()
    {
        if (Keyboard.current == null || FloatingWindows.BlocksInput) return;
        var keyboard = Keyboard.current;
        if (keyboard.escapeKey.wasPressedThisFrame && IsOpen && VolumesOpen) { VolumesOpen = false; return; }
        if (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)
        {
            if (IsOpen) Resume(); else Open();
        }
    }
    public void Open()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing || FloatingWindows.BlocksInput) return;
        VolumesOpen = false; GameManager.Instance.TogglePause();
    }
    public void Resume()
    {
        if (!IsOpen) return;
        VolumesOpen = false; GameManager.Instance.TogglePause();
    }
    public void Restart()
    {
        if (!IsOpen) return;
        VolumesOpen = false; GameManager.Instance.RestartGame();
    }
    public void ShowVolumes() { if (IsOpen) VolumesOpen = true; }
    private void Styles()
    {
        if (title != null) return;
        title = new GUIStyle(GUI.skin.label) { fontSize=38, fontStyle=FontStyle.Bold, alignment=TextAnchor.MiddleCenter, wordWrap=false };
        subtitle = new GUIStyle(title) { fontSize=18, fontStyle=FontStyle.Normal };
        label = new GUIStyle(GUI.skin.label) { fontSize=22, fontStyle=FontStyle.Bold, alignment=TextAnchor.MiddleLeft };
        caption = new GUIStyle(label) { fontSize=16, fontStyle=FontStyle.Normal };
        button = new GUIStyle(GUI.skin.button) { fontSize=22, fontStyle=FontStyle.Bold, alignment=TextAnchor.MiddleCenter };
        title.normal.textColor=gold; subtitle.normal.textColor=ink;
        label.normal.textColor=caption.normal.textColor=ink;
        button.normal.textColor=button.hover.textColor=button.active.textColor=button.focused.textColor=ink;
    }
    private bool Button(Rect rect, string text, bool primary=false)
    {
        bool hover=rect.Contains(Event.current.mousePosition);
        MarketDrawing.Fill(new Rect(rect.x,rect.y+4,rect.width,rect.height),new Color(0,.03f,.02f));
        MarketDrawing.Fill(rect,primary?(hover?new Color(.24f,.45f,.28f):new Color(.15f,.34f,.23f)):(hover?new Color(.16f,.36f,.30f):card));
        MarketDrawing.Fill(new Rect(rect.x,rect.y,4,rect.height),primary?gold:AndeanUI.Green);
        Color old=GUI.backgroundColor; GUI.backgroundColor=Color.clear;
        bool clicked=GUI.Button(rect,text,button); GUI.backgroundColor=old;
        return clicked;
    }
    private void OnGUI()
    {
        if (GameManager.Instance == null || FloatingWindows.IsOpen) return;
        Styles();
        if (!IsOpen)
        {
            if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            {
                float s=Mathf.Clamp(Screen.height/1080f,.8f,1.25f);
                int previous=button.fontSize; button.fontSize=Mathf.RoundToInt(15*s);
                int oldDepth=GUI.depth; GUI.depth=-20;
                if (Button(new Rect(Screen.width-200*s,14*s,182*s,36*s),"PAUSA · ESC / P")) Open();
                button.fontSize=previous; GUI.depth=oldDepth;
            }
            return;
        }
        int depth=GUI.depth; GUI.depth=-30;
        MarketDrawing.Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.01f,.04f,.035f,.82f));
        Matrix4x4 matrix=GUI.matrix;
        float scale=Mathf.Min(Screen.width/960f,Screen.height/700f);
        GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-960*scale)*.5f,(Screen.height-700*scale)*.5f),Quaternion.identity,Vector3.one*scale);
        if (VolumesOpen) DrawVolumes(); else DrawPause();
        GUI.matrix=matrix; GUI.depth=depth;
    }
    private void Frame(Rect rect)
    {
        MarketDrawing.Fill(new Rect(rect.x+8,rect.y+10,rect.width,rect.height),new Color(0,.02f,.015f,.7f));
        MarketDrawing.Fill(rect,panel);
        // Franjas sin rotaciones: se recortan correctamente también con GUI.matrix escalada.
        MarketDrawing.Fill(new Rect(rect.x,rect.y,rect.width,3),AndeanUI.Magenta);
        MarketDrawing.Fill(new Rect(rect.x,rect.y+3,rect.width,2),AndeanUI.Yellow);
        MarketDrawing.Fill(new Rect(rect.x,rect.y+5,rect.width,2),AndeanUI.Green);
        MarketDrawing.Fill(new Rect(rect.x,rect.y+7,rect.width,3),AndeanUI.Blue);
        MarketDrawing.Fill(new Rect(rect.x,rect.yMax-3,rect.width,3),gold);
    }
    private void DrawPause()
    {
        Frame(new Rect(220,96,520,506));
        GUI.Label(new Rect(240,125,480,54),"PARTIDA EN PAUSA",title);
        GUI.Label(new Rect(240,179,480,34),"Mercado Lanza · Un respiro, caserito",subtitle);
        if (Button(new Rect(272,245,416,66),"REANUDAR",true)) Resume();
        if (Button(new Rect(272,329,416,66),"VOLVER A JUGAR")) Restart();
        if (Button(new Rect(272,413,416,66),"VOLÚMENES")) ShowVolumes();
        GUI.Label(new Rect(240,526,480,30),"ESC o P para continuar tu jornada",subtitle);
    }
    private void DrawVolumes()
    {
        Frame(new Rect(150,55,660,590));
        GUI.Label(new Rect(175,78,610,52),"VOLÚMENES",title);
        GUI.Label(new Rect(175,131,610,30),"Activa o silencia cada sonido por separado",subtitle);
        var audio=MarketAudio.Instance;
        if (audio != null)
        {
            VolumeRow(audio,MarketAudio.Channel.Radio,"MÚSICA RADIO","Canciones y transición",185);
            VolumeRow(audio,MarketAudio.Channel.Background,"FONDO","Ambiente continuo del mercado",293);
            VolumeRow(audio,MarketAudio.Channel.Effects,"EFECTOS","Misiones, potenciadores y derrota",401);
        }
        if (Button(new Rect(268,552,424,55),"VOLVER A PAUSA",true)) VolumesOpen=false;
    }
    private void VolumeRow(MarketAudio audio,MarketAudio.Channel channel,string name,string description,float y)
    {
        MarketDrawing.Fill(new Rect(184,y,592,96),card);
        GUI.Label(new Rect(202,y+7,355,30),name,label);
        GUI.Label(new Rect(202,y+37,355,23),description,caption);
        bool on=audio.IsEnabled(channel);
        int previous=button.fontSize; button.fontSize=16;
        if (Button(new Rect(612,y+12,144,38),on?"ACTIVADO":"DESACTIVADO",on)) audio.SetEnabled(channel,!on);
        button.fontSize=previous;
        Color old=GUI.color; GUI.color=gold;
        float volume=GUI.HorizontalSlider(new Rect(204,y+73,408,18),audio.Volume(channel),0f,1f);
        GUI.color=old;
        audio.SetVolume(channel,volume);
        GUI.Label(new Rect(642,y+60,112,28),Mathf.RoundToInt(volume*100)+" %",label);
    }
}
