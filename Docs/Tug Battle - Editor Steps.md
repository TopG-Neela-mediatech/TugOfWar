# Tug Battle - Editor Steps (simple)

Open `TugOfWar` scene. Wait for compile. Console must have 0 errors.

## 1. Empty objects (Hierarchy > Create Empty)
1. **GameManager** - add `GameManager`.
2. **LevelManager** - add `LevelManager`. Drag in: Playschool Back Button, **Battle** (step 3). Leave Level Configs empty.
   Drag LevelManager into GameManager > Level Manager.
3. **TugBattle** - add `TugBattleController` and `TugBattleVfx`.
4. **SoundManager** - add `SoundManager`. Right-click its header > **Populate All Entries**. (Sounds work with placeholders; drop real clips in later.)
5. Make sure the scene has **PlayschoolCommon** (with the WinLose prefab) and an **EventSystem**.

## 2. World objects (your sprites)
```
RopeGroup (empty)
  Rope      (sprite)
  Flag      (sprite)
  Player    (sprite)  + CharacterView
  Pet       (sprite)  + CharacterView
PlayerWinLine (sprite, left,  NOT inside RopeGroup)
PetWinLine    (sprite, right, NOT inside RopeGroup)
```
- Add `RopeView` to **RopeGroup**: Rope Root = RopeGroup, Flag = Flag, Max Offset = distance from centre to a win line.
- Player/Pet `CharacterView`: drag its SpriteRenderer, fill **Poses** (pose + sprite frames). Empty poses are skipped. Start with just: Ready, NormalPull, StrongPull, Celebrate, Lose (Pet also: Tired).
- Pet `CharacterView` > **Slide X** = -60 (pet slides toward player on loss). Jump Height on both ~ 0.4 (world) or 40 (UI pixels).

## 3. Canvas (Screen Space - Overlay, scale 1920x1080)
```
TapArea     Image, alpha 0, Raycast Target ON, add TapInput (Controller = TugBattle)   <- FIRST child
Countdown   TMP text
Banners     5 objects: TapFast, KeepTapping, StrongPull, PetTired, PowerPull (Image or text)
PopParent   empty RectTransform;  PopAnchor = empty above player head
PowerMeter  background Image + child Fill Image (Image Type = Filled, Horizontal) + optional Glow object
LevelText   TMP text
PauseButton, PausePanel (Resume + Home buttons, disabled by default)
```
Add `TugBattleUI` on the Canvas (or a HUD object) and drag everything into its slots.
Banners: Banners list > one row per kind, drag the object. Pops: make 3 prefabs (Good/Great/Super Pull text or image) and drag in.
TapArea must be ABOVE the other UI in the list (top) so buttons stay clickable.

## 4. Final wiring
- `TugBattleController`: Rope View, Player, Pet, Ui, Vfx.
- `TugBattleVfx`: drag Player/Pet Win Lines (for pulse) + any particle systems you have (all optional).
- `LevelManager` > Battle = TugBattle.

## 5. Test
Press Play, click Space or tap the screen. You should see 3-2-1-GO, then taps pull the flag left.
Console should show no errors; no audio assets are needed to test.

## Tween animations already in
Countdown pop, banner pop + pulse, floating Good/Great/Super text, tap squash, flag shake, win jump, lose slump,
pet slide, camera shake + meter punch on Power Pull, win-line pulse near the win, pause panel pop-in.
