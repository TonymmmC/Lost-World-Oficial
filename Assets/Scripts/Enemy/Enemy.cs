using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack }

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

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform player;

    [Header("Combat")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackHitDelay = 0.3f;

    private State currentState = State.Idle;
    private Transform currentTarget;
    private float waitTimer;
    private float attackTimer = 0f;
    private bool isPlayingAttack = false;
    private bool hitDelivered = false;
    private float hitTimer = 0f;
    private Health playerHealth;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindWithTag("Player")?.transform;
    }

    private void Start()
    {
        playerHealth = player?.GetComponent<Health>();

        if (pointA != null && pointB != null)
            currentState = State.Patrol;

        Health health = GetComponent<Health>();
        if (health != null)
            health.OnDeath += () => Destroy(gameObject);
        else
            Debug.LogWarning($"{gameObject.name} no tiene componente Health");
    }

    private void Update()
    {
        if (player == null) return;

        float distToPlayer = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Idle:
                rb.linearVelocity = Vector2.zero;
                PlayAnim(idleState);
                if (distToPlayer <= detectionRange)
                    currentState = State.Chase;
                break;

            case State.Patrol:
                if (distToPlayer <= detectionRange) { currentState = State.Chase; break; }
                Patrol();
                break;

            case State.Chase:
                isPlayingAttack = false;
                if (distToPlayer > chaseStopRange)
                {
                    rb.linearVelocity = Vector2.zero;
                    currentState = pointA != null && pointB != null ? State.Patrol : State.Idle;
                    break;
                }
                if (distToPlayer <= attackRange) { currentState = State.Attack; break; }
                MoveTowards(player.position, chaseSpeed);
                break;

            case State.Attack:
                rb.linearVelocity = Vector2.zero;
                if (distToPlayer > attackRange * 1.5f)
                {
                    isPlayingAttack = false;
                    currentState = State.Chase;
                    break;
                }
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
                        if (Vector2.Distance(transform.position, player.position) <= attackRange)
                            playerHealth?.TakeDamage(attackDamage);
                    }
                }
                if (isPlayingAttack)
                {
                    AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                    if (info.IsName(attackState) && info.normalizedTime >= 1f)
                        isPlayingAttack = false;
                }
                if (!isPlayingAttack)
                    PlayAnim(idleState);
                break;
        }
    }

    private void Patrol()
    {
        if (currentTarget == null)
        {
            currentTarget = pointA;
            waitTimer = waitAtPointTime;
        }

        float dist = Vector2.Distance(transform.position, currentTarget.position);

        if (dist < 0.15f)
        {
            rb.linearVelocity = Vector2.zero;
            PlayAnim(idleState);
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                currentTarget = currentTarget == pointA ? pointB : pointA;
                waitTimer = waitAtPointTime;
            }
            return;
        }

        MoveTowards(currentTarget.position, moveSpeed);
    }

    private void MoveTowards(Vector2 target, float speed)
    {
        Vector2 dir = ((Vector2)target - rb.position).normalized;
        rb.linearVelocity = dir * speed;
        spriteRenderer.flipX = dir.x < 0;
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
