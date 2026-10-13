using UnityEngine;

public class Ingredient : MonoBehaviour
{
    [Header("Ingredient Info")]
    public string ingredientName = "Tomate";
    public IngredientType ingredientType;

    [Header("State")]
    public bool isChopped = false;
    public bool isCooked = false;

    [Header("Visual References")]
    public GameObject rawModel;
    public GameObject choppedModel;
    public GameObject cookedModel;

    public enum IngredientType
    {
        Tomato,
        Lettuce,
        Bread,
        Meat,       // cerdo del sandwich de chola
        Cheese,
        // Ingredientes bolivianos (agregados al final para no cambiar los valores ya guardados)
        Potato,     // papa
        Corn,       // choclo
        Bean,       // haba
        PurpleCorn, // maiz morado (api)
        Dough,      // masa de pastel
        Fruit       // fruta picada
    }

    private void Start()
    {
        UpdateVisual();
    }

    public void UpdateVisual()
    {
        if (rawModel != null) rawModel.SetActive(!isChopped && !isCooked);
        if (choppedModel != null) choppedModel.SetActive(isChopped && !isCooked);
        if (cookedModel != null) cookedModel.SetActive(isCooked);
    }

    public bool IsReady()
    {
        return isChopped;
    }

    public string GetState()
    {
        if (isCooked) return "Cocido";
        if (isChopped) return "Picado";
        return "Crudo";
    }
}
