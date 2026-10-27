using System.Collections.Generic;
using UnityEngine;

// Diseño original de mallas según las fichas del informe. Sin retratos pegados ni texturas fotográficas.
public static partial class CharacterMeshFactory
{
    private static readonly Dictionary<string,Mesh> meshes = new Dictionary<string,Mesh>();
    private static readonly Dictionary<string,Material> materials = new Dictionary<string,Material>();
    private static readonly Color skin = new Color(.66f,.43f,.28f);
    private static readonly Color dark = new Color(.055f,.045f,.040f);
    private static readonly Color ivory = new Color(.95f,.90f,.77f);
    private static readonly Color teal = new Color(.075f,.32f,.31f);
    private static readonly Color navy = new Color(.11f,.16f,.21f);
    private static readonly Color gold = new Color(.94f,.62f,.18f);
    private static readonly Color red = new Color(.68f,.16f,.18f);
    private static readonly Color violet = new Color(.48f,.24f,.64f);
    private static readonly Color hair = new Color(.075f,.050f,.035f);

    private static Material Mat(Color color, bool metal = false)
    {
        string key = ColorUtility.ToHtmlStringRGBA(color) + (metal ? "m" : "c");
        if (materials.TryGetValue(key,out Material material) && material != null) return material;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        material = new Material(shader) { name = "Personaje_" + key, color = color };
        material.SetColor("_BaseColor",color);
        material.SetFloat("_Smoothness",metal ? .6f : .18f);
        material.SetFloat("_Metallic",metal ? .5f : 0f);
        materials[key] = material;
        return material;
    }
    private static Mesh Flat(string name,List<Vector3> vertices,List<int> triangles)
    {
        var v=new Vector3[triangles.Count];var t=new int[triangles.Count];
        for(int i=0;i<t.Length;i++){v[i]=vertices[triangles[i]];t[i]=i;}
        var mesh=new Mesh{name="Personaje_"+name};mesh.vertices=v;mesh.triangles=t;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    private static Mesh Geometry(string name)
    {
        if(meshes.TryGetValue(name,out Mesh mesh) && mesh!=null)return mesh;
        var v=new List<Vector3>();var t=new List<int>();
        if(name=="Sphere" || name=="SmoothSphere")
        {
            int sides=name=="SmoothSphere"?32:12,rings=name=="SmoothSphere"?20:8;
            for(int y=0;y<=rings;y++)for(int x=0;x<=sides;x++)
            {float p=Mathf.PI*y/rings,a=2*Mathf.PI*x/sides;v.Add(new Vector3(Mathf.Sin(p)*Mathf.Cos(a),Mathf.Cos(p),Mathf.Sin(p)*Mathf.Sin(a))*.5f);}
            for(int y=0;y<rings;y++)for(int x=0;x<sides;x++)
            {int a=y*(sides+1)+x,b=a+sides+1;t.AddRange(new[]{a,a+1,b,b,a+1,b+1});}
        }
        else if(name=="Box")
        {
            v.AddRange(new[]{new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f),
                new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f)});
            t.AddRange(new[]{0,2,1,0,3,2,5,6,4,4,6,7,4,3,0,4,7,3,1,2,5,5,2,6,3,7,2,2,7,6,4,0,5,5,0,1});
        }
        else if(name=="Star")
        {
            v.Add(new Vector3(0,0,.02f));v.Add(new Vector3(0,0,-.02f));
            for(int i=0;i<10;i++){float a=Mathf.PI*.5f+i*Mathf.PI/5;float r=i%2==0?.5f:.23f;v.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,.02f));v.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,-.02f));}
            for(int i=0;i<10;i++){int a=2+i*2,b=2+(i+1)%10*2;t.AddRange(new[]{0,a,b,1,b+1,a+1,a,a+1,b,b,a+1,b+1});}
        }
        else if(name=="Ring")
        {
            const int sides=20,tube=6;
            for(int i=0;i<=sides;i++)for(int j=0;j<=tube;j++)
            {float a=i*2*Mathf.PI/sides,b=j*2*Mathf.PI/tube,r=.5f+.055f*Mathf.Cos(b);v.Add(new Vector3(r*Mathf.Cos(a),r*Mathf.Sin(a),.055f*Mathf.Sin(b)));}
            for(int i=0;i<sides;i++)for(int j=0;j<tube;j++){int a=i*(tube+1)+j,b=a+tube+1;t.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
        }
        else
        {
            Vector2[] profile=name=="PoliceBelt"?new[]{new Vector2(.440f,-.155f),new Vector2(.450f,-.12f),new Vector2(.458f,-.085f)}:
                name=="SkirtHem"?new[]{new Vector2(.511f,.025f),new Vector2(.512f,.065f),new Vector2(.511f,.080f)}:
                name=="SkirtTrim"?new[]{new Vector2(.502f,.11f),new Vector2(.498f,.14f)}:
                name=="Skirt"?new[]{new Vector2(0,0),new Vector2(.49f,0),new Vector2(.50f,.06f),new Vector2(.43f,.42f),new Vector2(.32f,.82f),new Vector2(.28f,1),new Vector2(0,1)}:
                name=="Torso"?new[]{new Vector2(0,-.5f),new Vector2(.37f,-.5f),new Vector2(.48f,-.3f),new Vector2(.5f,.16f),new Vector2(.43f,.39f),new Vector2(.23f,.5f),new Vector2(0,.5f)}:
                new[]{new Vector2(0,-.5f),new Vector2(.43f,-.5f),new Vector2(.5f,-.4f),new Vector2(.5f,.4f),new Vector2(.43f,.5f),new Vector2(0,.5f)};
            int sides=name=="PoliceBelt"?40:20;
            for(int y=0;y<profile.Length;y++)for(int x=0;x<=sides;x++)
            {float a=x*2*Mathf.PI/sides,r=profile[y].x;if(name.StartsWith("Skirt"))r*=1f+.025f*Mathf.Cos(a*10);v.Add(new Vector3(r*Mathf.Cos(a),profile[y].y,r*Mathf.Sin(a)));}
            for(int y=0;y<profile.Length-1;y++)for(int x=0;x<sides;x++)
            {int a=y*(sides+1)+x,b=a+sides+1;t.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
        }
        if(name=="SmoothSphere")
        {
            mesh=new Mesh{name="Personaje_"+name};mesh.SetVertices(v);mesh.SetTriangles(t,0);
            var normals=new Vector3[v.Count];for(int i=0;i<v.Count;i++)normals[i]=v[i].normalized;
            mesh.normals=normals;mesh.RecalculateBounds();
        }
        else mesh=Flat(name,v,t);
        meshes[name]=mesh;return mesh;
    }
    private static Transform Joint(Transform parent,string name,Vector3 position)
    {var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;return go.transform;}
    private static Transform Part(Transform parent,string name,string geometry,Vector3 position,Vector3 scale,Color color,Vector3 rotation=default,bool metal=false)
    {
        Transform p=Joint(parent,name,position);p.localScale=scale;p.localRotation=Quaternion.Euler(rotation);
        p.gameObject.AddComponent<MeshFilter>().sharedMesh=Geometry(geometry);p.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Mat(color,metal);return p;
    }
    private static void Ellipsoid(Transform p,string n,Vector3 at,Vector3 size,Color c){Part(p,n,"Sphere",at,size,c);}
    private static void Box(Transform p,string n,Vector3 at,Vector3 size,Color c,Vector3 angles=default){Part(p,n,"Box",at,size,c,angles);}
    private static void Line(Transform parent,string name,Vector3 a,Vector3 b,float width,Color c)
    {
        Transform p=Part(parent,name,"Cylinder",(a+b)*.5f,new Vector3(width,Vector3.Distance(a,b),width),c);
        p.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
    }
    private static void Panel(Transform parent,string name,Vector2[] polygon,Vector3 position,Color color,float depth=.015f)
    {
        var v=new List<Vector3>();var t=new List<int>();int count=polygon.Length;
        foreach(var p in polygon)v.Add(new Vector3(p.x,p.y,depth*.5f));
        foreach(var p in polygon)v.Add(new Vector3(p.x,p.y,-depth*.5f));
        for(int i=1;i<count-1;i++){t.AddRange(new[]{0,i,i+1,count,count+i+1,count+i});}
        for(int i=0;i<count;i++){int j=(i+1)%count;t.AddRange(new[]{i,count+i,j,j,count+i,count+j});}
        var go=Joint(parent,name,position);go.gameObject.AddComponent<MeshFilter>().sharedMesh=Flat(name,v,t);go.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Mat(color);
    }
    private static void AndeanBand(Transform parent,string name,Vector3 center,float width,float height,Color backing)
    {
        Transform band=Joint(parent,name,center);Box(band,"Tejido",Vector3.zero,new Vector3(width,height,.012f),backing);
        int count=Mathf.Max(3,Mathf.RoundToInt(width/.055f));
        for(int i=0;i<count;i++)
        {
            float x=-width*.5f+(i+.5f)*width/count;
            Box(band,"Rombo andino",new Vector3(x,0,.009f),new Vector3(height*.58f,height*.58f,.008f),i%2==0?red:teal,new Vector3(0,0,45));
            Box(band,"Centro tejido",new Vector3(x,0,.015f),new Vector3(height*.20f,height*.20f,.005f),ivory,new Vector3(0,0,45));
        }
    }
    private static Transform Leg(Transform root,bool left,Color trousers)
    {
        Transform leg=Joint(root,left?"Pierna_L":"Pierna_R",new Vector3(left?-.14f:.14f,.69f,0));
        Part(leg,"Pantalón","Cylinder",new Vector3(0,-.22f,0),new Vector3(.19f,.47f,.21f),trousers);
        Part(leg,"Pierna inferior","Cylinder",new Vector3(0,-.49f,.015f),new Vector3(.17f,.30f,.18f),trousers);
        Ellipsoid(leg,"Zapatilla gastada",new Vector3(0,-.62f,.075f),new Vector3(.23f,.13f,.34f),dark);
        Part(leg,"Suela","Cylinder",new Vector3(0,-.665f,.08f),new Vector3(.24f,.035f,.34f),new Color(.55f,.54f,.47f));
        Box(leg,"Desgaste de la punta",new Vector3(0,-.613f,.232f),new Vector3(.13f,.035f,.012f),new Color(.59f,.57f,.49f));
        for(int i=0;i<3;i++)Box(leg,"Cordón",new Vector3(0,-.557f,.10f+i*.025f),new Vector3(.10f,.008f,.010f),ivory);
        return leg;
    }
    private static Transform Arm(Transform root,bool left,Color sleeve)
    {
        Transform arm=Joint(root,left?"Brazo_L":"Brazo_R",new Vector3(left?-.36f:.36f,1.18f,0));
        arm.localRotation=Quaternion.Euler(0,0,left?-7f:7f);
        Ellipsoid(arm,"Manga de polera",new Vector3(0,-.12f,0),new Vector3(.20f,.32f,.22f),sleeve);
        Part(arm,"Puño remangado","Cylinder",new Vector3(0,-.25f,.025f),new Vector3(.18f,.055f,.20f),ivory);
        Ellipsoid(arm,"Antebrazo",new Vector3(0,-.33f,.07f),new Vector3(.13f,.22f,.14f),skin);
        Ellipsoid(arm,"Mano",new Vector3(0,-.46f,.14f),new Vector3(.14f,.15f,.15f),skin);
        Ellipsoid(arm,"Pulgar",new Vector3(left?.065f:-.065f,-.43f,.17f),new Vector3(.06f,.09f,.07f),skin);
        return arm;
    }
    private static void HumanFace(Transform head,bool mature)
    {
        Ellipsoid(head,"Rostro",Vector3.zero,new Vector3(.46f,.49f,.40f),skin);
        Ellipsoid(head,"Oreja izquierda",new Vector3(-.235f,.015f,0),new Vector3(.08f,.14f,.085f),skin);
        Ellipsoid(head,"Oreja derecha",new Vector3(.235f,.015f,0),new Vector3(.08f,.14f,.085f),skin);
        for(int s=-1;s<=1;s+=2)
        {
            Ellipsoid(head,"Ojo",new Vector3(s*.095f,.045f,.184f),new Vector3(.055f,.067f,.027f),dark);
            Ellipsoid(head,"Brillo de mirada",new Vector3(s*.095f-.009f,.059f,.200f),new Vector3(.014f,.017f,.009f),ivory);
            Box(head,"Ceja",new Vector3(s*.095f,.103f,.175f),new Vector3(.092f,.018f,.018f),hair,new Vector3(0,0,s*-5));
            if(mature)Line(head,"Línea de expresión",new Vector3(s*.14f,.015f,.175f),new Vector3(s*.17f,.009f,.163f),.007f,new Color(.49f,.29f,.18f));
        }
        Ellipsoid(head,"Nariz",new Vector3(0,-.018f,.218f),new Vector3(.08f,.075f,.085f),new Color(.71f,.46f,.30f));
        for(int i=0;i<5;i++)
        {float x=(i-2)*.024f,y=-.105f+Mathf.Abs(i-2)*.006f;Line(head,"Sonrisa",new Vector3(x,y,.181f),new Vector3(x+.022f,y+(i<2?-.004f:.004f),.181f),.009f,dark);}
        Ellipsoid(head,"Cabello",new Vector3(0,.09f,-.055f),new Vector3(.475f,.36f,.33f),hair);
    }
    private static void Braid(Transform head,bool left,Color ribbon)
    {
        Transform braid=Joint(head,left?"Trenza_L":"Trenza_R",Vector3.zero);
        for(int i=0;i<9;i++)
        {
            float x=(left?-.25f:.25f)+(i%2==0?-.012f:.012f);
            Ellipsoid(braid,"Mechón trenzado",new Vector3(x,-.08f-i*.054f,.095f),new Vector3(.083f,.087f,.073f),hair);
        }
        Box(braid,"Cinta",new Vector3(left?-.25f:.25f,-.505f,.10f),new Vector3(.09f,.038f,.08f),ribbon);
    }
    private static void Cap(Transform head,bool rival)
    {
        Part(head,"Gorra de garzón","Cylinder",new Vector3(0,.245f,0),new Vector3(.44f,.055f,.40f),teal);
        Ellipsoid(head,"Copa de gorra",new Vector3(0,.285f,-.015f),new Vector3(.44f,.15f,.39f),teal);
        Ellipsoid(head,"Visera",new Vector3(0,.238f,.17f),new Vector3(.43f,.035f,.29f),teal);
        Ellipsoid(head,"Escarapela",new Vector3(-.10f,.284f,.187f),new Vector3(.048f,.048f,.014f),rival?violet:gold);
    }
    private static void Bowler(Transform head)
    {
        Color brown=new Color(.29f,.16f,.085f);
        Part(head,"Ala del sombrero","Cylinder",new Vector3(0,.225f,0),new Vector3(.62f,.035f,.49f),brown);
        Ellipsoid(head,"Sombrero bombín",new Vector3(0,.32f,-.015f),new Vector3(.43f,.27f,.38f),brown);
        Part(head,"Cinta del sombrero","Cylinder",new Vector3(0,.255f,-.015f),new Vector3(.435f,.045f,.385f),dark);
    }
    private static GameObject Stains(Transform parent,Vector3 center,bool animal=false)
    {
        Transform group=Joint(parent,"Manchas de sabotaje",center);
        for(int i=0;i<5;i++)Ellipsoid(group,"Mancha de comida",new Vector3(Mathf.Sin(i*2.4f)*.12f,-i*.028f,0),new Vector3(.055f+i%2*.035f,.040f,.008f),new Color(.30f,.14f,.055f));
        group.gameObject.SetActive(false);return group.gameObject;
    }
    private static CharacterVisualRig Human(string id,bool woman,bool cholita=false)
    {
        var root=new GameObject(id);var rig=root.AddComponent<CharacterVisualRig>();rig.designId=id;
        bool rival=id=="rival";
        rig.leftLeg=Leg(root.transform,true,navy);rig.rightLeg=Leg(root.transform,false,navy);
        rig.torso=Joint(root.transform,"Torso",new Vector3(0,.95f,0));
        Part(rig.torso,"Polera","Torso",Vector3.zero,new Vector3(woman?.66f:.62f,.65f,.42f),ivory);
        Part(root.transform,"Cuello","Cylinder",new Vector3(0,1.30f,0),new Vector3(.16f,.14f,.16f),skin);
        rig.leftArm=Arm(root.transform,true,ivory);rig.rightArm=Arm(root.transform,false,ivory);
        rig.head=Joint(root.transform,"Cabeza",new Vector3(0,1.49f,0));HumanFace(rig.head,woman);
        rig.carryPoint=Joint(root.transform,"PuntoManos",new Vector3(.37f,.97f,.46f));
        if(!woman)
        {
            Cap(rig.head,rival);
            Panel(rig.torso,"Chaleco izquierdo",new[]{new Vector2(-.29f,-.27f),new Vector2(-.075f,-.27f),new Vector2(-.035f,.05f),new Vector2(-.08f,.29f),new Vector2(-.26f,.30f)},new Vector3(0,0,.205f),teal);
            Panel(rig.torso,"Chaleco derecho",new[]{new Vector2(.075f,-.27f),new Vector2(.29f,-.27f),new Vector2(.26f,.30f),new Vector2(.08f,.29f),new Vector2(.035f,.05f)},new Vector3(0,0,.205f),teal);
            for(int s=-1;s<=1;s+=2)
            {
                Transform border=Joint(rig.torso,"Guarda andina del chaleco",new Vector3(s*.205f,.06f,.222f));
                border.localRotation=Quaternion.Euler(0,0,90);
                AndeanBand(border,"Guardas andinas",Vector3.zero,.31f,.047f,gold);
            }
            Part(rig.torso,"Escarapela de uniforme","Cylinder",new Vector3(-.11f,.235f,.247f),new Vector3(.055f,.018f,.055f),rival?violet:gold,new Vector3(90,0,0));
            Part(root.transform,"Cinta del mandil","Cylinder",new Vector3(0,.84f,0),new Vector3(.57f,.065f,.43f),ivory);
            Panel(rig.torso,"Mandil de garzón",new[]{new Vector2(-.25f,-.24f),new Vector2(.25f,-.24f),new Vector2(.23f,.20f),new Vector2(-.23f,.20f)},new Vector3(0,-.29f,.245f),ivory);
            Box(rig.torso,"Bolsillo del mandil",new Vector3(0,-.22f,.262f),new Vector3(.20f,.13f,.02f),new Color(.86f,.80f,.66f));
            Box(rig.torso,"Borde del bolsillo",new Vector3(0,-.16f,.275f),new Vector3(.22f,.013f,.012f),teal);
            rig.sabotageStains=Stains(rig.torso,new Vector3(.035f,-.24f,.285f));
        }
        else
        {
            Color skirt=cholita?new Color(.055f,.48f,.45f):new Color(.61f,.12f,.29f);
            Color shawl=cholita?new Color(.74f,.18f,.40f):new Color(.85f,.54f,.17f);
            Part(root.transform,"Pollera plisada","Skirt",new Vector3(0,.21f,0),new Vector3(.90f,.65f,.78f),skirt);
            Part(root.transform,"Guarda inferior de pollera","SkirtHem",new Vector3(0,.21f,0),new Vector3(.90f,.65f,.78f),gold);
            Part(root.transform,"Segunda guarda de pollera","SkirtTrim",new Vector3(0,.21f,0),new Vector3(.90f,.65f,.78f),gold);
            Part(root.transform,"Cintura de pollera","Cylinder",new Vector3(0,.85f,0),new Vector3(.61f,.065f,.44f),skirt);
            Panel(rig.torso,"Mantilla de espalda",new[]{new Vector2(-.40f,.28f),new Vector2(-.18f,-.17f),new Vector2(0,-.31f),new Vector2(.18f,-.17f),new Vector2(.40f,.28f)},new Vector3(0,0,-.21f),shawl);
            Panel(rig.torso,"Mantilla izquierda",new[]{new Vector2(-.39f,.05f),new Vector2(-.20f,-.03f),new Vector2(-.12f,.25f),new Vector2(-.38f,.29f)},new Vector3(0,0,.20f),shawl);
            Panel(rig.torso,"Mantilla derecha",new[]{new Vector2(.20f,-.03f),new Vector2(.39f,.05f),new Vector2(.38f,.29f),new Vector2(.12f,.25f)},new Vector3(0,0,.20f),shawl);
            if(cholita)
                for(int i=0;i<5;i++)AndeanBand(rig.torso,"Franja del aguayo",new Vector3(0,.20f-i*.065f,-.225f),.59f-i*.05f,.032f,i%2==0?gold:teal);
            Panel(rig.torso,"Pechera de delantal",new[]{new Vector2(-.15f,-.12f),new Vector2(.15f,-.12f),new Vector2(.13f,.23f),new Vector2(-.13f,.23f)},new Vector3(0,0,.235f),ivory);
            Panel(root.transform,"Delantal de cocina",new[]{new Vector2(-.31f,-.24f),new Vector2(.31f,-.24f),new Vector2(.22f,.23f),new Vector2(-.22f,.23f)},new Vector3(0,.56f,.35f),ivory);
            Box(root.transform,"Bolsillo de cocina",new Vector3(0,.64f,.368f),new Vector3(.22f,.14f,.018f),new Color(.86f,.80f,.66f));
            rig.sabotageStains=Stains(root.transform,new Vector3(.02f,.60f,.385f));
            Braid(rig.head,true,shawl);Braid(rig.head,false,shawl);Bowler(rig.head);
            if(cholita)
            {
                Transform basket=Joint(rig.leftArm,"Canasta de proveedora",new Vector3(-.10f,-.44f,.16f));
                Part(basket,"Canasta tejida","Torso",new Vector3(0,0,0),new Vector3(.30f,.22f,.26f),new Color(.62f,.36f,.15f));
                Part(basket,"Asa de canasta","Ring",new Vector3(0,.15f,0),new Vector3(.27f,.29f,.09f),gold);
                for(int i=0;i<3;i++)Part(basket,"Tejido horizontal","Cylinder",new Vector3(0,-.07f+i*.06f,0),new Vector3(.305f,.012f,.265f),gold);
            }
            else
            {
                Transform ladle=Joint(rig.leftArm,"Cucharón de Doña Rosa",new Vector3(0,-.43f,.20f));
                Part(ladle,"Mango de madera","Cylinder",new Vector3(0,.12f,0),new Vector3(.024f,.32f,.024f),new Color(.45f,.24f,.09f));
                Ellipsoid(ladle,"Cazo del cucharón",new Vector3(0,.29f,0),new Vector3(.14f,.055f,.12f),new Color(.71f,.59f,.39f));
            }
        }
        return rig;
    }
    private static CharacterVisualRig Capybara() { return ReferencePolice(); }
    public static GameObject Create(int index)
    {
        CharacterVisualRig rig=index==4?Capybara():index==2?DocumentCook():index==3?DocumentCholita():DocumentWaiter(index==1);
        return rig.gameObject;
    }
}
