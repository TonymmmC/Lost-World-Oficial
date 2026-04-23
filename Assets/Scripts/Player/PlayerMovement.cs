using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Boundaries")]
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;

    [Header("Animation")]
    [SerializeField] private string idleState   = "Warrior_Idle_Black";
    [SerializeField] private string runState    = "Warrior_Run_Black";
    [SerializeField] private string attackState = "Warrior_Attack1_Black";
    [SerializeField] private float  attackDuration = 0.5f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    [Header("Combat")]
    [SerializeField] private float attackHitDelay = 0.3f;
    [SerializeField] private float attackRadius = 0.8f;
    [SerializeField] private int attackDamage = 1;

    private Vector2 moveInput;
    private bool isAttacking;
    private float attackTimer;
    private bool hitDelivered;
    private float hitTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        Health health = GetComponent<Health>();
        if (health != null)
            health.OnDeath += OnPlayerDeath;
    }

    private void OnPlayerDeath()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void Update()
    {
        HandleAttack();
        HandleMovement();
        HandleAnimation();
    }

    private void HandleAttack()
    {
        if (isAttacking)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
                isAttacking = false;

            if (!hitDelivered)
            {
                hitTimer -= Time.deltaTime;
                if (hitTimer <= 0f)
                {
                    hitDelivered = true;
                    Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRadius);
                    foreach (var hit in hits)
                    {
                        if (hit.gameObject == gameObject) continue;
                        Health h = hit.GetComponent<Health>() ?? hit.GetComponentInParent<Health>();
                        h?.TakeDamage(attackDamage);
                    }
                }
            }
            return;
        }

        bool attackPressed = Input.GetKeyDown(KeyCode.Space);
        var gamepad = Gamepad.current;
        if (gamepad != null)
            attackPressed |= gamepad.buttonSouth.wasPressedThisFrame
                          || gamepad.buttonWest.wasPressedThisFrame;

        if (attackPressed)
        {
            isAttacking = true;
            attackTimer = attackDuration;
            hitDelivered = false;
            hitTimer = attackHitDelay;
            animator.Play(attackState, 0, 0f);
        }
    }

    private void HandleMovement()
    {
        if (isAttacking)
        {
            moveInput = Vector2.zero;
            return;
        }

        float x = 0f;
        float y = 0f;

        // Teclado
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))  x =  1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))   x = -1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))     y =  1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))   y = -1f;

        // Mando
        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 stick = gamepad.leftStick.ReadValue();
            if (Mathf.Abs(stick.x) > 0.2f) x = stick.x;
            if (Mathf.Abs(stick.y) > 0.2f) y = stick.y;
        }

        moveInput = new Vector2(x, y).normalized;

        if (x != 0)
            spriteRenderer.flipX = x < 0;
    }

    private void HandleAnimation()
    {
        if (isAttacking) return;

        string targetState = moveInput != Vector2.zero ? runState : idleState;
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(targetState))
            animator.Play(targetState, 0, 0f);
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;

        if (minBounds != maxBounds)
        {
            Vector2 pos = rb.position;
            pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
            pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
            rb.position = pos;
        }
    }
}
