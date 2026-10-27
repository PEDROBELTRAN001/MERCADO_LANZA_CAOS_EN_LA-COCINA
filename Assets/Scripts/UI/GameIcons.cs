using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Iconos 2D del juego (Assets/Resources/Icons): ingredientes, preparacion (picar/cocinar) y platos.
// El archivo de cada icono se llama como el tipo de ingrediente ("Potato") o el asset de la receta ("PlatoPaceno").
public static class GameIcons
{
    private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

    public static Texture2D Get(string name)
    {
        if (!cache.TryGetValue(name, out Texture2D icon) || icon == null)
        {
            icon = Resources.Load<Texture2D>("Icons/" + name);
            cache[name] = icon;
        }
        return icon;
    }

    public static Texture2D Ingredient(Ingredient.IngredientType type) => Get(type.ToString());

    public static Texture2D Dish(RecipeData recipe) => recipe != null ? Get(recipe.name) : null;

    public static Texture2D Prep(RecipeData.Prep prep)
    {
        switch (prep)
        {
            case RecipeData.Prep.Chop: return Get("Prep_Chop");
            case RecipeData.Prep.Cook: return Get("Prep_Cook");
            default: return null;
        }
    }

    // Material URP transparente (para iconos en el mundo y efectos)
    public static void MakeTransparent(Material m)
    {
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
        if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
    }
}
