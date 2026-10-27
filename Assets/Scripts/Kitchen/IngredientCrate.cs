using UnityEngine;

// Caja de insumos infinita: con la mano vacia, E entrega un ingrediente nuevo.
public class IngredientCrate : Interactable
{
    [Header("Insumo")]
    public Ingredient.IngredientType ingredientType;
    public string ingredientName = "Papa";
    public bool femenino = true;

    [Header("Aspecto del ingrediente en la mano")]
    public Color color = Color.white;
    public PrimitiveType shape = PrimitiveType.Sphere;
    public Vector3 size = new Vector3(0.25f, 0.25f, 0.25f);

    private void Start()
    {
        Transform oldExample = transform.Find("Muestra");
        if (oldExample != null) oldExample.gameObject.SetActive(false);
        GameObject example = FoodModelLibrary.CreateIngredient(ingredientType, FoodModelFactory.Preparation.Raw);
        example.name = "Muestra 3D del ingrediente";
        example.transform.SetParent(transform, true);
        Collider body = GetComponent<Collider>();
        float top = body != null ? body.bounds.max.y : transform.position.y;
        example.transform.position = new Vector3(transform.position.x, top + 0.025f, transform.position.z);
    }

    public override void Interact(PlayerController player)
    {
        base.Interact(player);

        Transform hand = player.transform.Find("Hand");
        if (hand == null) return;
        if (hand.childCount > 0)
        {
            GameObject held = hand.GetChild(0).gameObject;
            Ingredient heldIngredient = held.GetComponent<Ingredient>();

            // Si te equivocaste: el mismo insumo, sin preparar, se puede devolver a su caja
            if (heldIngredient != null && heldIngredient.ingredientType == ingredientType &&
                !heldIngredient.isCooked && !heldIngredient.isChopped)
            {
                player.ReleaseHeldItem();
                Destroy(held);
                GameHUD.Notify("Devolviste " + ingredientName.ToLower() + " a su caja");
                return;
            }

            GameHUD.Notify("Llevas " + held.name + ": devuélvelo a su caja o tíralo en la BASURA", 3.5f);
            return;
        }

        GameObject item = new GameObject(ingredientName);
        item.AddComponent<FoodVisual>().Refresh(ingredientType, false, false);

        Ingredient ingredient = item.AddComponent<Ingredient>();
        ingredient.ingredientName = ingredientName;
        ingredient.ingredientType = ingredientType;
        ingredient.femenino = femenino;
        if (!player.TryHoldItem(item)) Destroy(item);
    }
}
