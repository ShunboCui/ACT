# 玩家攻击状态

English version: [PlayerAttackState.md](PlayerAttackState.md)

代码：

- `Assets/Scripts/StateMachines/PlayerStates/PlayerAttackState.cs`：状态本身
- `Assets/Scripts/AttackCombo/ComboManager.cs`：连招播放
- `Assets/Scripts/AttackCombo/ActionTimelineSO.cs`、`CharacterSkillSetSO.cs`：数据类型
- `Assets/Scripts/AttackCombo/NormalAttack1..4.asset`、`PlayerNormalAttack.asset`：数据
- `Assets/Scripts/PlayerControl.cs`：输入、打击盒开关、中断
- `Assets/Scripts/StateMachines/PlayerStates/PlayerStateMachine.cs`：离开攻击状态时的清理

本文描述 `main` 分支上的代码。行为是从代码和数据资产读出来的，没有在运行模式下实测。

## 概览

```
          按下攻击                            到达 End 帧
Idle/Move ─────────> PlayerAttackState ─────────────────> Move（有移动输入）/ Idle
                       │        ▲
                       │        │ 在连招窗口内按下攻击
                       └────────┘（进入下一段，不离开本状态）

                     按下闪避 ──> PlayerDodgeState        （任意时刻）
                     被敌人击中 ──> PlayerTakeDamageState （任意时刻，闪避中除外）
```

`PlayerAttackState` 很薄，自己不管理任何时间，全部交给 `ComboManager`：

| 钩子 | 行为 |
|---|---|
| `Enter` | `ComboManager.StartCombo()`，从第 0 段开始 |
| `Tick` | `UpdateCombo()`；本帧按了攻击则 `NextSegment()`；连招已不在播放则切到 Move 或 Idle |
| `Exit` | `ComboManager.EndCombo()` |

整套连招只进入一次攻击状态。切到下一段时不会退出再重新进入状态。

## 连招数据

一套连招是 `CharacterSkillSetSO` 里的一个 `SkillData`，由若干 `ComboSegment` 组成，
每段指向一个 `ActionTimelineSO`。时间轴是 `ActionFrame { frame, effect }` 的列表。

| 效果 | 结果 |
|---|---|
| `Start` | 只打日志 |
| `HitboxStart` | `PlayerControl.EnableDamageArea()` |
| `HitboxEnd` | `PlayerControl.DisableDamageArea()` |
| `ComboWindowStart` | `comboWindow = true` |
| `ComboWindowEnd` | `comboWindow = false` |
| `End` | `EndCombo()`：`isPlaying = false`，随后状态退出 |

玩家普通攻击（`PlayerNormalAttack.asset`）有四段。帧是 20Hz 的逻辑帧，一帧 0.05 秒。

| 段 | 时间轴 | 动画 | 打击盒开启 | 连招窗口 | 结束 |
|---|---|---|---|---|---|
| 1 | `NormalAttack1` | `combo_02_1` | 6–11（0.30–0.55s） | 7–23（0.35–1.15s） | 24（1.20s） |
| 2 | `NormalAttack2` | `combo_02_2` | 12–23（0.60–1.15s） | 13–33（0.65–1.65s） | 34（1.70s） |
| 3 | `NormalAttack3` | `combo_02_3` | 6–14（0.30–0.70s） | 7–19（0.35–0.95s） | 20（1.00s） |
| 4 | `NormalAttack4` | `combo_02_4` | 8–21（0.40–1.05s） | 9–27（0.45–1.35s） | 28（1.40s） |

每条时间轴的 `End` 帧等于对应动画片段的长度。

## 逻辑帧

`ComboManager.UpdateCombo()` 由 `PlayerAttackState.Tick` 每个渲染帧调用一次：

1. 把 `Time.deltaTime` 累加到 `logicTimer`。
2. 只要 `logicTimer >= 1/20 秒`，就减去 1/20 秒并让 `currentLogicFrame` 加一。
3. 执行当前时间轴中所有 `frame == currentLogicFrame` 的 `ActionFrame`。

由此带来的结果：

- 时间轴用的是受缩放影响的时间，所以游戏暂停（`Time.timeScale = 0`）时它也停。
- 时间轴和动画是两个独立的时钟。每段开始时用 `Animator.CrossFade` 播一次动画，之后没有任何机制让两者保持同步。
- 只要 `currentLogicFrame` 停在某个效果的帧号上，这个效果在每个渲染帧都会执行一次。60 帧下大约是三次。
  目前所有效果都是幂等的，所以没有影响。
- **被跳过的帧会丢失效果。** 如果某个渲染帧耗时超过 0.05 秒，第 2 步会一次推进多个逻辑帧，
  而第 3 步只检查最后一个。`End` 被跳过就永远不会触发，玩家会停在攻击状态，直到闪避或受击打断。
- `StartCombo` 和 `NextSegment` 都不重置 `logicTimer`，所以最多 0.05 秒的余量会带到下一段。

## 输入与连招窗口

攻击输入在 `PlayerControl.DoAttack()` 中读取，它在 `Update` 里先于状态机执行。
`AttackPressed` 只在鼠标左键按下的那一帧为 true。

无论处于什么状态，每次按下还会：

- 让玩家转向最近的存活敌人（`PlayerViewAngle`）；
- 打开武器拖尾特效（`WeaponEffect`），离开攻击状态时关闭。

在攻击状态内，按下会调用 `ComboManager.NextSegment()`：

- **窗口外的按键被忽略。** 没有输入缓冲：窗口打开前的按键直接丢弃，之后不会补触发下一段。
- **窗口内立即开始下一段**，就在当前帧。打击盒关闭，逻辑帧归零，窗口关闭，播放下一段动画。
- 第 4 段之后索引回到第 1 段，所以可以无限连下去。

每条时间轴里，窗口都在打击盒开启后一个逻辑帧打开，在 `End` 前一个逻辑帧关闭。因此：

- 最早可以在打击盒生效 0.05 秒后接下一段。这么早接会截掉当前段剩余的打击盒有效时间。
- 如果玩家在窗口内没有按键，窗口关闭后一帧触发 `End`，连招结束。

`Tick` 先调 `UpdateCombo()` 再调 `NextSegment()`，所以与 `ComboWindowStart` 同一渲染帧的按键有效，
与 `ComboWindowEnd` 同一渲染帧的按键无效。

## 打击判定

`EnableDamageArea` / `DisableDamageArea` 激活和停用 `DealDamageArea` 这个 GameObject，
它是一个标签为 `CharacterAttackBox` 的触发器碰撞体。它进入敌人时，`EnemyControl.OnTriggerEnter` 结算伤害。

- 每一段的伤害都是 `PlayerControl.playerDamage`。数据里有 `ComboSegment.damage`（值为 0），但没有被读取。
- 攻击状态下玩家不能移动：`PlayerControl.Move` 只由移动状态调用。

## 中断

没有优先级表或取消表。有两种情况可以在任意逻辑帧打断攻击状态，两者都在状态之外、由 `PlayerControl` 触发：

| 中断 | 触发 | 条件 |
|---|---|---|
| 闪避 | `PlayerControl.Dodge()` 中的 Jump 动作（空格） | 当前不在闪避中 |
| 受击 | `OnTriggerEnter` 碰到 `EnemyAttackBox` | 未死亡且不在闪避中 |

两者都调用 `SwitchState`，执行 `PlayerAttackState.Exit`（`EndCombo`）。随后 `PlayerStateMachine.SwitchState`
发现是从攻击状态切到非攻击状态，调用 `PlayerControl.QuitAttackingState()`：关闭打击盒、关闭武器特效、
重置 `ComboState`。连招正常结束时走的是同一套清理。

其他要点：

- 闪避可以取消攻击的任何阶段，包括前摇和打击盒生效期间。除了闪避自身的 0.5 秒，没有额外代价或冷却。
- 攻击没有霸体，任何一次敌人命中都会打断。
- 移动输入不能取消攻击，只在 `End` 之后决定回到 Move 还是 Idle。
- 闪避和受击状态下不能发起攻击，两者都要先在 0.5 秒后回到 Move 或 Idle。
- 死亡不会切换状态。`PlayerControl.Dead()` 播放死亡动画，2 秒后打开失败界面，
  但这期间状态机仍在运行，代码没有阻止此时发起攻击。
- 对话期间 `attackEnabled` 为 false，`DoAttack` 直接返回。

## 与旧版本（`PlayerAttackState1.cs`）的对比

`PlayerAttackState1.cs` 是上一版实现，整个文件被注释掉保留着。它不使用 `ComboManager`。
它用的动画片段还在 `Assets/Animations/temp/` 里。

| | 旧版（`PlayerAttackState1`） | 现版（`PlayerAttackState` + `ComboManager`） |
|---|---|---|
| 时间由谁管 | 状态里的一个 float 计时器，加上动画片段上的动画事件 | ScriptableObject 资产里的逻辑帧时间轴 |
| 切段方式 | `PlayerControl` 上的 `ComboState++`，然后 `SwitchState(playerAttackState)`，每一段都退出并重新进入本状态 | 在同一次状态停留内调用 `NextSegment()` |
| 连招窗口打开 | 进入后 0.3 秒，所有段相同，写死在代码里 | 每段各自的 `ComboWindowStart` 帧 |
| 连招窗口关闭 | 不关闭，一直开到动画结束 | `ComboWindowEnd` 帧 |
| 打击盒开 / 关 | 动画事件 `DealDamage` / `DisableDamageArea`。`DealDamage` 的处理函数已不在 `PlayerControl` 中，所以它当时如何生成打击盒，从现有代码无法还原 | 时间轴里的 `HitboxStart` / `HitboxEnd` 帧 |
| 一段结束 | 动画事件 `AttackAnimFinished` → `OnAttackFinished()` | 时间轴里的 `End` 帧 |
| 最后一段之后 | `ComboState` 变成 4，`PlayAttack` 没有对应分支，不播放任何动画 | 回到第 1 段 |
| 连招索引归属 | `PlayerControl.ComboState` | `ComboManager.currentSegmentIndex` |
| 调参方式 | 在 Animation 窗口里改片段事件；0.3 秒要改代码 | 改时间轴资产里的数字 |
| 动画名 | 状态里写死的 `switch` | `ComboManager` 里写死的 `switch`（没变） |
| 与动画的同步 | 打击盒和结束时机与动画严格一致，因为事件由动画发出 | 不保证，两个时钟 |
| 掉帧时 | Unity 仍会补发被跨过的动画事件 | 被跳过的逻辑帧上的效果丢失 |
| 提前接招时的打击盒 | 切段本身不会关闭打击盒 | `NextSegment()` 会关闭 |

现版得到了什么：

- 每段有各自的窗口，会开也会关，不再是一个固定延迟。
- 一个招式的全部时间参数集中在一个数据资产里，新角色或新武器换资产即可，不用改代码。
- 玩法时间不再依赖 Animator。片段被 cross-fade 切走或被打断时，动画事件不保证触发，
  而旧版的打击盒和攻击结束都依赖这些事件。
- 连招索引和窗口成了 `ComboManager` 的私有状态，不再是 `PlayerControl` 上的公开字段。

现版失去的或还没解决的：

- 打击盒时机不再天然跟随动画，时间轴要靠人工与片段对齐。
- 被跳过的逻辑帧会丢失效果（见[逻辑帧](#逻辑帧)）。

代码里还留着的旧版残余：

- `PlayerControl.ComboState` 仍然声明着，并在 `QuitAttackingState` 中重置，但没有别的地方读它。
- `PlayerControl.AttackAnimFinished()` 是个空方法。
- `Assets/Animations/` 下的现用片段只带 `PlayAttackSound` 事件（`combo_02_1` 上另有
  `StartTestTimer` / `StopTestTimer`），所以音效是唯一仍由动画事件驱动的部分。

## 候选的后续工作

- 一次更新里跨过的每个逻辑帧都执行，而不是只执行最后一个。
- 加输入缓冲（即被注释掉的 `nextAttackQueued`），让窗口打开前不久的按键在窗口打开时生效。
- 把动画名放进 `ComboSegment`，并按 id 选择技能，去掉 `switch` 和写死的 `skills[0]`。
- 结算命中时读取 `ComboSegment.damage`。
- 把"哪些状态、哪些帧可以取消到哪里"做成时间轴上的数据（例如只允许在打击盒结束后闪避取消）。
- 确认不再需要参考后，删除 `PlayerAttackState1.cs`、`ComboState`、`AttackAnimFinished` 和 `Assets/Animations/temp/`。
