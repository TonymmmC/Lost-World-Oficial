using System.Collections;
using UnityEngine;

// Daño recurrente (veneno, fuego). Se agrega al objetivo en el momento del golpe y
// aplica daño en ticks. Reutilizable: cualquier ataque puede llamar Aplicar.
[RequireComponent(typeof(Health))]
public class DanioPorTiempo : MonoBehaviour
{
    private Health health;
    private Coroutine rutina;

    private void Awake() => health = GetComponent<Health>();

    private void OnDestroy()
    {
        if (rutina != null) StopCoroutine(rutina);
    }

    // Reinicia el efecto si ya habia uno activo (no acumula, refresca duracion).
    public void Aplicar(int danioPorTick, float intervalo, int ticks, Vector2 origen)
    {
        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(Tickear(danioPorTick, intervalo, ticks, origen));
    }

    private IEnumerator Tickear(int danio, float intervalo, int ticks, Vector2 origen)
    {
        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(intervalo);
            if (health == null || health.IsDead) break;
            health.TakeDamage(danio, origen);
        }
        rutina = null;
    }
}
