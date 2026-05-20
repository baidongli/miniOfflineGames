using MiniGames.Games.FruitMerge.Logic;

namespace MiniGames.Games.FruitMerge.AI
{
    public sealed class CpuFruitMergeController
    {
        public readonly FruitMergeGame Game;
        public readonly IFruitMergeAI Ai;

        public CpuFruitMergeController(FruitMergeGame game, IFruitMergeAI ai)
        {
            Game = game;
            Ai = ai;
        }

        /// <summary>Drops the AI's choice. Returns true on success; false if game over.</summary>
        public bool TakeTurn()
        {
            if (Game.IsGameOver) return false;
            int column = Ai.ChooseColumn(Game);
            return Game.TryDrop(column);
        }
    }
}
