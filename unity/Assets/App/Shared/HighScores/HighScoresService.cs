using System;
using System.Collections.Generic;
using MiniGames.GameModule;

namespace MiniGames.App.Shared.HighScores
{
    /// <summary>
    /// Per-game local leaderboard. Keeps the top N scores per game id,
    /// persisted via ISaveStore so they survive restarts. No network -
    /// these are strictly local "your personal best" tables.
    /// </summary>
    public sealed class HighScoresService
    {
        public const int MaxEntriesPerGame = 10;

        private readonly ISaveStore _store;
        private readonly Func<DateTimeOffset> _now;
        private readonly Dictionary<string, HighScoresPayload> _cache
            = new Dictionary<string, HighScoresPayload>();

        public HighScoresService(ISaveStore store, Func<DateTimeOffset> now = null)
        {
            _store = store;
            _now = now ?? (() => DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// Submit a new score. Returns the rank (1-based) it took, or 0 if it
        /// didn't make the leaderboard (too low to displace existing entries).
        /// </summary>
        public int Submit(string gameId, string playerId, string displayName, int score)
        {
            if (string.IsNullOrEmpty(gameId)) return 0;
            var board = LoadOrCreate(gameId);

            var entry = new HighScoreEntry
            {
                PlayerId = playerId,
                DisplayName = displayName,
                Score = score,
                AchievedAtUtcTicks = _now().UtcTicks
            };

            board.Entries.Add(entry);
            board.Entries.Sort((a, b) => b.Score.CompareTo(a.Score));   // desc
            if (board.Entries.Count > MaxEntriesPerGame)
                board.Entries.RemoveRange(MaxEntriesPerGame, board.Entries.Count - MaxEntriesPerGame);

            int rank = board.Entries.IndexOf(entry) + 1;
            if (rank > 0) Persist(gameId, board);
            return rank > 0 ? rank : 0;
        }

        public IReadOnlyList<HighScoreEntry> Top(string gameId, int n = MaxEntriesPerGame)
        {
            var board = LoadOrCreate(gameId);
            int take = Math.Min(n, board.Entries.Count);
            return board.Entries.GetRange(0, take);
        }

        /// <summary>Best score for a specific player in a specific game, or null if none.</summary>
        public HighScoreEntry BestFor(string gameId, string playerId)
        {
            var board = LoadOrCreate(gameId);
            foreach (var e in board.Entries)
                if (e.PlayerId == playerId) return e;     // entries are sorted desc; first match is best
            return null;
        }

        public void Clear(string gameId)
        {
            _cache.Remove(gameId);
            _store.Delete(KeyFor(gameId));
        }

        // --- internals ---

        private HighScoresPayload LoadOrCreate(string gameId)
        {
            if (_cache.TryGetValue(gameId, out var cached)) return cached;
            if (_store.TryLoad<HighScoresPayload>(KeyFor(gameId), out var loaded) && loaded != null)
            {
                _cache[gameId] = loaded;
                return loaded;
            }
            var fresh = new HighScoresPayload { GameId = gameId };
            _cache[gameId] = fresh;
            return fresh;
        }

        private void Persist(string gameId, HighScoresPayload board)
        {
            _cache[gameId] = board;
            _store.Save(KeyFor(gameId), board);
        }

        private static string KeyFor(string gameId) => "scores_" + gameId;
    }
}
