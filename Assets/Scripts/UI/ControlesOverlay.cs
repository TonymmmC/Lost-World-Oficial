using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Muestra a pantalla completa la imagen de controles (Resources/Controls/controls).
// Pausa el juego mientras se lee y se cierra con cualquier tecla, toque, click o
// boton de mando. Se auto-muestra una vez por sesion al entrar al World (tras la
// historia) y el menu de pausa la reabre con ControlesOverlay.Mostrar().
// Se construye por codigo para no depender de Canvas montados en escena.
public class ControlesOverlay : MonoBehaviour
{
    private const string EscenaJuego = "World";
    private const string RutaImagen = "Controls/controls";

    private static bool yaMostrado;

    private float escalaPrevia;
    private int frameCreacion;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Registrar()
    {
        SceneManager.sceneLoaded += AlCargarEscena;
        Scene activa = SceneManager.GetActiveScene();
        if (activa.name == EscenaJuego) AlCargarEscena(activa, LoadSceneMode.Single);
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (escena.name != EscenaJuego || yaMostrado) return;
        yaMostrado = true;
        Mostrar();
    }

    public static void Mostrar()
    {
        var go = new GameObject("ControlesOverlay");
        go.AddComponent<ControlesOverlay>();
    }

    private void Start()
    {
        escalaPrevia = Time.timeScale; // 1 sobre el juego, 0 si viene de la pausa
        Time.timeScale = 0f;
        frameCreacion = Time.frameCount;
        ConstruirUI();
    }

    private void Update()
    {
        if (Time.frameCount <= frameCreacion) return; // ignorar el toque que la abrio
        if (CierrePresionado()) Cerrar();
    }

    private void Cerrar()
    {
        Time.timeScale = escalaPrevia;
        Destroy(gameObject);
    }

    private bool CierrePresionado()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
        if (Gamepad.current != null &&
            (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame))
            return true;
        return false;
    }

    private void ConstruirUI()
    {
        GameObject root = CrearCanvas();
        CrearFondoNegro(root.transform);
        CrearImagen(root.transform);
    }

    private GameObject CrearCanvas()
    {
        var go = new GameObject("ControlesCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000; // por encima del HUD, controles tactiles y menu de pausa
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        return go;
    }

    private void CrearFondoNegro(Transform padre)
    {
        var go = new GameObject("Fondo", typeof(Image));
        go.transform.SetParent(padre, false);
        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
        Estirar(go.GetComponent<RectTransform>());
    }

    private void CrearImagen(Transform padre)
    {
        var texto = Resources.Load<Texture2D>(RutaImagen);
        if (texto == null) { Debug.LogError($"ControlesOverlay: falta {RutaImagen} en Resources"); return; }

        var go = new GameObject("Imagen", typeof(RawImage), typeof(AspectRatioFitter));
        go.transform.SetParent(padre, false);
        go.GetComponent<RawImage>().texture = texto;
        var fitter = go.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = (float)texto.width / texto.height;
        Estirar(go.GetComponent<RectTransform>());
    }

    private static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
