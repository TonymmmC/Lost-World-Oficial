using UnityEngine;

// Corazon del mundo: al tocarlo, sube la vida maxima del jugador y desaparece.
// Progresion estilo Zelda (mas corazones encontrados en el mundo).
[RequireComponent(typeof(Collider2D))]
public class CorazonPickup : MonoBehaviour
{
    [SerializeField] private int cantidadVida = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement jugador = other.GetComponentInParent<PlayerMovement>();
        if (jugador == null) return;

        Health vida = jugador.GetComponent<Health>();
        if (vida == null) return;

        vida.AumentarVidaMaxima(cantidadVida);
        Destroy(gameObject);
    }
}
