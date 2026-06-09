using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Intro de historia: muestra 3 imagenes a pantalla completa, una a una, con un
// fundido a negro entre cada una. Se avanza con Espacio/Enter/click en PC o con
// el boton X del mando (buttonSouth). Tras la ultima carga el gameplay.
// Se construye por codigo para no depender de Canvas montados en escena.
public class StorySequence : MonoBehaviour
{
    private const string EscenaJuego = "World";
    private const string RutaImagenes = "Historia/Historia";
    private const int CantidadImagenes = 3;
    private const float DuracionFade = 0.5f;

    private readonly Texture2D[] texturas = new Texture2D[CantidadImagenes];
    private RawImage imagen;
    private AspectRatioFitter fitter;
    private int indice;
    private bool bloqueado;

    public static void Iniciar()
    {
        var go = new GameObject("StorySequence");
        go.AddComponent<StorySequence>();
    }

    private void Start()
    {
        CargarTexturas();
        ConstruirUI();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        MostrarTextura(0);
        StartCoroutine(FadeEntrada());
    }

    private void Update()
    {
        if (bloqueado) return;
        if (AvancePresionado())
            Avanzar();
    }

    private void CargarTexturas()
    {
        for (int i = 0; i < CantidadImagenes; i++)
            texturas[i] = Resources.Load<Texture2D>(RutaImagenes + (i + 1));
    }

    private bool AvancePresionado()
    {
        if (Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
            return true;
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
        return false;
    }

    private void Avanzar()
    {
        indice++;
        if (indice >= CantidadImagenes)
            StartCoroutine(Terminar());
        else
            StartCoroutine(Transicion(indice));
    }

    private IEnumerator Transicion(int siguiente)
    {
        bloqueado = true;
        yield return Fade(1f, 0f);
        MostrarTextura(siguiente);
        yield return Fade(0f, 1f);
        bloqueado = false;
    }

    private IEnumerator FadeEntrada()
    {
        bloqueado = true;
        yield return Fade(0f, 1f);
        bloqueado = false;
    }

    private IEnumerator Terminar()
    {
        bloqueado = true;
        yield return Fade(1f, 0f);
        SceneManager.LoadScene(EscenaJuego);
    }

    private void MostrarTextura(int i)
    {
        imagen.texture = texturas[i];
        if (texturas[i] != null && texturas[i].height > 0)
            fitter.aspectRatio = (float)texturas[i].width / texturas[i].height;
    }

    private IEnumerator Fade(float desde, float hasta)
    {
        float t = 0f;
        while (t < DuracionFade)
        {
            t += Time.unscaledDeltaTime;
            FijarAlpha(Mathf.Lerp(desde, hasta, t / DuracionFade));
            yield return null;
        }
        FijarAlpha(hasta);
    }

    private void FijarAlpha(float a)
    {
        Color c = imagen.color;
        c.a = a;
        imagen.color = c;
    }

    private void ConstruirUI()
    {
        GameObject root = CrearCanvas();
        CrearFondoNegro(root.transform);
        CrearImagen(root.transform);
        CrearHint(root.transform);
    }

    private GameObject CrearCanvas()
    {
        var go = new GameObject("StoryOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        return go;
    }

    private void CrearFondoNegro(Transform padre)
    {
        var go = new GameObject("Fondo", typeof(Image));
        go.transform.SetParent(padre, false);
        go.GetComponent<Image>().color = Color.black;
        Estirar(go.GetComponent<RectTransform>());
    }

    private void CrearImagen(Transform padre)
    {
        var go = new GameObject("Imagen", typeof(RawImage), typeof(AspectRatioFitter));
        go.transform.SetParent(padre, false);
        imagen = go.GetComponent<RawImage>();
        imagen.color = new Color(1f, 1f, 1f, 0f);
        fitter = go.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        Estirar(go.GetComponent<RectTransform>());
    }

    private void CrearHint(Transform padre)
    {
        var go = new GameObject("Hint", typeof(Text));
        go.transform.SetParent(padre, false);
        var txt = go.GetComponent<Text>();
        txt.text = "Continuar  (X / Espacio)";
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 28;
        txt.color = new Color(1f, 1f, 1f, 0.8f);
        txt.alignment = TextAnchor.LowerRight;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(0, 40);
        rt.offsetMax = new Vector2(-60, 0);
    }

    private static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
