using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Transform fill;
    [SerializeField] private Color fillColor = Color.red;
    [SerializeField] private Color bgColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    private Health health;
    private float fillOriginalX;
    private float fillWidth;

    private void Awake()
    {
        health = GetComponentInParent<Health>();

        SpriteRenderer bgRenderer = GetComponent<SpriteRenderer>();
        SpriteRenderer fillRenderer = fill.GetComponent<SpriteRenderer>();

        if (bgRenderer != null) bgRenderer.color = bgColor;
        if (fillRenderer != null) fillRenderer.color = fillColor;

        fillOriginalX = fill.localPosition.x;
        fillWidth = fill.localScale.x;

        health.OnChanged += UpdateBar;
        health.OnDeath += Hide;
        gameObject.SetActive(false);
    }

    private void UpdateBar(int current, int max)
    {
        gameObject.SetActive(true);
        float pct = (float)current / max;
        fill.localScale = new Vector3(fillWidth * pct, fill.localScale.y, 1f);
        fill.localPosition = new Vector3(fillOriginalX - fillWidth * (1f - pct) * 0.5f, fill.localPosition.y, 0f);
    }

    private void Hide() => gameObject.SetActive(false);
}
