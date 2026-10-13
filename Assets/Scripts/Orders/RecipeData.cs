using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Kitchen/Recipe Data")]
public class RecipeData : ScriptableObject
{
    [Header("Recipe Info")]
    public string recipeName = "Hamburguesa";
    public Sprite recipeIcon;
    [Tooltip("Modelo del plato terminado (FBX de Assets/Prefabs)")]
    public GameObject dishPrefab;

    [Header("Ingredients")]
    public Ingredient.IngredientType[] requiredIngredients;

    [Header("Requirements")]
    public bool requiresChopping = true;
    public bool requiresCooking = true;

    [Header("Scoring")]
    public int basePoints = 25;

    [Header("Balance")]
    [Tooltip("Segundos que tiene el jugador para entregar este plato (0 = usar el tiempo general de OrderSystem)")]
    public float timeLimit = 0f;

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
                    if (requiresChopping && !ingredients[j].isChopped) return false;
                    if (requiresCooking && !ingredients[j].isCooked) return false;
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }

        return true;
    }
}
