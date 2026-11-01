using UnityEngine;

// Poción roja común: aumenta el daño de las balas durante unos segundos. Repetirla renueva el temporizador.
public class PocionFuerza : PocionBase
{
    [Header("Efecto")]
    public float bonusDanio = 10f;
    public float duracion = 5f;

    protected override bool Aplicar(PlayerMovement jugador)
    {
        jugador.ApplyBuff(BuffType.Danio, bonusDanio, duracion);
        return true;
    }
}
