using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace tmkoc.tugofwar
{
    public enum MusicId
    {
        None,
        MatchStart,        // plays during 3-2-1-GO
        Gameplay,          // Tug Battle Gameplay Music
        FinalPush,         // when someone is close to winning
        Victory,
        Lose,              // Lose / Retry
        FinalCelebration   // all levels complete
    }

    public enum SfxId
    {
        Countdown, Go, Tap, RopeTension, PlayerEffort, PetEffort, FlagMove,
        StrongAlert, PetTired, MeterFill, MeterFull, PowerWhoosh, Combo,
        NearWinAlert, FinalPull, VictoryChime, Stars, Confetti, FunnyLose, ButtonClick
    }

    public enum VoiceId
    {
        LevelIntro,   // as the round starts
        Tutorial,     // first round only: how to play
        Go,           // right after GO
        Good,         // tapping cheers (tap speed tiers)
        Great,
        SuperPull,
        PowerPull,    // power meter full
        KeepTapping,  // kid is close to winning
        StrongPull,   // pet starts a strong pull
        PetTired,     // pet is tired - attack now
        Encourage,    // kid is losing ground (motivational)
        Win,          // round won   (one line per level)
        Lose,         // round lost  (one line per level)
        FinalWin      // every round won
    }

    /// <summary>How a voice line behaves when another line is already playing. Lines NEVER overlap.</summary>
    public enum VoicePriority
    {
        Low,      // chatty cheers: dropped if a line is playing
        Normal,   // waits its turn (a newer Normal replaces an older waiting one)
        High      // intro / win / lose: waits its turn and goes ahead of Normal
    }

    [Serializable]
    public class MusicEntry
    {
        public MusicId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.6f;
        public bool loop = true;
    }

    [Serializable]
    public class SfxEntry
    {
        public SfxId id;
        [Tooltip("Optional. Several clips = random variation. Empty = built-in synthesised placeholder is used.")]
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Random pitch range (min, max).")]
        public Vector2 pitchRange = Vector2.one;
    }

    [Serializable]
    public class LevelLine
    {
        [Tooltip("One or more RuntimeAudioLoader keys for this level (picked at random).")]
        public string[] keys;
        public LevelLine() { }
        public LevelLine(params string[] k) { keys = k; }
    }

    /// <summary>
    /// Voice-over keys for RuntimeAudioLoader (same idea as Animal Claw's AudioMapper).
    /// Each cue can hold several keys - one is picked at random, never the same twice in a row.
    /// Win / Lose have one entry PER LEVEL (index 0 = first level). No line ever says a level number.
    /// Edit the strings to match the clip names in your audio bundle. Full script: Docs/Tug Battle - Audio Script.md
    /// </summary>
    [Serializable]
    public class AudioMapper
    {
        public string[] levelIntro = { "tugintro1", "tugintro2", "tugintro3" };
        public string[] tutorial = { "tugtutorial" };
        public string[] go = { "tuggo1", "tuggo2" };
        public string[] good = { "tuggood1", "tuggood2", "tuggood3" };
        public string[] great = { "tuggreat1", "tuggreat2", "tuggreat3" };
        public string[] superPull = { "tugsuper1", "tugsuper2" };
        public string[] powerPull = { "tugpower1", "tugpower2" };
        public string[] keepTapping = { "tugkeep1", "tugkeep2" };
        public string[] strongPull = { "tugstrong1", "tugstrong2" };
        public string[] petTired = { "tugtired1", "tugtired2" };
        public string[] encourage = { "tugcheer1", "tugcheer2", "tugcheer3" };
        public LevelLine[] win =
        {
            new LevelLine("tugwin1"), new LevelLine("tugwin2"), new LevelLine("tugwin3"), new LevelLine("tugwin4"), new LevelLine("tugwin5")
        };
        public LevelLine[] lose =
        {
            new LevelLine("tuglose1"), new LevelLine("tuglose2"), new LevelLine("tuglose3"), new LevelLine("tuglose4"), new LevelLine("tuglose5")
        };
        public string[] finalWin = { "tugfinal" };

        public string[] Get(VoiceId id, int level)
        {
            switch (id)
            {
                case VoiceId.LevelIntro: return levelIntro;
                case VoiceId.Tutorial: return tutorial;
                case VoiceId.Go: return go;
                case VoiceId.Good: return good;
                case VoiceId.Great: return great;
                case VoiceId.SuperPull: return superPull;
                case VoiceId.PowerPull: return powerPull;
                case VoiceId.KeepTapping: return keepTapping;
                case VoiceId.StrongPull: return strongPull;
                case VoiceId.PetTired: return petTired;
                case VoiceId.Encourage: return encourage;
                case VoiceId.FinalWin: return finalWin;
                case VoiceId.Win: return ForLevel(win, level);
                case VoiceId.Lose: return ForLevel(lose, level);
            }
            return null;
        }

        private static string[] ForLevel(LevelLine[] lines, int level)
        {
            if (lines == null || lines.Length == 0) return null;
            return lines[Mathf.Clamp(level, 0, lines.Length - 1)].keys;
        }
    }


    /// <summary>
    /// Scene sound manager: music (crossfade), pooled SFX, and a single voice channel.
    /// Fill the three lists in the Inspector (right-click the component > "Populate All Entries" creates the rows).
    /// Missing SFX fall back to synthesised placeholders; missing music/voice are silently skipped.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        [Header("Library")]
        [SerializeField] private MusicEntry[] music;
        [SerializeField] private SfxEntry[] sfx;
        [Header("Voice-over keys (RuntimeAudioLoader) - edit to match your audio bundle")]
        [SerializeField] private AudioMapper voiceKeys = new AudioMapper();

        [Header("Volumes")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.9f;
        [SerializeField] private int sfxVoices = 10;
        [Tooltip("Music is lowered to this fraction while a voice line plays.")]
        [Range(0.2f, 1f)] [SerializeField] private float duckTo = 0.55f;

        private readonly Dictionary<MusicId, MusicEntry> musicMap = new Dictionary<MusicId, MusicEntry>();
        private readonly Dictionary<SfxId, SfxEntry> sfxMap = new Dictionary<SfxId, SfxEntry>();
        private readonly Dictionary<VoiceId, int> lastVoiceIndex = new Dictionary<VoiceId, int>();

        private AudioSource[] musicSources;   // two sources for crossfading
        private int activeMusic;
        private MusicId currentMusic = MusicId.None;
        private float currentMusicVolume;
        private AudioSource[] sfxSources;
        private int nextSfx;
        private bool muted;
        private Coroutine duckRoutine;

        private void Awake()
        {
            foreach (var m in music ?? Array.Empty<MusicEntry>()) if (m != null) musicMap[m.id] = m;
            foreach (var s in sfx ?? Array.Empty<SfxEntry>()) if (s != null) sfxMap[s.id] = s;

            musicSources = new[] { CreateSource("Music A", true), CreateSource("Music B", true) };
            sfxSources = new AudioSource[Mathf.Max(1, sfxVoices)];
            for (int i = 0; i < sfxSources.Length; i++) sfxSources[i] = CreateSource("Sfx " + i, false);
        }

        private void OnDestroy()
        {
            AudioListener.pause = false;
            DOTween.Kill(this);
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            return src;
        }

        #region Music
        public void PlayMusic(MusicId id, float fade = 0.4f)
        {
            if (id == currentMusic) return;
            currentMusic = id;

            var old = musicSources[activeMusic];
            DOTween.Kill(old);
            FadeTo(old, 0f, fade, stopAtEnd: true);

            if (id == MusicId.None || !musicMap.TryGetValue(id, out var entry) || entry.clip == null) return;

            activeMusic = 1 - activeMusic;
            var src = musicSources[activeMusic];
            DOTween.Kill(src);
            src.clip = entry.clip;
            src.loop = entry.loop;
            src.volume = 0f;
            src.Play();
            currentMusicVolume = entry.volume * masterVolume * (muted ? 0f : 1f);
            FadeTo(src, currentMusicVolume, fade, false);
        }

        public void StopMusic(float fade = 0.4f) => PlayMusic(MusicId.None, fade);

        private void FadeTo(AudioSource src, float volume, float time, bool stopAtEnd)
        {
            DOTween.To(() => src.volume, v => src.volume = v, volume, Mathf.Max(0.01f, time))
                .SetTarget(src).SetId(this)
                .OnComplete(() => { if (stopAtEnd) src.Stop(); });
        }
        #endregion

        #region SFX
        /// <param name="pitch">Multiplier on top of the entry's random pitch (e.g. tap tier).</param>
        public void PlaySfx(SfxId id, float pitch = 1f, float volume = 1f, float delay = 0f)
        {
            if (muted) return;
            if (delay > 0f) { StartCoroutine(PlaySfxDelayed(id, pitch, volume, delay)); return; }

            sfxMap.TryGetValue(id, out var entry);
            AudioClip clip = null;
            if (entry != null && entry.clips != null && entry.clips.Length > 0)
                clip = entry.clips[UnityEngine.Random.Range(0, entry.clips.Length)];
            if (clip == null) clip = ProceduralAudio.Get(id);

            float entryVol = entry != null ? entry.volume : 1f;
            float randomPitch = entry != null ? UnityEngine.Random.Range(entry.pitchRange.x, entry.pitchRange.y) : 1f;

            var src = sfxSources[nextSfx];
            nextSfx = (nextSfx + 1) % sfxSources.Length;
            src.pitch = randomPitch * pitch;
            src.volume = entryVol * volume * sfxVolume * masterVolume;
            src.clip = clip;
            src.Play();
        }

        private IEnumerator PlaySfxDelayed(SfxId id, float pitch, float volume, float delay)
        {
            yield return new WaitForSeconds(delay);
            PlaySfx(id, pitch, volume);
        }

        /// <summary>Tap sound that rises with tap speed.</summary>
        public void PlayTap(TapTier tier) => PlaySfx(SfxId.Tap, 1f + (int)tier * 0.18f);
        #endregion

        #region Voice (RuntimeAudioLoader keys) - lines never overlap
        public bool IsVoicePlaying => RuntimeAudioLoader.Instance != null && RuntimeAudioLoader.Instance._commonAudioSource.isPlaying;

        private struct VoiceRequest { public VoiceId id; public VoicePriority priority; public int level; }
        private VoiceRequest pending;
        private bool hasPending;
        private float nextVoiceTime;
        private const float VoiceGap = 0.25f;   // breathing room between two lines

        private bool VoiceBusy => IsVoicePlaying || Time.time < nextVoiceTime;

        /// <summary>
        /// Plays a voice line without ever overlapping another one.
        /// Low = dropped if something is playing; Normal / High = wait for their turn (High goes first).
        /// </summary>
        /// <param name="level">Level index, used by Win / Lose (one line per level).</param>
        /// <returns>True if the line started or was queued.</returns>
        public bool PlayVoice(VoiceId id, VoicePriority priority = VoicePriority.Normal, float delay = 0f, int level = -1)
        {
            if (muted || RuntimeAudioLoader.Instance == null) return false;
            if (delay > 0f) { StartCoroutine(PlayVoiceDelayed(id, priority, delay, level)); return true; }

            if (VoiceBusy)
            {
                if (priority == VoicePriority.Low) return false;
                if (!hasPending || priority >= pending.priority)
                {
                    pending = new VoiceRequest { id = id, priority = priority, level = level };
                    hasPending = true;
                }
                return true;
            }
            return StartVoice(id, level);
        }

        private bool StartVoice(VoiceId id, int level)
        {
            string key = PickKey(id, level);
            if (string.IsNullOrEmpty(key)) return false;

            float length = RuntimeAudioLoader.Instance.PlayRuntimeAudio(key);
            if (length < 0f) return false;   // clip not in the bundle: stay silent, keep going
            nextVoiceTime = Time.time + length + VoiceGap;
            if (duckRoutine != null) StopCoroutine(duckRoutine);
            duckRoutine = StartCoroutine(DuckMusic(length));
            return true;
        }

        private void Update()
        {
            if (!hasPending || VoiceBusy || RuntimeAudioLoader.Instance == null) return;
            var next = pending;
            hasPending = false;
            StartVoice(next.id, next.level);
        }

        private IEnumerator PlayVoiceDelayed(VoiceId id, VoicePriority priority, float delay, int level)
        {
            yield return new WaitForSeconds(delay);
            PlayVoice(id, priority, 0f, level);
        }

        /// <summary>Stops the current line and forgets any waiting one.</summary>
        public void StopVoice()
        {
            hasPending = false;
            nextVoiceTime = 0f;
            if (RuntimeAudioLoader.Instance != null) RuntimeAudioLoader.Instance.StopCommonAudioSource();
        }

        // Several keys for one cue = random variation, never the same one twice in a row.
        private string PickKey(VoiceId id, int level)
        {
            string[] keys = voiceKeys.Get(id, level);
            if (keys == null || keys.Length == 0) return null;
            int index = UnityEngine.Random.Range(0, keys.Length);
            if (keys.Length > 1 && lastVoiceIndex.TryGetValue(id, out int last) && last == index)
                index = (index + 1) % keys.Length;
            lastVoiceIndex[id] = index;
            return keys[index];
        }

        private IEnumerator DuckMusic(float seconds)
        {
            var src = musicSources[activeMusic];
            if (src.isPlaying) src.volume = currentMusicVolume * duckTo;
            yield return new WaitForSeconds(seconds);
            if (src != null && src.isPlaying) src.volume = currentMusicVolume;
        }
        #endregion

        #region Global
        public void SetMuted(bool value)
        {
            muted = value;
            if (value) { StopVoice(); foreach (var s in sfxSources) s.Stop(); }
            foreach (var m in musicSources) m.mute = value;
            if (RuntimeAudioLoader.Instance != null) RuntimeAudioLoader.Instance._commonAudioSource.mute = value;
        }

        /// <summary>Used by pause: freezes every AudioSource (AudioListener.pause).</summary>
        public void SetPaused(bool paused) => AudioListener.pause = paused;

        public void StopAllExceptMusic()
        {
            StopVoice();
            foreach (var s in sfxSources) s.Stop();
        }
        #endregion

#if UNITY_EDITOR
        private void Reset() => PopulateAll();

        [ContextMenu("Populate All Entries")]
        private void PopulateAll()
        {
            music = Fill(music, (MusicId[])Enum.GetValues(typeof(MusicId)), (e, id) => e.id == id, id => new MusicEntry { id = id }, id => id != MusicId.None);
            sfx = Fill(sfx, (SfxId[])Enum.GetValues(typeof(SfxId)), (e, id) => e.id == id, id => new SfxEntry { id = id }, id => true);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private static T[] Fill<T, TId>(T[] existing, TId[] ids, Func<T, TId, bool> matches, Func<TId, T> create, Func<TId, bool> include)
        {
            var list = new List<T>(existing ?? new T[0]);
            foreach (var id in ids)
            {
                if (!include(id)) continue;
                if (list.Exists(e => matches(e, id))) continue;
                list.Add(create(id));
            }
            return list.ToArray();
        }
#endif
    }
}
