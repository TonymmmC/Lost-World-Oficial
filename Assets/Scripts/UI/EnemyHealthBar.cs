using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Transform fill;
    [SerializeField] private Color fillColor = Color.red;

    private Health health;
    private float fillWidthOriginal;
    private float fillLeftEdge;

    private void Awake()
    {
        health = GetComponentInParent<Health>();
        if (health == null) { Debug.LogError($"{name}: falta Health en el padre"); return; }

        SpriteRenderer fillRenderer = fill.GetComponent<SpriteRenderer>();
        if (fillRenderer != null) fillRenderer.color = fillColor;

        fillWidthOriginal = fill.localScale.x;
        fillLeftEdge = fill.localPosition.x - fillWidthOriginal * 0.5f;

        health.OnChanged += UpdateBar;
        health.OnDeath += Hide;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (health == null) return;
        health.OnChanged -= UpdateBar;
        health.OnDeath -= Hide;
    }

    private void UpdateBar(int current, int max)
    {
        gameObject.SetActive(true);
        float pct = (float)current / max;
        float newWidth = fillWidthOriginal * pct;
        fill.localScale = new Vector3(newWidth, fill.localScale.y, 1f);
        fill.localPosition = new Vector3(fillLeftEdge + newWidth * 0.5f, fill.localPosition.y, 0f);
    }

    private void Hide() => gameObject.SetActive(false);
}
