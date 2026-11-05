using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// Pools short-lived effect objects (hit sparks etc.) so they are reused
// instead of Instantiate / Destroy on every hit.
//
//   EffectPool.Instance.Play(effectPrefab, position, rotation);
//
// One ObjectPool per prefab. A played effect gives itself back through
// PooledEffect.Release when its lifetime is over.
public class EffectPool : MonoBehaviour
{
    private const int DefaultCapacity = 8;
    private const int MaxSize = 32;

    private static EffectPool _instance;

    public static EffectPool Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject poolObject = new GameObject("EffectPool");
                DontDestroyOnLoad(poolObject);
                _instance = poolObject.AddComponent<EffectPool>();
            }
            return _instance;
        }
    }

    private readonly Dictionary<GameObject, ObjectPool<GameObject>> pools =
        new Dictionary<GameObject, ObjectPool<GameObject>>();

    // Plays an instance of prefab at the given pose. Returns it, or null if prefab is null.
    public GameObject Play(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return null;
        }

        ObjectPool<GameObject> pool = GetPool(prefab);
        GameObject effect = pool.Get();
        effect.transform.SetPositionAndRotation(position, rotation);

        // Activating last so OnEnable already sees the final pose.
        effect.SetActive(true);
        return effect;
    }

    private ObjectPool<GameObject> GetPool(GameObject prefab)
    {
        ObjectPool<GameObject> pool;
        if (pools.TryGetValue(prefab, out pool))
        {
            return pool;
        }

        pool = new ObjectPool<GameObject>(
            createFunc: () => CreateInstance(prefab),
            actionOnGet: null,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyInstance,
            collectionCheck: false,
            defaultCapacity: DefaultCapacity,
            maxSize: MaxSize);
        pools.Add(prefab, pool);
        return pool;
    }

    private GameObject CreateInstance(GameObject prefab)
    {
        // Created inactive so Awake / OnEnable run when Play activates it.
        GameObject effect = Instantiate(prefab, transform);
        effect.SetActive(false);

        PooledEffect pooled = effect.GetComponent<PooledEffect>();
        if (pooled == null)
        {
            pooled = effect.AddComponent<PooledEffect>();
        }
        pooled.Init(pools[prefab]);
        return effect;
    }

    private void OnRelease(GameObject effect)
    {
        effect.SetActive(false);
        effect.transform.SetParent(transform, false);
    }

    private void OnDestroyInstance(GameObject effect)
    {
        Destroy(effect);
    }
}
