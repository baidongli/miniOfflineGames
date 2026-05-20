using MiniGames.Games.ColorBlocks.Logic;

namespace MiniGames.Games.ColorBlocks.AI
{
    /// <summary>
    /// Drives a ColorBlocksGame with an AI. Call TakeTurn() once per
    /// "AI tick" (e.g. every 1.5s of wall time in same-device mode so the
    /// CPU player visibly takes its turn).
    /// </summary>
    public sealed class CpuColorBlocksController
    {
        public readonly ColorBlocksGame Game;
        public readonly IColorBlocksAI Ai;

        public CpuColorBlocksController(ColorBlocksGame game, IColorBlocksAI ai)
        {
            Game = game;
            Ai = ai;
        }

        /// <summary>Picks and plays one move. Returns true on success; false if the AI has no legal move (game over).</summary>
        public bool TakeTurn()
        {
            if (Game.IsGameOver) return false;
            var move = Ai.ChooseMove(Game);
            if (move == null) return false;
            var (h, x, y) = move.Value;
            return Game.TryPlay(h, x, y, out _);
        }
    }
}
