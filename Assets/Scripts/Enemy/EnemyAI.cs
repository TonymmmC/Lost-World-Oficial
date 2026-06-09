using System.Collections.Generic;
using UnityEngine;

// Base de todos los enemigos agresivos. Centraliza lo comun: deteccion por faccion,
// merodeo en zona, persecucion, knockback recibido y muerte. Cada subclase solo
// implementa su ataque propio en ComportamientoAtaque y, si hace falta, ajusta los
// flags virtuales (mantener distancia, detenerse al atacar, etc.).
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public abstract class EnemyAI : MonoBehaviour
{
    protected enum State { Idle, Wander, Chase, Attack, Return }

    [Header("Faccion")]
    [SerializeField] private int factionId = 0;
    public int FactionId => factionId;

    [Header("Zona de merodeo")]
    [SerializeField] private Vector2 zoneSize = new Vector2(12f, 12f);
    [SerializeField] private float wanderSpeed = 1.3f;
    [SerializeField] private float wanderDuration = 2.5f;
    [SerializeField] private float idleDuration = 1.5f;

    [Header("Deteccion")]
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float giveUpRange = 9f;
    [SerializeField] private float reevalInterval = 0.3f;

    [Header("Persecucion")]
    [SerializeField] protected float chaseSpeed = 3f;
    [SerializeField] protected float returnSpeed = 2f;
    [SerializeField] protected float attackRange = 1f;

    [Header("Cansancio al huir (solo enemigos a distancia)")]
    // Cuanto puede huir seguido antes de cansarse, y cuanto queda plantado (vulnerable)
    // al cansarse. Evita el kiting infinito: el arquero se deja alcanzar al cansarse.
    [SerializeField] private float kiteResistencia = 2.5f;
    [SerializeField] private float kiteRecuperacion = 1.5f;

    [Header("Ataque (cono frontal)")]
    // attackFrontDot: ancho del cono. 0 = medio circulo (180 grados), 0.3 ~ 145, 0.5 ~ 120, 0.7 ~ 90.
    [SerializeField] protected float attackFrontDot = 0.2f;
    [SerializeField] protected float attackAngleOffset = 0f;

    [Header("Animacion base")]
    [SerializeField] private string idleAnim = "Idle";
    [SerializeField] private string runAnim = "Run";

    [Header("Knockback recibido")]
    [SerializeField] private float knockbackRecibidoForce = 9f;
    [SerializeField] private float knockbackRecibidoDuracion = 0.25f;

    [Header("Muerte")]
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private string deathAnim;           // estado de muerte (vacio = destruir al instante)
    [SerializeField] private float deathDuration = 0.8f; // cuanto dura la animacion antes de destruir

    protected Rigidbody2D rb;
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;
    protected Health health;
    protected Collider2D selfCollider;

    private int idleHash, runHash;
    private readonly HashSet<int> estadosInvalidos = new HashSet<int>();
    private State currentState;
    private Vector2 homePosition;
    private Vector2 wanderTarget;
    private Transform currentTarget;
    private Collider2D targetCollider;
    private float stateTimer;
    private float searchTimer;
    private float knockbackTimer;
    private float kiteStamina;
    private float kiteCansadoTimer;
    private bool kiteCansado;
    private bool kiteHuyoEsteFrame;
    private bool muriendo;

    // Un enemigo a distancia solo puede huir si le queda aguante (no esta cansado).
    protected bool PuedeHuir => MantieneDistancia && !kiteCansado;

    protected Transform Objetivo => currentTarget;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>();
        selfCollider = GetComponent<Collider2D>();
        idleHash = Animator.StringToHash(idleAnim);
        runHash = Animator.StringToHash(runAnim);
        ValidarEstado(idleAnim, idleHash);
        ValidarEstado(runAnim, runHash);

        health.OnDeath += Morir;
        health.OnDamaged += AplicarKnockbackRecibido;
    }

    protected virtual void OnDestroy()
    {
        if (health == null) return;
        health.OnDeath -= Morir;
        health.OnDamaged -= AplicarKnockbackRecibido;
    }

    protected virtual void Start()
    {
        homePosition = transform.position;
        kiteStamina = kiteResistencia;
        EntrarIdle();
    }

    protected virtual void Update()
    {
        if (muriendo) return;
        if (ProcesarKnockback()) return;

        kiteHuyoEsteFrame = false;
        switch (currentState)
        {
            case State.Idle:
            case State.Wander: MerodearYBuscar(); break;
            case State.Chase:  Perseguir();        break;
            case State.Attack: Atacar();           break;
            case State.Return: Regresar();         break;
        }
        ActualizarCansancio();
    }

    // Gestiona el aguante para huir: drena mientras huye, se cansa al agotarlo (queda
    // plantado kiteRecuperacion segundos) y regenera cuando no huye.
    private void ActualizarCansancio()
    {
        if (!MantieneDistancia) return;
        if (kiteCansado)
        {
            kiteCansadoTimer -= Time.deltaTime;
            if (kiteCansadoTimer <= 0f) { kiteCansado = false; kiteStamina = kiteResistencia; }
            return;
        }
        if (kiteHuyoEsteFrame)
        {
            kiteStamina -= Time.deltaTime;
            if (kiteStamina <= 0f) { kiteCansado = true; kiteCansadoTimer = kiteRecuperacion; }
        }
        else kiteStamina = Mathf.Min(kiteStamina + Time.deltaTime, kiteResistencia);
    }

    // --- Estados ---

    private void MerodearYBuscar()
    {
        BuscarObjetivo();
        if (currentState == State.Chase) return;

        stateTimer -= Time.deltaTime;
        if (currentState == State.Wander)
        {
            if (Vector2.Distance(transform.position, wanderTarget) > 0.3f && stateTimer > 0f)
            { MoverHacia(wanderTarget, wanderSpeed); return; }
            EntrarIdle();
            return;
        }
        if (stateTimer <= 0f) EntrarWander();
    }

    private void Perseguir()
    {
        if (!ObjetivoVivo()) { EntrarReturn(); return; }
        ReevaluarObjetivo();

        float dist = DistanciaAlObjetivo();
        if (dist > giveUpRange) { EntrarReturn(); return; }

        if (PuedeHuir && dist < distanciaRetirada)
        { kiteHuyoEsteFrame = true; Alejarse(currentTarget.position, chaseSpeed); return; }

        if (dist <= attackRange) { currentState = State.Attack; return; }
        MoverHacia(currentTarget.position, chaseSpeed);
    }

    private void Atacar()
    {
        if (!ObjetivoVivo()) { EntrarReturn(); return; }

        float dist = DistanciaAlObjetivo();
        if (!AtaqueEnCurso)
        {
            if (dist > attackRange * 1.5f) { currentState = State.Chase; return; }
            if (PuedeHuir && dist < distanciaRetirada) { currentState = State.Chase; return; }
        }
        if (DetenerEnAtaque) Detener();
        if (!AtaqueEnCurso) MirarHacia(currentTarget.position);

        ComportamientoAtaque(dist);
    }

    private void Regresar()
    {
        BuscarObjetivo();
        if (currentState == State.Chase) return;
        if (Vector2.Distance(transform.position, homePosition) < 0.4f) { EntrarIdle(); return; }
        MoverHacia(homePosition, returnSpeed);
    }

    // --- Transiciones ---

    private void EntrarIdle()
    {
        currentState = State.Idle;
        currentTarget = null;
        stateTimer = idleDuration + Random.Range(-0.3f, 0.3f);
        Detener();
        Reproducir(idleHash);
    }

    private void EntrarWander()
    {
        currentState = State.Wander;
        stateTimer = wanderDuration + Random.Range(-0.5f, 0.5f);
        float x = Random.Range(-zoneSize.x * 0.5f, zoneSize.x * 0.5f);
        float y = Random.Range(-zoneSize.y * 0.5f, zoneSize.y * 0.5f);
        wanderTarget = homePosition + new Vector2(x, y);
    }

    private void EntrarReturn()
    {
        currentState = State.Return;
        currentTarget = null;
        AlPerderObjetivo();
    }

    // --- Deteccion por faccion ---

    private void BuscarObjetivo()
    {
        if (ObjetivoVivo()) return;
        Transform best = EncontrarMasCercano(out Collider2D col, out _);
        if (best == null) return;
        currentTarget = best;
        targetCollider = col;
        currentState = State.Chase;
        AlDetectarObjetivo();
    }

    private void ReevaluarObjetivo()
    {
        searchTimer -= Time.deltaTime;
        if (searchTimer > 0f) return;
        searchTimer = reevalInterval;

        Transform best = EncontrarMasCercano(out Collider2D col, out float nuevaDist);
        if (best == null || best == currentTarget) return;
        if (nuevaDist < DistanciaAlObjetivo() * 0.8f) { currentTarget = best; targetCollider = col; }
    }

    private Transform EncontrarMasCercano(out Collider2D col, out float dist)
    {
        col = null;
        dist = float.MaxValue;
        Transform best = null;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRange);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            EnemyAI otro = hit.GetComponentInParent<EnemyAI>();
            if (otro != null && otro.factionId == factionId) continue;
            Health h = hit.GetComponentInParent<Health>();
            if (h == null || h == health || h.IsDead) continue;
            float d = Vector2.Distance(transform.position, hit.transform.position);
            if (d < dist) { dist = d; best = h.transform; col = hit; }
        }
        return best;
    }

    // --- Movimiento ---

    protected void MoverHacia(Vector2 target, float speed)
    {
        Vector2 toTarget = target - rb.position;
        Vector2 dir = toTarget.normalized;
        float wobble = Mathf.Clamp01(toTarget.magnitude - 1f);
        Vector2 perp = Vector2.Perpendicular(dir) * Mathf.Sin(Time.time * 4f) * 0.4f * wobble;
        Vector2 finalDir = (dir + perp).normalized;
        rb.linearVelocity = finalDir * speed;
        // El sprite mira segun la direccion real al destino, no la bamboleada: si usara
        // finalDir, el wobble haria voltear el sprite varias veces por segundo (parpadeo).
        if (Mathf.Abs(dir.x) > 0.05f) spriteRenderer.flipX = dir.x < 0;
        Reproducir(runHash);
    }

    private void Alejarse(Vector2 amenaza, float speed)
    {
        Vector2 dir = (rb.position - amenaza).normalized;
        if (dir == Vector2.zero) dir = Vector2.right;
        rb.linearVelocity = dir * speed;
        MirarHacia(amenaza);
        Reproducir(runHash);
    }

    protected void Detener() => rb.linearVelocity = Vector2.zero;

    protected void MirarHacia(Vector2 objetivo)
    {
        float dx = objetivo.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.05f) spriteRenderer.flipX = dx < 0;
    }

    // --- Knockback recibido ---

    private void AplicarKnockbackRecibido(Vector2 origen)
    {
        Vector2 dir = ((Vector2)transform.position - origen).normalized;
        if (dir == Vector2.zero) return;
        rb.linearVelocity = dir * knockbackRecibidoForce;
        knockbackTimer = knockbackRecibidoDuracion;
    }

    private bool ProcesarKnockback()
    {
        if (knockbackTimer <= 0f) return false;
        knockbackTimer -= Time.deltaTime;
        return true;
    }

    // --- Utilidades para subclases ---

    protected bool ObjetivoVivo()
    {
        if (currentTarget == null) return false;
        Health h = currentTarget.GetComponentInParent<Health>();
        return h != null && !h.IsDead;
    }

    protected float DistanciaAlObjetivo()
    {
        if (currentTarget == null) return float.MaxValue;
        if (selfCollider != null && targetCollider != null)
            return selfCollider.Distance(targetCollider).distance;
        return Vector2.Distance(transform.position, currentTarget.position);
    }

    protected Vector2 FacingActual()
    {
        // GetComponent como respaldo: en modo edicion (gizmos) spriteRenderer aun es null.
        SpriteRenderer sr = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
        Vector2 f = Quaternion.Euler(0f, 0f, attackAngleOffset) * Vector3.right;
        if (sr != null && sr.flipX) f.x = -f.x;
        return f;
    }

    protected bool ObjetivoEnFrente() => ObjetivoEnFrente(attackFrontDot);

    protected bool ObjetivoEnFrente(float minDot)
    {
        if (currentTarget == null) return false;
        return CombatUtils.EnFrente(transform.position, FacingActual(), currentTarget.position, minDot);
    }

    protected bool AplicarDanioAObjetivo(int danio)
    {
        if (currentTarget == null) return false;
        Health h = currentTarget.GetComponentInParent<Health>();
        if (h == null || h.IsDead) return false;
        return h.TakeDamage(danio, transform.position);
    }

    // Golpea a todos los objetivos de faccion distinta dentro de una caja frontal.
    // tamano: ancho x alto de la caja. distanciaFrente: cuanto la separa del centro
    // hacia donde mira. Devuelve true si conecto con alguien.
    protected bool GolpearCajaFrontal(Vector2 tamano, float distanciaFrente, int danio)
    {
        Vector2 facing = FacingActual().normalized;
        Vector2 centro = (Vector2)transform.position + facing * distanciaFrente;
        float angulo = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
        Collider2D[] hits = Physics2D.OverlapBoxAll(centro, tamano, angulo);
        bool golpeo = false;
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            EnemyAI otro = hit.GetComponentInParent<EnemyAI>();
            if (otro != null && otro.factionId == factionId) continue;
            Health h = hit.GetComponentInParent<Health>();
            if (h == null || h == health || h.IsDead) continue;
            h.TakeDamage(danio, transform.position);
            golpeo = true;
        }
        return golpeo;
    }

    // Golpe en cono frontal, igual que el ataque del jugador: alcanza a todos los
    // objetivos de faccion distinta dentro del radio (attackRange) y del cono
    // (attackFrontDot). Devuelve true si conecto con alguien.
    protected bool GolpearConoFrontal(int danio)
    {
        Vector2 facing = FacingActual();
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);
        bool golpeo = false;
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            EnemyAI otro = hit.GetComponentInParent<EnemyAI>();
            if (otro != null && otro.factionId == factionId) continue;
            if (!CombatUtils.EnFrente(transform.position, facing, hit.transform.position, attackFrontDot)) continue;
            Health h = hit.GetComponentInParent<Health>();
            if (h == null || h == health || h.IsDead) continue;
            h.TakeDamage(danio, transform.position);
            golpeo = true;
        }
        return golpeo;
    }

    protected void Reproducir(int hash)
    {
        if (animator == null || estadosInvalidos.Contains(hash)) return;
        if (!animator.GetCurrentAnimatorStateInfo(0).shortNameHash.Equals(hash))
            animator.Play(hash, 0, 0f);
    }

    // Relanza un estado desde el frame 0 (animaciones de ataque que se reinician en cada
    // golpe). Usar en vez de animator.Play directo: respeta el guard de estados invalidos.
    protected void ReproducirDesdeInicio(int hash)
    {
        if (animator == null || estadosInvalidos.Contains(hash)) return;
        animator.Play(hash, 0, 0f);
    }

    // Reproduce el idle base (usado por subclases entre ataques).
    protected void ReproducirIdle() => Reproducir(idleHash);

    // Reproduce el run base (subclases que se mueven durante su ataque, como el skitter de la arana).
    protected void ReproducirRun() => Reproducir(runHash);

    // Registra los estados que no existen en el controller para no intentar reproducirlos
    // (cada Play fallido spamea un warning de Unity por frame). Avisa una sola vez en el
    // editor. Las subclases la llaman para sus estados propios (ataque, guard).
    protected void ValidarEstado(string nombre, int hash)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        if (animator.HasState(0, hash)) return;
        estadosInvalidos.Add(hash);
#if UNITY_EDITOR
        Debug.LogError($"{name}: el Animator no tiene el estado '{nombre}'. Revisa el nombre en el inspector.");
#endif
    }

    protected void VolverAPerseguir() => currentState = State.Chase;

    private void Morir()
    {
        if (muriendo) return;
        muriendo = true;
        rb.linearVelocity = Vector2.zero;
        if (selfCollider != null) selfCollider.enabled = false; // el cadaver no estorba ni recibe golpes
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        bool conAnim = !string.IsNullOrEmpty(deathAnim) && animator != null;
        if (conAnim) animator.Play(deathAnim, 0, 0f);
        Destroy(gameObject, conAnim ? deathDuration : 0f);
    }

    // --- Puntos de extension ---

    // El ataque propio de cada enemigo. dist es la distancia actual al objetivo.
    protected abstract void ComportamientoAtaque(float dist);

    // Override en enemigos a distancia para mantener separacion (kiting).
    protected virtual bool MantieneDistancia => false;
    protected virtual float distanciaRetirada => attackRange * 0.6f;

    // Override en enemigos que se mueven durante el ataque (carga del Minotauro).
    protected virtual bool DetenerEnAtaque => true;

    // Lo controla cada subclase mientras dura su animacion/secuencia de ataque.
    protected virtual bool AtaqueEnCurso => false;

    // Hooks opcionales para reaccionar a cambios de objetivo.
    protected virtual void AlDetectarObjetivo() { }
    protected virtual void AlPerderObjetivo() { }

    protected virtual void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? (Vector3)homePosition : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, new Vector3(zoneSize.x, zoneSize.y, 0f));
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, giveUpRange);

        DibujarGizmoAtaque();
    }

    // Por defecto dibuja el cono frontal (magenta). Los enemigos con hitbox rectangular
    // sobrescriben esto para dibujar su caja.
    protected virtual void DibujarGizmoAtaque()
    {
        Gizmos.color = Color.magenta;
        CombatUtils.DibujarCono(transform.position, FacingActual(), attackFrontDot, attackRange);
    }

    // Dibuja una caja frontal orientada segun el facing (para gizmos de ataque rectangular).
    protected void DibujarCajaFrontal(Vector2 tamano, float distanciaFrente)
    {
        Vector2 facing = FacingActual().normalized;
        Vector2 centro = (Vector2)transform.position + facing * distanciaFrente;
        float angulo = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
        Matrix4x4 prev = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(centro, Quaternion.Euler(0f, 0f, angulo), Vector3.one);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(Vector3.zero, tamano);
        Gizmos.matrix = prev;
    }
}
