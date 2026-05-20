namespace MiniGames.Games.ColorBlocks.Logic
{
    /// <summary>
    /// Tunable scoring constants. Tweaked from game feel testing, not balance
    /// math — keep numbers round and noticeable.
    /// </summary>
    public static class ScoringRules
    {
        public const int PointsPerCellPlaced = 1;
        public const int PointsPerLineCleared = 10;

        /// <summary>
        /// Bonus on top of per-line points when 2+ lines clear in one move.
        /// </summary>
        public static int ComboBonus(int linesAtOnce) => linesAtOnce switch
        {
            <= 1 => 0,
            2 => 20,
            3 => 50,
            4 => 100,
            _ => 100 + (linesAtOnce - 4) * 60
        };

        public static int ScoreFor(PlaceResult result)
        {
            int s = result.CellsPlaced * PointsPerCellPlaced;
            int lines = result.TotalLinesCleared;
            s += lines * PointsPerLineCleared;
            s += ComboBonus(lines);
            return s;
        }
    }
}
