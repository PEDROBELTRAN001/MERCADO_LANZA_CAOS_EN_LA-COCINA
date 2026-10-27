using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Ventanas flotantes del juego (IMGUI, igual que GameHUD: no necesita Canvas ni prefabs).
// - Antes de la primera partida: historia corta -> curriculum de Tomas -> fichas de personaje.
//   ENTER / Espacio / flecha derecha avanza, flecha izquierda vuelve, ESC salta.
// - Durante la partida: avisos que entran por la derecha:
//   "Pasea para ubicarte del mapa", "¡Apresurate!" e "Intento de sabotaje".
// - F1 fichas, F2 curriculum, F3 historia: se vuelven a abrir cuando quieras (pausan el juego).
// Se agrega solo al objeto que tiene el GameHUD al cargar la escena: no hay que tocar la escena.
public class FloatingWindows : MonoBehaviour
{
    [Header("Colores (estilo mercado paceño, los mismos del HUD)")]
    public Color panelColor = new Color(0.12f, 0.30f, 0.26f, 0.96f);  // verde mercado
    public Color accentColor = new Color(0.94f, 0.60f, 0.19f);        // dorado
    public Color urgentColor = new Color(0.90f, 0.25f, 0.20f);        // rojo
    public Color paperColor = new Color(0.97f, 0.94f, 0.86f);         // hoja del curriculum
    public Color inkColor = new Color(0.16f, 0.13f, 0.10f);

    [Header("Introduccion")]
    [Tooltip("Mostrar historia, curriculum y fichas antes de la primera partida")]
    public bool showIntro = true;

    [Header("Pasea para ubicarte del mapa")]
    [Tooltip("Segundos que dura el recorrido guiado si el jugador no visita todos los puntos")]
    public float exploreMaxSeconds = 30f;
    [Tooltip("Distancia (m) a la que un punto del mapa cuenta como visitado")]
    public float landmarkRadius = 3f;

    [Header("Apresurate")]
    [Tooltip("Segundos de partida restantes en los que aparece el aviso")]
    public float hurryAtSeconds = 30f;
    [Tooltip("Segundos restantes de un pedido en los que se avisa que la mesa se impacienta")]
    public float orderHurrySeconds = 10f;

    [Header("Sabotaje (simulado hasta que exista el impostor)")]
    public bool simulateSabotage = true;
    public float sabotageMinInterval = 40f;
    public float sabotageMaxInterval = 70f;

    private enum Page { None, Story, Curriculum, Cards }
    private enum Kind { Explore, Hurry, Sabotage }

    private class Toast
    {
        public Kind kind;
        public string title, body;
        public float start, end;
        public bool closed;
    }

    private class Landmark
    {
        public string label;
        public Transform[] spots;
        public bool visited;
    }

    private struct StoryPage
    {
        public string title, body;
        public Color sky;
    }

    private struct Character
    {
        public string name, alias, role, initial, about, skill, weakness;
        public Color color;
        public string[] statNames;
        public int[] stats; // de 0 a 10
    }

    // ---------------------------------------------------------------- textos

    private static readonly StoryPage[] Story =
    {
        new StoryPage { title = "La Paz, 6 de la mañana",
            sky = new Color(0.95f, 0.62f, 0.35f),
            body = "El Mercado Lanza despierta entre el olor a api con pastel y el grito de los voceadores de minibús. " +
                   "Entre sus cientos de kioskos hay una cocina que todos conocen: la de Doña Rosa." },
        new StoryPage { title = "La cocina de Doña Rosa",
            sky = new Color(0.45f, 0.70f, 0.90f),
            body = "Su plato paceño tiene fama desde la Pérez hasta El Alto. Este año la casera no da abasto, " +
                   "así que contrató a Tomás, un garzón joven que conoce el mercado como la palma de su mano." },
        new StoryPage { title = "Un rival en la cocina",
            sky = new Color(0.35f, 0.18f, 0.30f),
            body = "No todos quieren que a Doña Rosa le vaya bien. Un kiosko rival metió a un impostor vestido con el uniforme de la casa. " +
                   "Su misión: sabotear el fogón, ensuciar el piso y hacer que los caseros se vayan sin comer." },
        new StoryPage { title = "El Capibara-policía",
            sky = new Color(0.25f, 0.45f, 0.55f),
            body = "El guardián del mercado patrulla los pasillos. Si Tomás junta pistas y acusa bien tres veces, el impostor será expulsado. " +
                   "Pero cuidado: una acusación falsa hace perder tiempo valioso." },
        new StoryPage { title = "Tu misión",
            sky = new Color(0.94f, 0.60f, 0.19f),
            body = "Atiende los pedidos de las mesas antes de que se acabe el tiempo, mantén la cocina en orden " +
                   "y no le quites el ojo a nadie.\n\n¡Doña Rosa confía en ti!" },
    };

    private static readonly Character[] Characters =
    {
        new Character { name = "Tomás", alias = "El Garzón", role = "Protagonista · lo controlas tú", initial = "T",
            color = new Color(1.00f, 0.55f, 0.00f),
            about = "Garzón nuevo de la cocina de Doña Rosa. Lleva y trae platos, arma los pedidos y vigila que nadie toque lo que no debe.",
            statNames = new[] { "Velocidad", "Cocina", "Olfato para pistas" }, stats = new[] { 9, 6, 7 },
            skill = "Corre entre las mesas sin soltar el plato.",
            weakness = "Se distrae con el partido que suena en la radio del kiosko de al lado." },
        new Character { name = "Doña Rosa", alias = "La Casera", role = "Dueña de la cocina", initial = "R",
            color = new Color(0.75f, 0.20f, 0.45f),
            about = "Cocinera legendaria del Mercado Lanza. Su plato paceño y su api con pastel tienen fila desde temprano. " +
                    "Pollera, sombrero y una mirada a la que no se le escapa nada.",
            statNames = new[] { "Sazón", "Carácter", "Paciencia" }, stats = new[] { 10, 9, 4 },
            skill = "Sus recetas dan más puntos cuanto más rápido se entregan.",
            weakness = "Si un cliente se va sin comer, se enoja (y te descuenta puntos)." },
        new Character { name = "El Rival", alias = "El Impostor", role = "Saboteador infiltrado", initial = "?",
            color = new Color(0.35f, 0.20f, 0.45f),
            about = "Trabaja para un kiosko rival. Se viste con el uniforme de Doña Rosa para pasar desapercibido " +
                    "y sabotear la cocina: humo en el fogón, aceite en el piso, platos cambiados.",
            statNames = new[] { "Sigilo", "Astucia", "Cocina" }, stats = new[] { 9, 8, 2 },
            skill = "Se mezcla entre los trabajadores sin que nadie lo note.",
            weakness = "Su uniforme tiene un detalle que lo delata: hay que observar bien." },
        new Character { name = "Capibara-policía", alias = "El Guardián del Mercado", role = "Autoridad del mercado", initial = "C",
            color = new Color(0.55f, 0.38f, 0.22f),
            about = "Tranquilo, redondo e incorruptible. Patrulla los pasillos y escucha las acusaciones. " +
                    "Con tres acusaciones correctas (1/3, 2/3, 3/3) expulsa al impostor del mercado.",
            statNames = new[] { "Olfato", "Calma", "Rapidez" }, stats = new[] { 8, 10, 2 },
            skill = "Expulsa al impostor cuando se confirman tres pistas.",
            weakness = "Una acusación falsa te hace perder tiempo." },
    };

    private static readonly string[] SabotageClues =
    {
        "Una sombra con el uniforme de Doña Rosa merodea cerca de «{0}».",
        "Alguien subió el fuego al máximo en «{0}». ¡Está saliendo humo!",
        "Hay una mancha de aceite sospechosa junto a «{0}».",
        "Alguien cambió los insumos de lugar cerca de «{0}».",
    };

    // ---------------------------------------------------------------- estado

    private static FloatingWindows instance;

    // Una ventana grande esta abierta (el juego esta en pausa)
    public static bool IsOpen => instance != null && instance.page != Page.None;

    // GameHUD no debe usar ENTER/R mientras hay una ventana abierta, ni en el mismo frame en que se cerro
    // (si no, el ENTER que cierra la ultima ficha tambien empezaria la partida)
    public static bool BlocksInput => instance != null && (instance.page != Page.None || instance.closedFrame == Time.frameCount);

    private Page page = Page.None;
    private int index;
    private bool introFlow, introShown;
    private int closedFrame = -1;
    private float savedTimeScale = 1f;

    private const int MaxToasts = 4;
    private readonly List<Toast> toasts = new List<Toast>();
    private readonly List<Landmark> landmarks = new List<Landmark>();
    private Toast exploreToast;
    private readonly HashSet<Order> warnedOrders = new HashSet<Order>();
    private bool hurryShown;
    private float nextSabotage;
    private float flashUntil;
    private PlayerController player;

    private GUIStyle body, small, header, bigTitle, center, initialStyle, toastTitle, toastBody, iconStyle;
    private GUIStyle paperBody, paperHeader;
    private Texture2D circleTexture;

    // Se cuelga del mismo objeto que el GameHUD apenas carga la escena
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Attach()
    {
        if (FindAnyObjectByType<FloatingWindows>() != null) return;
        GameHUD hud = FindAnyObjectByType<GameHUD>();
        if (hud != null) hud.gameObject.AddComponent<FloatingWindows>();
    }

    // Para el sistema de sabotaje del impostor: llamar cuando intente sabotear una estacion
    public static void ShowSabotageWarning(string target)
    {
        if (instance == null) return;
        string clue = string.Format(SabotageClues[Random.Range(0, SabotageClues.Length)], target);
        instance.AddToast(Kind.Sabotage, "¡ADVERTENCIA! INTENTO DE SABOTAJE",
            clue + "\nRevisa la cocina y junta pistas para el Capibara-policía.", 7f);
        instance.flashUntil = Time.time + 1.5f;
    }

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted += OnGameStarted;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted -= OnGameStarted;
        if (page != Page.None) Time.timeScale = savedTimeScale;
        if (instance == this) instance = null;
        if (circleTexture != null) Destroy(circleTexture);
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;
        var state = GameManager.Instance.CurrentState;

        if (showIntro && !introShown && state == GameManager.GameState.Ready)
        {
            introShown = true;
            Open(Page.Story, true);
        }

        if (Keyboard.current != null) HandleKeys(Keyboard.current);

        if (state == GameManager.GameState.Playing && page == Page.None) UpdateWarnings();
        toasts.RemoveAll(t => t.closed || Time.time > t.end);
    }

    private void HandleKeys(Keyboard kb)
    {
        if (page != Page.None)
        {
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame ||
                kb.spaceKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) Next();
            else if (kb.leftArrowKey.wasPressedThisFrame) Previous();
            else if (kb.escapeKey.wasPressedThisFrame) Close();
            return;
        }

        if (kb.f1Key.wasPressedThisFrame) Open(Page.Cards, false);
        else if (kb.f2Key.wasPressedThisFrame) Open(Page.Curriculum, false);
        else if (kb.f3Key.wasPressedThisFrame) Open(Page.Story, false);
    }

    // ---------------------------------------------------------------- navegacion

    private void Open(Page target, bool intro)
    {
        // Pausa: el reloj de la partida y los pedidos usan Time.deltaTime
        if (page == Page.None)
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        page = target;
        index = 0;
        introFlow = intro;
    }

    private void Close()
    {
        if (page == Page.None) return;
        page = Page.None;
        Time.timeScale = savedTimeScale;
        closedFrame = Time.frameCount;
    }

    private void Next()
    {
        switch (page)
        {
            case Page.Story:
                if (++index < Story.Length) return;
                if (introFlow) { page = Page.Curriculum; index = 0; }
                else Close();
                break;
            case Page.Curriculum:
                if (introFlow) { page = Page.Cards; index = 0; }
                else Close();
                break;
            case Page.Cards:
                if (++index >= Characters.Length) Close();
                break;
        }
    }

    private void Previous()
    {
        if (index > 0) { index--; return; }
        if (!introFlow) return;
        if (page == Page.Cards) page = Page.Curriculum;
        else if (page == Page.Curriculum) { page = Page.Story; index = Story.Length - 1; }
    }

    private bool IsLastStep()
    {
        switch (page)
        {
            case Page.Story: return !introFlow && index == Story.Length - 1;
            case Page.Curriculum: return !introFlow;
            case Page.Cards: return index == Characters.Length - 1;
        }
        return true;
    }

    // ---------------------------------------------------------------- avisos de la partida

    private void OnGameStarted()
    {
        toasts.Clear();
        warnedOrders.Clear();
        hurryShown = false;
        flashUntil = 0f;
        nextSabotage = Time.time + Random.Range(sabotageMinInterval, sabotageMaxInterval);
        StartExploreTour();
    }

    private void StartExploreTour()
    {
        landmarks.Clear();
        AddLandmark<CookingStation>("Fogón de Doña Rosa");
        AddLandmark<IngredientCrate>("Cajas de insumos");
        AddLandmark<WashingStation>("Lavaplatos (platos limpios)");
        AddLandmark<AssemblyStation>("Mesa de armado");
        AddLandmark<FoodCourtTable>("Mesas del comedor");

        exploreToast = AddToast(Kind.Explore, "PASEA PARA UBICARTE DEL MAPA",
            "Recorre el mercado antes de que lleguen los pedidos. Acércate a:", exploreMaxSeconds);
    }

    private void AddLandmark<T>(string label) where T : Component
    {
        T[] found = FindObjectsByType<T>();
        if (found.Length == 0) return;
        var spots = new Transform[found.Length];
        for (int i = 0; i < found.Length; i++) spots[i] = found[i].transform;
        landmarks.Add(new Landmark { label = label, spots = spots });
    }

    private void UpdateWarnings()
    {
        UpdateExploreTour();

        float left = GameManager.Instance.GetTimeRemaining();
        int pending = OrderSystem.Instance != null ? OrderSystem.Instance.GetActiveOrders().Count : 0;
        if (!hurryShown && left <= hurryAtSeconds)
        {
            hurryShown = true;
            AddToast(Kind.Hurry, "¡APRESÚRATE, SE TE ACABA EL TIEMPO!",
                "Quedan " + Mathf.CeilToInt(left) + " segundos de partida." +
                (pending > 0 ? "\nTodavía hay " + pending + (pending == 1 ? " pedido esperando." : " pedidos esperando.") : ""), 6f);
            flashUntil = Time.time + 1f;
        }

        // Una sola vez por pedido: la mesa se impacienta
        if (OrderSystem.Instance != null)
        {
            foreach (Order order in OrderSystem.Instance.GetActiveOrders())
            {
                if (order.timeRemaining > orderHurrySeconds || !warnedOrders.Add(order)) continue;
                string table = order.table != null ? order.table.DisplayName : "Una mesa";
                AddToast(Kind.Hurry, "¡APRESÚRATE!",
                    table + " se impacienta: " + order.dishName + " vence en " + Mathf.CeilToInt(order.timeRemaining) + " s.", 4f);
            }
        }

        if (simulateSabotage && Time.time >= nextSabotage)
        {
            nextSabotage = Time.time + Random.Range(sabotageMinInterval, sabotageMaxInterval);
            ShowSabotageWarning(RandomSabotageTarget());
        }
    }

    private void UpdateExploreTour()
    {
        if (exploreToast == null) return;
        if (exploreToast.closed || Time.time > exploreToast.end) { exploreToast = null; return; }

        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        Vector3 p = player.transform.position;
        bool all = landmarks.Count > 0;
        foreach (Landmark mark in landmarks)
        {
            if (!mark.visited)
            {
                foreach (Transform spot in mark.spots)
                {
                    if (spot == null) continue;
                    Vector2 flat = new Vector2(spot.position.x - p.x, spot.position.z - p.z);
                    if (flat.magnitude <= landmarkRadius) { mark.visited = true; break; }
                }
            }
            all &= mark.visited;
        }

        if (all)
        {
            exploreToast.title = "¡YA CONOCES EL MERCADO!";
            exploreToast.body = "Visitaste todos los puntos de la cocina de Doña Rosa.";
            exploreToast.end = Time.time + 3f;
            exploreToast = null;
        }
    }

    private string RandomSabotageTarget()
    {
        var names = new List<string>();
        foreach (var s in FindObjectsByType<CookingStation>()) names.Add(s.DisplayName);
        foreach (var s in FindObjectsByType<ChoppingStation>()) names.Add(s.DisplayName);
        foreach (var s in FindObjectsByType<AssemblyStation>()) names.Add(s.DisplayName);
        return names.Count > 0 ? names[Random.Range(0, names.Count)] : "el fogón de Doña Rosa";
    }

    private Toast AddToast(Kind kind, string title, string text, float seconds)
    {
        var toast = new Toast { kind = kind, title = title, body = text, start = Time.time, end = Time.time + seconds };
        toasts.Add(toast);
        // Maximo 4 a la vez: se va el mas viejo que no sea un sabotaje
        while (toasts.Count > MaxToasts)
        {
            int i = toasts.FindIndex(t => t.kind != Kind.Sabotage && t != toast);
            if (i < 0) i = 0;
            toasts[i].closed = true;
            toasts.RemoveAt(i);
        }
        return toast;
    }

    // ---------------------------------------------------------------- dibujo

    private void OnGUI()
    {
        if (GameManager.Instance == null) return;
        GUI.depth = -10; // encima del GameHUD
        SetupStyles();

        if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            DrawFlash();
            DrawToasts();
        }
        if (page != Page.None) DrawModal();
    }

    private void DrawToasts()
    {
        float w = Mathf.Min(Scale(430), Screen.width * 0.45f);
        float y = Scale(16);
        float now = Time.time;

        foreach (Toast t in toasts)
        {
            if (t.closed || now > t.end) continue;
            float h = ToastHeight(t, w);

            // Entra deslizandose desde la derecha y se desvanece al final
            float slide = Mathf.Clamp01((now - t.start) / 0.35f);
            slide = 1f - Mathf.Pow(1f - slide, 3f);
            float alpha = Mathf.Clamp01((t.end - now) / 0.5f);
            float x = Screen.width - Scale(16) - w + (1f - slide) * (w + Scale(32));

            DrawToast(t, new Rect(x, y, w, h), alpha);
            y += h + Scale(10);
        }
        GUI.color = Color.white;
    }

    private float ToastTextWidth(float w) => w - Scale(16 + 34 + 12 + 34);

    private float ToastHeight(Toast t, float w)
    {
        float h = Scale(42) + toastBody.CalcHeight(new GUIContent(t.body), ToastTextWidth(w)) + Scale(12);
        if (t.kind == Kind.Explore) h += landmarks.Count * Scale(24) + Scale(4);
        return Mathf.Max(Scale(64), h);
    }

    private void DrawToast(Toast t, Rect r, float alpha)
    {
        Color tone = t.kind == Kind.Explore ? accentColor : urgentColor;
        GUI.color = new Color(1f, 1f, 1f, alpha);

        // El sabotaje tiene un borde rojo que late
        if (t.kind == Kind.Sabotage)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f);
            Fill(Grow(r, Scale(3)), new Color(tone.r, tone.g, tone.b, 0.35f + 0.65f * pulse));
        }
        Fill(r, panelColor);
        Fill(new Rect(r.x, r.y, Scale(6), r.height), tone);

        Rect icon = new Rect(r.x + Scale(16), r.y + Scale(12), Scale(34), Scale(34));
        DrawCircle(icon, tone);
        GUI.Label(icon, t.kind == Kind.Explore ? "i" : "!", iconStyle);

        Rect close = new Rect(r.xMax - Scale(30), r.y + Scale(6), Scale(24), Scale(24));
        GUI.Label(close, "x", center);
        if (GUI.Button(close, GUIContent.none, GUIStyle.none)) t.closed = true;

        float tx = icon.xMax + Scale(12), tw = ToastTextWidth(r.width);
        toastTitle.normal.textColor = tone;
        GUI.Label(new Rect(tx, r.y + Scale(10), tw, Scale(28)), t.title, toastTitle);

        float by = r.y + Scale(42);
        float bh = toastBody.CalcHeight(new GUIContent(t.body), tw);
        GUI.Label(new Rect(tx, by, tw, bh), t.body, toastBody);
        by += bh + Scale(4);

        if (t.kind != Kind.Explore) return;
        foreach (Landmark mark in landmarks)
        {
            Rect box = new Rect(tx, by + Scale(5), Scale(14), Scale(14));
            Fill(box, mark.visited ? accentColor : new Color(1f, 1f, 1f, 0.25f));
            toastBody.normal.textColor = mark.visited ? accentColor : Color.white;
            GUI.Label(new Rect(box.xMax + Scale(10), by, tw - Scale(24), Scale(24)), mark.label, toastBody);
            by += Scale(24);
        }
        toastBody.normal.textColor = Color.white;
    }

    // Borde rojo de la pantalla cuando hay sabotaje o se acaba el tiempo
    private void DrawFlash()
    {
        if (Time.time > flashUntil) return;
        float a = Mathf.Clamp01(flashUntil - Time.time) * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 12f)) * 0.55f;
        Color c = new Color(urgentColor.r, urgentColor.g, urgentColor.b, a);
        float b = Scale(14);
        Fill(new Rect(0, 0, Screen.width, b), c);
        Fill(new Rect(0, Screen.height - b, Screen.width, b), c);
        Fill(new Rect(0, b, b, Screen.height - b * 2), c);
        Fill(new Rect(Screen.width - b, b, b, Screen.height - b * 2), c);
    }

    private void DrawModal()
    {
        Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.65f));

        float w = Mathf.Min(Screen.width - Scale(32), Scale(940));
        float h = Mathf.Min(Screen.height - Scale(32), Scale(600));
        Rect r = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
        Fill(Grow(r, Scale(3)), accentColor);
        Fill(r, panelColor);

        Rect content = new Rect(r.x, r.y, r.width, r.height - Scale(64));
        switch (page)
        {
            case Page.Story: DrawStory(content); break;
            case Page.Curriculum: DrawCurriculum(content); break;
            case Page.Cards: DrawCard(content); break;
        }
        DrawFooter(new Rect(r.x, r.yMax - Scale(64), r.width, Scale(64)));
    }

    private void DrawFooter(Rect r)
    {
        float bh = Scale(40), y = r.y + (r.height - bh) / 2f;

        if (index > 0 || (introFlow && page != Page.Story))
            if (Button(new Rect(r.x + Scale(20), y, Scale(150), bh), "< Anterior", false)) Previous();

        string skip = introFlow ? "Saltar (ESC)" : "Cerrar (ESC)";
        if (!IsLastStep() && Button(new Rect(r.x + Scale(180), y, Scale(160), bh), skip, false)) Close();

        string next = IsLastStep() ? (introFlow ? "¡A cocinar!" : "Cerrar") : "Siguiente >";
        if (Button(new Rect(r.xMax - Scale(200), y, Scale(180), bh), next, true)) Next();

        // Puntos de progreso
        int count = page == Page.Story ? Story.Length : page == Page.Cards ? Characters.Length : 0;
        float dot = Scale(10), gap = Scale(8);
        float dx = r.x + r.width / 2f - (count * dot + (count - 1) * gap) / 2f + Scale(40);
        for (int i = 0; i < count; i++)
            DrawCircle(new Rect(dx + i * (dot + gap), r.y + (r.height - dot) / 2f, dot, dot),
                i == index ? accentColor : new Color(1f, 1f, 1f, 0.3f));
    }

    private void DrawStory(Rect r)
    {
        StoryPage story = Story[index];
        Rect band = new Rect(r.x, r.y, r.width, Scale(170));
        DrawMarket(band, story.sky);

        GUILayout.BeginArea(new Rect(r.x + Scale(40), band.yMax + Scale(16), r.width - Scale(80), r.height - band.height - Scale(16)));
        header.normal.textColor = accentColor;
        GUILayout.Label("HISTORIA  ·  " + (index + 1) + " / " + Story.Length, header);
        GUILayout.Label(story.title, bigTitle);
        GUILayout.Space(Scale(6));
        GUILayout.Label(story.body, body);
        GUILayout.EndArea();
    }

    // Una fila de kioskos con toldos de rayas (rojo, amarillo y verde) bajo el cielo de la pagina
    private void DrawMarket(Rect band, Color sky)
    {
        Fill(band, sky);
        DrawCircle(new Rect(band.xMax - Scale(140), band.y + Scale(18), Scale(54), Scale(54)), new Color(1f, 0.92f, 0.6f, 0.9f));

        Color[] stripes = { new Color(0.80f, 0.16f, 0.16f), new Color(0.98f, 0.80f, 0.15f), new Color(0.10f, 0.55f, 0.25f) };
        int stalls = 7;
        float sw = band.width / stalls;
        float roof = band.y + band.height * 0.42f, awning = band.height * 0.16f;
        for (int i = 0; i < stalls; i++)
        {
            float x = band.x + i * sw;
            Fill(new Rect(x + Scale(4), roof + awning, sw - Scale(8), band.yMax - roof - awning), i % 2 == 0 ? new Color(0.45f, 0.30f, 0.18f) : new Color(0.55f, 0.38f, 0.22f));
            Fill(new Rect(x + Scale(14), roof + awning + Scale(14), sw - Scale(28), Scale(30)), new Color(0.12f, 0.10f, 0.08f, 0.6f));
            Fill(new Rect(x + Scale(4), band.yMax - Scale(26), sw - Scale(8), Scale(8)), new Color(0.85f, 0.75f, 0.60f));

            int n = 6;
            float stripe = (sw - Scale(8)) / n;
            for (int k = 0; k < n; k++)
                Fill(new Rect(x + Scale(4) + k * stripe, roof, stripe, awning), stripes[(k + i) % stripes.Length]);
        }
        Fill(new Rect(band.x, band.yMax - Scale(4), band.width, Scale(4)), accentColor);
    }

    private void DrawCurriculum(Rect r)
    {
        Rect sheet = new Rect(r.x + Scale(20), r.y + Scale(20), r.width - Scale(40), r.height - Scale(28));
        Fill(sheet, paperColor);

        // Cabecera con foto, nombre y puesto
        Rect head = new Rect(sheet.x, sheet.y, sheet.width, Scale(100));
        Fill(head, new Color(0.18f, 0.40f, 0.34f));
        Rect photo = new Rect(head.x + Scale(20), head.y + Scale(12), Scale(76), Scale(76));
        DrawCircle(Grow(photo, Scale(3)), paperColor);
        DrawCircle(photo, Characters[0].color);
        GUI.Label(photo, "T", initialStyle);

        GUI.Label(new Rect(photo.xMax + Scale(20), head.y + Scale(14), head.width, Scale(44)), "TOMÁS", bigTitle);
        body.normal.textColor = Color.white;
        GUI.Label(new Rect(photo.xMax + Scale(22), head.y + Scale(58), head.width, Scale(30)), "Garzón · Cocina de Doña Rosa, Mercado Lanza", small);
        header.normal.textColor = accentColor;
        header.alignment = TextAnchor.UpperRight;
        GUI.Label(new Rect(head.x, head.y + Scale(16), head.width - Scale(20), Scale(30)), "CURRÍCULUM VITAE", header);
        header.alignment = TextAnchor.UpperLeft;

        float top = head.yMax + Scale(14), height = sheet.yMax - top - Scale(8);
        float leftW = sheet.width * 0.36f;

        GUILayout.BeginArea(new Rect(sheet.x + Scale(20), top, leftW - Scale(30), height));
        GUILayout.Label("DATOS PERSONALES", paperHeader);
        GUILayout.Label("<b>Edad:</b> 22 años\n<b>Vive en:</b> Zona Max Paredes, La Paz\n<b>Puesto:</b> garzón y ayudante de cocina\n<b>Disponible:</b> desde las 6:00 a.m.", paperBody);
        GUILayout.Space(Scale(8));
        GUILayout.Label("IDIOMAS", paperHeader);
        GUILayout.Label("Castellano (nativo)\nAymara (para saludar a las caseras)\nInglés (lo justo para los turistas)", paperBody);
        GUILayout.Space(Scale(8));
        GUILayout.Label("REFERENCIAS", paperHeader);
        GUILayout.Label("<b>Doña Rosa, la Casera</b>\n\"Es rápido y honrado. Si se le cae un plato, lo agarra antes de que toque el piso.\"", paperBody);
        GUILayout.EndArea();

        Fill(new Rect(sheet.x + leftW, top, Scale(2), height), new Color(inkColor.r, inkColor.g, inkColor.b, 0.2f));

        GUILayout.BeginArea(new Rect(sheet.x + leftW + Scale(20), top, sheet.width - leftW - Scale(40), height));
        GUILayout.Label("PERFIL", paperHeader);
        GUILayout.Label("Joven paceño, rápido como minibús en bajada. Conoce cada pasillo del Mercado Lanza y a cada casera por su nombre. Nunca olvida un pedido.", paperBody);
        GUILayout.Space(Scale(8));
        GUILayout.Label("EXPERIENCIA", paperHeader);
        GUILayout.Label("<b>2024 – 2026 · Ayudante de salteñería, calle Comercio</b>\nLlevaba salteñas a las mesas sin derramar ni una gota de jugo.", paperBody);
        GUILayout.Label("<b>2022 – 2024 · Voceador de minibús, ruta Miraflores – Pérez</b>\nAprendió a gritar «¡Lanza, Lanza!» por encima del tráfico.", paperBody);
        GUILayout.Label("<b>2021 · Cargador de canastas, feria de la 16 de Julio (El Alto)</b>", paperBody);
        GUILayout.Space(Scale(8));
        GUILayout.Label("HABILIDADES", paperHeader);
        StatRow("Velocidad", 9, paperBody, new Color(0.18f, 0.40f, 0.34f), new Color(inkColor.r, inkColor.g, inkColor.b, 0.15f));
        StatRow("Equilibrio con platos", 8, paperBody, new Color(0.18f, 0.40f, 0.34f), new Color(inkColor.r, inkColor.g, inkColor.b, 0.15f));
        StatRow("Memoria de pedidos", 7, paperBody, new Color(0.18f, 0.40f, 0.34f), new Color(inkColor.r, inkColor.g, inkColor.b, 0.15f));
        StatRow("Olfato para pistas", 6, paperBody, new Color(0.18f, 0.40f, 0.34f), new Color(inkColor.r, inkColor.g, inkColor.b, 0.15f));
        GUILayout.EndArea();
    }

    private void DrawCard(Rect r)
    {
        Character c = Characters[index];

        // Lado izquierdo: retrato con el color del personaje
        float leftW = r.width * 0.36f;
        Rect side = new Rect(r.x, r.y, leftW, r.height);
        Fill(side, Color.Lerp(c.color, Color.black, 0.45f));

        float size = Mathf.Min(leftW - Scale(80), Scale(200));
        Rect photo = new Rect(side.x + (leftW - size) / 2f, side.y + Scale(36), size, size);
        DrawCircle(Grow(photo, Scale(5)), accentColor);
        DrawCircle(photo, c.color);
        GUI.Label(photo, c.initial, initialStyle);

        float ty = photo.yMax + Scale(20);
        bigTitle.alignment = TextAnchor.UpperCenter;
        GUI.Label(new Rect(side.x, ty, leftW, Scale(44)), c.name, bigTitle);
        bigTitle.alignment = TextAnchor.UpperLeft;
        GUI.Label(new Rect(side.x, ty + Scale(46), leftW, Scale(28)), "«" + c.alias + "»", center);

        Rect tag = new Rect(side.x + Scale(30), ty + Scale(84), leftW - Scale(60), Scale(30));
        Fill(tag, accentColor);
        small.normal.textColor = panelColor;
        small.alignment = TextAnchor.MiddleCenter;
        GUI.Label(tag, c.role, small);
        small.normal.textColor = Color.white;
        small.alignment = TextAnchor.UpperLeft;

        // Lado derecho: descripcion, estadisticas, habilidad y debilidad
        GUILayout.BeginArea(new Rect(side.xMax + Scale(32), r.y + Scale(28), r.width - leftW - Scale(64), r.height - Scale(36)));
        header.normal.textColor = accentColor;
        GUILayout.Label("FICHA DE PERSONAJE  ·  " + (index + 1) + " / " + Characters.Length, header);
        GUILayout.Space(Scale(4));
        GUILayout.Label(c.about, body);
        GUILayout.Space(Scale(12));
        for (int i = 0; i < c.stats.Length; i++)
            StatRow(c.statNames[i], c.stats[i], small, accentColor, new Color(1f, 1f, 1f, 0.15f));
        GUILayout.Space(Scale(12));
        GUILayout.Label("HABILIDAD", header);
        GUILayout.Label(c.skill, small);
        GUILayout.Space(Scale(6));
        header.normal.textColor = urgentColor;
        GUILayout.Label("DEBILIDAD", header);
        header.normal.textColor = accentColor;
        GUILayout.Label(c.weakness, small);
        GUILayout.EndArea();
    }

    // Fila "Nombre  [######----]" dentro de un GUILayout
    private void StatRow(string label, int value, GUIStyle style, Color fill, Color track)
    {
        Rect row = GUILayoutUtility.GetRect(0f, Scale(26), GUILayout.ExpandWidth(true));
        float labelW = Mathf.Min(Scale(210), row.width * 0.5f);
        var wrap = style.wordWrap;
        style.wordWrap = false;
        GUI.Label(new Rect(row.x, row.y, labelW, row.height), label, style);
        style.wordWrap = wrap;

        Rect bar = new Rect(row.x + labelW, row.y + (row.height - Scale(12)) / 2f, row.width - labelW - Scale(4), Scale(12));
        Fill(bar, track);
        Fill(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(value / 10f), bar.height), fill);
    }

    private bool Button(Rect r, string label, bool primary)
    {
        bool hover = r.Contains(Event.current.mousePosition);
        Color bg = primary ? accentColor : new Color(1f, 1f, 1f, hover ? 0.22f : 0.12f);
        if (primary && hover) bg = Color.Lerp(accentColor, Color.white, 0.2f);
        Fill(r, bg);
        center.normal.textColor = primary ? panelColor : Color.white;
        GUI.Label(r, label, center);
        center.normal.textColor = Color.white;
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    // ---------------------------------------------------------------- utilidades

    // Escala la interfaz segun la altura de la pantalla (referencia 1080p), igual que GameHUD
    private float Scale(float v) => v * Mathf.Max(0.8f, Screen.height / 1080f);

    private static Rect Grow(Rect r, float d) => new Rect(r.x - d, r.y - d, r.width + d * 2, r.height + d * 2);

    private static void Fill(Rect r, Color c)
    {
        Color old = GUI.color;
        GUI.color = new Color(c.r, c.g, c.b, c.a * old.a);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    private void DrawCircle(Rect r, Color c)
    {
        if (circleTexture == null)
        {
            const int size = 128;
            circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float radius = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                    circleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - d))); // borde suavizado
                }
            circleTexture.Apply();
        }
        Color old = GUI.color;
        GUI.color = new Color(c.r, c.g, c.b, c.a * old.a);
        GUI.DrawTexture(r, circleTexture);
        GUI.color = old;
    }

    private void SetupStyles()
    {
        int size = Mathf.RoundToInt(Scale(20));
        if (body != null && body.fontSize == size) return;

        body = new GUIStyle(GUI.skin.label) { fontSize = size, wordWrap = true, alignment = TextAnchor.UpperLeft, richText = true };
        body.normal.textColor = Color.white;
        small = new GUIStyle(body) { fontSize = Mathf.RoundToInt(size * 0.85f) };
        header = new GUIStyle(body) { fontSize = Mathf.RoundToInt(size * 0.8f), fontStyle = FontStyle.Bold, wordWrap = false };
        bigTitle = new GUIStyle(body) { fontSize = Mathf.RoundToInt(size * 1.7f), fontStyle = FontStyle.Bold, wordWrap = false };
        bigTitle.normal.textColor = accentColor;
        center = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
        initialStyle = new GUIStyle(center) { fontSize = Mathf.RoundToInt(size * 2.6f), fontStyle = FontStyle.Bold };
        toastTitle = new GUIStyle(body) { fontSize = Mathf.RoundToInt(size * 0.85f), fontStyle = FontStyle.Bold, wordWrap = false, clipping = TextClipping.Clip };
        toastBody = new GUIStyle(body) { fontSize = Mathf.RoundToInt(size * 0.8f) };
        iconStyle = new GUIStyle(center) { fontSize = Mathf.RoundToInt(size * 1.1f), fontStyle = FontStyle.Bold };

        paperBody = new GUIStyle(body) { fontSize = Mathf.RoundToInt(size * 0.75f) };
        paperBody.normal.textColor = inkColor;
        paperHeader = new GUIStyle(header) { fontSize = Mathf.RoundToInt(size * 0.75f) };
        paperHeader.normal.textColor = new Color(0.70f, 0.36f, 0.05f);
    }
}
