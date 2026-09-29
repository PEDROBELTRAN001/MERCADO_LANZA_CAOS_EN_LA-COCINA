using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Assets are generated in edit mode, so the scene needs no runtime builder.
public static class DonaRosaBuilder
{
    const string Folder = "Assets/DonaRosa";
    static Material steel, dark, cream, wood, teal, gold, green, potato, meat, orange, purple;
    static Mesh rounded, sphere;


    [MenuItem("Mercado/Integrar cocina de Dona Rosa")]
    public static void Integrate()
    {
        Prepare();
        // Only replace the previous generated station and the placeholders it occupies.
        foreach (string name in new[] { "COCINA_DOÑA_ROSA", "Stove_1", "Stove_2", "Counter_N_1", "Counter_N_2", "Counter_N_3", "Counter_S_1", "Counter_S_2" })
        {
            var old = GameObject.Find(name);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
        }
        var root = Group("COCINA_DOÑA_ROSA", null, Vector3.zero);
        Place(Stove(), root, new Vector3(-9, .05f, -5.05f));
        Place(Shelf(), root, new Vector3(-11.6f, .05f, 5.2f));
        Place(Counter(), root, new Vector3(-6.3f, .05f, 3.6f));
        Place(Pot(), root, new Vector3(-13, 1.03f, -5.1f));
        Place(Pan(), root, new Vector3(-5, 1.03f, -5.1f));
        Place(Supplies(), root, new Vector3(-14, .05f, 5.35f));
        Place(Paceno(), root, new Vector3(-7.1f, 1.23f, 3.6f));
        Place(Sandwich(), root, new Vector3(-5.5f, 1.23f, 3.6f));
        // Side tables keep movable cookware away from the active cooking slots.
        Part(root, "Mesa_olla", new Vector3(-13, .48f, -5.1f), new Vector3(1.7f,.86f,1.1f), wood);
        Part(root, "Cubierta_olla", new Vector3(-13, .96f, -5.1f), new Vector3(1.85f,.12f,1.25f), cream);
        Part(root, "Mesa_sarten", new Vector3(-5, .48f, -5.1f), new Vector3(1.6f,.86f,1.1f), wood);
        Part(root, "Cubierta_sarten", new Vector3(-5, .96f, -5.1f), new Vector3(1.75f,.12f,1.25f), cream);
        foreach(string table in new[]{"Mesa_olla","Mesa_sarten"})
            root.transform.Find(table).gameObject.AddComponent<BoxCollider>();
        // Open the working aisle and keep the near wall low enough to see the stove.
        var prep=GameObject.Find("Prep_1");
        if(prep!=null)
        {
            Vector3 delta=new Vector3(-12,.45f,-2.7f)-prep.transform.position;
            prep.transform.position+=delta;
            foreach(string name in new[]{"Tomate","Lechuga","Pan"})
            {var ingredient=GameObject.Find(name);if(ingredient!=null)ingredient.transform.position+=delta;}
        }
        var wall=GameObject.Find("Wall_K_S");
        if(wall!=null){wall.transform.position=new Vector3(-9,.22f,-6);wall.transform.localScale=new Vector3(12,.44f,.2f);}
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(root.scene);
    }

    static void Prepare()
    {
        foreach (string dir in new[] { Folder, Folder+"/Materials", Folder+"/Meshes", Folder+"/Prefabs" })
            Directory.CreateDirectory(dir);
        AssetDatabase.Refresh();
        steel=Mat("Acero",new Color(.65f,.75f,.77f)); dark=Mat("Hierro",new Color(.12f,.19f,.20f));
        cream=Mat("Marfil",new Color(.94f,.89f,.74f)); wood=Mat("Madera",new Color(.52f,.29f,.14f));
        teal=Mat("Verde_mercado",new Color(.12f,.48f,.43f)); gold=Mat("Dorado",new Color(.94f,.60f,.19f));
        green=Mat("Habas_llajua",new Color(.35f,.62f,.16f)); potato=Mat("Papa",new Color(.64f,.39f,.25f));
        meat=Mat("Cerdo",new Color(.54f,.24f,.17f)); orange=Mat("Zanahoria",new Color(1,.36f,.07f));
        purple=Mat("Cebolla",new Color(.78f,.54f,.73f));
        rounded=RoundedBox(); sphere=Lathe("Ovalo",new[] {new Vector2(0,-.5f),new Vector2(.29f,-.4f),new Vector2(.46f,-.2f),new Vector2(.5f,0),new Vector2(.46f,.2f),new Vector2(.29f,.4f),new Vector2(0,.5f)},12);
    }
    static Material Mat(string name, Color color)
    {
        string path=Folder+"/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m!=null)return m;
        m=new Material(Shader.Find("Universal Render Pipeline/Lit")); m.name=name; m.color=color;
        m.SetFloat("_Smoothness",.25f); AssetDatabase.CreateAsset(m,path); return m;
    }
    static GameObject Group(string name, GameObject parent, Vector3 pos)
    {
        if(parent!=null){string stem=name;int index=1;while(parent.transform.Find(name)!=null)name=stem+"_"+(index++);}
        var g=new GameObject(name); if(parent!=null)g.transform.SetParent(parent.transform,false);
        g.transform.localPosition=pos; return g;
    }
    static GameObject Part(GameObject p,string n,Vector3 pos,Vector3 size,Material m,Mesh mesh=null)
    {
        var g=Group(n,p,pos); g.transform.localScale=size;
        g.AddComponent<MeshFilter>().sharedMesh=mesh!=null?mesh:rounded;
        g.AddComponent<MeshRenderer>().sharedMaterial=m; return g;
    }
    static void Block(GameObject g,Vector3 center,Vector3 size)
    {var c=g.AddComponent<BoxCollider>();c.center=center;c.size=size;}
    static void Place(GameObject g,GameObject parent,Vector3 pos)
    {
        string path=Folder+"/Prefabs/"+g.name+".prefab";
        var prefab=PrefabUtility.SaveAsPrefabAsset(g,path);
        UnityEngine.Object.DestroyImmediate(g);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent.transform);
        instance.transform.localPosition=pos;
    }
    static void Label(GameObject p,string words,Vector3 pos,float size,Color color)
    {
        var g=Group("Rotulo_"+words,p,pos); g.transform.localRotation=Quaternion.Euler(0,180,0);
        var t=g.AddComponent<TextMesh>();t.text=words;t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.color=color;
    }
    static GameObject Stove()
    {
        var g=Group("01_Fogon_Industrial",null,Vector3.zero);
        Part(g,"Cuerpo",new Vector3(0,.75f,0),new Vector3(4.8f,.55f,1.35f),steel);
        Part(g,"Cubierta",new Vector3(0,1.07f,0),new Vector3(5,.14f,1.5f),cream);
        Part(g,"Frente",new Vector3(0,.77f,.7f),new Vector3(4.8f,.34f,.09f),teal);
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
            Part(g,"Pata",new Vector3(x*2.15f,.27f,z*.53f),new Vector3(.14f,.54f,.14f),dark);
        Part(g,"Repisa_inferior",new Vector3(0,.22f,0),new Vector3(4.55f,.1f,1.2f),steel);
        Mesh ring=Lathe("Quemador",new[]{new Vector2(.24f,0),new Vector2(.36f,0),new Vector2(.36f,.075f),new Vector2(.24f,.075f),new Vector2(.24f,0)},16);
        for(int i=0;i<3;i++)
        {
            float x=(i-1)*1.55f;
            var station=Group("Hornalla_0"+(i+1),g,new Vector3(x,0,0));
            Part(station,"Aro",new Vector3(0,1.15f,0),Vector3.one,dark,ring);
            for(int j=0;j<2;j++)Part(station,"Parrilla",new Vector3(0,1.26f,0),j==0?new Vector3(.92f,.055f,.08f):new Vector3(.08f,.055f,.92f),dark);
            Part(station,"Perilla_0"+(i+1),new Vector3(0,.78f,.8f),new Vector3(.23f,.23f,.16f),dark,sphere);
            Part(station,"Indicador",new Vector3(0,.82f,.88f),new Vector3(.035f,.1f,.02f),cream);
            // Collider on the interaction component, tall enough for the player's ray.
            Block(station,new Vector3(0,1.1f,0),new Vector3(1.5f,2.2f,1.5f));
            var cook=station.AddComponent<CookingStation>();cook.interactionPrompt="E · Cocinar / recoger · Hornalla "+(i+1);
            cook.itemSlot=Group("Punto_olla",station,new Vector3(0,1.34f,0)).transform;
            var steam=Group("Vapor_coccion",station,new Vector3(0,1.35f,0)).AddComponent<ParticleSystem>();
            var main=steam.main;main.playOnAwake=false;main.loop=true;main.startLifetime=.8f;main.startSpeed=.4f;main.startSize=.13f;main.startColor=new Color(.85f,.95f,1,.4f);main.maxParticles=24;
            var emission=steam.emission;emission.rateOverTime=12;
            var shape=steam.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=10;shape.radius=.12f;
            steam.transform.localRotation=Quaternion.Euler(-90,0,0);steam.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var pm=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Vapor.mat");
            if(pm==null){pm=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));pm.SetColor("_BaseColor",new Color(.8f,.9f,1,.35f));AssetDatabase.CreateAsset(pm,Folder+"/Materials/Vapor.mat");}
            steam.GetComponent<ParticleSystemRenderer>().sharedMaterial=pm;cook.steamEffect=steam;
        }
        return g;
    }
    static GameObject Shelf()
    {
        var g=Group("02_Estante_Insumos",null,Vector3.zero);
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Part(g,"Poste",new Vector3(x*1.8f,1.45f,z*.4f),new Vector3(.13f,2.9f,.13f),teal);
        for(int i=0;i<3;i++)
        {
            float y=.3f+i*.85f;
            Part(g,"Nivel_0"+(i+1),new Vector3(0,y,0),new Vector3(3.85f,.13f,1.05f),wood);
            for(int j=0;j<3;j++)Crate(g,new Vector3((j-1)*1.19f,y+.07f,0),i==0?potato:i==1?gold:meat,i==0?"PAPA":i==1?"CHOCLO":"CARNE");
        }
        Part(g,"Cartel",new Vector3(0,2.82f,-.48f),new Vector3(3.9f,.4f,.1f),cream);
        // Shelf faces the camera and the centre of the kitchen (-Z).
        var label=Group("Nombre",g,new Vector3(0,2.82f,-.55f));
        var text=label.AddComponent<TextMesh>();text.text="DOÑA ROSA · INSUMOS";text.fontSize=64;text.characterSize=.045f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.12f,.3f,.26f);
        Block(g,new Vector3(0,1.4f,0),new Vector3(3.9f,2.8f,1.05f));return g;
    }
    static void Crate(GameObject p,Vector3 pos,Material filling,string title)
    {
        var g=Group("Caja_"+title,p,pos);
        Part(g,"Fondo",new Vector3(0,.04f,0),new Vector3(1,.08f,.75f),wood);
        for(int side=-1;side<=1;side+=2)
        {
            Part(g,"Lateral",new Vector3(side*.46f,.19f,0),new Vector3(.08f,.3f,.75f),wood);
            for(int row=0;row<2;row++)Part(g,"Tabla",new Vector3(0,.1f+row*.17f,side*.34f),new Vector3(.95f,.12f,.07f),wood);
        }
        for(int k=0;k<6;k++)Part(g,"Insumo",new Vector3((k%3-1)*.26f,.27f,(k/3-.5f)*.28f),new Vector3(.24f,.23f,.28f),filling,sphere);
    }
    static GameObject Counter()
    {
        var g=Group("03_Barra_Despacho",null,Vector3.zero);
        Part(g,"Mueble",new Vector3(0,.52f,0),new Vector3(3.5f,1.04f,1.2f),teal);
        for(int i=0;i<7;i++)Part(g,"Liston",new Vector3((i-3)*.47f,.49f,-.615f),new Vector3(.025f,.88f,.035f),cream);
        Part(g,"Cubierta",new Vector3(0,1.11f,0),new Vector3(3.8f,.15f,1.4f),cream);
        var sign=Group("Despacho",g,new Vector3(0,.67f,-.65f));var t=sign.AddComponent<TextMesh>();t.text="PEDIDOS";t.fontSize=64;t.characterSize=.07f;t.anchor=TextAnchor.MiddleCenter;t.color=Color.white;
        Block(g,new Vector3(0,1.1f,0),new Vector3(3.8f,2.2f,1.4f));
        var c=g.AddComponent<KitchenCounter>();c.interactionPrompt="E · Dejar / recoger pedido";c.itemSlot=Group("Pedido_listo",g,new Vector3(0,1.22f,0)).transform;
        return g;
    }
    static GameObject Pot()
    {
        var g=Group("04_Olla",null,Vector3.zero);
        var mesh=Lathe("Olla_hueca",new[]{new Vector2(0,0),new Vector2(.35f,0),new Vector2(.43f,.08f),new Vector2(.44f,.58f),new Vector2(.47f,.61f),new Vector2(.47f,.66f),new Vector2(.4f,.66f),new Vector2(.38f,.12f),new Vector2(0,.12f)},20);
        Part(g,"Olla",Vector3.zero,Vector3.one,steel,mesh);
        for(int i=-1;i<=1;i+=2)Part(g,"Asa",new Vector3(i*.52f,.44f,0),new Vector3(.28f,.13f,.22f),dark);
        return g;
    }
    static GameObject Pan()
    {
        var g=Group("05_Sarten",null,Vector3.zero);
        var mesh=Lathe("Sarten_hueca",new[]{new Vector2(0,0),new Vector2(.35f,0),new Vector2(.49f,.17f),new Vector2(.46f,.2f),new Vector2(.33f,.055f),new Vector2(0,.055f)},20);
        Part(g,"Sarten",Vector3.zero,Vector3.one,dark,mesh);
        Part(g,"Mango",new Vector3(.74f,.13f,0),new Vector3(.65f,.11f,.17f),wood);return g;
    }
    static GameObject Supplies()
    {
        var g=Group("06_Cajas_Sacos",null,Vector3.zero);
        Part(g,"Saco",new Vector3(0,.4f,0),new Vector3(.85f,.8f,.7f),cream,sphere);
        for(int i=0;i<5;i++)Part(g,"Papa",new Vector3(Mathf.Sin(i*2)*.22f,.72f,Mathf.Cos(i*2)*.18f),new Vector3(.24f,.2f,.23f),potato,sphere);
        Crate(g,new Vector3(.05f,0,-1),gold,"CHOCLO");Block(g,new Vector3(0,.4f,-.4f),new Vector3(1,.8f,1.8f));return g;
    }
    static void Plate(GameObject g)
    {
        var mesh=Lathe("Plato_ovalado",new[]{new Vector2(0,0),new Vector2(.32f,0),new Vector2(.56f,.10f),new Vector2(.56f,.14f),new Vector2(.49f,.14f),new Vector2(.3f,.05f),new Vector2(0,.05f)},24);
        Part(g,"Plato",Vector3.zero,new Vector3(1.25f,1,1),steel,mesh);
    }
    static GameObject Paceno()
    {
        var g=Group("07_Plato_Paceno",null,Vector3.zero);Plate(g);
        Part(g,"Papa",new Vector3(.22f,.17f,-.17f),new Vector3(.32f,.25f,.29f),potato,sphere);
        Part(g,"Queso_frito",new Vector3(.21f,.14f,.19f),new Vector3(.42f,.13f,.23f),gold);
        for(int i=0;i<3;i++)Part(g,"Haba",new Vector3(-.02f+i*.13f,.12f,-.32f),new Vector3(.12f,.09f,.17f),green,sphere);
        for(int row=0;row<6;row++)for(int k=0;k<8;k++)
        {float a=k*Mathf.PI/4;Part(g,"Grano_choclo",new Vector3(-.29f+Mathf.Cos(a)*.115f,.20f+Mathf.Sin(a)*.115f,(row-2.5f)*.071f),new Vector3(.084f,.084f,.079f),cream,sphere);}
        return g;
    }
    static GameObject Sandwich()
    {
        var g=Group("08_Sandwich_Chola",null,Vector3.zero);Plate(g);
        Part(g,"Pan_base",new Vector3(0,.14f,0),new Vector3(.79f,.2f,.51f),gold,sphere);
        for(int i=0;i<4;i++)Part(g,"Cerdo",new Vector3((i-1.5f)*.16f,.25f,0),new Vector3(.26f,.085f,.44f),meat);
        Part(g,"Cuerito",new Vector3(0,.30f,0),new Vector3(.66f,.04f,.4f),gold);
        for(int i=0;i<6;i++)
        {
            var a=Part(g,"Zanahoria",new Vector3((i-2.5f)*.1f,.34f,-.05f),new Vector3(.04f,.045f,.46f),orange);a.transform.localRotation=Quaternion.Euler(0,i*19,0);
            Part(g,"Cebolla",new Vector3((i-2.5f)*.09f,.37f,.02f),new Vector3(.06f,.04f,.4f),purple);
        }
        Part(g,"Llajua",new Vector3(0,.39f,-.07f),new Vector3(.48f,.055f,.37f),green,sphere);
        for(int i=-1;i<=1;i+=2)Part(g,"Marraqueta",new Vector3(i*.185f,.49f,.04f),new Vector3(.38f,.26f,.53f),gold,sphere);
        return g;
    }
    static Mesh SaveMesh(string name,List<Vector3> v,List<int> t)
    {
        string path=Folder+"/Meshes/"+name+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh!=null)return mesh;
        mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static Mesh Lathe(string name,Vector2[] profile,int segments)
    {
        var v=new List<Vector3>();var t=new List<int>();
        for(int j=0;j<profile.Length;j++)for(int i=0;i<=segments;i++)
        {float a=i*Mathf.PI*2/segments;v.Add(new Vector3(Mathf.Cos(a)*profile[j].x,profile[j].y,Mathf.Sin(a)*profile[j].x));}
        for(int j=0;j<profile.Length-1;j++)for(int i=0;i<segments;i++)
        {int a=j*(segments+1)+i,b=a+segments+1;t.Add(a);t.Add(b);t.Add(a+1);t.Add(a+1);t.Add(b);t.Add(b+1);}
        return SaveMesh(name,v,t);
    }
    static Mesh RoundedBox()
    {
        var v=new List<Vector3>();var t=new List<int>();float[] steps={-.5f,-.42f,.42f,.5f};
        Vector3[] normals={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
        foreach(var n in normals)
        {
            Vector3 u=Mathf.Abs(n.y)>.5f?Vector3.right:Vector3.up;Vector3 w=Vector3.Cross(n,u);int offset=v.Count;
            for(int j=0;j<4;j++)for(int i=0;i<4;i++)
            {Vector3 p=n*.5f+u*steps[i]+w*steps[j];Vector3 core=new Vector3(Mathf.Clamp(p.x,-.42f,.42f),Mathf.Clamp(p.y,-.42f,.42f),Mathf.Clamp(p.z,-.42f,.42f));v.Add(core+(p-core).normalized*.08f);}
            for(int j=0;j<3;j++)for(int i=0;i<3;i++)
            {int a=offset+j*4+i;t.Add(a);t.Add(a+1);t.Add(a+4);t.Add(a+1);t.Add(a+5);t.Add(a+4);}
        }
        return SaveMesh("Caja_biselada",v,t);
    }
    public static void BuildAndSave()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/KitchenScene.unity");
        if(GameObject.Find("Floor_Kitchen")==null)SceneBuilder.BuildContents();else Integrate();
        Integrate(); // Repeating integration must not duplicate generated objects.
        Validate();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        RenderPreview();Debug.Log("DONA_ROSA_BUILD_OK");
    }
    public static void Validate()
    {
        var root=GameObject.Find("COCINA_DOÑA_ROSA");
        if(root==null || root.GetComponentsInChildren<CookingStation>().Length!=3)throw new Exception("Se requieren exactamente tres hornallas.");
        foreach(var c in root.GetComponentsInChildren<KitchenCounter>())if(c.itemSlot==null)throw new Exception("Falta punto de colocacion: "+c.name);
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>())if(r.sharedMaterial==null || r.sharedMaterial.shader==null)throw new Exception("Material no valido: "+r.name);
        if(GameObject.Find("Player")==null || GameObject.Find("Floor_FoodCourt")==null)throw new Exception("Falta el escenario original.");
        Physics.SyncTransforms();
        var bar=root.transform.Find("03_Barra_Despacho").GetComponent<BoxCollider>();
        var trash=GameObject.Find("TrashStation");
        if(trash!=null && bar.bounds.Intersects(trash.GetComponent<Collider>().bounds))throw new Exception("La barra invade la estacion de basura.");
        foreach(var station in root.GetComponentsInChildren<CookingStation>())
        {
            Vector3 origin=station.transform.position+new Vector3(0,2,1.8f);
            if(!Physics.Raycast(origin,Vector3.back,out RaycastHit hit,1.5f) || hit.collider.GetComponent<CookingStation>()!=station)
                throw new Exception("Hornalla inaccesible: "+station.name);
        }
        var player=GameObject.Find("Player").GetComponent<PlayerController>();
        var hand=player.transform.Find("Hand");
        var counter=root.GetComponentInChildren<KitchenCounter>();
        foreach(var c in root.GetComponentsInChildren<KitchenCounter>())if(!(c is CookingStation))counter=c;
        var item=new GameObject("ValidationItem");item.transform.SetParent(hand,false);
        counter.Interact(player);
        if(!counter.isOccupied || item.transform.parent!=counter.itemSlot)throw new Exception("Fallo al colocar pedido.");
        counter.Interact(player);
        if(counter.isOccupied || counter.GetStoredItem()!=null || item.transform.parent!=hand)throw new Exception("Fallo al recoger pedido.");
        UnityEngine.Object.DestroyImmediate(item);
        Debug.Log("DONA_ROSA_INTERACTION_OK: tres hornallas accesibles y traslado de pedidos.");
        Debug.Log("DONA_ROSA_VALIDATION_OK: 3 hornallas, puntos de colocacion, materiales y mercado.");
    }
    public static void PreviewSaved()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/KitchenScene.unity");
        RenderPreview();
    }
    static void RenderPreview()
    {
        // A one-shot Camera.Render has no frame loop to initialize the GPU drawer.
        // Use a temporary pipeline copy for this offline preview only.
        var originalPipeline=QualitySettings.renderPipeline;
        var source=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
        UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset previewPipeline=null;
        if(source!=null)
        {
            previewPipeline=UnityEngine.Object.Instantiate(source);
            previewPipeline.gpuResidentDrawerMode=UnityEngine.Rendering.GPUResidentDrawerMode.Disabled;
            previewPipeline.useSRPBatcher=false;
            QualitySettings.renderPipeline=previewPipeline;
        }
        try
        {
        var go=new GameObject("PreviewCamera");var cam=go.AddComponent<Camera>();
        cam.transform.position=new Vector3(-9,13,-14);cam.transform.LookAt(new Vector3(-9,.7f,0));cam.orthographic=true;cam.orthographicSize=8;
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.13f,.19f,.22f);
        var rt=new RenderTexture(1400,1100,24);cam.targetTexture=rt;cam.Render();
        var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1400,1100,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1400,1100),0,0);image.Apply();
        Directory.CreateDirectory("Documentation");File.WriteAllBytes("Documentation/CocinaDonaRosa.png",image.EncodeToPNG());
        RenderTexture.active=previous;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(go);
        }
        finally { QualitySettings.renderPipeline=originalPipeline; if(previewPipeline!=null)UnityEngine.Object.DestroyImmediate(previewPipeline); }
    }
}
