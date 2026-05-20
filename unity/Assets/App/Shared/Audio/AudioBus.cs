using System.Collections.Generic;
using MiniGames.GameModule;
using UnityEngine;

namespace MiniGames.App.Shared.Audio
{
    /// <summary>
    /// Three-bus audio: BGM (one looping source with crossfade), SFX (round-
    /// robin pool of one-shots), UI (separate volume so UI sounds aren't
    /// ducked when SFX is muted in-game).
    ///
    /// MonoBehaviour because we need AudioSource components and Update for
    /// crossfade. Bootstrap adds a single instance to the root game object;
    /// games never instantiate AudioSources themselves.
    /// </summary>
    public sealed class AudioBus : MonoBehaviour, IAudio
    {
        [SerializeField] private int _sfxPoolSize = 6;
        [SerializeField] private float _bgmVolume = 0.7f;
        [SerializeField] private float _sfxVolume = 1f;

        private AudioSource _bgmA, _bgmB;
        private AudioSource _activeBgm;
        private AudioSource[] _sfxPool;
        private int _sfxNextIndex;
        private float _bgmFadeRemaining;
        private float _bgmFadeTotal;
        private readonly Dictionary<string, AudioClip> _clipCache = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            _bgmA = gameObject.AddComponent<AudioSource>();
            _bgmB = gameObject.AddComponent<AudioSource>();
            _bgmA.loop = _bgmB.loop = true;
            _bgmA.playOnAwake = _bgmB.playOnAwake = false;
            _activeBgm = _bgmA;

            _sfxPool = new AudioSource[_sfxPoolSize];
            for (int i = 0; i < _sfxPoolSize; i++)
            {
                _sfxPool[i] = gameObject.AddComponent<AudioSource>();
                _sfxPool[i].playOnAwake = false;
            }
        }

        public void PlaySfx(string id, float volume = 1f)
        {
            var clip = Resolve(id);
            if (clip == null) return;
            var src = _sfxPool[_sfxNextIndex];
            _sfxNextIndex = (_sfxNextIndex + 1) % _sfxPool.Length;
            src.PlayOneShot(clip, Mathf.Clamp01(volume * _sfxVolume));
        }

        public void PlayBgm(string id, float fadeSeconds = 0.5f)
        {
            var clip = Resolve(id);
            if (clip == null) return;
            var next = _activeBgm == _bgmA ? _bgmB : _bgmA;
            next.clip = clip;
            next.volume = 0f;
            next.Play();
            _activeBgm = next;
            _bgmFadeTotal = Mathf.Max(0.01f, fadeSeconds);
            _bgmFadeRemaining = _bgmFadeTotal;
        }

        public void StopBgm(float fadeSeconds = 0.5f)
        {
            _bgmFadeTotal = Mathf.Max(0.01f, fadeSeconds);
            _bgmFadeRemaining = _bgmFadeTotal;
            _activeBgm = null;
        }

        private void Update()
        {
            if (_bgmFadeRemaining <= 0f) return;
            _bgmFadeRemaining = Mathf.Max(0f, _bgmFadeRemaining - Time.unscaledDeltaTime);
            float t = 1f - _bgmFadeRemaining / _bgmFadeTotal;
            // Whichever source is "active" fades up; the other fades down and stops at zero.
            (_bgmA == _activeBgm ? _bgmA : _bgmB).volume = (_activeBgm == _bgmA ? t : 1f - t) * _bgmVolume;
            (_bgmB == _activeBgm ? _bgmB : _bgmA).volume = (_activeBgm == _bgmB ? t : 1f - t) * _bgmVolume;
            if (_bgmFadeRemaining <= 0f)
            {
                if (_activeBgm != _bgmA) _bgmA.Stop();
                if (_activeBgm != _bgmB) _bgmB.Stop();
                if (_activeBgm == null) { _bgmA.Stop(); _bgmB.Stop(); }
            }
        }

        private AudioClip Resolve(string id)
        {
            if (_clipCache.TryGetValue(id, out var cached)) return cached;
            // Resolve via Resources for v1; switch to Addressables for v2.
            var clip = Resources.Load<AudioClip>("Audio/" + id);
            if (clip != null) _clipCache[id] = clip;
            return clip;
        }
    }
}
