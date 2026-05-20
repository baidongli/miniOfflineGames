using MiniGames.Games.MazePaint.Logic;

namespace MiniGames.Games.MazePaint.AI
{
    public sealed class CpuMazePaintController
    {
        public readonly MazePaintGameState State;
        public readonly int PlayerIndex;
        public readonly IMazePaintAI Ai;

        public CpuMazePaintController(MazePaintGameState state, int playerIndex, IMazePaintAI ai)
        {
            State = state;
            PlayerIndex = playerIndex;
            Ai = ai;
        }

        public void BeforeTick()
        {
            if (PlayerIndex < 0 || PlayerIndex >= State.Players.Count) return;
            if (!State.Players[PlayerIndex].IsAlive) return;
            var d = Ai.Choose(State, PlayerIndex);
            State.SetInput(PlayerIndex, d);
        }
    }
}
