using System.Collections.Generic;
using UnityEngine;

// Las balas del jugador y las del jefe no deben chocar entre sí (rebotaban). Cada vez que una bala se activa
// ignora las colisiones con todas las balas activas del bando contrario.
public static class BulletCollisionFilter
{
    static readonly List<GameObject> playerBullets = new List<GameObject>();
    static readonly List<GameObject> bossBullets = new List<GameObject>();

    public static void Register(GameObject bullet, bool fromBoss)
    {
        List<GameObject> own = fromBoss ? bossBullets : playerBullets;
        List<GameObject> other = fromBoss ? playerBullets : bossBullets;

        if (!own.Contains(bullet)) own.Add(bullet);
        other.RemoveAll(b => b == null);

        Collider[] mine = bullet.GetComponentsInChildren<Collider>();
        foreach (GameObject enemyBullet in other)
        {
            if (!enemyBullet.activeInHierarchy) continue;
            foreach (Collider a in mine)
            {
                if (a == null || !a.enabled) continue;
                foreach (Collider b in enemyBullet.GetComponentsInChildren<Collider>())
                {
                    if (b != null && b.enabled) Physics.IgnoreCollision(a, b, true);
                }
            }
        }
    }
}
