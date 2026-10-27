using UnityEngine;
using UnityEngine.Rendering;

// Efectos visuales de sabotaje, creados por codigo (no necesitan prefabs ni texturas):
// - Smoke: humo gris que sale del fogon con un resplandor naranja que parpadea.
// - OilStain: mancha de aceite brillante en el piso que se desvanece sola.
public static class SabotageEffects
{
    private static Material smokeMaterial;
    private static Texture2D softTexture, stainTexture;
    private static Mesh quadMesh;

    public static GameObject Smoke(Vector3 position, float seconds = 12f)
    {
        var go = new GameObject("Sabotaje_Humo");
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // el cono del emisor apunta hacia arriba

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // la duracion solo se cambia detenido

        var main = ps.main;
        main.duration = seconds;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.22f, 0.22f, 0.22f, 0.85f), new Color(0.48f, 0.47f, 0.45f, 0.65f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.05f;
        main.maxParticles = 250;

        var emission = ps.emission;
        emission.rateOverTime = 20f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.3f;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.4f));

        var color = ps.colorOverLifetime;
        color.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.15f), new GradientAlphaKey(0f, 1f) });
        color.color = fade;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.5f;

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = SmokeMaterial();
        ps.Play();

        // Resplandor del fuego al maximo
        var lightGo = new GameObject("Fuego");
        lightGo.transform.SetParent(go.transform, false);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.45f, 0.1f);
        light.range = 3.5f;

        var fx = go.AddComponent<SabotageFx>();
        fx.lifetime = seconds + 4f; // deja que el ultimo humo se disipe
        fx.flicker = light;
        fx.flickerUntil = seconds;
        return go;
    }

    public static GameObject OilStain(Vector3 floorPosition, float seconds = 25f)
    {
        if (quadMesh == null)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadMesh = q.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(q);
        }

        var go = new GameObject("Sabotaje_Aceite");
        go.transform.position = floorPosition + Vector3.up * 0.02f;
        go.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
        go.transform.localScale = new Vector3(Random.Range(1.2f, 1.7f), Random.Range(0.9f, 1.3f), 1f);
        go.AddComponent<MeshFilter>().sharedMesh = quadMesh;

        // Material propio por mancha: se desvanece por separado
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(lit != null ? lit : Shader.Find("Standard"));
        Color oil = new Color(0.42f, 0.30f, 0.05f, 0.85f);
        mat.color = oil;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", oil);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", StainTexture());
        mat.mainTexture = StainTexture();
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.95f); // brilla como aceite
        GameIcons.MakeTransparent(mat);
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = ShadowCastingMode.Off;

        var fx = go.AddComponent<SabotageFx>();
        fx.lifetime = seconds;
        fx.fadeMaterial = mat;
        return go;
    }

    // ---------------------------------------------------------------- materiales y texturas

    private static Material SmokeMaterial()
    {
        if (smokeMaterial != null) return smokeMaterial;
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        smokeMaterial = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
        if (smokeMaterial.HasProperty("_BaseMap")) smokeMaterial.SetTexture("_BaseMap", SoftTexture());
        smokeMaterial.mainTexture = SoftTexture();
        GameIcons.MakeTransparent(smokeMaterial);
        return smokeMaterial;
    }

    // Circulo difuso para cada bocanada de humo
    private static Texture2D SoftTexture()
    {
        if (softTexture != null) return softTexture;
        const int size = 64;
        softTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                float a = Mathf.Clamp01(1f - d);
                softTexture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        softTexture.Apply();
        return softTexture;
    }

    // Charco irregular: el borde se deforma con ruido para que no sea un circulo perfecto
    private static Texture2D StainTexture()
    {
        if (stainTexture != null) return stainTexture;
        const int size = 128;
        stainTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Vector2 c = new Vector2(size / 2f, size / 2f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y) - c;
                float angle = Mathf.Atan2(p.y, p.x);
                float edge = 0.72f + 0.12f * Mathf.Sin(angle * 3f + 1f) + 0.08f * Mathf.Sin(angle * 7f) + 0.05f * Mathf.Sin(angle * 13f + 2f);
                float d = p.magnitude / (size / 2f) / edge;
                float a = Mathf.Clamp01((1f - d) * 8f);
                stainTexture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        stainTexture.Apply();
        return stainTexture;
    }
}

// Vida de un efecto: parpadeo del fuego, desvanecido de la mancha y autodestruccion
public class SabotageFx : MonoBehaviour
{
    public float lifetime = 10f;
    public Light flicker;
    public float flickerUntil;
    public Material fadeMaterial;

    private float age;
    private float baseAlpha = -1f;

    private void Update()
    {
        age += Time.deltaTime;

        if (flicker != null)
        {
            float on = age < flickerUntil ? 1f : Mathf.Clamp01(1f - (age - flickerUntil));
            flicker.intensity = on * (2.2f + Mathf.PerlinNoise(age * 8f, 0f) * 2.5f);
        }

        if (fadeMaterial != null)
        {
            if (baseAlpha < 0f) baseAlpha = fadeMaterial.color.a;
            Color c = fadeMaterial.color;
            c.a = baseAlpha * Mathf.Clamp01((lifetime - age) / 3f); // los ultimos 3 s
            fadeMaterial.color = c;
            if (fadeMaterial.HasProperty("_BaseColor")) fadeMaterial.SetColor("_BaseColor", c);
        }

        if (age >= lifetime) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (fadeMaterial != null) Destroy(fadeMaterial);
    }
}
