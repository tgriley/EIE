using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DCM.Core.Rendering;

// CPU-side copy of a texture's pixels. The raycaster samples these directly
// and never touches GPU textures.
public sealed record PixelTexture(Color[] Pixels, int Width, int Height)
{
    public static PixelTexture Load(ContentManager content, string name)
    {
        var tex = content.Load<Texture2D>(name);
        var pixels = new Color[tex.Width * tex.Height];
        tex.GetData(pixels);
        return new PixelTexture(pixels, tex.Width, tex.Height);
    }
}
