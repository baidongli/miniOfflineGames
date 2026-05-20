using System;

namespace MiniGames.App.Shared.HighScores
{
    /// <summary>One row on a per-game leaderboard. JsonUtility-serializable.</summary>
    [Serializable]
    public sealed class HighScoreEntry
    {
        public string PlayerId;     // stable id from AppBootstrap
        public string DisplayName;  // human-readable
        public int Score;
        public long AchievedAtUtcTicks;
    }

    /// <summary>Persistent leaderboard payload, one per game id.</summary>
    [Serializable]
    public sealed class HighScoresPayload
    {
        public string GameId;
        // Sorted descending by Score.
        public System.Collections.Generic.List<HighScoreEntry> Entries
            = new System.Collections.Generic.List<HighScoreEntry>();
    }
}
