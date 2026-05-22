using System.Collections.Generic;
using UnityEngine;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Tiny procedural sound effects. Generates short tone clips at runtime
    /// (no audio asset files needed) and plays them through one persistent
    /// AudioSource. Call Sfx.Play("place") etc. Scenes built with a default
    /// Main Camera already have an AudioListener, so this is audible both in
    /// the full Boot flow and when playing a game scene directly.
    /// </summary>
    public static class Sfx
    {
        private const int Rate = 44100;
        private static AudioSource _src;
        private static Dictionary<string, AudioClip> _clips;

        public static void Play(string id, float volume = 0.6f)
        {
            Ensure();
            if (_clips.TryGetValue(id, out var clip)) _src.PlayOneShot(clip, volume);
        }

        private static void Ensure()
        {
            if (_src != null) return;
            var go = new GameObject("Sfx");
            Object.DontDestroyOnLoad(go);
            _src = go.AddComponent<AudioSource>();
            _src.playOnAwake = false;

            _clips = new Dictionary<string, AudioClip>
            {
                ["move"]  = Tone("move", 300f, 0.05f, square: false),
                ["place"] = Tone("place", 440f, 0.08f, square: false),
                ["clear"] = Sequence("clear", 0.09f, false, 600f, 900f),
                ["hit"]   = Tone("hit", 200f, 0.12f, square: true),
                ["miss"]  = Tone("miss", 150f, 0.07f, square: false),
                ["win"]   = Sequence("win", 0.09f, false, 523f, 659f, 784f, 1046f),
                ["lose"]  = Sequence("lose", 0.13f, false, 392f, 311f, 247f),
                ["end"]   = Tone("end", 440f, 0.16f, square: false),
            };
        }

        private static AudioClip Tone(string name, float freq, float dur, bool square)
        {
            int n = Mathf.Max(1, (int)(Rate * dur));
            var data = new float[n];
            FillTone(data, 0, n, freq, square);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Sequence(string name, float noteDur, bool square, params float[] freqs)
        {
            int per = Mathf.Max(1, (int)(Rate * noteDur));
            int n = per * freqs.Length;
            var data = new float[n];
            for (int i = 0; i < freqs.Length; i++)
                FillTone(data, i * per, per, freqs[i], square);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Writes a single decaying note into [offset, offset+count) of buffer.
        private static void FillTone(float[] buf, int offset, int count, float freq, bool square)
        {
            const int attack = 200; // short fade-in to avoid a click
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)Rate;
                float phase = 2f * Mathf.PI * freq * t;
                float wave = square ? Mathf.Sign(Mathf.Sin(phase)) : Mathf.Sin(phase);
                float env = Mathf.Exp(-t * 11f);
                if (i < attack) env *= i / (float)attack;
                buf[offset + i] = wave * env * 0.6f;
            }
        }
    }
}
