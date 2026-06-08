using System;
using UnityEngine;

// Proyectil reutilizable disparado por enemigos a distancia. Viaja en linea recta,
// daña al primer Health de faccion distinta que toca y vuelve al pool. Si tiene
// radio de explosion, daña a todos en area en el punto de impacto (magia del Shaman).
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private float explosionRadius = 0f;
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private LayerMask impactMask = ~0;

    private Rigidbody2D rb;
    private int factionId;
    private int danio;
    private float speed;
    private float timer;
    private Action<EnemyProjectile> devolver;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    // Lo llama el enemigo al disparar. devolver permite que el proyectil regrese al pool.
    public void Lanzar(Vector2 origen, Vector2 direccion, int danio, float speed, int factionId, Action<EnemyProjectile> devolver)
    {
        this.danio = danio;
        this.speed = speed;
        this.factionId = factionId;
        this.devolver = devolver;
        timer = lifetime;
        transform.position = origen;
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angulo);
        rb.linearVelocity = direccion.normalized * speed;
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f) Despawn(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((impactMask.value & (1 << other.gameObject.layer)) == 0) return;

        EnemyAI ai = other.GetComponentInParent<EnemyAI>();
        if (ai != null && ai.FactionId == factionId) return;

        if (explosionRadius > 0f) { ExplotarEnArea(); Despawn(true); return; }

        Health h = other.GetComponentInParent<Health>();
        if (h == null || h.IsDead) return;
        h.TakeDamage(danio, transform.position);
        Despawn(true);
    }

    private void ExplotarEnArea()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            EnemyAI ai = hit.GetComponentInParent<EnemyAI>();
            if (ai != null && ai.FactionId == factionId) continue;
            Health h = hit.GetComponentInParent<Health>();
            if (h == null || h.IsDead) continue;
            h.TakeDamage(danio, transform.position);
        }
    }

    private void Despawn(bool impacto)
    {
        if (impacto && explosionEffectPrefab != null)
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        rb.linearVelocity = Vector2.zero;
        if (devolver != null) devolver(this);
        else gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (explosionRadius <= 0f) return;
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
