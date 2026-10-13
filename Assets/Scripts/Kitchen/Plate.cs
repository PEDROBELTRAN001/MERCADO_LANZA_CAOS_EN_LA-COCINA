using System.Collections.Generic;
using UnityEngine;

// Plato limpio (sale del lavaplatos). En la mesa de armado se le ponen los ingredientes encima.
// La raiz no tiene escala: los ingredientes se emparentan aqui sin deformarse.
public class Plate : MonoBehaviour
{
    [Header("Plate State")]
    public bool isDirty = false;

    [Header("Contents")]
    public List<Ingredient> ingredients = new List<Ingredient>();

    // Diametro parecido al de los platos de Blender en la escena (~0.65 m)
    public const float Diameter = 0.65f;

    public static Plate Create()
    {
        GameObject root = new GameObject("Plato limpio");
        Plate plate = root.AddComponent<Plate>();

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(visual.GetComponent<Collider>());
        visual.name = "Loza";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = new Vector3(0f, 0.015f, 0f);
        visual.transform.localScale = new Vector3(Diameter, 0.015f, Diameter);
        Renderer rend = visual.GetComponent<Renderer>();
        if (rend != null) rend.material.color = new Color(0.93f, 0.94f, 0.96f);

        return plate;
    }

    public bool HasCompleteDish()
    {
        return ingredients.Count > 0;
    }

    public bool Contains(Ingredient.IngredientType type)
    {
        foreach (Ingredient i in ingredients)
            if (i != null && i.ingredientType == type) return true;
        return false;
    }

    public void AddIngredient(Ingredient ingredient)
    {
        ingredients.Add(ingredient);
    }

    public void ClearPlate()
    {
        ingredients.Clear();
        isDirty = true;
    }
}
