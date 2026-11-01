using UnityEngine;

// Poción roja mejorada: más daño que la común durante menos tiempo. No se acumula mientras siga activa.
public class PocionFuerzaII : PocionBase
{
    [Header("Efecto")]
    public float bonusDanio = 18f;
    public float duracion = 3f;

    protected override bool Aplicar(PlayerMovement jugador)
    {
        return jugador.TryApplyEliteBuff(BuffType.DanioMejorado, bonusDanio, duracion);
    }
}
