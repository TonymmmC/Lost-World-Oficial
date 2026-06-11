using UnityEngine;
using UnityEngine.InputSystem;

// Recoloca el HUD segun el tipo de entrada: en PC queda donde lo dejo la escena (abajo-izquierda);
// en movil sube a la esquina configurada para no chocar con el joystick tactil.
// Va en el RectTransform raiz del HUD.
[RequireComponent(typeof(RectTransform))]
public class HudPosicionAdaptable : MonoBehaviour
{
    [SerializeField] private Vector2 esquinaTactil = new Vector2(0, 1);            // 0,1 = arriba-izquierda
    [SerializeField] private Vector2 posicionTactil = new Vector2(151.2f, -111.4f);
    [SerializeField] private bool forzarTactil; // para probar el reacomodo en el editor

    private RectTransform rt;
    private Vector2 anchorPc;
    private Vector2 posicionPc;

    private void Awake()
    {
        rt = (RectTransform)transform;
        anchorPc = rt.anchorMin;          // se asume anchorMin == anchorMax
        posicionPc = rt.anchoredPosition; // el layout de PC es el que ya tiene la escena
        InputSystem.onDeviceChange += AlCambiarDispositivo;
        Aplicar();
    }

    private void OnDestroy() => InputSystem.onDeviceChange -= AlCambiarDispositivo;

    // Reacomodar al conectar/desconectar un mando en caliente.
    private void AlCambiarDispositivo(InputDevice device, InputDeviceChange change) => Aplicar();

    private void Aplicar()
    {
        bool tactil = forzarTactil || EntradaTactil.Activa;
        rt.anchorMin = rt.anchorMax = tactil ? esquinaTactil : anchorPc;
        rt.anchoredPosition = tactil ? posicionTactil : posicionPc;
    }
}
