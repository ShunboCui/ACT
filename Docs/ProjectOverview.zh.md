# ACT 项目总体介绍

English version: [ProjectOverview.md](ProjectOverview.md)

第三人称动作游戏（ACT）原型。玩家登录后进入场景，与 NPC 对话接取任务，用连招击败敌人，
并通过背包、抽卡和武器强化管理装备。

本文描述 `main` 分支的状态。专题文档：

- [UIFramework.zh.md](UIFramework.zh.md)：面板管理器、层级、返回栈
- [PlayerAttackState.zh.md](PlayerAttackState.zh.md)：连招、输入窗口、中断、与旧版本的对比

## 技术栈

| 项目 | 内容 |
|---|---|
| 引擎 | Unity 6（6000.4.9f1） |
| 渲染 | URP 17.4，角色使用自定义 MMD 卡通着色器（`Assets/Shaders/MMDToon`） |
| 输入 | Input System 1.19（`InputSystem_Actions`），部分代码仍用旧 `Input` 类 |
| 相机 | Cinemachine 3.1 |
| 寻路 | AI Navigation 2.0（NavMeshAgent） |
| UI | UGUI + TextMesh Pro |
| 配置数据 | ScriptableObject |
| 存档 | PlayerPrefs + JsonUtility |
| 网络 | UnityWebRequest，HTTP + JSON |

## 游戏流程

```
LoginScene ──登录成功──> de_inferno（游戏场景）
                            │
                            ├─ 走近 NPC，按 F 对话 ──> 接取任务 ──> 刷出任务敌人
                            ├─ 鼠标左键连招 / 空格闪避 ──> 击杀敌人 ──> 任务进度更新
                            ├─ 回到 NPC 对话 ──> 交付任务
                            └─ Tab 打开主菜单 ──> 背包 / 抽卡 / 武器强化
```

构建设置中启用的场景是 `LoginScene` 和 `de_inferno`；`SampleScene` 已停用。

按键：

| 按键 | 作用 |
|---|---|
| WASD | 移动 |
| 鼠标左键 | 攻击 / 连招下一段 |
| 空格 | 闪避；对话中为下一句 |
| F | 与 NPC 对话 |
| Tab | 打开主菜单（暂停游戏） |
| Esc | 返回上一级面板 |
| 左 Alt（按住） | 临时显示鼠标 |

## 模块总览

```
                        ┌──────────────┐
                        │ GameManager  │ 暂停、鼠标、物品表查询、抽卡
                        └──────┬───────┘
        ┌──────────────┬───────┴──────┬───────────────┐
┌───────┴──────┐ ┌─────┴──────┐ ┌─────┴──────┐ ┌──────┴──────┐
│  战斗        │ │  UI 框架   │ │ 任务/对话  │ │ 背包/武器   │
│ PlayerControl│ │ UIManager  │ │QuestManager│ │InventorySlot│
│ EnemyControl │ │ BasePanel  │ │DialogueMgr │ │   Data      │
│ 状态机/连招  │ │ 各面板     │ │EnemySpawner│ │WeaponCtrl   │
└──────────────┘ └────────────┘ └────────────┘ └─────────────┘
```

所有自有代码位于 `Assets/Scripts/`，共约 5,000 行 C#。各管理器都是单例，彼此直接引用。

### 1. 核心管理（`Scripts/GameManager.cs`）

`GameManager` 是跨场景单例（`DontDestroyOnLoad`），目前承担几类职责：

- 暂停与鼠标：Tab 打开主菜单并把 `Time.timeScale` 置 0；锁定和解锁鼠标。
- 物品表查询：从 `Resources/TableData/PackageTable` 加载物品表，按 id 或类型查询。
- 背包操作：读取、排序、删除、扣减数量。
- 抽卡：`PullRandom()` / `PullRandom10()` 从武器类物品中随机抽取并写入存档。
- 相机开关：对话期间停用 Cinemachine 相机。
- 敌人列表：每帧按标签收集场景中的敌人，供玩家攻击时自动朝向最近的敌人。

### 2. 角色与战斗（`Scripts/PlayerControl.cs`、`Scripts/EnemyControl.cs`、`Scripts/StateMachines/`、`Scripts/AttackCombo/`）

**状态机。** `StateMachine` / `State` 是纯 C# 基类（非 MonoBehaviour），由角色控制脚本每帧驱动。
`State` 有 `Enter` / `Tick` / `Exit` 三个钩子。

| 角色 | 状态 |
|---|---|
| 玩家 | Idle、Move、Attack、Dodge、TakeDamage |
| 敌人 | Idle、Move、Attack、TakeDamage |

**玩家。** `PlayerControl` 读取输入，用 `CharacterController` 做相对相机方向的移动，
并持有 `PlayerStateMachine` 和 `ComboManager`。生命值在 `PlayerHealth` 中，
通过 `OnHealthChanged` 事件驱动血条（`PlayerHealthBar`）。

**连招。** 数据驱动，配置是两个 ScriptableObject：

- `CharacterSkillSetSO`：角色的技能列表，每个技能由若干 `ComboSegment`（连招段）组成。
- `ActionTimelineSO`：一段攻击的帧时间轴，每个 `ActionFrame` 在指定逻辑帧触发一个效果。

`ComboManager` 以 20Hz 的逻辑帧推进时间轴，支持的效果有：
`Start`、`HitboxStart`、`HitboxEnd`、`ComboWindowStart`、`ComboWindowEnd`、`End`。
玩家在连招窗口内再次按攻击键即进入下一段。
详见 [PlayerAttackState.zh.md](PlayerAttackState.zh.md)。

**打击判定。** 攻击方在指定帧激活一个带 Trigger 的伤害区域，受击方在 `OnTriggerEnter`
中按标签（`CharacterAttackBox` / `EnemyAttackBox`）识别并扣血。闪避期间玩家不受伤害。

**敌人。** `EnemyControl` 用 `NavMeshAgent` 追击玩家，按与玩家的距离在待机、移动、攻击间切换
（`searchRange` / `attackRange`），死亡时通过 `QuestEnemyContext` 上报任务系统。

### 3. UI 框架（`Scripts/UI/`）

`UIManager` 是面板管理器，面板预制体放在 `Resources/Prefabs/Panels/`，都继承 `BasePanel`。

- 注册表：`RegisterPanels()` 中登记面板名、预制体路径、所在层。
- 四个层级：`Background` < `Normal` < `Popup` < `Top`，高层始终画在低层之上。
- 生命周期：`OnOpen(args)` / `OnClose()`，打开时可传参。
- 返回栈：Esc 调用 `GoBack()`，由最上层面板的 `OnBack()` 决定行为；`CanGoBack` 为 false 的面板不可关闭。
- 事件：`PanelOpened` / `PanelClosed`。

| 面板 | 层 | 作用 |
|---|---|---|
| `MainPanel` | Normal | 主菜单（暂停时） |
| `InventoryPanel` | Normal | 背包，含删除模式 |
| `WishPanel` | Normal | 抽卡 |
| `DialoguePanel` | Normal | NPC 对话 |
| `ItemDetailPanel` | Popup | 物品详情与武器强化 |
| `VictoryPanel` / `DeadMenu` | Top | 胜利 / 死亡界面 |

`QuestTrackerPanel`（任务追踪）和 `PlayerHealthBar`（血条）是常驻 HUD，不经过 `UIManager`。

详见 [UIFramework.zh.md](UIFramework.zh.md)。

### 4. 背包、抽卡与武器（`Scripts/UI/Inventory/`、`Scripts/Weapon/`）

- **物品表** `PackageTable`（ScriptableObject）：每个 `PackageTableItem` 含 id、类型、星级、
  名称、描述、图标路径、模型路径、强化经验值。类型分为武器（0）、食物（1）、材料（2）。
- **存档** `InventorySlotData`：玩家持有的物品列表（`InventorySlotItem`：uid、物品 id、数量、经验、是否新获得），
  序列化为 JSON 存入 PlayerPrefs。武器等级由经验推算（每 2000 经验一级）。
- **背包界面**：`InventoryPanel` + `InventorySlotView`（格子）+ `ItemDetailCard`（详情卡）。
- **武器强化**：`ItemDetailPanel` 中选择材料（`MaterialSlotView`）消耗后增加武器经验。
- **抽卡**：`WishPanel` 调用 `GameManager.PullRandom()` / `PullRandom10()`，结果用 `PullCell` 展示。
- **换装** `WeaponController`：把武器模型挂到角色手部挂点。已实例化的武器会缓存复用，换武器时只做显隐切换。

### 5. 任务与对话（`Scripts/Quest/`、`Scripts/Dialogue/`）

```
NPCInteractable ──F──> DialogueManager ──接受──> QuestManager.AcceptQuest
                                                     │ OnQuestAccepted
                                                     ▼
                                               EnemySpawner 生成敌人
                                                     │ 敌人死亡
                                                     ▼
                              QuestEnemyContext ──> QuestManager.ReportEnemyKilled
                                                     │ OnProgressChanged
                                                     ▼
                                               QuestTrackerPanel 刷新
```

- `QuestDefinition`（ScriptableObject）：任务 id、名称、描述、目标敌人 id、需击杀数、奖励经验。
- `QuestManager`：维护每个任务的运行时状态（`InProgress` → `ReadyToTurnIn` → `Completed`），
  对外提供 `OnQuestAccepted`、`OnProgressChanged`、`OnQuestCompleted` 三个事件。
  奖励发放（`GrantReward`）尚未实现。
- `EnemySpawner`：监听任务接取事件，按 `QuestSpawnConfig` 在指定刷怪点生成敌人。
- `DialogueData` + `DialogueManager`：逐句推进对话，最后一句显示接受 / 交付按钮；
  对话期间禁用攻击、解锁鼠标、停用相机。
- `NPCInteractable`：玩家进入触发范围后显示交互提示，按 F 开始对话。

### 6. 登录与网络（`Scripts/Network/GameServerClient.cs`、`Scripts/UI/LoginSpiralBackground.cs`）

- `GameServerClient`：向游戏服务器 `POST /api/auth/login` 发送用户名和密码（JSON），
  成功后把 `userId` 存入 PlayerPrefs 并加载游戏场景。默认地址是 `https://localhost:7031`。
  服务器代码不在本仓库中。
- `LoginSpiralBackground`：登录界面背景，多个角色模型旋转上升。

目前只有登录走网络，背包和任务数据仍是本地存档。

### 7. 编辑器工具（`Assets/Editor/GMCmd.cs`）

菜单栏 `GMCmd` 下的调试命令：打印物品表、生成测试背包存档、打印背包、打开背包面板、切换武器、开关相机。

## 目录结构

```
Assets/
├─ Scripts/            自有代码
│  ├─ AttackCombo/     连招数据与 ComboManager
│  ├─ StateMachines/   状态机基类、玩家状态、敌人状态
│  ├─ UI/              UIManager、BasePanel、各面板；Inventory/ 为背包相关
│  ├─ Quest/           任务、刷怪
│  ├─ Dialogue/        对话、NPC 交互
│  ├─ Weapon/          武器换装
│  └─ Network/         登录请求
├─ Editor/             GM 调试命令
├─ Resources/          运行时加载：面板与武器预制体、UI 图片、物品表
├─ Scenes/             LoginScene、de_inferno、SampleScene
├─ Shaders/MMDToon/    MMD 角色的 URP 卡通着色器
├─ Prefabs/            角色、打击判定盒、受击特效
└─ 其余目录            第三方美术与插件资源（场景、角色、动画、特效、天空盒等）
Docs/                  项目文档
```

## 已知限制

- 没有程序集定义（asmdef），自有代码与第三方脚本都编译进 `Assembly-CSharp`。
- 没有自动化测试。
- 资源通过 `Resources.Load` 同步加载，未使用 Addressables。
- 暂停、鼠标锁定的逻辑分散在 `GameManager`、`DialogueManager` 和各面板中。
- 连招的动画名在 `ComboManager` 中硬编码，且只使用技能表中的第一个技能。
- 受击特效每次命中都 `Instantiate`；对象池方案在 `dev/claude/hit-effect-pool` 分支上，尚未合入。
- 任务进度不存档，任务奖励未发放。
