using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }
    
    void Awake()
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
    #endregion

    #region Game States
    public enum GameState
    {
        Playing,
        GameOver,
        Victory,
        Paused
    }
    
    [SerializeField] private GameState currentState = GameState.Playing;
    public GameState CurrentState => currentState;
    #endregion

    #region Events
    // Game Events
    public static event Action OnGameWon;
    public static event Action OnGameLost;
    public static event Action<int> OnScoreChanged;
    public static event Action<int> OnLevelChanged;
    
    // Enemy Events
    public static event Action<EnemyController> OnEnemyKilled;
    
    // Power-up Events
    public static event Action<PowerUpType> OnPowerUpCollected;
    public static event Action<PowerUpType, float> OnPowerUpActivated;
    public static event Action<PowerUpType> OnPowerUpExpired;
    #endregion

    #region Game Data
    [Header("Game Settings")]
    [SerializeField] private int currentScore = 0;
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int enemiesKilledThisLevel = 0;
    [SerializeField] private int enemiesNeededForNextLevel = 10;
    [SerializeField] private int scorePerEnemy = 100;
    [SerializeField] private int levelBonusMultiplier = 2;
    
    
    // References
    private PlayerController playerInstance;
    
    // Properties
    public int CurrentScore => currentScore;
    public int CurrentLevel => currentLevel;
    public int EnemiesKilledThisLevel => enemiesKilledThisLevel;
    public int EnemiesNeededForNextLevel => enemiesNeededForNextLevel;
    #endregion

    #region Power-up Types
    public enum PowerUpType
    {
        SpeedBoost,
        DamageBoost,
        HealthRegeneration,
        Shield,
        RapidFire,
        DoubleScore,
        Invincibility
    }
    
    [System.Serializable]
    public class PowerUpConfig
    {
        public PowerUpType type;
        public float duration;
        public float effectValue;
        public string displayName;
        public Color effectColor;
    }
    
    [Header("Power-up Configuration")]
    [SerializeField] private PowerUpConfig[] powerUpConfigs = new PowerUpConfig[]
    {
        new PowerUpConfig { type = PowerUpType.SpeedBoost, duration = 10f, effectValue = 1.5f, displayName = "Speed Boost", effectColor = Color.blue },
        new PowerUpConfig { type = PowerUpType.DamageBoost, duration = 15f, effectValue = 2f, displayName = "Damage Boost", effectColor = Color.red },
        new PowerUpConfig { type = PowerUpType.HealthRegeneration, duration = 20f, effectValue = 5f, displayName = "Health Regen", effectColor = Color.green },
        new PowerUpConfig { type = PowerUpType.Shield, duration = 12f, effectValue = 1f, displayName = "Shield", effectColor = Color.yellow },
        new PowerUpConfig { type = PowerUpType.RapidFire, duration = 8f, effectValue = 0.5f, displayName = "Rapid Fire", effectColor = Color.orange },
        new PowerUpConfig { type = PowerUpType.DoubleScore, duration = 30f, effectValue = 2f, displayName = "Double Score", effectColor = Color.magenta },
        new PowerUpConfig { type = PowerUpType.Invincibility, duration = 5f, effectValue = 1f, displayName = "Invincibility", effectColor = Color.white }
    };
    #endregion

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        // Find player
        playerInstance = FindObjectOfType<PlayerController>();
        
        // Subscribe to events
        OnEnemyKilled += HandleEnemyKilled;
        
        Debug.Log("GameManager initialized");
    }

    #region Score Management
    public void AddScore(int points)
    {
        // Apply score multipliers from active power-ups
        float multiplier = 1f;
        
        int finalPoints = Mathf.RoundToInt(points * multiplier);
        currentScore += finalPoints;
        
        OnScoreChanged?.Invoke(currentScore);
        Debug.Log($"Score increased by {finalPoints}. Total: {currentScore}");
    }

    public void AddEnemyKillScore(EnemyController enemy)
    {
        int baseScore = scorePerEnemy;
        int levelBonus = (currentLevel - 1) * levelBonusMultiplier;
        int totalScore = baseScore + levelBonus;
        
        AddScore(totalScore);
    }
    #endregion

    #region Level Management
    void HandleEnemyKilled(EnemyController enemy)
    {
        enemiesKilledThisLevel++;
        AddEnemyKillScore(enemy);
        
        // Check for level up
        if (enemiesKilledThisLevel >= enemiesNeededForNextLevel)
        {
            LevelUp();
        }
        
        // Check win condition (example: reach level 10)
        if (currentLevel >= 10 && enemiesKilledThisLevel >= enemiesNeededForNextLevel)
        {
            TriggerVictory();
        }
    }

    void LevelUp()
    {
        currentLevel++;
        enemiesKilledThisLevel = 0;
        enemiesNeededForNextLevel = Mathf.RoundToInt(enemiesNeededForNextLevel * 1.2f); // Increase requirement
        
        // Level up bonus score
        int levelBonus = 1000 * currentLevel;
        AddScore(levelBonus);
        
        OnLevelChanged?.Invoke(currentLevel);
        Debug.Log($"Level up! Now level {currentLevel}. Next level needs {enemiesNeededForNextLevel} enemies");
    }
    #endregion

    #region Win/Lose Conditions
    public void TriggerVictory()
    {
        if (currentState != GameState.Playing) return;
        
        currentState = GameState.Victory;
        Time.timeScale = 0f;
        
        OnGameWon?.Invoke();
        Debug.Log("VICTORY! Player won the game!");
    }

    public void GameOver()
    {
        if (currentState != GameState.Playing) return;
        
        currentState = GameState.GameOver;
        Time.timeScale = 0f;
        
        OnGameLost?.Invoke();
        Debug.Log("GAME OVER! Player lost the game!");
    }

    public void RestartGame()
    {
        // Reset game state
        currentState = GameState.Playing;
        currentScore = 0;
        currentLevel = 1;
        enemiesKilledThisLevel = 0;
        enemiesNeededForNextLevel = 10;
        
        // Reset time scale
        Time.timeScale = 1f;
        
        // Notify listeners
        OnScoreChanged?.Invoke(currentScore);
        OnLevelChanged?.Invoke(currentLevel);
        
        Debug.Log("Game restarted");
    }

    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            currentState = GameState.Paused;
            Time.timeScale = 0f;
        }
    }

    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            currentState = GameState.Playing;
            Time.timeScale = 1f;
        }
    }
    #endregion

    #region Public API
    public void RegisterEnemyKill(EnemyController enemy)
    {
        OnEnemyKilled?.Invoke(enemy);
    }

   
    #endregion

    void OnDestroy()
    {
        // Unsubscribe from events
        OnEnemyKilled -= HandleEnemyKilled;
    }
}