using System.Collections.Generic;
using MiniGames.GameModule;
using MiniGames.Games.Snakes;
using MiniGames.Games.Snakes.Logic;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Session;
using MiniGames.Networking.Transport;
using NUnit.Framework;

namespace MiniGames.Tests.Networking
{
    /// <summary>
    /// Full-stack: SnakesModule -> ctx.Net -> RoomManager -> MockTransport
    /// -> peer RoomManager -> GameSession -> SnakesModule. Verifies that
    /// snapshots actually flow through every layer and that host + client
    /// converge to identical state.
    /// </summary>
    public class SnakesMultiplayerIntegrationTests
    {
        private const string LocalH = "host";
        private const string LocalC = "client";
        private const int Seed = 42;

        private (SnakesModule mod, GameSession session, RoomManager room)
            BuildPlayer(string playerId, string displayName, MockNetwork net, bool isHost)
        {
            var transport = new MockTransport(playerId, net);
            var ser = new MessagePackMessageSerializer();
            var room = new RoomManager(transport, ser, playerId, displayName);

            var mod = new SnakesModule();
            var ctx = new GameContext(
                audio: null, save: null, analytics: null, haptics: null,
                net: new RoomSendChannel(room),
                localPlayerId: playerId);

            var session = new GameSession(mod, ctx, room);
            return (mod, session, room);
        }

        [Test]
        public void Two_players_converge_after_host_ticks_propagate()
        {
            var net = new MockNetwork();
            var host = BuildPlayer(LocalH, "Host", net, isHost: true);
            var client = BuildPlayer(LocalC, "Client", net, isHost: false);

            // Pair.
            RoomSnapshot lastSnap = null;
            client.room.SnapshotChanged += s => lastSnap = s;
            host.room.HostRoom("svc");
            client.room.JoinDiscovery("svc");
            // Trigger handshake.
            net.Find(LocalC).RequestConnection(new PeerId(LocalH));
            net.Find(LocalH).AcceptConnection(new PeerId(LocalC));
            Assert.IsNotNull(lastSnap, "client should have a room snapshot after pairing");
            Assert.AreEqual(2, lastSnap.Players.Count);

            // Both start the game with same seed.
            host.mod.StartMultiplayer(host.session.Context, lastSnap, Seed, isHost: true);
            client.mod.StartMultiplayer(client.session.Context, lastSnap, Seed, isHost: false);

            // Drive the host ticks; snapshots ride through the mock network.
            for (int t = 0; t < 8; t++) host.mod.MultiplayerState.HostTick();

            Assert.AreEqual(host.mod.MultiplayerState.State.Tick,
                            client.mod.MultiplayerState.State.Tick,
                            "tick counters should match");
            for (int i = 0; i < 2; i++)
            {
                Assert.AreEqual(host.mod.MultiplayerState.State.Snakes[i].Head,
                                client.mod.MultiplayerState.State.Snakes[i].Head,
                                $"snake {i} head diverged");
                Assert.AreEqual(host.mod.MultiplayerState.State.Snakes[i].Length,
                                client.mod.MultiplayerState.State.Snakes[i].Length,
                                $"snake {i} length diverged");
            }
        }

        [Test]
        public void Client_input_reaches_host_and_is_applied_on_next_tick()
        {
            var net = new MockNetwork();
            var host = BuildPlayer(LocalH, "Host", net, isHost: true);
            var client = BuildPlayer(LocalC, "Client", net, isHost: false);

            RoomSnapshot lastSnap = null;
            client.room.SnapshotChanged += s => lastSnap = s;
            host.room.HostRoom("svc");
            client.room.JoinDiscovery("svc");
            net.Find(LocalC).RequestConnection(new PeerId(LocalH));
            net.Find(LocalH).AcceptConnection(new PeerId(LocalC));

            host.mod.StartMultiplayer(host.session.Context, lastSnap, Seed, isHost: true);
            client.mod.StartMultiplayer(client.session.Context, lastSnap, Seed, isHost: false);

            // Client's snake (index 1) starts at the far corner, heading Left.
            // Pick a perpendicular direction.
            var startHeading = host.mod.MultiplayerState.State.Snakes[1].Heading;
            var newDir = startHeading == Direction.Left || startHeading == Direction.Right
                ? Direction.Up : Direction.Right;

            client.mod.MultiplayerState.LocalInput(newDir);
            host.mod.MultiplayerState.HostTick();

            Assert.AreEqual(newDir, host.mod.MultiplayerState.State.Snakes[1].Heading,
                "host should have applied the client's input");
        }
    }
}
