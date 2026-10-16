# Tug Battle - Audio Script

Assign every clip on the **SoundManager** component (right-click the component > *Populate All Entries* creates all rows).
Missing SFX use a built-in synthesised placeholder. Missing music / voice are silently skipped.

Target: mobile, kids 3-8. Voice = warm, energetic, friendly; short lines (under 1.5 s) so they never block tapping.

## 1. Music (MusicId)

| Id | File name | Plays when | Direction | Loop | Length |
|---|---|---|---|---|---|
| MatchStart | `mus_match_start` | 3-2-1-GO | Short upbeat build-up, anticipation | Yes | 8-12 s |
| Gameplay | `mus_tug_gameplay` | Match in progress | Playful marching beat, ~120 BPM, ukulele / claps / woodblock. Keep it light - taps are the main sound | Yes | 45-60 s |
| FinalPush | `mus_final_push` | Rope past 70% either way | Same key as Gameplay, faster (~140 BPM), more drums, tension | Yes | 20-30 s |
| Victory | `mus_victory` | Win (levels 1-4) | Cheerful fanfare + celebration | No | 5-8 s |
| Lose | `mus_lose` | Lose / retry | Gentle, funny "oops" - never sad or harsh. Encouraging | No | 4-6 s |
| FinalCelebration | `mus_final_celebration` | Level 5 won (all complete) | Big party fanfare, longest and happiest | No | 8-12 s |

## 2. Sound effects (SfxId)

| Id | File name | Trigger | Description | Variations |
|---|---|---|---|---|
| Countdown | `sfx_countdown` | 3, 2, 1 | Soft clear beep / pop | 1 |
| Go | `sfx_go` | GO! | Brighter two-note rising beep or whistle | 1 |
| Tap | `sfx_tap` | Every tap | Very short (<80 ms) soft pop / rope-creak. Pitch rises with tap speed automatically | 3 |
| RopeTension | `sfx_rope_tension` | Pet strong pull (every 0.5 s) | Low rope creak / stretch | 2 |
| PlayerEffort | `sfx_player_effort` | Fast taps | Kid "hup / hnngh" effort, cartoonish | 3 |
| PetEffort | `sfx_pet_effort` | Pet strong pull starts | Pet growl-grunt effort (dog "grrr-uff") | 2 |
| FlagMove | `sfx_flag_move` | Round end | Cloth flutter / whoosh | 1 |
| StrongAlert | `sfx_strong_alert` | "Strong Pull!" warning | Rising warning stinger, playful not scary | 1 |
| PetTired | `sfx_pet_tired` | Pet becomes tired | Funny deflating "pfffff" + panting | 1 |
| MeterFill | `sfx_meter_fill` | Power meter tick | Tiny rising sparkle (pitch rises with meter) | 1 |
| MeterFull | `sfx_meter_full` | Meter above 75% | Charged "ding-ding" shimmer | 1 |
| PowerWhoosh | `sfx_power_whoosh` | Power Pull | Big whoosh + burst | 1 |
| Combo | `sfx_combo` | Tap speed tier goes up | Quick two-note rising chime | 2 |
| NearWinAlert | `sfx_near_win_alert` | Rope past 70% (every 0.55 s) | Soft ticking pulse | 1 |
| FinalPull | `sfx_final_pull` | Winning pull | Big effort swoosh + flag snap | 1 |
| VictoryChime | `sfx_victory_chime` | Win | Bright ascending 4-note chime | 1 |
| Stars | `sfx_stars` | Win (+0.5 s) | 3 sparkle pings, rising | 1 |
| Confetti | `sfx_confetti` | Win (+0.2 s) | Pop + paper shimmer | 1 |
| FunnyLose | `sfx_funny_lose` | Lose | Slide-whistle down + boing | 2 |
| ButtonClick | `sfx_button_click` | UI buttons (call `SoundManager.Instance.PlaySfx(SfxId.ButtonClick)`) | Soft pop | 1 |

## 3. Voice-overs (VoiceId)

Voices are NOT Inspector clips. They play through `RuntimeAudioLoader` by key. On SoundManager > **Voice Keys** each cue has a string array (default keys shown below, e.g. `tapfast`). Edit the strings to match the clip names in your audio bundle; add several strings to a cue for random variation.

Record 2-3 takes of the chatty ones so it doesn't repeat. Assign extra takes to the same row (random pick, no immediate repeat).

| Id | File name | Line | When | Delivery |
|---|---|---|---|---|
| TapFast | `vo_tap_fast` | "Tap Fast!" | After GO; when pet is winning (rope past 62% pet side) | Urgent, excited |
| KeepTapping | `vo_keep_tapping` | "Keep Tapping!" | Player close to winning | Encouraging, rising |
| StrongPull | `vo_strong_pull` | "Strong Pull!" | Pet warning | Alert, playful |
| NowTapFast | `vo_now_tap_fast` | "Now! Tap Fast!" | Pet is tired - attack window | Big energy |
| Good | `vo_good` | "Good!" | Normal tap speed (max 1 per 2.5 s) | Short, cheerful |
| Great | `vo_great` | "Great!" | Fast tapping | Short, brighter |
| SuperPull | `vo_super_pull` | "Super Pull!" | Very fast tapping | Big |
| PowerPull | `vo_power_pull` | "Power Pull!" | Power meter full | Biggest, shouted |
| YouWin | `vo_you_win` | "You Win!" | Win, levels 1-4 | Celebratory |
| AlmostTapFaster | `vo_almost_tap_faster` | "Almost! Tap Faster!" | Lose | Kind, motivating, never disappointed |
| AmazingFinal | `vo_amazing_final` | "Amazing! You won all the Tug Battles!" | Win on the last level | Proud, huge |

Voice rules built into SoundManager: voice plays on RuntimeAudioLoader's common audio source, music ducks to 55% while it plays, and chatty cues
(Good / Great / Super / Keep Tapping / Tap Fast in danger zone) are skipped when another line is still playing.
Important cues (Strong Pull, Now! Tap Fast, Power Pull, win/lose) interrupt.

## 4. Import settings (mobile)

- Music: Vorbis, Load Type *Streaming*, 22-44 kHz, Quality ~60%, Preload off.
- SFX: Vorbis or ADPCM, *Decompress On Load* for tap/short clips, Force To Mono ON.
- Voice: Vorbis, *Compressed In Memory*, Mono, 22 kHz is enough.
- Normalise voice to about -3 dB peak; music mastered around -14 LUFS so voice sits on top.
