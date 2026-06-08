using UnityEngine;

// Enemigo a distancia generico. Sirve para el Shaman (proyectil magico con explosion)
// y el Gnoll (lanza huesos): cambia el prefab de proyectil, los stats y la animacion
// en el inspector. Mantiene separacion del objetivo y retrocede si lo acorralan.
public class EnemyADistancia : EnemyAI
{
    [Header("Ataque a distancia")]
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private Transform puntoDisparo;
    [SerializeField] private int danioProyectil = 2;
    [SerializeField] private float velocidadProyectil = 7f;
    [SerializeField] private float cooldownAtaque = 2f;
    [SerializeField] private float liberarEn = 0.6f;
    [SerializeField] private string attackAnim = "Attack";

    private int attackHash;
    private ProjectilePool pool;
    private float attackTimer;
    private bool atacando;
    private bool proyectilLanzado;

    protected override bool MantieneDistancia => true;
    protected override bool AtaqueEnCurso => atacando;

    protected override void Awake()
    {
        base.Awake();
        attackHash = Animator.StringToHash(attackAnim);
        ValidarEstado(attackAnim, attackHash);
        if (projectilePrefab != null) pool = new ProjectilePool(projectilePrefab);
    }

    protected override void ComportamientoAtaque(float dist)
    {
        attackTimer -= Time.deltaTime;
        if (!atacando && attackTimer <= 0f)
        {
            atacando = true;
            proyectilLanzado = false;
            animator.Play(attackHash, 0, 0f);
            attackTimer = cooldownAtaque;
        }
        if (!atacando) { ReproducirIdle(); return; }

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName(attackAnim)) return;
        if (!proyectilLanzado && info.normalizedTime >= liberarEn) DispararProyectil();
        if (info.normalizedTime >= 1f) atacando = false;
    }

    // Tambien invocable desde un Animation Event en el frame de lanzamiento.
    public void DispararProyectil()
    {
        if (proyectilLanzado || pool == null || Objetivo == null) return;
        proyectilLanzado = true;
        Vector2 origen = puntoDisparo != null ? (Vector2)puntoDisparo.position : (Vector2)transform.position;
        Vector2 dir = ((Vector2)Objetivo.position - origen).normalized;
        EnemyProjectile p = pool.Obtener(origen, Quaternion.identity);
        p.Lanzar(origen, dir, danioProyectil, velocidadProyectil, FactionId, pool.Devolver);
    }
}
