using System.Collections.Generic;
using UnityEngine;

// Modelos 3D originales hechos con mallas y piezas de color. Las fotos son referencia de forma y paleta.
public static class FoodModelFactory
{
    public enum Preparation { Raw, Chopped, Cooked, ChoppedCooked }
    private static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
    private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    private static readonly Color bread = new Color(0.87f, 0.51f, 0.14f);
    private static readonly Color crust = new Color(0.62f, 0.29f, 0.065f);
    private static readonly Color cream = new Color(0.98f, 0.88f, 0.62f);
    private static readonly Color pork = new Color(0.39f, 0.16f, 0.065f);
    private static readonly Color rind = new Color(0.72f, 0.36f, 0.11f);
    private static readonly Color green = new Color(0.27f, 0.57f, 0.13f);
    private static readonly Color purple = new Color(0.48f, 0.055f, 0.39f);
    private static readonly Color onion = new Color(0.75f, 0.14f, 0.50f);
    private static readonly Color white = new Color(0.96f, 0.94f, 0.86f);

    private static Material Mat(Color color)
    {
        string key = ColorUtility.ToHtmlStringRGBA(color);
        if (materials.TryGetValue(key, out Material material) && material != null) return material;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        material = new Material(shader) { name = "Lanza_" + key, color = color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.22f);
        materials[key] = material;
        return material;
    }
    private static Mesh FlatMesh(string name, List<Vector3> source, List<int> indices)
    {
        var vertices = new Vector3[indices.Count];
        var triangles = new int[indices.Count];
        for (int i = 0; i < indices.Count; i++) { vertices[i] = source[indices[i]]; triangles[i] = i; }
        var mesh = new Mesh { name = name };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
    private static Mesh Shape(string name)
    {
        if (meshes.TryGetValue(name, out Mesh mesh) && mesh != null) return mesh;
        var v = new List<Vector3>();
        var t = new List<int>();
        if (name == "Box")
        {
            foreach (Vector3 p in new[] { new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(-.5f,.5f,-.5f),
                new Vector3(-.5f,-.5f,.5f), new Vector3(.5f,-.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(-.5f,.5f,.5f) }) v.Add(p);
            t.AddRange(new[] {0,2,1,0,3,2,5,6,4,4,6,7,4,3,0,4,7,3,1,2,5,5,2,6,3,7,2,2,7,6,4,0,5,5,0,1});
        }
        else if (name == "Sphere")
        {
            const int sides = 10, rings = 6;
            for (int y = 0; y <= rings; y++)
            {
                float phi = Mathf.PI * y / rings;
                for (int x = 0; x <= sides; x++)
                {
                    float theta = 2 * Mathf.PI * x / sides;
                    v.Add(new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta)) * .5f);
                }
            }
            for (int y = 0; y < rings; y++) for (int x = 0; x < sides; x++)
            {
                int a = y * (sides + 1) + x, b = a + sides + 1;
                t.AddRange(new[] { a, a + 1, b, b, a + 1, b + 1 });
            }
        }
        else if (name == "Handle")
        {
            const int sides = 16, tube = 5;
            for (int i = 0; i <= sides; i++) for (int j = 0; j <= tube; j++)
            {
                float a = 2 * Mathf.PI * i / sides, b = 2 * Mathf.PI * j / tube;
                float r = .5f + .11f * Mathf.Cos(b);
                v.Add(new Vector3(r * Mathf.Cos(a), r * Mathf.Sin(a), .11f * Mathf.Sin(b)));
            }
            for (int i = 0; i < sides; i++) for (int j = 0; j < tube; j++)
            {
                int a = i * (tube + 1) + j, b = a + tube + 1;
                t.AddRange(new[] {a,b,a+1,a+1,b,b+1});
            }
        }
        else if (name == "Cheese")
        {
            v.AddRange(new[] {new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(-.5f,0,.5f),
                new Vector3(-.5f,1,-.5f),new Vector3(.5f,1,-.5f),new Vector3(-.5f,1,.5f)});
            t.AddRange(new[] {0,1,2,3,5,4,0,3,1,1,3,4,1,4,2,2,4,5,2,5,0,0,5,3});
        }
        else if (name == "Pastry")
        {
            Vector2[] profile={new Vector2(0,.46f),new Vector2(.45f,.40f),new Vector2(.85f,.22f),new Vector2(1,.035f),new Vector2(1,0),new Vector2(0,0)};
            const int sides=16;
            foreach(var ring in profile)for(int i=0;i<=sides;i++)
            {
                float a=i*2*Mathf.PI/sides,c=Mathf.Cos(a),s=Mathf.Sin(a);
                v.Add(new Vector3(Mathf.Sign(c)*Mathf.Sqrt(Mathf.Abs(c))*.5f*ring.x,ring.y,Mathf.Sign(s)*Mathf.Sqrt(Mathf.Abs(s))*.5f*ring.x));
            }
            for(int y=0;y<profile.Length-1;y++)for(int x=0;x<sides;x++)
            {
                int a=y*(sides+1)+x,b=a+sides+1;
                t.AddRange(new[]{a,a+1,b,a+1,b+1,b});
            }
        }
        else
        {
            Vector2[] profile = name == "Dome" ? new[] {new Vector2(0,0),new Vector2(.5f,0),new Vector2(.49f,.12f),new Vector2(.41f,.31f),new Vector2(.24f,.43f),new Vector2(0,.46f)} :
                name == "Plate" ? new[] {new Vector2(0,0),new Vector2(.32f,0),new Vector2(.46f,.018f),new Vector2(.5f,.05f),new Vector2(.48f,.065f),new Vector2(.34f,.032f),new Vector2(0,.026f)} :
                name == "Bowl" ? new[] {new Vector2(0,0),new Vector2(.28f,0),new Vector2(.48f,.35f),new Vector2(.5f,.5f),new Vector2(.45f,.5f),new Vector2(.40f,.34f),new Vector2(.23f,.09f),new Vector2(0,.09f)} :
                new[] {new Vector2(0,-.5f),new Vector2(.5f,-.5f),new Vector2(.5f,.5f),new Vector2(0,.5f)};
            const int sides = 16;
            for (int y = 0; y < profile.Length; y++) for (int x = 0; x <= sides; x++)
            {
                float a = 2 * Mathf.PI * x / sides;
                v.Add(new Vector3(profile[y].x * Mathf.Cos(a), profile[y].y, profile[y].x * Mathf.Sin(a)));
            }
            for (int y = 0; y < profile.Length - 1; y++) for (int x = 0; x < sides; x++)
            {
                int a = y * (sides + 1) + x, b = a + sides + 1;
                t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
            }
        }
        mesh = FlatMesh("Lanza_" + name, v, t);
        meshes[name] = mesh;
        return mesh;
    }
    private static Transform Part(Transform parent, string name, string shape, Vector3 position, Vector3 size, Color color, Vector3 angles = default)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = size;
        go.transform.localRotation = Quaternion.Euler(angles);
        go.AddComponent<MeshFilter>().sharedMesh = Shape(shape);
        go.AddComponent<MeshRenderer>().sharedMaterial = Mat(color);
        return go.transform;
    }
    private static void Lump(Transform root, string name, Vector3 p, Vector3 s, Color c) { Part(root, name, "Sphere", p, s, c); }
    private static void Box(Transform root, string name, Vector3 p, Vector3 s, Color c, Vector3 r = default) { Part(root, name, "Box", p, s, c, r); }
    private static void Corn(Transform root, bool morado, bool cooked)
    {
        Color kernels = morado ? purple : new Color(1f, .75f, .10f);
        Part(root, "Corazón del choclo", "Cylinder", new Vector3(0,.19f,0), new Vector3(.115f,.34f,.115f), morado ? pork : cream);
        for (int row = 0; row < 8; row++) for (int side = 0; side < 9; side++)
        {
            float angle = side * Mathf.PI * 2 / 9;
            Vector3 p = new Vector3(Mathf.Cos(angle) * .064f, .04f + row * .041f, Mathf.Sin(angle) * .064f);
            Lump(root, "Grano", p, new Vector3(.046f,.045f,.043f), kernels * (1f - (row + side) % 3 * .035f));
        }
        if (!cooked)
            for (int i = 0; i < 3; i++)
                Part(root, "Chala", "Dome", new Vector3((i-1)*.055f,.03f,.02f), new Vector3(.07f,.42f,.028f), morado ? new Color(.31f,.22f,.24f) : green, new Vector3(0,i*55,-25+i*25));
    }
    private static void Pastry(Transform root, bool cooked)
    {
        Part(root, "Pastel inflado", "Pastry", new Vector3(0,.012f,0), new Vector3(.37f,cooked ? .27f : .09f,.25f), cooked ? new Color(1f,.66f,.13f) : cream);
        if (cooked)
            for (int i = 0; i < 100; i++)
            {
                float x = Mathf.Sin(i * 2.37f) * .145f, z = Mathf.Cos(i * 1.73f) * .090f;
                float radius=Mathf.Pow(Mathf.Pow(Mathf.Abs(x/.185f),4)+Mathf.Pow(Mathf.Abs(z/.125f),4),.25f);
                float profile=radius<.45f?Mathf.Lerp(.46f,.40f,radius/.45f):Mathf.Lerp(.40f,.22f,(radius-.45f)/.40f);
                float y=.017f+.27f*profile;
                Box(root, "Azúcar", new Vector3(x,y,z), Vector3.one * .007f, white, new Vector3(i*23,i*31,0));
            }
    }
    private static float DomeHeight(float radius)
    {
        if(radius<.48f)return Mathf.Lerp(.46f,.43f,radius/.48f);
        if(radius<.82f)return Mathf.Lerp(.43f,.31f,(radius-.48f)/.34f);
        if(radius<.98f)return Mathf.Lerp(.31f,.12f,(radius-.82f)/.16f);
        return Mathf.Lerp(.12f,0,(radius-.98f)/.02f);
    }
    private static void BreadScoring(Transform root,float bottom,Vector3 size)
    {
        for(int cut=0;cut<3;cut++)for(int segment=0;segment<5;segment++)
        {
            float x=(cut-1)*size.x*.20f,z=(segment-2)*size.z*.065f;
            float radius=Mathf.Sqrt(x*x/(size.x*size.x*.25f)+z*z/(size.z*size.z*.25f));
            float y=bottom+size.y*DomeHeight(radius)+.002f;
            Box(root,"Greña de corteza",new Vector3(x,y,z),new Vector3(size.x*.022f,.004f,size.z*.075f),cream);
        }
    }
    private static void Api(Transform root)
    {
        Color glass = new Color(.66f,.79f,.83f);
        Part(root, "Api morado", "Cylinder", new Vector3(0,.155f,0), new Vector3(.20f,.27f,.20f), new Color(.27f,.015f,.16f));
        Part(root, "Borde de la taza", "Handle", new Vector3(0,.299f,0), new Vector3(.19f,.19f,.045f), glass, new Vector3(90,0,0));
        Part(root, "Base de vidrio", "Cylinder", new Vector3(0,.015f,0), new Vector3(.225f,.022f,.225f), glass);
        Part(root, "Asa", "Handle", new Vector3(-.15f,.16f,0), new Vector3(.14f,.23f,.12f), glass);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4;
            Box(root, "Arista de vidrio", new Vector3(Mathf.Cos(a)*.106f,.15f,Mathf.Sin(a)*.106f), new Vector3(.009f,.27f,.009f), glass);
        }
        Box(root, "Canela", new Vector3(-.037f,.29f,.02f), new Vector3(.025f,.16f,.022f), crust, new Vector3(0,0,16));
        for (int i=0;i<8;i++) Box(root,"Anís",new Vector3(.035f,.296f,.035f),new Vector3(.06f,.008f,.012f),crust,new Vector3(0,i*45,0));
    }
    private static void Fruit(Transform root, bool chopped)
    {
        if (!chopped)
        {
            Lump(root,"Manzana",new Vector3(-.08f,.10f,0),new Vector3(.19f,.19f,.18f),new Color(.83f,.075f,.06f));
            Lump(root,"Naranja",new Vector3(.09f,.10f,.01f),new Vector3(.18f,.18f,.18f),new Color(1,.48f,.035f));
            for (int i=0;i<4;i++) Lump(root,"Plátano",new Vector3(-.13f+i*.075f,.18f+Mathf.Sin(i*Mathf.PI/3)*.025f,.10f),new Vector3(.11f,.07f,.085f),new Color(.96f,.79f,.17f));
        }
        else
        {
            Part(root,"Cuenco de fruta","Bowl",Vector3.zero,new Vector3(.34f,.13f,.29f),new Color(.65f,.36f,.13f));
            for (int i=0;i<12;i++)
            {
                float a = i * 2.4f;
                Color c = i%3==0 ? new Color(.88f,.09f,.06f) : i%3==1 ? new Color(.94f,.68f,.19f) : new Color(.46f,.72f,.15f);
                Box(root,"Fruta picada",new Vector3(Mathf.Sin(a)*.10f,.09f+(i%2)*.035f,Mathf.Cos(a)*.08f),new Vector3(.065f,.055f,.06f),c,new Vector3(i*13,i*31,0));
            }
        }
    }
    public static GameObject IngredientModel(Ingredient.IngredientType type, Preparation state)
    {
        var root = new GameObject(type + "_" + state);
        Transform r = root.transform;
        bool chopped = state == Preparation.Chopped || state == Preparation.ChoppedCooked;
        bool cooked = state == Preparation.Cooked || state == Preparation.ChoppedCooked;
        switch (type)
        {
            case Ingredient.IngredientType.Corn: Corn(r,false,cooked); break;
            case Ingredient.IngredientType.PurpleCorn: if(cooked) Api(r); else Corn(r,true,false); break;
            case Ingredient.IngredientType.Dough: Pastry(r,cooked); break;
            case Ingredient.IngredientType.Fruit: Fruit(r,chopped); break;
            case Ingredient.IngredientType.Potato:
                if (chopped)
                    for(int i=0;i<4;i++) Lump(r,"Trozo de papa",new Vector3((i%2-.5f)*.13f,.07f,(i/2-.5f)*.11f),new Vector3(.12f,.12f,.10f),cream);
                else
                {
                    Lump(r,"Papa",new Vector3(0,.12f,0),new Vector3(.30f,.23f,.24f),cooked ? new Color(.94f,.80f,.43f) : new Color(.58f,.36f,.17f));
                    for(int i=0;i<6;i++) Lump(r,"Ojo de papa",new Vector3(Mathf.Sin(i*2.2f)*.1f,.16f+(i%2)*.04f,Mathf.Cos(i*2.2f)*.09f),Vector3.one*.014f,cooked ? cream : crust);
                }
                break;
            case Ingredient.IngredientType.Bean:
                if(!cooked) Part(r,"Vaina de haba","Dome",new Vector3(0,.01f,0),new Vector3(.36f,.13f,.12f),green);
                for(int i=0;i<3;i++) Lump(r,"Haba",new Vector3((i-1)*.092f,.075f,0),new Vector3(.085f,.065f,.073f),cooked ? new Color(.69f,.77f,.32f) : new Color(.48f,.70f,.29f));
                break;
            case Ingredient.IngredientType.Cheese:
                if(chopped) for(int i=0;i<4;i++) Box(r,"Queso en cubos",new Vector3((i%2-.5f)*.14f,.06f,(i/2-.5f)*.12f),new Vector3(.11f,.11f,.09f),cooked ? bread : white);
                else
                {
                    Part(r,"Queso fresco","Cheese",Vector3.zero,new Vector3(.32f,.12f,.24f),cooked ? new Color(.93f,.68f,.20f) : white);
                    if(cooked) for(int i=0;i<4;i++) Box(r,"Marca de dorado",new Vector3(-.12f+i*.045f,.123f,-.045f),new Vector3(.016f,.006f,.11f),rind,new Vector3(0,-20,0));
                }
                break;
            case Ingredient.IngredientType.Bread:
                Part(r,"Base del pan","Cylinder",new Vector3(0,.035f,0),new Vector3(.36f,.065f,.28f),rind);
                Part(r,"Miga","Cylinder",new Vector3(0,.074f,0),new Vector3(.36f,.022f,.28f),cream);
                Part(r,"Pan de chola","Dome",new Vector3(0,.095f,0),new Vector3(.37f,.27f,.29f),bread);
                BreadScoring(r,.095f,new Vector3(.37f,.27f,.29f));
                break;
            case Ingredient.IngredientType.Meat:
                for(int i=0;i<(chopped?4:2);i++)
                {
                    Vector3 p=new Vector3((i%2-.5f)*.145f,.08f,(i/2-.5f)*.12f);
                    Box(r,"Cerdo",p,new Vector3(.145f,.13f,.16f),pork,new Vector3(0,i*12,0));
                    Box(r,"Cuerito dorado",p+Vector3.up*.068f,new Vector3(.15f,.026f,.165f),rind,new Vector3(0,i*12,0));
                    Box(r,"Franja de grasa",p+new Vector3(0,.03f,-.082f),new Vector3(.13f,.022f,.006f),cream);
                }
                break;
            case Ingredient.IngredientType.Tomato:
                Lump(r,"Tomate",new Vector3(0,.11f,0),new Vector3(.24f,.21f,.23f),new Color(.84f,.07f,.035f));
                for(int i=0;i<5;i++) Box(r,"Hoja",new Vector3(0,.215f,0),new Vector3(.13f,.012f,.03f),green,new Vector3(0,i*72,0));
                break;
            case Ingredient.IngredientType.Lettuce:
                for(int i=0;i<7;i++) Lump(r,"Hoja de lechuga",new Vector3(Mathf.Sin(i*2.4f)*.07f,.10f+(i%2)*.03f,Mathf.Cos(i*2.4f)*.06f),new Vector3(.18f,.16f,.15f),green*(1-i%3*.08f));
                break;
        }
        return root;
    }
    private static Transform IngredientOn(Transform root, Ingredient.IngredientType type, Vector3 p, Vector3 s, Vector3 angles = default)
    {
        Transform child=IngredientModel(type,Preparation.Cooked).transform;
        child.SetParent(root,false); child.localPosition=p; child.localScale=s; child.localRotation=Quaternion.Euler(angles);
        return child;
    }
    private static void Plate(Transform root) { Part(root,"Plato de loza","Plate",Vector3.zero,new Vector3(.88f,1f,.88f),white); }
    public static GameObject DishModel(string id)
    {
        var root=new GameObject(id);
        Transform r=root.transform;
        Plate(r);
        if(id=="Sandwich")
        {
            Part(r,"Pan inferior","Cylinder",new Vector3(0,.075f,0),new Vector3(.58f,.075f,.39f),rind);
            Part(r,"Miga inferior","Cylinder",new Vector3(0,.117f,0),new Vector3(.59f,.018f,.40f),cream);
            for(int i=0;i<6;i++)
            {
                Vector3 p=new Vector3((i%3-1)*.16f,.18f,(i/3-.5f)*.16f);
                Box(r,"Cerdo asado",p,new Vector3(.16f,.12f,.17f),pork,new Vector3(0,i*17,0));
                Box(r,"Cuerito",p+Vector3.up*.062f,new Vector3(.165f,.018f,.17f),rind,new Vector3(0,i*17,0));
                Box(r,"Grasa",p+new Vector3(0,.025f,-.088f),new Vector3(.13f,.018f,.006f),cream);
            }
            for(int i=0;i<16;i++)
            {
                float x=Mathf.Sin(i*2.37f)*.25f,z=Mathf.Cos(i*1.73f)*.15f;
                Box(r,"Cebolla de escabeche",new Vector3(x,.267f,z),new Vector3(.055f,.045f,.045f),onion,new Vector3(0,i*29,8));
                Box(r,"Zanahoria",new Vector3(x,.29f,z),new Vector3(.13f,.012f,.015f),new Color(1,.43f,.045f),new Vector3(0,i*37,5));
                Lump(r,"Hoja de cilantro",new Vector3(x,.31f,z),new Vector3(.075f,.012f,.06f),green);
            }
            Part(r,"Pan superior","Dome",new Vector3(0,.324f,0),new Vector3(.63f,.34f,.43f),bread);
            BreadScoring(r,.324f,new Vector3(.63f,.34f,.43f));
        }
        else if(id=="Api")
        {
            var mug=new GameObject("Api en taza").transform; mug.SetParent(r,false); mug.localPosition=new Vector3(-.23f,.033f,0); Api(mug);
            var pastry=new GameObject("Pastel con azúcar").transform; pastry.SetParent(r,false); pastry.localPosition=new Vector3(.17f,.04f,0); Pastry(pastry,true);
        }
        else if(id=="Paceno")
        {
            IngredientOn(r,Ingredient.IngredientType.Corn,new Vector3(-.10f,.10f,.13f),Vector3.one*.8f,new Vector3(0,0,90));
            IngredientOn(r,Ingredient.IngredientType.Potato,new Vector3(-.15f,.04f,-.13f),Vector3.one*.62f);
            IngredientOn(r,Ingredient.IngredientType.Potato,new Vector3(.02f,.04f,-.15f),Vector3.one*.55f);
            IngredientOn(r,Ingredient.IngredientType.Cheese,new Vector3(.16f,.05f,.10f),Vector3.one*.85f,new Vector3(0,35,0));
            IngredientOn(r,Ingredient.IngredientType.Bean,new Vector3(.14f,.04f,-.09f),Vector3.one*.65f,new Vector3(0,85,0));
        }
        else if(id=="Saltena")
        {
            Part(r,"Masa de salteña","Dome",new Vector3(0,.04f,0),new Vector3(.60f,.62f,.38f),bread);
            for(int i=0;i<11;i++)
            {
                float x=-.25f+i*.05f;
                float y=.065f+.266f*Mathf.Sin(Mathf.PI*(i+1)/12);
                Part(r,"Repulgue trenzado","Sphere",new Vector3(x,y,0),new Vector3(.085f,.040f,.055f),rind,new Vector3(0,0,i%2==0?35:-35));
            }
            for(int i=0;i<6;i++) Lump(r,"Dorado",new Vector3(Mathf.Sin(i*2.2f)*.19f,.12f+(i%2)*.05f,-.145f),new Vector3(.055f,.045f,.012f),rind);
        }
        else
        {
            Part(r,"Copa de frutas","Bowl",new Vector3(0,.04f,0),new Vector3(.43f,.35f,.43f),new Color(.26f,.66f,.68f));
            var fruits=new GameObject("Fruta fresca").transform; fruits.SetParent(r,false); fruits.localPosition=new Vector3(0,.19f,0); Fruit(fruits,true);
            Lump(r,"Crema",new Vector3(0,.33f,0),new Vector3(.22f,.13f,.20f),white);
            Box(r,"Oblea",new Vector3(.10f,.35f,.03f),new Vector3(.065f,.22f,.015f),bread,new Vector3(0,20,-18));
        }
        return root;
    }
}
