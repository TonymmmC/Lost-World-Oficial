using UnityEngine;

// Arana: cazadora nerviosa de ataque a distancia. Escupe telarana que ralentiza al
// objetivo y, tras cada disparo, dardea de lado (skitter) para reposicionarse en vez de
// quedarse quieta. Mantiene distancia del objetivo y reusa el pool de proyectiles.
public class SpiderAI : EnemyAI
{
    [Header("Telarana")]
    [SerializeField] private EnemyProjectile telaranaPrefab;
    [SerializeField] private Transform puntoDisparo;
    [SerializeField] private int danioTelarana = 0;
    [SerializeField] private float velocidadTelarana = 6f;
    [SerializeField] private float cooldownDisparo = 2.5f;
    [SerializeField] private float liberarEn = 0.5f;
    [SerializeField] private string attackAnim = "Spider_Attack";

    [Header("Skitter (reposicion tras disparar)")]
    [SerializeField] private float velocidadSkitter = 5f;
    [SerializeField] private float duracionSkitter = 0.4f;

    private int attackHash;
    private ProjectilePool pool;
    private float attackTimer;
    private float skitterTimer;
    private Vector2 dirSkitter;
    private bool atacando;
    private bool proyectilLanzado;
    private bool skittering;

    protected override bool MantieneDistancia => true;
    protected override bool DetenerEnAtaque => false;
    protected override bool AtaqueEnCurso => atacando || skittering;

    // Forma robable: la telarana ralentiza (danio 0); como melee robado pega 1.
    public override string AttackAnim => attackAnim;
    public override int DanioAtaque => 1;

    protected override void Awake()
    {
        base.Awake();
        attackHash = Animator.StringToHash(attackAnim);
        ValidarEstado(attackAnim, attackHash);
        if (telaranaPrefab != null) pool = new ProjectilePool(telaranaPrefab);
    }

    protected override void ComportamientoAtaque(float dist)
    {
        if (skittering) { ActualizarSkitter(); return; }

        Detener();
        attackTimer -= Time.deltaTime;
        if (!atacando)
        {
            if (attackTimer <= 0f && ObjetivoEnFrente()) IniciarDisparo();
            else ReproducirIdle();
            return;
        }
        ActualizarDisparo();
    }

    private void IniciarDisparo()
    {
        atacando = true;
        proyectilLanzado = false;
        attackTimer = cooldownDisparo;
        ReproducirDesdeInicio(attackHash);
        if (Objetivo != null) MirarHacia(Objetivo.position);
    }

    private void ActualizarDisparo()
    {
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName(attackAnim)) return;
        if (!proyectilLanzado && info.normalizedTime >= liberarEn) DispararTelarana();
        if (info.normalizedTime >= 1f) { atacando = false; IniciarSkitter(); }
    }

    // Tambien invocable desde un Animation Event en el frame de lanzamiento.
    public void DispararTelarana()
    {
        if (proyectilLanzado || pool == null || Objetivo == null) return;
        proyectilLanzado = true;
        Vector2 origen = puntoDisparo != null ? (Vector2)puntoDisparo.position : (Vector2)transform.position;
        Vector2 dir = ((Vector2)Objetivo.position - origen).normalized;
        EnemyProjectile p = pool.Obtener(origen, Quaternion.identity);
        p.Lanzar(origen, dir, danioTelarana, velocidadTelarana, FactionId, pool.Devolver);
    }

    private void IniciarSkitter()
    {
        skittering = true;
        skitterTimer = duracionSkitter;
        Vector2 hacia = Objetivo != null
            ? ((Vector2)Objetivo.position - (Vector2)transform.position).normalized
            : FacingActual();
        // Dardea de lado y un poco hacia atras, alternando, para un patron erratico de arana.
        Vector2 lado = Vector2.Perpendicular(hacia) * (Random.value < 0.5f ? 1f : -1f);
        dirSkitter = (lado - hacia * 0.3f).normalized;
        if (Objetivo != null) MirarHacia(Objetivo.position);
    }

    private void ActualizarSkitter()
    {
        skitterTimer -= Time.deltaTime;
        rb.linearVelocity = dirSkitter * velocidadSkitter;
        ReproducirRun();
        if (skitterTimer <= 0f) { skittering = false; Detener(); }
    }
}
