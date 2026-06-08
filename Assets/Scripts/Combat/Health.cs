using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    public Func<Vector2, bool> BlockCheck;
    [SerializeField] private int maxHealth = 3;

    public int Current { get; private set; }
    public int Max => maxHealth;
    public bool IsDead => Current <= 0;

    public event Action OnDeath;
    public event Action OnBlocked;
    public event Action<int, int> OnChanged;
    public event Action<Vector2> OnDamaged;
    public event Action<Vector2> OnBlockedFrom;

    private void Awake()
    {
        Current = maxHealth;
    }

    public bool TakeDamage(int amount) => TakeDamage(amount, transform.position);

    public bool TakeDamage(int amount, Vector2 sourcePosition)
    {
        if (IsDead) return false;
        if (BlockCheck != null && BlockCheck(sourcePosition)) { OnBlocked?.Invoke(); OnBlockedFrom?.Invoke(sourcePosition); return true; }
        Current = Mathf.Max(0, Current - amount);
        OnChanged?.Invoke(Current, maxHealth);
        OnDamaged?.Invoke(sourcePosition);
        if (Current <= 0)
            OnDeath?.Invoke();
        return false;
    }

    public void AumentarVidaMaxima(int cantidad)
    {
        if (cantidad <= 0) return;
        maxHealth += cantidad;
        Current = maxHealth;
        OnChanged?.Invoke(Current, maxHealth);
    }
}
