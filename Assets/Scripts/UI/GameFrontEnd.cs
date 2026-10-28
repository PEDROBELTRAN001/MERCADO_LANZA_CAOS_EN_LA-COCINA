using UnityEngine;

// Menú inicial independiente del mapa y de las estaciones de presentacion_3.
public class GameFrontEnd : MonoBehaviour
{
    public static GameFrontEnd Instance {get;private set;}
    public int WorkerSkin {get;private set;}
    public int RivalSkin {get;private set;}=1;
    public LocalMultiplayer.GameMode SelectedMode {get;private set;}
    private GUIStyle title,heading,body,muted,button,centered;
    private readonly Color background=new Color(.035f,.095f,.085f),panel=new Color(.08f,.20f,.17f),gold=new Color(.96f,.67f,.24f);
    private void Awake(){Instance=this;}
    public bool SelectMode(LocalMultiplayer.GameMode mode)
    {
        if(GameManager.Instance==null || GameManager.Instance.CurrentState!=GameManager.GameState.Ready || LocalMultiplayer.Instance==null)return false;
        if(!LocalMultiplayer.Instance.ConfigureMode(mode))return false;SelectedMode=mode;return true;
    }
    public bool SelectSkin(bool rival,int index)
    {
        if(GameManager.Instance.CurrentState!=GameManager.GameState.Ready || index<0 || index>=5)return false;
        var player=rival?LocalMultiplayer.Instance.Rival:LocalMultiplayer.Instance.Worker;
        if(!player.GetComponent<CharacterAppearance>().Apply(index))return false;
        if(rival)RivalSkin=index;else WorkerSkin=index;return true;
    }
    public bool StartSelectedGame()
    {
        if(FloatingWindows.BlocksInput || !SelectMode(SelectedMode))return false;
        if(!SelectSkin(false,WorkerSkin) || !SelectSkin(true,RivalSkin))return false;
        Time.timeScale=1;GameManager.Instance.SetState(GameManager.GameState.Playing);return true;
    }
    public void ReturnToMenu()
    {
        Time.timeScale=1;GameManager.Instance.SetState(GameManager.GameState.Ready);LocalMultiplayer.Instance.ResetRound();
    }
    private void Styles()
    {
        if(title!=null)return;
        title=new GUIStyle(GUI.skin.label){fontSize=42,fontStyle=FontStyle.Bold,wordWrap=true};heading=new GUIStyle(title){fontSize=26};
        body=new GUIStyle(GUI.skin.label){fontSize=21,wordWrap=true};muted=new GUIStyle(body){fontSize=18};centered=new GUIStyle(body){alignment=TextAnchor.MiddleCenter};
        button=new GUIStyle(GUI.skin.button){fontSize=22,wordWrap=true};title.normal.textColor=heading.normal.textColor=Color.white;
        body.normal.textColor=centered.normal.textColor=new Color(.95f,.95f,.89f);muted.normal.textColor=new Color(.70f,.80f,.74f);
    }
    public void DrawReady()
    {
        if(LocalMultiplayer.Instance==null || LocalMultiplayer.Instance.Rival==null)return;Styles();
        var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;GUI.depth=-5;
        Fill(new Rect(0,0,Screen.width,Screen.height),background);
        float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1440*scale)*.5f,(Screen.height-900*scale)*.5f),Quaternion.identity,Vector3.one*scale);
        Fill(new Rect(64,22,1312,5),gold);GUI.Label(new Rect(64,48,1250,58),"MERCADO LANZA",title);
        GUI.Label(new Rect(66,110,1250,36),"Caos en la cocina · Elige cómo jugar",body);
        if(Choice(new Rect(64,164,640,95),"JUGAR INDIVIDUAL\nWASD + E · Rival automático",SelectedMode==LocalMultiplayer.GameMode.SinglePlayer))SelectMode(LocalMultiplayer.GameMode.SinglePlayer);
        if(Choice(new Rect(728,164,648,95),"MULTIJUGADOR LOCAL\nDos personas · Un teclado · Dos cámaras",SelectedMode==LocalMultiplayer.GameMode.LocalMultiplayer))SelectMode(LocalMultiplayer.GameMode.LocalMultiplayer);
        DrawSkin(new Rect(64,280,640,322),false);DrawSkin(new Rect(728,280,648,322),true);
        if(GUI.Button(new Rect(64,628,410,66),"HISTORIA · F3",button))FloatingWindows.OpenHistory();
        if(GUI.Button(new Rect(498,628,430,66),"PERSONAJES · F1",button))FloatingWindows.OpenCharacters();
        if(GUI.Button(new Rect(952,628,424,66),"CURRÍCULUM · F2",button))FloatingWindows.OpenCurriculum();
        GUI.Label(new Rect(64,723,870,75),SelectedMode==LocalMultiplayer.GameMode.SinglePlayer?"Tomás: WASD · E: interactuar · Shift: correr\nSe conserva el sabotaje automático de la partida.":"Tomás: WASD + E · Rival: IJKL + O\nRival: U para humo/aceite en una estación cercana.",body);
        if(GUI.Button(new Rect(994,729,382,76),"COMENZAR PARTIDA",button))StartSelectedGame();
        GUI.Label(new Rect(64,840,1312,32),"Elige las apariencias antes del turno. El personaje seleccionado conserva su rol y sus controles.",muted);
        GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;
    }
    private void DrawSkin(Rect rect,bool rival)
    {
        Fill(rect,panel);int index=rival?RivalSkin:WorkerSkin;
        GUI.Label(new Rect(rect.x+24,rect.y+16,rect.width-48,40),rival?(SelectedMode==LocalMultiplayer.GameMode.SinglePlayer?"RIVAL AUTOMÁTICO":"JUGADOR 2 · RIVAL"):"JUGADOR 1 · TOMÁS",heading);
        var image=CharacterPortraitLibrary.Get(index);if(image!=null)GUI.DrawTexture(new Rect(rect.x+18,rect.y+65,230,235),image,ScaleMode.ScaleToFit);
        GUI.Label(new Rect(rect.x+272,rect.y+76,330,48),CharacterSkinCatalog.Names[index],heading);
        GUI.Label(new Rect(rect.x+272,rect.y+136,320,70),"La apariencia cambia; el rol se mantiene.",body);
        if(GUI.Button(new Rect(rect.x+272,rect.y+234,70,58),"<",button))SelectSkin(rival,(index+4)%5);
        GUI.Label(new Rect(rect.x+352,rect.y+243,155,45),(index+1)+" / 5",centered);
        if(GUI.Button(new Rect(rect.x+528,rect.y+234,70,58),">",button))SelectSkin(rival,(index+1)%5);
    }
    private bool Choice(Rect rect,string text,bool selected)
    {
        Fill(rect,selected?panel:new Color(.055f,.13f,.12f));if(selected)Fill(new Rect(rect.x,rect.y,rect.width,4),gold);
        GUI.Label(rect,text,centered);return GUI.Button(rect,GUIContent.none,GUIStyle.none);
    }
    private static void Fill(Rect rect,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
    private void OnDestroy(){if(Instance==this)Instance=null;}
}
