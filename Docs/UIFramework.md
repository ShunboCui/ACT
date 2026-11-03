# UI framework (Panel manager)

Code: `Assets/Scripts/UI/UIManager.cs`, `Assets/Scripts/UI/BasePanel.cs`

## Structure

```
UIManager (singleton)
  |- panelInfoDict   name -> UIPanelInfo { path, layer }   registration table
  |- openedPanelDict name -> BasePanel                     panels currently open
  |- prefabDict      name -> prefab                        Resources.Load cache
  |- panelStack      List<BasePanel>                       open panels in open order
  '- layerRootDict   UILayer -> RectTransform              one stretched root per layer

Canvas
  |- Layer_Background
  |- Layer_Normal      MainPanel / InventoryPanel / WishPanel / DialoguePanel
  |- Layer_Popup       ItemDetailPanel
  '- Layer_Top         DeadMenu / VictoryMenu
```

## Changes in this branch

| Before | After |
|---|---|
| Every panel is a child of Canvas; draw order = open order | Four layers (`UILayer`). A higher layer always draws above a lower one |
| Path table built in the private constructor | Registration table `RegisterPanels()` filled in `Awake`; `Register(name, path, layer)` is public |
| `OpenPanel` returned null when the panel was already open | Returns the open panel |
| Missing prefab -> null stored in the cache -> NullReferenceException | Logged with the path, nothing cached, returns null |
| No way to pass data into a panel | `OpenPanel(name, args)` -> `BasePanel.OnOpen(object args)` |
| No lifecycle hooks | `OnOpen(args)` / `OnClose()` virtual hooks |
| `ClosePanel()` called twice in one frame touched the dict twice | `isClosing` guard; dict entry removed only if it still belongs to that panel |
| No notifications | `UIManager.PanelOpened` / `PanelClosed` events |
| No back key; every panel wired its own close button | Panel stack + `UIManager.GoBack()` (Esc). `BasePanel.OnBack()` / `CanGoBack` let each panel define what "back" means |
| Only `OpenPanel` / `ClosePanel` / `GetPanel` | Added `OpenPanel<T>`, `GetPanel<T>`, `IsOpened`, `CloseAllPanels` |
| `openedPanelDict` was public and edited by `BasePanel` | Private; `BasePanel` calls `HandlePanelClosed` |

Existing call sites (`OpenPanel("MainPanel")`, `ClosePanel(...)`, `GetPanel(...)`) compile unchanged.

## Back key (panel stack)

`UIManager.TopPanel` = the panel the player sees: highest `UILayer` wins, and inside a layer the most
recently opened one wins. `UIManager.Update` sends Esc to `GoBack()`, which calls `TopPanel.GoBack()`.

- Default `OnBack()` is `ClosePanel()`. Panels with their own close flow override it and reuse their close
  button handler, so Esc and the button can never drift apart:
  `MainPanel` (resume game), `WishPanel` / `InventoryPanel` (return to main menu; inventory leaves delete
  mode first), `ItemDetailPanel` (refresh inventory), `DialoguePanel` (end the conversation).
- `CanGoBack => false` makes a panel un-dismissable and also blocks everything under it
  (`DeadMenu`, `VictoryPanel`).
- Example: Inventory (Normal) -> ItemDetail (Popup). Esc closes ItemDetail first, the next Esc goes back
  to the main menu, the next resumes the game.

## Adding a panel

1. Put the prefab under `Resources/Prefabs/Panels/`, with a script deriving from `BasePanel`.
2. Add a name constant to `UIConst`.
3. Add one line to `UIManager.RegisterPanels()`.
4. Open it: `UIManager.Instance.OpenPanel(UIConst.X, args)`; read `args` in `OnOpen`.

## Not done yet (candidate next steps)

- A pause/cursor-lock counter driven by `HasOpenPanel` / `PanelOpened` / `PanelClosed`, instead of each panel setting `Time.timeScale` itself, and gameplay input (attack, dodge) checking `HasOpenPanel`.
- Sub-states inside one panel (e.g. the enhance page of `ItemDetailPanel`) are not part of the stack yet.
- Cache and hide panels instead of `Destroy` / re-`Instantiate`. Needs the existing `Awake -> InitUI` button wiring moved to a once-only init, and `DialoguePanel`'s registration with `DialogueManager` moved to `OnOpen`.
- Replace `transform.Find("A/B")` string paths with serialized references.
- Replace `Resources.Load` with Addressables behind an async `OpenPanelAsync`.
- Move `Time.timeScale` / cursor handling out of `GameManager.Update` and the panels into one place driven by `PanelOpened` / `PanelClosed`.
