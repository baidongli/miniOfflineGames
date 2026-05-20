using System.Collections.Generic;
using MiniGames.Games.Snakes.Logic;
using MiniGames.Games.Snakes.Multiplayer;
using NUnit.Framework;

namespace MiniGames.Tests.Games.Snakes
{
    public class SnakesMultiplayerTests
    {
        [Test]
        public void Host_emits_snapshot_each_tick()
        {
            var host = new SnakesMultiplayer(isHost: true, localPlayerIndex: 0,
                boardWidth: 20, boardHeight: 20, playerCount: 2, seed: 1);
            SnakeSnapshot lastSnap = null;
            host.SnapshotProduced += s => lastSnap = s;

            host.HostTick();
            Assert.IsNotNull(lastSnap);
            Assert.AreEqual(1, lastSnap.Tick);
            Assert.AreEqual(2, lastSnap.Snakes.Count);
        }

        [Test]
        public void Client_input_immediately_changes_local_pending_heading()
        {
            var client = new SnakesMultiplayer(isHost: false, localPlayerIndex: 0,
                boardWidth: 20, boardHeight: 20, playerCount: 2, seed: 1);
            var startHeading = client.State.Snakes[0].Heading;
            // Pick a direction perpendicular to start, so it's not a U-turn.
            var newDir = startHeading == Direction.Right || startHeading == Direction.Left
                ? Direction.Up : Direction.Right;

            client.LocalInput(newDir);

            Assert.AreEqual(newDir, client.State.Snakes[0].PendingHeading,
                "predictive client should apply input immediately");
        }

        [Test]
        public void Client_input_produces_wire_message()
        {
            var client = new SnakesMultiplayer(isHost: false, localPlayerIndex: 0,
                boardWidth: 20, boardHeight: 20, playerCount: 2, seed: 1);
            SnakeInputCmd sent = null;
            client.InputProduced += c => sent = c;

            client.LocalInput(Direction.Up);

            Assert.IsNotNull(sent);
            Assert.AreEqual(0, sent.PlayerIndex);
            Assert.AreEqual((byte)Direction.Up, sent.NewDirection);
        }

        [Test]
        public void Snapshot_overwrites_client_state_but_preserves_local_pending_intent()
        {
            var host = new SnakesMultiplayer(isHost: true, localPlayerIndex: 0,
                boardWidth: 20, boardHeight: 20, playerCount: 2, seed: 1);
            var client = new SnakesMultiplayer(isHost: false, localPlayerIndex: 1,
                boardWidth: 20, boardHeight: 20, playerCount: 2, seed: 1);

            // Client requests a direction change locally.
            var clientStart = client.State.Snakes[1].Heading;
            var wantedDir = clientStart == Direction.Left || clientStart == Direction.Right
                ? Direction.Up : Direction.Right;
            client.LocalInput(wantedDir);

            // Host advances once (without seeing the client's input) and ships snapshot.
            SnakeSnapshot snap = null;
            host.SnapshotProduced += s => snap = s;
            host.HostTick();
            Assert.IsNotNull(snap);

            // Client applies host snapshot - host had no input for client snake.
            client.OnSnapshot(snap);

            // Client's pending intent should be preserved.
            Assert.AreEqual(wantedDir, client.State.Snakes[1].PendingHeading,
                "client's local intent should survive the snapshot snap");
        }

        [Test]
        public void Host_and_client_produce_same_state_for_same_input_sequence()
        {
            // Same seed + same input order on both sides = identical states.
            var host = new SnakesMultiplayer(isHost: true, localPlayerIndex: 0,
                boardWidth: 12, boardHeight: 12, playerCount: 2, seed: 7);
            var clientView = new SnakesMultiplayer(isHost: true, localPlayerIndex: 0,
                boardWidth: 12, boardHeight: 12, playerCount: 2, seed: 7);

            for (int t = 0; t < 10; t++)
            {
                host.HostTick();
                clientView.HostTick();
            }

            Assert.AreEqual(host.State.Tick, clientView.State.Tick);
            for (int i = 0; i < host.State.Snakes.Count; i++)
            {
                Assert.AreEqual(host.State.Snakes[i].Head, clientView.State.Snakes[i].Head,
                    $"snake {i} head diverged");
                Assert.AreEqual(host.State.Snakes[i].Length, clientView.State.Snakes[i].Length);
            }
        }

        [Test]
        public void Host_collects_remote_input_and_applies_on_next_tick()
        {
            var host = new SnakesMultiplayer(isHost: true, localPlayerIndex: 0,
                boardWidth: 20, boardHeight: 20, playerCount: 2, seed: 1);
            var startHeading = host.State.Snakes[1].Heading;
            var newDir = startHeading == Direction.Right || startHeading == Direction.Left
                ? Direction.Up : Direction.Right;

            host.OnRemoteInput(new SnakeInputCmd
            {
                PlayerIndex = 1,
                ClientTick = 0,
                NewDirection = (byte)newDir
            });
            host.HostTick();
            Assert.AreEqual(newDir, host.State.Snakes[1].Heading);
        }

        [Test]
        public void U_turn_input_is_rejected_at_source()
        {
            var client = new SnakesMultiplayer(isHost: false, localPlayerIndex: 0,
                boardWidth: 20, boardHeight: 20, playerCount: 2, seed: 1);
            var heading = client.State.Snakes[0].Heading;
            int sent = 0;
            client.InputProduced += _ => sent++;
            client.LocalInput(heading.Opposite());
            Assert.AreEqual(0, sent, "U-turn should not produce a wire message");
        }
    }
}
