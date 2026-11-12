using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Pone la atmósfera de la cripta y de la arena del jefe cada vez que se carga la escena del juego:
// antorchas vivas, efectos de la arena y un resplandor (bloom) algo más marcado.
public static class AtmosphereBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Apply();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply();

    static void Apply()
    {
        Boss boss = Object.FindFirstObjectByType<Boss>(FindObjectsInactive.Include);
        if (boss == null) return;   // solo en la escena del juego

        Transform arena = boss.transform.parent != null ? boss.transform.parent : boss.transform;

        // Antorchas de la cripta: luces puntuales de intensidad 30 y alcance 20 fuera de la arena
        foreach (Light torch in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (torch.type != LightType.Point || torch.transform.IsChildOf(arena)) continue;
            if (!Mathf.Approximately(torch.intensity, 30f) || !Mathf.Approximately(torch.range, 20f)) continue;
            if (torch.GetComponent<TorchFlicker>() == null) torch.gameObject.AddComponent<TorchFlicker>();
        }

        if (arena.GetComponent<ArenaAtmosphere>() == null) arena.gameObject.AddComponent<ArenaAtmosphere>();

        BoostBloom();
    }

    // Resplandor más marcado alrededor de lo muy brillante (llamas, cristal, halos)
    static void BoostBloom()
    {
        foreach (Volume volume in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            if (volume.sharedProfile == null) continue;

            // 'profile' crea una copia propia, así que el archivo del proyecto no se modifica
            if (volume.profile.TryGet(out Bloom bloom))
            {
                bloom.active = true;
                bloom.threshold.Override(1.0f);
                bloom.intensity.Override(0.45f);
                bloom.scatter.Override(0.6f);
                bloom.tint.Override(new Color(1f, 0.9f, 0.78f));
            }
        }
    }
}
