namespace MiniGames.Games.Tetris.Logic
{
    /// <summary>
    /// Hardcoded cell offsets for each tetromino in each of its 4 rotations.
    ///
    /// Coordinate convention: each piece lives in a 4x4 box with origin at
    /// the bottom-left. Y grows up; gravity moves the piece down. The piece
    /// position (X, Y) on the board adds to each cell offset to get the
    /// absolute cell. Movement / rotation just swaps the rotation index and
    /// re-checks placement.
    ///
    /// Rotation 0 = spawn orientation. Each subsequent rotation is 90° CW.
    /// No wall-kicks beyond a basic "shift left/right if rotation is blocked"
    /// in the engine - this is offline casual Tetris, not Guideline-perfect.
    /// </summary>
    public static class TetrominoShapes
    {
        // Indexed by [(int)type][rotation 0..3] -> 4 cells.
        private static readonly (int x, int y)[][][] _byType = new (int, int)[][][]
        {
            // None
            null,
            // I (cyan)
            new[]
            {
                new (int, int)[] { (0,2),(1,2),(2,2),(3,2) }, // horizontal
                new (int, int)[] { (2,0),(2,1),(2,2),(2,3) }, // vertical right
                new (int, int)[] { (0,1),(1,1),(2,1),(3,1) }, // horizontal lower
                new (int, int)[] { (1,0),(1,1),(1,2),(1,3) }, // vertical left
            },
            // O (yellow) - identical for all rotations
            new[]
            {
                new (int, int)[] { (1,1),(2,1),(1,2),(2,2) },
                new (int, int)[] { (1,1),(2,1),(1,2),(2,2) },
                new (int, int)[] { (1,1),(2,1),(1,2),(2,2) },
                new (int, int)[] { (1,1),(2,1),(1,2),(2,2) },
            },
            // T (purple)
            new[]
            {
                new (int, int)[] { (0,2),(1,2),(2,2),(1,1) }, // ### / .#.
                new (int, int)[] { (1,2),(1,1),(2,1),(1,0) }, // .#. / ## / .#. (stem right)
                new (int, int)[] { (1,2),(0,1),(1,1),(2,1) }, // .#. / ###
                new (int, int)[] { (1,2),(0,1),(1,1),(1,0) }, // .#. / ## / .#. (stem left)
            },
            // S (green)
            new[]
            {
                new (int, int)[] { (1,2),(2,2),(0,1),(1,1) },
                new (int, int)[] { (0,2),(0,1),(1,1),(1,0) },
                new (int, int)[] { (1,1),(2,1),(0,0),(1,0) },
                new (int, int)[] { (1,2),(1,1),(2,1),(2,0) },
            },
            // Z (red)
            new[]
            {
                new (int, int)[] { (0,2),(1,2),(1,1),(2,1) },
                new (int, int)[] { (1,2),(0,1),(1,1),(0,0) },
                new (int, int)[] { (0,1),(1,1),(1,0),(2,0) },
                new (int, int)[] { (2,2),(1,1),(2,1),(1,0) },
            },
            // J (blue)
            new[]
            {
                new (int, int)[] { (0,2),(0,1),(1,1),(2,1) },
                new (int, int)[] { (1,2),(2,2),(1,1),(1,0) },
                new (int, int)[] { (0,1),(1,1),(2,1),(2,0) },
                new (int, int)[] { (1,2),(1,1),(0,0),(1,0) },
            },
            // L (orange)
            new[]
            {
                new (int, int)[] { (2,2),(0,1),(1,1),(2,1) },
                new (int, int)[] { (1,2),(1,1),(1,0),(2,0) },
                new (int, int)[] { (0,1),(1,1),(2,1),(0,0) },
                new (int, int)[] { (0,2),(1,2),(1,1),(1,0) },
            },
        };

        public static (int x, int y)[] Cells(TetrominoType type, int rotation)
        {
            var rotations = _byType[(int)type];
            return rotations[((rotation % 4) + 4) % 4];
        }
    }
}
