using UnityEngine;

public class Ingredient : MonoBehaviour
{
    [Header("Ingredient Info")]
    public string ingredientName = "Tomate";
    public IngredientType ingredientType;
    [Tooltip("Para escribir 'Papa cocida' / 'Choclo cocido'")]
    public bool femenino = false;

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

    // Las estaciones llaman a estos metodos: cambian el estado, el nombre visible y el aspecto
    public void MarkCooked()
    {
        if (isCooked) return;
        isCooked = true;
        Renderer rend = GetComponent<Renderer>();
        if (rend != null) rend.material.color = rend.material.color * 0.65f + new Color(0.1f, 0.05f, 0f, 1f) * 0.35f;
        RefreshName();
        UpdateVisual();
    }

    public void MarkChopped()
    {
        if (isChopped) return;
        isChopped = true;
        transform.localScale = Vector3.Scale(transform.localScale, new Vector3(1.25f, 0.5f, 1.25f));
        RefreshName();
        UpdateVisual();
    }

    public string DisplayName
    {
        get
        {
            string name = ingredientName;
            if (isChopped) name += femenino ? " picada" : " picado";
            if (isCooked) name += femenino ? " cocida" : " cocido";
            return name;
        }
    }

    private void RefreshName()
    {
        gameObject.name = DisplayName;
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
