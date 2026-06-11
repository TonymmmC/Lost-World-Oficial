using UnityEngine;

// Marca una zona de suelo de tierra: mientras el jugador esta dentro, sus pasos suenan
// a tierra en vez de a pasto. Poner en un GameObject con un Collider2D marcado IsTrigger
// y estirarlo sobre el area (por ejemplo, la zona de esqueletos). Funciona con cualquier
// forma de collider (Box, Polygon).
[RequireComponent(typeof(Collider2D))]
public class ZonaTierra : MonoBehaviour
{
    private void Reset() => GetComponent<Collider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other) => Avisar(other, true);

    private void OnTriggerExit2D(Collider2D other) => Avisar(other, false);

    private void Avisar(Collider2D other, bool dentro)
    {
        PlayerMovement jugador = other.GetComponentInParent<PlayerMovement>();
        if (jugador != null) jugador.EnZonaTierra(dentro);
    }
}
