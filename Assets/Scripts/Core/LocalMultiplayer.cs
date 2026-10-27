using UnityEngine;

public class LocalMultiplayer : MonoBehaviour
{
    public enum GameMode { SinglePlayer, LocalMultiplayer }
    public static LocalMultiplayer Instance { get; private set; }
    public PlayerController Worker { get; private set; }
    public PlayerController Rival { get; private set; }
    public GameMode Mode { get; private set; } = GameMode.SinglePlayer;
    private Vector3 workerSpawn, rivalSpawn;
    private Quaternion initialRotation;
    private Camera firstCamera, secondCamera;
    private RivalAI rivalAI;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameHUD hud = FindAnyObjectByType<GameHUD>();
        if (hud != null && FindAnyObjectByType<LocalMultiplayer>() == null) hud.gameObject.AddComponent<LocalMultiplayer>();
    }
    private void Awake() { Instance = this; }
    private void Start()
    {
        Worker = FindAnyObjectByType<PlayerController>();
        if (Worker == null) return;
        Worker.controls = PlayerController.LocalControls.WASD;
        Worker.role = PlayerController.PlayerRole.Worker;
        Worker.playerName = "Tomás";
        workerSpawn = Worker.transform.position;
        initialRotation = Worker.transform.rotation;
        GameObject clone = Instantiate(Worker.gameObject);
        clone.name = "Rival_Local";
        Rival = clone.GetComponent<PlayerController>();
        Rival.controls = PlayerController.LocalControls.IJKL;
        Rival.role = PlayerController.PlayerRole.Impostor;
        Rival.playerName = "Rival";
        GameObject copiedItem = Rival.ReleaseHeldItem();
        if (copiedItem != null) Destroy(copiedItem);
        rivalSpawn = FindSpawn();
        Rival.Teleport(rivalSpawn);
        Rival.gameObject.AddComponent<TackleSabotage>();
        Worker.gameObject.AddComponent<CharacterAppearance>();
        Rival.gameObject.AddComponent<CharacterAppearance>();
        AddBadge();
        CreateCameras();
        FloatingWindows windows = FindAnyObjectByType<FloatingWindows>();
        if (windows != null) windows.simulateSabotage = false;
        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted += ResetRound;
        if (GetComponent<InvestigationSystem>() == null) gameObject.AddComponent<InvestigationSystem>();
        rivalAI = Rival.gameObject.AddComponent<RivalAI>();
        rivalAI.Initialize(Worker);
        ConfigureMode(GameMode.SinglePlayer);
        Worker.GetComponent<CharacterAppearance>().Apply(0);
        Rival.GetComponent<CharacterAppearance>().Apply(1);
        if (GetComponent<GameFrontEnd>() == null) gameObject.AddComponent<GameFrontEnd>();
    }
    private Vector3 FindSpawn()
    {
        CapsuleCollider body = Worker.GetComponent<CapsuleCollider>();
        float radius = body != null ? body.radius : 0.5f;
        float height = body != null ? body.height : 2.5f;
        Vector3 center = body != null ? body.center : Vector3.up * 0.5f;
        for (int ring = 1; ring <= 4; ring++)
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                Vector3 point = workerSpawn + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ring * 1.5f;
                Vector3 a = point + center + Vector3.up * (height * 0.5f - radius);
                Vector3 b = point + center - Vector3.up * (height * 0.5f - radius);
                if (Physics.CheckCapsule(a, b, radius * 0.95f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                if (Physics.Raycast(point + center, Vector3.down, height + 1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return point;
            }
        return workerSpawn + Vector3.right * 1.5f;
    }
    private void AddBadge()
    {
        GameObject badge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        badge.name = "Escarapela violeta";
        badge.GetComponent<Collider>().enabled = false;
        Destroy(badge.GetComponent<Collider>());
        badge.transform.SetParent(Rival.transform, false);
        badge.transform.localPosition = new Vector3(-0.22f, 0.85f, 0.42f);
        badge.transform.localScale = new Vector3(0.12f, 0.16f, 0.04f);
        badge.GetComponent<Renderer>().material.color = new Color(0.55f, 0.15f, 0.7f);
    }
    private void CreateCameras()
    {
        firstCamera = Camera.main;
        if (firstCamera == null) return;
        GameObject secondObject = Instantiate(firstCamera.gameObject);
        secondObject.name = "Camara_Rival_Local";
        secondObject.tag = "Untagged";
        secondCamera = secondObject.GetComponent<Camera>();
        foreach (var listener in secondObject.GetComponentsInChildren<AudioListener>()) listener.enabled = false;
        firstCamera.cullingMask &= ~(1 << RuntimePreview.PreviewLayer);
        secondCamera.cullingMask &= ~(1 << RuntimePreview.PreviewLayer);
        secondCamera.depth = firstCamera.depth + 1f;
        Worker.viewCamera = firstCamera;
        Rival.viewCamera = secondCamera;
        firstCamera.GetComponent<CameraFollow>()?.SetTarget(Worker.transform);
        secondCamera.GetComponent<CameraFollow>()?.SetTarget(Rival.transform);
    }
    public bool ConfigureMode(GameMode mode)
    {
        if (Worker == null || Rival == null) return false;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing) return false;
        Mode = mode;
        bool local = mode == GameMode.LocalMultiplayer;
        rivalAI.SetActive(!local);
        if (firstCamera != null) firstCamera.rect = new Rect(0f, 0f, local ? 0.5f : 1f, 1f);
        if (secondCamera != null)
        {
            secondCamera.rect = new Rect(0.5f, 0f, 0.5f, 1f);
            secondCamera.gameObject.SetActive(local);
        }
        return true;
    }
    public void ReturnRival()
    {
        if (Rival == null) return;
        Rival.ResetForRound(rivalSpawn, initialRotation);
        Rival.GetComponent<TackleSabotage>().SuspendForExpulsion();
        rivalAI.ResetForRound();
    }
    public void PrepareMenu() { ResetRound(); }
    private void ResetRound()
    {
        GameFrontEnd.Instance?.CloseGuide();
        Worker.ResetForRound(workerSpawn, initialRotation);
        Rival.ResetForRound(rivalSpawn, initialRotation);
        Rival.GetComponent<TackleSabotage>().ResetForRound();
        rivalAI?.ResetForRound();
        foreach (var evidence in FindObjectsByType<SabotageEvidence>()) Destroy(evidence.gameObject);
    }
    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted -= ResetRound;
        if (Instance == this) Instance = null;
    }
}
