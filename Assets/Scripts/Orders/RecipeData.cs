using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Kitchen/Recipe Data")]
public class RecipeData : ScriptableObject
{
    // Como se prepara cada ingrediente antes de armar el plato
    public enum Prep { None, Chop, Cook }

    [Header("Recipe Info")]
    public string recipeName = "Hamburguesa";
    public Sprite recipeIcon;
    [Tooltip("Modelo del plato terminado (FBX de Assets/Prefabs)")]
    public GameObject dishPrefab;

    [Header("Ingredients")]
    public Ingredient.IngredientType[] requiredIngredients;
    [Tooltip("Preparacion de cada ingrediente, en el mismo orden (si esta vacio se usan los requisitos de abajo para todos)")]
    public Prep[] requiredPrep;

    [Header("Requirements (si requiredPrep esta vacio)")]
    public bool requiresChopping = true;
    public bool requiresCooking = true;

    [Header("Scoring")]
    public int basePoints = 25;

    [Header("Balance")]
    [Tooltip("Segundos que tiene el jugador para entregar este plato (0 = usar el tiempo general de OrderSystem)")]
    public float timeLimit = 0f;

    // Preparacion del ingrediente i de la receta
    public Prep PrepAt(int i)
    {
        if (requiredPrep != null && i < requiredPrep.Length) return requiredPrep[i];
        if (requiresChopping) return Prep.Chop;
        if (requiresCooking) return Prep.Cook;
        return Prep.None;
    }

    public Prep PrepOf(Ingredient.IngredientType type)
    {
        int i = System.Array.IndexOf(requiredIngredients, type);
        return i >= 0 ? PrepAt(i) : Prep.None;
    }

    public static bool IsPrepared(Ingredient ingredient, Prep prep)
    {
        switch (prep)
        {
            case Prep.Chop: return ingredient.isChopped;
            case Prep.Cook: return ingredient.isCooked;
            default: return true;
        }
    }

    public bool CheckRecipe(Ingredient[] ingredients)
    {
        if (ingredients.Length != requiredIngredients.Length) return false;

        for (int i = 0; i < requiredIngredients.Length; i++)
        {
            bool found = false;
            for (int j = 0; j < ingredients.Length; j++)
            {
                if (ingredients[j].ingredientType == requiredIngredients[i])
                {
                    if (!IsPrepared(ingredients[j], PrepAt(i))) return false;
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }

        return true;
    }
}
