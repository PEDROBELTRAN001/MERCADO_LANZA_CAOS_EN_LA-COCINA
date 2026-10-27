using System.Collections.Generic;
using UnityEngine;

public static partial class CharacterMeshFactory
{
    private static void Blob(Transform parent,string name,Vector3 at,Vector3 size,Color color)
    {Part(parent,name,"SmoothSphere",at,size,color);}

    // Letras de malla, para que el parche policial no dependa de una imagen o fuente externa.
    private static void PoliceLettering(Transform parent,string word,Vector3 position,float pixel,Color color)
    {
        var font=new Dictionary<char,string>{
            {'P',"110101110100100"},{'O',"111101101101111"},{'L',"100100100100111"},
            {'I',"111010010010111"},{'C',"111100100100111"},{'A',"010101111101101"},
            {'B',"110101110101110"},{'V',"101101101101010"}};
        Transform label=Joint(parent,word,position);
        float start=-(word.Length*4-1)*pixel*.5f;
        for(int letter=0;letter<word.Length;letter++)for(int row=0;row<5;row++)for(int col=0;col<3;col++)
            if(font[word[letter]][row*3+col]=='1')
                Box(label,"Bordado",new Vector3(-start-(letter*4+col)*pixel,(2-row)*pixel,0),new Vector3(pixel*.82f,pixel*.82f,.003f),color);
    }

    private static CharacterVisualRig ReferencePolice()
    {
        var root=new GameObject("OficialCapibara");var rig=root.AddComponent<CharacterVisualRig>();
        rig.designId="capibara";rig.quadruped=false;rig.carryingArmAngle=-6f;
        Color olive=new Color(.38f,.43f,.22f),vest=new Color(.25f,.39f,.24f);
        Color trim=new Color(.20f,.29f,.16f),tan=new Color(.82f,.65f,.42f);
        Color black=new Color(.065f,.075f,.052f),scarlet=new Color(.65f,.13f,.09f);

        // Silueta vertical y redonda, con botines cortos como en la hoja de referencia.
        rig.torso=Joint(root.transform,"Torso",new Vector3(0,.64f,0));
        Blob(rig.torso,"Cuerpo redondo de capibara",Vector3.zero,new Vector3(.91f,.94f,.74f),olive);
        Blob(rig.torso,"Chaleco verde oliva",new Vector3(0,.14f,.005f),new Vector3(.94f,.64f,.77f),vest);
        for(int side=-1;side<=1;side+=2)
        {
            Transform leg=Joint(root.transform,side<0?"Pierna_L":"Pierna_R",new Vector3(side*.145f,.25f,0));
            Blob(leg,"Pantalón verde oliva",new Vector3(0,-.05f,0),new Vector3(.23f,.29f,.24f),olive);
            Blob(leg,"Botín policial",new Vector3(0,-.16f,.045f),new Vector3(.24f,.16f,.30f),black);
            if(side<0)rig.leftLeg=leg;else rig.rightLeg=leg;
            Transform arm=Joint(root.transform,side<0?"Brazo_L":"Brazo_R",new Vector3(side*.41f,1.00f,.015f));
            Blob(arm,"Manga del uniforme",new Vector3(side*.035f,-.15f,.025f),new Vector3(.26f,.36f,.30f),olive);
            Blob(arm,"Antebrazo apoyado",new Vector3(side*.005f,-.275f,.16f),new Vector3(.245f,.205f,.36f),olive);
            Part(arm,"Puño del uniforme","Cylinder",new Vector3(-side*.015f,-.29f,.31f),new Vector3(.16f,.035f,.16f),trim,new Vector3(90,0,0));
            Blob(arm,"Mano redonda",new Vector3(-side*.03f,-.30f,.355f),new Vector3(.17f,.135f,.14f),tan);
            Blob(arm,"Pulgar",new Vector3(-side*.09f,-.265f,.375f),new Vector3(.065f,.075f,.075f),tan);
            if(side<0)rig.leftArm=arm;else rig.rightArm=arm;
            Blob(rig.torso,"Hombrera",new Vector3(side*.30f,.37f,.02f),new Vector3(.20f,.045f,.25f),trim);
            Blob(rig.torso,"Botón de hombrera",new Vector3(side*.29f,.394f,.065f),new Vector3(.035f,.014f,.035f),gold);
        }
        Part(rig.torso,"Cinturón verde","PoliceBelt",Vector3.zero,new Vector3(1f,1f,.74f/.91f),trim);
        Box(rig.torso,"Hebilla",new Vector3(0,-.12f,.366f),new Vector3(.11f,.065f,.018f),gold);
        Box(rig.torso,"Centro de hebilla",new Vector3(0,-.12f,.378f),new Vector3(.065f,.035f,.012f),trim);

        // Cuello blanco visible y solapas del chaleco táctico.
        Part(root.transform,"Cuello de camisa blanca","Cylinder",new Vector3(0,1.065f,0),new Vector3(.28f,.11f,.27f),ivory);
        Panel(rig.torso,"Cuello blanco izquierdo",new[]{new Vector2(-.19f,.05f),new Vector2(-.08f,-.10f),new Vector2(0,.015f)},new Vector3(0,.37f,.29f),ivory);
        Panel(rig.torso,"Cuello blanco derecho",new[]{new Vector2(0,.015f),new Vector2(.08f,-.10f),new Vector2(.19f,.05f)},new Vector3(0,.37f,.29f),ivory);
        Box(rig.torso,"Cierre del chaleco",new Vector3(0,.10f,.391f),new Vector3(.035f,.37f,.015f),trim);
        for(int i=0;i<3;i++)Blob(rig.torso,"Botón dorado",new Vector3(0,.23f-i*.11f,.409f),new Vector3(.027f,.027f,.014f),gold);
        Box(rig.torso,"Placa policial",new Vector3(.19f,.18f,.361f),new Vector3(.25f,.13f,.025f),gold);
        Box(rig.torso,"Parche POLICIA BOLIVIA",new Vector3(.19f,.18f,.378f),new Vector3(.235f,.115f,.014f),trim);
        PoliceLettering(rig.torso,"POLICIA",new Vector3(.19f,.204f,.39f),.0075f,gold);
        PoliceLettering(rig.torso,"BOLIVIA",new Vector3(.19f,.157f,.39f),.006f,gold);
        Blob(rig.torso,"Bolsillo de radio",new Vector3(-.21f,.12f,.36f),new Vector3(.20f,.245f,.065f),trim);
        Box(rig.torso,"Radio de chaleco",new Vector3(-.21f,.175f,.405f),new Vector3(.095f,.14f,.04f),black);
        Box(rig.torso,"Pantalla de radio",new Vector3(-.21f,.20f,.43f),new Vector3(.065f,.035f,.008f),new Color(.58f,.66f,.38f));
        Line(rig.torso,"Antena del chaleco",new Vector3(-.23f,.24f,.41f),new Vector3(-.23f,.36f,.41f),.012f,black);
        Transform whistle=Joint(rig.torso,"Silbato de policía",new Vector3(.06f,.02f,.412f));
        Blob(whistle,"Silbato metálico",Vector3.zero,new Vector3(.065f,.035f,.043f),new Color(.73f,.76f,.69f));
        Box(whistle,"Boquilla",new Vector3(0,.01f,.023f),new Vector3(.03f,.02f,.04f),new Color(.73f,.76f,.69f));
        Line(rig.torso,"Cordón del silbato",new Vector3(.09f,.29f,.34f),new Vector3(.06f,.04f,.415f),.007f,gold);

        // Rostro sencillo, cálido y sonriente, sin el hocico largo de la versión anterior.
        rig.head=Joint(root.transform,"Cabeza",new Vector3(0,1.24f,.005f));
        Blob(rig.head,"Cabeza redonda",Vector3.zero,new Vector3(.50f,.45f,.45f),tan);
        for(int side=-1;side<=1;side+=2)
        {
            Blob(rig.head,"Oreja pequeña",new Vector3(side*.24f,.015f,0),new Vector3(.075f,.10f,.07f),tan);
            Blob(rig.head,"Ojo amable",new Vector3(side*.09f,.025f,.215f),new Vector3(.045f,.054f,.023f),black);
            Blob(rig.head,"Brillo de mirada",new Vector3(side*.09f-.007f,.038f,.228f),new Vector3(.010f,.011f,.006f),ivory);
            Blob(rig.head,"Mejilla",new Vector3(side*.14f,-.055f,.193f),new Vector3(.073f,.035f,.011f),new Color(.78f,.52f,.32f));
        }
        Blob(rig.head,"Nariz pequeña",new Vector3(0,-.025f,.231f),new Vector3(.060f,.045f,.035f),tan);
        for(int i=0;i<8;i++)
        {
            float x=-.077f+i*.01925f,nx=x+.01925f;
            Line(rig.head,"Sonrisa",new Vector3(x,-.104f+3.4f*x*x,.209f),new Vector3(nx,-.104f+3.4f*nx*nx,.209f),.008f,black);
        }
        Transform cap=Joint(rig.head,"Gorra de policía",new Vector3(0,.21f,0));
        Part(cap,"Banda roja","Cylinder",new Vector3(0,.005f,0),new Vector3(.55f,.065f,.48f),scarlet);
        Part(cap,"Ribete dorado inferior","Cylinder",new Vector3(0,-.029f,0),new Vector3(.56f,.016f,.49f),gold);
        Part(cap,"Ribete dorado superior","Cylinder",new Vector3(0,.039f,0),new Vector3(.57f,.014f,.50f),gold);
        Blob(cap,"Copa verde oliva",new Vector3(0,.085f,-.015f),new Vector3(.65f,.16f,.56f),olive);
        Blob(cap,"Visera negra",new Vector3(0,-.045f,.19f),new Vector3(.55f,.026f,.31f),black);
        Blob(cap,"Escarapela dorada",new Vector3(0,.086f,.262f),new Vector3(.082f,.092f,.019f),gold);
        Blob(cap,"Centro de escarapela",new Vector3(0,.086f,.275f),new Vector3(.050f,.061f,.009f),trim);
        Part(cap,"Estrella de escarapela","Star",new Vector3(0,.086f,.282f),new Vector3(.036f,.043f,.10f),gold);

        // Bandeja de inspección como en la referencia; se oculta cuando el jugador lleva comida.
        Transform tray=Joint(root.transform,"Equipo policial de inspección",new Vector3(0,.705f,.47f));
        Box(tray,"Bandeja clara",Vector3.zero,new Vector3(.80f,.028f,.31f),ivory);
        Box(tray,"Borde frontal",new Vector3(0,.018f,.148f),new Vector3(.81f,.022f,.014f),new Color(.68f,.70f,.60f));
        Box(tray,"Portapapeles",new Vector3(.17f,.022f,.01f),new Vector3(.25f,.015f,.22f),new Color(.49f,.45f,.30f));
        Box(tray,"Formulario",new Vector3(.17f,.034f,.014f),new Vector3(.21f,.008f,.185f),ivory);
        for(int i=0;i<5;i++)Box(tray,"Línea del formulario",new Vector3(.17f,.039f,-.045f+i*.025f),new Vector3(.16f,.002f,.004f),new Color(.40f,.46f,.39f));
        Part(tray,"Bolígrafo","Cylinder",new Vector3(.31f,.037f,0),new Vector3(.012f,.18f,.012f),black,new Vector3(90,0,0));
        Box(tray,"Radio portátil",new Vector3(-.25f,.10f,0),new Vector3(.13f,.20f,.085f),black);
        Box(tray,"Pantalla portátil",new Vector3(-.25f,.15f,.047f),new Vector3(.09f,.055f,.009f),new Color(.51f,.62f,.45f));
        for(int i=0;i<3;i++)Box(tray,"Tecla de radio",new Vector3(-.28f+i*.03f,.068f,.047f),new Vector3(.019f,.025f,.008f),new Color(.44f,.47f,.40f));
        Line(tray,"Antena de radio portátil",new Vector3(-.29f,.20f,0),new Vector3(-.29f,.39f,0),.014f,black);
        Box(tray,"Terminal de inspección",new Vector3(-.035f,.09f,.01f),new Vector3(.10f,.16f,.045f),trim);
        Box(tray,"Pantalla del terminal",new Vector3(-.035f,.11f,.036f),new Vector3(.07f,.07f,.008f),new Color(.68f,.74f,.40f));
        rig.idleEquipment=tray.gameObject;
        rig.carryPoint=Joint(root.transform,"PuntoManos",new Vector3(.25f,.79f,.48f));
        rig.sabotageStains=Stains(rig.torso,new Vector3(.04f,-.04f,.40f));
        return rig;
    }
}
