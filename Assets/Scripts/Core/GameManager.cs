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

    private float gameTimer;
    private int currentScore;
    public bool WonByExpulsions { get; private set; }

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
        GameState previousState = CurrentState;
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);

        switch (newState)
        {
            case GameState.Playing:
                if (previousState != GameState.Paused) StartGame();
                break;
            case GameState.GameOver:
                OnGameOver?.Invoke();
                break;
        }
    }

    private void StartGame()
    {
        WonByExpulsions = false;
        gameTimer = gameDuration;
        currentScore = 0;
        OrdersDelivered = 0;
        OrdersLost = 0;
        PointsEarned = 0;
        PointsLost = 0;
        OnGameStarted?.Invoke();
        Debug.Log("Juego iniciado! Tiempo: " + gameDuration);
    }

    private void GameOver()
    {
        SetState(GameState.GameOver);
        Debug.Log("Juego terminado! Puntuacion: " + currentScore);
    }

    public void AddScore(int points)
    {
        // Las penalizaciones restan, pero el puntaje no baja de 0
        if (points >= 0) PointsEarned += points;
        else PointsLost += -points;
        currentScore = Mathf.Max(0, currentScore + points);
        Debug.Log("Puntuacion: " + currentScore);
    }

    public void DeductTime(float seconds)
    {
        if (CurrentState != GameState.Playing) return;
        gameTimer = Mathf.Max(0f, gameTimer - Mathf.Max(0f, seconds));
        if (gameTimer <= 0f) GameOver();
    }

    public void WinByExpulsions()
    {
        if (CurrentState != GameState.Playing) return;
        WonByExpulsions = true;
        SetState(GameState.GameOver);
    }

    public void RegisterOrderDelivered() => OrdersDelivered++;
    public void RegisterOrderLost() => OrdersLost++;

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
