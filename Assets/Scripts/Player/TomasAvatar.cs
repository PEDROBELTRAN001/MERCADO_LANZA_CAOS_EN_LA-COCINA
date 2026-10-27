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
    }

    // ---------------------------------------------------------------- vestuario

    private void DressUp(Transform model)
    {
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
                        MakeVest(smr);
                        break;
                    case PantsMat: Paint(mats[i], pantsColor, 0.3f); break;
                    case ShoesMat: Paint(mats[i], pantsColor, 0.6f); break;
                    case FeatherMat: Paint(mats[i], featherColor, 0.3f); break;
                    case HatMat:
                        Paint(mats[i], hatColor, 0.25f);
                        MakeHatBand(smr);
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
                    newVertices.Add(vertices[v] + normals[v] * offset);
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

    private void AddButton(SkinnedMeshRenderer shirt, ShapeSpace shape, float height)
    {
        Mesh mesh = shirt.sharedMesh;
        if (!mesh.isReadable) return;
        Vector3[] vertices = mesh.vertices;
        BoneWeight[] weights = mesh.boneWeights;

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
        if (below < 0 && above < 0) return;
        if (below < 0) { below = above; belowU = aboveU; belowF = aboveF; }
        if (above < 0) { above = below; aboveU = belowU; aboveF = belowF; }
        float t = aboveU > belowU ? (height - belowU) / (aboveU - belowU) : 0f;
        Vector3 point = shape.Point(0f, height, Mathf.Lerp(belowF, aboveF, t)) + shape.front * (0.015f * shape.size);

        int nearest = t < 0.5f ? below : above;
        int bone = weights[nearest].boneIndex0;
        if (bone >= shirt.bones.Length || shirt.bones[bone] == null) return;

        var button = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(button.GetComponent<Collider>());
        button.name = "Boton";
        Transform parent = shirt.bones[bone];
        button.transform.SetParent(parent, false);
        // bindpose: del espacio de la malla al espacio del hueso
        button.transform.localPosition = mesh.bindposes[bone].MultiplyPoint3x4(point);
        float worldSize = 0.044f * shape.size * shirt.transform.lossyScale.x;
        button.transform.localScale = Vector3.one * worldSize / Mathf.Max(0.0001f, parent.lossyScale.x);
        button.GetComponent<Renderer>().sharedMaterial = NewMaterial(buttonColor, 0.6f);
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
