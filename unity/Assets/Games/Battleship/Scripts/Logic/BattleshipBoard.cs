using System.Collections.Generic;

namespace MiniGames.Games.Battleship.Logic
{
    /// <summary>
    /// A single 10x10 grid. Each cell stores which ship occupies it
    /// (0 = empty, otherwise a (byte)ShipKind value). A parallel `_hits`
    /// bitmap tracks whether each cell has been shot at.
    /// Used twice per player:
    ///   - OwnFleet: my real ship placements (private to me).
    ///   - OpponentTracker: my partial knowledge of opponent's grid,
    ///     built up from shot results.
    /// </summary>
    public sealed class BattleshipBoard
    {
        public const int Size = 10;

        private readonly byte[] _ships = new byte[Size * Size];
        private readonly bool[] _hits = new bool[Size * Size];
        // For OwnFleet: hits-per-ship counter so we can detect "sunk".
        private readonly Dictionary<ShipKind, int> _shipCells = new Dictionary<ShipKind, int>();
        private readonly Dictionary<ShipKind, int> _shipHits = new Dictionary<ShipKind, int>();

        public byte ShipAt(int x, int y) => _ships[y * Size + x];
        public bool ShotAt(int x, int y) => _hits[y * Size + x];
        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Size && y < Size;

        /// <summary>Try to place a ship. Returns false if it would go out of bounds or overlap.</summary>
        public bool TryPlaceShip(ShipKind kind, int x, int y, ShipOrientation o)
        {
            int len = Fleet.LengthOf(kind);
            // Already placed?
            if (_shipCells.ContainsKey(kind)) return false;
            // In bounds?
            int x2 = x + (o == ShipOrientation.Horizontal ? len - 1 : 0);
            int y2 = y + (o == ShipOrientation.Vertical   ? len - 1 : 0);
            if (!InBounds(x, y) || !InBounds(x2, y2)) return false;
            // Overlap?
            for (int i = 0; i < len; i++)
            {
                int cx = x + (o == ShipOrientation.Horizontal ? i : 0);
                int cy = y + (o == ShipOrientation.Vertical   ? i : 0);
                if (_ships[cy * Size + cx] != 0) return false;
            }
            // Place.
            for (int i = 0; i < len; i++)
            {
                int cx = x + (o == ShipOrientation.Horizontal ? i : 0);
                int cy = y + (o == ShipOrientation.Vertical   ? i : 0);
                _ships[cy * Size + cx] = (byte)kind;
            }
            _shipCells[kind] = len;
            _shipHits[kind] = 0;
            return true;
        }

        /// <summary>Marks a cell as shot. Returns the resolution for the targeted player's OwnFleet.</summary>
        public ShotResult RegisterIncomingShot(int x, int y, out ShipKind sunkKind, out List<(int x, int y)> sunkCells)
        {
            sunkKind = 0;
            sunkCells = null;
            if (!InBounds(x, y) || _hits[y * Size + x]) return ShotResult.Miss;  // already shot here counts as miss
            _hits[y * Size + x] = true;
            byte ship = _ships[y * Size + x];
            if (ship == 0) return ShotResult.Miss;

            ShipKind kind = (ShipKind)ship;
            _shipHits[kind]++;
            if (_shipHits[kind] >= _shipCells[kind])
            {
                sunkKind = kind;
                sunkCells = new List<(int, int)>();
                for (int yy = 0; yy < Size; yy++)
                    for (int xx = 0; xx < Size; xx++)
                        if (_ships[yy * Size + xx] == ship) sunkCells.Add((xx, yy));
                return ShotResult.Sunk;
            }
            return ShotResult.Hit;
        }

        /// <summary>For the shooter's OpponentTracker: just records what they learned at (x, y).</summary>
        public void RecordShotResult(int x, int y, ShotResult result)
        {
            if (!InBounds(x, y)) return;
            _hits[y * Size + x] = true;
            // We don't know the ship kind for Hit (only on Sunk via separate cells list).
            // Encode a Hit as a sentinel "1" so renderers can show "hit unknown" vs miss vs sunk.
            if (result == ShotResult.Hit) _ships[y * Size + x] = 255;   // sentinel for "hit unknown"
            // Miss / Sunk: the caller will fill in the sunk-ship cells separately.
        }

        /// <summary>Mark cells as belonging to a known-sunk ship on the OpponentTracker.</summary>
        public void RecordSunkShip(IList<(int x, int y)> cells, ShipKind kind)
        {
            foreach (var (cx, cy) in cells)
            {
                if (!InBounds(cx, cy)) continue;
                _ships[cy * Size + cx] = (byte)kind;
                _hits[cy * Size + cx] = true;
            }
        }

        public bool AllShipsSunk()
        {
            if (_shipCells.Count == 0) return false;  // no ships placed yet
            foreach (var kv in _shipHits)
                if (kv.Value < _shipCells[kv.Key]) return false;
            return true;
        }

        public bool HasShip(ShipKind kind) => _shipCells.ContainsKey(kind);

        public int PlacedShipCount => _shipCells.Count;
    }
}
