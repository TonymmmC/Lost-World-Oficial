using UnityEngine;

// Esqueleto: melee agresivo cuyo golpe usa una caja rectangular frontal (modificable
// en tamano y distancia) en vez del cono. La logica de ataque la hereda de la base.
public class SkullAI : EnemyMeleeAnimado
{
    [Header("Forma del golpe (caja frontal)")]
    [SerializeField] private Vector2 cajaTamano = new Vector2(1.2f, 0.8f);
    [SerializeField] private float cajaDistancia = 0.7f;

    protected override void EjecutarGolpe() => GolpearCajaFrontal(cajaTamano, cajaDistancia, danioAtaque);

    protected override void DibujarGizmoAtaque() => DibujarCajaFrontal(cajaTamano, cajaDistancia);
}
