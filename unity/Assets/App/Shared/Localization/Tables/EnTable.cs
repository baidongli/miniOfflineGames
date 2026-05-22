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
            { "ui.new_game", "New Game" },
            { "ui.randomize", "Randomize" },
            { "ui.rotate", "Rotate" },
            { "ui.drop", "Drop" },
            { "ui.bomb", "BOMB" },
            { "ui.up", "Up" },
            { "ui.down", "Down" },
            { "ui.left", "Left" },
            { "ui.right", "Right" },

            // In-game words / phrases.
            { "ig.score", "Score" },
            { "ig.best", "Best" },
            { "ig.lines", "Lines" },
            { "ig.next", "Next" },
            { "ig.territory", "Territory" },
            { "ig.you", "You" },
            { "ig.cpu", "CPU" },
            { "ig.red", "Red" },
            { "ig.yellow", "Yellow" },
            { "ig.black", "Black" },
            { "ig.white", "White" },
            { "ig.blue", "Blue" },
            { "ig.your_turn", "Your turn" },
            { "ig.computer_turn", "Computer's turn" },
            { "ig.thinking", "Thinking..." },
            { "ig.cpu_wins", "Computer wins" },
            { "ig.turn_of", "{0}'s turn" },
            { "ig.wins", "{0} wins!" },
            { "ig.reached_2048", "You reached 2048!" },
            { "ig.place_fleet", "Place your fleet: Randomize or Start" },
            { "ig.fire_turn", "Your turn - tap enemy waters" },
            { "ig.enemy_firing", "Enemy firing..." },
            { "ig.bomb_vs", "You (blue) vs CPU (red)" },
            { "ig.player_turn", "Player {0}'s turn" },
            { "ig.player_tap", "Player {0} - tap to start your turn" },
            { "ig.player_wins", "Player {0} wins!" },

            // How-to-play (one per game).
            { "howto.connect_four", "Tap a column to drop your disc. Get four in a row - across, down, or diagonally - before the computer." },
            { "howto.reversi", "Tap a highlighted square to place a disc and flip the opponent's discs trapped between yours. Most discs at the end wins." },
            { "howto.number_merge", "Swipe to slide all tiles. Two equal tiles merge into one that doubles. Reach the 2048 tile!" },
            { "howto.dots_and_boxes", "Tap a line between two dots. Completing the 4th side of a box claims it and gives you another turn. Most boxes wins." },
            { "howto.snakes", "Swipe or use arrow keys to steer. Eat food to grow longer. Don't hit the walls or your own tail." },
            { "howto.tetris", "Move and rotate the falling pieces. Fill a whole row to clear it. Don't let the stack reach the top." },
            { "howto.fruit_merge", "Tap a column to drop a fruit. Matching fruits that touch merge into a bigger one - chain them for big scores." },
            { "howto.bomb_sweep", "Move with the D-pad and drop bombs. Blow up the enemy while dodging blasts. Grab power-ups from soft blocks." },
            { "howto.maze_paint", "Leave your territory to lay a trail, then return home to capture the enclosed area. Don't cross your own trail." },
            { "howto.color_blocks", "Drag pieces onto the board. Fill a full row or column to clear it. The game ends when no piece fits." },
            { "howto.battleship", "Place your fleet (Randomize/Start), then tap the enemy waters to fire. Sink all enemy ships before yours go down." },

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
