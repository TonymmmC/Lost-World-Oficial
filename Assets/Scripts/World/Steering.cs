using UnityEngine;

// Evasion de paredes por steering. Si el camino directo choca con una pared,
// busca la direccion libre mas cercana a la deseada (abanico de angulos).
// No es pathfinding completo (puede atascarse en recovecos en U), pero evita
// que los enemigos se traben corriendo de frente contra una pared.
public static class Steering
{
    private static readonly float[] Angulos =
        { 25f, -25f, 50f, -50f, 75f, -75f, 100f, -100f, 135f, -135f };

    public static Vector2 EvitarParedes(Vector2 origen, Vector2 deseada, float distancia, float radio, LayerMask paredes)
    {
        if (deseada == Vector2.zero) return deseada;
        if (!Physics2D.CircleCast(origen, radio, deseada, distancia, paredes))
            return deseada;

        foreach (float a in Angulos)
        {
            Vector2 dir = Quaternion.Euler(0f, 0f, a) * (Vector3)deseada;
            if (!Physics2D.CircleCast(origen, radio, dir, distancia, paredes))
                return dir;
        }
        return deseada;
    }
}
