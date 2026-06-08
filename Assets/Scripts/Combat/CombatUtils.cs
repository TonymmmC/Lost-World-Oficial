using UnityEngine;

// Utilidades de combate direccional compartidas. Evita el combate radial (360 grados):
// el golpe y el bloqueo solo cuentan hacia el frente del atacante o defensor.
public static class CombatUtils
{
    // True si el objetivo cae dentro del cono frontal segun la direccion de mira.
    // minDot 0 = medio circulo (180 grados), 0.3 ~ 145 grados, 0.5 ~ 120, 0.707 ~ 90.
    public static bool EnFrente(Vector2 origen, Vector2 facing, Vector2 objetivo, float minDot)
    {
        Vector2 dir = objetivo - origen;
        if (dir.sqrMagnitude < 0.0001f) return true;
        return Vector2.Dot(facing.normalized, dir.normalized) >= minDot;
    }

    // Dibuja el cono frontal con Gizmos para verlo y tunearlo en la escena.
    public static void DibujarCono(Vector2 origen, Vector2 facing, float minDot, float longitud)
    {
        float medioAngulo = Mathf.Acos(Mathf.Clamp(minDot, -1f, 1f)) * Mathf.Rad2Deg;
        Vector3 frente = ((Vector3)facing).normalized;

        const int segmentos = 14;
        Vector3 anterior = origen;
        for (int i = 0; i <= segmentos; i++)
        {
            float angulo = Mathf.Lerp(medioAngulo, -medioAngulo, (float)i / segmentos);
            Vector3 dir = Quaternion.Euler(0f, 0f, angulo) * frente;
            Vector3 punto = (Vector3)origen + dir * longitud;
            Gizmos.DrawLine(origen, punto);
            if (i > 0) Gizmos.DrawLine(anterior, punto);
            anterior = punto;
        }
    }
}
