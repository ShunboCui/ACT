using UnityEngine;

/// <summary>
/// 登录界面背景：大量角色模型各自绕自己的 Y 轴旋转并向上飘升，速度随机，彼此保持间距。
/// 挂在场景中的空物体上（放在摄像机前方、Canvas 后面的位置），把角色模型拖到 characterPrefab。
/// 提示：Canvas 用 Screen Space - Overlay 时 UI 永远在 3D 物体之上；
/// 若用 Screen Space - Camera / World Space，请让 Canvas 的 Plane Distance 小于本物体到摄像机的距离。
/// </summary>
public class LoginSpiralBackground : MonoBehaviour
{
    [Header("角色")]
    [Tooltip("Assets/soledier11/11号.fbx")]
    public GameObject characterPrefab;
    [Tooltip("可选：张开双臂的动画控制器；留空则保持模型导入时的姿势")]
    public RuntimeAnimatorController animatorController;
    [Min(1)] public int count = 24;
    public float modelScale = 1f;

    [Header("区域（以本物体为中心，局部坐标）")]
    [Tooltip("X 方向宽度（屏幕左右）")]
    public float areaWidth = 24f;
    [Tooltip("Z 方向深度（离摄像机远近）")]
    public float areaDepth = 10f;
    [Tooltip("底部起点高度")]
    public float bottomY = -8f;
    [Tooltip("顶部终点高度")]
    public float topY = 12f;
    [Tooltip("两个角色在水平面上的最小间距")]
    public float minDistance = 3f;

    [Header("运动（每个角色随机）")]
    public Vector2 riseSpeedRange = new Vector2(0.6f, 2.5f);
    [Tooltip("绕 Y 轴自转速度（度/秒），随机正负方向")]
    public Vector2 spinSpeedRange = new Vector2(20f, 120f);
    [Tooltip("初始高度是否随机分布（否则全部从底部出发）")]
    public bool randomizeStartHeight = true;
    [Range(0f, 0.5f)] public float fadeZone = 0.1f;
    public float bobAmount = 0.2f;

    Transform[] _items;
    Vector2[] _lane;      // 每个角色固定的水平位置 (x, z)
    float[] _y, _rise, _spin, _bobSeed;

    void Start()
    {
        if (characterPrefab == null)
        {
            Debug.LogWarning("[LoginSpiralBackground] 未指定 characterPrefab", this);
            enabled = false;
            return;
        }

        _items = new Transform[count];
        _lane = new Vector2[count];
        _y = new float[count];
        _rise = new float[count];
        _spin = new float[count];
        _bobSeed = new float[count];

        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(characterPrefab, transform);
            go.name = $"{characterPrefab.name}_{i:00}";
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>()) Destroy(rb);

            if (animatorController != null)
            {
                var anim = go.GetComponentInChildren<Animator>();
                if (anim == null) anim = go.AddComponent<Animator>();
                anim.runtimeAnimatorController = animatorController;
            }

            _items[i] = go.transform;
            _lane[i] = PickLane(i);
            _y[i] = randomizeStartHeight ? Random.Range(bottomY, topY) : bottomY;
            Randomize(i);
            _items[i].localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            _bobSeed[i] = Random.value * 100f;
            Apply(i);
        }
    }

    // 拒绝采样：尽量保证与已有角色的水平距离 >= minDistance；
    // 放不下时取"离最近邻最远"的候选点。
    Vector2 PickLane(int index)
    {
        Vector2 best = default;
        float bestDist = -1f;
        for (int attempt = 0; attempt < 40; attempt++)
        {
            var p = new Vector2(Random.Range(-areaWidth * 0.5f, areaWidth * 0.5f),
                                Random.Range(-areaDepth * 0.5f, areaDepth * 0.5f));
            float nearest = float.MaxValue;
            for (int j = 0; j < index; j++)
                nearest = Mathf.Min(nearest, Vector2.Distance(p, _lane[j]));
            if (nearest >= minDistance) return p;
            if (nearest > bestDist) { bestDist = nearest; best = p; }
        }
        return best;
    }

    void Randomize(int i)
    {
        _rise[i] = Random.Range(riseSpeedRange.x, riseSpeedRange.y);
        _spin[i] = Random.Range(spinSpeedRange.x, spinSpeedRange.y) * (Random.value < 0.5f ? -1f : 1f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < count; i++)
        {
            _y[i] += _rise[i] * dt;
            if (_y[i] > topY)
            {
                _y[i] = bottomY;
                Randomize(i);
            }
            _items[i].Rotate(0f, _spin[i] * dt, 0f, Space.Self);
            Apply(i);
        }
    }

    void Apply(int i)
    {
        var tr = _items[i];
        tr.localPosition = new Vector3(_lane[i].x,
                                       _y[i] + Mathf.Sin(Time.time + _bobSeed[i]) * bobAmount,
                                       _lane[i].y);

        float h01 = Mathf.InverseLerp(bottomY, topY, _y[i]);
        float fade = fadeZone > 0f ? Mathf.Clamp01(Mathf.Min(h01, 1f - h01) / fadeZone) : 1f;
        tr.localScale = Vector3.one * (modelScale * Mathf.SmoothStep(0f, 1f, fade));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        var center = transform.position + Vector3.up * (bottomY + topY) * 0.5f;
        Gizmos.DrawWireCube(center, new Vector3(areaWidth, topY - bottomY, areaDepth));
    }
}
