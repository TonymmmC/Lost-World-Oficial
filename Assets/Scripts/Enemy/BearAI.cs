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

    [Header("Combate")]
    [SerializeField] private float chaseSpeed = 4.5f;
    [SerializeField] private float returnSpeed = 2f;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float attackHitDelay = 0.4f;
    [SerializeField] private int attackDamage = 3;

    [Header("Animacion")]
    [SerializeField] private string idleAnim = "Bear_Idle";
    [SerializeField] private string runAnim = "Bear_Run";
    [SerializeField] private string attackAnim = "Bear_Attack";

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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

        if (stateTimer <= 0f) EnterWander();
    }

    private void SearchForTarget()
    {
        if (currentTarget != null && !IsTargetDead()) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRange);
        float nearest = float.MaxValue;
        Transform best = null;
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            Health h = hit.GetComponent<Health>() ?? hit.GetComponentInParent<Health>();
            if (h == null || h.IsDead) continue;
            float d = Vector2.Distance(transform.position, hit.transform.position);
            if (d < nearest) { nearest = d; best = hit.transform; }
        }
        if (best != null) { currentTarget = best; currentState = State.Chase; }
    }

    private void HandleChase()
    {
        if (currentTarget == null || IsTargetDead()) { EnterReturn(); return; }
        float distToHome = Vector2.Distance(transform.position, homePosition);
        if (distToHome > leashRange) { EnterReturn(); return; }

        float distToTarget = Vector2.Distance(transform.position, currentTarget.position);
        if (distToTarget <= attackRange) { currentState = State.Attack; return; }
        MoveTo(currentTarget.position, chaseSpeed);
    }

    private void HandleAttack()
    {
        if (currentTarget == null || IsTargetDead()) { EnterReturn(); return; }
        float dist = Vector2.Distance(transform.position, currentTarget.position);
        if (dist > attackRange * 1.5f && !isPlayingAttack) { currentState = State.Chase; return; }
        rb.linearVelocity = Vector2.zero;

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            isPlayingAttack = true;
            hitDelivered = false;
            hitTimer = attackHitDelay;
            animator.Play(attackHash, 0, 0f);
            attackTimer = attackCooldown;
        }

        if (isPlayingAttack && !hitDelivered)
        {
            hitTimer -= Time.deltaTime;
            if (hitTimer <= 0f)
            {
                hitDelivered = true;
                if (Vector2.Distance(transform.position, currentTarget.position) <= attackRange)
                {
                    Health h = currentTarget.GetComponent<Health>() ?? currentTarget.GetComponentInParent<Health>();
                    h?.TakeDamage(attackDamage);
                }
            }
        }
        if (isPlayingAttack)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(attackAnim) && info.normalizedTime >= 1f)
                isPlayingAttack = false;
        }
        if (!isPlayingAttack) PlayAnim(idleHash);
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

    private void MoveTo(Vector2 target, float speed)
    {
        Vector2 dir = (target - rb.position).normalized;
        Vector2 perp = Vector2.Perpendicular(dir) * Mathf.Sin(Time.time * 4f) * 0.4f;
        Vector2 finalDir = (dir + perp).normalized;
        rb.linearVelocity = finalDir * speed;
        spriteRenderer.flipX = finalDir.x < 0;
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
    }
}
