using UnityEngine;

public static class FoodModelLibrary
{
    public static string DishId(RecipeData recipe)
    {
        string name=recipe.recipeName.ToLowerInvariant();
        if(name.Contains("chola")) return "Sandwich";
        if(name.Contains("api")) return "Api";
        if(name.Contains("pace")) return "Paceno";
        if(name.Contains("salte")) return "Saltena";
        return "Frutas";
    }
    public static GameObject IngredientPrefab(Ingredient.IngredientType type, FoodModelFactory.Preparation state) =>
        Resources.Load<GameObject>("FoodModels/Ingredients/"+type+"_"+state);
    public static GameObject DishPrefab(RecipeData recipe) => Resources.Load<GameObject>("FoodModels/Dishes/"+DishId(recipe));
    public static GameObject CreateIngredient(Ingredient.IngredientType type, FoodModelFactory.Preparation state)
    {
        var prefab=IngredientPrefab(type,state);
        return prefab!=null ? Object.Instantiate(prefab) : FoodModelFactory.IngredientModel(type,state);
    }
    public static GameObject CreateDish(RecipeData recipe)
    {
        var prefab=DishPrefab(recipe);
        return prefab!=null ? Object.Instantiate(prefab) : FoodModelFactory.DishModel(DishId(recipe));
    }
}
