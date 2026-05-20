using System;
using System.Collections.Generic;
using MiniGames.Games.Battleship.Logic;

namespace MiniGames.Games.Battleship.AI
{
    public interface IBattleshipAI
    {
        /// <summary>Place all 5 standard ships on the given board.</summary>
        void PlaceFleet(BattleshipBoard board, Random rng);

        /// <summary>Pick a (x, y) target on opponent's grid. Returns null if no untried cell remains.</summary>
        (int x, int y)? ChooseShot(BattleshipBoard opponentTracker);
    }

    /// <summary>
    /// Random placement + two-mode shooting:
    ///  - HUNT mode: probe untried cells on a checkerboard pattern (every
    ///    other cell, since the smallest ship covers 2 cells).
    ///  - TARGET mode: once a hit happens, queue its 4 neighbors and shoot
    ///    those before going back to hunting.
    /// </summary>
    public sealed class SimpleBattleshipAI : IBattleshipAI
    {
        private readonly Random _rng;
        private readonly Queue<(int x, int y)> _targetQueue = new Queue<(int, int)>();
        private (int x, int y)? _lastShot;

        public SimpleBattleshipAI(int seed = 0) { _rng = new Random(seed); }

        public void PlaceFleet(BattleshipBoard board, Random rng)
        {
            foreach (var kind in Fleet.Standard)
            {
                int len = Fleet.LengthOf(kind);
                while (true)
                {
                    var o = (rng.Next(2) == 0) ? ShipOrientation.Horizontal : ShipOrientation.Vertical;
                    int maxX = BattleshipBoard.Size - (o == ShipOrientation.Horizontal ? len : 1);
                    int maxY = BattleshipBoard.Size - (o == ShipOrientation.Vertical   ? len : 1);
                    int x = rng.Next(maxX + 1);
                    int y = rng.Next(maxY + 1);
                    if (board.TryPlaceShip(kind, x, y, o)) break;
                }
            }
        }

        public (int x, int y)? ChooseShot(BattleshipBoard opponentTracker)
        {
            // Drain target queue (positions adjacent to a recent hit).
            while (_targetQueue.Count > 0)
            {
                var c = _targetQueue.Dequeue();
                if (!opponentTracker.InBounds(c.x, c.y)) continue;
                if (opponentTracker.ShotAt(c.x, c.y)) continue;
                _lastShot = c;
                return c;
            }
            // Hunt mode: checkerboard scan for unshot cells.
            // Random offset for variety, but only over cells where (x+y) is even.
            var candidates = new List<(int, int)>();
            for (int y = 0; y < BattleshipBoard.Size; y++)
                for (int x = 0; x < BattleshipBoard.Size; x++)
                    if (((x + y) % 2 == 0) && !opponentTracker.ShotAt(x, y))
                        candidates.Add((x, y));
            // If no checkerboard cell left, fall back to any untried cell.
            if (candidates.Count == 0)
            {
                for (int y = 0; y < BattleshipBoard.Size; y++)
                    for (int x = 0; x < BattleshipBoard.Size; x++)
                        if (!opponentTracker.ShotAt(x, y)) candidates.Add((x, y));
            }
            if (candidates.Count == 0) { _lastShot = null; return null; }
            var pick = candidates[_rng.Next(candidates.Count)];
            _lastShot = pick;
            return pick;
        }

        /// <summary>Caller invokes this with the result of the last shot to drive target mode.</summary>
        public void RecordResult(ShotResult result)
        {
            if (_lastShot == null) return;
            if (result == ShotResult.Hit)
            {
                var (x, y) = _lastShot.Value;
                _targetQueue.Enqueue((x - 1, y));
                _targetQueue.Enqueue((x + 1, y));
                _targetQueue.Enqueue((x, y - 1));
                _targetQueue.Enqueue((x, y + 1));
            }
            else if (result == ShotResult.Sunk)
            {
                // Clear target queue: the ship we were chasing is dead.
                _targetQueue.Clear();
            }
        }
    }

    /// <summary>Drives a Battleship session against an AI opponent (for solo / vs-CPU mode).</summary>
    public sealed class CpuBattleshipController
    {
        public readonly BattleshipGame Game;
        public readonly SimpleBattleshipAI Ai;

        public CpuBattleshipController(BattleshipGame g, SimpleBattleshipAI a) { Game = g; Ai = a; }

        public void PlaceFleet(Random rng) { Ai.PlaceFleet(Game.OwnFleet, rng); Game.DeclareMyFleetReady(); }

        public bool TakeShot()
        {
            if (!Game.IsLocalTurn) return false;
            var pick = Ai.ChooseShot(Game.OpponentTracker);
            return pick.HasValue && Game.TryShoot(pick.Value.x, pick.Value.y);
        }
    }
}
