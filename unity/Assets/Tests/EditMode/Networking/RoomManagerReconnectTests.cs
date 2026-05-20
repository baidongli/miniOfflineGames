using System;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Session;
using MiniGames.Networking.Transport;
using NUnit.Framework;

namespace MiniGames.Tests.Networking
{
    public class RoomManagerReconnectTests
    {
        private MockNetwork _net;
        private MockTransport _hostT, _clientT;
        private RoomManager _host;
        private RoomManager _client;
        private DateTimeOffset _now = new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);

        [SetUp]
        public void Setup()
        {
            _net = new MockNetwork();
            _hostT = new MockTransport("host", _net);
            _clientT = new MockTransport("c1", _net);
            var ser = new MessagePackMessageSerializer();
            _host = new RoomManager(_hostT, ser, "host", "Host", now: () => _now);
            _host.ReconnectGrace = TimeSpan.FromSeconds(10);
            _client = new RoomManager(_clientT, ser, "c1", "Bob", now: () => _now);

            _host.HostRoom("svc");
            _client.JoinDiscovery("svc");
            _clientT.RequestConnection(new PeerId("host"));
            _hostT.AcceptConnection(new PeerId("c1"));
        }

        [Test]
        public void Disconnected_player_appears_in_snapshot_as_IsConnected_false_during_grace()
        {
            RoomSnapshot last = null;
            _host.SnapshotChanged += s => last = s;

            _hostT.Disconnect(new PeerId("c1"));

            Assert.IsNotNull(last);
            Assert.AreEqual(2, last.Players.Count, "grace-period slot should still appear");
            var c1Slot = last.Players.Find(p => p.PlayerId == "c1");
            Assert.IsNotNull(c1Slot);
            Assert.IsFalse(c1Slot.IsConnected);
        }

        [Test]
        public void Reconnection_within_grace_restores_slot_with_same_color_index()
        {
            // Capture original color index.
            int originalColorIndex = -1;
            foreach (var kv in _host.ConnectedPlayers)
                if (kv.Value.PlayerId == "c1") originalColorIndex = kv.Value.ColorIndex;
            Assert.AreNotEqual(-1, originalColorIndex);

            // Disconnect, advance time within grace, reconnect.
            _hostT.Disconnect(new PeerId("c1"));
            _now += TimeSpan.FromSeconds(3);

            // Reconnect: client paired again and sent a new Hello.
            // Easiest in MockTransport: just re-pair via accept.
            PlayerSlot restored = default;
            bool fired = false;
            _host.PlayerRestored += (_, slot) => { restored = slot; fired = true; };

            _clientT.RequestConnection(new PeerId("host"));
            _hostT.AcceptConnection(new PeerId("c1"));

            Assert.IsTrue(fired, "PlayerRestored should fire for a within-grace reconnect");
            Assert.AreEqual(originalColorIndex, restored.ColorIndex,
                "color index should be preserved across reconnect");
            Assert.AreEqual("c1", restored.PlayerId);
            Assert.IsTrue(restored.IsConnected);
        }

        [Test]
        public void PruneStaleDisconnects_drops_slot_after_grace_expires()
        {
            _hostT.Disconnect(new PeerId("c1"));
            _now += TimeSpan.FromSeconds(11);
            _host.PruneStaleDisconnects();

            // After prune, a new Hello with same id should create a FRESH slot,
            // not restore the old one.
            bool fired = false;
            _host.PlayerRestored += (_, __) => fired = true;
            _clientT.RequestConnection(new PeerId("host"));
            _hostT.AcceptConnection(new PeerId("c1"));

            Assert.IsFalse(fired, "after grace expires, reconnect should be a fresh join");
        }
    }
}
