using UnityEngine;
using UnityEngine.InputSystem;

// Cofre interactuable: cuando el jugador esta cerca y pulsa interactuar,
// se abre una sola vez, cambia su sprite y suelta una recompensa.
[RequireComponent(typeof(Collider2D))]
public class Cofre : MonoBehaviour
{
    [SerializeField] private Sprite spriteAbierto;
    [SerializeField] private GameObject recompensaPrefab;
    [SerializeField] private GameObject panelAviso;

    private SpriteRenderer spriteRenderer;
    private bool jugadorCerca;
    private bool abierto;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (panelAviso != null) panelAviso.SetActive(false);
    }

    private void Update()
    {
        if (!jugadorCerca || abierto) return;
        if (InputAbrir()) Abrir();
    }

    private bool InputAbrir()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true;
        if (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame) return true;
        return false;
    }

    private void Abrir()
    {
        abierto = true;
        if (spriteAbierto != null) spriteRenderer.sprite = spriteAbierto;
        if (panelAviso != null) panelAviso.SetActive(false);
        if (recompensaPrefab != null)
            Instantiate(recompensaPrefab, transform.position + Vector3.down * 0.5f, Quaternion.identity);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerMovement>() == null) return;
        jugadorCerca = true;
        if (!abierto && panelAviso != null) panelAviso.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerMovement>() == null) return;
        jugadorCerca = false;
        if (panelAviso != null) panelAviso.SetActive(false);
    }
}
