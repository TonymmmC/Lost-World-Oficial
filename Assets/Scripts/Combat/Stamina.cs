using UnityEngine;
using System;

public class Stamina : MonoBehaviour
{
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float regenRate = 30f;
    [SerializeField] private float regenDelay = 1.5f;

    public float Current { get; private set; }
    public float Max => maxStamina;
    public bool IsEmpty => Current <= 0f;

    public event Action<float, float> OnChanged;
    public event Action OnEmpty;

    private float regenTimer;

    private void Awake() => Current = maxStamina;

    private void Update()
    {
        if (Current >= maxStamina) return;

        regenTimer -= Time.deltaTime;
        if (regenTimer > 0f) return;

        Current = Mathf.Min(Current + regenRate * Time.deltaTime, maxStamina);
        OnChanged?.Invoke(Current, maxStamina);
    }

    public void Drain(float amount)
    {
        if (IsEmpty) return;
        Current = Mathf.Max(0f, Current - amount);
        regenTimer = regenDelay;
        OnChanged?.Invoke(Current, maxStamina);
        if (IsEmpty) OnEmpty?.Invoke();
    }
}
