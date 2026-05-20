using System;

namespace MiniGames.Networking.Protocol
{
    /// <summary>
    /// Serialize a typed message to a wire payload: [1 byte MessageType][N bytes body].
    /// Concrete impl will use MessagePack-CSharp once the package is added to manifest.json.
    /// </summary>
    public interface IMessageSerializer
    {
        ArraySegment<byte> Encode<T>(MessageType type, T body);
        bool TryDecode<T>(ArraySegment<byte> payload, out MessageType type, out T body);
        MessageType PeekType(ArraySegment<byte> payload);
    }
}
