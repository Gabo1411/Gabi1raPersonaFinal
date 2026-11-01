using UnityEngine;

// Poción verde común: cura vida al instante.
public class PocionSalud : PocionBase
{
    [Header("Efecto")]
    public float curacion = 25f;

    protected override bool Aplicar(PlayerMovement jugador)
    {
        jugador.Heal((int)curacion);
        return true;
    }
}
