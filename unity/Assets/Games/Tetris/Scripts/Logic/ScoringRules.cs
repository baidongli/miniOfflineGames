namespace MiniGames.Games.Tetris.Logic
{
    /// <summary>Classic Tetris scoring + attack-line table.</summary>
    public static class ScoringRules
    {
        // Single / Double / Triple / Tetris line-clear scores per level.
        public static int LineClearScore(int lines, int level) => lines switch
        {
            1 => 100 * level,
            2 => 300 * level,
            3 => 500 * level,
            4 => 800 * level,
            _ => 0
        };

        public const int SoftDropPointsPerCell = 1;
        public const int HardDropPointsPerCell = 2;

        /// <summary>Garbage lines sent to opponents on a clear.</summary>
        public static int AttackLines(int linesCleared) => linesCleared switch
        {
            1 => 0,
            2 => 1,
            3 => 2,
            4 => 4, // Tetris is a power move
            _ => 0
        };

        /// <summary>Drop interval (seconds per row) by level. Classic NES curve, smoothed.</summary>
        public static float GravitySeconds(int level)
        {
            // Level 1: ~0.8s/row; level 10: ~0.18s/row; caps near 0.05.
            float t = 0.8f - (level - 1) * 0.07f;
            return t < 0.05f ? 0.05f : t;
        }
    }
}
