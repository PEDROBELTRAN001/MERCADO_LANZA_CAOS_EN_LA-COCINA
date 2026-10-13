using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

// Conecta las texturas de los platos hechos en Blender a sus FBX.
// Convencion: Assets/Prefabs/<Plato>.fbx + Assets/Textures/<Plato>/T_<Nombre>.png
// El material "M_<Nombre>" del FBX se reemplaza por Assets/Materials/Platos/<Plato>/M_<Nombre>.mat (URP Lit)
public static class DishMaterialSetup
{
    const string TexturesRoot = "Assets/Textures";
    const string MaterialsRoot = "Assets/Materials/Platos";

    [MenuItem("Mercado/Configurar Materiales de Platos")]
    public static void SetupAll()
    {
        if (!AssetDatabase.IsValidFolder(TexturesRoot)) return;

        foreach (string dir in AssetDatabase.GetSubFolders(TexturesRoot))
        {
            string dish = Path.GetFileName(dir);
            string fbxPath = "Assets/Prefabs/" + dish + ".fbx";
            if (AssetImporter.GetAtPath(fbxPath) is ModelImporter importer)
                Setup(dish, fbxPath, importer);
        }
        AssetDatabase.SaveAssets();
    }

    static void Setup(string dish, string fbxPath, ModelImporter importer)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) lit = Shader.Find("Standard");

        string matDir = MaterialsRoot + "/" + dish;
        EnsureFolder(matDir);

        // Materiales que trae el FBX (los ya reasignados ya no aparecen como sub-asset)
        var sources = new Dictionary<string, Material>();
        foreach (Object sub in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            if (sub is Material m) sources[m.name] = m;
        var remaps = importer.GetExternalObjectMap();
        foreach (var id in remaps.Keys)
            if (id.type == typeof(Material) && !sources.ContainsKey(id.name)) sources[id.name] = null;

        bool needsReimport = false;
        foreach (var pair in sources)
        {
            string matName = pair.Key;
            string matPath = matDir + "/" + matName + ".mat";

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(lit) { name = matName };
                // Sin textura se conserva el color que venia de Blender (ej. el plato de ceramica)
                if (pair.Value != null) mat.SetColor("_BaseColor", GetColor(pair.Value));
                AssetDatabase.CreateAsset(mat, matPath);
            }

            string texName = matName.StartsWith("M_") ? "T_" + matName.Substring(2) : matName;
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesRoot + "/" + dish + "/" + texName + ".png");
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.mainTexture = tex;
                mat.SetColor("_BaseColor", Color.white);
            }
            EditorUtility.SetDirty(mat);

            var key = new AssetImporter.SourceAssetIdentifier(typeof(Material), matName);
            if (!remaps.TryGetValue(key, out Object current) || current != mat)
            {
                importer.AddRemap(key, mat);
                needsReimport = true;
            }
        }

        if (needsReimport)
        {
            importer.SaveAndReimport();
            Debug.Log("Materiales con textura conectados a " + fbxPath);
        }
    }

    static Color GetColor(Material m)
    {
        if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
        if (m.HasProperty("_Color")) return m.GetColor("_Color");
        return Color.white;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
