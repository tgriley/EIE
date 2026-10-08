using DCM.Core.Entities;
using DCM.Core.Rendering;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DCM.Core.World;

// All art a stage needs, pulled from the content pipeline into CPU-side pixel
// arrays for the raycaster.
public sealed class LevelAssets
{
    public PixelTexture Wall { get; }
    public PixelTexture Door { get; }
    public PixelTexture Floor { get; }
    public PixelTexture Ceiling { get; }
    public Texture2D Weapon { get; }
    public EnemySpriteSheet[] EnemySheets { get; }
    public EnemySpriteSheet[] EnemyHideSheets { get; }

    private LevelAssets(ContentManager content, int textureVariant)
    {
        Wall    = PixelTexture.Load(content, $"TextureWall{textureVariant}");
        Door    = PixelTexture.Load(content, "TextureDoor0");
        Floor   = PixelTexture.Load(content, $"TextureFloor{textureVariant}");
        Ceiling = PixelTexture.Load(content, $"TextureCeiling{textureVariant}");
        Weapon  = content.Load<Texture2D>("Camera");

        EnemySheets = new EnemySpriteSheet[EnemyCatalog.SpriteCount];
        for (var i = 0; i < EnemySheets.Length; i++)
            EnemySheets[i] = new EnemySpriteSheet(PixelTexture.Load(content, $"SpritesheetEnemy{i}"));

        EnemyHideSheets = new EnemySpriteSheet[EnemyCatalog.HideSpriteCount];
        for (var i = 0; i < EnemyHideSheets.Length; i++)
            EnemyHideSheets[i] = new EnemySpriteSheet(PixelTexture.Load(content, $"SpritesheetEnemy{i}_hide"));
    }

    public static LevelAssets Load(ContentManager content, int textureVariant) => new(content, textureVariant);
}
