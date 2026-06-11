using UnityEngine;
using UnityEngine.InputSystem;

// Regla unica de "el jugador esta usando entrada tactil": solo en Android/iOS reales y sin mando.
// La consultan los controles tactiles en pantalla y la posicion adaptable del HUD.
public static class EntradaTactil
{
    public static bool Activa
    {
        get
        {
            if (HayMandoReal()) return false; // hay mando fisico: el jugador usa ese
            return EsMovil();
        }
    }

    // Plataforma real, no build target ni presencia de touchscreen: muchos PCs/laptops
    // (y el editor con target Android) reportan touchscreen y mostraban los controles
    // en el .exe. En escritorio se usa teclado/mando; los controles tactiles son solo de movil.
    private static bool EsMovil()
    {
        return Application.platform == RuntimePlatform.Android
            || Application.platform == RuntimePlatform.IPhonePlayer;
    }

    // Los controles OnScreen en pantalla simulan un Gamepad virtual, asi que Gamepad.current
    // deja de ser null en cuanto se muestran. Ese mando virtual no tiene descripcion de
    // hardware: solo cuenta como mando real si la descripcion no esta vacia. Sin esto los
    // controles tactiles se ocultan a si mismos al activarse.
    private static bool HayMandoReal()
    {
        Gamepad gp = Gamepad.current;
        return gp != null && !gp.description.empty;
    }
}
