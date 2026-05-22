using System.Collections.Generic;

namespace MiniGames.App.Shared.Localization.Tables
{
    /// <summary>Chinese Simplified (zh) strings table.</summary>
    public static class ZhTable
    {
        public static Dictionary<string, string> Build() => new Dictionary<string, string>
        {
            // 游戏名
            { "game.color_blocks.title", "彩色方块" },
            { "game.tetris.title", "俄罗斯方块" },
            { "game.snakes.title", "贪吃蛇" },
            { "game.maze_paint.title", "迷宫涂色" },
            { "game.fruit_merge.title", "水果合并" },
            { "game.connect_four.title", "四子棋" },
            { "game.bomb_sweep.title", "炸弹大乱斗" },
            { "game.reversi.title", "黑白棋" },
            { "game.number_merge.title", "2048" },
            { "game.dots_and_boxes.title", "点格棋" },
            { "game.battleship.title", "海战" },

            // 游戏卡片下方的一句话简介
            { "game.color_blocks.tagline",   "拖块消行" },
            { "game.tetris.tagline",         "经典消除" },
            { "game.snakes.tagline",         "活得更久" },
            { "game.maze_paint.tagline",     "圈地为王" },
            { "game.fruit_merge.tagline",    "合并连击" },
            { "game.connect_four.tagline",   "四子连珠" },
            { "game.bomb_sweep.tagline",     "炸弹乱斗" },
            { "game.reversi.tagline",        "翻转更多棋子" },
            { "game.number_merge.tagline",   "滑动合并到 2048" },
            { "game.dots_and_boxes.tagline", "完成方格再来一手" },
            { "game.battleship.tagline",     "击沉对方舰队" },

            // 通用按钮 / 标签
            { "ui.play", "开始" },
            { "ui.pause", "暂停" },
            { "ui.resume", "继续" },
            { "ui.quit", "退出" },
            { "ui.cancel", "取消" },
            { "ui.ok", "确定" },
            { "ui.back", "返回" },
            { "ui.next", "下一步" },
            { "ui.start", "开始" },
            { "ui.ready", "准备好了" },
            { "ui.settings", "设置" },
            { "ui.hub", "游戏" },
            { "ui.rematch", "再来一局" },
            { "ui.resign", "认输" },
            { "ui.pass", "跳过" },
            { "ui.close", "关闭" },
            { "ui.restart", "重玩" },
            { "ui.play_again", "再玩一次" },
            { "ui.home", "主页" },
            { "ui.menu", "菜单" },
            { "ui.remove_ads", "去广告" },
            { "ui.sound_on", "音效：开" },
            { "ui.sound_off", "音效：关" },
            { "ui.new_game", "新游戏" },
            { "ui.randomize", "随机布置" },
            { "ui.rotate", "旋转" },
            { "ui.drop", "硬降" },
            { "ui.bomb", "炸弹" },
            { "ui.up", "上" },
            { "ui.down", "下" },
            { "ui.left", "左" },
            { "ui.right", "右" },

            // 游戏内词 / 短语
            { "ig.score", "得分" },
            { "ig.best", "最佳" },
            { "ig.lines", "消行" },
            { "ig.next", "下一个" },
            { "ig.territory", "领地" },
            { "ig.you", "你" },
            { "ig.cpu", "电脑" },
            { "ig.red", "红方" },
            { "ig.yellow", "黄方" },
            { "ig.black", "黑方" },
            { "ig.white", "白方" },
            { "ig.blue", "蓝方" },
            { "ig.your_turn", "轮到你" },
            { "ig.computer_turn", "电脑回合" },
            { "ig.thinking", "思考中…" },
            { "ig.cpu_wins", "电脑获胜" },
            { "ig.turn_of", "{0}回合" },
            { "ig.wins", "{0}获胜！" },
            { "ig.reached_2048", "达成 2048！" },
            { "ig.place_fleet", "布置舰队：随机 或 开始" },
            { "ig.fire_turn", "你的回合 — 点击敌方海域" },
            { "ig.enemy_firing", "敌方开火中…" },
            { "ig.bomb_vs", "你（蓝）对 电脑（红）" },
            { "ig.player_turn", "玩家{0} 回合" },
            { "ig.player_tap", "玩家{0} — 点击开始你的回合" },
            { "ig.player_wins", "玩家{0} 获胜！" },

            // 玩法说明（每个游戏一条）
            { "howto.connect_four", "点击某一列落子。抢在电脑之前,在横、竖或斜方向连成四子即获胜。" },
            { "howto.reversi", "点击高亮格落子,把夹在你棋子之间的对方棋子翻面。结束时棋子多的一方获胜。" },
            { "howto.number_merge", "滑动让所有方块移动。两个相同方块会合并翻倍。目标是合出 2048!" },
            { "howto.dots_and_boxes", "点击两点之间的连线。补上方格的第四条边即可占领该格并再走一手。占格多者获胜。" },
            { "howto.snakes", "滑动或用方向键控制方向,吃食物变长。不要撞到墙壁或自己的身体。" },
            { "howto.tetris", "移动并旋转下落的方块,填满一整行即可消除。别让方块堆到顶部。" },
            { "howto.fruit_merge", "点击某一列投放水果。相同水果相邻会合并成更大的,连锁合并可获高分。" },
            { "howto.bomb_sweep", "用方向键移动并放置炸弹,炸掉对手同时躲开爆炸。可从软块中获得道具。" },
            { "howto.maze_paint", "离开自己的领地会留下轨迹,绕回领地即可圈占围住的区域。不要压到自己的轨迹。" },
            { "howto.color_blocks", "把方块拖到棋盘上,填满整行或整列即可消除。没有方块能放下时游戏结束。" },
            { "howto.battleship", "先布置舰队(随机/开始),再点击敌方海域开火。抢先击沉对方所有舰船。" },

            // 模式
            { "mode.solo", "单人" },
            { "mode.same_device", "同屏多人" },
            { "mode.nearby_host", "建房（附近）" },
            { "mode.nearby_join", "加入（附近）" },

            // 大厅
            { "lobby.searching", "正在搜索附近的房间……" },
            { "lobby.host_waiting", "等待玩家加入……" },
            { "lobby.player_count", "{0} 人，{1} 人已就绪" },
            { "lobby.start_game", "开始游戏" },
            { "lobby.leave", "离开" },

            // 结算
            { "result.you_win", "你赢了！" },
            { "result.you_lose", "你输了" },
            { "result.draw", "平局" },
            { "result.game_over", "游戏结束" },
            { "result.final_score", "得分：{0}" },
            { "result.best_score", "最佳：{0}" },
            { "result.new_record", "新纪录！" },
            { "result.rank", "第 {0} 名" },

            // 设置
            { "settings.audio", "音频" },
            { "settings.bgm_volume", "音乐" },
            { "settings.sfx_volume", "音效" },
            { "settings.mute", "静音" },
            { "settings.haptics", "震动" },
            { "settings.display_name", "昵称" },
            { "settings.language", "语言" },
            { "settings.color", "颜色" },
            { "settings.reduced_motion", "减少动效" },
            { "settings.reset", "恢复默认" },

            // 体力
            { "energy.full", "已满" },
            { "energy.refills_in", "{0} 后恢复" },

            // 错误
            { "error.network_off", "请打开 Wi-Fi 和蓝牙。" },
            { "error.permission_denied", "权限被拒绝，请到系统设置开启。" },
        };
    }
}
