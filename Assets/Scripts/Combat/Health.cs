using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    public Func<bool> BlockCheck;
    [SerializeField] private int maxHealth = 3;

    public int Current { get; private set; }
    public int Max => maxHealth;
    public bool IsDead => Current <= 0;

    public event Action OnDeath;
    public event Action OnBlocked;
    public event Action<int, int> OnChanged;

    private void Awake()
    {
        Current = maxHealth;
    }

    public bool TakeDamage(int amount)
    {
        if (IsDead) return false;
        if (BlockCheck != null && BlockCheck()) { OnBlocked?.Invoke(); return true; }
        Current = Mathf.Max(0, Current - amount);
        OnChanged?.Invoke(Current, maxHealth);
        if (Current <= 0)
            OnDeath?.Invoke();
        return false;
    }
}
