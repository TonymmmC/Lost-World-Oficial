using System.Collections;
using UnityEngine;

// Ralentizacion temporal (telarana de la arana). Expone un Multiplicador de velocidad
// (1 = normal, <1 = lento) que el sistema de movimiento consulta cada frame. Reutilizable:
// cualquier ataque puede llamar Aplicar. Refresca la duracion, no acumula multiplicadores.
public class StatusLentitud : MonoBehaviour
{
    public float Multiplicador { get; private set; } = 1f;

    private Coroutine rutina;

    private void OnDestroy()
    {
        if (rutina != null) StopCoroutine(rutina);
    }

    public void Aplicar(float multiplicador, float duracion)
    {
        if (multiplicador >= 1f || duracion <= 0f) return;
        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(Ralentizar(multiplicador, duracion));
    }

    private IEnumerator Ralentizar(float multiplicador, float duracion)
    {
        Multiplicador = multiplicador;
        yield return new WaitForSeconds(duracion);
        Multiplicador = 1f;
        rutina = null;
    }
}
