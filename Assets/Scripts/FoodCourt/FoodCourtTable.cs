using UnityEngine;

public class FoodCourtTable : Interactable
{
    [Header("Table Settings")]
    public Transform plateSlot;
    public bool hasOrder = false;
    public bool orderCompleted = false;

    private Order currentOrder;
    private GameObject servedDish;

    [Tooltip("Segundos que el plato servido queda sobre la mesa antes de desaparecer")]
    public float servedDishLifetime = 4f;

    public override void Interact(PlayerController player)
    {
        base.Interact(player);

        if (hasOrder && !orderCompleted)
        {
            TryServeOrder(player);
        }
        else
        {
            GameHUD.Notify(name.Replace("_", " ") + " no tiene pedido");
        }
    }

    private void TryServeOrder(PlayerController player)
    {
        Transform hand = player.transform.Find("Hand");
        if (hand == null || hand.childCount == 0)
        {
            GameHUD.Notify(name.Replace("_", " ") + " pidió: " + currentOrder.dishName);
            return;
        }

        GameObject item = hand.GetChild(0).gameObject;
        DishItem dish = item.GetComponent<DishItem>();

        if (dish == null || dish.recipe == null)
        {
            GameHUD.Notify("Aquí se entrega el plato terminado: " + currentOrder.dishName);
            return;
        }
        if (dish.recipe.recipeName != currentOrder.dishName)
        {
            GameHUD.Notify("Esta mesa pidió " + currentOrder.dishName + ", no " + dish.recipe.recipeName);
            return;
        }

        // Plato servido: queda sobre la mesa unos segundos (sin emparentar: el cubo escalado lo deformaria)
        servedDish = item;
        Collider body = GetComponent<Collider>();
        float top = body != null ? body.bounds.max.y : transform.position.y;
        item.transform.SetParent(plateSlot);
        item.transform.position = plateSlot != null ? plateSlot.position : new Vector3(transform.position.x, top + 0.02f, transform.position.z);
        Destroy(item, servedDishLifetime);

        int points = dish.recipe.basePoints;
        GameManager.Instance.AddScore(points);
        if (OrderSystem.Instance != null)
        {
            OrderSystem.Instance.CompleteOrder(currentOrder);
        }
        GameHUD.Notify("¡Pedido entregado: " + currentOrder.dishName + "! +" + points + " puntos");
        ClearOrder();
    }

    public void SetOrder(Order order)
    {
        currentOrder = order;
        order.table = this;
        hasOrder = true;
        orderCompleted = false;
    }

    // Libera la mesa cuando el pedido vence (si no, queda ocupada para siempre)
    public void ClearOrder()
    {
        currentOrder = null;
        hasOrder = false;
        orderCompleted = false;
    }

    public Order GetCurrentOrder()
    {
        return currentOrder;
    }
}

[System.Serializable]
public class Order
{
    public string dishName;
    public RecipeData recipe;
    public float timeLimit;
    public float timeRemaining;
    public FoodCourtTable table;

    public Order(string name, RecipeData recipeData, float time)
    {
        dishName = name;
        recipe = recipeData;
        timeLimit = time;
        timeRemaining = time;
    }
}
