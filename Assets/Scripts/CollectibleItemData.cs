// CollectibleItemData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New Collectible Item", menuName = "Game/Collectible Item Data")]
public class CollectibleItemData : ScriptableObject
{
    public enum ItemType
    {
        Coin,
        Experience,
        Health,
        PowerUp
    }

    [Header("Item Identity")]
    public ItemType itemType;
    public string itemName;
    public int value; // Amount of coins/exp/health etc.

    [Header("Visual Settings")]
    public Material itemMaterial;
    public Color itemColor = Color.white;
    
    [Header("Rotation Settings")]
    public float rotationSpeed = 90f;
    public Vector3 rotationAxis = Vector3.up;
    
    [Header("Float Settings")]
    public bool enableFloating = true;
    public float floatHeight = 0.5f;
    public float floatSpeed = 1f;
    
    [Header("Collection Settings")]
    public float collectionRadius = 1.5f;
    public AudioClip collectionSound;
    public GameObject collectionEffect;
}