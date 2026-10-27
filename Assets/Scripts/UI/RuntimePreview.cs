using System;
using UnityEngine;
using UnityEngine.Rendering;

// Vista 3D aislada para el menú; no añade colliders al mapa ni ilumina sus cámaras.
public sealed class RuntimePreview : IDisposable
{
    public const int PreviewLayer = 31;
    private static int sequence;
    private static Light sharedLight;
    private static int references;
    private GameObject root;
    private Camera camera;
    private RenderTexture texture;
    private GameObject visual;
    private Bounds bounds;
    public RenderTexture Texture => texture;
    public static bool Available => SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
    public RuntimePreview(GameObject model, int size = 320, bool character = false)
    {
        if (!Available || model == null) return;
        root = new GameObject("Vista de menú");
        root.transform.position = new Vector3(10000 + sequence++ * 20, 10000, 10000);
        visual = UnityEngine.Object.Instantiate(model, root.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        foreach (var collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (var body in visual.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
        foreach (Transform child in visual.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = PreviewLayer;
        var catalog = character ? Resources.Load<CharacterSkinCatalog>("CharacterSkins") : null;
        foreach (var animator in visual.GetComponentsInChildren<Animator>())
        {
            if (animator.runtimeAnimatorController == null && catalog != null && model == catalog.tomas)
                animator.runtimeAnimatorController = catalog.tomasAnimator;
            if (animator.runtimeAnimatorController != null) animator.Update(0f);
        }
        var renderers = visual.GetComponentsInChildren<Renderer>();
        bounds = new Bounds(root.transform.position, Vector3.one);
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        }
        references++;
        if (sharedLight == null)
        {
            var lightObject = new GameObject("Iluminación de las vistas del menú");
            lightObject.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
            sharedLight = lightObject.AddComponent<Light>();
            sharedLight.type = LightType.Directional;
            sharedLight.intensity = 1.15f;
            sharedLight.color = new Color(1f, 0.94f, 0.85f);
            sharedLight.cullingMask = 1 << PreviewLayer;
        }
        foreach (var sceneLight in UnityEngine.Object.FindObjectsByType<Light>())
            if (sceneLight != sharedLight) sceneLight.cullingMask &= ~(1 << PreviewLayer);
        var cameraObject = new GameObject("Cámara del retrato");
        cameraObject.transform.SetParent(root.transform, false);
        camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.14f, 0.13f);
        camera.cullingMask = 1 << PreviewLayer;
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(bounds.size.y, Mathf.Max(bounds.size.x, bounds.size.z)) * 0.65f;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = Mathf.Max(30f, bounds.size.magnitude * 8f);
        Vector3 angle = character ? new Vector3(1.1f,0.4f,3f) : new Vector3(1.8f,2.4f,3f);
        camera.transform.position = bounds.center + angle.normalized * Mathf.Max(4f, bounds.size.magnitude * 2f);
        camera.transform.LookAt(bounds.center);
        texture = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
        texture.Create();
        camera.targetTexture = texture;
        // La primera cámara prepara la iluminación interna de URP; la segunda pasada la muestra completa.
        Render();
        Render();
    }
    public void Render() { if (camera != null) camera.Render(); }
    public void FrameCharacterPortrait()
    {
        if(camera==null)return;
        Vector3 focus=new Vector3(bounds.center.x,bounds.max.y-bounds.size.y*.25f,bounds.center.z);
        camera.orthographicSize=bounds.size.y*.29f;
        camera.transform.position=focus+new Vector3(.18f,.12f,3f).normalized*Mathf.Max(4f,bounds.size.magnitude*2f);
        camera.transform.LookAt(focus);Render();Render();
    }
    public void Dispose()
    {
        if (texture != null) { texture.Release(); Release(texture); }
        if (root != null)
        {
            Release(root);
            references--;
            if (references == 0 && sharedLight != null) Release(sharedLight.gameObject);
        }
    }
    public static void Release(UnityEngine.Object value)
    {
        if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value);
    }
}
