using MiniGames.GameModule;
using MiniGames.Games.MazePaint;
using MiniGames.Games.MazePaint.Logic;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Session;
using MiniGames.Networking.Transport;
using NUnit.Framework;

namespace MiniGames.Tests.Networking
{
    public class MazePaintMultiplayerIntegrationTests
    {
        private static (MazePaintModule mod, GameSession session, RoomManager room, MockTransport transport)
            BuildPlayer(string id, string name, MockNetwork net)
        {
            var t = new MockTransport(id, net);
            var ser = new MessagePackMessageSerializer();
            var room = new RoomManager(t, ser, id, name);
            var mod = new MazePaintModule();
            var ctx = new GameContext(null, null, null, null,
                new RoomSendChannel(room), id);
            var session = new GameSession(mod, ctx, room);
            return (mod, session, room, t);
        }

        [Test]
        public void Host_snapshot_replicates_board_ownership_to_client()
        {
            var net = new MockNetwork();
            var host = BuildPlayer("H", "Host", net);
            var client = BuildPlayer("C", "Client", net);

            RoomSnapshot snap = null;
            client.room.SnapshotChanged += s => snap = s;
            host.room.HostRoom("svc");
            client.room.JoinDiscovery("svc");
            client.transport.RequestConnection(new PeerId("H"));
            host.transport.AcceptConnection(new PeerId("C"));
            Assert.IsNotNull(snap);

            host.mod.StartMultiplayer(host.session.Context, snap, seed: 1, isHost: true);
            client.mod.StartMultiplayer(client.session.Context, snap, seed: 1, isHost: false);

            // Drive host ticks; snapshots flow to client through the mock net.
            for (int t = 0; t < 5; t++) host.mod.MultiplayerState.HostTick();

            Assert.AreEqual(host.mod.MultiplayerState.State.Tick,
                            client.mod.MultiplayerState.State.Tick);
            // Every cell of ownership and every player head should match.
            int size = host.mod.MultiplayerState.State.Board.Size;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    Assert.AreEqual(host.mod.MultiplayerState.State.Board.OwnerAt(x, y),
                                    client.mod.MultiplayerState.State.Board.OwnerAt(x, y),
                                    $"owner mismatch at ({x},{y})");
            for (int i = 0; i < 2; i++)
                Assert.AreEqual(host.mod.MultiplayerState.State.Players[i].Head,
                                client.mod.MultiplayerState.State.Players[i].Head);
        }

        [Test]
        public void Client_input_changes_clients_snake_heading_on_host_next_tick()
        {
            var net = new MockNetwork();
            var host = BuildPlayer("H", "Host", net);
            var client = BuildPlayer("C", "Client", net);

            RoomSnapshot snap = null;
            client.room.SnapshotChanged += s => snap = s;
            host.room.HostRoom("svc");
            client.room.JoinDiscovery("svc");
            client.transport.RequestConnection(new PeerId("H"));
            host.transport.AcceptConnection(new PeerId("C"));

            host.mod.StartMultiplayer(host.session.Context, snap, seed: 1, isHost: true);
            client.mod.StartMultiplayer(client.session.Context, snap, seed: 1, isHost: false);

            // Client (player index 1) requests a turn perpendicular to its start heading.
            var startHeading = host.mod.MultiplayerState.State.Players[1].Heading;
            var newDir = startHeading == MazeDir.Left || startHeading == MazeDir.Right
                ? MazeDir.Up : MazeDir.Right;

            client.mod.MultiplayerState.LocalInput(newDir);
            host.mod.MultiplayerState.HostTick();

            Assert.AreEqual(newDir, host.mod.MultiplayerState.State.Players[1].Heading);
        }
    }
}
