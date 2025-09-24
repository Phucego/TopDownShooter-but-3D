using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Enemy/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Basic Info")]
    public string enemyName;
    public GameObject prefab;
    public string poolTag;
    
    [Header("Stats")]
    public float health = 100f;
    public float moveSpeed = 3f;
    public float attackDamage = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    
    [Header("Spawning")]
    public float spawnWeight = 1f; // Higher weight = more likely to spawn
    public int minWaveToSpawn = 1; // Earliest wave this enemy can appear
    public int maxSimultaneous = -1; // -1 for unlimited
    
    [Header("Behavior")]
    public EnemyBehaviorType behaviorType = EnemyBehaviorType.ChasePlayer;
    public float detectionRange = 10f;
    public bool canFly = false;
    public float knockbackResistance = 0f; // 0 = full knockback, 1 = immune
    
    [Header("Rewards")]
    public int experienceReward = 10;
    public float coinDropChance = 0.3f;
    public int coinAmount = 1;
    
    [Header("Visual/Audio")]
    public Material enemyMaterial;
    public AudioClip spawnSound;
    public AudioClip attackSound;
    public AudioClip deathSound;
    public GameObject deathEffect;
    
    [Header("Scaling Per Wave")]
    public float healthScaling = 0.1f; // +10% health per wave
    public float damageScaling = 0.05f; // +5% damage per wave
    public float speedScaling = 0.02f; // +2% speed per wave
}

public enum EnemyBehaviorType
{
    ChasePlayer,
    Patrol,
    Ambush,
    Ranged,
    Tank,
    Swarm
}