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

| 模式 | 物品数 | RefreshScroll 平均 ms | 切换帧耗时 ms | 切换帧 GC Alloc | 20 次切换后 Sprite 增量 | Content 子节点数 | 滚动时帧耗时 ms |
|---|---|---|---|---|---|---|---|
| Original | 50 | | | | | | |
| Original | 500 | | | | | | |
| Pooled | 50 | | | | | | |
| Pooled | 500 | | | | | | |
| Virtual | 50 | | | | | | |
| Virtual | 500 | | | | | | |

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
