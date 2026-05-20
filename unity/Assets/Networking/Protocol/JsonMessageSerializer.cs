using System;
using System.Text;
using Newtonsoft.Json;

namespace MiniGames.Networking.Protocol
{
    /// <summary>
    /// IMessageSerializer using Newtonsoft.Json (UPM
    /// com.unity.nuget.newtonsoft-json). Slightly larger payloads than
    /// MessagePack but Unity-native and battle-tested.
    /// Wire format: [1 byte MessageType][UTF-8 JSON body].
    /// </summary>
    public sealed class JsonMessageSerializer : IMessageSerializer
    {
        public ArraySegment<byte> Encode<T>(MessageType type, T body)
        {
            var json = JsonConvert.SerializeObject(body);
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var buffer = new byte[1 + jsonBytes.Length];
            buffer[0] = (byte)type;
            Buffer.BlockCopy(jsonBytes, 0, buffer, 1, jsonBytes.Length);
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
            try
            {
                string json = Encoding.UTF8.GetString(payload.Array, payload.Offset + 1, payload.Count - 1);
                body = JsonConvert.DeserializeObject<T>(json);
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

    /// <summary>Convenience for game modules: serialize a body without the type byte (caller adds it via ctx.Net.Broadcast/SendToHost which already frames).</summary>
    public static class Json
    {
        public static byte[] Serialize<T>(T body)
            => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body));

        public static T Deserialize<T>(ArraySegment<byte> payload)
        {
            if (payload.Count <= 0) return default;
            var json = Encoding.UTF8.GetString(payload.Array, payload.Offset, payload.Count);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
