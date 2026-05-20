using System.Collections.Generic;
using MiniGames.GameModule;
using MiniGames.Networking.Protocol;

namespace MiniGames.App.Navigation
{
    /// <summary>
    /// Screen presentation interface, decoupled from any concrete UI
    /// framework. Production: a MonoBehaviour swapping prefabs / canvases.
    /// Tests: an in-memory fake.
    /// </summary>
    public interface IAppView
    {
        void ShowBoot();
        void ShowHub(IReadOnlyList<IGameModule> games);
        void ShowGameSelect(IGameModule game);
        void ShowLobby(IGameModule game, bool isHost);
        void ShowInGame(IGameModule game, PlayMode mode);
        void ShowResults(IGameModule game, GameResults results);
    }

    public sealed class GameResults
    {
        public List<PlayerResult> Players;
        public string LocalPlayerId;
    }
}
