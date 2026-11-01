using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Pool sencillo de prefabs para balas y otros objetos que se crean/destruyen constantemente.
public static class PrefabPool
{
    class PooledObject : MonoBehaviour
    {
        public GameObject prefab;
        public TrailRenderer[] trails;
    }

    static readonly Dictionary<GameObject, Stack<GameObject>> pools = new Dictionary<GameObject, Stack<GameObject>>();
    static Transform root;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        pools.Clear();
        root = null;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    // Los objetos del pool mueren con la escena, así que se descarta todo al cambiar o recargar.
    static void OnSceneUnloaded(Scene scene)
    {
        pools.Clear();
        root = null;
    }

    public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject instance = null;

        if (pools.TryGetValue(prefab, out Stack<GameObject> stack))
        {
            while (stack.Count > 0 && instance == null) instance = stack.Pop();
        }

        if (instance == null)
        {
            instance = Object.Instantiate(prefab, position, rotation, GetRoot());
            PooledObject marker = instance.AddComponent<PooledObject>();
            marker.prefab = prefab;
            marker.trails = instance.GetComponentsInChildren<TrailRenderer>(true);
        }
        else
        {
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);

            foreach (TrailRenderer trail in instance.GetComponent<PooledObject>().trails) trail.Clear();
        }

        return instance;
    }

    public static void Release(GameObject instance)
    {
        if (instance == null) return;

        if (!instance.activeSelf) return;

        PooledObject marker = instance.GetComponent<PooledObject>();
        if (marker == null)
        {
            Object.Destroy(instance);
            return;
        }

        instance.SetActive(false);

        if (!pools.TryGetValue(marker.prefab, out Stack<GameObject> stack))
        {
            stack = new Stack<GameObject>();
            pools[marker.prefab] = stack;
        }
        stack.Push(instance);
    }

    static Transform GetRoot()
    {
        if (root == null) root = new GameObject("PrefabPool").transform;
        return root;
    }
}
