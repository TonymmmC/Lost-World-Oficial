using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(StatusLentitud))]
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
    [SerializeField] private string attackState  = "Warrior_Attack1_Black";
    [SerializeField] private string attackState2 = "Warrior_Attack2_Black";

    [Header("Combat")]
    [SerializeField] private float attackHitDelay = 0.3f;
    [SerializeField] private float attackRadius = 0.8f;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackFrontDot = 0.3f;
    [SerializeField] private float attackAngleOffset = 0f;
    [SerializeField] private bool ataqueCircular = true; // true = golpea 360 (Wukong gira el baston)

    [Header("Ataque fuerte")]
    [SerializeField] private string heavyAttackState = "Wuko_Heavy_Attack";
    [SerializeField] private float heavyHitDelay = 0.4f;
    [SerializeField] private float heavyRadius = 1.1f;
    [SerializeField] private int heavyDamage = 4;
    [SerializeField] private float heavyCooldown = 1.2f;
    [SerializeField] private float heavyHitStop = 0.12f;
    [SerializeField] private float heavyShakeIntensidad = 0.35f;
    [SerializeField] private float heavyShakeDuracion = 0.2f;

    [Header("Curar")]
    [SerializeField] private string healState = "Wuko_heal";
    [SerializeField] private float healCooldown = 8f;
    [SerializeField] private GameObject healEffectPrefab;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 4f;
    [SerializeField] private float knockbackDuration = 0.12f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerBlock playerBlock;
    private Stamina stamina;
    private Health health;
    private StatusLentitud lentitud;
    private GameOverScreen gameOverScreen;
    private PlayerWeapons weapons;

    private Vector2 knockbackVelocity;
    private float knockbackTimer;

    private Vector2 moveInput;
    private bool isAttacking;
    private float attackTimer;
    private bool hitDelivered;
    private float hitTimer;
    private float currentSpeed;
    private bool sprintToggled;
    private bool usarAtaque2;
    private bool atacandoFuerte;
    private float heavyTimer;
    private bool curando;
    private float curaTimer;
    private float healCooldownTimer;
    private string accionState;          // estado de animacion de la accion en curso
    private const float SafetyMax = 3f;  // tope por si la animacion no termina (clip en loop / nombre mal)

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerBlock = GetComponent<PlayerBlock>();
        stamina = GetComponent<Stamina>();
        weapons = GetComponent<PlayerWeapons>();
        lentitud = GetComponent<StatusLentitud>();
        currentSpeed = moveSpeed;

        gameOverScreen = FindAnyObjectByType<GameOverScreen>(FindObjectsInactive.Include);
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
        if (gameOverScreen != null)
            gameOverScreen.Show();
        else
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

    // Getters que respetan la forma activa (PlayerWeapons). Sin formas: usa los campos
    // serializados de este script como fallback (guerrero melee).
    private bool ConFormas => weapons != null && weapons.enabled && weapons.Listo;
    private string EstadoIdle => ConFormas ? weapons.Forma.idleState : idleState;
    private string EstadoRun  => ConFormas ? weapons.Forma.runState  : runState;
    private float DelayGolpe => ConFormas ? weapons.Forma.delayGolpe : attackHitDelay;

    private void HandleAttack()
    {
        if (heavyTimer > 0f) heavyTimer -= Time.deltaTime;
        if (healCooldownTimer > 0f) healCooldownTimer -= Time.deltaTime;

        if (isAttacking) { ActualizarAtaque(); return; }
        if (curando) { ActualizarCura(); return; }

        if (CuraPresionada() && healCooldownTimer <= 0f) { IniciarCura(); return; }
        if (FuertePresionado() && heavyTimer <= 0f) { IniciarAtaque(true); return; }
        if (AtaquePresionado()) IniciarAtaque(false);
    }

    private void ActualizarAtaque()
    {
        attackTimer -= Time.deltaTime;
        if (AnimacionTermino(accionState) || attackTimer <= 0f)
        {
            isAttacking = false;
            if (playerBlock != null && playerBlock.IsBlocking) playerBlock.ReturnToBlock();
        }
        if (hitDelivered) return;
        hitTimer -= Time.deltaTime;
        if (hitTimer <= 0f) { hitDelivered = true; EjecutarGolpe(); }
    }

    // True cuando ya estamos en la animacion de la accion y llego al final.
    private bool AnimacionTermino(string estado)
    {
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        return info.IsName(estado) && info.normalizedTime >= 0.97f;
    }

    private bool AtaquePresionado()
    {
        bool p = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J);
        var gamepad = Gamepad.current;
        if (gamepad != null) p |= gamepad.buttonWest.wasPressedThisFrame; // X
        return p;
    }

    private bool FuertePresionado()
    {
        bool p = Input.GetKeyDown(KeyCode.K);
        var gamepad = Gamepad.current;
        if (gamepad != null) p |= gamepad.buttonNorth.wasPressedThisFrame; // Y
        return p;
    }

    private bool CuraPresionada()
    {
        bool p = Input.GetKeyDown(KeyCode.H);
        var gamepad = Gamepad.current;
        if (gamepad != null) p |= gamepad.buttonEast.wasPressedThisFrame; // B
        return p;
    }

    private void IniciarAtaque(bool fuerte)
    {
        isAttacking = true;
        atacandoFuerte = fuerte;
        hitDelivered = false;
        attackTimer = SafetyMax;
        accionState = fuerte ? heavyAttackState : ElegirEstadoAtaque();
        hitTimer = fuerte ? heavyHitDelay : DelayGolpe;
        if (fuerte) heavyTimer = heavyCooldown;
        animator.Play(accionState, 0, 0f);
    }

    private void IniciarCura()
    {
        curando = true;
        curaTimer = SafetyMax;
        accionState = healState;
        healCooldownTimer = healCooldown;
        animator.Play(healState, 0, 0f);
        if (health != null) health.Curar(health.Max); // cura completa
        if (healEffectPrefab != null)
            Instantiate(healEffectPrefab, transform.position, Quaternion.identity);
    }

    private void ActualizarCura()
    {
        curaTimer -= Time.deltaTime;
        if (AnimacionTermino(accionState) || curaTimer <= 0f) curando = false;
    }

    private string ElegirEstadoAtaque()
    {
        string a1 = ConFormas ? weapons.Forma.attackState  : attackState;
        string a2 = ConFormas ? weapons.Forma.attackState2 : attackState2;
        if (string.IsNullOrEmpty(a2)) return a1; // sin combo (arquero, lancero)
        string elegido = usarAtaque2 ? a2 : a1;  // alterna para sensacion de combo
        usarAtaque2 = !usarAtaque2;
        return elegido;
    }

    private void EjecutarGolpe()
    {
        Vector2 facing = FacingAtaque();
        if (atacandoFuerte)
        {
            if (GolpearArea(facing, heavyRadius, attackFrontDot, heavyDamage)) ImpactoFuerte();
            return;
        }
        if (ConFormas && weapons.Forma.tipoAtaque == TipoAtaque.Arco) { weapons.Disparar(facing); return; }
        GolpearArea(facing,
            ConFormas ? weapons.Forma.alcance : attackRadius,
            ConFormas ? weapons.Forma.frontDot : attackFrontDot,
            ConFormas ? weapons.Forma.danio : attackDamage);
    }

    // Jugo del golpe pesado al conectar: hit stop mas largo + sacudida de camara = peso.
    private void ImpactoFuerte()
    {
        if (HitStop.Instance != null) HitStop.Instance.Aplicar(heavyHitStop);
        if (CameraFollow.Instance != null) CameraFollow.Instance.Sacudir(heavyShakeIntensidad, heavyShakeDuracion);
    }

    private bool GolpearArea(Vector2 facing, float radio, float dot, int dano)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radio);
        bool golpeo = false;
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (!ataqueCircular && !CombatUtils.EnFrente(transform.position, facing, hit.transform.position, dot)) continue;
            Health h = hit.GetComponent<Health>() ?? hit.GetComponentInParent<Health>();
            if (h != null) { h.TakeDamage(dano, transform.position); golpeo = true; }
        }
        return golpeo;
    }

    private void HandleSprint()
    {
        bool blocking = playerBlock != null && playerBlock.IsBlocking;
        if (isAttacking || curando || blocking)
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
        if (isAttacking || curando)
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
            if (stick.sqrMagnitude > 0.2f * 0.2f) { x = stick.x; y = stick.y; } // zona muerta por magnitud
        }

        moveInput = new Vector2(x, y).normalized;

        // Solo voltear con un X claro: si no, el ruido del stick al correr vertical
        // hace parpadear el flip izquierda/derecha cada frame.
        bool blocking = playerBlock != null && playerBlock.IsBlocking;
        if (!blocking && Mathf.Abs(x) > 0.3f)
            spriteRenderer.flipX = x < 0;
    }

    private void HandleAnimation()
    {
        if (isAttacking || curando) return;
        if (playerBlock != null && playerBlock.IsBlocking) return;

        string targetState = moveInput != Vector2.zero ? EstadoRun : EstadoIdle;
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
        rb.linearVelocity = moveInput * speed * lentitud.Multiplicador;

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
        Gizmos.color = Color.magenta;
        if (ataqueCircular)
            Gizmos.DrawWireSphere(transform.position, attackRadius);
        else
            CombatUtils.DibujarCono(transform.position, FacingAtaque(), attackFrontDot, attackRadius);
    }
}
