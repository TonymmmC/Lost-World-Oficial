using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Pantalla de muerte. Tras una breve pausa congela el juego y muestra un menu
// por codigo: REINICIAR vuelve a cargar el mundo, SALIR cierra el juego.
// No necesita estar montada en la escena: se instancia bajo demanda al morir.
public class GameOverScreen : MonoBehaviour
{
    private const string EscenaJuego = "World";
    private const float RetardoMostrar = 0.8f;

    public static void Mostrar()
    {
        var go = new GameObject("GameOverScreen");
        go.AddComponent<GameOverScreen>().StartCoroutine(MostrarTrasRetardo(go));
    }

    private static IEnumerator MostrarTrasRetardo(GameObject host)
    {
        yield return new WaitForSecondsRealtime(RetardoMostrar);
        Time.timeScale = 0f;
        var menu = MenuUI.Construir("HAS MUERTO",
            new MenuUI.Opcion("REINICIAR", Reiniciar),
            new MenuUI.Opcion("SALIR", Salir));
        menu.transform.SetParent(host.transform, false);
    }

    private static void Reiniciar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(EscenaJuego);
    }

    private static void Salir()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
