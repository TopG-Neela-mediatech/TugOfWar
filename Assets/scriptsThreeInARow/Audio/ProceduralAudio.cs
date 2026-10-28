using System.Collections.Generic;
using UnityEngine;

namespace tmkoc.threeinarow
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
                case SfxId.TapuPop: return Make(id, .25f, b => { b.Tone(0, 520, .12f, Wave.Triangle, .2f, 880); b.Tone(.08f, 1100, .12f, Wave.Sine, .1f); });
                case SfxId.PetPop: return Make(id, .3f, b => { b.Tone(0, 330, .1f, Wave.Triangle, .2f, 520); b.Tone(.1f, 620, .14f, Wave.Triangle, .16f, 420); });
                case SfxId.WinLine: return Make(id, .7f, b => { b.Tone(0, 400, .45f, Wave.Saw, .08f, 1200); b.Noise(0, .4f, .1f, 2500, Filter.Band); b.Tone(.3f, 1400, .3f, Wave.Triangle, .1f); });
                case SfxId.Victory: return Make(id, .9f, b => { float[] f = { 523, 659, 784, 1047 }; for (int i = 0; i < 4; i++) b.Tone(i * .13f, f[i], .28f, Wave.Triangle, .18f); });
                case SfxId.Lose: return Make(id, .9f, b => { float[] f = { 440, 370, 311 }; for (int i = 0; i < 3; i++) b.Tone(i * .18f, f[i], .3f, Wave.Triangle, .14f); b.Tone(.55f, 900, .3f, Wave.Sine, .1f, 200); });
                case SfxId.Draw: return Make(id, .7f, b => { float[] f = { 523, 523, 659 }; for (int i = 0; i < 3; i++) b.Tone(i * .16f, f[i], .22f, Wave.Triangle, .15f); });
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
