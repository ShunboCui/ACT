# 性能排查指南：受击特效对象池 A/B 对比

目标：用数据证明（或证伪）"受击特效改成对象池后性能更好"，而不是凭感觉。
流程和公司项目一致：**定义指标 → 固定场景 → 测基线 → 改动 → 同条件复测 → 对比 → 结论**。

对应代码：`Assets/Scripts/Pool/EffectPool.cs`、`PooledEffect.cs`（分支 `dev/claude/hit-effect-pool`）。

---

## 1. 工具清单

| 工具 | 用途 | 本次是否必需 |
|---|---|---|
| Unity Profiler（CPU Usage / Memory / Rendering 模块） | 看每帧耗时、`GC.Alloc`、`Instantiate`/`Destroy` 标记 | 必需 |
| Profile Analyzer（包 `com.unity.performance.profile-analyzer`） | 把两次抓取的 `.data` 做统计和对比（中位数、P95、最大帧） | 强烈建议 |
| Memory Profiler（包 `com.unity.memory-profiler`） | 抓内存快照，对比对象数量、托管堆大小 | 建议 |
| Frame Debugger | 看 DrawCall 和合批，确认特效是否增加渲染开销 | 可选 |
| Unity Test Framework + Performance Testing（`com.unity.test-framework.performance`） | 把对比写成自动化性能测试，进 CI 防回退 | 进阶 |
| `ProfilerRecorder`（代码内置采样） | 在真机/包体里不依赖 Profiler 窗口，直接输出帧耗时和 GC 数据 | 建议 |
| 平台工具：Android Studio Profiler / Snapdragon Profiler、Xcode Instruments、RenderDoc | 真机瓶颈（CPU/GPU/发热）。PC 项目可跳过 | 看目标平台 |

安装 Profile Analyzer 和 Memory Profiler：`Window > Package Manager > Unity Registry`。

---

## 2. 先建立正确的预期

- 一次受击只生成一个小特效，单次开销很小。对象池的收益主要体现在：
  1. **GC.Alloc 减少**，从而 **GC 触发造成的卡顿尖峰（最大帧耗时、P99）减少**。
  2. `Instantiate` / `Destroy` 的 CPU 耗时尖峰减少。
- 平均帧耗时可能几乎不变，甚至因为池里每个特效多了一次 `Clear/Play` 而略有上升。**这是正常结论，不是失败。** 判断标准看尖峰和 GC，不看平均 FPS。
- 单次受击不足以看出差异，必须用**压力场景**放大：每秒几十次受击，持续一分钟以上。

---

## 3. 实验设计

### 3.1 控制变量
- 同一场景、同一特效预制体、同一机器、同一画质设置。
- 关闭 VSync（`QualitySettings.vSyncCount = 0`），固定 `Application.targetFrameRate = 60`，避免帧率波动干扰。
- 先预热 10 秒再开始记录（首次加载、JIT、池的预分配都会污染数据）。
- 每个版本抓 **3 次**，取中位数。
- 编辑器里的数据只用于定位问题；**最终对比用 Development Build**（编辑器自身开销大，`GC.Alloc` 也会被编辑器干扰）。

### 3.2 A/B 切换方式
推荐在**同一个构建里用开关切换**，这样除了"是否用池"之外没有任何差异：

- A 组（基线）：`Instantiate` + 到时间 `Destroy`（等价于原来的写法）
- B 组：`EffectPool.Instance.Play`

> 说明：未经池创建的 `PooledEffect` 到时间会走 `Destroy`（见 `PooledEffect.Release`），所以 A 组行为和改动前一致。

最后再做一次**端到端验证**：用 git 切回改动前的提交，跑真实游戏流程，确认结论一致。

```bash
# 查看改动前后的差异，需要时用 worktree 保留一份基线版本
git worktree add ../ACT_baseline dev/claude/UI-panel-layers
```

### 3.3 压力测试脚本（放 `Assets/Scripts/Debug/`，不要提交）

```csharp
using UnityEngine;

// 每秒触发 hitsPerSecond 次受击特效。用于 A/B 对比。
public class HitEffectStressTest : MonoBehaviour
{
    public GameObject effectPrefab;   // 拖入 EnemyGetHitEffect 预制体
    public Transform spawnPoint;
    public bool usePool = true;       // A 组 false，B 组 true
    public int hitsPerSecond = 30;

    private float acc;

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    void Update()
    {
        acc += hitsPerSecond * Time.deltaTime;
        while (acc >= 1f)
        {
            acc -= 1f;
            Vector3 pos = spawnPoint.position + Random.insideUnitSphere;
            if (usePool)
                EffectPool.Instance.Play(effectPrefab, pos, Quaternion.identity);
            else
                Instantiate(effectPrefab, pos, Quaternion.identity);
        }
    }
}
```

### 3.4 数据采样脚本（可选，真机/包体里也能用）

```csharp
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

// 预热 warmup 秒后记录 duration 秒，结束时在 Console 输出汇总。
public class FrameStatsRecorder : MonoBehaviour
{
    public float warmup = 10f;
    public float duration = 60f;

    private ProfilerRecorder gcAlloc;
    private readonly List<float> frameMs = new List<float>();
    private long totalGcBytes;
    private float t;
    private bool done;

    void OnEnable()
    {
        gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
    }

    void OnDisable() { gcAlloc.Dispose(); }

    void Update()
    {
        if (done) return;
        t += Time.unscaledDeltaTime;
        if (t < warmup) return;

        frameMs.Add(Time.unscaledDeltaTime * 1000f);
        totalGcBytes += gcAlloc.LastValue;

        if (t >= warmup + duration)
        {
            done = true;
            frameMs.Sort();
            float p50 = frameMs[frameMs.Count / 2];
            float p99 = frameMs[(int)(frameMs.Count * 0.99f)];
            float max = frameMs[frameMs.Count - 1];
            Debug.Log($"[Stats] frames={frameMs.Count} p50={p50:F2}ms p99={p99:F2}ms max={max:F2}ms " +
                      $"gcAlloc/frame={totalGcBytes / (float)frameMs.Count:F0}B");
        }
    }
}
```

---

## 4. 操作步骤

### 步骤 1：准备场景
1. 新建一个测试场景（或在现有场景放一个空物体）。
2. 挂上 `HitEffectStressTest` 和 `FrameStatsRecorder`，`effectPrefab` 拖入 `Assets/Prefabs/EnemyGetHitEffect.prefab`，`spawnPoint` 指向相机前方的一个空物体。
3. 场景里不要放其他会随机变化的东西（敌人 AI、随机刷怪），避免干扰。

### 步骤 2：构建 Development Build
1. `File > Build Profiles`（或 Build Settings）勾选 **Development Build** 和 **Autoconnect Profiler**。
2. 脚本后端和目标平台与最终发布保持一致（有条件用 IL2CPP）。
3. 构建并运行。

### 步骤 3：抓 A 组（基线，`usePool = false`）
1. 构建运行，同时在编辑器打开 `Window > Analysis > Profiler`，选择连接到该构建。
2. 等预热 10 秒，开始录制 60 秒。
3. 在 Profiler 里重点看：
   - **CPU Usage > Timeline/Hierarchy**：搜索 `Instantiate`、`Destroy`、`GC.Collect`，记录它们的总耗时和调用次数。
   - **GC Alloc 列**：按它排序，看 `Instantiate` 和相关调用的每帧分配。
   - 找出 **最慢的几帧**，点进去看是不是 `GC.Collect` 或 `Instantiate`。
4. 保存：Profiler 窗口的 **Save** 按钮保存为 `A_nopool_1.data`。重复 3 次。
5. 记下 `FrameStatsRecorder` 在 Console 输出的汇总行。

### 步骤 4：抓内存快照
1. 打开 `Window > Analysis > Memory Profiler`。
2. 压测运行到稳定后抓一次 **Snapshot**，命名 `A_nopool_mem`。
3. 记录：托管堆大小（Managed Heap）、场景中 `ParticleSystem` / `GameObject` 数量。

### 步骤 5：抓 B 组（`usePool = true`）
1. 保持**完全相同的条件**，只把 `usePool` 改为 true，重复步骤 3、4，文件命名 `B_pool_1.data` 等。
2. 额外观察：Hierarchy 里 `EffectPool` 的子物体数量应稳定在一个小数，不会随时间增长。如果一直增长说明有泄漏。

### 步骤 6：对比
1. 打开 `Window > Analysis > Profile Analyzer`。
2. 载入 A 组一份数据作为 Data 1，B 组一份作为 Data 2，进入 **Compare** 视图，对比：
   - 帧耗时的中位数、上四分位、最大值
   - `Instantiate`、`Destroy`、`GC.Collect`、`ParticleSystem.Update` 的 Total / Count / Max
3. Memory Profiler 里选 **Compare Snapshots**，对比两个快照的托管堆和对象数。

### 步骤 7：记录结论
用下面的表格填结果（示例列，数值自己填）：

| 指标 | A：无对象池 | B：对象池 | 变化 |
|---|---|---|---|
| 帧耗时 P50 (ms) | | | |
| 帧耗时 P99 (ms) | | | |
| 最大帧耗时 (ms) | | | |
| GC.Alloc / 帧 (B) | | | |
| 60 秒内 GC.Collect 次数 | | | |
| `Instantiate` 总耗时 (ms) | | | |
| `ParticleSystem.Update` 总耗时 (ms) | | | |
| 托管堆峰值 (MB) | | | |

---

## 5. 结果怎么解读

| 现象 | 可能原因 | 下一步 |
|---|---|---|
| B 的 GC.Alloc 明显下降，P99/最大帧下降 | 符合预期，池生效 | 可以合并 |
| 平均帧耗时没变化 | 正常，池优化的是尖峰 | 看 P99 和最大帧 |
| B 的 GC.Alloc 仍然不低 | 还有别处分配，如 `GetComponentsInChildren`、字符串、日志 | 在 Profiler 里按 GC Alloc 排序找来源 |
| B 的 `ParticleSystem.Update` 更高 | 池里的特效没停，或同时存在的特效更多 | 检查 `OnRelease` 是否停了粒子 |
| EffectPool 子物体持续增长 | 特效没回池，或 maxSize 设置过大 | 检查 `PooledEffect.Release` 是否被调用 |
| 两组几乎没差别 | 压力不够，或瓶颈根本不在这里 | 提高 `hitsPerSecond`，检查真正的热点（见下） |

**重要**：如果对比发现差异很小，要在结论里如实写"收益有限"，并回到 Profiler 的 Hierarchy 视图按 **Self ms** 排序，看真正的热点。前面分析里提到的 `FindGameObjectsWithTag` 每帧调用等，很可能比受击特效更值得优化。

---

## 6. 常见坑

- **Deep Profile** 会让所有函数都被插桩，数据严重失真，只用于定位，不用于对比。
- 编辑器模式下首次运行有 Shader 编译、资源加载，必须预热。
- Profiler 窗口本身开销不小，与 `FrameStatsRecorder` 的数据可能有差异，对比时每组用同样方式。
- 编辑器里 `GC.Alloc` 常多出编辑器自己的分配，`Instantiate` 在编辑器里比包体里更贵，**别拿编辑器数据当最终结论**。
- 笔记本电脑要接电源、关掉省电模式，避免降频。
- 结论里要写明：机器型号、Unity 版本（6000.4.9f1）、构建类型、测试参数（`hitsPerSecond`、时长）。

---

## 7. 进阶：做成自动化性能测试

用 Performance Testing 扩展把对比固化下来，之后别人改了池相关代码，CI 会发现回退。

```csharp
using System.Collections;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

public class HitEffectPerfTests
{
    [UnityTest, Performance]
    public IEnumerator Pooled_Play()
    {
        var prefab = Resources.Load<GameObject>("..."); // 换成实际路径
        yield return Measure.Frames()
            .WarmupCount(30)
            .MeasurementCount(300)
            .ProfilerMarkers("GC.Alloc")
            .Run();
        // 在测试体里按帧调用 EffectPool.Instance.Play(...)
    }
}
```

要点：用 Baseline 功能记录基线值，之后的运行与基线比较，超出阈值就失败。

---

## 8. 这次对比的验收标准（建议）

- 压测 60 秒内 `GC.Collect` 次数不高于基线。
- 受击相关的 `GC.Alloc` 每帧下降。
- 最大帧耗时和 P99 不高于基线。
- `EffectPool` 子物体数量稳定，不随时间增长。
- 视觉上特效与改动前一致（位置、时长、音效）。
