using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float blockMoveMultiplier = 0.5f;

    [Header("Sprint")]
    [SerializeField] private float maxSprintSpeed = 5f;
    [SerializeField] private float sprintIncrement = 0.2f;
    [SerializeField] private float sprintDecay = 1f;
    [SerializeField] private float sprintDrainRate = 15f;

    [Header("Boundaries")]
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;

    [Header("Animation")]
    [SerializeField] private string idleState   = "Warrior_Idle_Black";
    [SerializeField] private string runState    = "Warrior_Run_Black";
    [SerializeField] private string attackState = "Warrior_Attack1_Black";
    [SerializeField] private float  attackDuration = 0.5f;

    [Header("Combat")]
    [SerializeField] private float attackHitDelay = 0.3f;
    [SerializeField] private float attackRadius = 0.8f;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackFrontDot = 0.3f;
    [SerializeField] private float attackAngleOffset = 0f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 4f;
    [SerializeField] private float knockbackDuration = 0.12f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerBlock playerBlock;
    private Stamina stamina;
    private Health health;

    private Vector2 knockbackVelocity;
    private float knockbackTimer;

    private Vector2 moveInput;
    private bool isAttacking;
    private float attackTimer;
    private bool hitDelivered;
    private float hitTimer;
    private float currentSpeed;
    private bool sprintToggled;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerBlock = GetComponent<PlayerBlock>();
        stamina = GetComponent<Stamina>();
        currentSpeed = moveSpeed;

        health = GetComponent<Health>();
        if (health != null)
        {
            health.OnDeath += OnPlayerDeath;
            health.OnDamaged += AplicarKnockback;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDeath -= OnPlayerDeath;
            health.OnDamaged -= AplicarKnockback;
        }
    }

    private void OnPlayerDeath()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void AplicarKnockback(Vector2 origen)
    {
        Vector2 dir = ((Vector2)transform.position - origen).normalized;
        if (dir == Vector2.zero) return;
        knockbackVelocity = dir * knockbackForce;
        knockbackTimer = knockbackDuration;
    }

    private void Update()
    {
        HandleAttack();
        HandleSprint();
        HandleMovement();
        HandleAnimation();
    }

    private void HandleAttack()
    {
        if (isAttacking)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                isAttacking = false;
                if (playerBlock != null && playerBlock.IsBlocking)
                    playerBlock.ReturnToBlock();
            }

            if (!hitDelivered)
            {
                hitTimer -= Time.deltaTime;
                if (hitTimer <= 0f)
                {
                    hitDelivered = true;
                    Vector2 facing = FacingAtaque();
                    Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRadius);
                    foreach (var hit in hits)
                    {
                        if (hit.gameObject == gameObject) continue;
                        if (!CombatUtils.EnFrente(transform.position, facing, hit.transform.position, attackFrontDot)) continue;
                        Health h = hit.GetComponent<Health>() ?? hit.GetComponentInParent<Health>();
                        h?.TakeDamage(attackDamage, transform.position);
                    }
                }
            }
            return;
        }

        bool attackPressed = Input.GetKeyDown(KeyCode.Space);
        var gamepad = Gamepad.current;
        if (gamepad != null)
            attackPressed |= gamepad.buttonWest.wasPressedThisFrame;

        if (attackPressed)
        {
            isAttacking = true;
            attackTimer = attackDuration;
            hitDelivered = false;
            hitTimer = attackHitDelay;
            animator.Play(attackState, 0, 0f);
        }
    }

    private void HandleSprint()
    {
        bool blocking = playerBlock != null && playerBlock.IsBlocking;
        if (isAttacking || blocking)
        {
            currentSpeed = moveSpeed;
            sprintToggled = false;
            return;
        }

        // Teclado: Shift es interruptor on/off.
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            sprintToggled = !sprintToggled;
            if (!sprintToggled) currentSpeed = moveSpeed;
        }

        // Mando: cada pulsacion sube la velocidad (con decaimiento).
        var gamepad = Gamepad.current;
        if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)
            currentSpeed = Mathf.Min(currentSpeed + sprintIncrement, maxSprintSpeed);

        bool moving = moveInput.sqrMagnitude > 0.01f;
        if (sprintToggled)
            currentSpeed = moving ? maxSprintSpeed : moveSpeed;

        if (currentSpeed > moveSpeed)
        {
            stamina?.Drain(sprintDrainRate * Time.deltaTime);
            if (stamina != null && stamina.IsEmpty)
            {
                currentSpeed = moveSpeed;
                sprintToggled = false;
            }
            else if (!sprintToggled)
                currentSpeed = Mathf.Max(currentSpeed - sprintDecay * Time.deltaTime, moveSpeed);
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

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))  x =  1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))   x = -1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))     y =  1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))   y = -1f;

        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 stick = gamepad.leftStick.ReadValue();
            if (Mathf.Abs(stick.x) > 0.2f) x = stick.x;
            if (Mathf.Abs(stick.y) > 0.2f) y = stick.y;
        }

        moveInput = new Vector2(x, y).normalized;

        bool blocking = playerBlock != null && playerBlock.IsBlocking;
        if (!blocking && x != 0)
            spriteRenderer.flipX = x < 0;
    }

    private void HandleAnimation()
    {
        if (isAttacking) return;
        if (playerBlock != null && playerBlock.IsBlocking) return;

        string targetState = moveInput != Vector2.zero ? runState : idleState;
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(targetState))
            animator.Play(targetState, 0, 0f);
    }

    private void FixedUpdate()
    {
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = knockbackVelocity;
            return;
        }

        bool blocking = playerBlock != null && playerBlock.IsBlocking;
        float speed = blocking ? moveSpeed * blockMoveMultiplier : currentSpeed;
        rb.linearVelocity = moveInput * speed;

        if (minBounds != maxBounds)
        {
            Vector2 pos = rb.position;
            pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
            pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
            rb.position = pos;
        }
    }

    private Vector2 FacingAtaque()
    {
        SpriteRenderer sr = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
        Vector2 f = Quaternion.Euler(0f, 0f, attackAngleOffset) * Vector3.right;
        if (sr != null && sr.flipX) f.x = -f.x;
        return f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
        Gizmos.color = Color.magenta;
        CombatUtils.DibujarCono(transform.position, FacingAtaque(), attackFrontDot, attackRadius);
    }
}
