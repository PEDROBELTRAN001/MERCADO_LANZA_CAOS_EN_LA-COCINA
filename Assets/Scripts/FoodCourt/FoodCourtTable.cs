using UnityEngine;

public class FoodCourtTable : Interactable
{
    [Header("Table Settings")]
    public Transform plateSlot;
    public bool hasOrder = false;
    public bool orderCompleted = false;

    private Order currentOrder;
    private GameObject servedDish;

    public override void Interact(PlayerController player)
    {
        base.Interact(player);

        if (hasOrder && !orderCompleted)
        {
            TryServeOrder(player);
        }
    }

    private void TryServeOrder(PlayerController player)
    {
        Transform hand = player.transform.Find("Hand");
        if (hand == null || hand.childCount == 0)
        {
            Debug.Log("No tienes nada en la mano");
            return;
        }

        GameObject item = hand.GetChild(0).gameObject;
        Plate plate = item.GetComponent<Plate>();

        if (plate != null && plate.HasCompleteDish())
        {
            servedDish = item;
            orderCompleted = true;
            hasOrder = false;

            item.transform.SetParent(plateSlot != null ? plateSlot : transform);
            item.transform.localPosition = Vector3.zero;

            Debug.Log("Pedido servido!");
            GameManager.Instance.AddScore(25);

            if (OrderSystem.Instance != null)
            {
                OrderSystem.Instance.CompleteOrder(currentOrder);
            }
        }
        else
        {
            Debug.Log("No es un plato completo");
        }
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
