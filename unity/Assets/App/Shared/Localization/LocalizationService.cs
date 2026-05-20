using System;
using System.Collections.Generic;
using MiniGames.App.Shared.Localization.Tables;
using MiniGames.App.Shared.Settings;

namespace MiniGames.App.Shared.Localization
{
    /// <summary>
    /// In-memory localization service. Holds the en/zh tables and a
    /// pointer to the current one. Language is read from SettingsService;
    /// changes via SetLanguage persist back to settings.
    ///
    /// Fallback rules:
    ///   1. Look up in active table.
    ///   2. If missing, look up in en (the canonical fallback).
    ///   3. If still missing, return the key verbatim (visible in UI so
    ///      missing translations are obvious during development).
    /// </summary>
    public sealed class LocalizationService : ILocalizationProvider
    {
        public const string DefaultLanguage = "en";

        private readonly SettingsService _settings;
        private readonly Dictionary<string, Dictionary<string, string>> _tables;

        public string Language { get; private set; }

        public event Action<string> LanguageChanged;

        public LocalizationService(SettingsService settings = null)
        {
            _settings = settings;
            _tables = new Dictionary<string, Dictionary<string, string>>
            {
                { "en", EnTable.Build() },
                { "zh", ZhTable.Build() },
            };

            string requested = settings?.Current?.PreferredLanguage ?? DefaultLanguage;
            Language = _tables.ContainsKey(requested) ? requested : DefaultLanguage;
        }

        public IReadOnlyCollection<string> SupportedLanguages => _tables.Keys;

        public string Get(string key, params object[] args)
        {
            if (string.IsNullOrEmpty(key)) return key;
            string template = LookupInternal(key);
            if (args == null || args.Length == 0) return template;
            try { return string.Format(template, args); }
            catch { return template; }
        }

        public bool SetLanguage(string lang)
        {
            if (!_tables.ContainsKey(lang)) return false;
            if (Language == lang) return true;
            Language = lang;
            _settings?.Update(s => s.PreferredLanguage = lang);
            LanguageChanged?.Invoke(lang);
            return true;
        }

        // --- internals ---

        private string LookupInternal(string key)
        {
            if (_tables.TryGetValue(Language, out var table) && table.TryGetValue(key, out var v))
                return v;
            if (Language != DefaultLanguage &&
                _tables.TryGetValue(DefaultLanguage, out var fallback) &&
                fallback.TryGetValue(key, out var fv))
                return fv;
            return key;
        }
    }

    /// <summary>Convenience shortcut: <c>L10n.T("ui.play")</c>. Requires LocalizationService.Active to be set at boot.</summary>
    public static class L10n
    {
        public static ILocalizationProvider Active { get; set; }
        public static string T(string key, params object[] args)
            => Active != null ? Active.Get(key, args) : key;
    }
}
