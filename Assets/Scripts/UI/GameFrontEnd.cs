using UnityEngine;
using UnityEngine.InputSystem;

// Menú previo y recetario con vistas de modelos 3D originales.
public class GameFrontEnd : MonoBehaviour
{
    public static GameFrontEnd Instance { get; private set; }
    public static bool GuideVisible => Instance != null && Instance.guide;
    public static bool BlocksInput => Instance != null && (Instance.guide || Instance.closedFrame == Time.frameCount);
    public LocalMultiplayer.GameMode SelectedMode { get; private set; } = LocalMultiplayer.GameMode.SinglePlayer;
    public int WorkerSkin { get; private set; }
    public int RivalSkin { get; private set; } = 1;
    private bool guide;
    private float savedScale;
    private int closedFrame = -1, selectedRecipe;
    private RecipeData[] recipes;
    private KitchenVisualLibrary visuals;
    private GUIStyle title, heading, body, muted, button, smallButton, centered;
    private readonly Color background = new Color(0.035f, 0.095f, 0.085f);
    private readonly Color panel = new Color(0.08f, 0.20f, 0.17f);
    private readonly Color gold = new Color(0.96f, 0.67f, 0.24f);
    private void Awake()
    {
        Instance = this;
        visuals = new KitchenVisualLibrary();
        recipes = Resources.LoadAll<RecipeData>("Recipes");
    }
    private void Update()
    {
        if (Keyboard.current == null || GameManager.Instance == null || FloatingWindows.BlocksInput) return;
        if (Keyboard.current.f4Key.wasPressedThisFrame) ToggleGuide();
        else if (guide && Keyboard.current.escapeKey.wasPressedThisFrame) CloseGuide();
    }
    public bool SelectMode(LocalMultiplayer.GameMode mode)
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Ready) return false;
        if (!LocalMultiplayer.Instance.ConfigureMode(mode)) return false;
        SelectedMode = mode;
        return true;
    }
    public bool SelectSkin(bool rival, int index)
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Ready) return false;
        PlayerController player = rival ? LocalMultiplayer.Instance.Rival : LocalMultiplayer.Instance.Worker;
        if (!player.GetComponent<CharacterAppearance>().Apply(index)) return false;
        if (rival) RivalSkin = index; else WorkerSkin = index;
        return true;
    }
    public bool StartSelectedGame()
    {
        if (guide || FloatingWindows.BlocksInput || GameManager.Instance.CurrentState != GameManager.GameState.Ready) return false;
        if (!SelectMode(SelectedMode)) return false;
        SelectSkin(false, WorkerSkin);
        SelectSkin(true, RivalSkin);
        GameManager.Instance.SetState(GameManager.GameState.Playing);
        return true;
    }
    public void ReturnToMenu()
    {
        CloseGuide();
        Time.timeScale = 1f;
        GameManager.Instance.SetState(GameManager.GameState.Ready);
        LocalMultiplayer.Instance.PrepareMenu();
    }
    public void ToggleGuide()
    {
        if (FloatingWindows.IsOpen || GameManager.Instance == null) return;
        var state = GameManager.Instance.CurrentState;
        if (state != GameManager.GameState.Ready && state != GameManager.GameState.Playing) return;
        if (guide) CloseGuide();
        else { guide = true; savedScale = Time.timeScale; Time.timeScale = 0f; }
    }
    public void CloseGuide()
    {
        if (!guide) return;
        guide = false;
        Time.timeScale = savedScale;
        closedFrame = Time.frameCount;
    }
    private void OnGUI()
    {
        if (!guide || FloatingWindows.IsOpen) return;
        Draw(true);
    }
    public void DrawReady() { if (!guide) Draw(false); }
    private void Styles()
    {
        if (title != null) return;
        title = new GUIStyle(GUI.skin.label) { fontSize = 42, fontStyle = FontStyle.Bold, wordWrap = true };
        heading = new GUIStyle(title) { fontSize = 26 };
        body = new GUIStyle(GUI.skin.label) { fontSize = 21, wordWrap = true };
        muted = new GUIStyle(body) { fontSize = 18 };
        centered = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter };
        button = new GUIStyle(GUI.skin.button) { fontSize = 23, wordWrap = true, alignment = TextAnchor.MiddleCenter };
        smallButton = new GUIStyle(button) { fontSize = 18 };
        title.normal.textColor = heading.normal.textColor = Color.white;
        body.normal.textColor = centered.normal.textColor = new Color(0.95f, 0.95f, 0.89f);
        muted.normal.textColor = new Color(0.70f, 0.80f, 0.74f);
    }
    private void Draw(bool showRecipes)
    {
        Styles();
        Matrix4x4 previous = GUI.matrix;
        Color color = GUI.color;
        int depth = GUI.depth;
        GUI.depth = -100;
        float scale = Mathf.Min(Screen.width / 1440f, Screen.height / 900f);
        Fill(new Rect(0, 0, Screen.width, Screen.height), background);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1440f * scale) / 2f, (Screen.height - 900f * scale) / 2f), Quaternion.identity, Vector3.one * scale);
        if (showRecipes) DrawRecipes(); else DrawHome();
        GUI.matrix = previous;
        GUI.color = color;
        GUI.depth = depth;
    }
    private void DrawHome()
    {
        GUI.Label(new Rect(64, 28, 1250, 58), "MERCADO LANZA", title);
        GUI.Label(new Rect(66, 88, 1250, 38), "Caos en la cocina · Prepara tu turno", body);
        if (Choice(new Rect(64, 146, 640, 95), "JUGAR INDIVIDUAL\nTú cocinas. El Rival actúa por su cuenta.", SelectedMode == LocalMultiplayer.GameMode.SinglePlayer))
            SelectMode(LocalMultiplayer.GameMode.SinglePlayer);
        if (Choice(new Rect(728, 146, 648, 95), "MULTIJUGADOR LOCAL\nDos personas, un teclado y dos vistas.", SelectedMode == LocalMultiplayer.GameMode.LocalMultiplayer))
            SelectMode(LocalMultiplayer.GameMode.LocalMultiplayer);
        DrawSkin(new Rect(64, 265, 640, 338), false);
        DrawSkin(new Rect(728, 265, 648, 338), true);
        Fill(new Rect(64, 628, 1312, 112), panel);
        GUI.Label(new Rect(88, 642, 820, 38), "Conoce los platos y elige los ingredientes correctos", heading);
        GUI.Label(new Rect(88, 685, 800, 34), "Modelos 3D de comida y pasos de preparación en el recetario.", muted);
        if (GUI.Button(new Rect(1000, 651, 350, 64), "VER PLATOS E INGREDIENTES", smallButton)) ToggleGuide();
        GUI.Label(new Rect(64, 772, 840, 58), SelectedMode == LocalMultiplayer.GameMode.SinglePlayer
            ? "WASD: moverse · E: cocinar e investigar · F4: recetario"
            : "Tomás: WASD + E · Rival: IJKL + O · U: tacle · F4: recetario", body);
        if (GUI.Button(new Rect(1000, 768, 376, 68), "COMENZAR PARTIDA", button)) StartSelectedGame();
        GUI.Label(new Rect(64, 849, 1312, 32), "F1: personajes · F2: currículum de Tomás · F3: historia · ENTER: comenzar", muted);
    }
    private void DrawSkin(Rect rect, bool rival)
    {
        Fill(rect, panel);
        int skin = rival ? RivalSkin : WorkerSkin;
        string label = rival ? (SelectedMode == LocalMultiplayer.GameMode.SinglePlayer ? "RIVAL AUTOMÁTICO" : "JUGADOR 2 · RIVAL") : "JUGADOR 1 · TRABAJADOR";
        GUI.Label(new Rect(rect.x + 24, rect.y + 16, rect.width - 48, 40), label, heading);
        Rect portrait = new Rect(rect.x + 22, rect.y + 65, 230, 242);
        Picture(portrait, visuals.SkinPicture(skin), CharacterSkinCatalog.Names[skin]);
        GUI.Label(new Rect(rect.x + 278, rect.y + 80, 330, 52), CharacterSkinCatalog.Names[skin], heading);
        GUI.Label(new Rect(rect.x + 278, rect.y + 140, 325, 82), "Elige tu apariencia. Tu rol y habilidades se mantienen.", body);
        if (GUI.Button(new Rect(rect.x + 278, rect.y + 238, 70, 58), "<", button))
            SelectSkin(rival, (skin + CharacterSkinCatalog.Names.Length - 1) % CharacterSkinCatalog.Names.Length);
        GUI.Label(new Rect(rect.x + 354, rect.y + 246, 170, 45), (skin + 1) + " / " + CharacterSkinCatalog.Names.Length, centered);
        if (GUI.Button(new Rect(rect.x + 533, rect.y + 238, 70, 58), ">", button))
            SelectSkin(rival, (skin + 1) % CharacterSkinCatalog.Names.Length);
    }
    private void DrawRecipes()
    {
        GUI.Label(new Rect(64, 28, 1100, 58), "RECETARIO DEL MERCADO", title);
        GUI.Label(new Rect(64, 91, 1170, 38), "Selecciona un plato para reconocer sus ingredientes antes de armarlo.", body);
        if (GUI.Button(new Rect(1190, 35, 186, 58), "VOLVER · ESC", smallButton)) CloseGuide();
        if (recipes.Length == 0) { GUI.Label(new Rect(64, 170, 1200, 80), "No hay recetas disponibles.", heading); return; }
        selectedRecipe = Mathf.Clamp(selectedRecipe, 0, recipes.Length - 1);
        float tabWidth = (1312f - (recipes.Length - 1) * 12) / recipes.Length;
        for (int i = 0; i < recipes.Length; i++)
            if (Choice(new Rect(64 + i * (tabWidth + 12), 151, tabWidth, 66), recipes[i].recipeName, i == selectedRecipe)) selectedRecipe = i;
        RecipeData recipe = recipes[selectedRecipe];
        Fill(new Rect(64, 241, 536, 527), panel);
        Picture(new Rect(82, 258, 500, 410), visuals.DishPicture(recipe), recipe.recipeName);
        GUI.Label(new Rect(88, 687, 488, 62), recipe.recipeName, heading);
        Fill(new Rect(624, 241, 752, 527), panel);
        GUI.Label(new Rect(650, 261, 700, 42), "QUÉ DEBES SELECCIONAR", heading);
        var types = recipe.requiredIngredients;
        for (int i = 0; i < types.Length; i++)
        {
            float x = 650 + i % 2 * 352;
            float y = 323 + i / 2 * 124;
            Fill(new Rect(x, y, 332, 106), new Color(0.055f, 0.135f, 0.115f));
            Picture(new Rect(x + 8, y + 10, 82, 82), visuals.IngredientIcon(types[i]), "");
            GUI.Label(new Rect(x + 100, y + 9, 220, 43), KitchenVisualLibrary.IngredientName(types[i]), body);
            GUI.Label(new Rect(x + 100, y + 56, 220, 45), Preparation(recipe), muted);
        }
        GUI.Label(new Rect(650, 594, 692, 140), "1. Consigue un plato limpio en el lavaplatos.\n2. Prepara los ingredientes indicados.\n3. Déjalos en la mesa de armado y recoge el plato terminado.", body);
        GUI.Label(new Rect(64, 803, 1312, 64), "Los modelos de los ingredientes coinciden con lo que recibes de sus cajas. F4 abre este recetario durante la partida y pausa el turno.", muted);
    }
    private static string Preparation(RecipeData recipe)
    {
        if (recipe.requiresChopping && recipe.requiresCooking) return "Picar y cocinar";
        if (recipe.requiresChopping) return "Picar";
        if (recipe.requiresCooking) return "Cocinar";
        return "Listo para armar";
    }
    private bool Choice(Rect rect, string text, bool selected)
    {
        Color old = GUI.backgroundColor;
        GUI.backgroundColor = selected ? gold : new Color(0.25f, 0.42f, 0.35f);
        bool pressed = GUI.Button(rect, text, button);
        GUI.backgroundColor = old;
        return pressed;
    }
    private void Picture(Rect rect, Texture texture, string fallback)
    {
        Fill(rect, new Color(0.055f, 0.13f, 0.115f));
        if (texture != null) GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
        else GUI.Label(rect, fallback, centered);
    }
    private static void Fill(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }
    private void OnDestroy()
    {
        CloseGuide();
        visuals?.Dispose();
        if (Instance == this) Instance = null;
    }
}
