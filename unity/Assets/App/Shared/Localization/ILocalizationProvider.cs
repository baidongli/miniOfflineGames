namespace MiniGames.App.Shared.Localization
{
    /// <summary>
    /// Lookup interface UI code calls to localize a string key. Falls back
    /// to returning the key itself if no translation exists, so missing
    /// entries are visible during development.
    /// </summary>
    public interface ILocalizationProvider
    {
        string Language { get; }
        string Get(string key, params object[] args);
    }
}
