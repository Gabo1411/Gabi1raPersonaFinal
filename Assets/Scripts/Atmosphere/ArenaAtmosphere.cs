using System.Collections.Generic;
using UnityEngine;

// Se añade a la arena del jefe al cargar la escena. Construye sus efectos (foco central, candelabro colgante,
// decoración de los muros) y los deja dormidos mientras el jugador está en la cripta: luces apagadas, partículas
// paradas y scripts sin ejecutarse. TrapDoorDemo.ActivarArena() los despierta cuando el jugador cae a la arena.
public class ArenaAtmosphere : MonoBehaviour
{
    private bool built;
    private bool asleep;
    private readonly List<Behaviour> sleeping = new List<Behaviour>();
    private readonly List<ParticleSystem> stopped = new List<ParticleSystem>();

    void Start()
    {
        if (built) return;
        built = true;

        Boss boss = GetComponentInChildren<Boss>(true);
        if (boss == null) return;

        ArenaChandelier.Create(transform, boss);
        ArenaFocus.Create(transform, boss);
        ArenaWalls.Create(transform);

        Sleep();
    }

    // Despierta la arena (luces, partículas y animaciones). No hace nada si ya está despierta.
    public static void WakeUp()
    {
        ArenaAtmosphere atmosphere = FindFirstObjectByType<ArenaAtmosphere>(FindObjectsInactive.Include);
        if (atmosphere != null) atmosphere.Wake();
    }

    void Sleep()
    {
        if (asleep) return;
        asleep = true;

        foreach (Light light in GetComponentsInChildren<Light>(true)) Park(light);
        foreach (TorchFlicker torch in GetComponentsInChildren<TorchFlicker>(true)) Park(torch);
        foreach (ArenaChandelier chandelier in GetComponentsInChildren<ArenaChandelier>(true)) Park(chandelier);
        foreach (ArenaFocus focus in GetComponentsInChildren<ArenaFocus>(true)) Park(focus);
        foreach (GlowBillboard glow in GetComponentsInChildren<GlowBillboard>(true)) Park(glow);

        foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>(true))
        {
            if (!particles.isPlaying) continue;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            stopped.Add(particles);
        }
    }

    void Park(Behaviour behaviour)
    {
        if (behaviour == null || !behaviour.enabled) return;
        behaviour.enabled = false;
        sleeping.Add(behaviour);
    }

    void Wake()
    {
        if (!asleep) return;
        asleep = false;

        foreach (Behaviour behaviour in sleeping) if (behaviour != null) behaviour.enabled = true;
        foreach (ParticleSystem particles in stopped) if (particles != null) particles.Play(true);
        sleeping.Clear();
        stopped.Clear();
    }
}
