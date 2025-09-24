using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class ShootingController : MonoBehaviour
{
    [Header("Bullet Settings")]
    public string bulletPoolTag;
    public Transform gunPoint;
    
    [Header("Shooting Settings")]
    public float shootingCooldown = 0.2f;
    public bool allowButtonHold = false;
    public bool isAutoShootingOn = false;
    
    [Header("Auto-Targeting Settings")]
    public float targetingRange = 15f;
    public LayerMask enemyLayerMask;
    public float targetCheckInterval = 0.3f;
    
    [Header("Visual Feedback")]
    public ParticleSystem muzzleFlash;
    public AudioClip shootSound;
    
    // Public properties for external access
    public Transform CurrentTarget { get; private set; }
    public bool HasTarget() => CurrentTarget != null;
    
    // Private variables
    private float lastShotTime;
    private AudioSource audioSource;
    private bool shootInputHeld = false;
    private float lastTargetCheckTime;
    private InputAction shootAction;
    private InputAction toggleAutoShootAction;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Initialize input actions
        shootAction = new InputAction("Shoot", InputActionType.Button, "<Mouse>/leftButton");
        shootAction.performed += OnShootPerformed;
        shootAction.canceled += OnShootCanceled;
        shootAction.Enable();
        
        // Initialize auto-shooting toggle action
        toggleAutoShootAction = new InputAction("ToggleAutoShoot", InputActionType.Button, "<Keyboard>/f");
        toggleAutoShootAction.performed += OnToggleAutoShootPerformed;
        toggleAutoShootAction.Enable();
    }

    void Update()
    {
        HandleManualShooting();
        HandleAutoShooting();
    }

    void HandleManualShooting()
    {
        if (isAutoShootingOn) return;
        
        if (allowButtonHold && shootInputHeld)
        {
            TryShoot();
        }
    }

    void HandleAutoShooting()
    {
        if (!isAutoShootingOn) return;
        
        // Check for new targets periodically
        if (Time.time - lastTargetCheckTime >= targetCheckInterval)
        {
            FindClosestEnemy();
            lastTargetCheckTime = Time.time;
        }
        
        // If we have a target and it's time to shoot
        if (CurrentTarget != null && Time.time - lastShotTime >= shootingCooldown)
        {
            TryShoot();
        }
    }

    public void FindClosestEnemy()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, targetingRange, enemyLayerMask);
        
        if (enemies.Length == 0)
        {
            CurrentTarget = null;
            return;
        }
        
        Transform closestEnemy = null;
        float closestDistance = Mathf.Infinity;
        
        foreach (Collider enemy in enemies)
        {
            // Check if enemy is alive (has HealthSystem component)
            HealthSystem health = enemy.GetComponent<HealthSystem>();
            if (health != null && !health.IsAlive) continue;
            
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }
        
        CurrentTarget = closestEnemy;
    }

    void TryShoot()
    {
        if (Time.time - lastShotTime >= shootingCooldown)
        {
            Shoot();
            lastShotTime = Time.time;
        }
    }

    void Shoot()
    {
        // Get bullet from pool
        GameObject bullet = ObjectPool.Instance.SpawnFromPool(bulletPoolTag, gunPoint.position, gunPoint.rotation);
        if (bullet == null) return;
        
        // If auto-shooting, set the bullet's direction towards the target
        if (isAutoShootingOn && CurrentTarget != null)
        {
            Vector3 direction = (CurrentTarget.position - gunPoint.position).normalized;
            bullet.transform.rotation = Quaternion.LookRotation(direction);
        }
        
        // Visual and audio feedback
        if (muzzleFlash != null) muzzleFlash.Play();
        if (shootSound != null) audioSource.PlayOneShot(shootSound);
    }

    void OnShootPerformed(InputAction.CallbackContext context)
    {
        shootInputHeld = true;
        
        if (!isAutoShootingOn && (!allowButtonHold || !shootInputHeld))
        {
            TryShoot();
        }
    }

    void OnShootCanceled(InputAction.CallbackContext context)
    {
        shootInputHeld = false;
    }

    void OnToggleAutoShootPerformed(InputAction.CallbackContext context)
    {
        isAutoShootingOn = !isAutoShootingOn;
        Debug.Log($"Auto-shooting {(isAutoShootingOn ? "enabled" : "disabled")}");
        
        // Clear target when disabling auto-shooting
        if (!isAutoShootingOn)
        {
            CurrentTarget = null;
        }
    }

    void OnEnable()
    {
        shootAction?.Enable();
        toggleAutoShootAction?.Enable();
    }

    void OnDisable()
    {
        shootAction?.Disable();
        toggleAutoShootAction?.Disable();
    }

    void OnDestroy()
    {
        shootAction?.Dispose();
        toggleAutoShootAction?.Dispose();
    }

    // Gizmos for debugging targeting range
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, targetingRange);
        
        if (CurrentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(gunPoint.position, CurrentTarget.position);
        }
    }
}