using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Construye menus de overlay por codigo (pausa, game over) sin depender de
// Canvas montados a mano en la escena. Excepcion consciente a la regla de
// "prefabs para todo lo instanciado": es UI de arranque, una sola vez, no
// objetos de gameplay que se pooleen. Funciona a timeScale 0 y deja el primer
// boton seleccionado para navegacion con mando y teclado.
public static class MenuUI
{
    private static readonly Color FondoColor = new Color(0f, 0f, 0f, 0.75f);
    private static readonly Color BotonColor = new Color(0.15f, 0.15f, 0.18f, 1f);
    private static readonly Color TextoColor = Color.white;

    public struct Opcion
    {
        public string Texto;
        public Action AlPulsar;
        public Opcion(string texto, Action alPulsar) { Texto = texto; AlPulsar = alPulsar; }
    }

    // Crea el overlay completo y devuelve su raiz para poder destruirlo despues.
    public static GameObject Construir(string titulo, params Opcion[] opciones)
    {
        AsegurarEventSystem();
        GameObject root = CrearCanvas();
        CrearFondo(root.transform);
        CrearTitulo(root.transform, titulo);
        GameObject primero = CrearBotones(root.transform, opciones);

        if (primero != null)
            EventSystem.current.SetSelectedGameObject(primero);

        return root;
    }

    private static void AsegurarEventSystem()
    {
        if (EventSystem.current != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        UnityEngine.Object.DontDestroyOnLoad(go);
    }

    private static GameObject CrearCanvas()
    {
        var go = new GameObject("MenuOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        return go;
    }

    private static void CrearFondo(Transform padre)
    {
        var go = new GameObject("Fondo", typeof(Image));
        go.transform.SetParent(padre, false);
        go.GetComponent<Image>().color = FondoColor;
        Estirar(go.GetComponent<RectTransform>());
    }

    private static void CrearTitulo(Transform padre, string titulo)
    {
        var go = new GameObject("Titulo", typeof(Text));
        go.transform.SetParent(padre, false);
        var txt = ConfigurarTexto(go.GetComponent<Text>(), titulo, 64);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, 200);
        rt.sizeDelta = new Vector2(900, 120);
        txt.fontStyle = FontStyle.Bold;
    }

    private static GameObject CrearBotones(Transform padre, Opcion[] opciones)
    {
        GameObject primero = null;
        for (int i = 0; i < opciones.Length; i++)
        {
            GameObject boton = CrearBoton(padre, opciones[i], i);
            if (primero == null) primero = boton;
        }
        return primero;
    }

    private static GameObject CrearBoton(Transform padre, Opcion opcion, int indice)
    {
        var go = new GameObject(opcion.Texto, typeof(Image), typeof(Button));
        go.transform.SetParent(padre, false);
        go.GetComponent<Image>().color = BotonColor;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, 40 - indice * 90);
        rt.sizeDelta = new Vector2(420, 70);

        CrearEtiqueta(go.transform, opcion.Texto);
        Action accion = opcion.AlPulsar;
        go.GetComponent<Button>().onClick.AddListener(() => accion());
        return go;
    }

    private static void CrearEtiqueta(Transform padre, string texto)
    {
        var go = new GameObject("Texto", typeof(Text));
        go.transform.SetParent(padre, false);
        ConfigurarTexto(go.GetComponent<Text>(), texto, 32);
        Estirar(go.GetComponent<RectTransform>());
    }

    private static Text ConfigurarTexto(Text txt, string contenido, int tamano)
    {
        txt.text = contenido;
        txt.font = FuentePorDefecto();
        txt.fontSize = tamano;
        txt.color = TextoColor;
        txt.alignment = TextAnchor.MiddleCenter;
        return txt;
    }

    private static Font FuentePorDefecto()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }

    private static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
