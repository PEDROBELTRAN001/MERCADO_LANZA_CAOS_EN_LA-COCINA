using UnityEngine;
using UnityEngine.AI;

public class MarketPedestrian : Interactable
{
    private static readonly string[] Names = { "Doña Julia", "Don René", "Casera Naty", "Don Víctor", "Doña Elvira", "Casero Luis", "Doña Carmen", "Don Mario" };
    private static readonly string[] Calls = {
        "¡Casero, pase sin compromiso!", "¡Api calientito con pastel!", "¡Plato paceño, recién servido!",
        "¡Marraqueta crocante, caserita!", "¡Frutita fresca de los Yungas!", "¡Lleve su agüita, casero!",
        "¡Qué linda está La Paz hoy!", "Me espera mi comadre en Merlan Altos.", "¡Jallalla nuestro Mercado Lanza!",
        "¡Cuidado, que la sopa está caliente!", "¿Nos tomamos un refresquito?", "¡Buen provecho, caseritos!" };
    private static readonly string[] Conversations = {
        "¡Buen día, caserito! En el Lanza nos conocemos todos. Lleva los platos a tiempo y los clientes volverán contentos.",
        "Mi puesto abre tempranito. Aquí el api con pastel nunca falta. Mira las comandas: las mesas VIP cuentan mucho para la jornada.",
        "¿Vas al comedor? Sigue los pasillos y mira tu mapa. Las marcas doradas son mesas con pedidos pendientes.",
        "Dicen que aparecen hojitas de coca en los pasillos. Cada una te da un 15 % más de velocidad durante este turno. ¡Solo duran cinco segundos!",
        "Si encuentras un sachet Cascadita, recógelo rapidito. Entrega automáticamente el pedido más urgente, o guarda una entrega para el siguiente.",
        "¡No dejes que te arruinen la sazón! Si ves rastros de sabotaje, examínalos y avisa al Capibara con tu evidencia.",
        "Antes de salir de La Paz, prueba una marraqueta con quesito. ¡Ese sí es un desayuno de casero!",
        "Los mejores encuentros son aquí, entre los puestos. Mi comadre viene por verduras y siempre termina almorzando con nosotros." };

    public NavMeshAgent Agent { get; private set; }
    public bool Stationary { get; private set; }
    private MarketLife market;
    private MarketNavigation navigation;
    private Vector3 home;
    private int personality, line;
    private float wait, speechTimer, speechRemaining;
    private string speech;
    private GUIStyle bubbleStyle;

    public void Initialize(MarketLife owner, MarketNavigation nav, int index)
    {
        market = owner; navigation = nav; home = transform.position; personality = index;
        displayName = Names[index % Names.Length]; interactionPrompt = "E · Conversar";
        int interactable = LayerMask.NameToLayer("Interactable");
        gameObject.layer = interactable >= 0 ? interactable : 0;
        var collider = gameObject.AddComponent<CapsuleCollider>();
        collider.center = Vector3.up; collider.height = 2f; collider.radius = .38f; collider.isTrigger = true;
        Stationary = index % 6 == 0;
        Agent = gameObject.AddComponent<NavMeshAgent>();
        Agent.agentTypeID = nav.AgentType; Agent.radius = .35f; Agent.height = 1.85f;
        Agent.speed = Random.Range(1f, 1.9f); Agent.angularSpeed = 160f;
        Agent.acceleration = 5f; Agent.stoppingDistance = .25f;
        Agent.avoidancePriority = 30 + index % 40; Agent.autoBraking = true;
        Agent.Warp(home);
        var catalog = Resources.Load<CharacterSkinCatalog>("MenuIntegration/CharacterSkins");
        GameObject prefab = catalog != null ? catalog.Model(index % 2 == 0 ? 3 : 2) : null;
        if (prefab != null)
        {
            var visual = Instantiate(prefab, transform);
            visual.name = "Casero del mercado";
            foreach (var body in visual.GetComponentsInChildren<Collider>(true)) body.enabled = false;
            var renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Color[] clothes = { AndeanUI.Magenta, AndeanUI.Blue, AndeanUI.Green, AndeanUI.Orange,
                    new Color(.48f,.24f,.58f), new Color(.22f,.58f,.66f) };
                var tint = new MaterialPropertyBlock();
                tint.SetColor("_BaseColor", clothes[index % clothes.Length]);
                tint.SetColor("_Color", clothes[index % clothes.Length]);
                foreach (var renderer in renderers)
                    if (renderer.name.StartsWith("Pollera") || renderer.name.StartsWith("Shirt") || renderer.name.StartsWith("Sleeve"))
                        renderer.SetPropertyBlock(tint);
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                visual.transform.localScale *= Random.Range(1.7f, 2f) / Mathf.Max(.01f, bounds.size.y);
                bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                visual.transform.position += home - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            }
            visual.GetComponentInChildren<CharacterVisualRig>()?.BindAgent(Agent);
        }
        ResetForRound();
        transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
    }
    public void ResetForRound()
    {
        if (Agent != null && Agent.isOnNavMesh) { Agent.ResetPath(); Agent.Warp(home); }
        wait = Random.Range(1f, 7f); speechTimer = Random.Range(.5f, 8f);
        speechRemaining = 0f; line = personality % Calls.Length;
    }
    private void Update()
    {
        if (Agent == null || !Agent.isOnNavMesh) return;
        bool active = PlayerController.GameplayActive;
        Agent.isStopped = !active;
        if (!active) return;
        speechTimer -= Time.deltaTime; speechRemaining -= Time.deltaTime;
        if (speechTimer <= 0f)
        {
            speech = Calls[line++ % Calls.Length]; speechRemaining = 3.6f;
            speechTimer = Random.Range(8f, 15f);
        }
        if (Stationary || Agent.pathPending) return;
        if (Agent.hasPath && Agent.remainingDistance > Agent.stoppingDistance + .2f) return;
        wait -= Time.deltaTime;
        if (wait > 0f) return;
        // Algunos pasean por todo el mercado; otros hacen recorridos cortos alrededor de su puesto.
        for (int attempt = 0; attempt < 8; attempt++)
        {
            if (!market.Layout.TryRandomPoint(out Vector3 destination)) continue;
            if (personality % 3 == 0 && Vector3.Distance(home, destination) > 18f) continue;
            if (!navigation.CanReach(transform.position, destination, out destination)) continue;
            Agent.SetDestination(destination); break;
        }
        wait = Random.Range(2f, personality % 4 == 0 ? 12f : 5f);
    }
    public override void Interact(PlayerController player)
    {
        if (!PlayerController.GameplayActive || player == null || player.IsDetained) return;
        if (Vector3.Distance(transform.position + Vector3.up, player.transform.position) > 3f) return;
        if (Agent != null && Agent.isOnNavMesh) Agent.ResetPath();
        wait = 7f; speech = "¡Buen día, caserito!"; speechRemaining = 5f;
        FloatingWindows.ShowInfo(displayName + " · Casero del Lanza", Conversations[personality % Conversations.Length], 8f);
    }
    private void OnGUI()
    {
        if (!PlayerController.GameplayActive || speechRemaining <= 0f || market == null || market.Worker == null) return;
        // Solo los cercanos muestran su pregón; no llenamos el plano de textos lejanos.
        if (Vector3.Distance(market.Worker.transform.position, transform.position) > 13f) return;
        Camera camera = market.Worker.viewCamera != null ? market.Worker.viewCamera : Camera.main;
        if (camera == null) return;
        Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * 2.25f);
        Rect viewport = camera.pixelRect;
        if (screen.z <= 0 || !viewport.Contains(new Vector2(screen.x, screen.y))) return;
        float scale = Mathf.Clamp(Screen.height / 1080f, .7f, 1.2f);
        float width = 180f * scale, height = 57f * scale;
        Rect rect = new Rect(screen.x - width / 2, Screen.height - screen.y - height, width, height);
        if (rect.x < viewport.x || rect.xMax > viewport.xMax || rect.y < 200f * scale) return;
        if (bubbleStyle == null) bubbleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
        bubbleStyle.fontSize = Mathf.RoundToInt(14f * scale); bubbleStyle.normal.textColor = new Color(.13f, .23f, .2f);
        int depth = GUI.depth; GUI.depth = 10;
        MarketDrawing.Fill(rect, new Color(1f, .97f, .84f, .96f));
        MarketDrawing.Fill(new Rect(rect.x, rect.y, 3f * scale, rect.height), AndeanUI.Magenta);
        GUI.Label(new Rect(rect.x + 6f, rect.y + 2f, rect.width - 12f, rect.height - 4f), speech, bubbleStyle);
        GUI.depth = depth;
    }
}
