using UnityEngine;
using System;

public interface IDamageable
{
    void TakeDamage(int damage, bool isCritical = false);
    void Heal(int amount, bool showPopup = true);
    void Die();
    bool IsAlive { get; }
    int CurrentHealth { get; }
    int MaxHealth { get; }
    event Action<int, int> OnHealthChanged;
    event Action<int> OnDamageTaken;
    event Action<int> OnHealed;
    event Action OnDeath;
}