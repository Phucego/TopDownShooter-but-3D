using UnityEngine;
using System;
using System.Collections;

public class HealthSystem : MonoBehaviour, IDamageable
{
    [SerializeField] private HealthSettings healthSettings = new HealthSettings();
    
    [Header("References")]
    [SerializeField] private GameObject damageNumberPrefab;
    [SerializeField] private AudioSource audioSource;
    
    // Events
    public event Action<int, int> OnHealthChanged;
    public event Action<int> OnDamageTaken;
    public event Action<int> OnHealed;
    public event Action OnDeath;
    
    // Properties
    public int CurrentHealth { get; private set; }
    public int MaxHealth => healthSettings.maxHealth;
    public bool IsAlive => CurrentHealth > 0;
    public HealthSettings Settings => healthSettings;
    
    private float lastDamageTime;
    private bool isInitialized;

    void Awake()
    {
        Initialize();
    }

    void Start()
    {
        InitializeAudioSource();
    }

    private void InitializeAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    public void Initialize()
    {
        if (isInitialized) return;

        CurrentHealth = healthSettings.maxHealth;
        isInitialized = true;
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    // SINGLE TakeDamage method - removed the duplicate
    public void TakeDamage(int damage, bool isCritical = false)
    {
        if (!CanTakeDamage()) return;
        
        damage = Mathf.Max(1, damage);
        CurrentHealth -= damage;
        lastDamageTime = Time.time;
        
        CurrentHealth = Mathf.Max(0, CurrentHealth);
        
        HandleDamageEffects(damage, isCritical);
        CheckForDeath();
        
        Debug.Log($"{gameObject.name} took {damage} damage. Current health: {CurrentHealth}");
    }

    // Overload for backwards compatibility
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, false);
    }

    private bool CanTakeDamage()
    {
        return IsAlive && 
               !healthSettings.invulnerable && 
               (Time.time - lastDamageTime >= healthSettings.invulnerabilityTime);
    }

    private void HandleDamageEffects(int damage, bool isCritical)
    {
        OnDamageTaken?.Invoke(damage);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        
        PlayDamageSound();
    }

    private void PlayDamageSound()
    {
        if (healthSettings.damageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(healthSettings.damageSound);
        }
    }

    private void CheckForDeath()
    {
        if (CurrentHealth <= 0)
        {
            Debug.Log($"{gameObject.name} health reached 0! Calling Die()");
            Die();
        }
    }

    public void Heal(int amount, bool showPopup = true)
    {
        if (!IsAlive) return;
        
        amount = Mathf.Max(1, amount);
        
        if (healthSettings.canHealAboveMax)
        {
            CurrentHealth += amount;
        }
        else
        {
            CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
        }
        
        OnHealed?.Invoke(amount);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        
        if (healthSettings.healSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(healthSettings.healSound);
        }
    }

    // Overload for backwards compatibility
    public void Heal(int amount)
    {
        Heal(amount, true);
    }

    public void Die()
    {
    
        CurrentHealth = 0;
        
        Debug.Log($"{gameObject.name} Die() method called");
        
        PlayDeathSound();
        SpawnDeathEffect();
        OnDeath?.Invoke();
        
        HandleDeath();
    }

    private void PlayDeathSound()
    {
        if (healthSettings.deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(healthSettings.deathSound);
        }
    }

    private void SpawnDeathEffect()
    {
        if (healthSettings.deathEffectPrefab != null && DeathEffectPool.Instance != null)
        {
            GameObject deathEffect = DeathEffectPool.Instance.GetDeathEffect();
            deathEffect.transform.position = transform.position;
            deathEffect.transform.rotation = Quaternion.identity;
            
            ParticleSystem particles = deathEffect.GetComponent<ParticleSystem>();
            if (particles != null)
            {
                StartCoroutine(ReturnEffectToPool(deathEffect, particles.main.duration));
            }
            else
            {
                StartCoroutine(ReturnEffectToPool(deathEffect, 2f));
            }
        }
    }

    private IEnumerator ReturnEffectToPool(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        DeathEffectPool.Instance.ReturnDeathEffect(effect);
    }

    protected virtual void HandleDeath()
    {
        Debug.Log($"{gameObject.name} HandleDeath() called");
        
        PoolableObject poolableObject = GetComponent<PoolableObject>();
        if (poolableObject != null)
        {
            Debug.Log("Returning to pool via PoolableObject");
            poolableObject.ReturnToPool();
        }
        else
        {
            Debug.Log("No PoolableObject found, destroying normally");
            if (gameObject.CompareTag("Enemy"))
            {
                Destroy(gameObject, 0.1f);
            }
        }
    }

    public void SetInvulnerable(bool invulnerable)
    {
        healthSettings.invulnerable = invulnerable;
    }

    public void SetMaxHealth(int newMaxHealth, bool healToMax = false)
    {
        healthSettings.maxHealth = newMaxHealth;
        if (healToMax)
        {
            CurrentHealth = newMaxHealth;
        }
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void ResetHealth()
    {
        CurrentHealth = MaxHealth;
        lastDamageTime = 0f;
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public float GetHealthPercentage()
    {
        return (float)CurrentHealth / MaxHealth;
    }

    public bool IsFullHealth()
    {
        return CurrentHealth >= MaxHealth;
    }
}