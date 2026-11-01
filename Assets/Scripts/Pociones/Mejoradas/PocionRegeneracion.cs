using UnityEngine;

// Poción verde mejorada: en lugar de curar de golpe, regenera más vida en total repartida durante unos segundos.
// No se acumula mientras siga activa.
public class PocionRegeneracion : PocionBase
{
    [Header("Efecto")]
    [Tooltip("Vida total que se recupera durante toda la duración.")]
    public float curacionTotal = 40f;
    public float duracion = 8f;

    protected override bool Aplicar(PlayerMovement jugador)
    {
        float vidaPorSegundo = curacionTotal / duracion;
        return jugador.TryApplyEliteBuff(BuffType.Regeneracion, vidaPorSegundo, duracion);
    }
}
