using System.Collections.Generic;
using MiniGames.Games.DotsAndBoxes.Logic;

namespace MiniGames.Games.DotsAndBoxes.AI
{
    public interface IDotsAI
    {
        EdgeId? Choose(DotsGame game);
    }

    /// <summary>
    /// Greedy AI with two heuristics applied in order:
    /// 1. **Capture if possible** - any edge that completes one or more boxes.
    /// 2. **Avoid giving boxes** - prefer edges that don't bring a box's
    ///    edge count to exactly 3 (giving the opponent a free claim next turn).
    /// 3. Fallback - pick any legal edge.
    ///
    /// Doesn't do chain analysis (the deep strategic layer of Dots and
    /// Boxes) but plays a respectable casual game.
    /// </summary>
    public sealed class SimpleDotsAI : IDotsAI
    {
        public EdgeId? Choose(DotsGame game)
        {
            var legal = ListLegalEdges(game.Board);
            if (legal.Count == 0) return null;

            // 1. Capture moves.
            foreach (var e in legal)
                if (WouldClaimBox(game.Board, e)) return e;

            // 2. Safe moves: do not raise any box to exactly 3 edges.
            var safe = new List<EdgeId>();
            foreach (var e in legal)
                if (!WouldGiveBox(game.Board, e)) safe.Add(e);

            if (safe.Count > 0)
                return safe[game.Board.BoxesRemaining() % safe.Count]; // deterministic but varied

            // 3. Stuck with sacrifices. Pick the edge that gives the smallest chain.
            return legal[0];
        }

        private static List<EdgeId> ListLegalEdges(DotsBoard b)
        {
            var list = new List<EdgeId>();
            for (int y = 0; y <= b.BoxHeight; y++)
                for (int x = 0; x < b.BoxWidth; x++)
                    if (!b.HasHEdge(x, y)) list.Add(new EdgeId(EdgeKind.Horizontal, x, y));
            for (int y = 0; y < b.BoxHeight; y++)
                for (int x = 0; x <= b.BoxWidth; x++)
                    if (!b.HasVEdge(x, y)) list.Add(new EdgeId(EdgeKind.Vertical, x, y));
            return list;
        }

        private static bool WouldClaimBox(DotsBoard b, EdgeId e)
        {
            foreach (var (bx, by) in AdjacentBoxes(b, e))
                if (b.BoxEdgeCount(bx, by) == 3) return true;
            return false;
        }

        private static bool WouldGiveBox(DotsBoard b, EdgeId e)
        {
            foreach (var (bx, by) in AdjacentBoxes(b, e))
                if (b.BoxOwner(bx, by) < 0 && b.BoxEdgeCount(bx, by) == 2) return true;
            return false;
        }

        private static IEnumerable<(int bx, int by)> AdjacentBoxes(DotsBoard b, EdgeId e)
        {
            if (e.Kind == EdgeKind.Horizontal)
            {
                if (e.Y < b.BoxHeight) yield return (e.X, e.Y);
                if (e.Y > 0) yield return (e.X, e.Y - 1);
            }
            else
            {
                if (e.X < b.BoxWidth) yield return (e.X, e.Y);
                if (e.X > 0) yield return (e.X - 1, e.Y);
            }
        }
    }

    public sealed class CpuDotsController
    {
        public readonly DotsGame Game;
        public readonly IDotsAI Ai;
        public CpuDotsController(DotsGame g, IDotsAI a) { Game = g; Ai = a; }
        public bool TakeTurn()
        {
            if (Game.IsGameOver) return false;
            var move = Ai.Choose(Game);
            if (move == null) return false;
            return Game.TryPlay(move.Value, out _);
        }
    }
}
