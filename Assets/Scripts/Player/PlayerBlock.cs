using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBlock : MonoBehaviour
{
    [SerializeField] private string shieldState = "Warrior_Guard_Black";
    [SerializeField] private float blockDrainPerHit = 25f;

    private Animator animator;
    private Health health;
    private Stamina stamina;
    private int shieldHash;

    public bool IsBlocking { get; private set; }

    public void ReturnToBlock() => animator.Play(shieldHash, 0, 0f);

    private void Awake()
    {
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();
        stamina = GetComponent<Stamina>();
        shieldHash = Animator.StringToHash(shieldState);

        if (health != null)
        {
            health.BlockCheck = () => IsBlocking && (stamina == null || !stamina.IsEmpty);
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
