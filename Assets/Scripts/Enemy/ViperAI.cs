using UnityEngine;

// Serpiente venenosa. En vez de pegar quieta, embiste (lunge) cubriendo el hueco con
// el objetivo y al morder aplica veneno (dano por tiempo). Golpe rapido y darting.
public class ViperAI : EnemyAI
{
    [Header("Mordida")]
    [SerializeField] private int danioMordida = 1;
    [SerializeField] private float cooldownMordida = 1.6f;
    [SerializeField] private string attackAnim = "Snake_Attack";

    [Header("Embestida")]
    [SerializeField] private float velocidadEmbestida = 8f;
    [SerializeField] private float duracionEmbestida = 0.25f;

    [Header("Veneno")]
    [SerializeField] private int venenoPorTick = 1;
    [SerializeField] private float venenoIntervalo = 1f;
    [SerializeField] private int venenoTicks = 3;

    private int attackHash;
    private float attackTimer;
    private float embestidaTimer;
    private bool embistiendo;
    private bool mordidaEntregada;

    protected override bool DetenerEnAtaque => false;
    protected override bool AtaqueEnCurso => embistiendo;

    // Forma robable: el jugador transformado muerde con la animacion y el danio de la vibora.
    public override string AttackAnim => attackAnim;
    public override int DanioAtaque => danioMordida;

    protected override void Awake()
    {
        base.Awake();
        attackHash = Animator.StringToHash(attackAnim);
        ValidarEstado(attackAnim, attackHash);
    }

    protected override void ComportamientoAtaque(float dist)
    {
        if (embistiendo) { ActualizarEmbestida(dist); return; }

        Detener();
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f && ObjetivoEnFrente()) IniciarEmbestida();
        else ReproducirIdle();
    }

    private void IniciarEmbestida()
    {
        embistiendo = true;
        mordidaEntregada = false;
        embestidaTimer = duracionEmbestida;
        attackTimer = cooldownMordida;
        ReproducirDesdeInicio(attackHash);
        if (Objetivo != null)
        {
            Vector2 dir = ((Vector2)Objetivo.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = dir * velocidadEmbestida;
            MirarHacia(Objetivo.position);
        }
    }

    private void ActualizarEmbestida(float dist)
    {
        embestidaTimer -= Time.deltaTime;
        if (!mordidaEntregada && dist <= attackRange && ObjetivoEnFrente()) Morder();
        if (embestidaTimer <= 0f) { embistiendo = false; Detener(); }
    }

    // Tambien invocable desde un Animation Event en el frame de mordida.
    public void Morder()
    {
        if (mordidaEntregada || Objetivo == null) return;
        mordidaEntregada = true;
        bool bloqueado = AplicarDanioAObjetivo(danioMordida);
        if (bloqueado) return;
        AplicarVeneno();
    }

    private void AplicarVeneno()
    {
        Health h = Objetivo.GetComponentInParent<Health>();
        if (h == null) return;
        DanioPorTiempo dot = h.GetComponent<DanioPorTiempo>();
        if (dot == null) dot = h.gameObject.AddComponent<DanioPorTiempo>();
        dot.Aplicar(venenoPorTick, venenoIntervalo, venenoTicks, transform.position);
    }
}
