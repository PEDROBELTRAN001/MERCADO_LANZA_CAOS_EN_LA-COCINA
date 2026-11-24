using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Integración aditiva: funciona al pulsar Play en KitchenScene, sin reconstruir el mapa.
public class MarketLife : MonoBehaviour
{
    public static MarketLife Instance { get; private set; }
    public const float CocaInterval = 20f, CascaditaInterval = 20f, PickupLifetime = 10f;
    public const int CopiesPerSpawn = 3;
    [Range(0, 80)] public int pedestrianCount = 36;
    public MarketLayout Layout { get; private set; }
    public PlayerController Worker => LocalMultiplayer.Instance != null ? LocalMultiplayer.Instance.Worker : null;
    public List<MarketPedestrian> Pedestrians { get; } = new List<MarketPedestrian>();
    private readonly List<MarketPowerup> cocaPickups = new List<MarketPowerup>();
    private readonly List<MarketPowerup> cascadiPickups = new List<MarketPowerup>();
    public IReadOnlyList<MarketPowerup> CocaPickups => cocaPickups;
    public IReadOnlyList<MarketPowerup> CascaditaPickups => cascadiPickups;
    public MarketPowerup Coca => FirstAvailable(cocaPickups);
    public MarketPowerup Cascadita => FirstAvailable(cascadiPickups);
    public int PendingDeliveries { get; private set; }
    public float CocaCountdown => Coca != null ? 0f : Mathf.Max(0f, CocaInterval - cocaTimer);
    public float CascaditaCountdown => Cascadita != null ? 0f : Mathf.Max(0f, CascaditaInterval - cascTimer);
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
        if (Layout == null) { cocaTimer += Time.deltaTime; cascTimer += Time.deltaTime; return; }
        TickPickups(cocaPickups, cascadiPickups, MarketPowerup.Kind.CocaLeaf, ref cocaTimer, CocaInterval);
        TickPickups(cascadiPickups, cocaPickups, MarketPowerup.Kind.Cascadita, ref cascTimer, CascaditaInterval);
        if (PendingDeliveries > 0 && OrderSystem.Instance != null && OrderSystem.Instance.DeliverMostUrgentAutomatically())
            PendingDeliveries--;
    }
    private static MarketPowerup FirstAvailable(List<MarketPowerup> pickups)
    {
        foreach (var pickup in pickups) if (pickup != null && pickup.Available) return pickup;
        return null;
    }
    private void TickPickups(List<MarketPowerup> pickups, List<MarketPowerup> other,
        MarketPowerup.Kind kind, ref float timer, float interval)
    {
        if (pickups.Count > 0)
        {
            pickups.RemoveAll(p => p == null || !p.Available);
            if (pickups.Count > 0) return;
            // La espera comienza cuando ya no queda ninguna copia de esta tanda.
            timer = 0f;
        }
        timer += Time.deltaTime;
        if (timer < interval) return;
        if (SpawnBatch(pickups, other, kind)) timer = 0f;
        else timer = interval - 1f;
    }
    private bool SpawnBatch(List<MarketPowerup> pickups, List<MarketPowerup> other, MarketPowerup.Kind kind)
    {
        var positions = new List<Vector3>(CopiesPerSpawn);
        for (int attempt = 0; attempt < 120 && positions.Count < CopiesPerSpawn; attempt++)
        {
            if (!Layout.TryRandomPoint(out Vector3 point)) continue;
            if (Worker != null && Vector3.Distance(point, Worker.transform.position) < 2.2f) continue;
            // Distribuir las tres opciones por distintos tramos y evitar superponer tipos.
            float spacing = attempt < 90 ? 8f : 1.5f;
            bool free = true;
            foreach (var previous in positions)
                if (Vector3.Distance(point, previous) < spacing) { free = false; break; }
            foreach (var existing in other)
                if (existing != null && existing.Available && Vector3.Distance(point, existing.transform.position) < 4f)
                { free = false; break; }
            if (free) positions.Add(point);
        }
        if (positions.Count != CopiesPerSpawn) return false;
        foreach (Vector3 point in positions) pickups.Add(Spawn(kind, point));
        return true;
    }
    private MarketPowerup Spawn(MarketPowerup.Kind kind, Vector3 point)
    {
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
        foreach (var pickup in cocaPickups) if (pickup != null) pickup.Consume();
        foreach (var pickup in cascadiPickups) if (pickup != null) pickup.Consume();
        cocaPickups.Clear(); cascadiPickups.Clear();
        Worker?.ResetMarketBoosts();
        foreach (var actor in Pedestrians) if (actor != null) actor.ResetForRound();
    }
    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted -= ResetRound;
        if (Instance == this) Instance = null;
    }
}
