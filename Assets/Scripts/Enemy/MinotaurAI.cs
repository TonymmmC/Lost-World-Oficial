using UnityEngine;

// Mini-jefe pesado. Su ataque es una carga telegrafiada: se planta, avisa, y embiste
// en linea recta con dano fuerte y screen shake. Entre cargas levanta la Guard y bloquea
// los golpes que vienen de frente (vulnerable por la espalda y durante la carga).
public class MinotaurAI : EnemyAI
{
    private enum Fase { Listo, Telegrafiando, Cargando, Recuperando }

    [Header("Carga")]
    [SerializeField] private int danioCarga = 3;
    [SerializeField] private float cooldownCarga = 2.5f;
    [SerializeField] private float telegrafioDuracion = 0.6f;
    [SerializeField] private float velocidadCarga = 9f;
    [SerializeField] private float duracionCarga = 0.6f;
    [SerializeField] private float screenShakeIntensidad = 0.4f;
    [SerializeField] private float screenShakeDuracion = 0.25f;

    [Header("Guard")]
    [SerializeField] private float guardFrontDot = 0.2f;

    [Header("Animacion")]
    [SerializeField] private string attackAnim = "Minotaur_Attack";
    [SerializeField] private string guardAnim = "Minotaur_Guard";

    private int attackHash, guardHash;
    private Fase fase = Fase.Listo;
    private float faseTimer;
    private float cargaCooldownTimer;
    private Vector2 dirCarga;
    private bool golpeEntregado;
    private bool guardActivo;

    protected override bool DetenerEnAtaque => false;
    protected override bool AtaqueEnCurso => fase != Fase.Listo;

    protected override void Awake()
    {
        base.Awake();
        attackHash = Animator.StringToHash(attackAnim);
        guardHash = Animator.StringToHash(guardAnim);
        ValidarEstado(attackAnim, attackHash);
        ValidarEstado(guardAnim, guardHash);
        health.BlockCheck = BloqueaDesde;
    }

    private bool BloqueaDesde(Vector2 origen)
    {
        return guardActivo && CombatUtils.EnFrente(transform.position, FacingActual(), origen, guardFrontDot);
    }

    protected override void ComportamientoAtaque(float dist)
    {
        cargaCooldownTimer -= Time.deltaTime;
        switch (fase)
        {
            case Fase.Listo:        EsperarParaCargar();      break;
            case Fase.Telegrafiando: Telegrafiar();           break;
            case Fase.Cargando:     ActualizarCarga(dist);    break;
            case Fase.Recuperando:  Recuperar();              break;
        }
    }

    private void EsperarParaCargar()
    {
        Detener();
        guardActivo = true;
        Reproducir(guardHash);
        if (cargaCooldownTimer <= 0f && ObjetivoEnFrente(0.1f))
        {
            fase = Fase.Telegrafiando;
            faseTimer = telegrafioDuracion;
            ReproducirDesdeInicio(attackHash);
        }
    }

    private void Telegrafiar()
    {
        Detener();
        if (Objetivo != null) MirarHacia(Objetivo.position);
        faseTimer -= Time.deltaTime;
        if (faseTimer > 0f) return;

        guardActivo = false;
        golpeEntregado = false;
        dirCarga = Objetivo != null
            ? ((Vector2)Objetivo.position - (Vector2)transform.position).normalized
            : FacingActual();
        rb.linearVelocity = dirCarga * velocidadCarga;
        fase = Fase.Cargando;
        faseTimer = duracionCarga;
    }

    private void ActualizarCarga(float dist)
    {
        rb.linearVelocity = dirCarga * velocidadCarga;
        faseTimer -= Time.deltaTime;
        if (!golpeEntregado && dist <= attackRange) EntregarGolpe();
        if (faseTimer <= 0f) { fase = Fase.Recuperando; faseTimer = 0.4f; Detener(); }
    }

    // Tambien invocable desde un Animation Event durante la carga.
    public void EntregarGolpe()
    {
        if (golpeEntregado || Objetivo == null) return;
        golpeEntregado = true;
        bool bloqueado = AplicarDanioAObjetivo(danioCarga);
        if (!bloqueado && CameraFollow.Instance != null)
            CameraFollow.Instance.Sacudir(screenShakeIntensidad, screenShakeDuracion);
    }

    private void Recuperar()
    {
        Detener();
        ReproducirIdle();
        faseTimer -= Time.deltaTime;
        if (faseTimer <= 0f) { fase = Fase.Listo; cargaCooldownTimer = cooldownCarga; }
    }
}
