using System.Collections;
using UnityEngine;

// Reaccion visual al recibir dano: destello del sprite y hit stop.
// El knockback lo aplica cada script de movimiento (Player y Enemy) por separado.
[RequireComponent(typeof(Health))]
public class DamageFeedback : MonoBehaviour
{
    [Header("Flash")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.08f;

    [Header("Hit stop")]
    [SerializeField] private bool aplicaHitStop = true;
    [SerializeField] private float hitStopDuration = 0.05f;

    private Health health;
    private SpriteRenderer spriteRenderer;
    private Color colorOriginal;
    private Coroutine flashRoutine;

    private void Awake()
    {
        health = GetComponent<Health>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) colorOriginal = spriteRenderer.color;
    }

    private void OnEnable()  { health.OnDamaged += ReaccionarAlDano; }
    private void OnDisable() { health.OnDamaged -= ReaccionarAlDano; }

    private void ReaccionarAlDano(Vector2 origen)
    {
        if (spriteRenderer != null)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(Flash());
        }
        if (aplicaHitStop && HitStop.Instance != null)
            HitStop.Instance.Aplicar(hitStopDuration);
    }

    private IEnumerator Flash()
    {
        spriteRenderer.color = flashColor;
        yield return new WaitForSecondsRealtime(flashDuration);
        spriteRenderer.color = colorOriginal;
        flashRoutine = null;
    }
}
