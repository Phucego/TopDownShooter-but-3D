using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public string enemyPoolTag = "Enemy";
    public Transform player;
    public float spawnRate = 1f; // enemies per second
    public int maxEnemies = 50;
    
    [Header("Spawn Area")]
    public float spawnDistance = 15f; // distance from player to spawn
    public LayerMask groundLayer = 1;
    
    [Header("Wave Progression")]
    public float waveTimer = 0f;
    public float spawnRateIncrease = 0.1f; // increase per minute
    public int maxEnemyIncrease = 5; // max enemy increase per minute
    
    private float timeSinceLastSpawn;
    private int currentEnemyCount;
    private float baseSpawnRate;
    private int baseMaxEnemies;
    
    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
        
        baseSpawnRate = spawnRate;
        baseMaxEnemies = maxEnemies;
        
        StartCoroutine(CountEnemies());
    }
    
    void Update()
    {
        if (ObjectPool.Instance == null || player == null) return;
        
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
        Vector3 spawnPos = GetRandomSpawnPosition();
        
        if (spawnPos != Vector3.zero)
        {
            GameObject enemy = ObjectPool.Instance.SpawnFromPool(enemyPoolTag, spawnPos, Quaternion.identity);
            if (enemy != null)
            {
                // Debug info to see what's being spawned
                Debug.Log($"Spawned: {enemy.name} with components: {string.Join(", ", System.Array.ConvertAll(enemy.GetComponents<Component>(), c => c.GetType().Name))}");
                currentEnemyCount++;
            }
        }
    }
    
    Vector3 GetRandomSpawnPosition()
    {
        for (int attempts = 0; attempts < 10; attempts++)
        {
            // Random angle around player
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 spawnPos = player.position + new Vector3(
                Mathf.Cos(angle) * spawnDistance,
                0f,
                Mathf.Sin(angle) * spawnDistance
            );
            
            // Raycast down to find ground
            RaycastHit hit;
            if (Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out hit, 10f, groundLayer))
            {
                return hit.point;
            }
        }
        
        return Vector3.zero; // Failed to find valid position
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
        GUILayout.Label($"Wave Time: {Mathf.FloorToInt(waveTimer / 60f)}:{Mathf.FloorToInt(waveTimer % 60f):00}");
        GUILayout.Label($"Spawn Rate: {spawnRate:F1}/sec");
        GUILayout.Label($"Enemies: {currentEnemyCount}/{maxEnemies}");
    }
    

}