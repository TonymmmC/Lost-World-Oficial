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

    private static readonly int IdleHash     = Animator.StringToHash("Turtle_Idle");
    private static readonly int RunHash      = Animator.StringToHash("Turtle_Run");
    private static readonly int GuardInHash  = Animator.StringToHash("Turtle_Guard_In");
    private static readonly int GuardOutHash = Animator.StringToHash("Turtle_Guard_Out");

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform player;

    private State currentState = State.Idle;
    private Vector2 wanderDir;
    private float stateTimer;
    private float showDelayTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindWithTag("Player")?.transform;
        if (player == null) Debug.LogError($"{name}: no encontro al Player — verifica que el tag sea exactamente 'Player'");
    }

    private void Start()
    {
        EnterIdle();
    }

    private void Update()
    {
        if (player == null) return;

        float distToPlayer = Vector2.Distance(transform.position, player.position);

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
        if (currentState == State.Wander)
            rb.linearVelocity = wanderDir * moveSpeed;
        else if (currentState != State.Hidden)
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
