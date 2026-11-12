using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using static CommandRegistry;

// Todos los comandos de la consola debug. Para añadir uno basta con escribir un método estático con
// [ConsoleCommand(...)] que reciba string[] y devuelva el texto de respuesta.
public static class DebugCommands
{
    // ---------- Utilidades ----------

    static PlayerMovement Player
    {
        get
        {
            PlayerMovement p = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
            if (p == null) throw new CommandException("No hay jugador en esta escena.");
            return p;
        }
    }

    static Weapon Gun
    {
        get
        {
            Weapon w = UnityEngine.Object.FindFirstObjectByType<Weapon>();
            if (w == null) throw new CommandException("No hay arma en esta escena.");
            return w;
        }
    }

    static Boss BossObject
    {
        get
        {
            Boss b = UnityEngine.Object.FindFirstObjectByType<Boss>(FindObjectsInactive.Include);
            if (b == null) throw new CommandException("No hay jefe en esta escena.");
            return b;
        }
    }

    static TrapDoorDemo Trapdoor => UnityEngine.Object.FindFirstObjectByType<TrapDoorDemo>();

    static string OnOff(bool value) => value ? "activado" : "desactivado";

    // Punto del suelo delante del jugador
    static Vector3 InFront(float distance, float sideAngle = 0f)
    {
        PlayerMovement p = Player;
        Vector3 forward = p.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        Vector3 direction = Quaternion.AngleAxis(sideAngle, Vector3.up) * forward.normalized;
        return GroundAt(p.transform.position + direction * distance);
    }

    static Vector3 GroundAt(Vector3 position)
    {
        Vector3 origin = position + Vector3.up * 12f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        Vector3 point = position;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<PlayerMovement>() != null) continue;
            if (hit.collider.GetComponentInParent<EnemyBase>() != null) continue;
            if (hit.distance < best) { best = hit.distance; point = hit.point; }
        }
        return point;
    }

    static string Vec(Vector3 v) => v.x.ToString("F1") + ", " + v.y.ToString("F1") + ", " + v.z.ToString("F1");

    // ---------- Ayuda y utilidades de la consola ----------

    [ConsoleCommand("help", "Lista los comandos o explica uno", "help [comando]")]
    static string Help(string[] args)
    {
        if (args.Length > 0)
        {
            if (!TryGet(args[0], out CommandRegistry.Command command)) throw new CommandException("No existe el comando '" + args[0] + "'.");
            return "Uso: " + command.info.Usage + "\n" + command.info.Help;
        }

        StringBuilder sb = new StringBuilder("Comandos disponibles (help <comando> para el detalle):");
        foreach (CommandRegistry.Command c in All)
        {
            sb.Append('\n').Append("  ").Append(c.info.Name.PadRight(12)).Append(c.info.Help);
        }
        return sb.ToString();
    }

    [ConsoleCommand("clear", "Limpia la consola (también Ctrl+L)")]
    static string ClearConsole(string[] args)
    {
        DebugConsole.Clear();
        return null;
    }

    [ConsoleCommand("log", "Muestra los últimos mensajes del registro de Unity (Debug.Log, avisos y errores)", "log [cantidad=15]")]
    static string Log(string[] args) => DebugConsole.RecentUnityLog(Int(args, 0, 15));

    [ConsoleCommand("errors", "Muestra o no los errores de Unity en la consola cuando ocurren", "errors [on/off]", "on,off")]
    static string Errors(string[] args)
    {
        DebugConsole.SetShowErrors(Switch(args, 0, DebugConsole.ShowErrors));
        return "Errores en consola: " + OnOff(DebugConsole.ShowErrors);
    }

    [ConsoleCommand("restart", "Reinicia la escena actual")]
    static string Restart(string[] args)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        return "Reiniciando...";
    }

    // ---------- Jugador ----------

    [ConsoleCommand("god", "Invulnerabilidad: no recibes daño", "god [on/off]", "on,off")]
    static string God(string[] args)
    {
        PlayerMovement p = Player;
        p.godMode = Switch(args, 0, p.godMode);
        return "Modo dios " + OnOff(p.godMode);
    }

    [ConsoleCommand("heal", "Cura al jugador (sin número, vida completa)", "heal [cantidad]")]
    static string Heal(string[] args)
    {
        PlayerMovement p = Player;
        p.Heal(Int(args, 0, p.maxHealth), false);
        return "Vida: " + p.Health + " / " + p.maxHealth;
    }

    [ConsoleCommand("hp", "Fija la vida del jugador (si supera el máximo, sube el máximo)", "hp <n>")]
    static string Hp(string[] args)
    {
        PlayerMovement p = Player;
        int value = Mathf.Max(1, Int(args, 0, p.maxHealth));
        if (value > p.maxHealth) p.maxHealth = value;
        p.Health = value;
        return "Vida: " + p.Health + " / " + p.maxHealth;
    }

    [ConsoleCommand("speed", "Multiplicador de velocidad del jugador", "speed [x]")]
    static string Speed(string[] args)
    {
        PlayerMovement p = Player;
        if (args.Length > 0) p.debugSpeedMultiplier = Mathf.Clamp(Float(args, 0, 1f), 0.1f, 10f);
        return "Velocidad x" + p.debugSpeedMultiplier.ToString("0.##") + " (actual " + p.CurrentSpeed.ToString("0.#") + ")";
    }

    [ConsoleCommand("damage", "Bonus de daño fijo del jugador (se suma al de las pociones)", "damage [n]")]
    static string Damage(string[] args)
    {
        PlayerMovement p = Player;
        if (args.Length > 0) p.debugBonusDamage = Int(args, 0, 0);
        return "Bonus de daño debug: " + p.debugBonusDamage + " (total " + p.bonusDanio + ")";
    }

    [ConsoleCommand("noclip", "Atravesar paredes y volar libremente (Espacio sube, Ctrl baja)", "noclip [on/off]", "on,off")]
    static string Noclip(string[] args)
    {
        PlayerMovement p = Player;
        p.noclip = Switch(args, 0, p.noclip);
        if (p.noclip) p.fly = false;
        return "Noclip " + OnOff(p.noclip);
    }

    [ConsoleCommand("fly", "Vuelo libre con colisiones (Espacio sube, Ctrl baja)", "fly [on/off]", "on,off")]
    static string Fly(string[] args)
    {
        PlayerMovement p = Player;
        p.fly = Switch(args, 0, p.fly);
        if (p.fly) p.noclip = false;
        return "Vuelo " + OnOff(p.fly);
    }

    [ConsoleCommand("tp", "Teletransporta al jugador", "tp <cripta|arena|boss|portal|x y z>", "cripta,arena,boss,portal")]
    static string Teleport(string[] args)
    {
        PlayerMovement p = Player;
        if (args.Length == 0) throw new CommandException("Uso: tp <cripta|arena|boss|portal|x y z>");

        switch (args[0].ToLowerInvariant())
        {
            case "cripta":
                if (!DebugRuntime.HasStart) throw new CommandException("Aún no se conoce el punto de inicio de la cripta.");
                p.DebugTeleport(DebugRuntime.StartPosition);
                return "Teletransportado a la cripta.";

            case "arena":
            {
                TrapDoorDemo door = Trapdoor;
                if (door == null) throw new CommandException("No hay escotilla ni arena en esta escena.");
                door.WarpPlayer(p.transform);
                if (!door.ArenaActive) door.ActivarArena();
                return "Teletransportado a la arena del jefe.";
            }

            case "boss":
            {
                TrapDoorDemo door = Trapdoor;
                Boss boss = BossObject;
                if (!boss.gameObject.activeInHierarchy)
                {
                    if (door == null) throw new CommandException("La arena no está activa y no hay escotilla para activarla.");
                    door.WarpPlayer(p.transform);
                    door.ActivarArena();
                }
                Vector3 away = p.transform.position - boss.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude < 4f) away = Vector3.back;
                p.DebugTeleport(GroundAt(boss.transform.position + away.normalized * 14f) + Vector3.up * 2f);
                return "Teletransportado frente al jefe.";
            }

            case "portal":
            {
                GameManager gm = GameManager.Instance;
                if (gm == null || gm.portalSalida == null) throw new CommandException("No hay portal de salida.");
                p.DebugTeleport(GroundAt(gm.portalSalida.transform.position + Vector3.right * 4f) + Vector3.up * 2f);
                return "Teletransportado junto al portal.";
            }
        }

        if (args.Length >= 3)
        {
            Vector3 target = new Vector3(Float(args, 0, 0f), Float(args, 1, 0f), Float(args, 2, 0f));
            p.DebugTeleport(target);
            return "Teletransportado a " + Vec(target);
        }
        throw new CommandException("Uso: tp <cripta|arena|boss|portal|x y z>");
    }

    [ConsoleCommand("crouch", "Ajusta el agacharse: altura de la cápsula y factor de velocidad", "crouch [height n | speed x]", "height,speed")]
    static string Crouch(string[] args)
    {
        PlayerMovement p = Player;
        if (args.Length >= 2)
        {
            if (args[0] == "height") p.CrouchHeight = Float(args, 1, p.CrouchHeight);
            else if (args[0] == "speed") p.CrouchSpeedFactor = Float(args, 1, p.CrouchSpeedFactor);
            else throw new CommandException("Usa: crouch height <n> | crouch speed <x>");
        }
        return "Agacharse: altura " + p.CrouchHeight.ToString("0.##") + " (de pie " + p.StandHeight.ToString("0.##") + "), velocidad x" + p.CrouchSpeedFactor.ToString("0.##");
    }

    // ---------- Arma ----------

    [ConsoleCommand("ammo", "Munición infinita o un valor concreto", "ammo <inf|n>", "inf")]
    static string Ammo(string[] args)
    {
        Weapon w = Gun;
        if (args.Length == 0) return "Munición: " + w.bulletsLeft + " / " + w.magazineSize + (w.infiniteAmmo ? " (infinita)" : "");

        if (args[0].ToLowerInvariant() == "inf")
        {
            w.infiniteAmmo = !w.infiniteAmmo;
            w.DebugSetAmmoUi();
            return "Munición infinita " + OnOff(w.infiniteAmmo);
        }

        w.DebugSetAmmo(Int(args, 0, w.magazineSize));
        return "Munición: " + w.bulletsLeft + " / " + w.magazineSize;
    }

    [ConsoleCommand("reload", "Recarga el arma ahora")]
    static string Reload(string[] args)
    {
        Gun.DebugReload();
        return "ok";
    }

    [ConsoleCommand("sniper", "Ajustes del disparo cargado: coste en balas, multiplicador de daño, espera, o recargar el cristal",
        "sniper <cost n | dmg x | delay s | recharge>", "cost,dmg,delay,recharge")]
    static string Sniper(string[] args)
    {
        Weapon w = Gun;
        if (args.Length == 0)
            return "Disparo cargado: coste " + w.sniperBulletCost + " balas, daño x" + w.sniperDamageMultiplier + ", espera " + w.sniperDelay + " s";

        switch (args[0].ToLowerInvariant())
        {
            case "cost": w.sniperBulletCost = Mathf.Max(1, Int(args, 1, w.sniperBulletCost)); return "Coste: " + w.sniperBulletCost + " balas";
            case "dmg": w.sniperDamageMultiplier = Mathf.Max(0.1f, Float(args, 1, w.sniperDamageMultiplier)); return "Multiplicador de daño: x" + w.sniperDamageMultiplier;
            case "delay": w.sniperDelay = Mathf.Max(0.1f, Float(args, 1, w.sniperDelay)); return "Espera: " + w.sniperDelay + " s";
            case "recharge": w.DebugRechargeSniper(); return "Cristal recargado.";
        }
        throw new CommandException("Uso: sniper <cost n | dmg x | delay s | recharge>");
    }

    // ---------- Jefe ----------

    [ConsoleCommand("boss", "Controla al jefe: vida, matarlo, forzar ataques, congelarlo, fase o activar la arena",
        "boss <hp n | kill | attack <bullets|gatling|missiles> | freeze | phase <%> | spawn | status>",
        "hp,kill,attack:bullets/gatling/missiles,freeze,phase,spawn,status")]
    static string BossCommand(string[] args)
    {
        if (args.Length == 0) throw new CommandException("Uso: boss <hp n | kill | attack x | freeze | phase % | spawn | status>");

        Boss boss = BossObject;
        string sub = args[0].ToLowerInvariant();

        if (sub == "spawn")
        {
            TrapDoorDemo door = Trapdoor;
            if (door == null) throw new CommandException("No hay escotilla en esta escena.");
            door.ActivarArena();
            return "Arena activada (el jefe y los spawners están despiertos).";
        }

        if (!boss.gameObject.activeInHierarchy) throw new CommandException("El jefe aún no está activo. Usa: boss spawn");

        switch (sub)
        {
            case "hp":
                boss.DebugSetHealth(Int(args, 1, boss.maxHealth));
                return "Vida del jefe: " + boss.CurrentHealth + " / " + boss.maxHealth;

            case "kill":
                boss.DebugSetHealth(0);
                return "Jefe eliminado.";

            case "attack":
                if (args.Length < 2 || !boss.DebugAttack(args[1].ToLowerInvariant()))
                    throw new CommandException("Uso: boss attack <bullets|gatling|missiles>");
                return "Ataque lanzado: " + args[1];

            case "freeze":
                boss.frozen = !boss.frozen;
                return "Jefe " + (boss.frozen ? "congelado (no inicia ataques nuevos)" : "descongelado");

            case "phase":
            {
                float percent = Mathf.Clamp(Float(args, 1, 50f), 1f, 100f);
                boss.DebugSetHealth(Mathf.Max(1, Mathf.RoundToInt(boss.maxHealth * percent / 100f)));
                return "Vida del jefe al " + percent + " %";
            }

            case "status":
                return "Jefe: " + boss.CurrentHealth + " / " + boss.maxHealth + " (" + Mathf.RoundToInt(boss.HealthFraction * 100f) + " %), "
                       + (boss.frozen ? "congelado" : "activo") + ", posición " + Vec(boss.transform.position);
        }
        throw new CommandException("Subcomando desconocido: " + sub);
    }

    // ---------- Enemigos ----------

    static GameObject FindEnemyPrefab(bool mushroom)
    {
        foreach (EnemySpawner spawner in UnityEngine.Object.FindObjectsByType<EnemySpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (spawner.enemigoPrefab != null && IsKind(spawner.enemigoPrefab, mushroom)) return spawner.enemigoPrefab;
        }
        foreach (BossArenaSpawner spawner in UnityEngine.Object.FindObjectsByType<BossArenaSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (spawner.enemigosPrefabs == null) continue;
            foreach (GameObject prefab in spawner.enemigosPrefabs) if (prefab != null && IsKind(prefab, mushroom)) return prefab;
        }
        return null;
    }

    static bool IsKind(GameObject prefab, bool mushroom) => (prefab.GetComponent<EnemyMushroom>() != null) == mushroom && prefab.GetComponent<EnemyBase>() != null;

    [ConsoleCommand("spawn", "Crea enemigos delante del jugador", "spawn <cactus|hongo> [cantidad=1]", "cactus,hongo")]
    static string Spawn(string[] args)
    {
        if (args.Length == 0) throw new CommandException("Uso: spawn <cactus|hongo> [cantidad]");

        string kind = args[0].ToLowerInvariant();
        bool mushroom = kind == "hongo" || kind == "mushroom";
        if (!mushroom && kind != "cactus") throw new CommandException("Tipo desconocido: usa cactus o hongo.");

        GameObject prefab = FindEnemyPrefab(mushroom);
        if (prefab == null) throw new CommandException("No encontré el prefab de ese enemigo en esta escena.");

        int count = Mathf.Clamp(Int(args, 1, 1), 1, 30);
        TrapDoorDemo door = Trapdoor;
        bool arena = door != null && door.ArenaActive;

        for (int i = 0; i < count; i++)
        {
            float angle = count == 1 ? 0f : Mathf.Lerp(-35f, 35f, i / (float)(count - 1));
            Vector3 position = InFront(9f + (i % 3) * 1.5f, angle);
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 8f, NavMesh.AllAreas)) position = hit.position;

            GameObject enemy = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            EnemyBase enemyBase = enemy.GetComponent<EnemyBase>();
            if (enemyBase != null && arena) enemyBase.efectoMuerteArena = true;
        }
        return count + " " + (mushroom ? "hongo(s)" : "cactus") + " creado(s).";
    }

    [ConsoleCommand("kill", "Elimina a todos los enemigos (sin botín ni puntos de baja)", "kill all", "all")]
    static string Kill(string[] args)
    {
        if (args.Length == 0 || args[0].ToLowerInvariant() != "all") throw new CommandException("Uso: kill all");

        int count = 0;
        foreach (EnemyBase enemy in new List<EnemyBase>(EnemyBase.Active))
        {
            if (enemy.health > 0) { enemy.Kill(); count++; }
        }
        return count + " enemigo(s) eliminado(s).";
    }

    [ConsoleCommand("enemies", "Congela la IA de los enemigos o cuenta los que hay", "enemies <freeze|count>", "freeze,count")]
    static string Enemies(string[] args)
    {
        string sub = args.Length > 0 ? args[0].ToLowerInvariant() : "count";
        if (sub == "freeze")
        {
            EnemyBase.Frozen = !EnemyBase.Frozen;
            return "IA de enemigos " + (EnemyBase.Frozen ? "congelada" : "activa");
        }

        int cactus = 0, mushrooms = 0;
        foreach (EnemyBase enemy in EnemyBase.Active)
        {
            if (enemy.health <= 0) continue;
            if (enemy is EnemyMushroom) mushrooms++; else cactus++;
        }
        return "Enemigos vivos: " + (cactus + mushrooms) + " (" + cactus + " cactus, " + mushrooms + " hongos)" + (EnemyBase.Frozen ? " [IA congelada]" : "");
    }

    // ---------- Pociones, puntos y partida ----------

    static readonly Dictionary<string, BuffType> BuffNames = new Dictionary<string, BuffType>
    {
        { "velocidad", BuffType.Velocidad }, { "danio", BuffType.Danio }, { "daño", BuffType.Danio }, { "fuerza", BuffType.Danio },
        { "velocidad2", BuffType.VelocidadMejorada }, { "danio2", BuffType.DanioMejorado }, { "fuerza2", BuffType.DanioMejorado },
        { "regeneracion", BuffType.Regeneracion }, { "regeneración", BuffType.Regeneracion }, { "disparo", BuffType.DisparoContinuo }
    };

    static float DefaultBuffAmount(BuffType type)
    {
        switch (type)
        {
            case BuffType.Velocidad: return 5f;
            case BuffType.VelocidadMejorada: return 9f;
            case BuffType.Danio: return 3f;
            case BuffType.DanioMejorado: return 6f;
            case BuffType.Regeneracion: return 4f;
            default: return 1f;
        }
    }

    [ConsoleCommand("buff", "Aplica un efecto de poción al instante (o limpia los efectos)", "buff <tipo|clear> [segundos=30]",
        "velocidad,fuerza,velocidad2,fuerza2,regeneracion,disparo,clear")]
    static string Buff(string[] args)
    {
        PlayerMovement p = Player;
        if (args.Length == 0) return "Tipos: velocidad, fuerza, velocidad2, fuerza2, regeneracion, disparo   (buff clear los quita)";

        if (args[0].ToLowerInvariant() == "clear")
        {
            p.DebugClearBuffs();
            return "Efectos eliminados.";
        }

        if (!BuffNames.TryGetValue(args[0].ToLowerInvariant(), out BuffType type)) throw new CommandException("Tipo desconocido: " + args[0]);

        float duration = Float(args, 1, 30f);
        p.ApplyBuff(type, DefaultBuffAmount(type), duration);
        return "Efecto " + type + " durante " + duration + " s.";
    }

    static readonly Dictionary<string, string> PotionTypes = new Dictionary<string, string>
    {
        { "salud", "PocionSalud" }, { "fuerza", "PocionFuerza" }, { "velocidad", "PocionVelocidad" },
        { "fuerza2", "PocionFuerzaII" }, { "velocidad2", "PocionVelocidadII" },
        { "regeneracion", "PocionRegeneracion" }, { "disparo", "PocionDisparoContinuo" }
    };

    static GameObject FindPotionPrefab(string typeName)
    {
        List<LootTable> tables = new List<LootTable>(UnityEngine.Object.FindObjectsByType<LootTable>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        foreach (EnemySpawner spawner in UnityEngine.Object.FindObjectsByType<EnemySpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (spawner.enemigoPrefab != null && spawner.enemigoPrefab.GetComponent<LootTable>() != null) tables.Add(spawner.enemigoPrefab.GetComponent<LootTable>());
        foreach (BossArenaSpawner spawner in UnityEngine.Object.FindObjectsByType<BossArenaSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (spawner.enemigosPrefabs == null) continue;
            foreach (GameObject prefab in spawner.enemigosPrefabs)
                if (prefab != null && prefab.GetComponent<LootTable>() != null) tables.Add(prefab.GetComponent<LootTable>());
        }

        foreach (LootTable table in tables)
        {
            List<GameObject> candidates = new List<GameObject>();
            if (table.pociones != null) candidates.AddRange(table.pociones);
            if (table.pocionesMejoradas != null) candidates.AddRange(table.pocionesMejoradas);
            if (table.pocionDisparoContinuo != null) candidates.Add(table.pocionDisparoContinuo);

            foreach (GameObject candidate in candidates)
            {
                if (candidate == null) continue;
                PocionBase potion = candidate.GetComponent<PocionBase>();
                if (potion != null && potion.GetType().Name == typeName) return candidate;
            }
        }
        return null;
    }

    [ConsoleCommand("potion", "Suelta una poción delante del jugador", "potion <salud|fuerza|velocidad|fuerza2|velocidad2|regeneracion|disparo>",
        "salud,fuerza,velocidad,fuerza2,velocidad2,regeneracion,disparo")]
    static string Potion(string[] args)
    {
        if (args.Length == 0) return "Pociones: salud, fuerza, velocidad, fuerza2, velocidad2, regeneracion, disparo";
        if (!PotionTypes.TryGetValue(args[0].ToLowerInvariant(), out string typeName)) throw new CommandException("Poción desconocida: " + args[0]);

        GameObject prefab = FindPotionPrefab(typeName);
        if (prefab == null) throw new CommandException("No encontré el prefab de esa poción en las tablas de botín de la escena.");

        UnityEngine.Object.Instantiate(prefab, InFront(4f) + Vector3.up * 0.4f, Quaternion.identity);
        return "Poción creada: " + args[0];
    }

    [ConsoleCommand("score", "Fija la puntuación (al llegar al objetivo se abre la trampilla como siempre)", "score <n>")]
    static string Score(string[] args)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) throw new CommandException("No hay GameManager en esta escena.");
        if (args.Length > 0) gm.DebugSetScore(Int(args, 0, gm.CurrentScore));
        return "Puntos: " + gm.CurrentScore + " / " + gm.targetScore + (gm.ScoreReached ? " (objetivo alcanzado)" : "");
    }

    [ConsoleCommand("win", "Fuerza la pantalla de victoria")]
    static string Win(string[] args)
    {
        if (GameManager.Instance == null) throw new CommandException("No hay GameManager en esta escena.");
        GameManager.Instance.WinGame();
        return null;
    }

    [ConsoleCommand("lose", "Fuerza la pantalla de derrota")]
    static string Lose(string[] args)
    {
        if (GameManager.Instance == null) throw new CommandException("No hay GameManager en esta escena.");
        GameManager.Instance.PlayerDied();
        return null;
    }

    // ---------- Mundo y ajustes ----------

    [ConsoleCommand("timescale", "Velocidad del tiempo (0.2 cámara lenta, 1 normal, 3 rápido, 0 pausa)", "timescale [x]")]
    static string TimeScale(string[] args)
    {
        if (args.Length > 0) Time.timeScale = Mathf.Clamp(Float(args, 0, 1f), 0f, 10f);
        return "Escala de tiempo: " + Time.timeScale.ToString("0.##");
    }

    [ConsoleCommand("flashlight", "Enciende o apaga la linterna", "flashlight [on/off]", "on,off")]
    static string Flashlight(string[] args)
    {
        FlashlightController f = UnityEngine.Object.FindFirstObjectByType<FlashlightController>();
        if (f == null) throw new CommandException("No hay linterna en esta escena.");
        f.IsOn = Switch(args, 0, f.IsOn);
        return "Linterna " + (f.IsOn ? "encendida" : "apagada");
    }

    [ConsoleCommand("fov", "Campo de visión de la cámara", "fov [grados]")]
    static string Fov(string[] args)
    {
        Weapon w = Gun;
        if (args.Length > 0) w.DebugSetFov(Float(args, 0, w.BaseFov));
        return "FOV: " + w.BaseFov.ToString("0.#");
    }

    [ConsoleCommand("sens", "Sensibilidad del ratón", "sens [x]")]
    static string Sensitivity(string[] args)
    {
        if (args.Length > 0) GameSettings.Sensitivity = Float(args, 0, GameSettings.Sensitivity);
        return "Sensibilidad: " + GameSettings.Sensitivity.ToString("0.##");
    }

    [ConsoleCommand("fog", "Activa o desactiva la niebla", "fog [on/off]", "on,off")]
    static string Fog(string[] args)
    {
        RenderSettings.fog = Switch(args, 0, RenderSettings.fog);
        return "Niebla " + OnOff(RenderSettings.fog);
    }

    [ConsoleCommand("bloom", "Resplandor del postprocesado: on, off o una intensidad", "bloom [on|off|valor]", "on,off")]
    static string BloomCommand(string[] args)
    {
        int found = 0;
        string report = "";
        foreach (Volume volume in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            if (volume.sharedProfile == null || !volume.profile.TryGet(out Bloom bloom)) continue;
            found++;

            if (args.Length > 0)
            {
                string a = args[0].ToLowerInvariant();
                if (a == "on") bloom.active = true;
                else if (a == "off") bloom.active = false;
                else { bloom.active = true; bloom.intensity.Override(Mathf.Max(0f, Float(args, 0, bloom.intensity.value))); }
            }
            report = (bloom.active ? "activo" : "apagado") + ", intensidad " + bloom.intensity.value.ToString("0.##") + ", umbral " + bloom.threshold.value.ToString("0.##");
        }
        if (found == 0) throw new CommandException("No hay efecto Bloom en esta escena.");
        return "Bloom: " + report;
    }

    [ConsoleCommand("shadows", "Activa o desactiva las sombras", "shadows [on/off]", "on,off")]
    static string Shadows(string[] args)
    {
        UniversalRenderPipelineAsset asset = UniversalRenderPipeline.asset;
        if (asset == null) throw new CommandException("No hay pipeline URP activo.");

        bool on = DebugRuntime.ShadowDistanceBackup < 0f;
        on = Switch(args, 0, on);
        if (!on && DebugRuntime.ShadowDistanceBackup < 0f)
        {
            DebugRuntime.ShadowDistanceBackup = asset.shadowDistance;
            asset.shadowDistance = 0f;
        }
        else if (on && DebugRuntime.ShadowDistanceBackup >= 0f)
        {
            asset.shadowDistance = DebugRuntime.ShadowDistanceBackup;
            DebugRuntime.ShadowDistanceBackup = -1f;
        }
        return "Sombras " + OnOff(on);
    }

    // ---------- Información en pantalla ----------

    [ConsoleCommand("fps", "Muestra u oculta el contador de FPS", "fps [on/off]", "on,off")]
    static string Fps(string[] args)
    {
        DebugRuntime.ShowFps = Switch(args, 0, DebugRuntime.ShowFps);
        return "FPS " + OnOff(DebugRuntime.ShowFps);
    }

    [ConsoleCommand("stats", "Muestra u oculta el panel de estadísticas (vida, munición, enemigos, jefe, posición...)", "stats [on/off]", "on,off")]
    static string Stats(string[] args)
    {
        DebugRuntime.ShowStats = Switch(args, 0, DebugRuntime.ShowStats);
        return "Estadísticas " + OnOff(DebugRuntime.ShowStats);
    }

    [ConsoleCommand("hitboxes", "Dibuja los colliders cercanos: jugador (verde), enemigos (rojo), jefe (magenta), balas (amarillo), resto (cian)", "hitboxes [on/off]", "on,off")]
    static string Hitboxes(string[] args)
    {
        DebugRuntime.ShowHitboxes = Switch(args, 0, DebugRuntime.ShowHitboxes);
        return "Hitboxes " + OnOff(DebugRuntime.ShowHitboxes);
    }

    [ConsoleCommand("navmesh", "Dibuja el NavMesh y el destino de cada enemigo", "navmesh [on/off]", "on,off")]
    static string NavMeshCommand(string[] args)
    {
        DebugRuntime.ShowNavMesh = Switch(args, 0, DebugRuntime.ShowNavMesh);
        return "NavMesh " + OnOff(DebugRuntime.ShowNavMesh);
    }

    [ConsoleCommand("missiles", "Dibuja el punto fijado y la zona de explosión de cada misil del jefe", "missiles [on/off]", "on,off")]
    static string Missiles(string[] args)
    {
        DebugRuntime.ShowMissiles = Switch(args, 0, DebugRuntime.ShowMissiles);
        return "Misiles " + OnOff(DebugRuntime.ShowMissiles);
    }
}
