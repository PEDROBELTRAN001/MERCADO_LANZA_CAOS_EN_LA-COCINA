using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Integración aditiva: funciona al pulsar Play en KitchenScene, sin reconstruir el mapa.
public class MarketLife : MonoBehaviour
{
    public static MarketLife Instance { get; private set; }
    public const float CocaInterval = 25f, CascaditaInterval = 40f, PickupLifetime = 10f;
    [Range(0, 80)] public int pedestrianCount = 36;
    public MarketLayout Layout { get; private set; }
    public PlayerController Worker => LocalMultiplayer.Instance != null ? LocalMultiplayer.Instance.Worker : null;
    public List<MarketPedestrian> Pedestrians { get; } = new List<MarketPedestrian>();
    public MarketPowerup Coca { get; private set; }
    public MarketPowerup Cascadita { get; private set; }
    public int PendingDeliveries { get; private set; }
    public float CocaCountdown => Mathf.Max(0f, CocaInterval - cocaTimer);
    public float CascaditaCountdown => Mathf.Max(0f, CascaditaInterval - cascTimer);
    private float cocaTimer, cascTimer;
    private Transform population;
    private MarketNavigation navigation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Attach()
    {
        var hud = FindAnyObjectByType<GameHUD>();
        if (hud != null && FindAnyObjectByType<MarketLife>() == null) hud.gameObject.AddComponent<MarketLife>();
    }
    private void Awake() { Instance = this; }
    private IEnumerator Start()
    {
        while (GameManager.Instance == null || Worker == null) yield return null;
        GameManager.Instance.OnGameStarted += ResetRound;
        // El sistema de investigación ya prepara la navegación y los enlaces de escaleras.
        while (InvestigationSystem.Instance == null || InvestigationSystem.Instance.Navigation == null) yield return null;
        navigation = InvestigationSystem.Instance.Navigation;
        if (!navigation.Ready) yield break;
        if (!TryWorkerFloor(out Vector3 origin)) yield break;
        Layout = new MarketLayout();
        if (!Layout.Build(navigation, origin)) { Layout = null; yield break; }
        gameObject.AddComponent<MarketMinimap>().Initialize(this);
        Worker.gameObject.AddComponent<PlayerDirectionIndicator>();
        population = new GameObject("Vida del Mercado Lanza").transform;
        population.SetParent(transform, false);
        for (int i = 0; i < pedestrianCount; i++)
        {
            if (Layout.TryRandomPoint(out Vector3 point, .4f) && FarFromPeople(point))
            {
                var root = new GameObject("Casero " + (i + 1));
                root.transform.SetParent(population, false); root.transform.position = point;
                var pedestrian = root.AddComponent<MarketPedestrian>();
                pedestrian.Initialize(this, navigation, i);
                Pedestrians.Add(pedestrian);
            }
            else { i--; if (++spawnFailures > 80) break; }
            yield return null;
        }
    }
    private int spawnFailures;
    private bool TryWorkerFloor(out Vector3 point)
    {
        point = Worker.transform.position;
        var hits = Physics.RaycastAll(point + Vector3.up, Vector3.down, 30f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
            if (hit.normal.y > .7f && hit.collider.GetComponentInParent<PlayerController>() == null &&
                navigation.Sample(hit.point, 1f, out point)) return true;
        return false;
    }
    private bool FarFromPeople(Vector3 point)
    {
        if (Worker != null && Vector3.Distance(point, Worker.transform.position) < 2f) return false;
        foreach (var actor in Pedestrians)
            if (actor != null && Vector3.Distance(point, actor.transform.position) < 2f) return false;
        return true;
    }
    private void Update()
    {
        if (!PlayerController.GameplayActive) return;
        cocaTimer += Time.deltaTime; cascTimer += Time.deltaTime;
        if (Layout == null) return;
        if (cocaTimer >= CocaInterval) { cocaTimer -= CocaInterval; Coca = Spawn(MarketPowerup.Kind.CocaLeaf); }
        if (cascTimer >= CascaditaInterval) { cascTimer -= CascaditaInterval; Cascadita = Spawn(MarketPowerup.Kind.Cascadita); }
        if (PendingDeliveries > 0 && OrderSystem.Instance != null && OrderSystem.Instance.DeliverMostUrgentAutomatically())
            PendingDeliveries--;
    }
    private MarketPowerup Spawn(MarketPowerup.Kind kind)
    {
        if (!Layout.TryRandomPoint(out Vector3 point)) return null;
        var root = new GameObject(kind == MarketPowerup.Kind.CocaLeaf ? "Hoja de coca" : "Cascadita");
        root.transform.SetParent(transform, false); root.transform.position = point;
        var pickup = root.AddComponent<MarketPowerup>(); pickup.Initialize(this, kind);
        return pickup;
    }
    public bool TryCollect(MarketPowerup pickup, PlayerController player)
    {
        if (!PlayerController.GameplayActive || pickup == null || player == null || player != Worker ||
            player.IsDetained || player.usesAI || player.role != PlayerController.PlayerRole.Worker || !pickup.Available) return false;
        Collider body = player.GetComponent<Collider>();
        Vector3 pickupCenter = pickup.transform.position + Vector3.up * .8f;
        if (body == null || Vector3.Distance(body.ClosestPoint(pickupCenter), pickupCenter) > .7f) return false;
        pickup.Consume();
        MarketAudio.Instance?.PlayPickup();
        if (pickup.Type == MarketPowerup.Kind.CocaLeaf)
        {
            player.CollectCocaLeaf();
            GameHUD.Notify("Hoja de coca: +15 % de velocidad · x" + player.CocaSpeedMultiplier.ToString("0.00"), 4f);
        }
        else if (OrderSystem.Instance == null || !OrderSystem.Instance.DeliverMostUrgentAutomatically())
        {
            PendingDeliveries++;
            GameHUD.Notify("Cascadita guardada: el próximo pedido se entregará automáticamente.", 4f);
        }
        return true;
    }
    public void ResetRound()
    {
        cocaTimer = cascTimer = 0f; PendingDeliveries = 0;
        if (Coca != null) { Coca.Consume(); Coca = null; }
        if (Cascadita != null) { Cascadita.Consume(); Cascadita = null; }
        Worker?.ResetMarketBoosts();
        foreach (var actor in Pedestrians) if (actor != null) actor.ResetForRound();
    }
    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted -= ResetRound;
        if (Instance == this) Instance = null;
    }
}
