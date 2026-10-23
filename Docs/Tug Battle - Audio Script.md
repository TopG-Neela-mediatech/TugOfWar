# Tug Battle - Audio Script

Kid-friendly game for ages 3-6. Everything here plays through the **SoundManager** (reached with `GameManager.Instance.SoundManager`).

- **Voice-overs** play through **RuntimeAudioLoader** by key (same as Animal Claw). Keys are strings on `SoundManager > Voice Keys`; they are NOT Inspector clips.
- **Music + SFX** use Inspector clips on the SoundManager (right-click the component > *Populate All Entries* creates the rows). Missing SFX fall back to built-in placeholder tones; missing music is silent.
- **No line ever says a level number.** Win and lose lines are written per level, but only by tone / progress, never "level 3".

---

## 1. Voice-over rules (built into SoundManager)

| Rule | What happens |
|---|---|
| **Lines never overlap** | Only one voice line plays at a time. A new line never cuts the current one off. |
| **Low priority** (tap cheers: Good / Great / Super Pull, Keep Tapping) | Dropped if another line is playing, so cheering never piles up. Also at most one cheer every 2.5 s. |
| **Normal priority** (Go, Tutorial, Strong Pull, Pet Tired, Power Pull, Encourage) | Waits for the current line to finish, then plays. A newer Normal line replaces an older waiting one, so nothing gets stale. |
| **High priority** (round intro, Win, Lose, Final Win) | Waits for the current line, then plays ahead of anything Normal that is waiting. |
| **Gap + ducking** | 0.25 s of quiet between two lines. Music drops to 55 % while a line plays. |
| **End panel** | The win / lose panel stays up for at least 5 s and then waits until the voice line has finished before moving on. |
| **Missing clip** | If a key is not in the audio bundle the game stays silent and carries on. |

Record 2-3 takes of the repeated cues (intro, cheers) - one is picked at random and never the same twice in a row.

**Voice direction:** a warm, cheerful big-sister / big-brother narrator. Smiling voice, clear and slow for little ears. Short lines. Never scolding, never disappointed - even on a loss it is encouraging.

---

## 2. Voice-over script

Length targets: tapping cheers under 1.2 s so they never block tapping; everything else under 3.5 s.

### 2.1 Round start

| Key(s) | Line | When | Priority | Delivery |
|---|---|---|---|---|
| `tugintro1` | "Let's play tug of war! Get ready to tap!" | Round starts (during 3-2-1) | High | Bright, welcoming |
| `tugintro2` | "Who is stronger, you or the puppy? Let's find out!" | same | High | Playful, curious |
| `tugintro3` | "Ready, set... get your fingers ready!" | same | High | Building excitement |
| `tugtutorial` | "Tap the screen again and again to pull the rope!" | After GO, **first round only** | Normal | Clear, slow, demonstrating |
| `tuggo1` | "Go, go, go! Tap, tap, tap!" | After GO (every round except the first) | Normal | Energetic |
| `tuggo2` | "Tap fast!" | same (variation) | Normal | Quick, excited |

### 2.2 While the kid is tapping (cheerful)

| Key(s) | Line | When | Priority | Delivery |
|---|---|---|---|---|
| `tuggood1` | "Good!" | Normal tap speed | Low | Short, happy |
| `tuggood2` | "Nice!" | same | Low | Short, happy |
| `tuggood3` | "Yay!" | same | Low | Giggly |
| `tuggreat1` | "Great!" | Fast tapping | Low | Brighter |
| `tuggreat2` | "Wow, great pulling!" | same | Low | Impressed |
| `tuggreat3` | "Awesome!" | same | Low | Big smile |
| `tugsuper1` | "Super pull!" | Very fast tapping | Low | Big, excited |
| `tugsuper2` | "Wow, you are so strong!" | same | Low | Amazed |
| `tugpower1` | "Power pull!" | Power meter full | Normal | Biggest, shouted with joy |
| `tugpower2` | "Boom! Power pull!" | same | Normal | Playful punch |
| `tugkeep1` | "Keep tapping, you are almost there!" | Flag is close to winning | Low | Rising, encouraging |
| `tugkeep2` | "Almost there, don't stop!" | same | Low | Cheering |

### 2.3 The puppy (game events)

| Key(s) | Line | When | Priority | Delivery |
|---|---|---|---|---|
| `tugstrong1` | "Uh-oh! The puppy is pulling hard!" | Puppy starts a strong pull | Normal | Playful alarm, not scary |
| `tugstrong2` | "Hold on, strong pull!" | same | Normal | Playful alarm |
| `tugtired1` | "The puppy is tired! Tap, tap, tap now!" | Puppy gets tired (the kid's chance) | Normal | Big energy, giggly |
| `tugtired2` | "Now is your chance! Tap fast!" | same | Normal | Big energy |

### 2.4 Motivational (the kid is losing ground)

| Key(s) | Line | When | Priority | Delivery |
|---|---|---|---|---|
| `tugcheer1` | "Don't give up, you can do it!" | Flag moves toward the puppy's side | Normal | Warm, reassuring |
| `tugcheer2` | "Tap faster, you are so strong!" | same | Normal | Cheering them on |
| `tugcheer3` | "Come on, keep going, you've got this!" | same | Normal | Warm, confident |

### 2.5 Winning - one line per level (never says the level number)

The five rounds get warmer and prouder as the kid progresses. One line per level, played when the round is won.

| Key | Line | Tone |
|---|---|---|
| `tugwin1` | "Yay! You pulled it! You did it!" | Delighted, first success |
| `tugwin2` | "Hooray! You are a tug of war star!" | Cheering |
| `tugwin3` | "Wow! What a super strong pull!" | Impressed |
| `tugwin4` | "Amazing! The puppy couldn't beat you!" | Proud, playful |
| `tugwin5` | "Incredible! You are the strongest champion!" | Huge, proud |
| `tugfinal` | "Amazing! You won all the tug of war games! You are the champion!" | Biggest celebration - plays instead of the line above on the very last win |

### 2.6 Losing - one line per level, always motivational

Played when the puppy wins. Gentle, never disappointed, always ends with "try again".

| Key | Line | Tone |
|---|---|---|
| `tuglose1` | "Oops! That's okay. Let's try again, you can do it!" | Soft, reassuring |
| `tuglose2` | "So close! Tap a little faster this time!" | Encouraging |
| `tuglose3` | "Don't worry! Every try makes you stronger. Let's go again!" | Positive |
| `tuglose4` | "Almost! Keep trying, you are getting better and better!" | Supportive |
| `tuglose5` | "Nice try! Take a deep breath and tap again. You've got this!" | Calm, confident |

### 2.7 Copy-paste list of every clip name (36)

```
tugintro1  tugintro2  tugintro3  tugtutorial  tuggo1  tuggo2
tuggood1  tuggood2  tuggood3  tuggreat1  tuggreat2  tuggreat3
tugsuper1  tugsuper2  tugpower1  tugpower2  tugkeep1  tugkeep2
tugstrong1  tugstrong2  tugtired1  tugtired2
tugcheer1  tugcheer2  tugcheer3
tugwin1  tugwin2  tugwin3  tugwin4  tugwin5  tugfinal
tuglose1  tuglose2  tuglose3  tuglose4  tuglose5
```

---

## 3. Music (Inspector clips - MusicId)

| Id | File name | Plays when | Direction | Loop | Length |
|---|---|---|---|---|---|
| MatchStart | `mus_match_start` | 3-2-1-GO | Short upbeat build-up, anticipation | Yes | 8-12 s |
| Gameplay | `mus_tug_gameplay` | Match in progress | Playful marching beat, ~120 BPM, ukulele / claps / woodblock. Keep it light - taps are the main sound | Yes | 45-60 s |
| FinalPush | `mus_final_push` | Rope past 70% either way | Same key as Gameplay, faster (~140 BPM), more drums, tension | Yes | 20-30 s |
| Victory | `mus_victory` | Win | Cheerful fanfare + celebration | No | 5-8 s |
| Lose | `mus_lose` | Lose / retry | Gentle, funny "oops" - never sad or harsh. Encouraging | No | 4-6 s |
| FinalCelebration | `mus_final_celebration` | Every round won | Big party fanfare, longest and happiest | No | 8-12 s |

## 4. Sound effects (Inspector clips - SfxId)

| Id | File name | Trigger | Description | Variations |
|---|---|---|---|---|
| Countdown | `sfx_countdown` | 3, 2, 1 | Soft clear beep / pop | 1 |
| Go | `sfx_go` | GO! | Brighter two-note rising beep or whistle | 1 |
| Tap | `sfx_tap` | Every tap | Very short (<80 ms) soft pop / rope-creak. Pitch rises with tap speed automatically | 3 |
| RopeTension | `sfx_rope_tension` | Pet strong pull (every 0.5 s) | Low rope creak / stretch | 2 |
| PlayerEffort | `sfx_player_effort` | Fast taps | Kid "hup / hnngh" effort, cartoonish | 3 |
| PetEffort | `sfx_pet_effort` | Pet strong pull starts | Pet growl-grunt effort (dog "grrr-uff") | 2 |
| FlagMove | `sfx_flag_move` | Round end | Cloth flutter / whoosh | 1 |
| StrongAlert | `sfx_strong_alert` | Pet strong-pull warning | Rising warning stinger, playful not scary | 1 |
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
| ButtonClick | `sfx_button_click` | UI buttons (`GameManager.Instance.SoundManager.PlaySfx(SfxId.ButtonClick)`) | Soft pop | 1 |

## 5. Import settings (mobile)

- Music: Vorbis, Load Type *Streaming*, 22-44 kHz, Quality ~60%, Preload off.
- SFX: Vorbis or ADPCM, *Decompress On Load* for tap/short clips, Force To Mono ON.
- Voice: Vorbis, *Compressed In Memory*, Mono, 22 kHz is enough.
- Normalise voice to about -3 dB peak; music mastered around -14 LUFS so the voice sits on top.
