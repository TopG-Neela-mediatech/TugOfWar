using System.Collections.Generic;
using UnityEngine;

namespace tmkoc.tugofwar
{
    /// <summary>
    /// Synthesised placeholder SFX (ported from the HTML prototype's Web Audio sounds).
    /// SoundManager uses these automatically whenever an SFX slot has no AudioClip assigned.
    /// </summary>
    public static class ProceduralAudio
    {
        private const int SampleRate = 44100;
        private enum Wave { Sine, Square, Triangle, Saw }
        private enum Filter { Low, High, Band }

        private static readonly Dictionary<SfxId, AudioClip> cache = new Dictionary<SfxId, AudioClip>();

        public static AudioClip Get(SfxId id)
        {
            if (cache.TryGetValue(id, out var clip) && clip != null) return clip;
            clip = Build(id);
            cache[id] = clip;
            return clip;
        }

        private static AudioClip Build(SfxId id)
        {
            switch (id)
            {
                case SfxId.Countdown: return Make(id, .25f, b => b.Tone(0, 520, .16f, Wave.Square, .12f));
                case SfxId.Go: return Make(id, .6f, b => { b.Tone(0, 660, .12f, Wave.Square, .14f); b.Tone(.1f, 990, .38f, Wave.Square, .14f); });
                case SfxId.Tap: return Make(id, .12f, b => b.Tone(0, 300, .07f, Wave.Triangle, .2f, 140));
                case SfxId.RopeTension: return Make(id, .55f, b => b.Tone(0, 85, .45f, Wave.Saw, .06f, 62));
                case SfxId.PlayerEffort: return Make(id, .2f, b => b.Noise(0, .12f, .14f, 500, Filter.Low));
                case SfxId.PetEffort: return Make(id, .25f, b => { b.Noise(0, .16f, .12f, 380, Filter.Low); b.Tone(0, 140, .16f, Wave.Triangle, .08f, 100); });
                case SfxId.FlagMove: return Make(id, .25f, b => b.Noise(0, .18f, .1f, 1500, Filter.Band));
                case SfxId.StrongAlert: return Make(id, .6f, b => { b.Tone(0, 240, .18f, Wave.Square, .12f); b.Tone(.2f, 190, .3f, Wave.Square, .12f); });
                case SfxId.PetTired: return Make(id, .6f, b => b.Tone(0, 620, .5f, Wave.Sine, .15f, 220));
                case SfxId.MeterFill: return Make(id, .15f, b => b.Tone(0, 700, .08f, Wave.Sine, .1f, 1000));
                case SfxId.MeterFull: return Make(id, .5f, b => { b.Tone(0, 880, .15f, Wave.Triangle, .14f); b.Tone(.12f, 1320, .3f, Wave.Triangle, .14f); });
                case SfxId.PowerWhoosh: return Make(id, .8f, b => { b.Tone(0, 180, .5f, Wave.Saw, .12f, 900); b.Noise(0, .4f, .18f, 2500, Filter.Band); b.Tone(.35f, 1200, .3f, Wave.Triangle, .1f); });
                case SfxId.Combo: return Make(id, .3f, b => { b.Tone(0, 880, .08f, Wave.Triangle, .14f); b.Tone(.07f, 1320, .12f, Wave.Triangle, .12f); });
                case SfxId.NearWinAlert: return Make(id, .15f, b => b.Tone(0, 1050, .07f, Wave.Square, .06f));
                case SfxId.FinalPull: return Make(id, .8f, b => { b.Tone(0, 150, .6f, Wave.Saw, .14f, 700); b.Noise(0, .5f, .14f, 1800, Filter.Band); });
                case SfxId.VictoryChime: return Make(id, .9f, b => { float[] f = { 523, 659, 784, 1047 }; for (int i = 0; i < 4; i++) b.Tone(i * .13f, f[i], .28f, Wave.Triangle, .18f); });
                case SfxId.Stars: return Make(id, 1.3f, b => { float[] f = { 1200, 1500, 1800 }; for (int i = 0; i < 3; i++) b.Tone(.1f + i * .22f, f[i], .25f, Wave.Sine, .14f); });
                case SfxId.Confetti: return Make(id, .7f, b => b.Noise(0, .6f, .12f, 4000, Filter.High));
                case SfxId.FunnyLose: return Make(id, .9f, b => { float[] f = { 440, 370, 311 }; for (int i = 0; i < 3; i++) b.Tone(i * .18f, f[i], .3f, Wave.Triangle, .14f); b.Tone(.55f, 900, .3f, Wave.Sine, .1f, 200); });
                case SfxId.ButtonClick: return Make(id, .1f, b => b.Tone(0, 600, .05f, Wave.Triangle, .12f));
                default: return Make(id, .1f, b => { });
            }
        }

        private static AudioClip Make(SfxId id, float seconds, System.Action<Buf> build)
        {
            var buf = new Buf(seconds);
            build(buf);
            return buf.ToClip("proc_" + id);
        }

        private class Buf
        {
            private readonly float[] data;
            public Buf(float seconds) { data = new float[Mathf.CeilToInt(seconds * SampleRate)]; }

            /// <summary>Oscillator with optional exponential pitch glide (f2) and the same envelope as the web version.</summary>
            public void Tone(float start, float f, float dur, Wave wave, float vol, float f2 = 0f)
            {
                int s0 = Mathf.RoundToInt(start * SampleRate);
                int n = Mathf.RoundToInt(dur * SampleRate);
                double phase = 0;
                for (int i = 0; i < n && s0 + i < data.Length; i++)
                {
                    float t = i / (float)SampleRate;
                    float freq = f2 > 0f ? f * Mathf.Pow(f2 / f, t / dur) : f;
                    phase += freq / SampleRate;
                    float frac = (float)(phase - System.Math.Floor(phase));
                    float osc;
                    switch (wave)
                    {
                        case Wave.Square: osc = frac < .5f ? 1f : -1f; break;
                        case Wave.Triangle: osc = 4f * Mathf.Abs(frac - .5f) - 1f; break;
                        case Wave.Saw: osc = 2f * frac - 1f; break;
                        default: osc = Mathf.Sin(frac * Mathf.PI * 2f); break;
                    }
                    data[s0 + i] += osc * Envelope(t, dur, vol);
                }
            }

            public void Noise(float start, float dur, float vol, float cutoff, Filter type)
            {
                int s0 = Mathf.RoundToInt(start * SampleRate);
                int n = Mathf.RoundToInt(dur * SampleRate);
                float a = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / SampleRate);
                float aHi = 1f - Mathf.Exp(-2f * Mathf.PI * (cutoff * .5f) / SampleRate);
                float aLo = 1f - Mathf.Exp(-2f * Mathf.PI * (cutoff * 1.5f) / SampleRate);
                float lp = 0, lp1 = 0, lp2 = 0;
                var rng = new System.Random(1234 + s0);
                for (int i = 0; i < n && s0 + i < data.Length; i++)
                {
                    float x = (float)(rng.NextDouble() * 2 - 1);
                    float y;
                    switch (type)
                    {
                        case Filter.High: lp += a * (x - lp); y = x - lp; break;
                        case Filter.Band: lp1 += aHi * (x - lp1); float hp = x - lp1; lp2 += aLo * (hp - lp2); y = lp2 * 2f; break;
                        default: lp += a * (x - lp); y = lp * 2f; break;
                    }
                    float t = i / (float)SampleRate;
                    float env = vol * Mathf.Pow(.0001f / vol, t / dur);
                    data[s0 + i] += y * env;
                }
            }

            private static float Envelope(float t, float dur, float vol)
            {
                const float attack = .01f;
                if (t < attack) return vol * (t / attack);
                float k = Mathf.Clamp01((t - attack) / Mathf.Max(.001f, dur - attack));
                return vol * Mathf.Pow(.0001f / vol, k);
            }

            public AudioClip ToClip(string name)
            {
                for (int i = 0; i < data.Length; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
                var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
                clip.SetData(data, 0);
                return clip;
            }
        }
    }
}
