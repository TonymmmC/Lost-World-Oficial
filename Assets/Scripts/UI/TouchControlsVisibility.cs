using UnityEngine;
using UnityEngine.InputSystem;

// Muestra el contenedor de controles tactiles solo cuando hay pantalla tactil (build movil).
// En PC permanece oculto para no estorbar al teclado/mando. Vive en el Canvas tactil.
public class TouchControlsVisibility : MonoBehaviour
{
    [SerializeField] private GameObject contenedor;
    [SerializeField] private bool forzarVisible; // para probar el layout en el editor

    private void Awake()
    {
        if (contenedor == null) { Debug.LogError($"{name}: falta el contenedor de controles"); return; }
        InputSystem.onDeviceChange += AlCambiarDispositivo;
        Evaluar();
    }

    private void OnDestroy()
    {
        InputSystem.onDeviceChange -= AlCambiarDispositivo;
    }

    // Reevaluar al conectar/desconectar un mando: si entra un mando, ocultar los controles tactiles.
    private void AlCambiarDispositivo(InputDevice device, InputDeviceChange change) => Evaluar();

    private void Evaluar()
    {
        if (contenedor == null) return;
        bool debe = forzarVisible || DebeMostrar();
        // Solo cambiar si hace falta: al activar/desactivar, los OnScreen disparan onDeviceChange
        // que reentra aqui; un SetActive redundante a media (des)activacion lanza error de Unity.
        if (contenedor.activeSelf != debe) contenedor.SetActive(debe);
    }

    private bool DebeMostrar() => EntradaTactil.Activa;
}
