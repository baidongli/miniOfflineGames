using System;
using MessagePack;
using MessagePack.Resolvers;

namespace MiniGames.Networking.Protocol
{
    /// <summary>
    /// MessagePack-CSharp implementation of IMessageSerializer.
    /// Wire format: [1 byte MessageType][body bytes].
    /// </summary>
    public sealed class MessagePackMessageSerializer : IMessageSerializer
    {
        private readonly MessagePackSerializerOptions _options;

        public MessagePackMessageSerializer()
        {
            // ContractlessStandardResolver lets us serialize POCOs without
            // declaring [MessagePackObject] on every type, while still honoring
            // attributes when present. StandardResolver alone would require attributes.
            var resolver = CompositeResolver.Create(
                StandardResolver.Instance,
                ContractlessStandardResolver.Instance
            );
            _options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
        }

        public ArraySegment<byte> Encode<T>(MessageType type, T body)
        {
            var bodyBytes = MessagePackSerializer.Serialize(body, _options);
            var buffer = new byte[1 + bodyBytes.Length];
            buffer[0] = (byte)type;
            Buffer.BlockCopy(bodyBytes, 0, buffer, 1, bodyBytes.Length);
            return new ArraySegment<byte>(buffer);
        }

        public bool TryDecode<T>(ArraySegment<byte> payload, out MessageType type, out T body)
        {
            if (payload.Count < 1)
            {
                type = MessageType.Unknown;
                body = default;
                return false;
            }

            type = (MessageType)payload.Array[payload.Offset];
            var bodyView = new ArraySegment<byte>(payload.Array, payload.Offset + 1, payload.Count - 1);
            try
            {
                body = MessagePackSerializer.Deserialize<T>(bodyView, _options);
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[Protocol] decode failed for {typeof(T).Name}: {ex.Message}");
                body = default;
                return false;
            }
        }

        public MessageType PeekType(ArraySegment<byte> payload)
        {
            if (payload.Count < 1) return MessageType.Unknown;
            return (MessageType)payload.Array[payload.Offset];
        }
    }
}
