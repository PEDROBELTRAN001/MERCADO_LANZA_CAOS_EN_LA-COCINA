using UnityEngine;
using UnityEngine.InputSystem;

// HUD minimo del prototipo (IMGUI: no necesita Canvas ni prefabs).
// Muestra inicio, tiempo, puntos, pedidos activos, que tiene enfrente el jugador y fin de partida.
// Controles: ENTER empieza, R reinicia al terminar.
public class GameHUD : MonoBehaviour
{
    [Header("Colores (estilo mercado paceño)")]
    public Color panelColor = new Color(0.12f, 0.30f, 0.26f, 0.85f);  // verde mercado
    public Color accentColor = new Color(0.94f, 0.60f, 0.19f);        // dorado
    public Color urgentColor = new Color(0.90f, 0.25f, 0.20f);        // rojo: pedido por vencer

    [Tooltip("Segundos restantes a partir de los cuales un pedido se marca como urgente")]
    public float urgentSeconds = 10f;

    private PlayerController player;
    private GUIStyle textStyle, titleStyle, centerStyle, cardStyle;
    private Texture2D panelTexture;

    // Aviso temporal ("¡Plato listo!", "Esta mesa pidio...") que cualquier estacion puede mostrar
    private static string notice;
    private static float noticeUntil;
    private static bool noticeIsWarning;

    public static void Notify(string message, float seconds = 2.5f)
    {
        // Un aviso normal no tapa una penalizacion que todavia se esta mostrando
        if (noticeIsWarning && Time.time < noticeUntil) return;
        notice = message;
        noticeUntil = Time.time + seconds;
        noticeIsWarning = false;
    }

    // Penalizaciones: en rojo y con prioridad sobre los avisos normales
    public static void Warn(string message, float seconds = 4f)
    {
        notice = message;
        noticeUntil = Time.time + seconds;
        noticeIsWarning = true;
    }

    // Los campos static sobreviven entre una prueba y otra en el editor (si no se recarga el dominio):
    // sin esto, un aviso de la partida anterior quedaba en pantalla y bloqueaba los nuevos
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearNotice()
    {
        notice = null;
        noticeUntil = 0f;
        noticeIsWarning = false;
    }

    private void Update()
    {
        if (GameManager.Instance == null || Keyboard.current == null) return;
        // Mientras hay una ventana flotante abierta (historia, fichas...) ENTER es para ella
        if (FloatingWindows.BlocksInput) return;

        var state = GameManager.Instance.CurrentState;
        bool enter = Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame;

        if (state == GameManager.GameState.Ready && enter)
        {
            ClearNotice();
            GameManager.Instance.SetState(GameManager.GameState.Playing);
        }
        else if (state == GameManager.GameState.GameOver && Keyboard.current.rKey.wasPressedThisFrame)
        {
            ClearNotice();
            GameManager.Instance.RestartGame();
        }
    }

    private void OnGUI()
    {
        if (GameManager.Instance == null) return;
        SetupStyles();

        switch (GameManager.Instance.CurrentState)
        {
            case GameManager.GameState.Ready:
                if (FloatingWindows.IsOpen) break;
                DrawCenterPanel("MERCADO LANZA: CAOS EN LA COCINA",
                    "Atiende los pedidos de las mesas antes de que se acabe el tiempo.\n\n" +
                    "WASD: moverse    Shift: correr    E: interactuar    Rueda del mouse: zoom\n" +
                    "F1: fichas de personaje    F2: currículum de Tomás    F3: historia\n\n" +
                    "Presiona ENTER para empezar");
                break;

            case GameManager.GameState.Playing:
                DrawTopBar();
                DrawOrders();
                DrawInteraction();
                DrawNotice();
                break;

            case GameManager.GameState.GameOver:
                DrawCenterPanel("¡SE ACABÓ EL TIEMPO!",
                    "Puntos: " + GameManager.Instance.GetCurrentScore() +
                    "   (meta: " + GameManager.Instance.targetScore + ")\n" +
                    "Ganados: +" + GameManager.Instance.PointsEarned +
                    "   ·   Penalizaciones: -" + GameManager.Instance.PointsLost + "\n" +
                    "Pedidos entregados: " + GameManager.Instance.OrdersDelivered +
                    "   ·   Clientes que se fueron sin comer: " + GameManager.Instance.OrdersLost + "\n" +
                    "Sabotajes del impostor: " + (SabotageSystem.Instance != null ? SabotageSystem.Instance.SabotageCount : 0) + "\n\n" +
                    (GameManager.Instance.GetCurrentScore() >= GameManager.Instance.targetScore
                        ? "¡Doña Rosa está orgullosa!" : "Los caseros se fueron con hambre...") +
                    "\n\nPresiona R para jugar otra vez");
                break;
        }
    }

    private void DrawTopBar()
    {
        float time = Mathf.Max(0f, GameManager.Instance.GetTimeRemaining());
        string clock = Mathf.FloorToInt(time / 60f) + ":" + Mathf.FloorToInt(time % 60f).ToString("00");

        Rect r = new Rect(Scale(16), Scale(16), Scale(330), Scale(44));
        GUI.DrawTexture(r, panelTexture);
        AndeanUI.DrawAguayo(new Rect(r.x, r.y - Scale(7), r.width, Scale(7)));
        textStyle.normal.textColor = time <= 30f ? urgentColor : Color.white;
        GUI.Label(Inset(r), "Tiempo  " + clock, textStyle);

        Rect s = new Rect(r.x + r.width - Scale(130), r.y, Scale(130), r.height);
        textStyle.normal.textColor = accentColor;
        GUI.Label(Inset(s), "Puntos  " + GameManager.Instance.GetCurrentScore(), textStyle);

        // Panel de penalizaciones a la derecha: clientes perdidos y puntos descontados
        Rect lost = new Rect(r.x + r.width + Scale(8), r.y, Scale(210), r.height);
        GUI.DrawTexture(lost, panelTexture);
        AndeanUI.DrawAguayo(new Rect(lost.x, lost.y - Scale(7), lost.width, Scale(7)));
        textStyle.normal.textColor = GameManager.Instance.OrdersLost > 0 ? urgentColor : Color.white;
        string lostPoints = GameManager.Instance.PointsLost > 0 ? "  (-" + GameManager.Instance.PointsLost + ")" : "";
        GUI.Label(Inset(lost), "Perdidos  " + GameManager.Instance.OrdersLost + lostPoints, textStyle);
        textStyle.normal.textColor = Color.white;
    }

    private void DrawOrders()
    {
        if (OrderSystem.Instance == null) return;
        var orders = OrderSystem.Instance.GetActiveOrders();

        float x = Scale(16), y = Scale(72);
        if (orders.Count == 0)
        {
            Rect wait = new Rect(x, y, Scale(250), Scale(40));
            GUI.DrawTexture(wait, panelTexture);
            AndeanUI.DrawAguayo(new Rect(wait.x, wait.y, wait.width, Scale(5)));
            GUI.Label(Inset(wait), "Esperando clientes...", textStyle);
            return;
        }

        // Una tarjeta por pedido, en fila (si no entran, pasan a la fila de abajo)
        float rowHeight = 0f;
        foreach (Order order in orders)
        {
            Vector2 size = OrderCardSize(order);
            if (x > Scale(16) && x + size.x > Screen.width - Scale(16))
            {
                x = Scale(16);
                y += rowHeight + Scale(8);
                rowHeight = 0f;
            }
            DrawOrderCard(order, new Rect(x, y, size.x, size.y));
            x += size.x + Scale(8);
            rowHeight = Mathf.Max(rowHeight, size.y);
        }
    }

    // Tarjeta de pedido (como una comanda): mesa y segundos, foto del plato, barra de tiempo
    // y los ingredientes con su preparacion; los que ya estan en la mesa de armado salen marcados.
    private float CardCell => Scale(46);

    private Vector2 OrderCardSize(Order order)
    {
        int n = order.recipe != null ? order.recipe.requiredIngredients.Length : 0;
        return new Vector2(Mathf.Max(Scale(178), Scale(16) + n * CardCell), Scale(166));
    }

    private void DrawOrderCard(Order order, Rect r)
    {
        bool urgent = order.timeRemaining <= urgentSeconds;
        if (urgent)
        {
            // Borde rojo que late cuando el pedido esta por vencer
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 10f);
            Fill(new Rect(r.x - Scale(3), r.y - Scale(3), r.width + Scale(6), r.height + Scale(6)), new Color(urgentColor.r, urgentColor.g, urgentColor.b, 0.4f + 0.6f * pulse));
        }
        GUI.DrawTexture(r, panelTexture);
        AndeanUI.DrawAguayo(new Rect(r.x, r.y, r.width, Scale(5)));

        // Mesa y segundos
        float pad = Scale(8);
        string table = order.table != null ? order.table.DisplayName : "";
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, order.timeRemaining));
        cardStyle.alignment = TextAnchor.UpperLeft;
        cardStyle.normal.textColor = accentColor;
        GUI.Label(new Rect(r.x + pad, r.y + Scale(8), r.width - pad * 2, Scale(20)), table.ToUpper(), cardStyle);
        cardStyle.alignment = TextAnchor.UpperRight;
        cardStyle.normal.textColor = urgent ? urgentColor : Color.white;
        GUI.Label(new Rect(r.x + pad, r.y + Scale(8), r.width - pad * 2, Scale(20)), seconds + " s", cardStyle);

        // Foto del plato y su nombre
        Rect photo = new Rect(r.x + pad, r.y + Scale(28), Scale(48), Scale(48));
        Texture2D dishIcon = GameIcons.Dish(order.recipe);
        if (dishIcon != null) GUI.DrawTexture(photo, dishIcon, ScaleMode.ScaleToFit);
        cardStyle.alignment = TextAnchor.MiddleLeft;
        cardStyle.normal.textColor = Color.white;
        cardStyle.wordWrap = true;
        GUI.Label(new Rect(photo.xMax + Scale(6), photo.y, r.xMax - photo.xMax - Scale(6) - pad, photo.height), order.dishName, cardStyle);
        cardStyle.wordWrap = false;

        // Barra de tiempo: verde -> amarillo -> rojo
        float left = order.timeLimit > 0f ? Mathf.Clamp01(order.timeRemaining / order.timeLimit) : 0f;
        Rect bar = new Rect(r.x + pad, photo.yMax + Scale(6), r.width - pad * 2, Scale(7));
        Fill(bar, new Color(0f, 0f, 0f, 0.35f));
        Color barColor = left > 0.5f ? Color.Lerp(new Color(0.98f, 0.80f, 0.16f), new Color(0.30f, 0.80f, 0.35f), (left - 0.5f) * 2f)
                                     : Color.Lerp(urgentColor, new Color(0.98f, 0.80f, 0.16f), left * 2f);
        Fill(new Rect(bar.x, bar.y, bar.width * left, bar.height), barColor);

        // Ingredientes: icono arriba, preparacion abajo; verde si ya esta en el plato
        if (order.recipe == null) return;
        var ingredients = order.recipe.requiredIngredients;
        float cellsWidth = ingredients.Length * CardCell;
        float cx = r.x + (r.width - cellsWidth) / 2f, cy = bar.yMax + Scale(6);
        AssemblyStation assembly = AssemblyStation.Instance;
        for (int i = 0; i < ingredients.Length; i++)
        {
            Rect cell = new Rect(cx + i * CardCell + Scale(2), cy, CardCell - Scale(4), r.yMax - cy - Scale(6));
            bool done = assembly != null && assembly.IsOnPlate(order.recipe, ingredients[i]);
            Fill(cell, done ? new Color(0.30f, 0.75f, 0.35f, 0.55f) : new Color(1f, 1f, 1f, 0.12f));

            Texture2D icon = GameIcons.Ingredient(ingredients[i]);
            Rect iconRect = new Rect(cell.x + (cell.width - Scale(36)) / 2f, cell.y + Scale(2), Scale(36), Scale(36));
            if (icon != null) GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

            Texture2D prep = GameIcons.Prep(order.recipe.PrepAt(i));
            Rect prepRect = new Rect(cell.x + (cell.width - Scale(20)) / 2f, iconRect.yMax + Scale(1), Scale(20), Scale(20));
            if (prep != null)
            {
                Fill(prepRect, new Color(1f, 1f, 1f, 0.85f));
                GUI.DrawTexture(prepRect, prep, ScaleMode.ScaleToFit);
            }

            // Marca de listo: circulo verde en la esquina
            if (done) Fill(new Rect(cell.xMax - Scale(11), cell.y + Scale(2), Scale(9), Scale(9)), new Color(0.2f, 0.9f, 0.3f));
        }
    }

    private static void Fill(Rect r, Color c)
    {
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    private void DrawInteraction()
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        string text = "";
        Texture2D heldIcon = null, targetIcon = null;
        Transform hand = player.transform.Find("Hand");
        if (hand != null && hand.childCount > 0)
        {
            GameObject held = hand.GetChild(0).gameObject;
            text = "En la mano: " + held.name;
            if (held.TryGetComponent(out Ingredient ingredient)) heldIcon = GameIcons.Ingredient(ingredient.ingredientType);
            else if (held.TryGetComponent(out DishItem dish)) heldIcon = GameIcons.Dish(dish.recipe);
        }

        Interactable target = player.CurrentInteractable;
        if (target != null)
        {
            string action = string.IsNullOrEmpty(target.interactionPrompt) ? "E" : target.interactionPrompt;
            text += (text.Length > 0 ? "      " : "") + action + "  →  " + target.DisplayName;
            if (target is IngredientCrate crate) targetIcon = GameIcons.Ingredient(crate.ingredientType);
        }
        if (text.Length == 0) return;

        float w = Scale(640), h = Scale(44);
        Rect r = new Rect((Screen.width - w) / 2f, Screen.height - h - Scale(16), w, h);
        GUI.DrawTexture(r, panelTexture);
        GUI.Label(r, text, centerStyle);

        // Iconos a los costados: lo que lleva (izquierda) y lo que va a sacar de la caja (derecha)
        float icon = Scale(38);
        if (heldIcon != null) GUI.DrawTexture(new Rect(r.x + Scale(6), r.y + (h - icon) / 2f, icon, icon), heldIcon, ScaleMode.ScaleToFit);
        if (targetIcon != null) GUI.DrawTexture(new Rect(r.xMax - Scale(6) - icon, r.y + (h - icon) / 2f, icon, icon), targetIcon, ScaleMode.ScaleToFit);
    }

    private void DrawNotice()
    {
        if (string.IsNullOrEmpty(notice) || Time.time > noticeUntil) return;

        float w = Scale(640), h = Scale(44);
        Rect r = new Rect((Screen.width - w) / 2f, Screen.height - h * 2 - Scale(24), w, h);
        GUI.DrawTexture(r, panelTexture);
        centerStyle.normal.textColor = noticeIsWarning ? urgentColor : accentColor;
        GUI.Label(r, notice, centerStyle);
        centerStyle.normal.textColor = Color.white;
    }

    private void DrawCenterPanel(string title, string body)
    {
        float w = Mathf.Min(Screen.width - Scale(32), Scale(760)), h = Scale(340);
        Rect r = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
        GUI.DrawTexture(r, panelTexture);

        // Bordes de aguayo arriba y abajo, chakanas a los lados del titulo
        AndeanUI.DrawAguayo(new Rect(r.x, r.y, r.width, Scale(16)));
        AndeanUI.DrawAguayo(new Rect(r.x, r.yMax - Scale(16), r.width, Scale(16)));
        float chakana = Scale(34);
        AndeanUI.DrawChakana(new Rect(r.x + Scale(24), r.y + Scale(28), chakana, chakana), accentColor);
        AndeanUI.DrawChakana(new Rect(r.xMax - Scale(24) - chakana, r.y + Scale(28), chakana, chakana), accentColor);

        titleStyle.normal.textColor = accentColor;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(r.x, r.y + Scale(20), r.width, Scale(50)), title, titleStyle);
        titleStyle.alignment = TextAnchor.MiddleLeft;

        GUI.Label(new Rect(r.x + Scale(24), r.y + Scale(80), r.width - Scale(48), r.height - Scale(100)), body, centerStyle);
    }

    // Escala la interfaz segun la altura de la pantalla (referencia 1080p)
    // (minimo 0.8 para que se lea en una vista Game pequeña)
    private float Scale(float v) => v * Mathf.Max(0.8f, Screen.height / 1080f);

    private Rect Inset(Rect r) => new Rect(r.x + Scale(12), r.y, r.width - Scale(24), r.height);

    private void SetupStyles()
    {
        if (panelTexture == null)
        {
            panelTexture = new Texture2D(1, 1);
            panelTexture.SetPixel(0, 0, panelColor);
            panelTexture.Apply();
        }

        int size = Mathf.RoundToInt(Scale(20));
        if (textStyle == null || textStyle.fontSize != size)
        {
            // Sin salto de linea: un texto largo partido en dos se encimaba con la fila siguiente
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = TextAnchor.MiddleLeft, wordWrap = false, clipping = TextClipping.Clip };
            textStyle.normal.textColor = Color.white;
            titleStyle = new GUIStyle(textStyle) { fontSize = Mathf.RoundToInt(size * 1.3f), fontStyle = FontStyle.Bold };
            centerStyle = new GUIStyle(textStyle) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            cardStyle = new GUIStyle(textStyle) { fontSize = Mathf.RoundToInt(size * 0.78f), fontStyle = FontStyle.Bold };
        }
    }

    private void OnDestroy()
    {
        if (panelTexture != null) Destroy(panelTexture);
    }
}
