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
    
    // Item Collection Events
    public event Action<CollectibleItemData> OnItemCollected;
    #endregion

    #region Game Data
    [Header("Game Settings")]
    [SerializeField] private int currentScore = 0;
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int enemiesKilledThisLevel = 0;
    [SerializeField] private int enemiesNeededForNextLevel = 10;
    [SerializeField] private int scorePerEnemy = 100;
    [SerializeField] private int levelBonusMultiplier = 2;
    
    [Header("Item Collection Settings")]
    [SerializeField] private int healthRestoreAmount = 25;
    [SerializeField] private int expPerCollectible = 50;
    
    // References
    private PlayerController playerInstance;
    
    // Properties
    public int CurrentScore => currentScore;
    public int CurrentLevel => currentLevel;
    public int EnemiesKilledThisLevel => enemiesKilledThisLevel;
    public int EnemiesNeededForNextLevel => enemiesNeededForNextLevel;
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
        CollectibleItem.OnItemCollected += HandleItemCollected;
        
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

    #region Item Collection Handling
    private void HandleItemCollected(CollectibleItemData itemData, Vector3 position)
    {
        if (itemData == null) return;
        
        Debug.Log($"Item collected: {itemData.itemName} (Value: {itemData.value})");
        
        switch (itemData.itemType)
        {
            case CollectibleItemData.ItemType.Coin:
                AddScore(itemData.value);
                break;
                
            case CollectibleItemData.ItemType.Experience:
                // Convert exp to score or handle experience system
                AddScore(itemData.value * 10);
                break;
                
            case CollectibleItemData.ItemType.Health:
                // Restore player health
                if (playerInstance != null)
                {
                    // Assuming your PlayerController has a Heal method
                    // playerInstance.Heal(itemData.value);
                    Debug.Log($"Player healed for {itemData.value} health");
                }
                break;
                
            case CollectibleItemData.ItemType.PowerUp:
                // Power-up functionality removed - treat as score bonus
                AddScore(itemData.value * 5);
                Debug.Log($"Power-up collected (converted to score): {itemData.itemName}");
                break;
        }
        
        // Notify other systems
        OnItemCollected?.Invoke(itemData);
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

    // Method to manually trigger item collection (for testing or other systems)
    public void CollectItem(CollectibleItemData itemData, Vector3 position)
    {
        HandleItemCollected(itemData, position);
    }
    #endregion

    void OnDestroy()
    {
        // Unsubscribe from events
        OnEnemyKilled -= HandleEnemyKilled;
        CollectibleItem.OnItemCollected -= HandleItemCollected;
    }
}