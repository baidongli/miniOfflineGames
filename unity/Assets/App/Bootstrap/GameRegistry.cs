using System.Collections.Generic;
using MiniGames.GameModule;
using MiniGames.Games.ColorBlocks;
using MiniGames.Games.FruitMerge;
using MiniGames.Games.MazePaint;
using MiniGames.Games.Snakes;
using MiniGames.Games.BombSweep;
using MiniGames.Games.ConnectFour;
using MiniGames.Games.Battleship;
using MiniGames.Games.DotsAndBoxes;
using MiniGames.Games.NumberMerge;
using MiniGames.Games.Reversi;
using MiniGames.Games.Tetris;

namespace MiniGames.App.Bootstrap
{
    /// <summary>
    /// Central list of all games shipped with the app. To add a game:
    ///   1. Add a Games/&lt;Name&gt; folder with its own asmdef.
    ///   2. Implement IGameModule.
    ///   3. Add an entry here.
    /// </summary>
    public static class GameRegistry
    {
        public static IReadOnlyList<IGameModule> All { get; } = new IGameModule[]
        {
            new ColorBlocksModule(),
            new TetrisModule(),
            new SnakesModule(),
            new MazePaintModule(),
            new BombSweepModule(),
            new FruitMergeModule(),
            new ConnectFourModule(),
            new ReversiModule(),
            new NumberMergeModule(),
            new DotsAndBoxesModule(),
            new BattleshipModule()
        };

        public static IGameModule FindById(string id)
        {
            foreach (var g in All)
                if (g.Id == id) return g;
            return null;
        }
    }
}
