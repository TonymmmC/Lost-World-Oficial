using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;

    public int Current { get; private set; }
    public int Max => maxHealth;
    public bool IsDead => Current <= 0;

    public event Action OnDeath;
    public event Action<int, int> OnChanged;

    private void Awake()
    {
        Current = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;
        Current = Mathf.Max(0, Current - amount);
        OnChanged?.Invoke(Current, maxHealth);
        if (Current <= 0)
            OnDeath?.Invoke();
    }
}
