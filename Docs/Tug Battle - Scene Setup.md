# Tug Battle - Scene Setup Checklist

All scripts are in `Assets/Scripts/TugBattle/` (namespace `tmkoc.tugofwar`). `LevelManager` / `GameManager` were updated, not replaced.

## Hierarchy (suggested)

```
GameManager            -> GameManager (levelManager = LevelManager)
LevelManager           -> LevelManager
TugBattle              -> TugBattleController, TugBattleVfx
SoundManager           -> SoundManager   (right-click > Populate All Entries)
World / Canvas
  Background
  PlayerWinLine, PetWinLine           (FIXED - not inside RopeGroup)
  RopeGroup                           <- RopeView.ropeRoot (rope, flag, characters are children)
    Rope (SpriteRenderer or Image)
    Flag
    Player  (CharacterView)
    Pet     (CharacterView)
Canvas (UI)
  TapArea   full-screen transparent Image, Raycast Target ON, TapInput  (keep ABOVE nothing; sibling order below buttons)
  HUD       TugBattleUI: countdown TMP, banners, power meter, level text
  PauseButton, PausePanel (Resume / Home)
```

## Wiring

**LevelManager**: `Battle` = TugBattleController. `Level Configs` can stay empty (5 default levels from the prototype are created).
`Levels` (optional) = per-level scenery objects.

**TugBattleController**: Rope View, Player, Pet, Ui, Vfx.

**TapInput**: Controller. Needs an EventSystem in the scene. Space / Enter also tap in the Editor.

**RopeView**: `Rope Root` = RopeGroup, `Max Offset` = distance from centre to a win line (UI pixels or world units),
`Flag`, optional rope sprites per state (Neutral / PlayerPull / PetPull / Stretch / StrongPull / PowerPull).
Player starts on the LEFT and wins by pulling the flag to the left line (rope value -100).

**CharacterView** (Player and Pet): drag a SpriteRenderer or UI Image. Fill `Poses`: each entry = pose + frames + fps.
Or give it an Animator with int parameter `Pose` (value = enum index). Unused poses can stay empty.

| Pose | Player uses it when | Pet uses it when |
|---|---|---|
| Ready | countdown | countdown, strong-pull warning |
| RopeHold | not tapping for 0.6 s | - |
| NormalPull / StrongPull / VeryStrongPull | tap tier Normal / Fast / Power | Normal / Strong state |
| PowerPull | power pull | - |
| NearWin | flag within 38% of win | pet within 38% of winning |
| Tired | no tapping for 2.5 s (funny tired) | tired state |
| Recover | - | recover state |
| WinningGround / LosingGround | lose / - | pet ahead or player ahead by 40% |
| FinalPull, Celebrate, Lose | win pull, win, lose | - , lose, win |

**TugBattleUI**: `Banners` = one entry per kind (TapFast, KeepTapping, StrongPull, PetTired, PowerPull) - each a GameObject
(Image or TMP). `Pops` = prefabs for Good / Great / SuperPull (add a CanvasGroup for fade). `Pop Parent` = a RectTransform under the
Canvas, `Pop Anchor` = point above the player's head. `Power Fill` = Image with Type: Filled. Pause button/panel optional.

**TugBattleVfx**: every particle slot is optional.

**SoundManager**: see `Tug Battle - Audio Script.md`. SFX work immediately with placeholder tones.

## Saving (your helper)

- Start: `HelperGameCategoryDataSaver.Init(totalLevels)` + `GetStartLevel()` resumes at the first unfinished level.
- Win: `LevelCompleted(n)` where n = levels finished. Finishing level 5 saves 5/5 -> 5 stars, then the next level wraps to level 1.
- Lose: `AddAttempt()`.
- Win/lose panels use your existing `WinLosePanelScript` (retry restarts the same level with a faster countdown).

## Tuning

Per-level difficulty is `Level Configs` on LevelManager (pull, strong multiplier, gap between strong pulls, warn / strong / tired time).
Tap feel is on TugBattleController (tap strength, tap-speed thresholds, power meter).
