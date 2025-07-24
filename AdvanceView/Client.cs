using System.Diagnostics;
using System.Net.Sockets;

namespace AdvanceView;

public class Client : IDisposable
{
    private TcpClient _client;
    private NetworkStream _stream;
    public Action<Packet>? OnMessageReceived;

    private Thread receiveThread;
    private volatile bool running;

    public bool Connected => _client.Connected;
    
    public const int Port = 34977;

    /// <summary>
    /// Advance View client class
    /// </summary>
    public Client()
    {
        _client = new TcpClient("127.0.0.1", Port);
        _stream = _client.GetStream();

        running = true;
        receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
        receiveThread.Start();
    }

    public void Send(ReadOnlySpan<byte> data)
    {
        _stream.Write(data);
    }

    private void ReceiveLoop()
    {
        // Packet format: int = 578, int Type, int length, byte[] data
        Span<byte> packet = stackalloc byte[12];
        try
        {
            while (running)
            {
                var readLen = _stream.Read(packet);
                if (readLen != 12) return; // Ignore packet
                Debug.Assert(packet[..4].Length == 4);
                Debug.Assert(packet[4..8].Length == 4);
                if (BitConverter.ToUInt32(packet[..4]) != 578)
                {
                    Console.WriteLine($"Header did not match: got {BitConverter.ToUInt32(packet[..3])}");
                    return;
                }
                var packetType = (PacketType)BitConverter.ToInt32(packet[4..8]);
                var packetLen = BitConverter.ToUInt32(packet[8..]);
                if (packetLen > 0xFFFFFF) throw new Exception("wtf are you doing exception");
                byte[] dataBuffer = new byte[packetLen];
                _stream.ReadExactly(dataBuffer);
                OnMessageReceived?.Invoke(new Packet(packetType, dataBuffer));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Receive error: " + ex.Message);
        }
    }

    private void Close()
    {
        _stream.Close();
        _client.Close();
    }

    public void Dispose()
    {
        Close();
    }
}