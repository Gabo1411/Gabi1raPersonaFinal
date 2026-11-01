using UnityEngine;

// Poción azul común: aumenta la velocidad de movimiento durante unos segundos. Repetirla renueva el temporizador.
public class PocionVelocidad : PocionBase
{
    [Header("Efecto")]
    public float bonusVelocidad = 10f;
    public float duracion = 3f;

    protected override bool Aplicar(PlayerMovement jugador)
    {
        jugador.ApplyBuff(BuffType.Velocidad, bonusVelocidad, duracion);
        return true;
    }
}
