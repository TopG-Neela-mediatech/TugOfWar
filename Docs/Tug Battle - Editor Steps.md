# Tug Battle - Editor Steps (current)

The scene is already built under **MainCanvas** (run **Tools > Tug Battle > Build Scene** to rebuild it; your own `BG`
and `WinLose` objects are reused). Press Play - the game starts straight away.

## What you fill in
Open **Assets/ScriptableObjects/AssetsSO** and drop sprites in. Anything left empty keeps its placeholder shape.

| Group | Sprites |
|---|---|
| Scene | background, ground, player win line, pet win line |
| Rope | rope, centre flag |
| Characters | player, pet (ONE sprite each) |
| Tutorial | tutorial hand |

No animation frames are needed: leaning, pulling shake, tired droop, jump, slump, rope stretch, flag sway, taps and
the tutorial hand are all DOTween.

## Tutorial hand
`TutorialManager` (reached via `GameManager.TutorialManager`): shows on **level 1** when GO ends, and again whenever the
rope goes past 40% toward the pet. It taps in sets of 3 (pressed + ripple), pauses, repeats, and hides after the player
taps 3 times (or recovers). Tuning is on the component (taps per set, speed, losing threshold, cooldown).

## Text
All text is TextMeshPro with **Fredoka**, wrapping off, auto-sized to fit its box.

## WinLose panel
The buttons in `Assets/Prefabs/WinLose.prefab` are disabled; the panel shows for 5 s then goes to the next level (win)
or retries (lose).

## Audio
See `Tug Battle - Audio Script.md`. Voice lines play through `RuntimeAudioLoader` keys set on SoundManager > Voice Keys.
