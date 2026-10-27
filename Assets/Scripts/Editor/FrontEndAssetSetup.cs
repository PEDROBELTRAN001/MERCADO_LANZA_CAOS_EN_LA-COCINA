#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public static class FrontEndAssetSetup
{
    [MenuItem("Mercado/Regenerar modelos y catálogo")]
    public static void Install()
    {
        string path = "Assets/Resources/CharacterSkins.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<CharacterSkinCatalog>(path);
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<CharacterSkinCatalog>(); AssetDatabase.CreateAsset(catalog, path); }
        catalog.tomas = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Skins/Tomas.prefab")
            ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Player/Model/Camarero_color.fbx");
        catalog.tomasAnimator = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Characters/Player/Animations/CamareroController.controller");
        catalog.rival = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Skins/Rival.prefab");
        catalog.donaRosa = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Skins/DonaRosa.prefab");
        catalog.cholita = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Skins/CholitaPacena.prefab");
        catalog.capibara = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/OficialCapibaraLocal.prefab");
        foreach(string skinPath in new[]{"Assets/Resources/Skins/Rival.prefab","Assets/Resources/Skins/DonaRosa.prefab","Assets/Resources/Skins/CholitaPacena.prefab","Assets/Resources/OficialCapibaraLocal.prefab"})
            RepairFace(skinPath);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        foreach (var index in new[] { 0, 1, 2, 3, 4 })
            if (catalog.Model(index) == null) throw new System.Exception("Falta la apariencia: " + CharacterSkinCatalog.Names[index]);
        GenerateFood();
        CompactNames();
        Debug.Log("Catálogo preparado: cinco apariencias de personajes existentes.");
    }
    private static void RepairFace(string path)
    {
        GameObject root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            Renderer face=null;
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())if(renderer.name=="Face"){face=renderer;break;}
            if(face==null)return;
            Bounds bounds=face.bounds;
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if(renderer.name=="EyeL"||renderer.name=="EyeR")
                {
                    float sign=renderer.name=="EyeL"?-1f:1f;
                    renderer.transform.position=new Vector3(bounds.center.x+sign*bounds.size.x*.22f,bounds.center.y+bounds.size.y*.12f,bounds.max.z+.008f);
                }
                else if(renderer.name.StartsWith("Mouth"))
                {Vector3 p=renderer.transform.position;p.z=bounds.max.z+.006f;renderer.transform.position=p;}
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    private static void GenerateFood()
    {
        string basePath="Assets/Resources/FoodModels";
        foreach(string folder in new[]{"Ingredients","Dishes","Geometry","Materials"}) Directory.CreateDirectory(basePath+"/"+folder);
        AssetDatabase.Refresh();
        foreach(Ingredient.IngredientType type in System.Enum.GetValues(typeof(Ingredient.IngredientType)))
            foreach(FoodModelFactory.Preparation state in System.Enum.GetValues(typeof(FoodModelFactory.Preparation)))
                SaveFood(FoodModelFactory.IngredientModel(type,state),basePath+"/Ingredients/"+type+"_"+state+".prefab",basePath);
        foreach(string id in new[]{"Sandwich","Api","Paceno","Frutas","Saltena"})
            SaveFood(FoodModelFactory.DishModel(id),basePath+"/Dishes/"+id+".prefab",basePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Modelos originales creados: 44 ingredientes/estados y cinco platos, sin fotografías ni texturas extraídas.");
    }
    private static void SaveFood(GameObject root,string prefabPath,string basePath)
    {
        var groups=new Dictionary<Material,List<CombineInstance>>();
        foreach(MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
        {
            var renderer=filter.GetComponent<MeshRenderer>();
            Material original=renderer.sharedMaterial;
            string materialPath=basePath+"/Materials/"+original.name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(original);AssetDatabase.CreateAsset(material,materialPath);}
            if(!groups.ContainsKey(material))groups[material]=new List<CombineInstance>();
            groups[material].Add(new CombineInstance{mesh=filter.sharedMesh,transform=root.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix});
        }
        var originalChildren=new List<GameObject>();
        foreach(Transform child in root.transform)originalChildren.Add(child.gameObject);
        foreach(var group in groups)
        {
            var mesh=new Mesh{name=root.name+"_"+group.Key.name};
            mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(group.Value.ToArray(),true,true);
            string meshPath=basePath+"/Geometry/"+Hash128.Compute(mesh.name).ToString().Substring(0,12)+".asset";
            var stored=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(stored==null){stored=mesh;AssetDatabase.CreateAsset(stored,meshPath);}else{EditorUtility.CopySerialized(mesh,stored);Object.DestroyImmediate(mesh);}
            var part=new GameObject("Malla "+group.Key.name);
            part.transform.SetParent(root.transform,false);
            part.AddComponent<MeshFilter>().sharedMesh=stored;
            part.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
        }
        foreach(var child in originalChildren)Object.DestroyImmediate(child);
        PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
        Object.DestroyImmediate(root);
    }
    public static void CompactNames()
    {
        foreach(string guid in AssetDatabase.FindAssets("t:Mesh",new[]{"Assets/Resources/FoodModels/Geometry"}))
        {
            string oldPath=AssetDatabase.GUIDToAssetPath(guid);
            string name=Path.GetFileNameWithoutExtension(oldPath);
            if(System.Text.RegularExpressions.Regex.IsMatch(name,"^[a-f0-9]{12}$"))continue;
            string newPath="Assets/Resources/FoodModels/Geometry/"+Hash128.Compute(name).ToString().Substring(0,12)+".asset";
            if(oldPath==newPath)continue;
            if(AssetDatabase.LoadAssetAtPath<Mesh>(newPath)!=null)continue;
            string error=AssetDatabase.MoveAsset(oldPath,newPath);
            if(!string.IsNullOrEmpty(error))throw new System.Exception(error);
        }
        AssetDatabase.SaveAssets();
    }
    public static void RebuildPastry()
    {
        string path="Assets/Resources/FoodModels";
        foreach(FoodModelFactory.Preparation state in System.Enum.GetValues(typeof(FoodModelFactory.Preparation)))
            SaveFood(FoodModelFactory.IngredientModel(Ingredient.IngredientType.Dough,state),path+"/Ingredients/Dough_"+state+".prefab",path);
        SaveFood(FoodModelFactory.DishModel("Api"),path+"/Dishes/Api.prefab",path);
        AssetDatabase.SaveAssets();
    }
}
#endif
