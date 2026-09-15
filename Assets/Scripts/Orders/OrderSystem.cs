using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OrderSystem : MonoBehaviour
{
    public static OrderSystem Instance { get; private set; }

    [Header("Order Settings")]
    public float orderInterval = 10f;
    public float orderTimeLimit = 30f;
    public int maxOrders = 5;

    [Header("References")]
    public FoodCourtTable[] tables;

    private List<Order> activeOrders = new List<Order>();
    private RecipeData[] availableRecipes;

    public event System.Action<Order> OnNewOrder;
    public event System.Action<Order> OnOrderCompleted;
    public event System.Action<Order> OnOrderExpired;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        availableRecipes = Resources.LoadAll<RecipeData>("Recipes");

        if (availableRecipes.Length == 0)
        {
            Debug.LogWarning("No se encontraron recetas. Crea RecipeData en Resources/Recipes/");
        }

        tables = FindObjectsByType<FoodCourtTable>();

        StartCoroutine(OrderCycle());
    }

    private IEnumerator OrderCycle()
    {
        yield return new WaitForSeconds(2f);

        while (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            if (activeOrders.Count < maxOrders)
            {
                GenerateNewOrder();
            }

            UpdateOrders();

            yield return new WaitForSeconds(orderInterval);
        }
    }

    private void GenerateNewOrder()
    {
        if (availableRecipes.Length == 0) return;

        RecipeData recipe = availableRecipes[Random.Range(0, availableRecipes.Length)];
        Order order = new Order(recipe.recipeName, recipe, orderTimeLimit);

        FoodCourtTable freeTable = GetFreeTable();
        if (freeTable != null)
        {
            freeTable.SetOrder(order);
            activeOrders.Add(order);
            OnNewOrder?.Invoke(order);
            Debug.Log("Nuevo pedido: " + order.dishName);
        }
    }

    private void UpdateOrders()
    {
        for (int i = activeOrders.Count - 1; i >= 0; i--)
        {
            activeOrders[i].timeRemaining -= Time.deltaTime;

            if (activeOrders[i].timeRemaining <= 0)
            {
                OnOrderExpired?.Invoke(activeOrders[i]);
                activeOrders.RemoveAt(i);
                Debug.Log("Pedido expirado!");
            }
        }
    }

    public void CompleteOrder(Order order)
    {
        if (activeOrders.Contains(order))
        {
            activeOrders.Remove(order);
            OnOrderCompleted?.Invoke(order);

            float timeBonus = order.timeRemaining / order.timeLimit;
            int bonusPoints = Mathf.RoundToInt(timeBonus * 25);
            GameManager.Instance.AddScore(bonusPoints);

            Debug.Log("Pedido completado: " + order.dishName + " +Bonus: " + bonusPoints);
        }
    }

    private FoodCourtTable GetFreeTable()
    {
        foreach (FoodCourtTable table in tables)
        {
            if (!table.hasOrder)
            {
                return table;
            }
        }
        return null;
    }

    public List<Order> GetActiveOrders()
    {
        return activeOrders;
    }
}
