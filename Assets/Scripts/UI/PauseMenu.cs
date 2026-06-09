using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Pausa el juego con Escape o el boton Start del mando y muestra un menu
// construido por codigo: CONTINUA, REINICIAR, SALIR. SALIR cierra el juego.
public class PauseMenu : MonoBehaviour
{
    private const string EscenaJuego = "World";

    private bool enPausa;
    private GameObject menu;
    private PlayerMovement playerMovement;

    private void Start()
    {
        playerMovement = FindAnyObjectByType<PlayerMovement>();
    }

    private void Update()
    {
        if (PausaPresionada())
            Alternar();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private bool PausaPresionada()
    {
        bool teclado = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool mando = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
        return teclado || mando;
    }

    private void Alternar()
    {
        if (enPausa) Continuar();
        else Pausar();
    }

    private void Pausar()
    {
        enPausa = true;
        Time.timeScale = 0f;
        if (playerMovement != null) playerMovement.enabled = false;

        menu = MenuUI.Construir("PAUSA",
            new MenuUI.Opcion("CONTINUA", Continuar),
            new MenuUI.Opcion("REINICIAR", Reiniciar),
            new MenuUI.Opcion("SALIR", Salir));
    }

    public void Continuar()
    {
        enPausa = false;
        Time.timeScale = 1f;
        if (playerMovement != null) playerMovement.enabled = true;
        if (menu != null) Destroy(menu);
    }

    public void Reiniciar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(EscenaJuego);
    }

    public void Salir()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
