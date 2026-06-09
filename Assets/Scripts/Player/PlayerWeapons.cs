using UnityEngine;
using UnityEngine.InputSystem;

public enum TipoAtaque { Melee, Arco }

// Una forma/arma del jugador. El mago sin identidad cambia entre estas (y a futuro,
// roba la del enemigo que mata). Cada forma trae su propio Animator Controller y stats.
[System.Serializable]
public class PlayerForma
{
    public string nombre = "Guerrero";
    public RuntimeAnimatorController controller;

    [Header("Estados de animacion")]
    public string idleState;
    public string runState;
    public string attackState;
    public string attackState2; // vacio = sin combo (arquero, lancero)

    [Header("Ataque")]
    public TipoAtaque tipoAtaque = TipoAtaque.Melee;
    public int danio = 1;
    public float alcance = 0.8f;        // radio del cono melee
    public float frontDot = 0.3f;       // ancho del cono (mayor = mas angosto)
    public float duracionAtaque = 0.5f;
    public float delayGolpe = 0.3f;

    [Header("Solo arco")]
    public EnemyProjectile proyectilPrefab;
    public float velocidadProyectil = 9f;
}

// Arsenal del jugador: maneja el cambio de forma (cruceta o teclas 1/2/3), intercambia
// el Animator Controller y dispara las flechas del arquero. PlayerMovement lo consulta
// para saber que animaciones y stats de ataque usar segun la forma activa.
[RequireComponent(typeof(Animator))]
public class PlayerWeapons : MonoBehaviour
{
    // Faccion del jugador: distinta a la de los enemigos (0) para que la flecha los dañe.
    private const int FaccionJugador = 99;

    [SerializeField] private PlayerForma[] formas;
    [SerializeField] private Transform puntoDisparo;
    [SerializeField] private float offsetDisparo = 0.5f;

    private Animator animator;
    private Collider2D selfCollider;
    private int indice;
    private ProjectilePool poolFlechas;

    public bool Listo => formas != null && formas.Length > 0;
    public PlayerForma Forma => formas[indice];

    private void Awake()
    {
        animator = GetComponent<Animator>();
        selfCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        if (Listo) AplicarForma(0);
    }

    private void Update()
    {
        if (Listo) LeerCambioDeForma();
    }

    private void LeerCambioDeForma()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) AplicarForma(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) AplicarForma(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) AplicarForma(2);

        var gp = Gamepad.current;
        if (gp == null) return;
        if (gp.dpad.up.wasPressedThisFrame)    AplicarForma(0); // guerrero
        if (gp.dpad.left.wasPressedThisFrame)  AplicarForma(1); // arquero
        if (gp.dpad.right.wasPressedThisFrame) AplicarForma(2); // lancero
    }

    private void AplicarForma(int i)
    {
        if (i < 0 || i >= formas.Length || i == indice) return;
        indice = i;
        PlayerForma f = formas[i];
        if (f.controller != null) animator.runtimeAnimatorController = f.controller;
        poolFlechas = (f.tipoAtaque == TipoAtaque.Arco && f.proyectilPrefab != null)
            ? new ProjectilePool(f.proyectilPrefab) : null;
        if (!string.IsNullOrEmpty(f.idleState)) animator.Play(f.idleState, 0, 0f);
    }

    // Lo llama PlayerMovement en el momento de impacto si la forma es Arco.
    public void Disparar(Vector2 direccion)
    {
        if (poolFlechas == null) return;
        Vector2 origen = puntoDisparo != null
            ? (Vector2)puntoDisparo.position
            : (Vector2)transform.position + direccion.normalized * offsetDisparo;
        EnemyProjectile flecha = poolFlechas.Obtener(origen, Quaternion.identity);
        flecha.Lanzar(origen, direccion, Forma.danio, Forma.velocidadProyectil, FaccionJugador, poolFlechas.Devolver, selfCollider);
    }

#if UNITY_EDITOR
    // Clic derecho en el componente -> "Autollenar formas Blue": busca los controllers
    // Warrior/Archer/Lancer Blue y rellena las 3 formas con sus estados y stats. Solo
    // queda asignar a mano el prefab de flecha del arquero (no existe hasta crearlo).
    [ContextMenu("Autollenar formas Blue")]
    private void AutollenarFormas()
    {
        formas = new[]
        {
            CrearForma("Guerrero", "Warrior_Blue", "Warrior_Idle_Blue", "Warrior_Run_Blue", "Warrior_Attack_Blue", "Warrior_Attack2_Blue", TipoAtaque.Melee, 1, 0.8f, 0.3f),
            CrearForma("Arquero",  "Archer_Blue",  "Archer_Idle_Blue",  "Archer_Run_Blue",  "Archer_Shoot_Blue",  "",                    TipoAtaque.Arco,  1, 0.8f, 0.3f),
            CrearForma("Lancero",  "Lancer_Blue",  "Lancer_Idle_Blue",  "Lancer_ Run_Blue", "Lancer_ Right_Attack_Blue", "",             TipoAtaque.Melee, 2, 1.6f, 0.5f),
        };
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("PlayerWeapons: formas autollenadas. Falta asignar el Proyectil Prefab del Arquero.");
    }

    private static PlayerForma CrearForma(string nombre, string controllerName, string idle, string run, string atk, string atk2, TipoAtaque tipo, int danio, float alcance, float frontDot)
    {
        return new PlayerForma
        {
            nombre = nombre,
            controller = BuscarController(controllerName),
            idleState = idle, runState = run, attackState = atk, attackState2 = atk2,
            tipoAtaque = tipo, danio = danio, alcance = alcance, frontDot = frontDot,
        };
    }

    private static RuntimeAnimatorController BuscarController(string nombre)
    {
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets($"{nombre} t:AnimatorController"))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == nombre)
                return UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
        }
        return null;
    }
#endif
}
