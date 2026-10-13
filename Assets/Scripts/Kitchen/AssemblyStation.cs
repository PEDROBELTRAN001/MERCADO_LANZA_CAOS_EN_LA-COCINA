using System.Collections.Generic;
using UnityEngine;

// Mesa de armado:
// 1) se deja un plato limpio (sale del lavaplatos),
// 2) se agregan los ingredientes; cada uno va a su lugar en el plato, imitando la presentacion
//    de los modelos de Blender (Plato Paceño, Sandwich de Chola, Api con Pastel, Ensalada de Frutas),
// 3) cuando coinciden con una receta (tipos y estado picado/cocido) se convierten en el plato terminado.
// Con la mano vacia se recoge: el plato listo, o el ultimo ingrediente, o el plato vacio.
public class AssemblyStation : Interactable
{
    private Plate plate;
    private GameObject readyDish;
    private RecipeData[] recipes;

    // Lugar de cada ingrediente sobre el plato (metros, respecto al centro del plato)
    private static readonly Dictionary<Ingredient.IngredientType, Vector3> Layout = new Dictionary<Ingredient.IngredientType, Vector3>
    {
        // Plato Paceño: choclo a un lado, papa al frente, queso atras, habas al costado
        { Ingredient.IngredientType.Corn,       new Vector3(-0.18f, 0.10f,  0.02f) },
        { Ingredient.IngredientType.Potato,     new Vector3( 0.02f, 0.13f, -0.14f) },
        { Ingredient.IngredientType.Cheese,     new Vector3( 0.06f, 0.08f,  0.14f) },
        { Ingredient.IngredientType.Bean,       new Vector3( 0.20f, 0.10f, -0.02f) },
        // Sandwich de Chola: pan abajo y el cerdo encima
        { Ingredient.IngredientType.Bread,      new Vector3( 0f,    0.12f,  0f) },
        { Ingredient.IngredientType.Meat,       new Vector3( 0f,    0.26f,  0f) },
        // Api con Pastel: el api (vaso) a un lado, el pastel al otro
        { Ingredient.IngredientType.PurpleCorn, new Vector3(-0.15f, 0.17f,  0f) },
        { Ingredient.IngredientType.Dough,      new Vector3( 0.12f, 0.12f,  0.04f) },
        // Ensalada de Frutas: al centro
        { Ingredient.IngredientType.Fruit,      new Vector3( 0f,    0.09f,  0f) },
    };

    private static readonly Dictionary<Ingredient.IngredientType, string> Names = new Dictionary<Ingredient.IngredientType, string>
    {
        { Ingredient.IngredientType.Potato, "papa" }, { Ingredient.IngredientType.Corn, "choclo" },
        { Ingredient.IngredientType.Bean, "haba" }, { Ingredient.IngredientType.Cheese, "queso" },
        { Ingredient.IngredientType.Bread, "pan" }, { Ingredient.IngredientType.Meat, "cerdo" },
        { Ingredient.IngredientType.PurpleCorn, "maíz morado" }, { Ingredient.IngredientType.Dough, "masa" },
        { Ingredient.IngredientType.Fruit, "fruta" },
    };

    private void Start()
    {
        recipes = Resources.LoadAll<RecipeData>("Recipes");
    }

    public override void Interact(PlayerController player)
    {
        base.Interact(player);

        Transform hand = player.transform.Find("Hand");
        if (hand == null) return;
        GameObject held = hand.childCount > 0 ? hand.GetChild(0).gameObject : null;

        if (held == null)
        {
            TakeBack(hand);
            return;
        }

        if (readyDish != null)
        {
            GameHUD.Notify("Primero lleva el plato que está listo (" + readyDish.name + ")");
            return;
        }

        Plate heldPlate = held.GetComponent<Plate>();
        if (heldPlate != null)
        {
            PlacePlate(heldPlate);
            return;
        }

        Ingredient ingredient = held.GetComponent<Ingredient>();
        if (ingredient != null)
        {
            AddIngredient(ingredient);
            return;
        }

        GameHUD.Notify("En la mesa de armado se deja un plato limpio y luego los ingredientes");
    }

    private void PlacePlate(Plate heldPlate)
    {
        if (plate != null)
        {
            GameHUD.Notify("Ya hay un plato en la mesa de armado");
            return;
        }

        plate = heldPlate;
        Transform t = plate.transform;
        t.SetParent(null);
        t.position = TopOfTable();
        t.rotation = Quaternion.identity;
        GameHUD.Notify("Plato listo para armar: trae los ingredientes del pedido");
    }

    private void AddIngredient(Ingredient ingredient)
    {
        if (plate == null)
        {
            GameHUD.Notify("Primero trae un plato limpio del LAVAPLATOS");
            return;
        }
        if (plate.Contains(ingredient.ingredientType))
        {
            GameHUD.Notify("El plato ya tiene " + NameOf(ingredient.ingredientType));
            return;
        }

        List<Ingredient.IngredientType> types = TypesOnPlate();
        types.Add(ingredient.ingredientType);
        if (CandidateRecipe(types) == null)
        {
            GameHUD.Notify(Capitalize(NameOf(ingredient.ingredientType)) + " no va en ningún plato con " + ListNames(TypesOnPlate()) +
                           ". Devuélvelo a su caja o a la BASURA", 4f);
            return;
        }

        plate.AddIngredient(ingredient);
        Transform t = ingredient.transform;
        t.SetParent(plate.transform, false);
        t.localPosition = Layout.TryGetValue(ingredient.ingredientType, out Vector3 spot) ? spot : new Vector3(0f, 0.1f, 0f);
        t.localRotation = Quaternion.identity;

        TryCompleteDish();
    }

    private void TakeBack(Transform hand)
    {
        Transform item = null;
        if (readyDish != null)
        {
            item = readyDish.transform;
            readyDish = null;
        }
        else if (plate != null && plate.ingredients.Count > 0)
        {
            Ingredient last = plate.ingredients[plate.ingredients.Count - 1];
            plate.ingredients.RemoveAt(plate.ingredients.Count - 1);
            item = last.transform;
        }
        else if (plate != null)
        {
            item = plate.transform;
            plate = null;
        }

        if (item == null)
        {
            GameHUD.Notify("Trae un plato limpio del LAVAPLATOS y déjalo aquí");
            return;
        }

        item.SetParent(hand, false);
        item.localPosition = Vector3.zero;
        item.localRotation = Quaternion.identity;
    }

    private void TryCompleteDish()
    {
        Ingredient[] current = plate.ingredients.ToArray();
        foreach (RecipeData recipe in recipes)
        {
            if (!recipe.CheckRecipe(current)) continue;

            Destroy(plate.gameObject); // el plato y sus ingredientes se reemplazan por el modelo terminado
            plate = null;
            readyDish = CreateDish(recipe);
            GameHUD.Notify("¡" + recipe.recipeName + " listo! Llévalo a la mesa que lo pidió", 3.5f);
            return;
        }

        GameHUD.Notify(MissingMessage(), 4f);
    }

    // "Para Plato Paceño falta: haba, queso · cocinar: papa"
    private string MissingMessage()
    {
        RecipeData recipe = CandidateRecipe(TypesOnPlate());
        if (recipe == null) return "En el plato: " + ListNames(TypesOnPlate());

        var missing = new List<Ingredient.IngredientType>();
        foreach (Ingredient.IngredientType type in recipe.requiredIngredients)
            if (!plate.Contains(type)) missing.Add(type);

        var notReady = new List<string>();
        foreach (Ingredient i in plate.ingredients)
        {
            if (recipe.requiresCooking && !i.isCooked) notReady.Add("cocinar " + NameOf(i.ingredientType));
            if (recipe.requiresChopping && !i.isChopped) notReady.Add("picar " + NameOf(i.ingredientType));
        }

        string message = "Para " + recipe.recipeName;
        if (missing.Count > 0) message += " falta: " + ListNames(missing);
        if (notReady.Count > 0) message += (missing.Count > 0 ? " · " : ": ") + string.Join(", ", notReady);
        return message;
    }

    // Receta que contiene todos los tipos dados (la que menos ingredientes le falten)
    private RecipeData CandidateRecipe(List<Ingredient.IngredientType> types)
    {
        RecipeData best = null;
        foreach (RecipeData recipe in recipes)
        {
            var required = new List<Ingredient.IngredientType>(recipe.requiredIngredients);
            if (!types.TrueForAll(required.Contains)) continue;
            if (best == null || recipe.requiredIngredients.Length < best.requiredIngredients.Length) best = recipe;
        }
        return best;
    }

    private GameObject CreateDish(RecipeData recipe)
    {
        GameObject dish;
        if (recipe.dishPrefab != null)
        {
            dish = Instantiate(recipe.dishPrefab);
        }
        else
        {
            // Sin modelo: un plato simple para que el ciclo siga funcionando
            dish = Plate.Create().gameObject;
            Destroy(dish.GetComponent<Plate>());
        }
        dish.name = recipe.recipeName;
        dish.AddComponent<DishItem>().recipe = recipe;
        dish.transform.position = TopOfTable();
        dish.transform.rotation = Quaternion.identity;
        return dish;
    }

    // Encima del mueble (sin emparentar: el cubo escalado deformaria los objetos)
    private Vector3 TopOfTable()
    {
        Collider body = GetComponent<Collider>();
        float top = body != null ? body.bounds.max.y : transform.position.y;
        return new Vector3(transform.position.x, top + 0.01f, transform.position.z);
    }

    private List<Ingredient.IngredientType> TypesOnPlate()
    {
        var types = new List<Ingredient.IngredientType>();
        if (plate != null)
            foreach (Ingredient i in plate.ingredients) types.Add(i.ingredientType);
        return types;
    }

    private static string NameOf(Ingredient.IngredientType type) =>
        Names.TryGetValue(type, out string n) ? n : type.ToString();

    private static string ListNames(List<Ingredient.IngredientType> types)
    {
        var names = new List<string>();
        foreach (var t in types) names.Add(NameOf(t));
        return names.Count > 0 ? string.Join(", ", names) : "nada";
    }

    private static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
}
