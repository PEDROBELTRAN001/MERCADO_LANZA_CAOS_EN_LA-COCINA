using System.Collections.Generic;
using UnityEngine;

// Tomas, el garzon del Mercado Lanza: viste el modelo del camarero (quieto_1.fbx, rig de Mixamo)
// con los colores de la hoja de vistas del personaje, sin modificar el FBX:
//   camisa blanca, chaleco gris de fieltro abierto al frente con botones, pantalon y zapatos negros,
//   sombrero negro con banda roja y pluma roja.
// El chaleco y la banda se generan copiando partes de la malla original (usan el mismo esqueleto,
// asi que se mueven con las animaciones). Necesita Read/Write activado en el import de quieto_1.fbx.
//
// Ademas agrega las poses que faltan encima de las animaciones de caminar/quieto:
//   correr (Shift: animacion mas rapida e inclinado), cargar plato (brazos al frente)
//   y servir (reverencia al soltar lo que llevaba).
// Se agrega solo al jugador al cargar la escena.
public class TomasAvatar : MonoBehaviour
{
    [Header("Colores (hoja de vistas de Tomas)")]
    public Color shirtColor = new Color(0.96f, 0.96f, 0.94f);
    public Color vestColor = new Color(0.48f, 0.49f, 0.51f);
    public Color pantsColor = new Color(0.06f, 0.06f, 0.07f);
    public Color hatColor = new Color(0.06f, 0.06f, 0.07f);
    public Color bandColor = new Color(0.85f, 0.12f, 0.12f);
    public Color featherColor = new Color(0.92f, 0.18f, 0.12f);
    public Color buttonColor = new Color(0.12f, 0.12f, 0.13f);
    public Color tieColor = new Color(0.55f, 0.06f, 0.10f);

    [Tooltip("El Rival (impostor) es Tomas con corbata: es lo unico que los diferencia")]
    public bool necktie;

    // Materiales del FBX (Blender les dejo nombres genericos)
    private const string ShirtMat = "Material.004";   // camisa y brazos
    private const string PantsMat = "Material.005";   // pantalon y puños
    private const string ShoesMat = "Material.006";
    private const string HatMat = "color";
    private const string FeatherMat = "Material.002";

    [Header("Lo que lleva en la mano")]
    [Tooltip("Donde se sostiene el plato o el ingrediente: delante de la panza, a la altura del pecho (el modelo es redondo y el punto original quedaba dentro del cuerpo)")]
    public Vector3 handPosition = new Vector3(0f, 0.35f, 0.95f);

    [Header("Poses")]
    [Tooltip("Velocidad de la animacion al correr (1 = normal)")]
    public float runAnimationSpeed = 1.6f;
    public float runLean = 10f;
    public float serveBow = 25f;

    private Animator animator;
    private Transform hand, spine, armL, foreL, handL, armR, foreR, handR;
    private Vector3 lastPosition;
    private float speed, carryWeight, runWeight, serveTimer;
    private bool wasCarrying;
    private PlayerController player;
    private readonly List<Object> created = new List<Object>();
    private readonly List<GameObject> buttons = new List<GameObject>();
    private GameObject tie;
    private SkinnedMeshRenderer shirtRenderer;
    private bool dressed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Attach()
    {
        PlayerController found = FindAnyObjectByType<PlayerController>();
        if (found != null && found.GetComponent<TomasAvatar>() == null) found.gameObject.AddComponent<TomasAvatar>();
    }

    private void Start()
    {
        player = GetComponent<PlayerController>();
        hand = transform.Find("Hand");
        if (hand != null) hand.localPosition = handPosition;
        animator = GetComponentInChildren<Animator>();
        lastPosition = transform.position;
        if (animator == null) { enabled = false; return; }

        Transform model = animator.transform;
        spine = FindBone(model, "Spine1") ?? FindBone(model, "Spine");
        armL = FindBone(model, "LeftArm"); foreL = FindBone(model, "LeftForeArm"); handL = FindBone(model, "LeftHand");
        armR = FindBone(model, "RightArm"); foreR = FindBone(model, "RightForeArm"); handR = FindBone(model, "RightHand");

        DressUp(model);
        dressed = true;
        SetNecktie(necktie);
    }

    // Pone o quita la corbata (y los botones, que quedarian debajo de ella)
    public void SetNecktie(bool value)
    {
        necktie = value;
        if (!dressed) return; // se aplica en Start
        if (value && tie == null && shirtRenderer != null) tie = MakeTie(shirtRenderer);
        if (tie != null) tie.SetActive(value);
        foreach (GameObject button in buttons) if (button != null) button.SetActive(!value);
    }

    // ---------------------------------------------------------------- vestuario

    private void DressUp(Transform model)
    {
        // El Rival se crea copiando a Tomas: si ya trae el chaleco no se genera otra vez
        bool alreadyDressed = false;
        foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Chaleco") alreadyDressed = true;
            else if (t.name == "Boton") buttons.Add(t.gameObject);
            else if (t.name == "Corbata") tie = t.gameObject;
        }

        foreach (SkinnedMeshRenderer smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Material[] mats = smr.materials; // copias: no se toca el material del FBX
            created.AddRange(mats);
            for (int i = 0; i < mats.Length; i++)
            {
                switch (mats[i].name.Replace(" (Instance)", ""))
                {
                    case ShirtMat:
                        Paint(mats[i], shirtColor, 0.25f);
                        shirtRenderer = smr;
                        if (!alreadyDressed) MakeVest(smr);
                        break;
                    case PantsMat: Paint(mats[i], pantsColor, 0.3f); break;
                    case ShoesMat: Paint(mats[i], pantsColor, 0.6f); break;
                    case FeatherMat: Paint(mats[i], featherColor, 0.3f); break;
                    case HatMat:
                        Paint(mats[i], hatColor, 0.25f);
                        if (!alreadyDressed) MakeHatBand(smr);
                        break;
                }
            }
            smr.materials = mats;
        }
    }

    // Chaleco: el torso de la camisa (sin brazos ni cuello), un poco inflado y abierto en V al frente
    private void MakeVest(SkinnedMeshRenderer shirt)
    {
        var shape = new ShapeSpace(shirt, transform);
        SkinnedMeshRenderer vest = CloneSkinnedPart(shirt, "Chaleco", shape, 0.03f, NewMaterial(vestColor, 0.1f),
            (s, u, f) => Mathf.Abs(s) < 0.53f, // sin brazos (en pose T empiezan a la mitad del ancho)
            (s, u, f) => u < 0.88f && !(f > 0.2f && Mathf.Abs(s) < 0.07f + 0.13f * u));
        if (vest == null) return;

        // Botones en la pechera, pegados al hueso que mas mueve esa parte del torso
        for (int i = 0; i < 4; i++) AddButton(shirt, shape, 0.3f + i * 0.14f);
    }

    // Banda: el anillo de abajo de la copa del sombrero
    private void MakeHatBand(SkinnedMeshRenderer hatRenderer)
    {
        var shape = new ShapeSpace(hatRenderer, transform);
        CloneSkinnedPart(hatRenderer, "BandaSombrero", shape, 0.035f, NewMaterial(bandColor, 0.35f),
            (s, u, f) => s * s + f * f < 0.56f, // solo la copa, no el ala
            (s, u, f) => u > 0.12f && u < 0.48f);
    }

    private delegate bool VertexTest(float side, float up, float front);

    // Copia los triangulos de una malla con piel que pasan las pruebas, empujados hacia afuera, con el mismo esqueleto
    private SkinnedMeshRenderer CloneSkinnedPart(SkinnedMeshRenderer source, string name, ShapeSpace shape, float push,
        Material material, VertexTest everyVertex, VertexTest centroid)
    {
        Mesh mesh = source.sharedMesh;
        if (mesh == null || !mesh.isReadable)
        {
            Debug.LogWarning("TomasAvatar: activa Read/Write en el import de " + (mesh != null ? mesh.name : source.name) + " para generar " + name);
            return null;
        }

        Vector3[] vertices = mesh.vertices, normals = mesh.normals;
        BoneWeight[] weights = mesh.boneWeights;
        int[] triangles = mesh.triangles;
        float offset = push * shape.size;

        // La malla tiene caras planas (vertices repetidos con normales distintas): se empuja con la
        // normal promedio de cada posicion, si no los triangulos se separan y la pieza se ve partida
        var smooth = new Dictionary<Vector3, Vector3>();
        for (int i = 0; i < vertices.Length; i++)
            smooth[vertices[i]] = (smooth.TryGetValue(vertices[i], out Vector3 sum) ? sum : Vector3.zero) + normals[i];

        var map = new Dictionary<int, int>();
        var newVertices = new List<Vector3>();
        var newNormals = new List<Vector3>();
        var newWeights = new List<BoneWeight>();
        var newTriangles = new List<int>();

        for (int t = 0; t < triangles.Length; t += 3)
        {
            int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
            if (!shape.Test(vertices[a], everyVertex) || !shape.Test(vertices[b], everyVertex) || !shape.Test(vertices[c], everyVertex)) continue;
            if (!shape.Test((vertices[a] + vertices[b] + vertices[c]) / 3f, centroid)) continue;

            foreach (int v in new[] { a, b, c })
            {
                if (!map.TryGetValue(v, out int index))
                {
                    index = newVertices.Count;
                    map[v] = index;
                    newVertices.Add(vertices[v] + smooth[vertices[v]].normalized * offset);
                    newNormals.Add(normals[v]);
                    newWeights.Add(weights[v]);
                }
                newTriangles.Add(index);
            }
        }
        if (newTriangles.Count == 0) return null;

        var part = new Mesh { name = name };
        part.SetVertices(newVertices);
        part.SetNormals(newNormals);
        part.boneWeights = newWeights.ToArray();
        part.bindposes = mesh.bindposes;
        part.SetTriangles(newTriangles, 0);
        part.RecalculateBounds();
        created.Add(part);
        return CreateSkinned(source, name, part, material);
    }

    // Una pieza nueva que se deforma con el mismo esqueleto que la malla original
    private static SkinnedMeshRenderer CreateSkinned(SkinnedMeshRenderer source, string name, Mesh part, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(source.transform.parent, false);
        go.transform.localPosition = source.transform.localPosition;
        go.transform.localRotation = source.transform.localRotation;
        go.transform.localScale = source.transform.localScale;
        var smr = go.AddComponent<SkinnedMeshRenderer>();
        smr.sharedMesh = part;
        smr.bones = source.bones;
        smr.rootBone = source.rootBone;
        smr.sharedMaterial = material;
        smr.updateWhenOffscreen = true;
        return smr;
    }

    // Botones y corbata se arman en el espacio de la camisa y copian los pesos del vertice de la camisa
    // mas cercano: asi se doblan junto con la panza y no quedan hundidos dentro del cuerpo al animarse
    private SkinnedMeshRenderer SkinnedProp(SkinnedMeshRenderer shirt, string name, List<Vector3> vertices, List<int> triangles, Material material)
    {
        Mesh mesh = shirt.sharedMesh;
        Vector3[] shirtVertices = mesh.vertices;
        BoneWeight[] shirtWeights = mesh.boneWeights;
        var weights = new BoneWeight[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            int nearest = 0;
            float best = float.MaxValue;
            for (int j = 0; j < shirtVertices.Length; j++)
            {
                float d = (shirtVertices[j] - vertices[i]).sqrMagnitude;
                if (d < best) { best = d; nearest = j; }
            }
            weights[i] = shirtWeights[nearest];
        }

        var part = new Mesh { name = name };
        part.SetVertices(vertices);
        part.SetTriangles(triangles, 0);
        part.boneWeights = weights;
        part.bindposes = mesh.bindposes;
        part.RecalculateNormals();
        part.RecalculateBounds();
        created.Add(part);
        return CreateSkinned(shirt, name, part, material);
    }

    // Agrega el triangulo mirando hacia afuera (lejos de "inside")
    private static void Triangle(List<Vector3> v, List<int> t, int a, int b, int c, Vector3 inside)
    {
        Vector3 normal = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
        bool outward = Vector3.Dot(normal, (v[a] + v[b] + v[c]) / 3f - inside) >= 0f;
        t.Add(a);
        t.Add(outward ? b : c);
        t.Add(outward ? c : b);
    }

    private void AddButton(SkinnedMeshRenderer shirt, ShapeSpace shape, float height)
    {
        if (!ChestPoint(shirt.sharedMesh, shape, height, out Vector3 center)) return;

        // Boton chato: octaedro aplastado contra la pechera
        float r = 0.022f * shape.size;
        var v = new List<Vector3>
        {
            center + shape.right * r, center - shape.right * r, center + shape.up * r, center - shape.up * r,
            center + shape.front * r * 0.6f, center - shape.front * r * 0.3f,
        };
        var t = new List<int>();
        foreach (int side in new[] { 0, 1 })
            foreach (int vertical in new[] { 2, 3 })
                foreach (int depth in new[] { 4, 5 })
                    Triangle(v, t, side, vertical, depth, center);
        buttons.Add(SkinnedProp(shirt, "Boton", v, t, NewMaterial(buttonColor, 0.6f)).gameObject);
    }

    // Corbata del Rival: nudo arriba, hoja que sigue la curva de la panza y punta en pico,
    // sobre la camisa que se ve en la V del chaleco
    private GameObject MakeTie(SkinnedMeshRenderer shirt)
    {
        Mesh mesh = shirt.sharedMesh;
        if (!mesh.isReadable) return null;
        var shape = new ShapeSpace(shirt, transform);

        // Secciones de arriba hacia abajo: altura en la pechera y media anchura
        float[] heights = { 0.84f, 0.78f, 0.75f, 0.64f, 0.52f, 0.40f };
        float[] widths = { 0.030f, 0.026f, 0.020f, 0.034f, 0.042f, 0.046f };
        float lift = 0.012f * shape.size, half = 0.008f * shape.size;
        var centers = new List<Vector3>();
        for (int i = 0; i < heights.Length; i++)
        {
            if (!ChestPoint(mesh, shape, heights[i], out Vector3 c)) return null;
            centers.Add(c + shape.front * lift);
        }
        // Punta en pico, un poco mas abajo de la ultima seccion
        Vector3 last = centers[centers.Count - 1], dir = (last - centers[centers.Count - 2]).normalized;
        Vector3 tip = last + dir * widths[widths.Length - 1] * 1.2f * shape.size;

        var v = new List<Vector3>();
        var t = new List<int>();
        for (int i = 0; i < centers.Count; i++)
        {
            Vector3 w = shape.right * widths[i] * shape.size, d = shape.front * half;
            v.Add(centers[i] - w + d); v.Add(centers[i] + w + d); v.Add(centers[i] + w - d); v.Add(centers[i] - w - d);
        }
        for (int i = 0; i + 1 < centers.Count; i++)
        {
            Vector3 inside = (centers[i] + centers[i + 1]) / 2f;
            for (int k = 0; k < 4; k++)
            {
                int a = i * 4 + k, b = i * 4 + (k + 1) % 4, c = a + 4, e = b + 4;
                Triangle(v, t, a, b, e, inside);
                Triangle(v, t, a, e, c, inside);
            }
        }
        // Tapa de arriba y punta
        Triangle(v, t, 0, 1, 2, centers[1]);
        Triangle(v, t, 0, 2, 3, centers[1]);
        int tipFront = v.Count; v.Add(tip + shape.front * half);
        int tipBack = v.Count; v.Add(tip - shape.front * half);
        int l = (centers.Count - 1) * 4;
        Triangle(v, t, l, l + 1, tipFront, last);
        Triangle(v, t, l + 3, l + 2, tipBack, last);
        Triangle(v, t, l, tipFront, tipBack, last); Triangle(v, t, l, tipBack, l + 3, last);
        Triangle(v, t, l + 1, tipFront, tipBack, last); Triangle(v, t, l + 1, tipBack, l + 2, last);

        return SkinnedProp(shirt, "Corbata", v, t, NewMaterial(tieColor, 0.45f)).gameObject;
    }

    // Punto (en espacio de la malla) del frente del pecho a esa altura
    private static bool ChestPoint(Mesh mesh, ShapeSpace shape, float height, out Vector3 point)
    {
        point = Vector3.zero;
        if (!mesh.isReadable) return false;
        Vector3[] vertices = mesh.vertices;

        // La malla es low poly: la profundidad del pecho a esa altura se interpola entre
        // el vertice central mas cercano de abajo y el de arriba
        int below = -1, above = -1;
        float belowU = float.MinValue, aboveU = float.MaxValue, belowF = 0f, aboveF = 0f;
        for (int i = 0; i < vertices.Length; i++)
        {
            shape.Normalize(vertices[i], out float s, out float u, out float f);
            if (Mathf.Abs(s) > 0.12f) continue;
            if (u <= height && (u > belowU + 1e-4f || (u > belowU - 1e-4f && f > belowF))) { below = i; belowU = u; belowF = f; }
            if (u >= height && (u < aboveU - 1e-4f || (u < aboveU + 1e-4f && f > aboveF))) { above = i; aboveU = u; aboveF = f; }
        }
        if (below < 0 && above < 0) return false;
        if (below < 0) { below = above; belowU = aboveU; belowF = aboveF; }
        if (above < 0) { above = below; aboveU = belowU; aboveF = belowF; }
        float t = aboveU > belowU ? (height - belowU) / (aboveU - belowU) : 0f;
        point = shape.Point(0f, height, Mathf.Lerp(belowF, aboveF, t)) + shape.front * (0.015f * shape.size);
        return true;
    }

    // Coordenadas normalizadas de una malla: s = lado (-1..1), u = altura (0..1), f = frente (-1..1)
    private class ShapeSpace
    {
        public readonly Vector3 right, up, front;
        public readonly float size;
        private readonly float midS, halfS, minU, rangeU, midF, halfF;

        public ShapeSpace(SkinnedMeshRenderer smr, Transform character)
        {
            // Direcciones del personaje vistas desde la malla (Mixamo y Blender no comparten ejes)
            right = smr.transform.InverseTransformDirection(character.right).normalized;
            up = smr.transform.InverseTransformDirection(character.up).normalized;
            front = smr.transform.InverseTransformDirection(character.forward).normalized;

            Vector3[] v = smr.sharedMesh.isReadable ? smr.sharedMesh.vertices : new Vector3[0];
            float minS = float.MaxValue, maxS = float.MinValue, maxU = float.MinValue, minF = float.MaxValue, maxF = float.MinValue;
            minU = float.MaxValue;
            foreach (Vector3 p in v)
            {
                float s = Vector3.Dot(p, right), u = Vector3.Dot(p, up), f = Vector3.Dot(p, front);
                minS = Mathf.Min(minS, s); maxS = Mathf.Max(maxS, s);
                minU = Mathf.Min(minU, u); maxU = Mathf.Max(maxU, u);
                minF = Mathf.Min(minF, f); maxF = Mathf.Max(maxF, f);
            }
            midS = (minS + maxS) / 2f; halfS = Mathf.Max(1e-5f, (maxS - minS) / 2f);
            rangeU = Mathf.Max(1e-5f, maxU - minU);
            midF = (minF + maxF) / 2f; halfF = Mathf.Max(1e-5f, (maxF - minF) / 2f);
            size = Mathf.Max(halfS * 2f, rangeU);
        }

        public void Normalize(Vector3 p, out float s, out float u, out float f)
        {
            s = (Vector3.Dot(p, right) - midS) / halfS;
            u = (Vector3.Dot(p, up) - minU) / rangeU;
            f = (Vector3.Dot(p, front) - midF) / halfF;
        }

        // De coordenadas normalizadas a espacio de la malla
        public Vector3 Point(float s, float u, float f) =>
            right * (midS + s * halfS) + up * (minU + u * rangeU) + front * (midF + f * halfF);

        public bool Test(Vector3 p, VertexTest test)
        {
            Normalize(p, out float s, out float u, out float f);
            return test(s, u, f);
        }
    }

    private Material NewMaterial(Color color, float smoothness)
    {
        // El proyecto usa URP: "Standard" se veria rosado
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader != null ? shader : Shader.Find("Standard"));
        Paint(mat, color, smoothness);
        created.Add(mat);
        return mat;
    }

    private static void Paint(Material mat, Color color, float smoothness)
    {
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
    }

    private static Transform FindBone(Transform root, string bone)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == bone || t.name.EndsWith(":" + bone)) return t;
        return null;
    }

    // ---------------------------------------------------------------- poses (despues del Animator)

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return; // juego en pausa

        // Velocidad real sobre el piso
        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        lastPosition = transform.position;
        speed = Mathf.Lerp(speed, delta.magnitude / dt, 12f * dt);

        float walkSpeed = player != null ? player.moveSpeed : 5f;
        bool running = speed > walkSpeed * 1.2f;
        bool carrying = hand != null && hand.childCount > 0;

        // Servir: acaba de soltar lo que llevaba en la mano
        if (wasCarrying && !carrying) serveTimer = 0.7f;
        wasCarrying = carrying;
        serveTimer = Mathf.Max(0f, serveTimer - dt);
        float serve = serveTimer > 0f ? Mathf.Sin((1f - serveTimer / 0.7f) * Mathf.PI) : 0f;

        carryWeight = Mathf.MoveTowards(carryWeight, carrying ? 1f : 0f, 5f * dt);
        runWeight = Mathf.MoveTowards(runWeight, running ? 1f : 0f, 4f * dt);
        animator.speed = Mathf.Lerp(1f, runAnimationSpeed, runWeight);

        // Inclinacion del torso: al correr hacia adelante, al servir una reverencia
        if (spine != null)
            spine.rotation = Quaternion.AngleAxis(runWeight * runLean + serve * serveBow, transform.right) * spine.rotation;

        // Brazos hacia lo que lleva (sosteniendolo por los costados); al servir se estiran ofreciendo el plato
        Vector3 fwd = transform.forward, up = transform.up, side = transform.right;
        float arms = Mathf.Max(carryWeight, serve);
        Vector3 reach = (fwd * 0.9f - up * (0.25f + serve * 0.2f)).normalized;
        Vector3 reachL = reach, reachR = reach;
        if (hand != null && armL != null && armR != null && carryWeight > 0f)
        {
            reachL = Vector3.Lerp(reach, (hand.position - side * 0.3f - armL.position).normalized, carryWeight);
            reachR = Vector3.Lerp(reach, (hand.position + side * 0.3f - armR.position).normalized, carryWeight);
        }
        AimBone(armL, foreL, (reachL - side * 0.15f).normalized, arms);
        AimBone(armR, foreR, (reachR + side * 0.15f).normalized, arms);
        AimBone(foreL, handL, (fwd + up * 0.15f + side * 0.2f).normalized, arms);
        AimBone(foreR, handR, (fwd + up * 0.15f - side * 0.2f).normalized, arms);
    }

    // Gira un hueso para que apunte (hacia su hijo) en la direccion pedida, mezclado con la animacion
    private static void AimBone(Transform bone, Transform child, Vector3 worldDirection, float weight)
    {
        if (bone == null || child == null || weight <= 0.001f) return;
        Vector3 current = child.position - bone.position;
        if (current.sqrMagnitude < 1e-8f) return;
        Quaternion turn = Quaternion.FromToRotation(current, worldDirection);
        bone.rotation = Quaternion.Slerp(Quaternion.identity, turn, weight) * bone.rotation;
    }

    private void OnDestroy()
    {
        if (animator != null) animator.speed = 1f;
        foreach (Object o in created) if (o != null) Destroy(o);
    }
}
