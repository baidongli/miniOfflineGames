using System;
using MiniGames.App.Shared.Localization;
using UnityEngine;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Runtime localization facade. Wraps the shared LocalizationService so UI
    /// can switch language at any time without the full Boot/Settings stack:
    /// language is persisted in PlayerPrefs and defaults to the device
    /// language on first run. Loc.T(key) looks up the active table.
    /// </summary>
    public static class Loc
    {
        private const string Key = "lang";
        private static LocalizationService _svc;

        public static event Action LanguageChanged;

        private static void Ensure()
        {
            if (_svc != null) return;
            _svc = L10n.Active as LocalizationService ?? new LocalizationService(null);
            L10n.Active = _svc;
            _svc.SetLanguage(PlayerPrefs.GetString(Key, DetectDefault()));
            _svc.LanguageChanged += _ => LanguageChanged?.Invoke();
        }

        public static string Language { get { Ensure(); return _svc.Language; } }

        public static string T(string key, params object[] args) { Ensure(); return _svc.Get(key, args); }

        public static void Set(string lang)
        {
            Ensure();
            if (_svc.SetLanguage(lang)) PlayerPrefs.SetString(Key, lang);
        }

        public static void Toggle() => Set(Language == "zh" ? "en" : "zh");

        private static string DetectDefault()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return "zh";
                default:
                    return "en";
            }
        }
    }
}
