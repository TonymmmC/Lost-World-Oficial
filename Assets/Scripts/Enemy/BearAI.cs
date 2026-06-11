using UnityEngine;

public class BearAI : MonoBehaviour
{
    public enum State { Idle, Wander, Chase, Attack, Return }

    [Header("Zona")]
    [SerializeField] private Vector2 zoneSize = new Vector2(16f, 16f);
    [SerializeField] private float wanderSpeed = 1.5f;
    [SerializeField] private float wanderDuration = 2.5f;
    [SerializeField] private float idleDuration = 1.5f;

    [Header("Deteccion")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float leashRange = 14f;
    [SerializeField] private float giveUpRange = 10f;

    [Header("Combate")]
    [SerializeField] private float chaseSpeed = 4.5f;
    [SerializeField] private float returnSpeed = 2f;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float meleeGap = 0.2f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float attackHitNormalized = 0.85f;
    [SerializeField] private int attackDamage = 3;
    [SerializeField] private float attackFrontDot = 0.2f;
    [SerializeField] private float attackAngleOffset = 0f;

    [Header("Frustracion")]
    [SerializeField] private float tiempoMaxBloqueado = 30f;
    [SerializeField] private float frustratedIgnoreTime = 6f;

    [Header("Evasion de paredes")]
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private float avoidDistance = 1.2f;
    [SerializeField] private float avoidRadius = 0.4f;

    [Header("Animacion")]
    [SerializeField] private string idleAnim = "Bear_Idle";
    [SerializeField] private string runAnim = "Bear_Run";
    [SerializeField] private string attackAnim = "Bear_Attack";

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D selfCollider;
    private Collider2D targetCollider;

    private int idleHash, runHash, attackHash;
    private Vector2 homePosition;
    private State currentState;
    private Transform currentTarget;
    private Vector2 wanderTarget;
    private float stateTimer;
    private float attackTimer;
    private float hitTimer;
    private float searchTimer;
    private bool isPlayingAttack;
    private bool hitDelivered;
    private float tiempoBloqueado;
    private bool ultimoGolpeBloqueado;
    private Transform objetivoIgnorado;
    private float ignorarTimer;
    private bool regresarTrasIdle;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        selfCollider = GetComponent<Collider2D>();
        idleHash = Animator.StringToHash(idleAnim);
        runHash = Animator.StringToHash(runAnim);
        attackHash = Animator.StringToHash(attackAnim);

        Health health = GetComponent<Health>();
        if (health != null) health.OnDeath += () => Destroy(gameObject);
    }

    private void Start()
    {
        homePosition = transform.position;
        EnterIdle();
    }

    private void Update()
    {
        if (ignorarTimer > 0f)
        {
            ignorarTimer -= Time.deltaTime;
            if (ignorarTimer <= 0f) objetivoIgnorado = null;
        }

        switch (currentState)
        {
            case State.Idle:
            case State.Wander:
                HandleZoneWander();
                SearchForTarget();
                break;
            case State.Chase:
                HandleChase();
                break;
            case State.Attack:
                HandleAttack();
                break;
            case State.Return:
                HandleReturn();
                SearchForTarget();
                break;
        }
    }

    private void HandleZoneWander()
    {
        stateTimer -= Time.deltaTime;

        if (currentState == State.Wander)
        {
            float dist = Vector2.Distance(transform.position, wanderTarget);
            if (dist > 0.3f && stateTimer > 0f) { MoveTo(wanderTarget, wanderSpeed); return; }
            EnterIdle();
            return;
        }

        if (stateTimer <= 0f)
        {
            if (regresarTrasIdle) { regresarTrasIdle = false; EnterReturn(); }
            else EnterWander();
        }
    }

    private void SearchForTarget()
    {
        if (currentTarget != null && !IsTargetDead()) return;

        Transform best = EncontrarMasCercano(out Collider2D col, out _);
        if (best != null)
        {
            currentTarget = best;
            targetCollider = col;
            tiempoBloqueado = 0f;
            ultimoGolpeBloqueado = false;
            regresarTrasIdle = false;
            currentState = State.Chase;
        }
    }

    private void ReevaluarObjetivo()
    {
        searchTimer -= Time.deltaTime;
        if (searchTimer > 0f) return;
        searchTimer = 0.3f;

        Transform best = EncontrarMasCercano(out Collider2D col, out float nuevaDist);
        if (best == null || best == currentTarget) return;

        float distActual = Vector2.Distance(transform.position, currentTarget.position);
        if (nuevaDist < distActual * 0.8f) { currentTarget = best; targetCollider = col; tiempoBloqueado = 0f; ultimoGolpeBloqueado = false; }
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
            if (hit.transform == objetivoIgnorado) continue;
            Health h = hit.GetComponent<Health>() ?? hit.GetComponentInParent<Health>();
            if (h == null || h.IsDead) continue;
            float d = Vector2.Distance(transform.position, hit.transform.position);
            if (d < dist) { dist = d; best = hit.transform; col = hit; }
        }
        return best;
    }

    private void HandleChase()
    {
        if (currentTarget == null || IsTargetDead()) { EnterReturn(); return; }
        ReevaluarObjetivo();

        float distToTarget = DistanciaAlObjetivo();
        if (distToTarget > giveUpRange) { EnterReturn(); return; }

        if (distToTarget <= attackRange)
        {
            if (!ObjetivoEnFrente()) { Reposicionar(); return; }
            if (distToTarget > meleeGap) { MoveTo(currentTarget.position, chaseSpeed); return; }
            currentState = State.Attack;
            return;
        }
        MoveTo(currentTarget.position, chaseSpeed);
    }

    private void HandleAttack()
    {
        if (currentTarget == null || IsTargetDead()) { EnterReturn(); return; }
        if (!isPlayingAttack) ReevaluarObjetivo();
        float dist = DistanciaAlObjetivo();
        if (dist > attackRange * 1.5f && !isPlayingAttack) { currentState = State.Chase; return; }
        rb.linearVelocity = Vector2.zero;
        if (!isPlayingAttack) MirarHacia(currentTarget.position);

        if (ultimoGolpeBloqueado)
        {
            tiempoBloqueado += Time.deltaTime;
            if (tiempoBloqueado >= tiempoMaxBloqueado) { RendirseDelObjetivo(); return; }
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            if (!ObjetivoEnFrente()) { attackTimer = 0f; currentState = State.Chase; return; }
            isPlayingAttack = true;
            hitDelivered = false;
            animator.Play(attackHash, 0, 0f);
            attackTimer = attackCooldown;
            AudioManager.PlayVozEnemigo(gameObject.name);
        }

        if (isPlayingAttack)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(attackAnim))
            {
                if (!hitDelivered && info.normalizedTime >= attackHitNormalized)
                    EntregarGolpe();
                if (info.normalizedTime >= 1f)
                    isPlayingAttack = false;
            }
        }
        if (!isPlayingAttack) PlayAnim(idleHash);
    }

    // Llamado por un Animation Event en el frame de impacto (Bear_Attack_6, cuando cierra
    // los brazos). El timer attackHitDelay queda solo como respaldo si falta el evento.
    public void EntregarGolpe()
    {
        if (!isPlayingAttack || hitDelivered) return;
        hitDelivered = true;
        if (currentTarget == null || IsTargetDead()) return;
        if (DistanciaAlObjetivo() > attackRange) return;
        if (!ObjetivoEnFrente()) return;

        Health h = currentTarget.GetComponent<Health>() ?? currentTarget.GetComponentInParent<Health>();
        if (h == null) return;

        bool bloqueado = h.TakeDamage(attackDamage, transform.position);
        ultimoGolpeBloqueado = bloqueado;
        if (!bloqueado) tiempoBloqueado = 0f;
    }

    private void HandleReturn()
    {
        float dist = Vector2.Distance(transform.position, homePosition);
        if (dist < 0.5f) { EnterIdle(); return; }
        SearchForTarget();
        if (currentState != State.Return) return;
        MoveTo(homePosition, returnSpeed);
    }

    private void EnterIdle()
    {
        currentState = State.Idle;
        currentTarget = null;
        stateTimer = idleDuration + Random.Range(-0.3f, 0.3f);
        rb.linearVelocity = Vector2.zero;
        PlayAnim(idleHash);
    }

    private void EnterWander()
    {
        currentState = State.Wander;
        stateTimer = wanderDuration + Random.Range(-0.5f, 0.5f);
        float x = Random.Range(-zoneSize.x * 0.5f, zoneSize.x * 0.5f);
        float y = Random.Range(-zoneSize.y * 0.5f, zoneSize.y * 0.5f);
        wanderTarget = homePosition + new Vector2(x, y);
    }

    private void EnterReturn()
    {
        currentState = State.Return;
        currentTarget = null;
        isPlayingAttack = false;
    }

    private void RendirseDelObjetivo()
    {
        objetivoIgnorado = currentTarget;
        ignorarTimer = frustratedIgnoreTime;
        tiempoBloqueado = 0f;
        ultimoGolpeBloqueado = false;
        isPlayingAttack = false;
        regresarTrasIdle = true;
        EnterIdle();
    }

    private void MoveTo(Vector2 target, float speed)
    {
        Vector2 toTarget = target - rb.position;
        Vector2 dir = toTarget.normalized;
        float wobbleScale = Mathf.Clamp01(toTarget.magnitude - 1f);
        Vector2 perp = Vector2.Perpendicular(dir) * Mathf.Sin(Time.time * 4f) * 0.4f * wobbleScale;
        Vector2 finalDir = (dir + perp).normalized;
        finalDir = Steering.EvitarParedes(rb.position, finalDir, avoidDistance, avoidRadius, wallMask);
        rb.linearVelocity = finalDir * speed;
        spriteRenderer.flipX = finalDir.x < 0;
        PlayAnim(runHash);
    }

    private float DistanciaAlObjetivo()
    {
        if (selfCollider != null && targetCollider != null)
            return selfCollider.Distance(targetCollider).distance;
        return Vector2.Distance(transform.position, currentTarget.position);
    }

    private bool ObjetivoEnFrente()
    {
        if (currentTarget == null) return false;
        return CombatUtils.EnFrente(transform.position, FacingHacia(currentTarget.position), currentTarget.position, attackFrontDot);
    }

    // Direccion de ataque hacia un objetivo (horizontal + inclinacion), independiente
    // del flipX visual: el oso puede mirar hacia donde camina mientras rodea.
    private Vector2 FacingHacia(Vector2 objetivo)
    {
        Vector2 f = Quaternion.Euler(0f, 0f, attackAngleOffset) * Vector3.right;
        if (objetivo.x < transform.position.x) f.x = -f.x;
        return f;
    }

    private Vector2 FacingActual()
    {
        SpriteRenderer sr = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
        Vector2 f = Quaternion.Euler(0f, 0f, attackAngleOffset) * Vector3.right;
        if (sr != null && sr.flipX) f.x = -f.x;
        return f;
    }

    private void MirarHacia(Vector2 objetivo)
    {
        float dx = objetivo.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.05f) spriteRenderer.flipX = dx < 0;
    }

    // En rango pero fuera del cono: rodea al objetivo hacia su altura para
    // encararlo de costado por el lado mas cercano (no siempre el mismo).
    private void Reposicionar()
    {
        Vector2 toTarget = (Vector2)currentTarget.position - rb.position;
        Vector2 strafe = Vector2.Perpendicular(toTarget.normalized);

        float dy = currentTarget.position.y - rb.position.y;
        if (Mathf.Abs(dy) > 0.1f && Mathf.Sign(strafe.y) != Mathf.Sign(dy))
            strafe = -strafe;

        strafe = Steering.EvitarParedes(rb.position, strafe, avoidDistance, avoidRadius, wallMask);
        if (Mathf.Abs(strafe.x) > 0.05f) spriteRenderer.flipX = strafe.x < 0;
        rb.linearVelocity = strafe * chaseSpeed;
        PlayAnim(runHash);
    }

    private bool IsTargetDead()
    {
        Health h = currentTarget.GetComponent<Health>() ?? currentTarget.GetComponentInParent<Health>();
        return h == null || h.IsDead;
    }

    private void PlayAnim(int hash)
    {
        if (!animator.GetCurrentAnimatorStateInfo(0).shortNameHash.Equals(hash))
            animator.Play(hash, 0, 0f);
    }

    private void FixedUpdate()
    {
        if (currentState == State.Attack || currentState == State.Idle || currentState == State.Return && Vector2.Distance(transform.position, homePosition) < 0.5f)
            rb.linearVelocity = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? (Vector3)homePosition : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, new Vector3(zoneSize.x, zoneSize.y, 0f));
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, leashRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.magenta;
        CombatUtils.DibujarCono(transform.position, FacingActual(), attackFrontDot, attackRange + 1.5f);
    }
}
