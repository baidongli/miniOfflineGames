using System.Collections.Generic;

namespace MiniGames.App.Shared.Localization.Tables
{
    /// <summary>English (en) strings table. Keys are stable, dotted paths.</summary>
    public static class EnTable
    {
        public static Dictionary<string, string> Build() => new Dictionary<string, string>
        {
            // Game titles - keyed by IGameModule.Id.
            { "game.color_blocks.title", "Color Blocks" },
            { "game.tetris.title", "Tetris" },
            { "game.snakes.title", "Snakes" },
            { "game.maze_paint.title", "Maze Paint" },
            { "game.fruit_merge.title", "Fruit Merge" },
            { "game.connect_four.title", "Connect Four" },
            { "game.bomb_sweep.title", "Bomb Sweep" },
            { "game.reversi.title", "Reversi" },
            { "game.number_merge.title", "2048" },
            { "game.dots_and_boxes.title", "Dots and Boxes" },
            { "game.battleship.title", "Battleship" },

            // One-line taglines shown under each game card on the Hub.
            { "game.color_blocks.tagline",   "Drag pieces, clear lines" },
            { "game.tetris.tagline",         "Stack the falling blocks" },
            { "game.snakes.tagline",         "Grow without crashing" },
            { "game.maze_paint.tagline",     "Claim the most territory" },
            { "game.fruit_merge.tagline",    "Chain the merges" },
            { "game.connect_four.tagline",   "Four in a row" },
            { "game.bomb_sweep.tagline",     "Drop bombs, dodge blasts" },
            { "game.reversi.tagline",        "Flip the most discs" },
            { "game.number_merge.tagline",   "Slide and combine to 2048" },
            { "game.dots_and_boxes.tagline", "Complete boxes for bonus turns" },
            { "game.battleship.tagline",     "Sink the hidden fleet" },

            // Generic UI verbs / labels.
            { "ui.play", "Play" },
            { "ui.pause", "Pause" },
            { "ui.resume", "Resume" },
            { "ui.quit", "Quit" },
            { "ui.cancel", "Cancel" },
            { "ui.ok", "OK" },
            { "ui.back", "Back" },
            { "ui.next", "Next" },
            { "ui.start", "Start" },
            { "ui.ready", "Ready" },
            { "ui.settings", "Settings" },
            { "ui.hub", "Games" },
            { "ui.rematch", "Rematch" },
            { "ui.resign", "Resign" },
            { "ui.pass", "Pass" },
            { "ui.close", "Close" },
            { "ui.restart", "Restart" },
            { "ui.play_again", "Play Again" },
            { "ui.home", "Home" },
            { "ui.menu", "Menu" },
            { "ui.remove_ads", "Remove Ads" },
            { "ui.sound_on", "Sound: On" },
            { "ui.sound_off", "Sound: Off" },

            // Play modes.
            { "mode.solo", "Solo" },
            { "mode.same_device", "Same device" },
            { "mode.nearby_host", "Host nearby" },
            { "mode.nearby_join", "Join nearby" },

            // Lobby.
            { "lobby.searching", "Looking for nearby hosts..." },
            { "lobby.host_waiting", "Waiting for players..." },
            { "lobby.player_count", "{0} players, {1} ready" },
            { "lobby.start_game", "Start" },
            { "lobby.leave", "Leave" },

            // Result screens.
            { "result.you_win", "You win!" },
            { "result.you_lose", "You lose" },
            { "result.draw", "Draw" },
            { "result.game_over", "Game Over" },
            { "result.final_score", "Score: {0}" },
            { "result.best_score", "Best: {0}" },
            { "result.new_record", "New record!" },
            { "result.rank", "Rank #{0}" },

            // Settings labels.
            { "settings.audio", "Audio" },
            { "settings.bgm_volume", "Music" },
            { "settings.sfx_volume", "Sound" },
            { "settings.mute", "Mute" },
            { "settings.haptics", "Vibration" },
            { "settings.display_name", "Name" },
            { "settings.language", "Language" },
            { "settings.color", "Color" },
            { "settings.reduced_motion", "Reduce motion" },
            { "settings.reset", "Reset to defaults" },

            // Energy.
            { "energy.full", "Full" },
            { "energy.refills_in", "Refill in {0}" },

            // Errors.
            { "error.network_off", "Wi-Fi or Bluetooth is off." },
            { "error.permission_denied", "Permission denied. See system settings." },
        };
    }
}
