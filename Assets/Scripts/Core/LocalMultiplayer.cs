using UnityEngine;

// Adaptador aditivo: comparte las estaciones existentes y conserva la cámara principal.
public class LocalMultiplayer : MonoBehaviour
{
    public enum GameMode { SinglePlayer, LocalMultiplayer }
    public static LocalMultiplayer Instance {get;private set;}
    public PlayerController Worker {get;private set;}
    public PlayerController Rival {get;private set;}
    public GameMode Mode {get;private set;}
    private Camera main,second;private Vector3 workerSpawn,rivalSpawn;private Quaternion rotation;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Attach()
    {
        var hud=FindAnyObjectByType<GameHUD>();if(hud!=null && FindAnyObjectByType<LocalMultiplayer>()==null)hud.gameObject.AddComponent<LocalMultiplayer>();
    }
    private void Awake(){Instance=this;}
    private void Start()
    {
        Worker=FindAnyObjectByType<PlayerController>();if(Worker==null)return;
        Worker.controls=PlayerController.LocalControls.WASD;Worker.role=PlayerController.PlayerRole.Worker;workerSpawn=Worker.transform.position;rotation=Worker.transform.rotation;
        var clone=Instantiate(Worker.gameObject);clone.name="Rival_Local";clone.tag="Untagged";
        Rival=clone.GetComponent<PlayerController>();Rival.controls=PlayerController.LocalControls.IJKL;Rival.role=PlayerController.PlayerRole.Impostor;
        foreach(Transform item in Rival.Hand)Destroy(item.gameObject);
        rivalSpawn=FindSpawn();Rival.Teleport(rivalSpawn);
        if(Worker.GetComponent<CharacterAppearance>()==null)Worker.gameObject.AddComponent<CharacterAppearance>();
        if(Rival.GetComponent<CharacterAppearance>()==null)Rival.gameObject.AddComponent<CharacterAppearance>();
        Rival.gameObject.AddComponent<LocalRivalDriver>();
        main=Camera.main;
        if(main!=null)
        {
            Worker.viewCamera=main;var cameraObject=new GameObject("Cámara del Rival");second=cameraObject.AddComponent<Camera>();second.CopyFrom(main);second.depth=main.depth+1;
            cameraObject.transform.SetPositionAndRotation(main.transform.position,main.transform.rotation);
            cameraObject.AddComponent<LocalPlayerView>().Initialize(Rival,main);Rival.viewCamera=second;
        }
        Rival.GetComponent<CharacterAppearance>().Apply(1);
        if(GetComponent<GameFrontEnd>()==null)gameObject.AddComponent<GameFrontEnd>();
        GameManager.Instance.OnGameStarted+=ResetRound;ConfigureMode(GameMode.SinglePlayer);
        if(GetComponent<InvestigationSystem>()==null)gameObject.AddComponent<InvestigationSystem>();
    }
    private Vector3 FindSpawn()
    {
        var body=Worker.GetComponent<CapsuleCollider>();float radius=body!=null?body.radius:.5f,height=body!=null?body.height:2.5f;
        Vector3 center=body!=null?body.center:Vector3.up*.5f;
        float floor=workerSpawn.y+center.y-height*.5f;
        var nearby=Physics.RaycastAll(workerSpawn+center,Vector3.down,height+4,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        System.Array.Sort(nearby,(a,b)=>a.distance.CompareTo(b.distance));
        foreach(var hit in nearby)if(hit.normal.y>.7f && hit.collider.GetComponentInParent<PlayerController>()==null){floor=hit.point.y;break;}
        for(int ring=1;ring<=4;ring++)for(int i=0;i<12;i++)
        {
            float angle=i*Mathf.PI/6;Vector3 point=workerSpawn+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*ring*1.5f;
            var hits=Physics.RaycastAll(point+Vector3.up*4,Vector3.down,8,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits)
            {
                if(hit.normal.y<.7f || Mathf.Abs(hit.point.y-floor)>.35f || hit.collider.GetComponentInParent<PlayerController>()!=null)continue;
                point.y=hit.point.y-center.y+height*.5f+.03f;
                if(!Physics.CheckCapsule(point+center+Vector3.down*(height*.5f-radius),point+center+Vector3.up*(height*.5f-radius),radius*.98f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))return point;
            }
        }
        return workerSpawn+Vector3.right*2;
    }
    public bool ConfigureMode(GameMode mode)
    {
        if(Worker==null || Rival==null)return false;Mode=mode;Rival.usesAI=mode==GameMode.SinglePlayer;
        if(main!=null)main.rect=mode==GameMode.SinglePlayer?new Rect(0,0,1,1):new Rect(0,0,.5f,1);
        if(second!=null){second.gameObject.SetActive(mode==GameMode.LocalMultiplayer);second.rect=new Rect(.5f,0,.5f,1);}
        if(SabotageSystem.Instance!=null)SabotageSystem.Instance.sabotageEnabled=mode==GameMode.SinglePlayer;
        return true;
    }
    public void ResetRound()
    {
        if(Worker==null || Rival==null)return;
        Worker.SetDetained(false);Rival.SetDetained(false);InvestigationSystem.Instance?.ResetRound();
        foreach(var player in new[]{Worker,Rival})foreach(Transform item in player.Hand)Destroy(item.gameObject);
        Worker.Teleport(workerSpawn);Worker.transform.rotation=rotation;Rival.Teleport(rivalSpawn);Rival.transform.rotation=rotation;
        Rival.GetComponent<LocalRivalDriver>().ResetRound();ConfigureMode(Mode);
    }
    public void ReturnRival()
    {
        if(Rival==null)return;Rival.SetDetained(false);Rival.Teleport(rivalSpawn);Rival.transform.rotation=rotation;
        Rival.GetComponent<LocalRivalDriver>().WaitAfterReturn();SabotageSystem.Instance?.DelayAutomatic(3f);
    }
    private void OnDestroy(){if(GameManager.Instance!=null)GameManager.Instance.OnGameStarted-=ResetRound;if(Instance==this)Instance=null;}
}
