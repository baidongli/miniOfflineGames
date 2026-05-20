using UnityEngine;

namespace MiniGames.GameModule.Input
{
    /// <summary>
    /// Rectangular fraction of the screen owned by one player in same-device
    /// modes. Origin is bottom-left, values in 0..1.
    /// </summary>
    public readonly struct ScreenRegion
    {
        public readonly float MinX, MinY, MaxX, MaxY;
        public readonly float Rotation; // degrees the player's UI rotates by (so 4 corners face inward)

        public ScreenRegion(float minX, float minY, float maxX, float maxY, float rotation = 0f)
        {
            MinX = minX; MinY = minY; MaxX = maxX; MaxY = maxY; Rotation = rotation;
        }

        public bool ContainsNormalized(Vector2 p) =>
            p.x >= MinX && p.x <= MaxX && p.y >= MinY && p.y <= MaxY;

        public bool ContainsScreen(Vector2 screenPx, Vector2 screenSize) =>
            ContainsNormalized(new Vector2(screenPx.x / screenSize.x, screenPx.y / screenSize.y));
    }

    /// <summary>
    /// Standard layouts for 1-4 players on a single device.
    /// </summary>
    public static class ScreenLayouts
    {
        public static ScreenRegion[] For(int playerCount) => playerCount switch
        {
            1 => new[] { new ScreenRegion(0f, 0f, 1f, 1f, 0f) },
            2 => new[]
            {
                new ScreenRegion(0f, 0f,    1f, 0.5f, 0f),    // bottom (P1)
                new ScreenRegion(0f, 0.5f,  1f, 1f,   180f)   // top, rotated to face P2
            },
            3 => new[]
            {
                new ScreenRegion(0f,   0f, 0.5f, 0.5f, 0f),
                new ScreenRegion(0.5f, 0f, 1f,   0.5f, 0f),
                new ScreenRegion(0f,   0.5f, 1f, 1f,   180f)
            },
            4 => new[]
            {
                new ScreenRegion(0f,   0f,   0.5f, 0.5f, 0f),
                new ScreenRegion(0.5f, 0f,   1f,   0.5f, 0f),
                new ScreenRegion(0f,   0.5f, 0.5f, 1f,   180f),
                new ScreenRegion(0.5f, 0.5f, 1f,   1f,   180f)
            },
            _ => new[] { new ScreenRegion(0f, 0f, 1f, 1f, 0f) }
        };
    }
}
