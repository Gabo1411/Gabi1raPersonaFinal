using UnityEngine;

// Poción especial: durante unos segundos el arma dispara como un subfusil (ver Weapon).
// Repetirla renueva el temporizador.
public class PocionDisparoContinuo : PocionBase
{
    [Header("Efecto")]
    public float duracion = 12f;

    protected override bool Aplicar(PlayerMovement jugador)
    {
        jugador.ApplyBuff(BuffType.DisparoContinuo, 1f, duracion);
        return true;
    }
}
