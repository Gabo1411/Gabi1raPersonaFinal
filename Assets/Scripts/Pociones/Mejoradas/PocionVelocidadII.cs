using UnityEngine;

// Poción azul mejorada: da una mejora de velocidad de movimiento ligeramente menor que la común
// y además reduce el cooldown del dash mientras dure (ver PlayerMovement). No se acumula mientras siga activa.
public class PocionVelocidadII : PocionBase
{
    [Header("Efecto")]
    [Tooltip("Un poco menos que la poción de velocidad común.")]
    public float bonusVelocidad = 8.5f;
    public float duracion = 3f;

    protected override bool Aplicar(PlayerMovement jugador)
    {
        return jugador.TryApplyEliteBuff(BuffType.VelocidadMejorada, bonusVelocidad, duracion);
    }
}
