#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class FrontEndVisualValidation
{
    public static void Run()
    {
        Directory.CreateDirectory("Logs/ModelPreviews");
        foreach(Ingredient.IngredientType type in System.Enum.GetValues(typeof(Ingredient.IngredientType)))
        {
            var prefab=FoodModelLibrary.IngredientPrefab(type,FoodModelFactory.Preparation.Raw);
            Save(prefab,"Ingrediente_"+type);
        }
        foreach(string id in new[]{"Sandwich","Api","Paceno","Frutas","Saltena"})
            Save(Resources.Load<GameObject>("FoodModels/Dishes/"+id),"Plato_"+id);
        var catalog=Resources.Load<CharacterSkinCatalog>("CharacterSkins");
        for(int i=0;i<5;i++)Save(catalog.Model(i),"Skin_"+i,true);
        Debug.Log("PASS: vistas 3D guardadas en Logs/ModelPreviews.");
    }
    private static void Save(GameObject model,string name,bool character=false)
    {
        using(var preview=new RuntimePreview(model,512,character))
        {
            if(preview.Texture==null)throw new System.Exception("No se pudo renderizar "+name);
            var old=RenderTexture.active;RenderTexture.active=preview.Texture;
            var pixels=new Texture2D(512,512,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,512,512),0,0);pixels.Apply();RenderTexture.active=old;
            File.WriteAllBytes("Logs/ModelPreviews/"+name+".png",pixels.EncodeToPNG());
            Object.DestroyImmediate(pixels);
        }
    }
}
#endif
