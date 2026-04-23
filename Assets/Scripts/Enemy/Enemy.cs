using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack }

    [Header("Faccion")]
    [SerializeField] private int factionId = 0;
    public int FactionId => factionId;

    [Header("Stats")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float chaseSpeed = 2.5f;
    [SerializeField] private float detectionRange = 4f;
    [SerializeField] private float chaseStopRange = 6f;
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private float attackCooldown = 1f;

    [Header("Patrol")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float waitAtPointTime = 1f;

    [Header("Animation")]
    [SerializeField] private string idleState   = "Warrior_Idle_Purple";
    [SerializeField] private string runState    = "Warrior_Run_Purple";
    [SerializeField] private string attackState = "Warrior_Attack1_Purple";

    [Header("Death")]
    [SerializeField] private GameObject deathEffectPrefab;

    [Header("Combat")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackHitDelay = 0.3f;
    [SerializeField] private float blockKnockbackForce = 4f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private State currentState = State.Idle;
    private Transform patrolTarget;
    private Transform chaseTarget;
    private Health targetHealth;
    private float waitTimer;
    private float attackTimer;
    private float knockbackTimer;
    private float searchTimer;
    private bool isPlayingAttack;
    private bool hitDelivered;
    private float hitTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (pointA != null && pointB != null)
            currentState = State.Patrol;

        Health health = GetComponent<Health>();
        if (health != null)
            health.OnDeath += OnDeath;
        else
            Debug.LogWarning($"{gameObject.name} no tiene componente Health");
    }

    private void Update()
    {
        if (knockbackTimer > 0f) { knockbackTimer -= Time.deltaTime; return; }

        switch (currentState)
        {
            case State.Idle:
                rb.linearVelocity = Vector2.zero;
                PlayAnim(idleState);
                SearchForTarget();
                break;

            case State.Patrol:
                SearchForTarget();
                if (chaseTarget != null) { currentState = State.Chase; break; }
                Patrol();
                break;

            case State.Chase:
                if (chaseTarget == null || IsTargetDead()) { LoseTarget(); break; }
                float distToTarget = Vector2.Distance(transform.position, chaseTarget.position);
                if (distToTarget > chaseStopRange) { LoseTarget(); break; }
                if (distToTarget <= attackRange) { currentState = State.Attack; break; }
                MoveTowards(chaseTarget.position, chaseSpeed);
                break;

            case State.Attack:
                rb.linearVelocity = Vector2.zero;
                if (chaseTarget == null || IsTargetDead()) { LoseTarget(); break; }
                if (Vector2.Distance(transform.position, chaseTarget.position) > attackRange * 1.5f && !isPlayingAttack)
                {
                    currentState = State.Chase;
                    break;
                }
                HandleAttackLogic();
                break;
        }
    }

    private void SearchForTarget()
    {
        searchTimer -= Time.deltaTime;
        if (searchTimer > 0f) return;
        searchTimer = 0.5f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRange);
        float nearest = float.MaxValue;
        Transform best = null;
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            Enemy e = hit.GetComponent<Enemy>() ?? hit.GetComponentInParent<Enemy>();
            if (e != null && e.FactionId == factionId) continue;
            Health h = hit.GetComponent<Health>() ?? hit.GetComponentInParent<Health>();
            if (h == null || h.IsDead) continue;
            float d = Vector2.Distance(transform.position, hit.transform.position);
            if (d < nearest) { nearest = d; best = hit.transform; }
        }
        if (best != null)
        {
            chaseTarget = best;
            targetHealth = chaseTarget.GetComponent<Health>() ?? chaseTarget.GetComponentInParent<Health>();
            currentState = State.Chase;
        }
    }

    private void HandleAttackLogic()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            isPlayingAttack = true;
            hitDelivered = false;
            hitTimer = attackHitDelay;
            animator.Play(attackState, 0, 0f);
            attackTimer = attackCooldown;
        }

        if (isPlayingAttack && !hitDelivered)
        {
            hitTimer -= Time.deltaTime;
            if (hitTimer <= 0f)
            {
                hitDelivered = true;
                Enemy targetEnemy = chaseTarget.GetComponent<Enemy>();
                bool sameFaction = targetEnemy != null && targetEnemy.FactionId == factionId;
                if (Vector2.Distance(transform.position, chaseTarget.position) <= attackRange && targetHealth != null && !sameFaction)
                {
                    bool blocked = targetHealth.TakeDamage(attackDamage);
                    if (blocked)
                    {
                        Vector2 knockDir = ((Vector2)transform.position - (Vector2)chaseTarget.position).normalized;
                        rb.linearVelocity = knockDir * blockKnockbackForce;
                        knockbackTimer = 0.2f;
                    }
                }
            }
        }
        if (isPlayingAttack)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(attackState) && info.normalizedTime >= 1f)
                isPlayingAttack = false;
        }
        if (!isPlayingAttack) PlayAnim(idleState);
    }

    private void LoseTarget()
    {
        chaseTarget = null;
        targetHealth = null;
        isPlayingAttack = false;
        rb.linearVelocity = Vector2.zero;
        currentState = pointA != null && pointB != null ? State.Patrol : State.Idle;
    }

    private bool IsTargetDead()
    {
        return targetHealth == null || targetHealth.IsDead;
    }

    private void OnDeath()
    {
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    private void Patrol()
    {
        if (patrolTarget == null) { patrolTarget = pointA; waitTimer = waitAtPointTime; }

        float dist = Vector2.Distance(transform.position, patrolTarget.position);
        if (dist < 0.15f)
        {
            rb.linearVelocity = Vector2.zero;
            PlayAnim(idleState);
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                patrolTarget = patrolTarget == pointA ? pointB : pointA;
                waitTimer = waitAtPointTime;
            }
            return;
        }
        MoveTowards(patrolTarget.position, moveSpeed);
    }

    private void MoveTowards(Vector2 target, float speed)
    {
        Vector2 dir = ((Vector2)target - rb.position).normalized;
        Vector2 perp = Vector2.Perpendicular(dir) * Mathf.Sin(Time.time * 4f) * 0.4f;
        Vector2 finalDir = (dir + perp).normalized;
        rb.linearVelocity = finalDir * speed;
        spriteRenderer.flipX = finalDir.x < 0;
        PlayAnim(runState);
    }

    private void PlayAnim(string state)
    {
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(state))
            animator.Play(state, 0, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, chaseStopRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
