using System.Collections.Generic;
using UnityEngine;

// Sabotajes del impostor durante la partida (hasta que exista la IA del impostor, los provoca este sistema):
// - En un fogon: sube el fuego al maximo -> humo y resplandor.
// - En otra estacion: derrama aceite en el piso, del lado por donde pasa el jugador.
// Ritmo (balance): el primero llega cuando el jugador ya conoce el mercado; despues cada vez mas seguido
// a medida que avanza la partida, y nunca en los ultimos segundos.
// Se agrega solo al objeto del GameManager al cargar la escena.
public class SabotageSystem : MonoBehaviour
{
    public static SabotageSystem Instance { get; private set; }

    public bool sabotageEnabled = true;
    [Tooltip("Segundos de partida antes del primer sabotaje")]
    public float firstSabotageAfter = 45f;
    [Tooltip("Segundos entre sabotajes al principio y al final de la partida")]
    public float startInterval = 55f;
    public float endInterval = 30f;
    [Tooltip("No hay sabotajes en estos ultimos segundos")]
    public float quietFinalSeconds = 15f;

    public int SabotageCount { get; private set; }
    public event System.Action<PlayerController,Interactable> OnSabotageCommitted;
    public void DelayAutomatic(float seconds)
    {
        if(GameManager.Instance!=null)nextSabotage=Mathf.Max(nextSabotage,GameManager.Instance.gameDuration-GameManager.Instance.GetTimeRemaining()+seconds);
    }

    private float nextSabotage = -1f;
    private readonly List<GameObject> activeEffects = new List<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Attach()
    {
        if (GameManager.Instance != null && GameManager.Instance.GetComponent<SabotageSystem>() == null)
            GameManager.Instance.gameObject.AddComponent<SabotageSystem>();
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted += OnGameStarted;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted -= OnGameStarted;
        if (Instance == this) Instance = null;
    }

    private void OnGameStarted()
    {
        foreach (GameObject fx in activeEffects) if (fx != null) Destroy(fx);
        activeEffects.Clear();
        SabotageCount = 0;
        nextSabotage = firstSabotageAfter;
    }

    private void Update()
    {
        var gm = GameManager.Instance;
        if (!sabotageEnabled || gm == null || gm.CurrentState != GameManager.GameState.Playing || nextSabotage < 0f) return;
        if(!PlayerController.GameplayActive)return;
        if(LocalMultiplayer.Instance!=null && LocalMultiplayer.Instance.Rival!=null && LocalMultiplayer.Instance.Rival.IsDetained)return;

        float elapsed = gm.gameDuration - gm.GetTimeRemaining();
        if (elapsed < nextSabotage || gm.GetTimeRemaining() <= quietFinalSeconds) return;

        float progress = Mathf.Clamp01(elapsed / gm.gameDuration);
        float interval = Mathf.Lerp(startInterval, endInterval, progress);
        nextSabotage = elapsed + interval * Random.Range(0.8f, 1.2f);
        Sabotage();
    }

    // Tambien lo puede llamar la IA del impostor cuando exista
    public void Sabotage()
    {
        var targets = new List<Interactable>();
        targets.AddRange(FindObjectsByType<CookingStation>());
        targets.AddRange(FindObjectsByType<ChoppingStation>());
        targets.AddRange(FindObjectsByType<AssemblyStation>());
        if (targets.Count == 0) return;
        Sabotage(targets[Random.Range(0, targets.Count)]);
    }

    public void Sabotage(Interactable target)
    {
        var local=LocalMultiplayer.Instance;
        Sabotage(target,local!=null && local.Mode==LocalMultiplayer.GameMode.SinglePlayer?local.Rival:null);
    }
    public void Sabotage(Interactable target,PlayerController culprit)
    {
        if(target==null || (culprit!=null && (culprit.IsDetained || LocalMultiplayer.Instance==null || culprit!=LocalMultiplayer.Instance.Rival)))return;
        SabotageCount++;
        activeEffects.RemoveAll(fx => fx == null);
        Bounds b = BoundsOf(target);

        if (target is CookingStation)
        {
            activeEffects.Add(SabotageEffects.Smoke(new Vector3(b.center.x, b.max.y, b.center.z)));
            FloatingWindows.ShowSabotageWarning("Alguien subió el fuego al máximo en «" + target.DisplayName + "». ¡Está saliendo humo!");
        }
        else
        {
            activeEffects.Add(SabotageEffects.OilStain(FloorPointNear(target.transform, b)));
            FloatingWindows.ShowSabotageWarning("Hay una mancha de aceite sospechosa junto a «" + target.DisplayName + "».");
        }
        if(culprit!=null)OnSabotageCommitted?.Invoke(culprit,target);
    }

    private static Bounds BoundsOf(Interactable target)
    {
        var renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(target.transform.position, Vector3.one);
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    // Al lado de la estacion, hacia donde esta el jugador (asi cae en el pasillo y no dentro del mueble)
    private static Vector3 FloorPointNear(Transform station, Bounds b)
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        Vector3 dir = player != null ? player.transform.position - b.center : Random.insideUnitSphere;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = station.forward;
        dir.Normalize();

        float floorY = b.min.y;
        if (player != null && player.TryGetComponent(out Collider body)) floorY = body.bounds.min.y;

        Vector3 p = b.center + dir * (Mathf.Max(b.extents.x, b.extents.z) * 0.5f + 0.9f);
        return new Vector3(p.x, floorY, p.z);
    }
}
