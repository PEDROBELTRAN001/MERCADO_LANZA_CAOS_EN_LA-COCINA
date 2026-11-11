using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { MainMenu, Ready, Playing, Paused, GameOver }
    public GameState CurrentState { get; private set; }

    public event Action<GameState> OnGameStateChanged;
    public event Action OnGameStarted;
    public event Action OnGameOver;

    [Header("Game Settings")]
    public float gameDuration = 180f;
    public int targetScore = 100;
    public float requiredReputation = 3.5f;
    public int requiredVipOrders = 2;
    public float Reputation { get; private set; } = 5f;
    public int VipOrdersDelivered { get; private set; }
    public int ContaminatedVipOrders { get; private set; }
    public bool WorkersWon { get; private set; }
    public string ResultReason { get; private set; } = "";

    private float gameTimer;
    private int currentScore;

    // Resumen de la partida (se muestra en la pantalla final)
    public int OrdersDelivered { get; private set; }
    public int OrdersLost { get; private set; }
    public int PointsEarned { get; private set; }
    public int PointsLost { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetState(GameState.Ready);
    }

    private void Update()
    {
        if (CurrentState != GameState.Playing) return;

        gameTimer -= Time.deltaTime;
        if (gameTimer <= 0f)
        {
            gameTimer = 0f;
            GameOver();
        }
    }

    public void SetState(GameState newState)
    {
        if (CurrentState == newState) return;
        bool resuming = CurrentState == GameState.Paused && newState == GameState.Playing;
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);

        switch (newState)
        {
            case GameState.Playing:
                if (!resuming) StartGame();
                break;
            case GameState.GameOver:
                OnGameOver?.Invoke();
                break;
        }
    }

    private void StartGame()
    {
        gameTimer = gameDuration;
        currentScore = 0;
        OrdersDelivered = 0;
        OrdersLost = 0;
        PointsEarned = 0;
        PointsLost = 0;
        Reputation = 5f;
        VipOrdersDelivered = 0;
        ContaminatedVipOrders = 0;
        WorkersWon = false;
        ResultReason = "";
        OnGameStarted?.Invoke();
        Debug.Log("Juego iniciado! Tiempo: " + gameDuration);
    }

    private void GameOver()
    {
        bool won = Reputation >= requiredReputation && VipOrdersDelivered >= requiredVipOrders && currentScore >= targetScore;
        FinishRound(won, won ? "Jornada completada: reputación, pedidos VIP y cuota de ventas cumplidos."
            : "Terminó el turno sin cumplir todas las metas del puesto.");
        Debug.Log("Juego terminado! Puntuacion: " + currentScore);
    }

    private void FinishRound(bool workersWon, string reason)
    {
        if (CurrentState != GameState.Playing) return;
        WorkersWon = workersWon;
        ResultReason = reason;
        SetState(GameState.GameOver);
    }

    private void CheckFailure()
    {
        if (Reputation <= 0f) FinishRound(false, "El puesto perdió toda su reputación.");
        else if (OrdersLost >= 3) FinishRound(false, "Tres clientes se fueron sin atención.");
        else if (ContaminatedVipOrders >= 2) FinishRound(false, "Clausura: dos platos contaminados entregados a la Cholita VIP.");
    }

    public void RegisterVipDelivery(bool contaminated)
    {
        if (CurrentState != GameState.Playing) return;
        if (contaminated) ContaminatedVipOrders++;
        else VipOrdersDelivered++;
        CheckFailure();
    }

    public void RegisterContaminatedDelivery(bool vip)
    {
        if (CurrentState != GameState.Playing) return;
        AddScore(-50);
        Reputation = Mathf.Max(0f, Reputation - 1.5f);
        if (vip) ContaminatedVipOrders++;
        CheckFailure();
    }

    public void AddScore(int points)
    {
        if (CurrentState != GameState.Playing) return;
        // Las penalizaciones restan, pero el puntaje no baja de 0
        if (points >= 0) PointsEarned += points;
        else PointsLost += -points;
        currentScore = Mathf.Max(0, currentScore + points);
        Debug.Log("Puntuacion: " + currentScore);
    }

    public void RegisterOrderDelivered()
    {
        if (CurrentState != GameState.Playing) return;
        OrdersDelivered++;
        Reputation = Mathf.Min(5f, Reputation + 0.25f);
    }
    public void RegisterOrderLost()
    {
        if (CurrentState != GameState.Playing) return;
        OrdersLost++;
        Reputation = Mathf.Max(0f, Reputation - 0.5f);
        CheckFailure();
    }

    public float GetTimeRemaining()
    {
        return gameTimer;
    }

    public int GetCurrentScore()
    {
        return currentScore;
    }

    public void TogglePause()
    {
        if (CurrentState == GameState.Playing)
        {
            SetState(GameState.Paused);
            Time.timeScale = 0f;
        }
        else if (CurrentState == GameState.Paused)
        {
            SetState(GameState.Playing);
            Time.timeScale = 1f;
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SetState(GameState.Ready);
        SetState(GameState.Playing);
    }
}
