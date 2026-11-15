# ACT project overview

中文版：[ProjectOverview.zh.md](ProjectOverview.zh.md)

A third-person action game (ACT) prototype. After logging in, the player enters the scene, talks to an
NPC to accept a quest, defeats enemies with combo attacks, and manages equipment through the inventory,
gacha and weapon enhancement.

This document describes the state of the `main` branch. Topic documents:

- [UIFramework.md](UIFramework.md) — panel manager, layers, back stack
- [PlayerAttackState.md](PlayerAttackState.md) — combo, input window, interrupts, comparison with the old version

## Tech stack

| Item | Content |
|---|---|
| Engine | Unity 6 (6000.4.9f1) |
| Rendering | URP 17.4; characters use a custom MMD toon shader (`Assets/Shaders/MMDToon`) |
| Input | Input System 1.19 (`InputSystem_Actions`); some code still uses the legacy `Input` class |
| Camera | Cinemachine 3.1 |
| Pathfinding | AI Navigation 2.0 (NavMeshAgent) |
| UI | UGUI + TextMesh Pro |
| Config data | ScriptableObject |
| Save data | PlayerPrefs + JsonUtility |
| Network | UnityWebRequest, HTTP + JSON |

## Game flow

```
LoginScene ──login succeeds──> de_inferno (game scene)
                                  │
                                  ├─ Walk to the NPC, press F ──> accept quest ──> quest enemies spawn
                                  ├─ Left mouse combo / Space dodge ──> kill enemies ──> quest progress updates
                                  ├─ Talk to the NPC again ──> turn in the quest
                                  └─ Tab opens the main menu ──> inventory / gacha / weapon enhancement
```

The scenes enabled in the build settings are `LoginScene` and `de_inferno`; `SampleScene` is disabled.

Controls:

| Key | Action |
|---|---|
| WASD | Move |
| Left mouse button | Attack / next combo segment |
| Space | Dodge; next line during a dialogue |
| F | Talk to an NPC |
| Tab | Open the main menu (pauses the game) |
| Esc | Back to the previous panel |
| Left Alt (hold) | Show the cursor temporarily |

## Module map

```
                        ┌──────────────┐
                        │ GameManager  │ pause, cursor, item table queries, gacha
                        └──────┬───────┘
        ┌──────────────┬───────┴──────┬───────────────┐
┌───────┴──────┐ ┌─────┴──────┐ ┌─────┴──────┐ ┌──────┴──────┐
│ Combat       │ │ UI         │ │ Quest /    │ │ Inventory / │
│ PlayerControl│ │ UIManager  │ │ Dialogue   │ │ Weapon      │
│ EnemyControl │ │ BasePanel  │ │QuestManager│ │InventorySlot│
│ state machine│ │ panels     │ │DialogueMgr │ │   Data      │
│ / combo      │ │            │ │EnemySpawner│ │WeaponCtrl   │
└──────────────┘ └────────────┘ └────────────┘ └─────────────┘
```

All first-party code is under `Assets/Scripts/`, about 5,000 lines of C#. Every manager is a singleton
and they reference each other directly.

### 1. Core management (`Scripts/GameManager.cs`)

`GameManager` is a cross-scene singleton (`DontDestroyOnLoad`). It currently has several jobs:

- Pause and cursor: Tab opens the main menu and sets `Time.timeScale` to 0; locks and unlocks the cursor.
- Item table queries: loads the item table from `Resources/TableData/PackageTable` and looks items up by id or type.
- Inventory operations: read, sort, delete, reduce amounts.
- Gacha: `PullRandom()` / `PullRandom10()` pick random weapon items and write them to the save data.
- Camera switch: disables the Cinemachine camera during a dialogue.
- Enemy list: collects the enemies in the scene by tag every frame, so that the player can turn to the
  nearest enemy when attacking.

### 2. Characters and combat (`Scripts/PlayerControl.cs`, `Scripts/EnemyControl.cs`, `Scripts/StateMachines/`, `Scripts/AttackCombo/`)

**State machine.** `StateMachine` / `State` are plain C# base classes (not MonoBehaviours), driven every
frame by the character control script. `State` has three hooks: `Enter` / `Tick` / `Exit`.

| Character | States |
|---|---|
| Player | Idle, Move, Attack, Dodge, TakeDamage |
| Enemy | Idle, Move, Attack, TakeDamage |

**Player.** `PlayerControl` reads input, moves relative to the camera direction with a
`CharacterController`, and owns the `PlayerStateMachine` and the `ComboManager`. Health lives in
`PlayerHealth`, which drives the health bar (`PlayerHealthBar`) through the `OnHealthChanged` event.

**Combo.** Data-driven, configured with two ScriptableObjects:

- `CharacterSkillSetSO`: the skill list of a character. Each skill is made of several `ComboSegment`s.
- `ActionTimelineSO`: the frame timeline of one attack. Each `ActionFrame` triggers one effect at a
  given logic frame.

`ComboManager` advances the timeline at 20Hz logic frames. The supported effects are
`Start`, `HitboxStart`, `HitboxEnd`, `ComboWindowStart`, `ComboWindowEnd`, `End`.
Pressing attack again inside the combo window moves to the next segment.
See [PlayerAttackState.md](PlayerAttackState.md) for details.

**Hit detection.** The attacker activates a trigger damage area at the configured frame. The receiver
identifies it by tag (`CharacterAttackBox` / `EnemyAttackBox`) in `OnTriggerEnter` and loses health.
The player takes no damage while dodging.

**Enemy.** `EnemyControl` chases the player with a `NavMeshAgent` and switches between idle, move and
attack by its distance to the player (`searchRange` / `attackRange`). On death it reports to the quest
system through `QuestEnemyContext`.

### 3. UI framework (`Scripts/UI/`)

`UIManager` is the panel manager. Panel prefabs are under `Resources/Prefabs/Panels/` and all derive
from `BasePanel`.

- Registration table: `RegisterPanels()` records each panel's name, prefab path and layer.
- Four layers: `Background` < `Normal` < `Popup` < `Top`. A higher layer always draws above a lower one.
- Lifecycle: `OnOpen(args)` / `OnClose()`; arguments can be passed when opening.
- Back stack: Esc calls `GoBack()`, and the top panel's `OnBack()` decides what happens. A panel whose
  `CanGoBack` is false cannot be dismissed.
- Events: `PanelOpened` / `PanelClosed`.

| Panel | Layer | Purpose |
|---|---|---|
| `MainPanel` | Normal | Main menu (while paused) |
| `InventoryPanel` | Normal | Inventory, with a delete mode |
| `WishPanel` | Normal | Gacha |
| `DialoguePanel` | Normal | NPC dialogue |
| `ItemDetailPanel` | Popup | Item detail and weapon enhancement |
| `VictoryPanel` / `DeadMenu` | Top | Victory / death screens |

`QuestTrackerPanel` (quest tracker) and `PlayerHealthBar` (health bar) are always-on HUD elements and
do not go through `UIManager`.

See [UIFramework.md](UIFramework.md) for details.

### 4. Inventory, gacha and weapons (`Scripts/UI/Inventory/`, `Scripts/Weapon/`)

- **Item table** `PackageTable` (ScriptableObject): each `PackageTableItem` has an id, type, star rating,
  name, description, icon path, model path and enhancement exp value. Types are weapon (0), food (1)
  and material (2).
- **Save data** `InventorySlotData`: the list of items the player owns (`InventorySlotItem`: uid, item id,
  amount, exp, is-new flag), serialized to JSON in PlayerPrefs. Weapon level is derived from exp (one
  level per 2000 exp).
- **Inventory UI**: `InventoryPanel` + `InventorySlotView` (slot) + `ItemDetailCard` (detail card).
- **Weapon enhancement**: in `ItemDetailPanel`, selected materials (`MaterialSlotView`) are consumed to
  add exp to a weapon.
- **Gacha**: `WishPanel` calls `GameManager.PullRandom()` / `PullRandom10()` and shows the results with
  `PullCell`.
- **Equipping** `WeaponController`: attaches the weapon model to the character's hand socket. Weapons
  that were already instantiated are cached and reused; switching weapons only toggles visibility.

### 5. Quest and dialogue (`Scripts/Quest/`, `Scripts/Dialogue/`)

```
NPCInteractable ──F──> DialogueManager ──accept──> QuestManager.AcceptQuest
                                                       │ OnQuestAccepted
                                                       ▼
                                                 EnemySpawner spawns enemies
                                                       │ enemy dies
                                                       ▼
                                QuestEnemyContext ──> QuestManager.ReportEnemyKilled
                                                       │ OnProgressChanged
                                                       ▼
                                                 QuestTrackerPanel refreshes
```

- `QuestDefinition` (ScriptableObject): quest id, name, description, target enemy id, required kills,
  reward exp.
- `QuestManager`: keeps the runtime state of each quest (`InProgress` → `ReadyToTurnIn` → `Completed`)
  and exposes three events: `OnQuestAccepted`, `OnProgressChanged`, `OnQuestCompleted`.
  Granting the reward (`GrantReward`) is not implemented yet.
- `EnemySpawner`: listens for the quest-accepted event and spawns enemies at the configured spawn
  points according to `QuestSpawnConfig`.
- `DialogueData` + `DialogueManager`: advance the dialogue line by line and show the accept / turn-in
  buttons on the last line. During a dialogue, attacking is disabled, the cursor is unlocked and the
  camera is stopped.
- `NPCInteractable`: shows the interaction prompt when the player enters the trigger range; F starts
  the dialogue.

### 6. Login and network (`Scripts/Network/GameServerClient.cs`, `Scripts/UI/LoginSpiralBackground.cs`)

- `GameServerClient`: sends the username and password (JSON) to the game server with
  `POST /api/auth/login`. On success it stores `userId` in PlayerPrefs and loads the game scene. The
  default address is `https://localhost:7031`. The server code is not in this repository.
- `LoginSpiralBackground`: the login screen background, with character models rotating and rising.

Only login uses the network for now. Inventory and quest data are still local.

### 7. Editor tools (`Assets/Editor/GMCmd.cs`)

Debug commands under the `GMCmd` menu: print the item table, create test inventory save data, print the
inventory, open the inventory panel, switch weapon, stop / start the camera.

## Directory layout

```
Assets/
├─ Scripts/            first-party code
│  ├─ AttackCombo/     combo data and ComboManager
│  ├─ StateMachines/   state machine base classes, player states, enemy states
│  ├─ UI/              UIManager, BasePanel, panels; Inventory/ holds the inventory code
│  ├─ Quest/           quests, enemy spawning
│  ├─ Dialogue/        dialogue, NPC interaction
│  ├─ Weapon/          weapon equipping
│  └─ Network/         login request
├─ Editor/             GM debug commands
├─ Resources/          loaded at runtime: panel and weapon prefabs, UI images, item table
├─ Scenes/             LoginScene, de_inferno, SampleScene
├─ Shaders/MMDToon/    URP toon shader for MMD characters
├─ Prefabs/            characters, hit boxes, hit effects
└─ other folders       third-party art and plugin assets (scenes, characters, animations, effects, skyboxes...)
Docs/                  project documentation
```

## Known limitations

- No assembly definitions (asmdef). First-party code and third-party scripts all compile into
  `Assembly-CSharp`.
- No automated tests.
- Assets are loaded synchronously with `Resources.Load`; Addressables is not used.
- Pause and cursor-lock logic is spread across `GameManager`, `DialogueManager` and the panels.
- Combo animation names are hard-coded in `ComboManager`, and only the first skill in the skill set is used.
- Hit effects are `Instantiate`d on every hit. An object pool exists on the
  `dev/claude/hit-effect-pool` branch and is not merged yet.
- Quest progress is not saved, and quest rewards are not granted.
