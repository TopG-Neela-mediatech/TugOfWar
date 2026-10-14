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
        TapFast,          // "Tap Fast!"
        KeepTapping,      // "Keep Tapping!"
        StrongPull,       // "Strong Pull!"
        NowTapFast,       // "Now! Tap Fast!"
        Good,             // "Good!"
        Great,            // "Great!"
        SuperPull,        // "Super Pull!"
        PowerPull,        // "Power Pull!"
        YouWin,           // "You Win!"
        AlmostTapFaster,  // "Almost! Tap Faster!"
        AmazingFinal      // "Amazing! You won all the Tug Battles!"
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
    public class VoiceEntry
    {
        public VoiceId id;
        [Tooltip("Several clips = random variation.")]
        public AudioClip[] clips;
        [Tooltip("Optional key for RuntimeAudioLoader.GetClip() when no clip is assigned.")]
        public string runtimeKey;
    }

    /// <summary>
    /// Scene sound manager: music (crossfade), pooled SFX, and a single voice channel.
    /// Fill the three lists in the Inspector (right-click the component > "Populate All Entries" creates the rows).
    /// Missing SFX fall back to synthesised placeholders; missing music/voice are silently skipped.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Library")]
        [SerializeField] private MusicEntry[] music;
        [SerializeField] private SfxEntry[] sfx;
        [SerializeField] private VoiceEntry[] voices;

        [Header("Volumes")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.9f;
        [Range(0f, 1f)] [SerializeField] private float voiceVolume = 1f;
        [SerializeField] private int sfxVoices = 10;
        [Tooltip("Music is lowered to this fraction while a voice line plays.")]
        [Range(0.2f, 1f)] [SerializeField] private float duckTo = 0.55f;

        private readonly Dictionary<MusicId, MusicEntry> musicMap = new Dictionary<MusicId, MusicEntry>();
        private readonly Dictionary<SfxId, SfxEntry> sfxMap = new Dictionary<SfxId, SfxEntry>();
        private readonly Dictionary<VoiceId, VoiceEntry> voiceMap = new Dictionary<VoiceId, VoiceEntry>();
        private readonly Dictionary<VoiceId, int> lastVoiceIndex = new Dictionary<VoiceId, int>();

        private AudioSource[] musicSources;   // two sources for crossfading
        private int activeMusic;
        private MusicId currentMusic = MusicId.None;
        private float currentMusicVolume;
        private AudioSource[] sfxSources;
        private int nextSfx;
        private AudioSource voiceSource;
        private bool muted;
        private Coroutine duckRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            foreach (var m in music ?? Array.Empty<MusicEntry>()) if (m != null) musicMap[m.id] = m;
            foreach (var s in sfx ?? Array.Empty<SfxEntry>()) if (s != null) sfxMap[s.id] = s;
            foreach (var v in voices ?? Array.Empty<VoiceEntry>()) if (v != null) voiceMap[v.id] = v;

            musicSources = new[] { CreateSource("Music A", true), CreateSource("Music B", true) };
            voiceSource = CreateSource("Voice", false);
            sfxSources = new AudioSource[Mathf.Max(1, sfxVoices)];
            for (int i = 0; i < sfxSources.Length; i++) sfxSources[i] = CreateSource("Sfx " + i, false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
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

        #region Voice
        public bool IsVoicePlaying => voiceSource != null && voiceSource.isPlaying;

        /// <param name="interrupt">False = skip if another line is still playing (for chatty cues like Good/Great).</param>
        public bool PlayVoice(VoiceId id, bool interrupt = true, float delay = 0f)
        {
            if (muted) return false;
            if (delay > 0f) { StartCoroutine(PlayVoiceDelayed(id, interrupt, delay)); return true; }
            if (!interrupt && IsVoicePlaying) return false;

            var clip = ResolveVoice(id);
            if (clip == null) return false;

            voiceSource.Stop();
            voiceSource.volume = voiceVolume * masterVolume;
            voiceSource.clip = clip;
            voiceSource.Play();
            if (duckRoutine != null) StopCoroutine(duckRoutine);
            duckRoutine = StartCoroutine(DuckMusic(clip.length));
            return true;
        }

        private IEnumerator PlayVoiceDelayed(VoiceId id, bool interrupt, float delay)
        {
            yield return new WaitForSeconds(delay);
            PlayVoice(id, interrupt);
        }

        public void StopVoice() => voiceSource.Stop();

        private AudioClip ResolveVoice(VoiceId id)
        {
            if (!voiceMap.TryGetValue(id, out var entry)) return null;

            if (entry.clips != null && entry.clips.Length > 0)
            {
                int index = UnityEngine.Random.Range(0, entry.clips.Length);
                if (entry.clips.Length > 1 && lastVoiceIndex.TryGetValue(id, out int last) && last == index)
                    index = (index + 1) % entry.clips.Length;
                lastVoiceIndex[id] = index;
                if (entry.clips[index] != null) return entry.clips[index];
            }

            if (!string.IsNullOrEmpty(entry.runtimeKey) && RuntimeAudioLoader.Instance != null)
                return RuntimeAudioLoader.Instance.GetClip(entry.runtimeKey);
            return null;
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
            if (value) { voiceSource.Stop(); foreach (var s in sfxSources) s.Stop(); }
            foreach (var m in musicSources) m.mute = value;
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
            voices = Fill(voices, (VoiceId[])Enum.GetValues(typeof(VoiceId)), (e, id) => e.id == id, id => new VoiceEntry { id = id }, id => true);
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
