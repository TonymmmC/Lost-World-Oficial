using UnityEngine;
using UnityEngine.InputSystem;

// Muestra paneles de intro y tutorial al iniciar la escena. Pausa el juego
// mientras se leen y avanza al siguiente con teclado, mando o toque en pantalla.
public class GameIntro : MonoBehaviour
{
    [SerializeField] private GameObject[] paneles;

    private int indice = -1;

    private void Start()
    {
        Time.timeScale = 0f;
        Avanzar();
    }

    private void Update()
    {
        if (indice < 0) return;
        if (InputAvanzar()) Avanzar();
    }

    private bool InputAvanzar()
    {
        if (Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
            return true;
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;
        return false;
    }

    private void Avanzar()
    {
        if (indice >= 0 && indice < paneles.Length && paneles[indice] != null)
            paneles[indice].SetActive(false);

        indice++;

        if (indice < paneles.Length)
        {
            if (paneles[indice] != null)
                paneles[indice].SetActive(true);
            return;
        }

        indice = -1;
        Time.timeScale = 1f;
    }
}
