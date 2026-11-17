using System.Collections.Generic;
using UnityEngine;

// Consultas de física sin asignar memoria y clasificación de colliders con caché.
// Los proyectiles (misiles del jefe, disparo cargado) las usan en cada fotograma: antes cada consulta creaba un array nuevo
// (RaycastAll / SphereCastAll) y cada impacto repetía varios GetComponentInParent.
public static class PhysicsBuffers
{
    public static readonly RaycastHit[] Hits = new RaycastHit[48];

    // Lanza una esfera y deja en Hits los impactos ordenados de más cerca a más lejos. Devuelve cuántos son.
    public static int SphereCastSorted(Vector3 origin, float radius, Vector3 direction, float distance, QueryTriggerInteraction triggers)
    {
        int count = Physics.SphereCastNonAlloc(origin, radius, direction, Hits, distance, ~0, triggers);
        SortByDistance(count);
        return count;
    }

    public static int RaycastSorted(Vector3 origin, Vector3 direction, float distance, QueryTriggerInteraction triggers)
    {
        int count = Physics.RaycastNonAlloc(origin, direction, Hits, distance, ~0, triggers);
        SortByDistance(count);
        return count;
    }

    // Ordenación por inserción: son pocos elementos y no crea basura
    static void SortByDistance(int count)
    {
        for (int i = 1; i < count; i++)
        {
            RaycastHit item = Hits[i];
            int j = i - 1;
            while (j >= 0 && Hits[j].distance > item.distance)
            {
                Hits[j + 1] = Hits[j];
                j--;
            }
            Hits[j + 1] = item;
        }
    }
}

// Qué es cada collider (jugador, bala, cosa que un misil debe atravesar...). Se calcula una vez por collider y se recuerda.
public static class ColliderInfo
{
    [System.Flags]
    enum Kind : byte
    {
        Player = 1,
        Bullet = 2,         // bala del jugador o del jefe
        MissileIgnores = 4  // lo que un misil atraviesa: jefe, enemigos, balas, otros misiles y el candelabro colgante
    }

    static readonly Dictionary<Collider, Kind> cache = new Dictionary<Collider, Kind>();

    static Kind Of(Collider col)
    {
        if (cache.TryGetValue(col, out Kind kind)) return kind;

        // La caché no debe crecer sin fin con colliders destruidos (balas, enemigos...)
        if (cache.Count > 1500) cache.Clear();

        kind = 0;
        if (col.CompareTag("Player") || col.GetComponentInParent<PlayerMovement>() != null) kind |= Kind.Player;
        if (col.GetComponentInParent<BalaJefe>() != null || col.GetComponentInParent<BulletDamage>() != null) kind |= Kind.Bullet;

        bool ignored = col.CompareTag("Boss") || col.CompareTag("Enemy")
                       || (kind & Kind.Bullet) != 0
                       || col.GetComponentInParent<BossMissile>() != null
                       || col.GetComponentInParent<Boss>() != null
                       || col.GetComponentInParent<EnemyBase>() != null;
        if (!ignored)
        {
            for (Transform t = col.transform; t != null; t = t.parent)
            {
                if (t.name == "Chandelier" || t.name == "CandelabroColgante") { ignored = true; break; }
            }
        }
        if (ignored) kind |= Kind.MissileIgnores;

        cache[col] = kind;
        return kind;
    }

    public static bool IsPlayer(Collider col) => (Of(col) & Kind.Player) != 0;
    public static bool IsBullet(Collider col) => (Of(col) & Kind.Bullet) != 0;
    public static bool IsPlayerOrBullet(Collider col) => (Of(col) & (Kind.Player | Kind.Bullet)) != 0;
    public static bool MissileIgnores(Collider col) => (Of(col) & Kind.MissileIgnores) != 0;
}
