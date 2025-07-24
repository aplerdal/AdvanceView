namespace AdvanceView;

public enum PacketType
{
    TrackLoaded,
    TrackUnloaded,
    TileGfx,
    TrackMap,
    PaletteUpdate,
    Driver,
    AiMap,
    Behaviors,
}

public record Packet(PacketType Type, byte[] Data);