# Player attack state

中文版：[PlayerAttackState.zh.md](PlayerAttackState.zh.md)

Code:

- `Assets/Scripts/StateMachines/PlayerStates/PlayerAttackState.cs` — the state
- `Assets/Scripts/AttackCombo/ComboManager.cs` — combo playback
- `Assets/Scripts/AttackCombo/ActionTimelineSO.cs`, `CharacterSkillSetSO.cs` — data types
- `Assets/Scripts/AttackCombo/NormalAttack1..4.asset`, `PlayerNormalAttack.asset` — data
- `Assets/Scripts/PlayerControl.cs` — input, hitbox switch, interrupts
- `Assets/Scripts/StateMachines/PlayerStates/PlayerStateMachine.cs` — cleanup when leaving the attack state

This document describes the code on `main`. Behaviour is read from the code and the data assets; it was
not measured in play mode.

## Overview

```
          attack pressed                     End frame reached
Idle/Move ─────────────> PlayerAttackState ─────────────────> Move (move input held) / Idle
                           │        ▲
                           │        │ attack pressed inside the combo window
                           └────────┘ (next segment, same state)

                         dodge pressed ──> PlayerDodgeState        (any time)
                         hit by enemy  ──> PlayerTakeDamageState   (any time, unless dodging)
```

`PlayerAttackState` is thin. It owns no timing itself and delegates to `ComboManager`:

| Hook | What it does |
|---|---|
| `Enter` | `ComboManager.StartCombo()` — start at segment 0 |
| `Tick` | `UpdateCombo()`; if attack was pressed this frame, `NextSegment()`; if the combo is no longer playing, switch to Move or Idle |
| `Exit` | `ComboManager.EndCombo()` |

The whole combo is one stay in the attack state. Moving to the next segment does not leave and
re-enter the state.

## Combo data

A combo is a `SkillData` inside a `CharacterSkillSetSO`. It is a list of `ComboSegment`s, and each
segment points to an `ActionTimelineSO`. A timeline is a list of `ActionFrame { frame, effect }`.

| Effect | Result |
|---|---|
| `Start` | Log only |
| `HitboxStart` | `PlayerControl.EnableDamageArea()` |
| `HitboxEnd` | `PlayerControl.DisableDamageArea()` |
| `ComboWindowStart` | `comboWindow = true` |
| `ComboWindowEnd` | `comboWindow = false` |
| `End` | `EndCombo()` — `isPlaying = false`, the state then exits |

The player's normal attack (`PlayerNormalAttack.asset`) has four segments. Frames are logic frames at
20Hz, so one frame is 0.05s.

| Segment | Timeline | Animation | Hitbox on | Combo window | End |
|---|---|---|---|---|---|
| 1 | `NormalAttack1` | `combo_02_1` | 6–11 (0.30–0.55s) | 7–23 (0.35–1.15s) | 24 (1.20s) |
| 2 | `NormalAttack2` | `combo_02_2` | 12–23 (0.60–1.15s) | 13–33 (0.65–1.65s) | 34 (1.70s) |
| 3 | `NormalAttack3` | `combo_02_3` | 6–14 (0.30–0.70s) | 7–19 (0.35–0.95s) | 20 (1.00s) |
| 4 | `NormalAttack4` | `combo_02_4` | 8–21 (0.40–1.05s) | 9–27 (0.45–1.35s) | 28 (1.40s) |

The `End` frame of each timeline equals the length of its animation clip.

## Logic frames

`ComboManager.UpdateCombo()` runs once per rendered frame from `PlayerAttackState.Tick`:

1. Add `Time.deltaTime` to `logicTimer`.
2. While `logicTimer >= 1/20s`, subtract 1/20s and increment `currentLogicFrame`.
3. Execute every `ActionFrame` of the current timeline whose `frame == currentLogicFrame`.

Consequences:

- The timeline uses scaled time, so it stops while the game is paused (`Time.timeScale = 0`).
- The timeline and the animation are two independent clocks. The animation is started once per segment
  with `Animator.CrossFade` and nothing keeps the two in step afterwards.
- An effect is executed on every rendered frame for which `currentLogicFrame` stays on its frame number.
  At 60 fps that is about three times. All current effects are idempotent, so this is harmless.
- **Skipped frames lose their effects.** If one rendered frame takes longer than 0.05s, step 2 advances
  more than one logic frame and step 3 only checks the last one. An `End` that is skipped never fires,
  and the player then stays in the attack state until a dodge or a hit interrupts it.
- `logicTimer` is not reset in `StartCombo` or `NextSegment`, so a remainder of up to 0.05s carries
  over into the next segment.

## Input and the combo window

Attack input is read in `PlayerControl.DoAttack()`, which runs before the state machine in `Update`.
`AttackPressed` is true only on the frame the left mouse button goes down.

Each press, in any state, also:

- turns the player to face the nearest living enemy (`PlayerViewAngle`),
- enables the weapon trail effect (`WeaponEffect`). It is turned off when the attack state is left.

Inside the attack state, a press calls `ComboManager.NextSegment()`:

- **Outside the combo window the press is ignored.** There is no input buffer: a press before the window
  opens is lost and does not trigger the next segment later.
- **Inside the window the next segment starts immediately**, on the same frame. The damage area is
  disabled, the logic frame is reset to 0, the window is closed and the next animation is played.
- After segment 4 the index wraps to segment 1, so the chain can continue without end.

In every timeline the window opens one logic frame after the hitbox turns on and closes one logic frame
before `End`. So:

- The earliest follow-up is 0.05s after the hitbox becomes active. Chaining that early cuts the rest of
  the current segment's active hitbox time.
- If the player does not press inside the window, `End` fires one frame after it closes and the combo
  ends.

`Tick` calls `UpdateCombo()` before `NextSegment()`, so a press on the same rendered frame as
`ComboWindowStart` is accepted and a press on the same frame as `ComboWindowEnd` is rejected.

## Hit detection

`EnableDamageArea` / `DisableDamageArea` activate and deactivate the `DealDamageArea` GameObject, a
trigger collider tagged `CharacterAttackBox`. `EnemyControl.OnTriggerEnter` applies the damage when it
enters.

- Damage is `PlayerControl.playerDamage` for every segment. `ComboSegment.damage` exists in the data
  (set to 0) and is not read.
- The player cannot move during the attack state: `PlayerControl.Move` is only called by the move state.

## Interrupts

There is no priority or cancel table. Two things can interrupt the attack state at any logic frame, and
both are triggered outside the state, in `PlayerControl`:

| Interrupt | Trigger | Condition |
|---|---|---|
| Dodge | Jump action (Space) in `PlayerControl.Dodge()` | Not already dodging |
| Hit reaction | `OnTriggerEnter` with an `EnemyAttackBox` | Not dead and not dodging |

Both call `SwitchState`, which runs `PlayerAttackState.Exit` (`EndCombo`). `PlayerStateMachine.SwitchState`
then sees that an attack state was left for a non-attack state and calls
`PlayerControl.QuitAttackingState()`: the damage area is disabled, the weapon effect is turned off and
`ComboState` is reset. The same cleanup runs when the combo ends normally.

Other points:

- The dodge can cancel any part of an attack, including startup and the active hitbox frames. There is
  no cost or cooldown beyond the dodge's own 0.5s.
- The attack has no super armor. Any enemy hit cancels it.
- Move input does not cancel an attack. It only decides between Move and Idle after `End`.
- An attack cannot be started from the dodge or hit-reaction states. Both return to Move or Idle after
  0.5s first.
- Death does not switch the state. `PlayerControl.Dead()` plays the death animation and opens the game
  over screen 2s later, but the state machine keeps running in between, so the code does not stop an
  attack from starting in that time.
- During a dialogue `attackEnabled` is false and `DoAttack` returns early.

## Comparison with the old version (`PlayerAttackState1.cs`)

`PlayerAttackState1.cs` is the previous implementation, kept as a fully commented-out file. It did not
use `ComboManager`. Its animation clips are still in `Assets/Animations/temp/`.

| | Old (`PlayerAttackState1`) | Current (`PlayerAttackState` + `ComboManager`) |
|---|---|---|
| Where timing lives | A float timer in the state, plus animation events on the clips | Logic-frame timelines in ScriptableObject assets |
| Segment switch | `ComboState++` on `PlayerControl`, then `SwitchState(playerAttackState)` — the state exits and re-enters itself for each segment | `NextSegment()` inside one stay in the state |
| Combo window opens | 0.3s after entering, the same for all segments, hard-coded | Per segment, at the `ComboWindowStart` frame |
| Combo window closes | Never. It stays open until the animation finishes | At the `ComboWindowEnd` frame |
| Hitbox on / off | Animation events `DealDamage` / `DisableDamageArea`. The `DealDamage` handler is no longer in `PlayerControl`, so how it created the hitbox is not recoverable from the current code | `HitboxStart` / `HitboxEnd` frames in the timeline |
| Segment end | Animation event `AttackAnimFinished` → `OnAttackFinished()` | `End` frame in the timeline |
| After the last segment | `ComboState` becomes 4 and `PlayAttack` has no case for it, so no animation is played | Wraps to segment 1 |
| Combo index owner | `PlayerControl.ComboState` | `ComboManager.currentSegmentIndex` |
| Tuning | Edit the clip's events in the Animation window; the 0.3s needs a code change | Edit numbers in the timeline asset |
| Animation names | Hard-coded `switch` in the state | Hard-coded `switch` in `ComboManager` (unchanged) |
| Sync with the animation | Exact for hitbox and end, because the animation raises the events | Not enforced; two clocks |
| When frames drop | Unity still raises animation events that were passed over | Effects on skipped logic frames are lost |
| Hitbox when chaining early | The segment switch itself did not turn the hitbox off | `NextSegment()` disables it |

What the current version gained:

- Per-segment windows that open and close, instead of one fixed delay.
- All timing for a move in one data asset, which a new character or weapon can replace without code.
- Gameplay timing that no longer depends on the Animator. Animation events are not raised reliably when
  a clip is cross-faded out or interrupted, which the old version relied on for both the hitbox and the
  end of the attack.
- The combo index and window are private to `ComboManager`, not public fields on `PlayerControl`.

What it lost or has not solved yet:

- Hitbox timing is no longer tied to the animation by construction. The timeline must be kept in step
  with the clip by hand.
- Skipped logic frames drop their effects (see [Logic frames](#logic-frames)).

Leftovers from the old version that are still in the code:

- `PlayerControl.ComboState` is still declared and reset in `QuitAttackingState`, and nothing else reads it.
- `PlayerControl.AttackAnimFinished()` is an empty method.
- The current clips in `Assets/Animations/` only carry `PlayAttackSound` events (plus
  `StartTestTimer` / `StopTestTimer` on `combo_02_1`), so sound is the one thing still driven by
  animation events.

## Candidate next steps

- Execute every logic frame that was crossed in an update, not only the last one.
- Add an input buffer (the commented-out `nextAttackQueued`), so that a press shortly before the window
  opens is used when it opens.
- Move the animation name into `ComboSegment` and select the skill by id, removing the `switch` and the
  fixed `skills[0]`.
- Read `ComboSegment.damage` when applying a hit.
- Decide which states and frames may cancel into which (for example dodge-cancel only after the hitbox
  ends), as data on the timeline.
- Remove `PlayerAttackState1.cs`, `ComboState`, `AttackAnimFinished` and `Assets/Animations/temp/` once
  they are no longer needed for reference.
