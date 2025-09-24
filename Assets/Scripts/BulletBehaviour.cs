using UnityEngine;

public class BulletBehaviour : MonoBehaviour
{
    [Header("Movement Settings")]
    public float bulletSpeed = 20f;
    
    [Header("Damage Settings")]
    public int damage = 10;
    public LayerMask damageLayers;
    public float criticalHitChance = 0.1f;
    public float criticalHitMultiplier = 2f;
    
    [Header("Auto-Destruction Settings")]
    public float lifeTime = 5f;
    
    [Header("Visual Effects")]
    public GameObject hitEffect;
    public AudioClip hitSound;
    
    private float spawnTime;
    private Camera mainCamera;

    void OnEnable()
    {
        spawnTime = Time.time;
        mainCamera = Camera.main;
    }

    void Update()
    {
        MoveBullet();
        CheckAutoDestruction();
    }

    void MoveBullet()
    {
        transform.Translate(Vector3.forward * bulletSpeed * Time.deltaTime);
    }

    void CheckAutoDestruction()
    {
        if (Time.time - spawnTime > lifeTime)
        {
            ReturnToPool();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & damageLayers) != 0)
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                bool isCritical = Random.value < criticalHitChance;
                int finalDamage = isCritical ? Mathf.RoundToInt(damage * criticalHitMultiplier) : damage;
                damageable.TakeDamage(finalDamage, isCritical);
            }
            
            SpawnHitEffect();
            ReturnToPool();
        }
        else if (other.CompareTag("Environment") || other.CompareTag("Wall"))
        {
            SpawnHitEffect();
            ReturnToPool();
        }
    }

    private void SpawnHitEffect()
    {
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
        
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }
    }

    private void ReturnToPool()
    {
        // If using object pooling for bullets
        if (TryGetComponent<PoolableObject>(out var poolable))
        {
            poolable.ReturnToPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}