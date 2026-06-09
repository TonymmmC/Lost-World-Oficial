using UnityEngine;

// Robo de forma estilo Wukong: guarda la forma del ultimo enemigo que el jugador mata y,
// al pulsar el boton de transformacion, adopta su Animator Controller y sus stats de ataque
// durante unos segundos. Al terminar vuelve a Wukong. PlayerMovement consulta Forma/Activa
// para animar y golpear con la forma adoptada. Una responsabilidad: gestionar la forma robada.
[RequireComponent(typeof(Animator))]
public class PlayerTransformacion : MonoBehaviour
{
    [SerializeField] private float duracion = 10f;
    [SerializeField] private float delayGolpeRobado = 0.3f;
    [SerializeField] private GameObject efectoPrefab; // humo al transformar (opcional)

    private Animator animator;
    private RuntimeAnimatorController controllerOriginal;
    private Vector3 escalaOriginal;
    private Vector3 escalaForma;
    private PlayerForma formaRobada;
    private float timer;

    public bool Activa => timer > 0f;
    public bool TieneForma => formaRobada != null;
    public PlayerForma Forma => formaRobada;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controllerOriginal = animator.runtimeAnimatorController;
        escalaOriginal = transform.localScale;
        escalaForma = escalaOriginal;
    }

    private void Update()
    {
        if (timer <= 0f) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) Revertir();
    }

    // La llama PlayerMovement al matar a un enemigo: copia su forma para usarla luego.
    public void RobarForma(EnemyAI enemigo)
    {
        if (enemigo == null || enemigo.Controller == null) return;
        // Adopta el tamano real del enemigo (en valor absoluto: el flip lo maneja flipX).
        Vector3 s = enemigo.transform.lossyScale;
        escalaForma = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), 1f);
        formaRobada = new PlayerForma
        {
            nombre = enemigo.name,
            controller = enemigo.Controller,
            idleState = enemigo.IdleAnim,
            runState = enemigo.RunAnim,
            attackState = enemigo.AttackAnim,
            attackState2 = "",
            tipoAtaque = TipoAtaque.Melee,
            danio = enemigo.DanioAtaque,
            alcance = enemigo.AlcanceAtaque,
            frontDot = 0.3f,
            delayGolpe = delayGolpeRobado,
        };
    }

    // La llama PlayerMovement con el boton de transformacion, solo cuando el jugador esta libre.
    public void Alternar()
    {
        if (Activa) Revertir();
        else Transformar();
    }

    private void Transformar()
    {
        if (formaRobada == null || formaRobada.controller == null) return;
        animator.runtimeAnimatorController = formaRobada.controller;
        transform.localScale = escalaForma;
        if (!string.IsNullOrEmpty(formaRobada.idleState))
            animator.Play(formaRobada.idleState, 0, 0f);
        timer = duracion;
        if (efectoPrefab != null)
            Instantiate(efectoPrefab, transform.position, Quaternion.identity);
    }

    private void Revertir()
    {
        timer = 0f;
        animator.runtimeAnimatorController = controllerOriginal;
        transform.localScale = escalaOriginal;
        if (efectoPrefab != null)
            Instantiate(efectoPrefab, transform.position, Quaternion.identity);
    }
}
