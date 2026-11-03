using UnityEngine;

// Fotos de los modelos guardadas como recursos: disponibles incluso antes del primer render 3D.
public static class CharacterPortraitLibrary
{
    private static readonly string[] Names={"Tomas","Rival","DonaRosa","Cholita","Capibara"};
    private static readonly Texture2D[] full=new Texture2D[5],bust=new Texture2D[5];
    public static Texture2D Get(int index,bool closeUp=false)
    {
        if(index<0 || index>=Names.Length)return null;
        var cache=closeUp?bust:full;
        if(cache[index]==null)cache[index]=Resources.Load<Texture2D>("CharacterPortraits/"+Names[index]+(closeUp?"_Rostro":""));
        return cache[index];
    }
}
