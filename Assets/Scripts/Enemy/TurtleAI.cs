using UnityEngine;

public class TurtleAI : MonoBehaviour
{
    public enum State { Wander, Idle, HidingIn, Hidden, HidingOut }

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 0.8f;
    [SerializeField] private float wanderDuration = 2f;
    [SerializeField] private float idleDuration = 1.5f;

    [Header("Deteccion")]
    [SerializeField] private float hideRange = 3.5f;
    [SerializeField] private float showRange = 5f;
    [SerializeField] private float showDelay = 2f;

    [Header("Muerte")]
    [SerializeField] private GameObject deathEffectPrefab;

    [Header("Empuje en caparazon")]
    [SerializeField] private float pushForce = 3f;
    [SerializeField] private float pushDuration = 0.25f;

    private static readonly int IdleHash     = Animator.StringToHash("Turtle_Idle");
    private static readonly int RunHash      = Animator.StringToHash("Turtle_Run");
    private static readonly int GuardInHash  = Animator.StringToHash("Turtle_Guard_In");
    private static readonly int GuardOutHash = Animator.StringToHash("Turtle_Guard_Out");

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Health health;

    private State currentState = State.Idle;
    private Vector2 wanderDir;
    private float stateTimer;
    private float showDelayTimer;
    private float searchTimer;
    private float distToThreat = Mathf.Infinity;
    private Vector2 pushVelocity;
    private float pushTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>();
        if (health != null)
        {
            health.BlockCheck = _ => EstaEscondida();
            health.OnDeath += ManejarMuerte;
            health.OnBlockedFrom += Empujar;
        }
    }

    private void OnDestroy()
    {
        if (health == null) return;
        health.OnDeath -= ManejarMuerte;
        health.OnBlockedFrom -= Empujar;
    }

    private void Empujar(Vector2 origen)
    {
        if (!EstaEscondida()) return;
        Vector2 dir = ((Vector2)transform.position - origen).normalized;
        if (dir == Vector2.zero) return;
        pushVelocity = dir * pushForce;
        pushTimer = pushDuration;
    }

    private bool EstaEscondida()
    {
        return currentState == State.Hidden || currentState == State.HidingIn;
    }

    private void ManejarMuerte()
    {
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    private void Start()
    {
        EnterIdle();
    }

    private void Update()
    {
        ActualizarAmenaza();
        float distToPlayer = distToThreat;

        switch (currentState)
        {
            case State.Wander:
            case State.Idle:
                if (distToPlayer <= hideRange)
                {
                    EnterHidingIn();
                    return;
                }
                HandleWanderIdle();
                break;

            case State.HidingIn:
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                if (info.IsName("Turtle_Guard_In") && info.normalizedTime >= 1f)
                    currentState = State.Hidden;
                break;

            case State.Hidden:
                if (distToPlayer >= showRange)
                {
                    showDelayTimer -= Time.deltaTime;
                    if (showDelayTimer <= 0f)
                        EnterHidingOut();
                }
                else
                {
                    showDelayTimer = showDelay;
                }
                break;

            case State.HidingOut:
                AnimatorStateInfo outInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (outInfo.IsName("Turtle_Guard_Out") && outInfo.normalizedTime >= 1f)
                    EnterIdle();
                break;
        }
    }

    private void ActualizarAmenaza()
    {
        searchTimer -= Time.deltaTime;
        if (searchTimer > 0f) return;
        searchTimer = 0.2f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, showRange + 1f);
        float nearest = Mathf.Infinity;
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            Health h = hit.GetComponent<Health>() ?? hit.GetComponentInParent<Health>();
            if (h == null || h == health || h.IsDead) continue;
            Vector2 puntoCercano = hit.ClosestPoint(transform.position);
            float d = Vector2.Distance(transform.position, puntoCercano);
            if (d < nearest) nearest = d;
        }
        distToThreat = nearest;
    }

    private void HandleWanderIdle()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f) return;

        if (currentState == State.Wander)
            EnterIdle();
        else
            EnterWander();
    }

    private void EnterWander()
    {
        currentState = State.Wander;
        stateTimer = wanderDuration;
        wanderDir = Random.insideUnitCircle.normalized;
        if (wanderDir.x != 0)
            spriteRenderer.flipX = wanderDir.x < 0;
        animator.Play(RunHash);
    }

    private void EnterIdle()
    {
        currentState = State.Idle;
        stateTimer = idleDuration + Random.Range(-0.5f, 0.5f);
        rb.linearVelocity = Vector2.zero;
        animator.Play(IdleHash);
    }

    private void EnterHidingIn()
    {
        currentState = State.HidingIn;
        rb.linearVelocity = Vector2.zero;
        showDelayTimer = showDelay;
        animator.Play(GuardInHash, 0, 0f);
    }

    private void EnterHidingOut()
    {
        currentState = State.HidingOut;
        animator.Play(GuardOutHash, 0, 0f);
    }

    private void FixedUpdate()
    {
        if (pushTimer > 0f)
        {
            pushTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = pushVelocity;
            return;
        }

        if (currentState == State.Wander)
            rb.linearVelocity = wanderDir * moveSpeed;
        else
            rb.linearVelocity = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hideRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, showRange);
    }
}
