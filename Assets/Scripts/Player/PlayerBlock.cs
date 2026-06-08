using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBlock : MonoBehaviour
{
    [SerializeField] private string shieldState = "Warrior_Guard_Black";
    [SerializeField] private float blockDrainPerHit = 25f;
    [SerializeField] private float blockFrontDot = 0f;

    private Animator animator;
    private Health health;
    private Stamina stamina;
    private SpriteRenderer spriteRenderer;
    private int shieldHash;

    public bool IsBlocking { get; private set; }

    public void ReturnToBlock() => animator.Play(shieldHash, 0, 0f);

    private void Awake()
    {
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();
        stamina = GetComponent<Stamina>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        shieldHash = Animator.StringToHash(shieldState);

        if (health != null)
        {
            health.BlockCheck = FrenteProtegido;
            health.OnBlocked += OnHitBlocked;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnBlocked -= OnHitBlocked;
    }

    private void OnHitBlocked()
    {
        stamina?.Drain(blockDrainPerHit);
        if (stamina != null && stamina.IsEmpty)
            IsBlocking = false;
    }

    private bool FrenteProtegido(Vector2 origen)
    {
        if (!IsBlocking) return false;
        if (stamina != null && stamina.IsEmpty) return false;
        Vector2 facing = spriteRenderer.flipX ? Vector2.left : Vector2.right;
        return CombatUtils.EnFrente(transform.position, facing, origen, blockFrontDot);
    }

    private void Update()
    {
        bool blockHeld = false;
        var gamepad = Gamepad.current;
        if (gamepad != null)
            blockHeld = gamepad.leftShoulder.isPressed;

        if (stamina != null && stamina.IsEmpty)
            blockHeld = false;

        bool wasBlocking = IsBlocking;
        IsBlocking = blockHeld;
        if (IsBlocking && !wasBlocking)
            animator.Play(shieldHash, 0, 0f);
    }
}
