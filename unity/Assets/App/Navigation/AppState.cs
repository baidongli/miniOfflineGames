namespace MiniGames.App.Navigation
{
    public enum AppState
    {
        Boot,
        Hub,
        GameSelect,
        Lobby,
        InGame,
        Results
    }

    public enum PlayMode
    {
        Solo,
        SameDevice,
        NearbyHost,
        NearbyJoin
    }
}
