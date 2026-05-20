using System.Collections.Generic;

namespace MiniGames.Games.Snakes.Logic
{
    /// <summary>
    /// One snake. Body is a doubly-linked list with HEAD at First and TAIL at
    /// Last. Movement = AddFirst(newHead) + (RemoveLast unless growing).
    /// </summary>
    public sealed class SnakeState
    {
        public readonly int PlayerIndex;
        public readonly LinkedList<GridPos> Body = new LinkedList<GridPos>();
        public Direction Heading;
        public Direction PendingHeading;
        public bool IsAlive = true;
        public int PendingGrowth;
        public int FoodEaten;

        public SnakeState(int playerIndex, GridPos startHead, Direction heading, int initialLength)
        {
            PlayerIndex = playerIndex;
            Heading = heading;
            PendingHeading = heading;
            var p = startHead;
            for (int i = 0; i < initialLength; i++)
            {
                Body.AddLast(p);
                p = heading.Opposite().Step(p);
            }
        }

        public GridPos Head => Body.First.Value;
        public int Length => Body.Count;

        public bool ContainsAnywhere(GridPos pos)
        {
            foreach (var p in Body) if (p == pos) return true;
            return false;
        }

        public bool ContainsExcludingTail(GridPos pos)
        {
            // When the tail is about to move, the cell currently occupied by
            // the tail will be free this tick, so we exclude it from collision.
            var node = Body.First;
            var tail = Body.Last;
            while (node != null && node != tail)
            {
                if (node.Value == pos) return true;
                node = node.Next;
            }
            return false;
        }
    }
}
