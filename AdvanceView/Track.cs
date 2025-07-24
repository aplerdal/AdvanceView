using System.Numerics;
using System.Runtime.InteropServices;
using Raylib_cs;

namespace AdvanceView;

public class Track : IDisposable
{
    public Texture2D? TilesTexture;
    public RenderTexture2D? TrackTexture;
    public Texture2D? AiMapOverlay;
    public byte[]? Behaviors;
    public Texture2D? BehaviorOverlay;

    public void LoadBehaviors(byte[] data)
    {
        Behaviors = data;
    }
    public void LoadTiles(byte[] data)
    {
        if (data.Length != 16384) throw new ArgumentException("data was not of the expected length", nameof(data));
        if (TilesTexture.HasValue ) Raylib.UnloadTexture(TilesTexture.Value);
        Image tileImage = Raylib.GenImageColor(256 * 8, 8, Color.White);
        for (int tile = 0; tile < data.Length/64; tile++)
        {
            for (int pixel = 0; pixel < 64; pixel++)
            {
                var col = data[tile*64 + pixel];
                Raylib.ImageDrawPixel(ref tileImage, tile*8 + pixel%8, pixel/8, new Color(col, col, col, (byte)255));   
            }
        }
        TilesTexture = Raylib.LoadTextureFromImage(tileImage);
        if (!Raylib.IsTextureValid(TilesTexture.Value))
        {
            Raylib.UnloadTexture(TilesTexture.Value);
            TilesTexture = null;
        }
        Raylib.UnloadImage(tileImage);
    }

    public void LoadTilemap(byte[] data)
    {
        if (!TilesTexture.HasValue) return;
        if (Behaviors is null) return;

        if (TrackTexture.HasValue) Raylib.UnloadRenderTexture(TrackTexture.Value);
        
        var size = (int)Math.Sqrt(data.Length);
        TrackTexture = Raylib.LoadRenderTexture(size*8, size*8);
        if (!Raylib.IsRenderTextureValid(TrackTexture.Value))
        {
            Raylib.UnloadRenderTexture(TrackTexture.Value);
            TrackTexture = null;
            return;
        }

        Raylib.BeginTextureMode(TrackTexture.Value);
        for (int i = 0; i < data.Length; i++)
        {
            var tile = data[i];
            Raylib.DrawTextureRec(TilesTexture.Value, new Rectangle(tile * 8, 0, 8, 8), 8 * new Vector2(i % size, i / size), Color.White);
        }

        Raylib.EndTextureMode();

        if (BehaviorOverlay.HasValue) Raylib.UnloadTexture(BehaviorOverlay.Value);
        var behaviorOverlayImg = Raylib.GenImageColor(size, size, new Color(0, 0, 0, 0));
        for (int i = 0; i < data.Length; i++)
        {
            var tile = data[i];
            var color = Palette[Behaviors[tile] % Palette.Length];
            Raylib.ImageDrawPixel(ref behaviorOverlayImg, i % size, i / size, color);
        }

        BehaviorOverlay = Raylib.LoadTextureFromImage(behaviorOverlayImg);
        Raylib.SetTextureFilter(BehaviorOverlay.Value, TextureFilter.Point);
        Raylib.UnloadImage(behaviorOverlayImg);
        if (!Raylib.IsTextureValid(BehaviorOverlay.Value))
        {
            Raylib.UnloadTexture(BehaviorOverlay.Value);
            BehaviorOverlay = null;
            return;
        }
    }

    private const int Opacity = 255;
    private static readonly Color[] Palette = [
        new(0xFF, 0xAD, 0xAD, Opacity),
        //new(0xFF, 0xD6, 0xA5, Opacity),
        new(0xFD, 0xFF, 0xB6, Opacity),
        //new(0xCA, 0xFF, 0xBF, Opacity),
        new(0x96, 0xE8, 0xFF, Opacity),
        //new(0xA0, 0xC4, 0xFF, Opacity),
        new(0xBD, 0xB2, 0xFF, Opacity),
        //new(0xDE, 0xBC, 0xFF, Opacity),
        new(0xFF, 0xC6, 0xFF, Opacity),
        //new(0xFF, 0xA7, 0xDC, Opacity),
    ];
    public void LoadAiMap(byte[] data)
    {
        if (AiMapOverlay.HasValue) Raylib.UnloadTexture(AiMapOverlay.Value);

        var size = (int)Math.Sqrt(data.Length);
        var aiOverlayImg = Raylib.GenImageColor(size, size, new Color(0,0,0,0));
        for (int i = 0; i < data.Length; i++)
        {
            var zoneId = data[i];
            var color = Palette[zoneId % Palette.Length];
            if (zoneId != 0x7F)
                Raylib.ImageDrawPixel(ref aiOverlayImg, i % size, i / size, color);
        }
        AiMapOverlay = Raylib.LoadTextureFromImage(aiOverlayImg);
        Raylib.UnloadImage(aiOverlayImg);
        if (!Raylib.IsTextureValid(AiMapOverlay.Value))
        {
            Raylib.UnloadTexture(AiMapOverlay.Value);
            AiMapOverlay = null;
            return;
        }
        Raylib.SetTextureFilter(AiMapOverlay.Value, TextureFilter.Point);
        
    }

    public void Dispose()
    {
        if (AiMapOverlay.HasValue) Raylib.UnloadTexture(AiMapOverlay.Value);
        if (TilesTexture.HasValue) Raylib.UnloadTexture(TilesTexture.Value);
        if (TrackTexture.HasValue) Raylib.UnloadRenderTexture(TrackTexture.Value);
        if (BehaviorOverlay.HasValue) Raylib.UnloadTexture(BehaviorOverlay.Value);
        GC.SuppressFinalize(this);
    }

    ~Track() => Dispose();
}