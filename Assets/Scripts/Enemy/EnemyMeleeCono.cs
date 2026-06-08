using UnityEngine;

// Enemigo melee que ataca igual que el jugador: un cono frontal que golpea a todos los
// objetivos dentro del radio (Attack Range) y del angulo (Attack Front Dot). Usa el
// gizmo de cono magenta de la base. Pensado para el enemigo morado.
public class EnemyMeleeCono : EnemyMeleeAnimado
{
    protected override void EjecutarGolpe() => GolpearConoFrontal(danioAtaque);
}
