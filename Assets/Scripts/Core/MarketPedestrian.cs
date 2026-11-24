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

    // Capibaras del mercado (modelo de Blender): uno de cada 9 caseros, con sus propios pregones y charla
    private static readonly string[] CapybaraNames = { "Capibara Pancho", "Capibara Lola", "Capibara Chino", "Capibara Rosita" };
    private static readonly string[] CapybaraCalls = { "¡Prrr!", "¡Ñam, pastito fresco!", "¡Pip pip!", "Zzz... ¿ya es hora de almorzar?" };
    private static readonly string[] CapybaraTalks = {
        "Soy el capibara más tranquilo del Lanza. Dicen que aquí hasta el Rival se calma si me mira un ratito.",
        "Me gusta pasear entre los puestos: el de las flores huele rico y el de las frutas me regala cáscaras.",
        "Cuidado con los sabotajes, caserito. Yo vi pasar a alguien sospechoso cerca de las hornallas. ¡Prrr!",
        "Si ves un charco de aceite, rodéalo. Yo ya me resbalé dos veces y no fue nada divertido." };
    public static bool IsCapybara(int index) => index % 9 == 4;

    // Perritos del mercado (Jack Russell de Blender): otro de cada 9 caseros
    private static readonly string[] DogNames = { "Perrito Canelo", "Perrita Chuspa", "Perrito Tunari", "Perrita Pelusa" };
    private static readonly string[] DogCalls = { "¡Guau guau!", "¡Guau!", "¡Arf, arf!", "¡Grrr... guau!" };
    private static readonly string[] DogTalks = {
        "¡Guau! Me escapé de la casa de Doña Rosa porque aquí huele a api con pastel. ¡No le digas a nadie!",
        "Los perritos del Lanza conocemos todos los puestos. El de los peluches me da miedo: ¡son muchísimos y no se mueven!",
        "Guau, guau. Si ves a alguien raro cerca de las hornallas, ¡ladra fuerte y avisa al Capibara policía!",
        "Hoy ya me comí media marraqueta. Nadie se dio cuenta... ¿o sí? ¡Guau!" };
    public static bool IsDog(int index) => index % 9 == 7;

    public NavMeshAgent Agent { get; private set; }
    public bool Stationary { get; private set; }
    private bool capybara, dog;
    private float bubbleHeight = 2.25f;
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
        capybara = IsCapybara(index); dog = IsDog(index);
        bool pet = capybara || dog;
        displayName = capybara ? CapybaraNames[(index / 9) % CapybaraNames.Length]
            : dog ? DogNames[(index / 9) % DogNames.Length] : Names[index % Names.Length];
        interactionPrompt = "E · Conversar";
        int interactable = LayerMask.NameToLayer("Interactable");
        gameObject.layer = interactable >= 0 ? interactable : 0;
        var collider = gameObject.AddComponent<CapsuleCollider>();
        collider.center = pet ? Vector3.up * .5f : Vector3.up; collider.height = pet ? 1f : 2f;
        collider.radius = pet ? .5f : .38f; collider.isTrigger = true;
        Stationary = index % 6 == 0;
        Agent = gameObject.AddComponent<NavMeshAgent>();
        Agent.agentTypeID = nav.AgentType; Agent.radius = pet ? .4f : .35f; Agent.height = pet ? 1f : 1.85f;
        Agent.speed = capybara ? Random.Range(.8f, 1.4f) : dog ? Random.Range(1.4f, 2.2f) : Random.Range(1f, 1.9f); Agent.angularSpeed = 160f;
        Agent.acceleration = 5f; Agent.stoppingDistance = .25f;
        Agent.avoidancePriority = 30 + index % 40; Agent.autoBraking = true;
        Agent.Warp(home);
        if (capybara && TryAddPet("Characters/CapibaraCasero", .85f, 1.05f)) { FinishInitialize(); return; }
        if (dog && TryAddPet("Characters/PerritoCasero", .7f, .85f)) { FinishInitialize(); return; }
        capybara = dog = false;
        // Cholitas modeladas en Blender (las mismas de las apariencias); si faltan, las del catalogo
        GameObject prefab = Resources.Load<GameObject>(index % 2 == 0 ? "Characters/CholitaPacena" : "Characters/DonaRosa");
        bool blender = prefab != null;
        if (!blender)
        {
            var catalog = Resources.Load<CharacterSkinCatalog>("MenuIntegration/CharacterSkins");
            prefab = catalog != null ? catalog.Model(index % 2 == 0 ? 3 : 2) : null;
        }
        if (prefab != null)
        {
            var visual = Instantiate(prefab, transform);
            visual.name = "Casero del mercado";
            if (blender)
            {
                // Que mire hacia donde camina (los ojos al frente)
                var eyes = visual.transform.Find("Ojos");
                var parts = visual.GetComponentsInChildren<Renderer>();
                if (eyes != null && parts.Length > 0)
                {
                    Bounds all = parts[0].bounds;
                    foreach (var part in parts) all.Encapsulate(part.bounds);
                    var eyesRenderer = eyes.GetComponent<Renderer>();
                    Vector3 eyesCenter = eyesRenderer != null ? eyesRenderer.bounds.center : eyes.position;
                    if (Vector3.Dot(eyesCenter - all.center, transform.forward) < 0f) visual.transform.Rotate(0f, 180f, 0f, Space.World);
                }
            }
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
                    if (renderer.name.StartsWith("Pollera") || renderer.name.StartsWith("Shirt") || renderer.name.StartsWith("Sleeve") || renderer.name.StartsWith("Blusa"))
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
        FinishInitialize();
    }
    private void FinishInitialize()
    {
        ResetForRound();
        transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
    }

    // Mascota del mercado (CapibaraCasero.fbx o PerritoCasero.fbx, de Blender): un cuerpo y un cubito
    // "MarcadorFrente" dibujado delante del hocico, que sirve para saber hacia donde mira despues de importarlo
    private bool TryAddPet(string resource, float minHeight, float maxHeight)
    {
        var prefab = Resources.Load<GameObject>(resource);
        if (prefab == null) return false;
        var visual = Instantiate(prefab, transform);
        visual.name = dog ? "Perrito del mercado" : "Capibara del mercado";
        foreach (var body in visual.GetComponentsInChildren<Collider>(true)) body.enabled = false;

        Vector3 markerPoint = Vector3.zero;
        bool hasMarker = false;
        foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            if (renderer.name == "MarcadorFrente") { markerPoint = renderer.bounds.center; hasMarker = true; renderer.gameObject.SetActive(false); }

        var renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) { Destroy(visual); return false; }
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        if (hasMarker && Vector3.Dot(markerPoint - bounds.center, transform.forward) < 0f)
            visual.transform.Rotate(0f, 180f, 0f, Space.World);

        visual.transform.localScale *= Random.Range(minHeight, maxHeight) / Mathf.Max(.01f, bounds.size.y);
        bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        visual.transform.position += home - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        bubbleHeight = dog ? 1.3f : 1.5f;
        return true;
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
            string[] calls = capybara ? CapybaraCalls : dog ? DogCalls : Calls;
            speech = calls[line++ % calls.Length]; speechRemaining = 3.6f;
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
        wait = 7f; speech = capybara ? "¡Prrr, prrr!" : dog ? "¡Guau, guau!" : "¡Buen día, caserito!"; speechRemaining = 5f;
        if (capybara)
            FloatingWindows.ShowInfo(displayName + " · Capibara del Lanza", CapybaraTalks[(personality / 9) % CapybaraTalks.Length], 8f);
        else if (dog)
            FloatingWindows.ShowInfo(displayName + " · Perrito del Lanza", DogTalks[(personality / 9) % DogTalks.Length], 8f);
        else
            FloatingWindows.ShowInfo(displayName + " · Casero del Lanza", Conversations[personality % Conversations.Length], 8f);
    }
    private void OnGUI()
    {
        if (!PlayerController.GameplayActive || speechRemaining <= 0f || market == null || market.Worker == null) return;
        // Solo los cercanos muestran su pregón; no llenamos el plano de textos lejanos.
        if (Vector3.Distance(market.Worker.transform.position, transform.position) > 13f) return;
        Camera camera = market.Worker.viewCamera != null ? market.Worker.viewCamera : Camera.main;
        if (camera == null) return;
        Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * bubbleHeight);
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
