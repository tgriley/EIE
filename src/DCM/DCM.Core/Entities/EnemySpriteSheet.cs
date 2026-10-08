using DCM.Core.Rendering;
using Microsoft.Xna.Framework;

namespace DCM.Core.Entities;

public class EnemySpriteSheet
{
    public const int DefaultFrameCount = 6;

    private readonly PixelTexture _texture;

    public Color[] Pixels => _texture.Pixels;
    public int Width => _texture.Width;
    public int Height => _texture.Height;
    public int FrameCount { get; }
    public int FrameWidth => Width / FrameCount;

    public EnemySpriteSheet(PixelTexture texture, int frameCount = DefaultFrameCount)
    {
        _texture = texture;
        FrameCount = frameCount;
    }
}
