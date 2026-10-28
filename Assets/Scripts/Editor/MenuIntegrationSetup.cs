#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

public static class MenuIntegrationSetup
{
    private const string Folder="Assets/Resources/MenuIntegration";
    public static void PrepareAndTest(){Install();SelectiveMenuValidation.Run();}
    [MenuItem("Mercado/Preparar selección con personajes existentes")]
    public static void Install()
    {
        Directory.CreateDirectory(Folder+"/Avatars");Directory.CreateDirectory(Folder+"/Materials");AssetDatabase.Refresh();
        var catalog=AssetDatabase.LoadAssetAtPath<CharacterSkinCatalog>(Folder+"/CharacterSkins.asset");
        if(catalog==null){catalog=ScriptableObject.CreateInstance<CharacterSkinCatalog>();AssetDatabase.CreateAsset(catalog,Folder+"/CharacterSkins.asset");}
        catalog.models=new GameObject[5];
        catalog.models[0]=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Player/Model/Camarero_color.fbx");
        string[] names={"Rival","DonaRosa","CholitaPacena","OficialCapibara"};
        var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);Mesh primitive=cube.GetComponent<MeshFilter>().sharedMesh;
        try
        {
            for(int i=0;i<names.Length;i++)
            {
                string source="Assets/Characters/Personajes extras/"+names[i]+".prefab";
                if(i==3 && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/OficialCapibaraLocal.prefab")!=null)continue;
                var model=PrefabUtility.LoadPrefabContents(source);
                try
                {
                    foreach(var child in model.GetComponentsInChildren<Transform>(true))GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
                    foreach(var filter in model.GetComponentsInChildren<MeshFilter>(true))if(filter.sharedMesh==null)filter.sharedMesh=primitive;
                    foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
                    {
                        var materials=renderer.sharedMaterials;for(int j=0;j<materials.Length;j++)if(materials[j]==null)materials[j]=Fallback(renderer.name,i);
                        renderer.sharedMaterials=materials;
                    }
                    string path=Folder+"/Avatars/"+names[i]+".prefab";PrefabUtility.SaveAsPrefabAsset(model,path,out bool saved);
                    if(!saved)throw new System.Exception("No se pudo preparar la copia de selección: "+names[i]);
                }
                finally{PrefabUtility.UnloadPrefabContents(model);}
            }
        }
        finally{Object.DestroyImmediate(cube);}
        AssetDatabase.Refresh();
        for(int i=0;i<names.Length;i++)
        {
            catalog.models[i+1]=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Avatars/"+names[i]+".prefab");
            if(i==3)catalog.models[i+1]=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/OficialCapibaraLocal.prefab")??catalog.models[i+1];
            if(catalog.models[i+1]==null)throw new System.Exception("Falta la copia de selección: "+names[i]);
        }
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        Debug.Log("PASS: selección enlazada a copias de los personajes existentes, conservando los prefabs originales.");
    }
    private static Material Fallback(string name,int character)
    {
        string key=name.ToLowerInvariant();Color color=new Color(.72f,.53f,.34f);
        if(key.Contains("eye") || key.Contains("mouth") || key.Contains("hair") || key.Contains("shoe") || key.Contains("trouser"))color=new Color(.07f,.06f,.05f);
        else if(key.Contains("apron") || key.Contains("shirt") || key.Contains("mandil"))color=new Color(.91f,.90f,.83f);
        else if(key.Contains("skirt") || key.Contains("pollera"))color=character==2?new Color(.12f,.50f,.50f):new Color(.65f,.15f,.35f);
        else if(key.Contains("vest") || key.Contains("jacket") || key.Contains("torso"))color=character==3?new Color(.30f,.39f,.20f):new Color(.30f,.30f,.31f);
        else if(key.Contains("hat") || key.Contains("cap"))color=character==3?new Color(.32f,.40f,.20f):new Color(.25f,.15f,.08f);
        string path=Folder+"/Materials/"+ColorUtility.ToHtmlStringRGB(color)+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Menu_"+ColorUtility.ToHtmlStringRGB(color),color=color};material.SetColor("_BaseColor",color);AssetDatabase.CreateAsset(material,path);}return material;
    }
}
#endif
