#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Ejecutar en una copia aislada: -batchmode -executeMethod RoundRulesValidation.Run.
public static class RoundRulesValidation
{
    private static int checks;
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
        Debug.Log("PASS: " + message);
    }
    private static void Begin(GameManager game)
    {
        game.SetState(GameManager.GameState.Ready);
        game.SetState(GameManager.GameState.Playing);
    }
    private static void Expire(GameManager game)
    {
        typeof(GameManager).GetField("gameTimer", Private).SetValue(game, 0f);
        typeof(GameManager).GetMethod("Update", Private).Invoke(game, null);
    }
    public static void Run()
    {
        GameObject root = null;
        try
        {
            root = new GameObject("Round rules validation");
            var game = root.AddComponent<GameManager>();
            int finishes = 0, starts = 0;
            game.OnGameOver += () => finishes++;
            game.OnGameStarted += () => starts++;

            Begin(game);
            Check(game.Reputation == 5f && game.VipOrdersDelivered == 0, "Round starts at five stars with no VIP deliveries");
            game.AddScore(100);
            game.RegisterVipDelivery(false);
            game.RegisterVipDelivery(false);
            Expire(game);
            Check(game.WorkersWon && game.CurrentState == GameManager.GameState.GameOver, "All three goals grant worker victory");
            Check(finishes == 1, "Finishing publishes exactly one result");
            game.RegisterOrderLost();
            game.AddScore(-100);
            game.RegisterContaminatedDelivery(true);
            Check(game.WorkersWon && game.GetCurrentScore() == 100 && game.OrdersLost == 0, "Result and score remain frozen after the round");

            Begin(game);
            game.AddScore(100);
            Expire(game);
            Check(!game.WorkersWon, "Sales alone cannot win without VIP orders");

            Begin(game);
            game.RegisterVipDelivery(false);
            game.RegisterVipDelivery(false);
            game.AddScore(99);
            Expire(game);
            Check(!game.WorkersWon, "Two VIP orders cannot win below the sales quota");

            Begin(game);
            game.RegisterVipDelivery(false);
            game.RegisterVipDelivery(false);
            game.RegisterContaminatedDelivery(false);
            game.AddScore(100);
            Expire(game);
            Check(game.Reputation == 3.5f && game.WorkersWon, "Exactly 3.5 stars satisfies the reputation goal");

            Begin(game);
            game.RegisterVipDelivery(false);
            game.RegisterVipDelivery(false);
            game.RegisterContaminatedDelivery(false);
            game.RegisterOrderLost();
            game.AddScore(100);
            Expire(game);
            Check(game.Reputation == 3f && !game.WorkersWon, "Sales and VIP orders cannot compensate for low reputation");

            Begin(game);
            game.RegisterOrderLost();
            game.RegisterOrderLost();
            Check(game.CurrentState == GameManager.GameState.Playing, "Two lost clients do not end the round");
            game.RegisterOrderLost();
            Check(game.CurrentState == GameManager.GameState.GameOver && !game.WorkersWon && game.ResultReason.Contains("Tres"), "Third lost client immediately awards the impostor victory");

            Begin(game);
            game.RegisterContaminatedDelivery(true);
            Check(game.VipOrdersDelivered == 0 && game.ContaminatedVipOrders == 1 && game.CurrentState == GameManager.GameState.Playing, "Contaminated VIP delivery never counts as successful");
            game.RegisterContaminatedDelivery(true);
            Check(game.CurrentState == GameManager.GameState.GameOver && game.ResultReason.Contains("Clausura"), "Second contaminated VIP delivery closes the kitchen immediately");

            Begin(game);
            for (int i = 0; i < 4; i++) game.RegisterContaminatedDelivery(false);
            Check(game.Reputation == 0f && game.CurrentState == GameManager.GameState.GameOver && game.ContaminatedVipOrders == 0, "Non-VIP contamination can exhaust reputation without counting as VIP contamination");

            Begin(game);
            game.AddScore(70);
            game.RegisterOrderDelivered();
            game.RegisterVipDelivery(false);
            int beforePause = starts;
            float time = game.GetTimeRemaining();
            game.TogglePause();
            game.TogglePause();
            Check(starts == beforePause && game.GetCurrentScore() == 70 && game.VipOrdersDelivered == 1 && game.GetTimeRemaining() == time, "Pause and resume preserve progress and do not restart systems");
            game.RestartGame();
            Check(game.Reputation == 5 && game.GetCurrentScore() == 0 && game.VipOrdersDelivered == 0 && game.OrdersLost == 0 && game.ContaminatedVipOrders == 0 && game.ResultReason == "", "Restart clears all goals, penalties and outcome");
            Debug.Log("ROUND RULES VALIDATION PASSED: " + checks + " checks");
            UnityEngine.Object.DestroyImmediate(root);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            EditorApplication.Exit(1);
        }
    }
}
#endif
