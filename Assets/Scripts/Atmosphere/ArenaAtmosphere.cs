using UnityEngine;

// Se añade a la arena del jefe al cargar la escena. Cuando la arena se activa construye sus efectos:
// foco central, candelabro colgante y decoración de los muros.
public class ArenaAtmosphere : MonoBehaviour
{
    private bool built;

    void Start()
    {
        if (built) return;
        built = true;

        Boss boss = GetComponentInChildren<Boss>(true);
        if (boss == null) return;

        ArenaChandelier.Create(transform, boss);
        ArenaFocus.Create(transform, boss);
        ArenaWalls.Create(transform);
    }
}
