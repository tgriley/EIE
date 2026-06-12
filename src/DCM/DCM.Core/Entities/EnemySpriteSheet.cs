using Microsoft.Xna.Framework;

namespace DCM.Core.Entities;

public class EnemySpriteSheet
{
    public Color[] Pixels { get; }
    public int Width { get; }
    public int Height { get; }
    public int FrameCount { get; }
    public int FrameWidth => Width / FrameCount;

    public EnemySpriteSheet(Color[] pixels, int width, int height, int frameCount)
    {
        Pixels = pixels;
        Width = width;
        Height = height;
        FrameCount = frameCount;
    }
}