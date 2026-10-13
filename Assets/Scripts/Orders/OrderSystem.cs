using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OrderSystem : MonoBehaviour
{
    public static OrderSystem Instance { get; private set; }

    [Header("Order Settings")]
    public float orderInterval = 20f;
    [Tooltip("Tiempo por pedido si la receta no define el suyo")]
    public float orderTimeLimit = 45f;
    [Tooltip("Segundos hasta el primer pedido al empezar la partida")]
    public float firstOrderDelay = 5f;
    public int maxOrders = 5;

    [Header("Penalizacion")]
    [Tooltip("Puntos que se pierden cuando un pedido vence (el cliente se va sin comer)")]
    public int expiredPenalty = 10;

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

        // El ciclo de pedidos arranca cada vez que empieza (o se reinicia) una partida
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStarted += BeginOrders;
            if (GameManager.Instance.CurrentState == GameManager.GameState.Playing) BeginOrders();
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted -= BeginOrders;
    }

    private void BeginOrders()
    {
        StopAllCoroutines();
        foreach (Order order in activeOrders)
            if (order.table != null) order.table.ClearOrder();
        activeOrders.Clear();
        StartCoroutine(OrderCycle());
    }

    private void Update()
    {
        // Los tiempos de los pedidos bajan cada frame (antes solo bajaban una vez cada orderInterval)
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            UpdateOrders();
    }

    private IEnumerator OrderCycle()
    {
        yield return new WaitForSeconds(firstOrderDelay);

        while (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            if (activeOrders.Count < maxOrders)
            {
                GenerateNewOrder();
            }

            yield return new WaitForSeconds(orderInterval);
        }
    }

    private void GenerateNewOrder()
    {
        if (availableRecipes.Length == 0) return;

        RecipeData recipe = availableRecipes[Random.Range(0, availableRecipes.Length)];
        // Cada plato tiene su tiempo (un Plato Paceño lleva mas pasos que una ensalada)
        float timeLimit = recipe.timeLimit > 0f ? recipe.timeLimit : orderTimeLimit;
        Order order = new Order(recipe.recipeName, recipe, timeLimit);

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
                Order expired = activeOrders[i];
                string table = expired.table != null ? expired.table.DisplayName : "Una mesa";
                if (expired.table != null) expired.table.ClearOrder();

                // Penalizacion: el cliente se fue sin comer
                GameManager.Instance.AddScore(-expiredPenalty);
                GameManager.Instance.RegisterOrderLost();
                GameHUD.Warn(table + " se fue sin comer (" + expired.dishName + "): -" + expiredPenalty + " puntos");

                OnOrderExpired?.Invoke(expired);
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
            GameManager.Instance.RegisterOrderDelivered();
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
