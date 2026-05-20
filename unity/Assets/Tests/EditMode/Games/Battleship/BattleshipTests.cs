using System;
using MiniGames.Games.Battleship.AI;
using MiniGames.Games.Battleship.Logic;
using MiniGames.Games.Battleship.Multiplayer;
using NUnit.Framework;

namespace MiniGames.Tests.Games.Battleship
{
    public class BattleshipBoardTests
    {
        [Test]
        public void Place_horizontal_carrier_lays_five_cells()
        {
            var b = new BattleshipBoard();
            Assert.IsTrue(b.TryPlaceShip(ShipKind.Carrier, 0, 0, ShipOrientation.Horizontal));
            for (int i = 0; i < 5; i++)
                Assert.AreEqual((byte)ShipKind.Carrier, b.ShipAt(i, 0));
        }

        [Test]
        public void Cannot_place_overlapping_ships()
        {
            var b = new BattleshipBoard();
            Assert.IsTrue(b.TryPlaceShip(ShipKind.Carrier, 0, 0, ShipOrientation.Horizontal));
            Assert.IsFalse(b.TryPlaceShip(ShipKind.Destroyer, 2, 0, ShipOrientation.Horizontal));
        }

        [Test]
        public void Cannot_place_out_of_bounds()
        {
            var b = new BattleshipBoard();
            // Carrier (5) at x=6 horizontal would end at x=10 (OOB).
            Assert.IsFalse(b.TryPlaceShip(ShipKind.Carrier, 6, 0, ShipOrientation.Horizontal));
        }

        [Test]
        public void Shot_on_empty_cell_is_miss()
        {
            var b = new BattleshipBoard();
            var r = b.RegisterIncomingShot(0, 0, out _, out _);
            Assert.AreEqual(ShotResult.Miss, r);
        }

        [Test]
        public void Shot_on_ship_cell_is_hit_then_sunk()
        {
            var b = new BattleshipBoard();
            b.TryPlaceShip(ShipKind.Destroyer, 0, 0, ShipOrientation.Horizontal);  // 2 cells
            var r1 = b.RegisterIncomingShot(0, 0, out _, out _);
            Assert.AreEqual(ShotResult.Hit, r1);
            var r2 = b.RegisterIncomingShot(1, 0, out var sunkKind, out var cells);
            Assert.AreEqual(ShotResult.Sunk, r2);
            Assert.AreEqual(ShipKind.Destroyer, sunkKind);
            Assert.AreEqual(2, cells.Count);
        }

        [Test]
        public void AllShipsSunk_after_full_fleet_destroyed()
        {
            var b = new BattleshipBoard();
            var rng = new Random(1);
            new SimpleBattleshipAI().PlaceFleet(b, rng);
            // Bombard every cell of the grid.
            for (int y = 0; y < BattleshipBoard.Size; y++)
                for (int x = 0; x < BattleshipBoard.Size; x++)
                    b.RegisterIncomingShot(x, y, out _, out _);
            Assert.IsTrue(b.AllShipsSunk());
        }
    }

    public class BattleshipGameTests
    {
        [Test]
        public void Game_starts_in_Setup_phase()
        {
            var g = new BattleshipGame(localSeat: 0);
            Assert.AreEqual(BattleshipPhase.Setup, g.Phase);
        }

        [Test]
        public void DeclareReady_requires_all_ships_placed()
        {
            var g = new BattleshipGame(localSeat: 0);
            g.TryPlaceShip(ShipKind.Destroyer, 0, 0, ShipOrientation.Horizontal);
            Assert.IsFalse(g.DeclareMyFleetReady(), "only 1 of 5 ships placed");
        }

        [Test]
        public void Phase_advances_to_Playing_when_both_ready()
        {
            var g = new BattleshipGame(localSeat: 0);
            new SimpleBattleshipAI().PlaceFleet(g.OwnFleet, new Random(1));
            Assert.IsTrue(g.DeclareMyFleetReady());
            Assert.AreEqual(BattleshipPhase.Setup, g.Phase);  // still waiting for opp
            g.NoteOpponentFleetReady();
            Assert.AreEqual(BattleshipPhase.Playing, g.Phase);
        }

        [Test]
        public void IsLocalTurn_is_true_for_seat_zero_at_start_of_play()
        {
            var g = new BattleshipGame(localSeat: 0);
            new SimpleBattleshipAI().PlaceFleet(g.OwnFleet, new Random(1));
            g.DeclareMyFleetReady();
            g.NoteOpponentFleetReady();
            Assert.IsTrue(g.IsLocalTurn);
        }
    }

    public class MultiplayerBattleshipTests
    {
        [Test]
        public void Seat_assignment_by_player_id_is_deterministic()
        {
            var a = new MultiplayerBattleship("alice", "bob");
            var b = new MultiplayerBattleship("bob", "alice");
            Assert.AreEqual(0, a.Game.LocalSeat);
            Assert.AreEqual(1, b.Game.LocalSeat);
        }

        [Test]
        public void Shot_round_trips_through_two_peers()
        {
            var alice = new MultiplayerBattleship("alice", "bob");
            var bob   = new MultiplayerBattleship("bob", "alice");
            var rng = new Random(1);
            new SimpleBattleshipAI().PlaceFleet(alice.Game.OwnFleet, rng);
            new SimpleBattleshipAI().PlaceFleet(bob.Game.OwnFleet, rng);
            alice.ShipsReadyOutgoing += m => bob.OnShipsReadyReceived(m);
            bob.ShipsReadyOutgoing   += m => alice.OnShipsReadyReceived(m);
            alice.ShotFiredOutgoing  += m => bob.OnShotFiredReceived(m);
            bob.ShotFiredOutgoing    += m => alice.OnShotFiredReceived(m);
            alice.ShotResultOutgoing += m => bob.OnShotResultReceived(m);
            bob.ShotResultOutgoing   += m => alice.OnShotResultReceived(m);

            alice.DeclareReady();
            bob.DeclareReady();
            Assert.AreEqual(BattleshipPhase.Playing, alice.Game.Phase);
            Assert.AreEqual(BattleshipPhase.Playing, bob.Game.Phase);

            // Alice (seat 0) shoots at (5, 5). Either hit or miss depending
            // on bob's fleet layout - we just verify the protocol round-trips.
            Assert.IsTrue(alice.TryLocalShoot(5, 5));
            // After the round-trip, alice's tracker should know about (5, 5)
            // and turn should have passed to bob.
            Assert.IsTrue(alice.Game.OpponentTracker.ShotAt(5, 5));
            Assert.IsFalse(alice.Game.IsLocalTurn);
        }
    }

    public class SimpleBattleshipAITests
    {
        [Test]
        public void Place_fleet_places_all_five_ships()
        {
            var b = new BattleshipBoard();
            new SimpleBattleshipAI().PlaceFleet(b, new Random(1));
            foreach (var kind in Fleet.Standard) Assert.IsTrue(b.HasShip(kind));
        }

        [Test]
        public void Self_play_terminates_with_a_winner()
        {
            var aGame = new BattleshipGame(localSeat: 0);
            var bGame = new BattleshipGame(localSeat: 1);
            var aAi = new SimpleBattleshipAI(seed: 7);
            var bAi = new SimpleBattleshipAI(seed: 11);
            var rng = new Random(1);
            aAi.PlaceFleet(aGame.OwnFleet, rng);
            bAi.PlaceFleet(bGame.OwnFleet, rng);
            aGame.DeclareMyFleetReady(); aGame.NoteOpponentFleetReady();
            bGame.DeclareMyFleetReady(); bGame.NoteOpponentFleetReady();

            int safety = 250;
            while (aGame.Phase == BattleshipPhase.Playing && safety-- > 0)
            {
                var shooter = aGame.IsLocalTurn ? aGame : bGame;
                var target  = aGame.IsLocalTurn ? bGame : aGame;
                var shooterAi = aGame.IsLocalTurn ? aAi : bAi;
                var pick = shooterAi.ChooseShot(shooter.OpponentTracker);
                if (pick == null) break;
                shooter.TryShoot(pick.Value.x, pick.Value.y);
                var result = target.ProcessIncomingShot(pick.Value.x, pick.Value.y, out var sunkKind, out var sunkCells);
                shooter.RecordOpponentResult(pick.Value.x, pick.Value.y, result, sunkCells, sunkKind);
                shooterAi.RecordResult(result);
            }
            Assert.AreEqual(BattleshipPhase.GameOver, aGame.Phase);
            Assert.IsTrue(aGame.Winner.HasValue);
        }
    }
}
