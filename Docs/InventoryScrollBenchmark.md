# 背包格子：三种刷新方式的性能对比指南

代码：`InventoryPanel.cs`（模式切换、基准辅助）、`InventoryVirtualGrid.cs`（虚拟滚动）、`InventorySlotView.cs`（复用支持、Sprite 缓存）

| 模式 | 做法 | 切换标签时 |
|---|---|---|
| `Original` | 旧代码。每次 `Destroy` 全部格子再 `Instantiate`，每个格子 `Sprite.Create` 一个新 Sprite | N 次销毁 + N 次实例化 |
| `Pooled` | 保留格子，重新绑定数据，多余的 `SetActive(false)` | 只绑定数据，最多新建 N 个（只发生一次） |
| `Virtual` | 只存在视口内 + 上下各一行缓冲的格子，滚动时回收再绑定 | 常数个格子，与物品总数无关 |

`Pooled` 和 `Virtual` 还会开启 Sprite 缓存（同一路径的图标只 `Sprite.Create` 一次）。`Original` 保持旧行为作为基准。

## 1. 切换模式

1. 选中 `Assets/Resources/Prefabs/Panels/Inventory/InventoryPanel.prefab`。
2. Inspector 里找到 `InventoryPanel` 组件，修改 `Scroll Mode`。
3. 勾上 `Log Benchmark`（可选，会在 Console 输出每次刷新的耗时）。
4. 每次改完重新进入 Play。背包面板每次打开都会重新实例化，所以改动在下次打开时生效。

注意：`Pooled`/`Virtual` 是新代码，默认值是 `Original`，不改就是旧行为。

## 2. 造测试数据

1. 进入 Play，打开背包。
2. Hierarchy 里选中 `InventoryPanel(Clone)`。
3. Inspector 里 `InventoryPanel` 组件右上角 ⋮ 菜单 → `Bench/Add 500 test weapons`。
4. 想对比不同数据量时，多点几次（每次 +500），或先从 50 件开始。

测试物品的 uid 以 `bench-` 开头，**没有保存**。但之后任何会调用 `SaveInventory()` 的操作（抽卡、删除、强化）都会把它们写进 PlayerPrefs。测完用 `Bench/Remove test weapons` 清掉，再做其他操作。

## 3. 测量前的准备

- 关闭 Profiler 的 **Deep Profile**，它会让所有数字失真。
- Game 窗口固定分辨率（例如 1920x1080），三种模式用同一个。
- Editor 里的数字噪声大。想要可信的数字，做一个 Development Build 并勾选 Autoconnect Profiler。
- 每种模式用同样的操作脚本，并且先做一轮预热（切 3 次标签）再开始记录，避免第一次加载资源的开销混进来。

## 4. 操作脚本（每种模式都做一遍）

1. 打开背包，等 1 秒。
2. 武器 ↔ 食物标签来回切换 20 次，每次间隔约 0.5 秒。
3. 回到武器标签，从顶部匀速滚动到底再滚回顶部，重复 3 次。
4. 在删除模式里点选 10 个格子，再点返回。

## 5. 看哪些数据

打开 Window → Analysis → Profiler，选 CPU Usage 和 Memory 模块，切到 Hierarchy 视图。

**a. 刷新本身（最直接）**
- 搜索 `Inventory.RefreshScroll`。看 `Calls`、`Total ms`、`GC Alloc` 三列。
- 这个标记只包住同步部分。`Destroy` 的真正清理、布局和 Canvas 重建发生在后面，要看下一条。

**b. 切换那一帧的总耗时**
- 在时间线上点中切换标签的那一帧，看帧总耗时。
- 展开后重点看 `Canvas.SendWillRenderCanvases`（布局重建）、`Canvas.BuildBatch`（合批）、`Layout`、`GC.Collect`。
- `Original` 预计这几项在切换帧最高，因为每个新格子都要重新布局和合批。

**c. GC 分配**
- Hierarchy 里按 `GC Alloc` 列倒序，看切换帧的分配量。
- 注意：数据侧的 `GetInventorySlotItemsByType` 每次都会分配几个 `List`，三种模式相同，这是共同的底噪，不是对比项。

**d. 内存增长（最能暴露 Original 的问题）**
- Original 每次刷新都 `Sprite.Create` 且从不销毁，来回切换 20 次后 Sprite 数量会持续增加。
- 用 Memory Profiler 包（Window → Analysis → Memory Profiler）在测试开始和结束各抓一次快照，Compare 两份，搜 `Sprite`，对比数量差。
- 或者用 Console 里的 `Log Benchmark` 日志，配合 Profiler 的 Memory 模块看 Total Used Memory 的趋势。

**e. 对象数量**
- Play 中展开 Hierarchy 的 `ScrollView/Viewport/Content`，数子节点：
  - `Original`：等于物品数（500+）
  - `Pooled`：等于出现过的最大物品数（隐藏的也算）
  - `Virtual`：只有视口内格子数加缓冲，约 20–40 个，不随物品数增长

**f. 滚动流畅度**
- 滚动期间在 Profiler 里观察每帧耗时。`Original`/`Pooled` 在物品多时，所有可见与不可见的活动格子都参与 Canvas 合批，`Virtual` 在滚动时只回收和绑定边缘的几个格子。

## 6. 记录模板

测试条件：Unity Editor Play 模式，Profiler CPU Usage → Hierarchy，搜索 `Inventory.RefreshScroll`，每组取 3 次切换帧。"—" 表示没有测。

| 模式 | 物品数 | RefreshScroll 平均 ms | 切换帧耗时 ms | 切换帧 GC Alloc | 20 次切换后 Sprite 增量 | Content 子节点数 | 滚动时帧耗时 ms |
|---|---|---|---|---|---|---|---|
| Original | 50 | 613.2 | 638.1 | 2.0 MB | — | — | — |
| Original | 500 | 4576.3 | 4681.5 | 18.7 MB | — | — | — |
| Pooled | 50 | — | — | — | — | — | — |
| Pooled | 500 | — | — | — | — | — | — |
| Virtual | 50 | 9.1 | 21.5 | 4.6 KB | — | — | — |
| Virtual | 500 | 8.9 | 21.5 | 11.6 KB | — | — | — |

### 原始数据（每组 3 次）

| 模式 | 物品数 | RefreshScroll ms | RefreshScroll Self ms | 切换帧 CPU ms | GC Alloc |
|---|---|---|---|---|---|
| Original | 50 | 596.41 / 606.40 / 636.88 | 42.60 / 42.32 / 46.72 | 616.59 / 628.02 / 669.83 | 2.0 / 2.0 / 2.1 MB |
| Original | 500 | 4651.22 / 4517.97 / 4559.78 | 371.81 / 370.31 / 372.14 | 4765.41 / 4617.67 / 4661.56 | 18.7 / 18.4 / 18.9 MB |
| Virtual | 50 | 8.75 / 9.60 / 8.80 | 1.37 / 1.42 / 1.36 | 21.13 / 22.55 / 20.85 | 4.6 / 4.6 / 4.6 KB |
| Virtual | 500 | 8.76 / 8.80 / 9.15 | 1.53 / 1.60 / 1.42 | 21.64 / 20.86 / 21.93 | 11.6 / 11.6 / 11.6 KB |

### 小结

- Original：物品数 ×10，耗时约 ×7.5，GC ×9.3，随数量线性增长。
- Virtual：50 件和 500 件都约 9 ms，与物品数无关。
- 对比 Original：50 件耗时约 68 倍、GC 约 440 倍；500 件耗时约 514 倍、GC 约 1650 倍。
- Virtual 的切换帧仍约 21.5 ms（RefreshScroll 约占 41%），超出 60 FPS 的 16.6 ms，其余部分还没有定位。
- Virtual 的 GC 从 4.6 KB 增到 11.6 KB 来自数据查询（`GetInventorySlotItemsByType` 每次新建 List），不是格子刷新。
- 这是 Editor 数据，绝对值偏大，倍数关系可信；引用绝对毫秒数前需要在 Development Build 重测。

## 7. 公平性与已知差异

- **Sprite 缓存只在 Pooled/Virtual 开启。** 想单独看"格子复用"本身的收益，可以把 `SetupScrollMode` 里 `InventorySlotView.CacheSprites = ...` 临时改成 `false` 重测；想单独看缓存的收益，则在 `Original` 下把它改成 `true`。
- 数据量小（几十件）时三种模式差距很小，虚拟滚动甚至多一点管理开销。差距要在几百件时才明显。
- `Log Benchmark` 的毫秒数只统计调用本身，`Original` 因为 `Destroy` 与布局都在后面执行，会被低估。以 Profiler 的帧耗时为准。
- 新模式相对旧行为有几处刻意的不同：
  - 食物标签没有物品时，隐藏详情卡片，不再残留上一个武器。
  - 切换标签后回到列表顶部。
  - 选中高亮只保留最后点击的那一个格子，滚出视口再回来仍会显示。
- `Virtual` 模式会禁用 Content 上的 `GridLayoutGroup` 和 `ContentSizeFitter`，由代码摆放格子。格子大小、间距、边距是从原 `GridLayoutGroup` 读取的，所以改网格参数仍在 prefab 里改。

## 8. 还没做的优化

- `GetInventorySlotItemsByType` / `GetPackageTableByType` 每次分配新 `List` 并整表遍历，可以按类型缓存。
- 面板每次打开都 Destroy 再 Instantiate，对象池随面板一起丢弃。把面板缓存起来（见 `UIFramework.md` 的待办）后，格子池才能跨次打开复用。
- `InventorySlotView.OnPointerEnter/Exit/Click` 里有 `Debug.Log`，鼠标划过每个格子都会输出，建议删除或用条件编译。

## 9. Profiler 截图

测试条件同 §6：Editor Play 模式，CPU Usage → Hierarchy，搜索 `refreshs`，每组 3 次切换帧。截图在 `Docs/images/`。

### Original

| 50 件 | 500 件 |
|---|---|
| ![Original 50 #1](images/profiler_01.png) | ![Original 500 #1](images/profiler_04.png) |
| 切换帧 616.59 ms，RefreshScroll 596.41 ms，GC 2.0 MB | 切换帧 4765.41 ms，RefreshScroll 4651.22 ms，GC 18.7 MB |

另外两次：50 件 `profiler_02/03.png`，500 件 `profiler_05/06.png`。

### Virtual

| 50 件 | 500 件 |
|---|---|
| ![Virtual 50 #1](images/profiler_07.png) | ![Virtual 500 #1](images/profiler_10.png) |
| 切换帧 21.13 ms，RefreshScroll 8.75 ms，GC 4.6 KB | 切换帧 21.64 ms，RefreshScroll 8.76 ms，GC 11.6 KB |

另外两次：50 件 `profiler_08/09.png`，500 件 `profiler_11/12.png`。

两组截图里 `RefreshScroll` 占切换帧的比例：Original 约 97%，Virtual 约 41%。Original 的帧几乎全是这个方法；Virtual 的帧里还有约 12 ms 在别处。

## 10. 发现总结

### 10.1 瓶颈是 `Sprite.Create`，不是 `Instantiate`

最初的假设是"每次 Destroy 再 Instantiate 全部格子太贵"，Profiler 展开后不成立：

- `RefreshScroll` 的 Self 只有 6–8%，耗时在它调用的子方法里。
- 子方法里占大头的是 `Sprite.Create`，内部是 `SpriteMeshGenerator`（`TraceShape` → `Simplify` → `Decompose`），约占 86%，每次约 7 ms。
- 50 件时共调用 55 次（50 个格子 + 详情卡等），约 400 ms，与总耗时吻合。
- Instantiate / Destroy 的贡献主要是 **GC 分配**（每个新格子产生托管对象和 Text 的 mesh 缓存等），不是时间。

这是 **CPU 主线程**瓶颈：全部同步执行在切换那一帧里，所以画面卡住。GPU 一栏是 `--ms`，没有参与。

### 10.2 为什么 Sprite.Create 慢

- Texture2D 是像素，Sprite 是"贴图的哪一块 + 中心点 + 网格"。
- `Sprite.Create` 默认网格类型是 **Tight**，要扫描贴图像素算出紧贴轮廓的网格，耗时随贴图尺寸增长。
- 原代码每个格子每次刷新都新建一个，而 50 个格子里只有约 5 种图标，等于同一张图重复算几十遍。
- 编辑器里导入为 Sprite 的资源，网格在导入时已算好；运行时 `Resources.Load<Sprite>` 只是读取，几乎免费。
- `Sprite.Create` 出来的对象是 `UnityEngine.Object`，C# GC 不会回收，只有 `Destroy` 或 `Resources.UnloadUnusedAssets` 才会释放，Original 模式会持续增长。

### 10.3 修复（已应用到 `InventorySlotView.GetSprite`）

1. 先查静态字典缓存，同一路径只处理一次。
2. 未命中时先 `Resources.Load<Sprite>`，用导入时算好的 Sprite。
3. 加载不到（贴图是 Multiple 且没切片，或不是 Sprite 类型）时，回退 `Sprite.Create(..., SpriteMeshType.FullRect)`，网格只是 4 个顶点的矩形，几乎无开销。

现在缓存是无条件的，`InventorySlotView.CacheSprites` 字段已经不被 `GetSprite` 使用。因此 §7 里"把 CacheSprites 改成 false 重测"的消融方法**失效**，需要先在 `GetSprite` 里恢复对这个开关的判断才能做。

素材相关：`Weapon_02`–`Weapon_05` 导入为 Sprite Multiple 且没有切片，`Resources.Load<Sprite>` 会返回 null，只能走回退路径。把它们改成 Sprite Mode = Single 即可走快速路径。

### 10.4 结论与可信度

| 结论 | 依据 | 可信度 |
|---|---|---|
| Original 耗时随物品数线性增长（×10 物品 → 约 ×7.5 耗时，×9.3 GC） | 两组各 3 次，重复性好 | 高 |
| Virtual 的刷新耗时与物品数无关（约 9 ms） | 50 / 500 件结果一致 | 高 |
| Virtual 比 Original 快 68×（50 件）/ 514×（500 件） | 同上 | **有混杂**：同时包含了"格子复用"和"Sprite 缓存"两个因素，没有拆开 |
| Instantiate 不是主要瓶颈 | `Sprite.Create` 占约 86% | 中高：来自单次展开的调用树，没有做关掉缓存的对照 |
| 绝对毫秒数 | Editor 里测的 | 低：Editor 有额外开销，倍数可信，毫秒数需要 Development Build 重测 |

### 10.5 仍未解决的问题

- **Virtual 切换帧仍约 21.5 ms**，超过 60 FPS 的 16.6 ms。`RefreshScroll` 只有约 8.8 ms，剩下约 12 ms 没有定位。猜测是 Canvas 重建 / 合批，**未验证**，需要在那一帧展开 `Canvas.SendWillRenderCanvases` 与 `Canvas.BuildBatch` 确认。
- Virtual 剩下的 8.8 ms 里，`Self` 只有 1.4 ms，其余是格子绑定（`GetComponent`、`Text` 赋值、`GetPackageTableItemById`），可以缓存组件引用。
- 数据侧 `GetInventorySlotItemsByType` 每次新建 `List`，Virtual 的 4.6 → 11.6 KB 就来自这里。
- 同样的未缓存 `Sprite.Create` 还在 `ItemDetailCard.cs:65`、`PullCell.cs:58`、`MaterialSlotView.cs:56`。点击一个格子或抽卡时会触发，可以抽成共享的 `SpriteCache.Get(path)`。
- **Pooled 模式没有测**，所以无法单独说明"格子复用"相比"只加 Sprite 缓存"的额外收益。

### 10.6 下一步建议

1. 修复后重测 Original（现在的 `GetSprite` 已经带缓存，所以 Original 会大幅变快），得到"只修 Sprite"的基线。
2. 再测 Pooled、Virtual，才能拆出三个因素各自的贡献：Sprite 缓存、格子复用、虚拟滚动。
3. 展开 Virtual 的 21.5 ms 帧，定位剩下的 12 ms。
4. 补测滚动期间每帧耗时与 GC，这是虚拟滚动真正的卖点，目前没有数据。
5. 在 Development Build（带 Autoconnect Profiler）上重复，再引用绝对数字。

### 10.7 其他提醒

- `ProfilerCaptures/` 里的 `.data` 文件有数百 MB 且未被 git 忽略，建议加入 `.gitignore`，不要提交。
- 对外描述这次优化时，准确说法是"定位到主线程上 `Sprite.Create` 的网格生成是主要耗时，通过缓存和使用导入时生成的 Sprite 消除，再配合虚拟滚动使刷新耗时与物品数无关"，不要说成"虚拟滚动让它快了 500 倍"，因为那个倍数混杂了 Sprite 缓存。
