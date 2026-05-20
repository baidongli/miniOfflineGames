using MiniGames.Games.Snakes.Logic;

namespace MiniGames.Games.Snakes.AI
{
    /// <summary>
    /// Drives one snake in a SnakesGameState via an ISnakesAI. Call BeforeTick()
    /// each tick BEFORE SnakesEngine.Step so the AI's chosen direction is
    /// applied as input for that tick.
    /// </summary>
    public sealed class CpuSnakesController
    {
        public readonly SnakesGameState State;
        public readonly int PlayerIndex;
        public readonly ISnakesAI Ai;

        public CpuSnakesController(SnakesGameState state, int playerIndex, ISnakesAI ai)
        {
            State = state;
            PlayerIndex = playerIndex;
            Ai = ai;
        }

        public void BeforeTick()
        {
            if (PlayerIndex < 0 || PlayerIndex >= State.Snakes.Count) return;
            if (!State.Snakes[PlayerIndex].IsAlive) return;
            var d = Ai.Choose(State, PlayerIndex);
            State.SetInput(PlayerIndex, d);
        }
    }
}
