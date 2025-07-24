namespace AdvanceView;

public class Driver
{
    public float X { get; set; }
    public float Y { get; set; }

    public void UpdateFromPacket(byte[] data)
    {
        var x = BitConverter.ToInt32(data, 0);
        var y = BitConverter.ToInt32(data, 4);
        X = x / (65536.0f);
        Y = y / (65536.0f);
    }
}