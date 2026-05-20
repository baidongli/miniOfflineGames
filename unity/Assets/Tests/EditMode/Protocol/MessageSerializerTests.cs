using MiniGames.Networking.Protocol;
using NUnit.Framework;

namespace MiniGames.Tests.Protocol
{
    public class MessageSerializerTests
    {
        private MessagePackMessageSerializer _ser;

        [SetUp]
        public void Setup() => _ser = new MessagePackMessageSerializer();

        [Test]
        public void Hello_round_trips()
        {
            var original = new Hello
            {
                PlayerId = "p123",
                DisplayName = "Alice",
                AppVersionMajor = 1,
                AppVersionMinor = 2,
                Platform = "android"
            };

            var bytes = _ser.Encode(MessageType.Hello, original);
            Assert.AreEqual(MessageType.Hello, _ser.PeekType(bytes));
            Assert.IsTrue(_ser.TryDecode<Hello>(bytes, out var type, out var decoded));

            Assert.AreEqual(MessageType.Hello, type);
            Assert.AreEqual(original.PlayerId, decoded.PlayerId);
            Assert.AreEqual(original.DisplayName, decoded.DisplayName);
            Assert.AreEqual(original.AppVersionMajor, decoded.AppVersionMajor);
            Assert.AreEqual(original.AppVersionMinor, decoded.AppVersionMinor);
            Assert.AreEqual(original.Platform, decoded.Platform);
        }

        [Test]
        public void RoomSnapshot_round_trips_with_multiple_players()
        {
            var original = new RoomSnapshot
            {
                RoomId = "abcd",
                HostPlayerId = "host",
                SelectedGameId = "color_blocks"
            };
            original.Players.Add(new PlayerSlot
            {
                PlayerId = "host", DisplayName = "Host", ColorIndex = 0,
                IsHost = true, IsReady = true, IsConnected = true
            });
            original.Players.Add(new PlayerSlot
            {
                PlayerId = "c1", DisplayName = "Bob", ColorIndex = 1,
                IsHost = false, IsReady = false, IsConnected = true
            });

            var bytes = _ser.Encode(MessageType.RoomSnapshot, original);
            Assert.IsTrue(_ser.TryDecode<RoomSnapshot>(bytes, out _, out var decoded));

            Assert.AreEqual(original.RoomId, decoded.RoomId);
            Assert.AreEqual(original.Players.Count, decoded.Players.Count);
            Assert.AreEqual("Host", decoded.Players[0].DisplayName);
            Assert.AreEqual("Bob", decoded.Players[1].DisplayName);
            Assert.IsTrue(decoded.Players[0].IsHost);
            Assert.IsFalse(decoded.Players[1].IsReady);
        }

        [Test]
        public void InputCommand_preserves_byte_payload()
        {
            var payload = new byte[] { 1, 2, 3, 255, 0, 128 };
            var original = new InputCommand { ClientFrame = 42, Payload = payload };
            var bytes = _ser.Encode(MessageType.InputCommand, original);
            Assert.IsTrue(_ser.TryDecode<InputCommand>(bytes, out _, out var decoded));
            Assert.AreEqual(42, decoded.ClientFrame);
            CollectionAssert.AreEqual(payload, decoded.Payload);
        }

        [Test]
        public void StartGame_round_trips()
        {
            var msg = new StartGame { GameId = "snakes", CountdownMs = 3000, Seed = -12345 };
            var bytes = _ser.Encode(MessageType.StartGame, msg);
            Assert.IsTrue(_ser.TryDecode<StartGame>(bytes, out _, out var decoded));
            Assert.AreEqual("snakes", decoded.GameId);
            Assert.AreEqual(3000, decoded.CountdownMs);
            Assert.AreEqual(-12345, decoded.Seed);
        }

        [Test]
        public void EndGame_round_trips_results()
        {
            var msg = new EndGame();
            msg.Results.Add(new PlayerResult { PlayerId = "a", Score = 100, Place = 1 });
            msg.Results.Add(new PlayerResult { PlayerId = "b", Score = 80, Place = 2 });
            var bytes = _ser.Encode(MessageType.EndGame, msg);
            Assert.IsTrue(_ser.TryDecode<EndGame>(bytes, out _, out var decoded));
            Assert.AreEqual(2, decoded.Results.Count);
            Assert.AreEqual(100, decoded.Results[0].Score);
            Assert.AreEqual(2, decoded.Results[1].Place);
        }

        [Test]
        public void Type_byte_is_first()
        {
            var msg = new Ping { ClientSendUtcMs = 1234567890123 };
            var bytes = _ser.Encode(MessageType.Ping, msg);
            Assert.AreEqual((byte)MessageType.Ping, bytes.Array[bytes.Offset]);
        }

        [Test]
        public void Empty_payload_decodes_as_unknown_type()
        {
            var empty = new System.ArraySegment<byte>(new byte[0]);
            Assert.AreEqual(MessageType.Unknown, _ser.PeekType(empty));
            Assert.IsFalse(_ser.TryDecode<Ping>(empty, out _, out _));
        }
    }
}
