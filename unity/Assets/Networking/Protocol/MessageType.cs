namespace MiniGames.Networking.Protocol
{
    /// <summary>
    /// Wire-level message types. Stable: never repurpose existing values. New types append.
    /// 0x80..0xFF reserved for game-specific subtypes routed via IGameModule.OnPeerMessage.
    /// </summary>
    public enum MessageType : byte
    {
        Unknown        = 0x00,
        Hello          = 0x01, // C -> H : introduce self
        RoomSnapshot   = 0x02, // H -> C : current room state
        PlayerReady    = 0x03, // C -> H : toggle ready
        SelectGame     = 0x04, // H -> C : host selected a game
        StartGame      = 0x05, // H -> C : begin countdown
        InputCommand   = 0x06, // C -> H : per-tick input (unreliable)
        StateSnapshot  = 0x07, // H -> C : per-tick state (unreliable)
        GameEvent      = 0x08, // H -> C : discrete event (reliable)
        EndGame        = 0x09, // H -> C : results
        Ping           = 0x0A,
        Pong           = 0x0B,
        Chat           = 0x0C,

        GameSpecificBase = 0x80
    }
}
