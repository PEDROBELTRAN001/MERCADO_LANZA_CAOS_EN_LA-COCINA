using UnityEngine;

public class IngredientPickup : Interactable
{
    [Header("Ingredient Settings")]
    public string ingredientName = "Tomate";
    public IngredientType ingredientType;

    public enum IngredientType
    {
        Tomato,
        Lettuce,
        Bread,
        Meat,
        Cheese,
        // Mismo orden que Ingredient.IngredientType
        Potato,
        Corn,
        Bean,
        PurpleCorn,
        Dough,
        Fruit
    }

    public override void Interact(PlayerController player)
    {
        base.Interact(player);

        PickUp(player);
    }

    private void PickUp(PlayerController player)
    {
        if (!player.TryHoldItem(gameObject))
        {
            GameHUD.Notify(player.playerName + ": necesitas la mano vacía.");
            return;
        }

        Debug.Log("Ingrediente recogido: " + ingredientName);
    }
}
