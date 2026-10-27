#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CharacterArtBuilder
{
    private const string Folder="Assets/Resources/CharacterArt";
    public static void BuildCapibaraReference()
    {
        Directory.CreateDirectory(Folder+"/G");Directory.CreateDirectory(Folder+"/M");AssetDatabase.Refresh();
        var root=CharacterMeshFactory.Create(4);Bake(root);
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/OficialCapibaraLocal.prefab");Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();RenderCapibaraReference();
        Debug.Log("PASS: policía redondo verde oliva de la referencia construido y guardado.");
    }
    public static void RenderCapibaraReference()
    {
        Directory.CreateDirectory("Logs/CharacterFinal");var prefab=Resources.Load<GameObject>("OficialCapibaraLocal");
        string[] names={"Capibara_frente","Capibara_perfil","Capibara_espalda","Capibara_isometrica","Capibara_superior"};
        Vector3[] angles={new Vector3(0,20,0),new Vector3(0,-70,0),new Vector3(0,200,0),new Vector3(0,-15,0),new Vector3(76,20,0)};
        for(int i=0;i<names.Length;i++)
        {
            var wrapper=new GameObject("Vista del diseño de referencia");wrapper.transform.position=Vector3.one*-10000;
            var model=Object.Instantiate(prefab,wrapper.transform);model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.Euler(angles[i]);
            Save(wrapper,names[i]);Object.DestroyImmediate(wrapper);
        }
        Save(prefab,"Personaje_4");
        Debug.Log("PASS: cinco vistas del nuevo policía renderizadas en Unity.");
    }
    public static void BuildAndRender(){Install();Render();}
    public static void BuildDocumentCharacters()
    {
        Install();Render();GeneratePortraits();
        Debug.Log("PASS: modelos del PDF y retratos persistentes de los cinco personajes generados.");
    }
    public static void BuildAndCheckInterface(){BuildDocumentCharacters();DocumentInterfaceValidation.Run();}
    [MenuItem("Mercado/Generar fotos de los personajes")]
    public static void GeneratePortraits()
    {
        string folder="Assets/Resources/CharacterPortraits";Directory.CreateDirectory(folder);
        var catalog=Resources.Load<CharacterSkinCatalog>("CharacterSkins");string[] names={"Tomas","Rival","DonaRosa","Cholita","Capibara"};
        for(int i=0;i<names.Length;i++)using(var preview=new RuntimePreview(catalog.Model(i),768,true))
        {
            SavePortrait(preview,folder+"/"+names[i]+".png",false);
            preview.FrameCharacterPortrait();SavePortrait(preview,folder+"/"+names[i]+"_Rostro.png",true);
        }
        AssetDatabase.Refresh();
        foreach(string file in Directory.GetFiles(folder,"*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));
            importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
            importer.isReadable=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=1024;importer.SaveAndReimport();
        }
        Debug.Log("PASS: diez fotos reales de los modelos guardadas como recursos del juego.");
    }
    private static void SavePortrait(RuntimePreview preview,string path,bool circle)
    {
        if(preview.Texture==null)throw new System.Exception("El generador de retratos necesita el motor gráfico activo.");
        var old=RenderTexture.active;RenderTexture.active=preview.Texture;var image=new Texture2D(768,768,TextureFormat.RGBA32,false);
        image.ReadPixels(new Rect(0,0,768,768),0,0);image.Apply();RenderTexture.active=old;
        if(circle)
        {
            var pixels=image.GetPixels32();for(int y=0;y<768;y++)for(int x=0;x<768;x++)
            {float distance=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(384,384));pixels[y*768+x].a=(byte)(Mathf.Clamp01(384-distance)*255);}
            image.SetPixels32(pixels);image.Apply();
        }
        File.WriteAllBytes(path,image.EncodeToPNG());Object.DestroyImmediate(image);
    }
    [MenuItem("Mercado/Regenerar diseño final de personajes")]
    public static void Install()
    {
        Directory.CreateDirectory(Folder+"/G");Directory.CreateDirectory(Folder+"/M");Directory.CreateDirectory("Assets/Resources/Skins");AssetDatabase.Refresh();
        string[] paths={"Assets/Resources/Skins/Tomas.prefab","Assets/Resources/Skins/Rival.prefab","Assets/Resources/Skins/DonaRosa.prefab","Assets/Resources/Skins/CholitaPacena.prefab","Assets/Resources/OficialCapibaraLocal.prefab"};
        for(int i=0;i<paths.Length;i++)
        {
            var root=CharacterMeshFactory.Create(i);Bake(root);PrefabUtility.SaveAsPrefabAsset(root,paths[i]);Object.DestroyImmediate(root);
        }
        var catalog=AssetDatabase.LoadAssetAtPath<CharacterSkinCatalog>("Assets/Resources/CharacterSkins.asset");
        if(catalog==null){catalog=ScriptableObject.CreateInstance<CharacterSkinCatalog>();AssetDatabase.CreateAsset(catalog,"Assets/Resources/CharacterSkins.asset");}
        catalog.tomas=AssetDatabase.LoadAssetAtPath<GameObject>(paths[0]);catalog.rival=AssetDatabase.LoadAssetAtPath<GameObject>(paths[1]);
        catalog.donaRosa=AssetDatabase.LoadAssetAtPath<GameObject>(paths[2]);catalog.cholita=AssetDatabase.LoadAssetAtPath<GameObject>(paths[3]);catalog.capibara=AssetDatabase.LoadAssetAtPath<GameObject>(paths[4]);
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        Debug.Log("PASS: cinco personajes finales construidos según el informe y enlazados con el catálogo.");
    }
    private static void Bake(GameObject root)
    {
        var rig=root.GetComponent<CharacterVisualRig>();
        var bones=new HashSet<Transform>{root.transform,rig.torso,rig.head,rig.leftArm,rig.rightArm,rig.leftLeg,rig.rightLeg,rig.backLeftLeg,rig.backRightLeg};
        if(rig.sabotageStains!=null)bones.Add(rig.sabotageStains.transform);
        if(rig.idleEquipment!=null)bones.Add(rig.idleEquipment.transform);
        var groups=new Dictionary<Transform,Dictionary<Material,List<CombineInstance>>>();
        var originals=root.GetComponentsInChildren<MeshFilter>(true);
        foreach(var filter in originals)
        {
            Transform bone=filter.transform.parent;while(bone!=null && !bones.Contains(bone))bone=bone.parent;if(bone==null)bone=root.transform;
            Material original=filter.GetComponent<MeshRenderer>().sharedMaterial;
            string path=Folder+"/M/"+original.name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(original);AssetDatabase.CreateAsset(material,path);}
            if(!groups.ContainsKey(bone))groups[bone]=new Dictionary<Material,List<CombineInstance>>();
            if(!groups[bone].ContainsKey(material))groups[bone][material]=new List<CombineInstance>();
            groups[bone][material].Add(new CombineInstance{mesh=filter.sharedMesh,transform=bone.worldToLocalMatrix*filter.transform.localToWorldMatrix});
        }
        foreach(var bone in groups)foreach(var group in bone.Value)
        {
            string key=rig.designId+"/"+bone.Key.name+"/"+group.Key.name;
            var mesh=new Mesh{name=key,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(group.Value.ToArray(),true,true);
            string path=Folder+"/G/"+Hash128.Compute(key).ToString().Substring(0,12)+".asset";
            var stored=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(stored==null){stored=mesh;AssetDatabase.CreateAsset(stored,path);}
            else
            {
                // Actualizar también el búfer gráfico, para revisar inmediatamente una malla regenerada.
                stored.Clear();stored.indexFormat=mesh.indexFormat;stored.vertices=mesh.vertices;
                stored.normals=mesh.normals;stored.triangles=mesh.triangles;stored.bounds=mesh.bounds;
                EditorUtility.SetDirty(stored);Object.DestroyImmediate(mesh);
            }
            var part=new GameObject("Malla "+group.Key.name);part.transform.SetParent(bone.Key,false);part.AddComponent<MeshFilter>().sharedMesh=stored;part.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
        }
        foreach(var filter in originals){Object.DestroyImmediate(filter.GetComponent<MeshRenderer>());Object.DestroyImmediate(filter);}
    }
    public static void Render()
    {
        Directory.CreateDirectory("Logs/CharacterFinal");var catalog=Resources.Load<CharacterSkinCatalog>("CharacterSkins");
        for(int i=0;i<5;i++)
        {
            Save(catalog.Model(i),"Personaje_"+i);
            if(i==1)Save(catalog.Model(i),"Rival_manchado",true);
        }
        Debug.Log("PASS: vistas finales de personajes guardadas en Logs/CharacterFinal.");
    }
    private static void Save(GameObject model,string name,bool stained=false)
    {
        GameObject temporary=null;
        if(stained){temporary=Object.Instantiate(model);temporary.GetComponent<CharacterVisualRig>().SetSabotageMarked(true);model=temporary;temporary.transform.position=Vector3.one*-10000;}
        using(var preview=new RuntimePreview(model,768,true))
        {
            if(preview.Texture==null)throw new System.Exception("No se pudo renderizar "+name);
            var old=RenderTexture.active;RenderTexture.active=preview.Texture;var pixels=new Texture2D(768,768,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,768,768),0,0);pixels.Apply();RenderTexture.active=old;File.WriteAllBytes("Logs/CharacterFinal/"+name+".png",pixels.EncodeToPNG());Object.DestroyImmediate(pixels);
        }
        if(temporary!=null)Object.DestroyImmediate(temporary);
    }
}
#endif
