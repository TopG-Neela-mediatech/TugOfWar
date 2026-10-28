using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tmkoc.threeinarow
{
    /// <summary>Only the effects the game really needs. Anything left empty falls back to a built-in placeholder tone.</summary>
    public enum SfxId
    {
        TapuPop, PetPop, WinLine, Victory, Lose, Draw
    }

    public enum VoiceId
    {
        LevelIntro,   // as the round starts
        Tutorial,     // first round only: how to play
        YourTurn,     // it is the player's turn
        Placed,       // cheer after the player places a face
        PetThinking,  // pet is thinking
        TwoInARow,    // player has two in a row - find the third
        StopPet,      // pet has two in a row - block it
        Hint,         // player waited too long
        Win,          // round won   (one line per level)
        Lose,         // round lost  (one line per level)
        Draw,         // tie
        FinalWin      // every round won
    }

    /// <summary>How a voice line behaves when another line is already playing. Lines NEVER overlap.</summary>
    public enum VoicePriority
    {
        Low,      // chatty cheers: dropped if a line is playing
        Normal,   // waits its turn (a newer Normal replaces an older waiting one)
        High      // intro / win / lose / draw: waits its turn and goes ahead of Normal
    }

    [Serializable]
    public class SfxEntry
    {
        public SfxId id;
        [Tooltip("Optional. Empty = built-in synthesised placeholder is used.")]
        public AudioClip clip;
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
    /// Edit the strings to match the clip names in your audio bundle.
    /// </summary>
    [Serializable]
    public class AudioMapper
    {
        public string[] levelIntro = { "ticintro1", "ticintro2", "ticintro3" };
        public string[] tutorial = { "tictutorial" };
        public string[] yourTurn = { "ticturn1", "ticturn2", "ticturn3" };
        public string[] placed = { "ticplaced1", "ticplaced2", "ticplaced3" };
        public string[] petThinking = { "ticthink1", "ticthink2" };
        public string[] twoInARow = { "tictwo1", "tictwo2" };
        public string[] stopPet = { "ticstop1", "ticstop2" };
        public string[] hint = { "tichint1", "tichint2" };
        public LevelLine[] win =
        {
            new LevelLine("ticwin1"), new LevelLine("ticwin2"), new LevelLine("ticwin3"), new LevelLine("ticwin4"), new LevelLine("ticwin5")
        };
        public LevelLine[] lose =
        {
            new LevelLine("ticlose1"), new LevelLine("ticlose2"), new LevelLine("ticlose3"), new LevelLine("ticlose4"), new LevelLine("ticlose5")
        };
        public string[] draw = { "ticdraw1", "ticdraw2", "ticdraw3" };
        public string[] finalWin = { "ticfinal" };

        public string[] Get(VoiceId id, int level)
        {
            switch (id)
            {
                case VoiceId.LevelIntro: return levelIntro;
                case VoiceId.Tutorial: return tutorial;
                case VoiceId.YourTurn: return yourTurn;
                case VoiceId.Placed: return placed;
                case VoiceId.PetThinking: return petThinking;
                case VoiceId.TwoInARow: return twoInARow;
                case VoiceId.StopPet: return stopPet;
                case VoiceId.Hint: return hint;
                case VoiceId.Draw: return draw;
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
    /// Scene sound manager: one looping BGM, a few pooled SFX and a single voice channel.
    /// Assign the BGM clip and the SFX clips in the Inspector (right-click > "Populate All Entries" creates the SFX rows).
    /// Missing SFX fall back to synthesised placeholders; voice lines come from RuntimeAudioLoader keys.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        [Header("Background music (looped)")]
        [SerializeField] private AudioClip bgm;
        [Header("SFX")]
        [SerializeField] private SfxEntry[] sfx;
        [Header("Voice-over keys (RuntimeAudioLoader) - edit to match your audio bundle")]
        [SerializeField] private AudioMapper voiceKeys = new AudioMapper();

        private const float BgmVolume = 0.6f;
        private const float DuckTo = 0.55f;     // BGM is lowered to this fraction while a voice line plays
        private const int SfxVoices = 6;

        private readonly Dictionary<SfxId, SfxEntry> sfxMap = new Dictionary<SfxId, SfxEntry>();
        private readonly Dictionary<VoiceId, int> lastVoiceIndex = new Dictionary<VoiceId, int>();

        private AudioSource musicSource;
        private AudioSource[] sfxSources;
        private int nextSfx;
        private bool muted;
        private Coroutine duckRoutine;

        private void Awake()
        {
            foreach (var e in sfx ?? Array.Empty<SfxEntry>()) if (e != null) sfxMap[e.id] = e;

            musicSource = CreateSource("Music", true);
            sfxSources = new AudioSource[SfxVoices];
            for (int i = 0; i < sfxSources.Length; i++) sfxSources[i] = CreateSource("Sfx " + i, false);
        }

        private void Start() => PlayBgm();

        private void OnDestroy()
        {
            AudioListener.pause = false;
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
        /// <summary>Starts the looping BGM (called once on Start; does nothing if no clip is assigned).</summary>
        public void PlayBgm()
        {
            if (bgm == null || musicSource.isPlaying) return;
            musicSource.clip = bgm;
            musicSource.loop = true;
            musicSource.volume = BgmVolume;
            musicSource.mute = muted;
            musicSource.Play();
        }
        #endregion

        #region SFX
        /// <param name="pitch">Multiplier (e.g. a little variation).</param>
        public void PlaySfx(SfxId id, float pitch = 1f, float volume = 1f, float delay = 0f)
        {
            if (muted) return;
            if (delay > 0f) { StartCoroutine(PlaySfxDelayed(id, pitch, volume, delay)); return; }

            sfxMap.TryGetValue(id, out var entry);
            AudioClip clip = entry != null ? entry.clip : null;
            if (clip == null) clip = ProceduralAudio.Get(id);

            var src = sfxSources[nextSfx];
            nextSfx = (nextSfx + 1) % sfxSources.Length;
            src.pitch = pitch;
            src.volume = volume;
            src.clip = clip;
            src.Play();
        }

        private IEnumerator PlaySfxDelayed(SfxId id, float pitch, float volume, float delay)
        {
            yield return new WaitForSeconds(delay);
            PlaySfx(id, pitch, volume);
        }
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
            if (musicSource.isPlaying) musicSource.volume = BgmVolume * DuckTo;
            yield return new WaitForSeconds(seconds);
            if (musicSource != null && musicSource.isPlaying) musicSource.volume = BgmVolume;
        }
        #endregion

        #region Global
        public void SetMuted(bool value)
        {
            muted = value;
            if (value) { StopVoice(); foreach (var s in sfxSources) s.Stop(); }
            musicSource.mute = value;
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
            var list = new List<SfxEntry>(sfx ?? new SfxEntry[0]);
            foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
                if (!list.Exists(e => e.id == id)) list.Add(new SfxEntry { id = id });
            sfx = list.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
