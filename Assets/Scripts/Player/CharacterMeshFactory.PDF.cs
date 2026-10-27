using System.Collections.Generic;
using UnityEngine;

public static partial class CharacterMeshFactory
{
    private static readonly Color documentSkin=new Color(.80f,.62f,.39f);
    private static readonly Color felt=new Color(.36f,.36f,.35f);
    private static readonly Color charcoal=new Color(.23f,.24f,.24f);
    private static readonly Color whiteShirt=new Color(.94f,.94f,.89f);

    private static CharacterVisualRig DocumentBody(string id,float hipHeight)
    {
        var root=new GameObject(id);var rig=root.AddComponent<CharacterVisualRig>();rig.designId=id;rig.carryingArmAngle=-6f;
        rig.torso=Joint(root.transform,"Torso",new Vector3(0,.64f,0));
        for(int side=-1;side<=1;side+=2)
        {
            var leg=Joint(root.transform,side<0?"Pierna_L":"Pierna_R",new Vector3(side*.14f,hipHeight,0));
            Blob(leg,"Pantalón y botín",new Vector3(0,-.055f,.01f),new Vector3(.23f,.26f,.25f),dark);
            Blob(leg,"Botín negro",new Vector3(0,-hipHeight+.085f,.045f),new Vector3(.24f,.15f,.29f),dark);
            if(side<0)rig.leftLeg=leg;else rig.rightLeg=leg;
        }
        rig.carryPoint=Joint(root.transform,"PuntoManos",new Vector3(.27f,.79f,.47f));
        return rig;
    }
    private static Transform DocumentArm(Transform parent,bool left,Color sleeve,Color complexion,float shoulder=1f,float spread=.35f)
    {
        float side=left?-1f:1f;var arm=Joint(parent,left?"Brazo_L":"Brazo_R",new Vector3(side*spread,shoulder,0));
        Blob(arm,"Manga larga",new Vector3(side*.035f,-.13f,.015f),new Vector3(.245f,.34f,.28f),sleeve);
        Blob(arm,"Antebrazo",new Vector3(side*.005f,-.25f,.14f),new Vector3(.23f,.19f,.31f),sleeve);
        Blob(arm,"Puño",new Vector3(-side*.015f,-.265f,.27f),new Vector3(.16f,.16f,.09f),whiteShirt);
        Blob(arm,"Mano sin dedos",new Vector3(-side*.025f,-.27f,.325f),new Vector3(.155f,.14f,.14f),complexion);
        return arm;
    }
    private static void DocumentFace(Transform head,Color complexion,bool serious=false,bool masked=false)
    {
        Blob(head,"Rostro minimalista",Vector3.zero,new Vector3(.46f,.43f,.42f),complexion);
        for(int side=-1;side<=1;side+=2)
        {
            Blob(head,"Oreja pequeña",new Vector3(side*.222f,0,0),new Vector3(.065f,.09f,.06f),complexion);
            Blob(head,"Ojo de punto",new Vector3(side*.087f,.027f,.204f),new Vector3(.041f,.050f,.024f),dark);
            Blob(head,"Brillo de mirada",new Vector3(side*.087f-.005f,.037f,.218f),new Vector3(.009f,.010f,.006f),ivory);
        }
        if(!masked)
        {
            Blob(head,"Nariz pequeña",new Vector3(0,-.025f,.218f),new Vector3(.048f,.040f,.028f),complexion);
            if(serious)Line(head,"Boca seria",new Vector3(-.065f,-.094f,.195f),new Vector3(.065f,-.094f,.195f),.008f,dark);
            else for(int i=0;i<8;i++)
            {
                float x=-.073f+i*.01825f,nx=x+.01825f;
                Line(head,"Sonrisa",new Vector3(x,-.099f+3.5f*x*x,.196f),new Vector3(nx,-.099f+3.5f*nx*nx,.196f),.008f,dark);
            }
        }
    }
    private static void DocumentCollar(Transform torso,Color color)
    {
        Panel(torso,"Cuello plegado izquierdo",new[]{new Vector2(-.17f,.055f),new Vector2(-.065f,-.08f),new Vector2(0,.02f)},new Vector3(0,.33f,.29f),color);
        Panel(torso,"Cuello plegado derecho",new[]{new Vector2(0,.02f),new Vector2(.065f,-.08f),new Vector2(.17f,.055f)},new Vector3(0,.33f,.29f),color);
    }
    private static void WaiterChestCloth(Transform torso,string name,float[] heights,float[] widths,Color color,float offset)
    {
        var v=new List<Vector3>();var t=new List<int>();
        for(int row=0;row<heights.Length;row++)for(int side=0;side<3;side++)
        {
            float x=(side-1)*widths[row],y=heights[row];
            float z=.35f*Mathf.Sqrt(Mathf.Max(.01f,1-y*y/(.425f*.425f)-x*x/(.435f*.435f)))+offset;
            v.Add(new Vector3(x,y,z));
            if(row>0 && side>0){int a=(row-1)*3+side-1,b=a+3;t.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
        }
        var node=Joint(torso,name,Vector3.zero);var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;node.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Mat(color);
    }
    private static void DocumentWaiterHat(Transform head)
    {
        var hat=Joint(head,"Sombrero de garzón",Vector3.zero);
        Blob(hat,"Ala negra",new Vector3(0,.20f,0),new Vector3(.62f,.028f,.51f),dark);
        Part(hat,"Copa baja negra","Cylinder",new Vector3(0,.285f,-.01f),new Vector3(.49f,.19f,.42f),dark);
        Part(hat,"Cinta roja del sombrero","Cylinder",new Vector3(0,.244f,-.01f),new Vector3(.502f,.047f,.432f),red);
        var feather=Joint(hat,"Pluma decorativa",new Vector3(.20f,.31f,.06f));feather.localRotation=Quaternion.Euler(0,0,-16);
        Blob(feather,"Pluma roja",new Vector3(0,.075f,0),new Vector3(.054f,.22f,.018f),red);
        Line(feather,"Raquis",new Vector3(0,-.025f,.012f),new Vector3(0,.17f,.012f),.006f,gold);
    }
    private static CharacterVisualRig DocumentWaiter(bool rival)
    {
        var rig=DocumentBody(rival?"rival":"tomas",.23f);Transform root=rig.transform;Color vest=rival?charcoal:felt;
        Blob(rig.torso,"Base negra",new Vector3(0,-.27f,0),new Vector3(.75f,.39f,.64f),dark);
        Blob(rig.torso,"Chaleco gris",Vector3.zero,new Vector3(.87f,.85f,.70f),vest);
        rig.leftArm=DocumentArm(root,true,whiteShirt,documentSkin);rig.rightArm=DocumentArm(root,false,whiteShirt,documentSkin);
        Part(root,"Camisa blanca","Cylinder",new Vector3(0,1.055f,0),new Vector3(.24f,.10f,.24f),whiteShirt);
        WaiterChestCloth(rig.torso,"Pechera blanca",new[]{.31f,.25f,.15f},new[]{.12f,.08f,.001f},whiteShirt,.012f);
        DocumentCollar(rig.torso,whiteShirt);
        for(int i=0;i<4;i++)
        {
            float y=.12f-i*.085f,z=.35f*Mathf.Sqrt(1-y*y/(.425f*.425f))+.009f;
            Blob(rig.torso,"Botón negro",new Vector3(0,y,z),new Vector3(.024f,.024f,.012f),dark);
        }
        if(rival)
        {
            WaiterChestCloth(rig.torso,"Corbata negra",new[]{.27f,.21f,.10f,.065f},new[]{.020f,.026f,.045f,.001f},dark,.024f);
            Blob(rig.torso,"Escarapela violeta",new Vector3(-.15f,.20f,.315f),new Vector3(.032f,.032f,.009f),violet);
        }
        rig.head=Joint(root,"Cabeza",new Vector3(0,1.24f,0));DocumentFace(rig.head,documentSkin,rival);DocumentWaiterHat(rig.head);
        var tray=Joint(root,"Bandeja de servicio",new Vector3(0,.746f,.45f));
        Blob(tray,"Bandeja metálica",Vector3.zero,new Vector3(.78f,.028f,.34f),new Color(.62f,.65f,.61f));
        Part(tray,"Plato pequeño","Cylinder",new Vector3(-.19f,.025f,0),new Vector3(.22f,.014f,.22f),ivory);
        Blob(tray,"Pan de muestra",new Vector3(-.19f,.073f,0),new Vector3(.16f,.09f,.14f),gold);
        Part(tray,"Vaso de muestra","Cylinder",new Vector3(.19f,.072f,0),new Vector3(.095f,.13f,.095f),new Color(.78f,.44f,.18f));
        Part(tray,"Borde del vaso","Cylinder",new Vector3(.19f,.14f,0),new Vector3(.10f,.010f,.10f),ivory);
        rig.idleEquipment=tray.gameObject;rig.sabotageStains=Stains(rig.torso,new Vector3(.04f,-.04f,.37f));
        return rig;
    }

    private static void DocumentBraids(Transform head,float length= .48f)
    {
        for(int side=-1;side<=1;side+=2)
        {
            var braid=Joint(head,side<0?"Trenza_L":"Trenza_R",Vector3.zero);
            for(int i=0;i<9;i++)Blob(braid,"Cabello trenzado",new Vector3(side*(.19f+(i%2==0?.008f:-.008f)),-.07f-i*length/9,-.17f),new Vector3(.085f,.085f,.078f),hair);
            Blob(braid,"Lazo rosa",new Vector3(side*.19f,-.07f-length,-.17f),new Vector3(.10f,.04f,.08f),new Color(.80f,.25f,.48f));
        }
    }
    private static void BackAguayo(Transform torso,float radiusX,float radiusZ)
    {
        Color[] colors={new Color(.73f,.12f,.40f),new Color(.94f,.44f,.61f),new Color(.28f,.24f,.57f),new Color(.10f,.54f,.55f),gold,new Color(.75f,.14f,.45f)};
        for(int stripe=0;stripe<12;stripe++)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int segment=0;segment<=28;segment++)
            {
                float a=Mathf.PI*.43f+segment/28f*Mathf.PI*1.14f;
                float y=.27f-stripe*.025f+.10f*Mathf.Sin(a)*Mathf.Sin(a);
                for(int edge=0;edge<2;edge++)vertices.Add(new Vector3(Mathf.Sin(a)*radiusX,y-edge*.025f,Mathf.Cos(a)*radiusZ));
                if(segment>0){int b=(segment-1)*2;triangles.AddRange(new[]{b,b+1,b+2,b+2,b+1,b+3});}
            }
            var node=Joint(torso,"Franja de aguayo",Vector3.zero);var mesh=new Mesh{name="Aguayo tejido"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;node.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Mat(colors[stripe%colors.Length]);
        }
    }
    private static CharacterVisualRig DocumentCook()
    {
        var rig=DocumentBody("donaRosa",.21f);Transform root=rig.transform;Color complexion=new Color(.64f,.44f,.29f),grey=new Color(.66f,.66f,.63f);
        Blob(rig.torso,"Vestido gris de cocina",new Vector3(0,-.035f,0),new Vector3(.98f,.89f,.77f),grey);
        Blob(rig.torso,"Mandil blanco",new Vector3(0,-.035f,.025f),new Vector3(.99f,.89f,.79f),whiteShirt);
        rig.leftArm=DocumentArm(root,true,grey,complexion,.95f,.40f);rig.rightArm=DocumentArm(root,false,grey,complexion,.95f,.40f);
        BackAguayo(rig.torso,.47f,.39f);
        for(int stripe=0;stripe<6;stripe++)
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int row=0;row<7;row++)for(int edge=0;edge<2;edge++)
            {
                float f=row/6f,x=-.33f+stripe*.020f+edge*.020f+.21f*f,y=.31f-.29f*f;
                float z=.025f+.395f*Mathf.Sqrt(Mathf.Max(.01f,1-(y+.035f)*(y+.035f)/(.445f*.445f)-x*x/(.495f*.495f)))+.009f;
                v.Add(new Vector3(x,y,z));if(row>0 && edge==1){int a=(row-1)*2;t.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
            }
            var node=Joint(rig.torso,"Aguayo sobre hombro",Vector3.zero);var mesh=new Mesh{name="Aguayo sobre hombro"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;node.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Mat(stripe%3==0?gold:stripe%3==1?violet:new Color(.83f,.25f,.48f));
        }
        for(int side=-1;side<=1;side+=2)FabricPocket(rig.torso,"Bolsillo de cocina",side*.16f,-.085f,new Color(.86f,.87f,.83f),false);
        rig.head=Joint(root,"Cabeza",new Vector3(0,1.16f,.005f));DocumentFace(rig.head,complexion,false,true);DocumentBraids(rig.head,.46f);
        Blob(rig.head,"Cofia blanca",new Vector3(0,.185f,-.012f),new Vector3(.57f,.25f,.49f),whiteShirt);
        Part(rig.head,"Borde de cofia","Cylinder",new Vector3(0,.145f,0),new Vector3(.50f,.035f,.44f),new Color(.80f,.82f,.78f));
        for(int i=-3;i<=3;i++)Line(rig.head,"Pliegue de cofia",new Vector3(i*.055f,.17f,.218f),new Vector3(i*.033f,.28f,.12f),.004f,new Color(.83f,.85f,.82f));
        Color mask=new Color(.78f,.89f,.91f);
        Blob(rig.head,"Barbijo de protección",new Vector3(0,-.067f,.219f),new Vector3(.32f,.14f,.055f),mask);
        for(int i=0;i<2;i++)Line(rig.head,"Pliegue del barbijo",new Vector3(-.12f,-.045f-i*.035f,.249f),new Vector3(.12f,-.045f-i*.035f,.249f),.004f,whiteShirt);
        for(int side=-1;side<=1;side+=2)Line(rig.head,"Elástico del barbijo",new Vector3(side*.15f,-.055f,.22f),new Vector3(side*.23f,-.035f,0),.007f,whiteShirt);
        var ladle=Joint(rig.rightArm,"Cucharón metálico",new Vector3(0,-.25f,.32f));ladle.localRotation=Quaternion.Euler(0,0,-18);
        Part(ladle,"Mango del cucharón","Cylinder",new Vector3(0,.12f,0),new Vector3(.025f,.34f,.025f),new Color(.58f,.62f,.61f));
        Blob(ladle,"Cazo metálico",new Vector3(0,.31f,.015f),new Vector3(.14f,.10f,.13f),new Color(.63f,.66f,.64f));
        Blob(ladle,"Interior del cazo",new Vector3(0,.342f,.027f),new Vector3(.105f,.027f,.091f),new Color(.26f,.30f,.28f));
        rig.idleEquipment=ladle.gameObject;rig.sabotageStains=Stains(rig.torso,new Vector3(0,-.05f,.42f));
        rig.carryPoint.localPosition=new Vector3(-.25f,.74f,.44f);
        return rig;
    }
    private static void BellBand(Transform parent,string name,float bottom,float top,float bottomRadius,float topRadius,Color color)
    {
        var v=new List<Vector3>();var t=new List<int>();const int sides=40;
        for(int ring=0;ring<2;ring++)for(int i=0;i<=sides;i++)
        {
            float a=i*Mathf.PI*2/sides,r=ring==0?bottomRadius:topRadius;
            r*=1f+.014f*Mathf.Cos(a*20);v.Add(new Vector3(Mathf.Cos(a)*r,ring==0?bottom:top,Mathf.Sin(a)*r));
        }
        for(int i=0;i<sides;i++){int a=i,b=a+sides+1;t.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
        var part=Joint(parent,name,Vector3.zero);var mesh=Flat(name,v,t);part.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;part.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Mat(color);
    }
    private static void CholitaApron(Transform root,Color blue)
    {
        float[] heights={.30f,.40f,.57f,.72f,.90f,1.03f},widths={.34f,.34f,.29f,.24f,.18f,.15f},front={.496f,.476f,.412f,.34f,.315f,.25f};
        var v=new List<Vector3>();var t=new List<int>();
        for(int row=0;row<heights.Length;row++)for(int col=0;col<7;col++)
        {
            float x=col/3f-1f;v.Add(new Vector3(x*widths[row],heights[row],front[row]-.10f*x*x));
            if(row>0 && col>0){int b=(row-1)*7+col-1;t.AddRange(new[]{b,b+1,b+7,b+7,b+1,b+8});}
        }
        var apron=Joint(root,"Mandil azul",Vector3.zero);var mesh=new Mesh{name="Mandil azul curvado"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        apron.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;apron.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Mat(blue);
        for(int side=-1;side<=1;side+=2)FabricPocket(root,"Bolsillo azul",side*.15f,.54f,new Color(.08f,.53f,.70f),true);
    }
    private static float PocketSurface(float x,float y,bool cholita)
    {
        if(!cholita)return .025f+.395f*Mathf.Sqrt(Mathf.Max(.01f,1-(y+.035f)*(y+.035f)/(.445f*.445f)-x*x/(.495f*.495f)));
        float[] heights={.30f,.40f,.57f,.72f,.90f,1.03f},widths={.34f,.34f,.29f,.24f,.18f,.15f},front={.496f,.476f,.412f,.34f,.315f,.25f};
        for(int i=1;i<heights.Length;i++)if(y<=heights[i])
        {float a=Mathf.InverseLerp(heights[i-1],heights[i],y),w=Mathf.Lerp(widths[i-1],widths[i],a);return Mathf.Lerp(front[i-1],front[i],a)-.10f*x*x/(w*w);}
        return .25f;
    }
    private static void FabricPocket(Transform parent,string name,float center,float top,Color color,bool cholita)
    {
        var v=new List<Vector3>();var t=new List<int>();
        for(int row=0;row<4;row++)for(int col=0;col<5;col++)
        {
            float y=top-row*.047f,half=row==3?.075f:.105f,x=center+(col/2f-1f)*half;
            v.Add(new Vector3(x,y,PocketSurface(x,y,cholita)+.010f));
            if(row>0 && col>0){int a=(row-1)*5+col-1,b=a+5;t.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
        }
        var node=Joint(parent,name,Vector3.zero);var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;node.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Mat(color);
    }
    private static CharacterVisualRig DocumentCholita()
    {
        var rig=DocumentBody("cholita",.19f);Transform root=rig.transform;Color purple=new Color(.35f,.23f,.52f),blue=new Color(.07f,.47f,.64f);
        rig.torso.localPosition=new Vector3(0,.78f,0);
        Blob(rig.torso,"Blusa morada",new Vector3(0,.055f,0),new Vector3(.67f,.55f,.58f),purple);
        BellBand(root,"Pollera verde",.19f,.33f,.49f,.47f,new Color(.19f,.48f,.18f));
        BellBand(root,"Pollera amarilla",.33f,.44f,.47f,.445f,new Color(.96f,.70f,.16f));
        BellBand(root,"Pollera naranja",.44f,.74f,.445f,.29f,new Color(.91f,.34f,.10f));
        rig.leftArm=DocumentArm(root,true,purple,documentSkin,1.02f,.40f);rig.rightArm=DocumentArm(root,false,purple,documentSkin,1.02f,.40f);
        CholitaApron(root,blue);BackAguayo(rig.torso,.35f,.31f);
        Panel(rig.torso,"Pañuelo de aguayo",new[]{new Vector2(-.23f,.25f),new Vector2(0,.04f),new Vector2(.23f,.25f)},new Vector3(0,0,.31f),new Color(.78f,.19f,.46f));
        for(int i=0;i<3;i++)Line(rig.torso,"Franja del pañuelo",new Vector3(-.17f+i*.045f,.23f,.322f),new Vector3(.025f+i*.045f,.10f,.322f),.015f,i%2==0?gold:teal);
        rig.head=Joint(root,"Cabeza",new Vector3(0,1.25f,0));DocumentFace(rig.head,documentSkin);DocumentBraids(rig.head,.40f);
        Color brown=new Color(.32f,.19f,.105f);
        Blob(rig.head,"Sombrero bombín",new Vector3(0,.29f,-.01f),new Vector3(.51f,.25f,.45f),brown);
        Blob(rig.head,"Ala del bombín",new Vector3(0,.20f,0),new Vector3(.67f,.028f,.57f),brown);
        Part(rig.head,"Cinta del bombín","Cylinder",new Vector3(0,.245f,-.01f),new Vector3(.525f,.045f,.46f),dark);
        Blob(rig.head,"Adorno lateral",new Vector3(-.25f,.245f,.04f),new Vector3(.075f,.055f,.025f),new Color(.78f,.21f,.46f));
        var basket=Joint(rig.leftArm,"Canasta de mercado",new Vector3(-.055f,-.34f,.17f));
        Blob(basket,"Canasta tejida",Vector3.zero,new Vector3(.33f,.24f,.29f),new Color(.60f,.37f,.16f));
        Part(basket,"Asa de canasta","Ring",new Vector3(0,.18f,0),new Vector3(.31f,.31f,.13f),gold);
        for(int i=0;i<4;i++)Part(basket,"Tejido horizontal","Cylinder",new Vector3(0,-.065f+i*.045f,0),new Vector3(.33f,.010f,.29f),gold);
        for(int i=0;i<5;i++)Blob(basket,i%2==0?"Papa de mercado":"Chuño",new Vector3(Mathf.Sin(i*2.4f)*.09f,.10f,Mathf.Cos(i*2.4f)*.07f),new Vector3(.09f,.07f,.075f),i%2==0?new Color(.58f,.39f,.19f):new Color(.84f,.81f,.66f));
        rig.idleEquipment=basket.gameObject;rig.sabotageStains=Stains(root,new Vector3(.03f,.60f,.44f));
        return rig;
    }
}
