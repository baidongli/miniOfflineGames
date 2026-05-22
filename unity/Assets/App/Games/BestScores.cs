using UnityEngine;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Per-game personal best, persisted via PlayerPrefs so it works whether or
    /// not the full Boot/save stack ran. Report() returns true when the new
    /// score beats the stored best (and stores it).
    /// </summary>
    public static class BestScores
    {
        public static int Get(string gameId) => PlayerPrefs.GetInt("best_" + gameId, 0);

        public static bool Report(string gameId, int score)
        {
            if (score <= Get(gameId)) return false;
            PlayerPrefs.SetInt("best_" + gameId, score);
            return true;
        }

        /// <summary>"   New Best!" if a record, else "   Best N" - for overlay text.</summary>
        public static string Suffix(string gameId, bool isRecord)
            => isRecord ? "   New Best!" : $"   Best {Get(gameId)}";
    }
}
