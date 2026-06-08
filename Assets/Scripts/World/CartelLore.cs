using UnityEngine;

// Muestra un panel de texto de lore cuando el jugador entra en el trigger
// y lo oculta al salir. Usar para carteles, tumbas o letreros del mundo.
[RequireComponent(typeof(Collider2D))]
public class CartelLore : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (EsJugador(other) && panel != null) panel.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (EsJugador(other) && panel != null) panel.SetActive(false);
    }

    private bool EsJugador(Collider2D other)
    {
        return other.GetComponentInParent<PlayerMovement>() != null;
    }
}
