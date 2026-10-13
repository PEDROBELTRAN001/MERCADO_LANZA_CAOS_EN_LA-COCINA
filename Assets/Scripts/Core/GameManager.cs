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

    // Resumen de la partida (se muestra en la pantalla final)
    public int OrdersDelivered { get; private set; }
    public int OrdersLost { get; private set; }

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
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);

        switch (newState)
        {
            case GameState.Playing:
                StartGame();
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
        currentScore = Mathf.Max(0, currentScore + points);
        Debug.Log("Puntuacion: " + currentScore);
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
