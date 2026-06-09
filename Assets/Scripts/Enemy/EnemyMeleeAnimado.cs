using UnityEngine;

// Base para enemigos melee con ataque dirigido por animacion: maneja el cooldown,
// reproduce la animacion de ataque y entrega el golpe en un punto de la animacion
// (golpeEn). La FORMA del golpe (cono estilo jugador, caja) la define cada subclase
// en EjecutarGolpe. Asi Skull (caja) y el melee de cono no duplican esta logica.
public abstract class EnemyMeleeAnimado : EnemyAI
{
    [Header("Ataque melee")]
    [SerializeField] protected int danioAtaque = 1;
    [SerializeField] private float cooldownAtaque = 1f;
    [SerializeField] private float golpeEn = 0.5f;
    [SerializeField] private string attackAnim = "Attack";

    private int attackHash;
    private float attackTimer;
    private bool atacando;
    private bool golpeEntregado;

    protected override bool AtaqueEnCurso => atacando;

    protected override void Awake()
    {
        base.Awake();
        attackHash = Animator.StringToHash(attackAnim);
        ValidarEstado(attackAnim, attackHash);
    }

    protected override void ComportamientoAtaque(float dist)
    {
        attackTimer -= Time.deltaTime;
        if (!atacando && attackTimer <= 0f)
        {
            atacando = true;
            golpeEntregado = false;
            ReproducirDesdeInicio(attackHash);
            attackTimer = cooldownAtaque;
        }
        if (!atacando) { ReproducirIdle(); return; }

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName(attackAnim)) return;
        if (!golpeEntregado && info.normalizedTime >= golpeEn) Golpear();
        if (info.normalizedTime >= 1f) atacando = false;
    }

    // Tambien invocable desde un Animation Event en el frame de impacto.
    public void Golpear()
    {
        if (golpeEntregado) return;
        golpeEntregado = true;
        EjecutarGolpe();
    }

    // La forma del golpe: cono (estilo jugador) o caja. La define la subclase.
    protected abstract void EjecutarGolpe();
}
