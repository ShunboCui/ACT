# UI 框架（面板管理器）

English version: [UIFramework.md](UIFramework.md)

代码：`Assets/Scripts/UI/UIManager.cs`、`Assets/Scripts/UI/BasePanel.cs`

## 结构

```
UIManager（单例）
  |- panelInfoDict   名称 -> UIPanelInfo { path, layer }   注册表
  |- openedPanelDict 名称 -> BasePanel                     当前打开的面板
  |- prefabDict      名称 -> 预制体                        Resources.Load 缓存
  |- panelStack      List<BasePanel>                       按打开顺序排列的已打开面板
  '- layerRootDict   UILayer -> RectTransform              每层一个铺满的根节点

Canvas
  |- Layer_Background
  |- Layer_Normal      MainPanel / InventoryPanel / WishPanel / DialoguePanel
  |- Layer_Popup       ItemDetailPanel
  '- Layer_Top         DeadMenu / VictoryMenu
```

## 本次分支的改动

| 改动前 | 改动后 |
|---|---|
| 所有面板都是 Canvas 的直接子节点；绘制顺序 = 打开顺序 | 四个层级（`UILayer`），高层始终画在低层之上 |
| 路径表在私有构造函数中建立 | 注册表 `RegisterPanels()` 在 `Awake` 中填充；`Register(name, path, layer)` 为公开方法 |
| 面板已打开时 `OpenPanel` 返回 null | 返回已打开的面板 |
| 预制体缺失 -> 缓存中存入 null -> NullReferenceException | 打印带路径的日志，不写缓存，返回 null |
| 无法向面板传数据 | `OpenPanel(name, args)` -> `BasePanel.OnOpen(object args)` |
| 没有生命周期钩子 | `OnOpen(args)` / `OnClose()` 虚方法 |
| 同一帧内调用两次 `ClosePanel()` 会操作字典两次 | `isClosing` 保护；仅当字典项仍属于该面板时才移除 |
| 没有通知 | `UIManager.PanelOpened` / `PanelClosed` 事件 |
| 没有返回键；每个面板各自绑定关闭按钮 | 面板栈 + `UIManager.GoBack()`（Esc）。`BasePanel.OnBack()` / `CanGoBack` 让每个面板自己定义"返回"的含义 |
| 只有 `OpenPanel` / `ClosePanel` / `GetPanel` | 新增 `OpenPanel<T>`、`GetPanel<T>`、`IsOpened`、`CloseAllPanels` |
| `openedPanelDict` 是公开字段，由 `BasePanel` 直接修改 | 改为私有；`BasePanel` 调用 `HandlePanelClosed` |

已有的调用（`OpenPanel("MainPanel")`、`ClosePanel(...)`、`GetPanel(...)`）无需修改即可编译。

## 返回键（面板栈）

`UIManager.TopPanel` 是玩家当前看到的面板：`UILayer` 最高的优先，同一层内最后打开的优先。
`UIManager.Update` 把 Esc 交给 `GoBack()`，后者调用 `TopPanel.GoBack()`。

- `OnBack()` 默认执行 `ClosePanel()`。有自己关闭流程的面板会重写它，并复用关闭按钮的处理函数，
  这样 Esc 和按钮的行为不会出现差异：
  `MainPanel`（恢复游戏）、`WishPanel` / `InventoryPanel`（回到主菜单；背包会先退出删除模式）、
  `ItemDetailPanel`（刷新背包）、`DialoguePanel`（结束对话）。
- `CanGoBack => false` 让面板不可关闭，同时挡住它下面的所有面板（`DeadMenu`、`VictoryPanel`）。
- 例子：背包（Normal）-> 物品详情（Popup）。第一次 Esc 关闭物品详情，第二次回到主菜单，第三次恢复游戏。

## 新增面板

1. 把预制体放到 `Resources/Prefabs/Panels/` 下，并挂上继承 `BasePanel` 的脚本。
2. 在 `UIConst` 中添加名称常量。
3. 在 `UIManager.RegisterPanels()` 中加一行。
4. 打开面板：`UIManager.Instance.OpenPanel(UIConst.X, args)`；在 `OnOpen` 中读取 `args`。

## 尚未完成（候选的后续工作）

- 用 `HasOpenPanel` / `PanelOpened` / `PanelClosed` 驱动一个暂停和鼠标锁定计数器，取代各面板自行设置 `Time.timeScale`；玩法输入（攻击、闪避）改为检查 `HasOpenPanel`。
- 单个面板内部的子状态（例如 `ItemDetailPanel` 的强化页）还没有纳入面板栈。
- 缓存并隐藏面板，取代 `Destroy` 后重新 `Instantiate`。需要把现有 `Awake -> InitUI` 的按钮绑定改为只执行一次的初始化，并把 `DialoguePanel` 向 `DialogueManager` 的注册移到 `OnOpen`。
- 把 `transform.Find("A/B")` 字符串路径换成序列化引用。
- 把 `Resources.Load` 换成 Addressables，并提供异步的 `OpenPanelAsync`。
- 把 `Time.timeScale` 和鼠标处理从 `GameManager.Update` 及各面板中移出，集中到一处，由 `PanelOpened` / `PanelClosed` 驱动。
