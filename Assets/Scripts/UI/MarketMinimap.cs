using UnityEngine;

// Plano completo con norte fijo, flecha de rumbo, caseros, mesas y objetos temporales.
public class MarketMinimap : MonoBehaviour
{
    private MarketLife market;
    private Texture2D map, leaf, sachet, arrow;
    private GUIStyle label, small;
    private Bounds bounds;
    public void Initialize(MarketLife owner)
    {
        market = owner; bounds = market.Layout.Bounds;
        leaf = MarketDrawing.Icon(0); sachet = MarketDrawing.Icon(1); arrow = MarketDrawing.Icon(2);
        BakeMap();
    }
    private Vector2 Pixel(Vector3 p, int resolution)
    {
        return new Vector2((p.x - bounds.min.x) / bounds.size.x * (resolution - 1),
            (p.z - bounds.min.z) / bounds.size.z * (resolution - 1));
    }
    private static float Cross(Vector2 a, Vector2 b, Vector2 p) => (b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x);
    private void BakeMap()
    {
        const int resolution = 512;
        var pixels = new Color32[resolution * resolution];
        Color32 background = new Color(.10f, .17f, .18f), road = new Color(.63f, .69f, .65f);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = background;
        var layout = market.Layout;
        for (int i = 0; i < layout.Indices.Count; i += 3)
        {
            Vector2 a = Pixel(layout.Vertices[layout.Indices[i]], resolution),
                b = Pixel(layout.Vertices[layout.Indices[i+1]], resolution),
                c = Pixel(layout.Vertices[layout.Indices[i+2]], resolution);
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x,c.x))));
            int maxX = Mathf.Min(resolution-1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x,c.x))));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y,c.y))));
            int maxY = Mathf.Min(resolution-1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y,c.y))));
            for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++)
            {
                Vector2 p = new Vector2(x+.5f, y+.5f);
                float ab = Cross(a,b,p), bc = Cross(b,c,p), ca = Cross(c,a,p);
                if ((ab >= 0 && bc >= 0 && ca >= 0) || (ab <= 0 && bc <= 0 && ca <= 0)) pixels[y*resolution+x] = road;
            }
        }
        map = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        map.SetPixels32(pixels); map.Apply(); map.wrapMode = TextureWrapMode.Clamp;
    }
    private Vector2 Position(Vector3 point, Rect rect)
    {
        return new Vector2(rect.x + Mathf.Clamp01((point.x-bounds.min.x)/bounds.size.x)*rect.width,
            rect.yMax - Mathf.Clamp01((point.z-bounds.min.z)/bounds.size.z)*rect.height);
    }
    private void Dot(Vector3 point, Rect rect, Color color, float size)
    {
        Vector2 p = Position(point, rect);
        MarketDrawing.Fill(new Rect(p.x-size*.5f, p.y-size*.5f,size,size), color);
    }
    private void Pickup(MarketPowerup pickup, Texture2D icon, Rect rect, float scale)
    {
        if (pickup == null || !pickup.Available) return;
        Vector2 p = Position(pickup.transform.position, rect);
        MarketDrawing.Fill(new Rect(p.x-10*scale,p.y-10*scale,20*scale,20*scale),new Color(.02f,.06f,.06f,.9f));
        GUI.DrawTexture(new Rect(p.x-11*scale,p.y-11*scale,22*scale,22*scale),icon);
        GUI.Label(new Rect(p.x+9*scale,p.y-10*scale,30*scale,18*scale),Mathf.CeilToInt(pickup.Remaining)+"s",small);
    }
    private void OnGUI()
    {
        if (market == null || market.Worker == null || map == null || GameManager.Instance == null ||
            GameManager.Instance.CurrentState != GameManager.GameState.Playing || FloatingWindows.IsOpen) return;
        float s = Mathf.Clamp(Screen.height/1080f,.8f,1.25f);
        if (label == null)
        {
            label = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, wordWrap = false };
            small = new GUIStyle(label) { fontStyle = FontStyle.Normal };
        }
        label.fontSize = Mathf.RoundToInt(18*s); label.normal.textColor = Color.white;
        small.fontSize = Mathf.RoundToInt(15*s); small.normal.textColor = Color.white;
        Rect panel = new Rect(Screen.width-398*s,Screen.height-530*s,380*s,430*s);
        MarketDrawing.Fill(panel,new Color(.03f,.09f,.10f,.96f));
        AndeanUI.DrawAguayo(new Rect(panel.x,panel.y,panel.width,5*s));
        GUI.Label(new Rect(panel.x+10*s,panel.y+8*s,panel.width-20*s,23*s),"MERCADO LANZA · LA PAZ",label);
        Rect area = new Rect(panel.x+12*s,panel.y+38*s,panel.width-24*s,292*s);
        // Conserva la proporción real de las calles, sin deformar el plano.
        float ratio = bounds.size.x/bounds.size.z;
        if (area.width/area.height > ratio) { float w=area.height*ratio;area.x+=(area.width-w)*.5f;area.width=w; }
        else { float h=area.width/ratio;area.y+=(area.height-h)*.5f;area.height=h; }
        GUI.DrawTexture(area,map);
        GUI.Label(new Rect(panel.xMax-42*s,panel.y+33*s,34*s,22*s),"N ↑",small);
        foreach (var npc in market.Pedestrians) if (npc != null) Dot(npc.transform.position,area,new Color(.19f,.38f,.36f),4*s);
        if (OrderSystem.Instance != null)
            foreach (var table in OrderSystem.Instance.tables)
                if (table != null) Dot(table.transform.position,area,table.hasOrder?AndeanUI.Yellow:new Color(.65f,.43f,.18f),5*s);
        Pickup(market.Coca,leaf,area,s); Pickup(market.Cascadita,sachet,area,s);
        Vector2 player = Position(market.Worker.transform.position,area);
        Matrix4x4 matrix = GUI.matrix; Color color = GUI.color;
        GUIUtility.RotateAroundPivot(market.Worker.transform.eulerAngles.y,player);
        GUI.color = new Color(.2f,1f,.86f);
        GUI.DrawTexture(new Rect(player.x-10*s,player.y-10*s,20*s,20*s),arrow);
        GUI.matrix = matrix; GUI.color = color;
        float y = panel.y+340*s;
        GUI.DrawTexture(new Rect(panel.x+10*s,y,19*s,19*s),leaf);
        string coca = market.Coca != null && market.Coca.Available ? "En pasillo · "+Mathf.CeilToInt(market.Coca.Remaining)+" s" : "Próxima · "+Mathf.CeilToInt(market.CocaCountdown)+" s";
        GUI.Label(new Rect(panel.x+33*s,y,panel.width-38*s,20*s),"Coca  "+coca,small);
        y+=23*s; GUI.DrawTexture(new Rect(panel.x+10*s,y,19*s,19*s),sachet);
        string casc = market.Cascadita != null && market.Cascadita.Available ? "En pasillo · "+Mathf.CeilToInt(market.Cascadita.Remaining)+" s" : "Próxima · "+Mathf.CeilToInt(market.CascaditaCountdown)+" s";
        GUI.Label(new Rect(panel.x+33*s,y,panel.width-38*s,20*s),"Cascadita  "+casc,small);
        GUI.Label(new Rect(panel.x+10*s,y+23*s,panel.width-20*s,20*s),"Velocidad ×"+market.Worker.CocaSpeedMultiplier.ToString("0.00")+"  ·  Entregas: "+market.PendingDeliveries,small);
        GUI.Label(new Rect(panel.x+10*s,y+42*s,panel.width-20*s,20*s),"Turquesa: tú / rumbo   Dorado: pedidos",small);
    }
    private void OnDestroy()
    {
        foreach (var texture in new[] {map,leaf,sachet,arrow}) if (texture != null) Destroy(texture);
    }
}
