// Modified EnemySpawner.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public string enemyPoolTag = "Enemy";
    public Transform player;
    public float spawnRate = 1f; // enemies per second
    public int maxEnemies = 50;
    
    [Header("Spawn Area - Camera Based")]
    public float minSpawnDistance = 15f; // minimum distance from player to spawn (outside camera view)
    public float maxSpawnDistance = 25f; // maximum distance from player to spawn
    public LayerMask groundLayer = 1;
    
    [Header("Wave Progression")]
    public float waveTimer = 0f;
    public float spawnRateIncrease = 0.1f; // increase per minute
    public int maxEnemyIncrease = 5; // max enemy increase per minute
    
    [Header("Enemy Data")]
    public List<EnemyData> availableEnemies = new List<EnemyData>();
   
    
    private float timeSinceLastSpawn;
    private int currentEnemyCount;
    private float baseSpawnRate;
    private int baseMaxEnemies;
    private Camera mainCamera;
    private float cameraHorizontalRadius;
    private float cameraVerticalRadius;
    
    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
        
        mainCamera = Camera.main;
        CalculateCameraBounds();
        
        baseSpawnRate = spawnRate;
        baseMaxEnemies = maxEnemies;
        
        StartCoroutine(CountEnemies());
    }
    
    void Update()
    {
        if (ObjectPool.Instance == null || player == null || mainCamera == null) return;
        
        // Update wave progression
        waveTimer += Time.deltaTime;
        UpdateDifficulty();
        
        // Spawn enemies
        timeSinceLastSpawn += Time.deltaTime;
        
        if (timeSinceLastSpawn >= (1f / spawnRate) && currentEnemyCount < maxEnemies)
        {
            SpawnEnemy();
            timeSinceLastSpawn = 0f;
        }
        
        // Recalculate camera bounds periodically (in case camera changes)
        if (Time.frameCount % 60 == 0) // Every 60 frames
        {
            CalculateCameraBounds();
        }
    }
    
    void CalculateCameraBounds()
    {
        if (mainCamera == null) return;
        
        // Calculate camera view bounds in world space
        float cameraHeight = 2f * mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        
        cameraHorizontalRadius = cameraWidth / 2f;
        cameraVerticalRadius = cameraHeight / 2f;
    }
    
    void UpdateDifficulty()
    {
        float minutes = waveTimer / 60f;
        
        // Increase spawn rate over time
        spawnRate = baseSpawnRate + (spawnRateIncrease * minutes);
        
        // Increase max enemies over time
        maxEnemies = baseMaxEnemies + Mathf.FloorToInt(maxEnemyIncrease * minutes);
    }
    
    void SpawnEnemy()
    {
        Vector3 spawnPos = GetSpawnPositionOutsideCameraView();
        
        if (spawnPos != Vector3.zero)
        {
            EnemyData enemyData = SelectEnemyForCurrentWave();
            if (enemyData != null)
            {
                GameObject enemy = ObjectPool.Instance.SpawnFromPool(enemyData.poolTag, spawnPos, Quaternion.identity);
                if (enemy != null)
                {
                    SetupEnemyWithData(enemy, enemyData);
                    currentEnemyCount++;
                    
                    Debug.Log($"Spawned {enemyData.enemyName} at wave time: {Mathf.FloorToInt(waveTimer)}s");
                }
            }
        }
    }
    
    EnemyData SelectEnemyForCurrentWave()
    {
        if (availableEnemies.Count == 0) return null;
        
        List<EnemyData> eligibleEnemies = new List<EnemyData>();
        List<float> weights = new List<float>();
        
        // Get current wave number (approx 1 wave per 30 seconds)
        int currentWave = Mathf.FloorToInt(waveTimer / 30f) + 1;
        
        // Filter eligible enemies and calculate weights
        foreach (EnemyData enemy in availableEnemies)
        {
            if (currentWave >= enemy.minWaveToSpawn)
            {
                // Check if we've reached max simultaneous spawns for this enemy type
                if (enemy.maxSimultaneous > 0)
                {
                    int currentCount = CountActiveEnemiesOfType(enemy.poolTag);
                    if (currentCount >= enemy.maxSimultaneous)
                    {
                        continue; // Skip this enemy type if max reached
                    }
                }
                
                eligibleEnemies.Add(enemy);
                weights.Add(enemy.spawnWeight);
            }
        }
        
        if (eligibleEnemies.Count == 0)
        {
            // Fallback to any available enemy if none are eligible
            foreach (EnemyData enemy in availableEnemies)
            {
                eligibleEnemies.Add(enemy);
                weights.Add(enemy.spawnWeight);
            }
        }
        
        if (eligibleEnemies.Count == 0) return null;
        
        // Weighted random selection
        float totalWeight = 0f;
        foreach (float weight in weights)
        {
            totalWeight += weight;
        }
        
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        for (int i = 0; i < eligibleEnemies.Count; i++)
        {
            currentWeight += weights[i];
            if (randomValue <= currentWeight)
            {
                return eligibleEnemies[i];
            }
        }
        
        return eligibleEnemies[eligibleEnemies.Count - 1]; // Fallback
    }
    
    int CountActiveEnemiesOfType(string enemyType)
    {
        int count = 0;
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        foreach (GameObject enemy in allEnemies)
        {
            if (enemy.activeInHierarchy)
            {
                EnemyController controller = enemy.GetComponent<EnemyController>();
                if (controller != null)
                {
                   
                    count++;
                }
            }
        }
        
        return count;
    }
    
    void SetupEnemyWithData(GameObject enemy, EnemyData enemyData)
    {
        EnemyController controller = enemy.GetComponent<EnemyController>();
        HealthSystem healthSystem = enemy.GetComponent<HealthSystem>();
        
        if (controller != null)
        {
            // Scale stats based on wave progression
            int currentWave = Mathf.FloorToInt(waveTimer / 30f) + 1;
            float waveMultiplier = 1f + ((currentWave - 1) * 0.1f); // 10% increase per wave
            
            controller.moveSpeed = enemyData.moveSpeed * (1f + (currentWave - 1) * enemyData.speedScaling);
            controller.attackDamage = Mathf.RoundToInt(enemyData.attackDamage * (1f + (currentWave - 1) * enemyData.damageScaling));
            controller.attackCooldown = enemyData.attackCooldown;
            controller.followDistance = enemyData.detectionRange;
            controller.attackDistance = enemyData.attackRange;
        }
        
        if (healthSystem != null)
        {
            int currentWave = Mathf.FloorToInt(waveTimer / 30f) + 1;
            int scaledHealth = Mathf.RoundToInt(enemyData.health * (1f + (currentWave - 1) * enemyData.healthScaling));
            healthSystem.SetMaxHealth(scaledHealth, true);
        }
        
        // Apply visual/audio settings
        if (enemyData.enemyMaterial != null)
        {
            Renderer renderer = enemy.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = enemyData.enemyMaterial;
            }
        }
        
        // Play spawn sound if available
        if (enemyData.spawnSound != null)
        {
            AudioSource audioSource = enemy.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(enemyData.spawnSound);
            }
        }
    }
    
    Vector3 GetSpawnPositionOutsideCameraView()
    {
        for (int attempts = 0; attempts < 15; attempts++)
        {
            // Get random point outside camera view but within spawn distance
            Vector3 spawnPos = GetRandomPointOutsideCamera();
            
            // Validate the position
            if (IsValidSpawnPosition(spawnPos))
            {
                return spawnPos;
            }
        }
        
        return Vector3.zero; // Failed to find valid position
    }
    
    Vector3 GetRandomPointOutsideCamera()
    {
        if (mainCamera == null || player == null) return Vector3.zero;
        
        // Get camera bounds in world space
        Vector3 cameraCenter = mainCamera.transform.position;
        cameraCenter.y = player.position.y; // Use player's Y level
        
        // Random angle around player
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        
        // Random distance between min and max spawn distance
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        
        Vector3 spawnPos = player.position + new Vector3(
            Mathf.Cos(angle) * distance,
            0f,
            Mathf.Sin(angle) * distance
        );
        
        // Ensure it's outside camera view with some padding
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(spawnPos);
        int safetyCounter = 0;
        
        while ((viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1) && safetyCounter < 10)
        {
            // Move further away if inside camera view
            distance += 5f;
            spawnPos = player.position + new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance
            );
            viewportPos = mainCamera.WorldToViewportPoint(spawnPos);
            safetyCounter++;
        }
        
        return spawnPos;
    }
    
    bool IsValidSpawnPosition(Vector3 position)
    {
        // Check if position is on ground
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 5f, Vector3.down, out hit, 10f, groundLayer))
        {
            // Check if the position is not too close to other enemies
            Collider[] nearbyColliders = Physics.OverlapSphere(hit.point, 2f);
            foreach (Collider col in nearbyColliders)
            {
                if (col.CompareTag("Enemy") || col.CompareTag("Player"))
                {
                    return false; // Too close to another enemy or player
                }
            }
            
            return true;
        }
        
        return false;
    }
    
    // Count active enemies every second
    IEnumerator CountEnemies()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            
            // Simple way to count active enemies with this pool tag
            GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
            currentEnemyCount = 0;
            
            foreach (GameObject enemy in allEnemies)
            {
                if (enemy.activeInHierarchy)
                {
                    currentEnemyCount++;
                }
            }
        }
    }
    
    // Debug info
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Label($"Wave Time: {Mathf.FloorToInt(waveTimer / 60f)}:{Mathf.FloorToInt(waveTimer % 60f):00}");
        GUILayout.Label($"Current Wave: {Mathf.FloorToInt(waveTimer / 30f) + 1}");
        GUILayout.Label($"Spawn Rate: {spawnRate:F1}/sec");
        GUILayout.Label($"Enemies: {currentEnemyCount}/{maxEnemies}");
        GUILayout.Label($"Available Enemy Types: {availableEnemies.Count}");
        GUILayout.EndArea();
    }
}