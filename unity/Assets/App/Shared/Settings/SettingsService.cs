using System;
using MiniGames.GameModule;

namespace MiniGames.App.Shared.Settings
{
    /// <summary>
    /// User-preferences service. Lazily loads on first access; persists
    /// after each Save() call. Fires Changed when settings mutate so UI
    /// or runtime systems (AudioBus, etc.) can react.
    /// </summary>
    public sealed class SettingsService
    {
        private const string Key = "settings";

        private readonly ISaveStore _store;
        private AppSettings _settings;

        public event Action<AppSettings> Changed;

        public SettingsService(ISaveStore store) { _store = store; }

        public AppSettings Current
        {
            get
            {
                if (_settings == null)
                {
                    if (!_store.TryLoad<AppSettings>(Key, out _settings) || _settings == null)
                        _settings = new AppSettings();
                }
                return _settings;
            }
        }

        /// <summary>Mutate via a delegate then persist. UI calls this with the form's new values.</summary>
        public void Update(Action<AppSettings> mutate)
        {
            var s = Current;
            mutate?.Invoke(s);
            _store.Save(Key, s);
            Changed?.Invoke(s);
        }

        public bool TutorialSeen(string gameId) => Current.TutorialsSeen.Contains(gameId);

        public void MarkTutorialSeen(string gameId)
        {
            if (Current.TutorialsSeen.Contains(gameId)) return;
            Update(s => s.TutorialsSeen.Add(gameId));
        }

        public void Reset()
        {
            _settings = new AppSettings();
            _store.Save(Key, _settings);
            Changed?.Invoke(_settings);
        }
    }
}
