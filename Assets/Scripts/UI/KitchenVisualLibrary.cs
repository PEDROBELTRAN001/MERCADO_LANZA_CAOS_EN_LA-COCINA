using System.Collections.Generic;
using UnityEngine;

public sealed class KitchenVisualLibrary : System.IDisposable
{
    private readonly Dictionary<int, RuntimePreview> ingredients = new Dictionary<int, RuntimePreview>();
    private readonly Dictionary<RecipeData, RuntimePreview> dishes = new Dictionary<RecipeData, RuntimePreview>();
    private readonly Dictionary<int, RuntimePreview> skins = new Dictionary<int, RuntimePreview>();
    public static string IngredientName(Ingredient.IngredientType type)
    {
        switch (type)
        {
            case Ingredient.IngredientType.Potato: return "Papa";
            case Ingredient.IngredientType.Corn: return "Choclo";
            case Ingredient.IngredientType.Bean: return "Haba";
            case Ingredient.IngredientType.Cheese: return "Queso";
            case Ingredient.IngredientType.Bread: return "Pan";
            case Ingredient.IngredientType.Meat: return "Cerdo";
            case Ingredient.IngredientType.PurpleCorn: return "Maíz morado (api)";
            case Ingredient.IngredientType.Dough: return "Masa (pastel)";
            case Ingredient.IngredientType.Fruit: return "Fruta";
            case Ingredient.IngredientType.Tomato: return "Tomate";
            case Ingredient.IngredientType.Lettuce: return "Lechuga";
            default: return type.ToString();
        }
    }
    public Texture IngredientIcon(Ingredient.IngredientType type)
    {
        int key = (int)type;
        if (!ingredients.TryGetValue(key, out RuntimePreview preview))
        {
            var model = FoodModelLibrary.CreateIngredient(type, FoodModelFactory.Preparation.Raw);
            model.transform.position = new Vector3(-10000, -10000, -10000);
            preview = new RuntimePreview(model, 128);
            RuntimePreview.Release(model);
            ingredients.Add(key, preview);
        }
        return preview.Texture;
    }
    public Texture DishPicture(RecipeData recipe)
    {
        if (!dishes.TryGetValue(recipe, out RuntimePreview preview))
        {
            var model = FoodModelLibrary.CreateDish(recipe);
            model.transform.position = new Vector3(-10000,-10000,-10000);
            preview = new RuntimePreview(model, 512);
            RuntimePreview.Release(model);
            dishes.Add(recipe, preview);
        }
        return preview.Texture;
    }
    public Texture SkinPicture(int index)
    {
        Texture2D saved=CharacterPortraitLibrary.Get(index);
        if(saved!=null)return saved;
        if (!skins.TryGetValue(index, out RuntimePreview preview))
        {
            var catalog = Resources.Load<CharacterSkinCatalog>("CharacterSkins");
            preview = new RuntimePreview(catalog != null ? catalog.Model(index) : null, 384, true);
            skins.Add(index, preview);
        }
        return preview.Texture;
    }
    public void Dispose()
    {
        foreach (var preview in ingredients.Values) preview.Dispose();
        foreach (var preview in dishes.Values) preview.Dispose();
        foreach (var preview in skins.Values) preview.Dispose();
    }
}
