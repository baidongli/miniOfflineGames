using MiniGames.Games.MazePaint.Logic;
using MiniGames.Games.MazePaint.Multiplayer;
using NUnit.Framework;

namespace MiniGames.Tests.Games.MazePaint
{
    public class MazePaintMultiplayerTests
    {
        [Test]
        public void Host_emits_snapshot_each_tick()
        {
            var host = new MazePaintMultiplayer(true, 0, boardSize: 12, playerCount: 2);
            MazeSnapshot last = null;
            host.SnapshotProduced += s => last = s;
            host.HostTick();
            Assert.IsNotNull(last);
            Assert.AreEqual(1, last.Tick);
            Assert.AreEqual(2, last.Players.Count);
        }

        [Test]
        public void Client_input_applies_locally_and_produces_wire_message()
        {
            var client = new MazePaintMultiplayer(false, 0, boardSize: 12, playerCount: 2);
            var startHeading = client.State.Players[0].Heading;
            var newDir = startHeading == MazeDir.Right || startHeading == MazeDir.Left
                ? MazeDir.Up : MazeDir.Right;

            MazeInputCmd cmd = null;
            client.InputProduced += c => cmd = c;
            client.LocalInput(newDir);

            Assert.AreEqual(newDir, client.State.Players[0].PendingHeading);
            Assert.IsNotNull(cmd);
            Assert.AreEqual((byte)newDir, cmd.NewDirection);
        }

        [Test]
        public void Snapshot_round_trips_through_serializer()
        {
            var host = new MazePaintMultiplayer(true, 0, boardSize: 8, playerCount: 2);
            host.HostTick();
            var snap = MazePaintSerialization.Encode(host.State);

            // Apply to a fresh state of same shape.
            var mirror = new MazePaintGameState(snap.BoardSize, snap.Players.Count);
            MazePaintSerialization.ApplyTo(snap, mirror);

            Assert.AreEqual(host.State.Tick, mirror.Tick);
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    Assert.AreEqual(host.State.Board.OwnerAt(x, y), mirror.Board.OwnerAt(x, y));
                    Assert.AreEqual(host.State.Board.TrailAt(x, y), mirror.Board.TrailAt(x, y));
                }
            for (int i = 0; i < host.State.Players.Count; i++)
            {
                Assert.AreEqual(host.State.Players[i].Head, mirror.Players[i].Head);
                Assert.AreEqual(host.State.Players[i].Heading, mirror.Players[i].Heading);
            }
        }
    }
}
