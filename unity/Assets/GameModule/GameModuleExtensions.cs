using MiniGames.GameModule;

namespace MiniGames.GameModule
{
    /// <summary>
    /// Conventions every game's localization keys follow. UI code calls
    /// these to get the right localization key for a module without each
    /// module having to declare them itself.
    ///
    /// Examples:
    ///   game.color_blocks.title       Display title
    ///   game.color_blocks.tagline     One-line description in the Hub
    ///   game.color_blocks.howto       Rules / tutorial text
    /// </summary>
    public static class GameModuleExtensions
    {
        public static string TitleKey(this IGameModule m)        => $"game.{m.Id}.title";
        public static string TaglineKey(this IGameModule m)      => $"game.{m.Id}.tagline";
        public static string HowToKey(this IGameModule m)        => $"game.{m.Id}.howto";
        public static string AchievementKey(this IGameModule m, string achievement)
            => $"game.{m.Id}.achievement.{achievement}.title";
    }
}
