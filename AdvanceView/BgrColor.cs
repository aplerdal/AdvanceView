namespace AdvanceView;

public class BgrColor
{
    public ushort Raw;

    public byte R
    {
        get => (byte)((Raw >> 0) & 0x1F);
        set => Raw = (ushort)((Raw & ~(0x1F << 0)) | ((value & 0x1F) << 0));
    }

    public byte G
    {
        get => (byte)((Raw >> 5) & 0x1F);
        set => Raw = (ushort)((Raw & ~(0x1F << 5)) | ((value & 0x1F) << 5));
    }

    public byte B
    {
        get => (byte)((Raw >> 10) & 0x1F);
        set => Raw = (ushort)((Raw & ~(0x1F << 10)) | ((value & 0x1F) << 10));
    }

    public BgrColor()
    {
    }

    public BgrColor(ushort value)
    {
        Raw = value;
    }
}