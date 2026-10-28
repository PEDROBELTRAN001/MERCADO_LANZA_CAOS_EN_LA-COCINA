using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Mercado > Tomar fotos de los personajes (en Play):
// viste al jugador 1 con cada apariencia hecha en el proyecto (Tomas, el Rival con corbata, Doña Rosa
// y la Cholita Paceña) y le saca las dos fotos que usan el menu y las fichas (CharacterPortraitLibrary):
// cuerpo entero y rostro en circulo, sobre el mismo fondo verde de las fotos originales.
// El capibara no se toca. Solo funciona en el editor.
public class PortraitCapture : MonoBehaviour
{
    private static readonly int[] Skins = { 0, 1, 2, 3 };
    private static readonly string[] Files = { "Tomas", "Rival", "DonaRosa", "Cholita" };
    private static readonly Color Background = new Color32(15, 36, 33, 255);
    private const int Size = 768;
    private const int Layer = 31;

#if UNITY_EDITOR
    [MenuItem("Mercado/Tomar fotos de los personajes (en Play)")]
    private static void Capture()
    {
        if (!Application.isPlaying || LocalMultiplayer.Instance == null || LocalMultiplayer.Instance.Worker == null)
        {
            EditorUtility.DisplayDialog("Fotos de personajes", "Dale Play a la escena KitchenScene y vuelve a usar este menú (puedes dejar abierta la historia o el menú).", "OK");
            return;
        }
        new GameObject("Fotos de personajes").AddComponent<PortraitCapture>();
    }

    private IEnumerator Start()
    {
        PlayerController player = LocalMultiplayer.Instance.Worker;
        var look = player.GetComponent<CharacterAppearance>();
        int previous = look.SelectedSkin;

        var cameraObject = new GameObject("Camara de fotos");
        var cam = cameraObject.AddComponent<Camera>();
        cam.enabled = false;
        cam.cullingMask = 1 << Layer;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Background;
        cam.fieldOfView = 24f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 100f;
        var lightObject = new GameObject("Luz de fotos");
        var key = lightObject.AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.1f;
        key.cullingMask = 1 << Layer;
        key.shadows = LightShadows.None;

        var target = new RenderTexture(Size, Size, 24) { antiAliasing = 8 };
        var saved = new List<string>();
        for (int i = 0; i < Skins.Length; i++)
        {
            if (!look.Apply(Skins[i])) continue;
            // Tomas arma su chaleco, botones y corbata en Start: se esperan unos cuadros
            for (int f = 0; f < 3; f++) yield return null;

            var layers = new Dictionary<Transform, int>();
            foreach (Transform t in player.GetComponentsInChildren<Transform>(true))
            {
                layers[t] = t.gameObject.layer;
                t.gameObject.layer = Layer;
            }
            var bounds = BodyBounds(player);
            if (bounds.size.y > 0.01f)
            {
                Vector3 front = Quaternion.AngleAxis(-12f, Vector3.up) * player.transform.forward;
                lightObject.transform.rotation = Quaternion.LookRotation(-(front + Vector3.up * 0.8f + player.transform.right * 0.5f));

                // Cuerpo entero
                Frame(cam, bounds.center, Mathf.Max(bounds.size.y, bounds.size.x) * 0.6f, front);
                Save(Render(cam, target, false), Files[i]);
                // Rostro (cabeza y hombros)
                Vector3 face = new Vector3(bounds.center.x, bounds.max.y - bounds.size.y * 0.27f, bounds.center.z);
                Frame(cam, face, bounds.size.y * 0.3f, front);
                Save(Render(cam, target, true), Files[i] + "_Rostro");
                saved.Add(Files[i]);
            }
            foreach (var pair in layers) if (pair.Key != null) pair.Key.gameObject.layer = pair.Value;
        }

        look.Apply(previous);
        target.Release();
        Destroy(target);
        Destroy(cameraObject);
        Destroy(lightObject);
        AssetDatabase.Refresh();
        Debug.Log("Fotos de personajes actualizadas en Assets/Resources/CharacterPortraits: " + string.Join(", ", saved));
        EditorUtility.DisplayDialog("Fotos de personajes", "Listo: " + string.Join(", ", saved) + ".\nSe ven al volver a dar Play.", "OK");
        Destroy(gameObject);
    }

    // Lo visible del personaje (sin lo que lleve en la mano)
    private static Bounds BodyBounds(PlayerController player)
    {
        bool found = false;
        var bounds = new Bounds();
        foreach (Renderer r in player.GetComponentsInChildren<Renderer>())
        {
            if (player.Hand != null && r.transform.IsChildOf(player.Hand)) continue;
            if (!found) { bounds = r.bounds; found = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return bounds;
    }

    private static void Frame(Camera cam, Vector3 center, float halfSize, Vector3 front)
    {
        float distance = halfSize / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        Vector3 direction = (front + Vector3.up * 0.12f).normalized;
        cam.transform.position = center + direction * distance;
        cam.transform.rotation = Quaternion.LookRotation(center - cam.transform.position);
    }

    private static Texture2D Render(Camera cam, RenderTexture target, bool circle)
    {
        cam.targetTexture = target;
        cam.Render();
        cam.targetTexture = null;

        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var image = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
        RenderTexture.active = previous;

        Color32[] pixels = image.GetPixels32();
        float radius = Size * 0.5f;
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                int index = y * Size + x;
                byte alpha = 255;
                if (circle)
                {
                    // Borde del circulo suavizado, como las fotos originales
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                    alpha = (byte)(Mathf.Clamp01(radius - d) * 255f);
                }
                pixels[index].a = alpha;
            }
        image.SetPixels32(pixels);
        image.Apply();
        return image;
    }

    private static void Save(Texture2D image, string file)
    {
        string path = Path.Combine(Application.dataPath, "Resources/CharacterPortraits", file + ".png");
        File.WriteAllBytes(path, image.EncodeToPNG());
        Destroy(image);
    }
#endif
}
