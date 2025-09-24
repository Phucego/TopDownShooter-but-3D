using UnityEngine;

[System.Serializable]
public class HealthSettings
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public bool invulnerable = false;
    public bool canHealAboveMax = false;
    public float invulnerabilityTime = 0.5f;
    
    [Header("Damage Popup Settings")]
    public bool showDamageNumbers = true;
    public float popupYOffset = 2f;
    
    [Header("Audio Feedback")]
    public AudioClip damageSound;
    public AudioClip healSound;
    public AudioClip deathSound;
    
    [Header("Death Effect")]
    public GameObject deathEffectPrefab;
}