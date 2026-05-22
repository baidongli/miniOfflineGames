namespace MiniGames.App.Games
{
    /// <summary>
    /// Tiny static hand-off from the Hub to a game scene. Set before
    /// SceneManager.LoadScene; read by the scene controller in Start. Defaults
    /// to solo (false) so playing a game scene directly behaves as single-player.
    /// </summary>
    public static class GameLaunch
    {
        public static bool SameDevice;
    }
}
