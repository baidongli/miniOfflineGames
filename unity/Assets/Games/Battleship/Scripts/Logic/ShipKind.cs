namespace MiniGames.Games.Battleship.Logic
{
    public enum ShipKind : byte
    {
        Carrier    = 1,  // length 5
        Battleship = 2,  // length 4
        Cruiser    = 3,  // length 3
        Submarine  = 4,  // length 3
        Destroyer  = 5,  // length 2
    }

    public enum ShipOrientation : byte
    {
        Horizontal = 0,
        Vertical = 1,
    }

    public enum ShotResult : byte
    {
        Miss = 0,
        Hit = 1,
        Sunk = 2,
    }

    public enum BattleshipPhase : byte
    {
        Setup = 0,        // both players placing ships
        Playing = 1,      // shots being fired
        GameOver = 2,
    }

    public static class Fleet
    {
        public static int LengthOf(ShipKind kind) => kind switch
        {
            ShipKind.Carrier    => 5,
            ShipKind.Battleship => 4,
            ShipKind.Cruiser    => 3,
            ShipKind.Submarine  => 3,
            ShipKind.Destroyer  => 2,
            _ => 0
        };

        public static readonly ShipKind[] Standard = new[]
        {
            ShipKind.Carrier,
            ShipKind.Battleship,
            ShipKind.Cruiser,
            ShipKind.Submarine,
            ShipKind.Destroyer,
        };

        /// <summary>Total cells occupied by a full standard fleet (17).</summary>
        public const int TotalCells = 5 + 4 + 3 + 3 + 2;
    }
}
