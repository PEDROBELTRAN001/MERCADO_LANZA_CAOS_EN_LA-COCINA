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
                Destroy(held);
                GameHUD.Notify("Devolviste " + ingredientName.ToLower() + " a su caja");
                return;
            }

            GameHUD.Notify("Llevas " + held.name + ": devuélvelo a su caja o tíralo en la BASURA", 3.5f);
            return;
        }

        GameObject item = GameObject.CreatePrimitive(shape);
        // Lo que se lleva en la mano no debe chocar con el jugador ni tapar la deteccion de estaciones
        Destroy(item.GetComponent<Collider>());
        item.name = ingredientName;
        item.transform.SetParent(hand, false);
        item.transform.localPosition = Vector3.zero;
        item.transform.localScale = size;

        Renderer rend = item.GetComponent<Renderer>();
        if (rend != null) rend.material.color = color;

        Ingredient ingredient = item.AddComponent<Ingredient>();
        ingredient.ingredientName = ingredientName;
        ingredient.ingredientType = ingredientType;
        ingredient.femenino = femenino;
    }
}
