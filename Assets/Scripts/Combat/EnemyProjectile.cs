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

    [Header("Ralentizacion (telarana, opcional)")]
    // slowMultiplier < 1 ralentiza al objetivo; slowDuration 0 = sin efecto.
    [SerializeField, Range(0.1f, 1f)] private float slowMultiplier = 1f;
    [SerializeField] private float slowDuration = 0f;

    [Header("Arco (flecha, opcional)")]
    // arcoAltura > 0 + visual asignado = la flecha describe una parabola. El hitbox viaja
    // recto a ras de suelo; solo el sprite hijo (visual) sube y baja (truco top-down).
    [SerializeField] private Transform visual;
    [SerializeField] private float arcoAltura = 0f;
    [SerializeField] private float arcoDuracion = 0.6f;

    private Rigidbody2D rb;
    private int factionId;
    private int danio;
    private float speed;
    private float timer;
    private Collider2D ignorar;
    private Vector2 dir;
    private float arcoElapsed;
    private bool modoArco;
    private Action<EnemyProjectile> devolver;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    // Lo llama el que dispara. devolver permite que el proyectil regrese al pool.
    // ignorar: collider del que dispara, para que no se autodañe (flecha del jugador).
    public void Lanzar(Vector2 origen, Vector2 direccion, int danio, float speed, int factionId, Action<EnemyProjectile> devolver, Collider2D ignorar = null)
    {
        this.danio = danio;
        this.speed = speed;
        this.factionId = factionId;
        this.devolver = devolver;
        this.ignorar = ignorar;
        dir = direccion.normalized;
        timer = lifetime;
        transform.position = origen;
        modoArco = arcoAltura > 0f && visual != null && arcoDuracion > 0f;
        if (modoArco)
        {
            arcoElapsed = 0f;
            transform.rotation = Quaternion.identity;
            visual.localPosition = Vector3.zero;
        }
        else
        {
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }
        rb.linearVelocity = dir * speed;
    }

    private void Update()
    {
        if (modoArco) { ActualizarArco(); return; }
        timer -= Time.deltaTime;
        if (timer <= 0f) Despawn(false);
    }

    // Sube y baja el sprite hijo en parabola y lo inclina segun la pendiente. El hitbox
    // (raiz) sigue recto. Al completar el arco la flecha "aterriza" y se despacha.
    private void ActualizarArco()
    {
        arcoElapsed += Time.deltaTime;
        float t = arcoElapsed / arcoDuracion;
        visual.localPosition = new Vector3(0f, arcoAltura * Mathf.Sin(Mathf.PI * t), 0f);
        float arcoVel = arcoAltura * Mathf.PI / arcoDuracion * Mathf.Cos(Mathf.PI * t);
        Vector2 vScreen = new Vector2(dir.x * speed, dir.y * speed + arcoVel);
        visual.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(vScreen.y, vScreen.x) * Mathf.Rad2Deg);
        if (t >= 1f) Despawn(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (ignorar != null && other == ignorar) return;
        if ((impactMask.value & (1 << other.gameObject.layer)) == 0) return;

        EnemyAI ai = other.GetComponentInParent<EnemyAI>();
        if (ai != null && ai.FactionId == factionId) return;

        if (explosionRadius > 0f) { ExplotarEnArea(); Despawn(true); return; }

        Health h = other.GetComponentInParent<Health>();
        if (h == null || h.IsDead) return;
        h.TakeDamage(danio, transform.position);
        AplicarSlow(h);
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
            AplicarSlow(h);
        }
    }

    private void AplicarSlow(Health h)
    {
        if (slowDuration <= 0f || slowMultiplier >= 1f) return;
        StatusLentitud s = h.GetComponent<StatusLentitud>();
        if (s != null) s.Aplicar(slowMultiplier, slowDuration);
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
