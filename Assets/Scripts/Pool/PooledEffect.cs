using UnityEngine;
using UnityEngine.Pool;

// Put on an effect that is played through EffectPool. When the lifetime is
// over the effect goes back to its pool instead of being destroyed.
// EffectPool adds this component itself if the prefab has none.
public class PooledEffect : MonoBehaviour
{
    [SerializeField] private float lifetime = 1.0f;

    private IObjectPool<GameObject> pool;
    private ParticleSystem[] particleSystems;
    private AudioSource[] audioSources;
    private float timer;

    // Called once by EffectPool when the instance is created.
    internal void Init(IObjectPool<GameObject> ownerPool)
    {
        pool = ownerPool;
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        audioSources = GetComponentsInChildren<AudioSource>(true);
    }

    // Runs every time the pool hands the effect out, so a reused effect
    // starts from the beginning like a fresh one.
    private void OnEnable()
    {
        timer = lifetime;

        if (particleSystems == null)
        {
            return;
        }
        for (int i = 0; i < particleSystems.Length; i++)
        {
            particleSystems[i].Clear(true);
            particleSystems[i].Play(true);
        }
        for (int i = 0; i < audioSources.Length; i++)
        {
            // Play On Awake only fires on the first activation of a new
            // object, but a reused one has to replay as well.
            if (audioSources[i].playOnAwake && audioSources[i].clip != null)
            {
                audioSources[i].Play();
            }
        }
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Release();
        }
    }

    private void Release()
    {
        if (pool != null)
        {
            pool.Release(gameObject);
        }
        else
        {
            // Not created by EffectPool (e.g. dropped into a scene).
            Destroy(gameObject);
        }
    }
}
