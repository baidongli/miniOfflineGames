using System;
using System.Collections.Generic;
using MiniGames.Networking.Protocol;
using MiniGames.Networking.Session;
using MiniGames.Networking.Transport;
using NUnit.Framework;

namespace MiniGames.Tests.Networking
{
    public class GameMessageRoutingTests
    {
        private MockNetwork _net;
        private MockTransport _hostT, _clientT;
        private RoomManager _hostRoom, _clientRoom;
        private JsonMessageSerializer _ser;

        [SetUp]
        public void Setup()
        {
            _net = new MockNetwork();
            _hostT = new MockTransport("host", _net);
            _clientT = new MockTransport("c1", _net);
            _ser = new JsonMessageSerializer();
            _hostRoom = new RoomManager(_hostT, _ser, "host", "Host");
            _clientRoom = new RoomManager(_clientT, _ser, "c1", "Bob");

            _hostRoom.HostRoom("svc");
            _clientRoom.JoinDiscovery("svc");
            _clientT.RequestConnection(new PeerId("host"));
            _hostT.AcceptConnection(new PeerId("c1"));
        }

        [Test]
        public void Client_BroadcastGameMessage_arrives_at_host_as_GameMessageReceived()
        {
            var got = new List<(MessageType type, byte[] body)>();
            _hostRoom.GameMessageReceived += (peer, t, payload) =>
            {
                var copy = new byte[payload.Count];
                Buffer.BlockCopy(payload.Array, payload.Offset, copy, 0, payload.Count);
                got.Add((t, copy));
            };

            _clientRoom.BroadcastGameMessage(MessageType.GameSpecificBase, new byte[] { 9, 8, 7 }, reliable: true);

            Assert.AreEqual(1, got.Count);
            Assert.AreEqual(MessageType.GameSpecificBase, got[0].type);
            CollectionAssert.AreEqual(new byte[] { 9, 8, 7 }, got[0].body);
        }

        [Test]
        public void SendGameMessageToHost_only_reaches_host()
        {
            var hostGot = 0;
            _hostRoom.GameMessageReceived += (_, __, ___) => hostGot++;

            _clientRoom.SendGameMessageToHost(MessageType.GameSpecificBase, new byte[] { 1 }, reliable: true);

            Assert.AreEqual(1, hostGot);
        }

        [Test]
        public void Client_tracks_HostPeer_after_connection()
        {
            Assert.IsNotNull(_clientRoom.HostPeer);
            Assert.AreEqual("host", _clientRoom.HostPeer.Value.Value);
        }

        [Test]
        public void Standard_messages_do_not_fire_GameMessageReceived()
        {
            int count = 0;
            _clientRoom.GameMessageReceived += (_, __, ___) => count++;
            _hostRoom.SelectGame("color_blocks");  // emits a RoomSnapshot to client
            Assert.AreEqual(0, count);
        }
    }
}
