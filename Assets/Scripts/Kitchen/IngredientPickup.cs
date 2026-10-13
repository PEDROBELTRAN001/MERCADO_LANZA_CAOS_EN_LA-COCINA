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
        Transform hand = player.transform.Find("Hand");
        if (hand == null)
        {
            GameObject handObj = new GameObject("Hand");
            handObj.transform.SetParent(player.transform);
            handObj.transform.localPosition = Vector3.up * 1.2f;
            hand = handObj.transform;
        }

        transform.SetParent(hand);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        Debug.Log("Ingrediente recogido: " + ingredientName);
    }
}
