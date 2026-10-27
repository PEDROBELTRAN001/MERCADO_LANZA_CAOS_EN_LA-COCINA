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
    private GUIStyle textStyle, titleStyle, centerStyle;
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
                    "WASD: moverse    E: interactuar    Rueda del mouse: zoom\n" +
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
                    "   ·   Clientes que se fueron sin comer: " + GameManager.Instance.OrdersLost + "\n\n" +
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
        textStyle.normal.textColor = time <= 30f ? urgentColor : Color.white;
        GUI.Label(Inset(r), "Tiempo  " + clock, textStyle);

        Rect s = new Rect(r.x + r.width - Scale(130), r.y, Scale(130), r.height);
        textStyle.normal.textColor = accentColor;
        GUI.Label(Inset(s), "Puntos  " + GameManager.Instance.GetCurrentScore(), textStyle);

        // Panel de penalizaciones a la derecha: clientes perdidos y puntos descontados
        Rect lost = new Rect(r.x + r.width + Scale(8), r.y, Scale(210), r.height);
        GUI.DrawTexture(lost, panelTexture);
        textStyle.normal.textColor = GameManager.Instance.OrdersLost > 0 ? urgentColor : Color.white;
        string lostPoints = GameManager.Instance.PointsLost > 0 ? "  (-" + GameManager.Instance.PointsLost + ")" : "";
        GUI.Label(Inset(lost), "Perdidos  " + GameManager.Instance.OrdersLost + lostPoints, textStyle);
        textStyle.normal.textColor = Color.white;
    }

    private void DrawOrders()
    {
        if (OrderSystem.Instance == null) return;
        var orders = OrderSystem.Instance.GetActiveOrders();

        float line = Scale(30);
        Rect panel = new Rect(Scale(16), Scale(68), Scale(430), Scale(44) + line * Mathf.Max(1, orders.Count));
        GUI.DrawTexture(panel, panelTexture);

        Rect row = new Rect(panel.x + Scale(12), panel.y + Scale(8), panel.width - Scale(24), line);
        titleStyle.normal.textColor = accentColor;
        GUI.Label(row, "PEDIDOS", titleStyle);
        row.y += line;

        if (orders.Count == 0)
        {
            GUI.Label(row, "Esperando clientes...", textStyle);
            return;
        }

        foreach (Order order in orders)
        {
            string table = order.table != null ? order.table.name.Replace("_", " ") + " · " : "";
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, order.timeRemaining));
            textStyle.normal.textColor = order.timeRemaining <= urgentSeconds ? urgentColor : Color.white;
            GUI.Label(row, table + order.dishName + "   " + seconds + " s", textStyle);
            row.y += line;
        }
        textStyle.normal.textColor = Color.white;
    }

    private void DrawInteraction()
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        string text = "";
        Transform hand = player.transform.Find("Hand");
        if (hand != null && hand.childCount > 0)
            text = "En la mano: " + hand.GetChild(0).name;

        Interactable target = player.CurrentInteractable;
        if (target != null)
        {
            string action = string.IsNullOrEmpty(target.interactionPrompt) ? "E" : target.interactionPrompt;
            text += (text.Length > 0 ? "      " : "") + action + "  →  " + target.DisplayName;
        }
        if (text.Length == 0) return;

        float w = Scale(640), h = Scale(44);
        Rect r = new Rect((Screen.width - w) / 2f, Screen.height - h - Scale(16), w, h);
        GUI.DrawTexture(r, panelTexture);
        GUI.Label(r, text, centerStyle);
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
        }
    }

    private void OnDestroy()
    {
        if (panelTexture != null) Destroy(panelTexture);
    }
}
