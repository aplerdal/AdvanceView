using System.Net.Sockets;
using System.Numerics;
using System.Text;
using Raylib_cs;

namespace AdvanceView;

class Program
{
    private static Client? _client;
    public static Track? Track;
    public static Driver Player = new Driver();
    public static bool freecam = false; // TODO: Enum
    public static bool detailed = true;
    public static RenderMode RenderMode = RenderMode.Map;
    
    public static Queue<Action> LoadActions = new Queue<Action>();
    

    public static Camera2D Camera = new Camera2D
    {
        Offset = new Vector2(0, 0),
        Target = new Vector2(0, 0),
        Rotation = 0.0f,
        Zoom = 1.0f
    };
    
    static void Main()
    {
        Raylib.InitWindow(1280, 720, "Advance View");
        Raylib.SetTargetFPS(60);
        Raylib.SetExitKey(KeyboardKey.Null);

        // Try to create client at startup
        _client = CreateClient();
        PaletteShader.Load();
        Camera.Offset = new Vector2(Raylib.GetScreenWidth() / 2, Raylib.GetScreenHeight() / 2);

        while (!Raylib.WindowShouldClose())
        {
            HandleInput();
            
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.White);
            if (_client is not null)
            {
                Raylib.BeginMode2D(Camera);
                Update();
                Raylib.EndMode2D();
                DrawUI();
            }
            else
            {
                NoClientScreen();
            }
            
            Raylib.EndDrawing();
        }
        Raylib.CloseWindow();
    }

    static void NoClientScreen()
    {
        Raylib.DrawText("Not Connected.", 10, 10, 30, Color.Black);
        Raylib.DrawText("Make sure the lua script is running.", 10, 50, 30, Color.Black);
        _client = CreateClient();
    }
    static void HandlePacket(Packet packet)
    {
        switch (packet.Type)
        {
            case PacketType.TrackLoaded:
                Console.WriteLine("Loading track...");
                Track?.Dispose();
                Track = new Track();
                break;
            case PacketType.TrackUnloaded:
                Console.WriteLine("Unloading Track");
                Track = null;
                break;
            case PacketType.TileGfx:
                Console.WriteLine("Received tileset data");
                LoadActions.Enqueue(()=>Track?.LoadTiles(packet.Data));
                break;
            case PacketType.TrackMap:
                Console.WriteLine("Received tilemap data");
                LoadActions.Enqueue(() => Track?.LoadTilemap(packet.Data));
                break;
            case PacketType.PaletteUpdate:
                LoadActions.Enqueue(() => PaletteShader.SetPalette(packet.Data));
                break;
            case PacketType.Driver:
                LoadActions.Enqueue(() => Player.UpdateFromPacket(packet.Data));
                break;
            case PacketType.AiMap:
                LoadActions.Enqueue(() => Track?.LoadAiMap(packet.Data));
                break;
            case PacketType.Behaviors:
                LoadActions.Enqueue(() => Track?.LoadBehaviors(packet.Data));
                break;
        }
    }

    static Client? CreateClient()
    {
        Client client;
        try
        {
            client = new Client();
        }
        catch
        {
            return null;
        }

        InitClient(client);
        return client;
    }

    static void InitClient(Client client)
    {
        client.OnMessageReceived += HandlePacket;
    }

    static bool isPanning = false;
    static void HandleInput()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.One)) RenderMode ^= RenderMode.Map;
        if (Raylib.IsKeyPressed(KeyboardKey.Two)) RenderMode ^= RenderMode.Ai;
        if (Raylib.IsKeyPressed(KeyboardKey.Three)) RenderMode ^= RenderMode.Collision;
        if (Raylib.IsKeyPressed(KeyboardKey.C)) freecam ^= true;
        
        float wheel = Raylib.GetMouseWheelMove();
        if (wheel != 0)
        {
            Vector2 mousePos = Raylib.GetMousePosition();
            Vector2 worldPosBeforeZoom = Raylib.GetScreenToWorld2D(mousePos, Camera);
            
            Camera.Zoom += wheel * 0.1f;
            if (Camera.Zoom < 0.1f) Camera.Zoom = 0.1f;
            if (Camera.Zoom > 10f) Camera.Zoom = 10f;

            Vector2 worldPosAfterZoom = Raylib.GetScreenToWorld2D(mousePos, Camera);

            // Adjust camera target to keep the zoom centered on the mouse
            Vector2 delta = worldPosBeforeZoom - worldPosAfterZoom;
            Camera.Target += delta;
        }

        if (Raylib.IsMouseButtonPressed(MouseButton.Middle))
        {
            isPanning = true;
        }

        if (Raylib.IsMouseButtonDown(MouseButton.Middle) && isPanning && freecam)
        {
            Camera.Target -= Raylib.GetMouseDelta() / Camera.Zoom;
        }

        if (Raylib.IsMouseButtonReleased(MouseButton.Middle))
        {
            isPanning = false;
        }

        if (!freecam)
        {
            Camera.Target = new Vector2(Player.X, Player.Y);
        }
    }

    static void DrawPlayer()
    {
        Raylib.DrawRectangle((int)(Player.X - 5), (int)(Player.Y - 5), 10, 10, Color.Red);
    }
    static void Update()
    {
        while (LoadActions.Count > 0)
        {
            var action = LoadActions.Dequeue();
            action();
        }
        if (Track is not null)
        {
            if (Track.TrackTexture.HasValue && RenderMode.HasFlag(RenderMode.Map))
            {
                PaletteShader.Begin();
                Raylib.DrawTextureRec(
                    Track.TrackTexture.Value.Texture, 
                    new Rectangle(0, 0, Track.TrackTexture.Value.Texture.Width, -Track.TrackTexture.Value.Texture.Height), 
                    Vector2.Zero, 
                    Color.White
                    );
                PaletteShader.End();
            }

            if (Track.AiMapOverlay.HasValue && RenderMode.HasFlag(RenderMode.Ai))
            {
                var tint = new Color(255, 255, 255, RenderMode.HasFlag(RenderMode.Map) ? 128 : 255);
                Raylib.DrawTextureEx(Track.AiMapOverlay.Value, Vector2.Zero, 0, 16, tint);
            }

            if (Track.BehaviorOverlay.HasValue && RenderMode.HasFlag(RenderMode.Collision))
            {
                var tint = new Color(255, 255, 255, RenderMode.HasFlag(RenderMode.Map) ? 128 : 255);
                Raylib.DrawTextureEx(Track.BehaviorOverlay.Value, Vector2.Zero, 0, 8, tint);
            }

            DrawPlayer();
            //Console.WriteLine($"{Player.X}, {Player.Y}");
        }
    }

    static void DrawUI()
    {
        Raylib.DrawRectangle(0,0,220, 200, Color.White);
        Raylib.DrawText($"X: {Player.X}", 10, 10, 20, Color.Black);
        Raylib.DrawText($"Y: {Player.Y}", 10, 32, 20, Color.Black);
        Raylib.DrawText($"Tile X: {Math.Round(Player.X / 8)}", 10, 54, 20, Color.Black);
        Raylib.DrawText($"Tile Y: {Math.Round(Player.Y / 8)}", 10, 76, 20, Color.Black);
        Raylib.DrawText("Press 1, 2, or 3", 10, 98, 20, Color.Black);
        Raylib.DrawText("To Toggle layers", 10, 120, 20, Color.Black);
        Raylib.DrawText("Press C", 10, 142, 20, Color.Black);
        Raylib.DrawText("To Toggle Freecam", 10, 164, 20, Color.Black);
    }
}