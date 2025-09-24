using UnityEngine;

[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(PoolableObject))]
public class EnemyController : MonoBehaviour
{
    [Header("Enemy Settings")]
    public float followDistance = 15f;
    public float attackDistance = 2f;
    public float moveSpeed = 3f;
    public float lookAtSpeed = 5f;
    public float updateTargetInterval = 0.2f;

    [Header("Obstacle Avoidance")]
    public float avoidanceRadius = 1f;
    public float obstacleCheckDistance = 2f;
    public LayerMask obstacleLayerMask = 1;
    public int raycastCount = 8;

    [Header("Combat Settings")]
    public int attackDamage = 10;
    public float attackCooldown = 1.5f;

    [Header("Visual Feedback")]
    public GameObject deathEffectPrefab;
    public AudioClip attackSound;

    [Header("Pool Settings")]
    public float deathDelayBeforeReturn = 1f;

    // Components
    private HealthSystem healthSystem;
    private Rigidbody rb;
    private AudioSource audioSource;
    private PoolableObject poolableObject;

    // Target tracking
    private Transform player;
    private float lastTargetUpdateTime;
    private float lastAttackTime;
    private bool isDead = false;

    // Movement
    private Vector3 currentMoveDirection;
    private Vector3 avoidanceForce;


    [Header("Enemy Identification")]
    public string enemyType = "BasicEnemy";
    public EnemyData enemyData; // Reference to the data used for this enemy


    void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        poolableObject = GetComponent<PoolableObject>();

        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.linearDamping = 5f;
        }

        // Subscribe to health events
        healthSystem.OnDeath += HandleDeath;
    }

    void OnEnable()
    {
        ResetEnemyState();
    }

    void Start()
    {
        FindPlayer();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void ResetEnemyState()
    {
        isDead = false;
        currentMoveDirection = Vector3.zero;
        lastTargetUpdateTime = 0f;
        lastAttackTime = 0f;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }

        if (healthSystem != null)
        {
            healthSystem.ResetHealth();
        }

        FindPlayer();
    }

    void Update()
    {
        if (isDead || player == null) return;

        if (Time.time - lastTargetUpdateTime >= updateTargetInterval)
        {
            UpdateTarget();
            lastTargetUpdateTime = Time.time;
        }

        HandleMovement();
        HandleCombat();
    }

    void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            return;
        }

        PlayerController playerComponent = FindObjectOfType<PlayerController>();
        if (playerComponent != null)
        {
            player = playerComponent.transform;
        }
    }

    void UpdateTarget()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= followDistance && distanceToPlayer > attackDistance)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            currentMoveDirection = directionToPlayer;
        }
        else
        {
            currentMoveDirection = Vector3.zero;
        }
    }

    void HandleMovement()
    {
        if (player == null || isDead) return;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0;

        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookAtSpeed * Time.deltaTime);
        }

        if (currentMoveDirection != Vector3.zero)
        {
            avoidanceForce = CalculateAvoidanceForce();
            Vector3 finalDirection = (currentMoveDirection + avoidanceForce).normalized;

            if (rb != null)
            {
                Vector3 targetVelocity = finalDirection * moveSpeed;
                targetVelocity.y = rb.linearVelocity.y;
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.deltaTime * 5f);
            }
            else
            {
                transform.position += finalDirection * moveSpeed * Time.deltaTime;
            }
        }
        else if (rb != null)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.x = 0;
            velocity.z = 0;
            rb.linearVelocity = velocity;
        }
    }

    Vector3 CalculateAvoidanceForce()
    {
        Vector3 avoidance = Vector3.zero;

        for (int i = 0; i < raycastCount; i++)
        {
            float angle = (360f / raycastCount) * i;
            Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * transform.forward;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, rayDirection, out hit, obstacleCheckDistance, obstacleLayerMask))
            {
                Vector3 avoidDirection = transform.position - hit.point;
                avoidDirection.y = 0;
                avoidDirection.Normalize();

                float weight = 1f - (hit.distance / obstacleCheckDistance);
                avoidance += avoidDirection * weight;
            }
        }

        Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, avoidanceRadius);
        foreach (Collider enemy in nearbyEnemies)
        {
            if (enemy != GetComponent<Collider>() && enemy.CompareTag("Enemy"))
            {
                Vector3 avoidDirection = transform.position - enemy.transform.position;
                avoidDirection.y = 0;
                float distance = avoidDirection.magnitude;

                if (distance > 0 && distance < avoidanceRadius)
                {
                    avoidDirection.Normalize();
                    float weight = 1f - (distance / avoidanceRadius);
                    avoidance += avoidDirection * weight * 0.5f;
                }
            }
        }

        return avoidance.normalized;
    }

    void HandleCombat()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackDistance && Time.time - lastAttackTime >= attackCooldown)
        {
            AttackPlayer();
            lastAttackTime = Time.time;
        }
    }

    void AttackPlayer()
    {
        HealthSystem playerHealth = player.GetComponent<HealthSystem>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }

        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }

        Debug.Log($"{gameObject.name} attacked player for {attackDamage} damage!");
    }

    // Modify the HandleDeath method to include enemy data
    void HandleDeath()
    {
        if (isDead) return;

        isDead = true;

        // Stop movement and disable physics
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        currentMoveDirection = Vector3.zero;

        // Disable collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Spawn death effect - use from enemyData if available
        if (enemyData != null && enemyData.deathEffect != null)
        {
            Instantiate(enemyData.deathEffect, transform.position, Quaternion.identity);
        }
        else if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        // Play death sound from enemyData if available
        if (enemyData != null && enemyData.deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(enemyData.deathSound);
        }

        // Register kill with GameManager
        GameManager.Instance?.RegisterEnemyKill(this);

        Debug.Log($"{gameObject.name} ({enemyType}) enemy has died!");

        // Return to pool after delay
        Invoke(nameof(ReturnToPool), deathDelayBeforeReturn);
    }
    // SINGLE ReturnToPool method - removed the duplicate
    private void ReturnToPool()
    {
        if (poolableObject != null)
        {
            poolableObject.ReturnToPool();
        }
        else
        {
            // Fallback
            Destroy(gameObject);
        }
    }

    // Public methods
    public void SetTarget(Transform newTarget) => player = newTarget;
    public void SetFollowDistance(float distance) => followDistance = distance;
    public void SetAttackDistance(float distance) => attackDistance = distance;

    void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnDeath -= HandleDeath;
        }
    }

    // Gizmos code remains the same...
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, followDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, avoidanceRadius);

        Gizmos.color = Color.cyan;
        for (int i = 0; i < raycastCount; i++)
        {
            float angle = (360f / raycastCount) * i;
            Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * transform.forward;
            Gizmos.DrawRay(transform.position, rayDirection * obstacleCheckDistance);
        }

        if (player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}