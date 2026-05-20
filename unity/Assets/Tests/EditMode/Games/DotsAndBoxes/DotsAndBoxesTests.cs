using MiniGames.Games.DotsAndBoxes.AI;
using MiniGames.Games.DotsAndBoxes.Logic;
using MiniGames.Games.DotsAndBoxes.Multiplayer;
using NUnit.Framework;

namespace MiniGames.Tests.Games.DotsAndBoxes
{
    public class DotsBoardTests
    {
        [Test]
        public void Box_edge_count_zero_on_empty_board()
        {
            var b = new DotsBoard(3, 3);
            Assert.AreEqual(0, b.BoxEdgeCount(0, 0));
            Assert.AreEqual(0, b.BoxEdgeCount(2, 2));
        }

        [Test]
        public void Drawing_4_edges_around_one_box_makes_count_4()
        {
            var b = new DotsBoard(3, 3);
            b.SetHEdge(0, 0, true);   // bottom of (0,0)
            b.SetHEdge(0, 1, true);   // top of (0,0)
            b.SetVEdge(0, 0, true);   // left
            b.SetVEdge(1, 0, true);   // right
            Assert.AreEqual(4, b.BoxEdgeCount(0, 0));
        }
    }

    public class DotsGameTests
    {
        [Test]
        public void Single_edge_passes_turn_when_no_box_completed()
        {
            var g = new DotsGame(playerCount: 2, boxWidth: 3, boxHeight: 3);
            Assert.AreEqual(0, g.CurrentPlayer);
            Assert.IsTrue(g.TryPlay(new EdgeId(EdgeKind.Horizontal, 0, 0), out var r));
            Assert.IsTrue(r.TurnPasses);
            Assert.AreEqual(1, g.CurrentPlayer);
        }

        [Test]
        public void Completing_a_box_keeps_the_same_player_active()
        {
            var g = new DotsGame(playerCount: 2, boxWidth: 3, boxHeight: 3);
            // Pre-draw 3 sides of box (0, 0).
            g.Board.SetHEdge(0, 0, true);   // bottom
            g.Board.SetVEdge(0, 0, true);   // left
            g.Board.SetVEdge(1, 0, true);   // right
            // Now seal it with the top edge.
            Assert.IsTrue(g.TryPlay(new EdgeId(EdgeKind.Horizontal, 0, 1), out var r));
            Assert.IsFalse(r.TurnPasses);
            Assert.AreEqual(0, g.CurrentPlayer, "claimer keeps the turn");
            Assert.AreEqual(0, g.Board.BoxOwner(0, 0));
            Assert.AreEqual(1, r.BoxesClaimed.Count);
        }

        [Test]
        public void Edge_between_two_completed_boxes_claims_both_at_once()
        {
            var g = new DotsGame(playerCount: 2, boxWidth: 3, boxHeight: 3);
            // Set up two adjacent boxes (0,0) and (0,1) so a single horizontal
            // edge between them completes both.
            // Box (0,0): bottom, left, right already drawn. Missing: top (H at y=1).
            g.Board.SetHEdge(0, 0, true);
            g.Board.SetVEdge(0, 0, true);
            g.Board.SetVEdge(1, 0, true);
            // Box (0,1): top, left, right already drawn. Missing: bottom (also H at y=1).
            g.Board.SetHEdge(0, 2, true);
            g.Board.SetVEdge(0, 1, true);
            g.Board.SetVEdge(1, 1, true);

            Assert.IsTrue(g.TryPlay(new EdgeId(EdgeKind.Horizontal, 0, 1), out var r));
            Assert.AreEqual(2, r.BoxesClaimed.Count);
            Assert.AreEqual(0, g.Board.BoxOwner(0, 0));
            Assert.AreEqual(0, g.Board.BoxOwner(0, 1));
        }

        [Test]
        public void Cannot_play_already_drawn_edge()
        {
            var g = new DotsGame(playerCount: 2);
            g.TryPlay(new EdgeId(EdgeKind.Horizontal, 0, 0), out _);
            Assert.IsFalse(g.TryPlay(new EdgeId(EdgeKind.Horizontal, 0, 0), out _));
        }

        [Test]
        public void Game_ends_when_all_boxes_owned()
        {
            // Tiny 1x1 board: only 4 edges, 1 box.
            var g = new DotsGame(playerCount: 2, boxWidth: 1, boxHeight: 1);
            g.TryPlay(new EdgeId(EdgeKind.Horizontal, 0, 0), out _);  // P0
            g.TryPlay(new EdgeId(EdgeKind.Horizontal, 0, 1), out _);  // P1
            g.TryPlay(new EdgeId(EdgeKind.Vertical, 0, 0), out _);    // P0
            // Last edge claims the box AND ends the game.
            Assert.IsTrue(g.TryPlay(new EdgeId(EdgeKind.Vertical, 1, 0), out var r));
            Assert.IsTrue(r.GameOver);
            Assert.IsTrue(g.IsGameOver);
        }
    }

    public class MultiplayerDotsTests
    {
        [Test]
        public void Seat_assignment_is_by_sorted_player_id()
        {
            var mp = new MultiplayerDots("bob", new[] { "alice", "bob", "charlie" });
            // Sorted: alice (0), bob (1), charlie (2). Bob is seat 1.
            Assert.AreEqual(1, mp.LocalSeat);
        }

        [Test]
        public void Two_peers_stay_in_sync_with_box_completion_keep_turn()
        {
            var alice = new MultiplayerDots("alice", new[] { "alice", "bob" }, 3, 3);
            var bob   = new MultiplayerDots("bob",   new[] { "alice", "bob" }, 3, 3);
            alice.MoveOutgoing += m => bob.OnMoveReceived(m);
            bob.MoveOutgoing   += m => alice.OnMoveReceived(m);

            // Alice (seat 0) plays an edge that doesn't complete - turn passes to Bob.
            Assert.IsTrue(alice.TryLocalPlay(new EdgeId(EdgeKind.Horizontal, 0, 0)));
            Assert.IsTrue(bob.IsLocalTurn);
            Assert.IsFalse(alice.IsLocalTurn);
        }
    }

    public class SimpleDotsAITests
    {
        [Test]
        public void AI_grabs_an_available_box_completion()
        {
            var g = new DotsGame(playerCount: 2, boxWidth: 3, boxHeight: 3);
            // Box (0,0): three sides drawn, missing top.
            g.Board.SetHEdge(0, 0, true);
            g.Board.SetVEdge(0, 0, true);
            g.Board.SetVEdge(1, 0, true);
            var move = new SimpleDotsAI().Choose(g);
            Assert.IsNotNull(move);
            Assert.AreEqual(EdgeKind.Horizontal, move.Value.Kind);
            Assert.AreEqual(0, move.Value.X);
            Assert.AreEqual(1, move.Value.Y);
        }

        [Test]
        public void Cpu_self_play_terminates_with_someone_winning()
        {
            var g = new DotsGame(playerCount: 2, boxWidth: 3, boxHeight: 3);
            var cpu = new CpuDotsController(g, new SimpleDotsAI());
            int safety = 200;
            while (!g.IsGameOver && safety-- > 0) cpu.TakeTurn();
            Assert.IsTrue(g.IsGameOver);
            int totalBoxes = g.Board.CountOwned(0) + g.Board.CountOwned(1);
            Assert.AreEqual(9, totalBoxes, "all 3x3 = 9 boxes should be claimed");
        }
    }
}
