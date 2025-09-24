using UnityEngine;
using System.Collections;

public class CollectibleItem : MonoBehaviour
{
    [SerializeField] private CollectibleItemData itemData;
    
    private Vector3 startPosition;
    private bool isCollected = false;
    private Renderer itemRenderer;
    private Collider itemCollider;
    
    // Events
    public static event System.Action<CollectibleItemData, Vector3> OnItemCollected;
    
    void Start()
    {
        InitializeItem();
    }
    
    void InitializeItem()
    {
        startPosition = transform.position;
        itemRenderer = GetComponent<Renderer>();
        itemCollider = GetComponent<Collider>();
        
        // Apply visual settings from ScriptableObject
        if (itemRenderer != null && itemData != null)
        {
            if (itemData.itemMaterial != null)
            {
                itemRenderer.material = itemData.itemMaterial;
            }
            else
            {
                itemRenderer.material.color = itemData.itemColor;
            }
        }
        
        // Scale based on item type
        ApplyTypeSpecificSettings();
    }
    
    void Update()
    {
        // Always handle rotation, even after collection
        HandleRotation();
        
        // Only handle floating and proximity checks if not collected
        if (!isCollected)
        {
            HandleFloating();
            CheckForPlayerProximity();
        }
    }
    
    void HandleRotation()
    {
        if (itemData != null)
        {
            // Rotate around the object's own axis at its current position
            transform.Rotate(itemData.rotationAxis * itemData.rotationSpeed * Time.deltaTime, Space.Self);
        }
        else
        {
            // Default rotation if no data assigned
            transform.Rotate(Vector3.up * 90f * Time.deltaTime, Space.Self);
        }
    }
    
    void HandleFloating()
    {
        if (itemData != null && itemData.enableFloating)
        {
            // Only modify the Y position for floating, keep X and Z unchanged
            float newY = startPosition.y + Mathf.Sin(Time.time * itemData.floatSpeed) * itemData.floatHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        }
    }
    
    void CheckForPlayerProximity()
    {
        if (PlayerController.Instance == null) return;
        
        float distance = Vector3.Distance(transform.position, PlayerController.Instance.transform.position);
        if (distance <= itemData.collectionRadius)
        {
            CollectItem();
        }
    }
    
    void ApplyTypeSpecificSettings()
    {
        if (itemData == null) return;
        
        switch (itemData.itemType)
        {
            case CollectibleItemData.ItemType.Coin:
                transform.localScale = Vector3.one * 0.5f;
                break;
            case CollectibleItemData.ItemType.Experience:
                transform.localScale = Vector3.one * 0.7f;
                break;
            case CollectibleItemData.ItemType.Health:
                transform.localScale = Vector3.one * 0.6f;
                break;
            case CollectibleItemData.ItemType.PowerUp:
                transform.localScale = Vector3.one * 0.8f;
                break;
        }
    }
    
    void CollectItem()
    {
        if (isCollected) return;
        
        isCollected = true;
        
        // Disable visual and collision
        if (itemRenderer != null) itemRenderer.enabled = false;
        if (itemCollider != null) itemCollider.enabled = false;
        
        // Play collection effects
        PlayCollectionEffects();
        
        // Notify GameManager and other listeners
        OnItemCollected?.Invoke(itemData, transform.position);
        
        // Fix position to current position to prevent floating
        transform.position = transform.position; // Ensures position is locked
        
        // Destroy after effects
        StartCoroutine(DestroyAfterDelay(.1f));
    }
    
    void PlayCollectionEffects()
    {
        // Play sound
        if (itemData.collectionSound != null)
        {
            AudioSource.PlayClipAtPoint(itemData.collectionSound, transform.position);
        }
        
        // Spawn particle effect
        if (itemData.collectionEffect != null)
        {
            Instantiate(itemData.collectionEffect, transform.position, Quaternion.identity);
        }
    }
    
    IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        
        if (other.CompareTag("Player"))
        {
            CollectItem();
        }
    }
    
    // Getters
    public CollectibleItemData GetItemData() => itemData;
    public bool IsCollected => isCollected;
}